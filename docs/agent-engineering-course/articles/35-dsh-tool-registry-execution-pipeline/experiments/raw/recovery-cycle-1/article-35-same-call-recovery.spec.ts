import { afterEach, describe, expect, it, vi } from 'vitest'
import { createHash } from 'node:crypto'
import { Context } from '@deepseek-ai/cordis'
import LlmRuntime, { createUserMessage, ToolCallId, type ContentBlock, type StreamChunk } from '@deepseek-ai/dsh-llm'
import SessionStore, { SessionId, type SessionEvent } from '@deepseek-ai/dsh-session'
import SystemPrompt from '@deepseek-ai/dsh-system-prompt'
import ToolRuntime, { defineContentToolFixture, TOOL_ABORTED, TOOL_ABORTED_BEFORE_DISPATCH, type PostToolDecision, type PreToolDecision } from '@deepseek-ai/dsh-tools'
import ApprovalService, { type ApprovalOutcome } from '@deepseek-ai/dsh-user-approval'
import * as timeoutPolicy from '@deepseek-ai/dsh-tool-call-timeout-policy'
import { TOOL_TIMEOUT } from '@deepseek-ai/dsh-tool-call-timeout-policy'
import { SpillLocator, SpillStore } from '@deepseek-ai/dsh-spill'
import type { SaveTextSpill, SpillRef } from '@deepseek-ai/dsh-spill'
import * as SpillPolicy from '@deepseek-ai/dsh-spill-policy'
import AgentRegistry, { type Agent } from '@deepseek-ai/dsh-agent'
import AgentLoop from '@deepseek-ai/dsh-agent-loop'
import { MockAdapter, textResponse } from './mock-adapter.ts'

type Stage = { seq: number; stage: string; phase: string; detail: string | null }
type StageMap = Map<string, Stage[]>
type SpillAttempt = { toolName: string; bytes: number; hash: string; suggestedName: string; storageError: string | null }

function sha256(text: string): string {
  return createHash('sha256').update(text, 'utf8').digest('hex')
}

function utf8Bytes(text: string): number {
  return Buffer.byteLength(text, 'utf8')
}

function rawMultiCall(calls: { id: string; name: string; rawArgs: string }[]): StreamChunk[] {
  const chunks: StreamChunk[] = []
  calls.forEach((call, index) => {
    chunks.push(
      { type: 'block-start', index, blockType: 'tool-call' },
      { type: 'block-end', index, block: { type: 'tool-call', id: ToolCallId(call.id), name: call.name, arguments: call.rawArgs } },
    )
  })
  chunks.push(
    { type: 'usage', usage: { inputTokens: 5, outputTokens: 5 } },
    { type: 'finish', reason: { kind: 'tool-calls' } },
  )
  return chunks
}

class RecordingSpillStore extends SpillStore {
  attempts: SpillAttempt[] = []
  saves: SaveTextSpill[] = []

  async saveText(input: SaveTextSpill): Promise<SpillRef> {
    const attempt: SpillAttempt = {
      toolName: input.source.toolName,
      bytes: utf8Bytes(input.content),
      hash: sha256(input.content),
      suggestedName: input.suggestedName,
      storageError: input.source.toolName === 'big-fail' ? 'injected storage failure' : null,
    }
    this.attempts.push(attempt)
    if (attempt.storageError !== null) throw new Error(attempt.storageError)
    this.saves.push(input)
    return {
      locator: SpillLocator(`/spill/${input.suggestedName}`),
      bytes: attempt.bytes,
      retrievalHint: 'Use the in-memory recovery locator.',
    }
  }
}

interface HarnessOptions {
  approval?: boolean
  timeout?: boolean
  spill?: boolean
  maxParallelToolCalls?: number
}

async function createHarness(adapter: MockAdapter, options: HarnessOptions = {}): Promise<{ ctx: Context; spill: RecordingSpillStore | null }> {
  const ctx = new Context()
  await ctx.plugin(LlmRuntime)
  await ctx.plugin(SessionStore)
  await ctx.plugin(SystemPrompt, { persona: '' })
  await ctx.plugin(ToolRuntime)
  if (options.approval) await ctx.plugin(ApprovalService)
  if (options.timeout) await ctx.plugin(timeoutPolicy)
  let spill: RecordingSpillStore | null = null
  if (options.spill) {
    await ctx.plugin(RecordingSpillStore)
    spill = ctx.spillStore as RecordingSpillStore
    await ctx.plugin(SpillPolicy, { maxInlineBytes: 200 })
  }
  await ctx.plugin(AgentRegistry)
  await ctx.plugin(AgentLoop, {
    agents: [],
    ...(options.maxParallelToolCalls === undefined ? {} : { maxParallelToolCalls: options.maxParallelToolCalls }),
  })
  ctx.llm.registerAdapter(['mock'], adapter)
  return { ctx, spill }
}

function waitForIdle(ctx: Context, agent: Agent): Promise<void> {
  return new Promise((resolve) => {
    const dispose = ctx.on('agent/status', ({ agent: subject, status }) => {
      if (subject === agent && status === 'idle') { dispose(); resolve() }
    })
  })
}

function events(agent: Agent): SessionEvent[] {
  return [...agent.session.events]
}

function pushStage(stages: StageMap, callId: string, stage: string, phase: string, detail: string | null = null): void {
  const list = stages.get(callId) ?? []
  list.push({ seq: [...stages.values()].reduce((sum, values) => sum + values.length, 0) + 1, stage, phase, detail })
  stages.set(callId, list)
}

function installStageObservers(ctx: Context, stages: StageMap): void {
  ctx.on('tools/pre-execute', async (exec, next): Promise<PreToolDecision> => {
    const id = String(exec.callId)
    pushStage(stages, id, 'pre', 'enter')
    const decision = await next()
    pushStage(stages, id, 'pre', 'exit', decision.kind)
    return decision
  })
  ctx.on('tools/execute', async (exec, next) => {
    const id = String(exec.callId)
    pushStage(stages, id, 'execute', 'enter')
    const result = await next()
    pushStage(stages, id, 'execute', 'exit', result.error?.info?.code ?? (result.isError ? 'ERROR' : 'SUCCESS'))
    return result
  })
  ctx.on('tools/post-execute', async (exec, _result, next): Promise<PostToolDecision> => {
    const id = String(exec.callId)
    pushStage(stages, id, 'post', 'enter')
    const decision = await next()
    pushStage(stages, id, 'post', 'exit', decision.kind)
    return decision
  })
  ctx.on('tools/result', (exec, result) => {
    pushStage(stages, String(exec.callId), 'result', 'emit', result.error?.info?.code ?? (result.isError ? 'ERROR' : 'SUCCESS'))
  })
}

function boundedText(content: ContentBlock[]): string {
  return content.filter((block): block is Extract<ContentBlock, { type: 'text' }> => block.type === 'text')
    .map(block => block.text).join('').slice(0, 2048)
}

function callFor(agent: Agent, callId: string): Extract<SessionEvent, { type: 'tool/call' }> {
  const found = events(agent).filter((event): event is Extract<SessionEvent, { type: 'tool/call' }> =>
    event.type === 'tool/call' && String(event.data.callId) === callId)
  expect(found).toHaveLength(1)
  return found[0]!
}

function resultFor(agent: Agent, callId: string): Extract<SessionEvent, { type: 'tool/result' }> {
  const found = events(agent).filter((event): event is Extract<SessionEvent, { type: 'tool/result' }> =>
    event.type === 'tool/result' && String(event.data.message.source.callId) === callId)
  expect(found).toHaveLength(1)
  return found[0]!
}

function resultBlock(event: Extract<SessionEvent, { type: 'tool/result' }>): any {
  const blocks = event.data.message.content.filter(block => block.type === 'tool-result')
  expect(blocks).toHaveLength(1)
  return blocks[0]!
}

function normalizeResult(event: Extract<SessionEvent, { type: 'tool/result' }>): Record<string, unknown> {
  const block = resultBlock(event)
  const text = boundedText(block.content as ContentBlock[])
  return {
    isError: block.isError,
    errorName: event.data.error?.name ?? null,
    code: event.data.error?.code ?? null,
    message: text.slice(0, 500),
    contentBlockCount: block.content.length,
    contentText: text,
    metaPresent: event.data.meta !== undefined,
  }
}

function nextProjection(adapter: MockAdapter, agent: Agent, callId: string, requestIndex = 1): Record<string, unknown> {
  const requestBlocks = (adapter.requests[requestIndex]?.messages ?? []).flatMap(message => message.content)
    .filter((block: any) => block.type === 'tool-result' && String(block.toolCallId) === callId) as any[]
  const historyBlocks = agent.session.deriveMessages().flatMap(message => message.content)
    .filter((block: any) => block.type === 'tool-result' && String(block.toolCallId) === callId) as any[]
  expect(requestBlocks).toHaveLength(1)
  expect(historyBlocks).toHaveLength(1)
  const requestText = boundedText(requestBlocks[0]!.content)
  const historyText = boundedText(historyBlocks[0]!.content)
  expect(requestText).toBe(historyText)
  expect(requestBlocks[0]!.isError).toBe(historyBlocks[0]!.isError)
  return {
    requestIndex,
    requestMatchCount: requestBlocks.length,
    historyMatchCount: historyBlocks.length,
    resultIsError: requestBlocks[0]!.isError,
    contentHash: sha256(requestText),
    followupCompleted: null,
  }
}

function sessionReceipt(agent: Agent, callId: string): Record<string, unknown> {
  const call = callFor(agent, callId)
  const result = resultFor(agent, callId)
  const block = resultBlock(result)
  const text = boundedText(block.content)
  return {
    callCount: 1,
    resultCount: 1,
    callSeq: call.seq,
    resultSeq: result.seq,
    rawArgs: call.data.arguments,
    resultCode: result.data.error?.code ?? null,
    resultIsError: block.isError,
    contentHash: sha256(text),
  }
}

function emptySpill(): Record<string, unknown> {
  return {
    inputBytes: 0,
    fullHash: null,
    attemptCount: 0,
    saveCount: 0,
    storedHash: null,
    locator: null,
    previewBytes: 0,
    previewHash: null,
    fallbackBytes: 0,
    fallbackHash: null,
    storageError: null,
    semanticSummary: false,
  }
}

function record(caseId: string, callId: string, rawArgs: string, stages: Stage[], overrides: Partial<Record<string, unknown>> = {}): Record<string, unknown> {
  return {
    schema: 'a35-same-call-recovery-v1',
    case: caseId,
    callId,
    rawArgs,
    stages,
    policy: { pre: null, approvalAsked: 0, approvalDecided: 0, approvalOutcome: null, timeoutMs: null, spillCapBytes: null },
    body: { startCount: 0, settleCount: 0, sentinelCount: 0, signalObserved: false, signalAborted: false, drained: false },
    normalizedResult: null,
    session: null,
    nextHistory: null,
    spill: emptySpill(),
    ...overrides,
  }
}

function emit(value: Record<string, unknown>): void {
  const required = ['schema', 'case', 'callId', 'rawArgs', 'stages', 'policy', 'body', 'normalizedResult', 'session', 'nextHistory', 'spill']
  for (const key of required) if (!Object.hasOwn(value, key)) throw new Error(`trace missing field ${key}`)
  const nested: Record<string, string[]> = {
    policy: ['pre', 'approvalAsked', 'approvalDecided', 'approvalOutcome', 'timeoutMs', 'spillCapBytes'],
    body: ['startCount', 'settleCount', 'sentinelCount', 'signalObserved', 'signalAborted', 'drained'],
    normalizedResult: ['isError', 'errorName', 'code', 'message', 'contentBlockCount', 'contentText', 'metaPresent'],
    session: ['callCount', 'resultCount', 'callSeq', 'resultSeq', 'rawArgs', 'resultCode', 'resultIsError', 'contentHash'],
    nextHistory: ['requestIndex', 'requestMatchCount', 'historyMatchCount', 'resultIsError', 'contentHash', 'followupCompleted'],
    spill: ['inputBytes', 'fullHash', 'attemptCount', 'saveCount', 'storedHash', 'locator', 'previewBytes', 'previewHash', 'fallbackBytes', 'fallbackHash', 'storageError', 'semanticSummary'],
  }
  for (const [parent, keys] of Object.entries(nested)) {
    const child = value[parent] as Record<string, unknown> | null
    if (child === null) throw new Error(`trace missing object ${parent}`)
    for (const key of keys) if (!Object.hasOwn(child, key)) throw new Error(`trace missing field ${parent}.${key}`)
  }
  console.log(`A35_TRACE ${JSON.stringify(value)}`)
}

async function poll(predicate: () => boolean): Promise<void> {
  for (let i = 0; i < 1000; i++) {
    if (predicate()) return
    await Promise.resolve()
  }
  throw new Error('bounded microtask poll exhausted')
}

afterEach(() => { vi.useRealTimers() })

  it('A35 recovery / 35-X01 / SAME-CALL', async () => {
    const calls = [
      { id: 'x01-valid', name: 'reader', rawArgs: '{"path":"/ok"}' },
      { id: 'x01-malformed', name: 'reader', rawArgs: '{"path":' },
      { id: 'x01-schema', name: 'reader', rawArgs: '{}' },
    ]
    const adapter = new MockAdapter([rawMultiCall(calls), textResponse('x01-done')])
    const { ctx } = await createHarness(adapter)
    const stages: StageMap = new Map()
    const starts = new Map<string, number>()
    const settles = new Map<string, number>()
    installStageObservers(ctx, stages)
    ctx.tools.register(defineContentToolFixture({
      name: 'reader', description: 'typed reader', parameters: { path: { type: 'string', required: true } },
      isConcurrencySafe: () => true,
      async execute(args, exec) {
        const id = String(exec.callId)
        starts.set(id, (starts.get(id) ?? 0) + 1)
        settles.set(id, (settles.get(id) ?? 0) + 1)
        return [{ type: 'text', text: `read:${args.path}` }]
      },
    }))
    const agent = ctx.agentLoop.create(SessionId('a35-x01'), { provider: 'mock', model: 'mock' })
    const idle = waitForIdle(ctx, agent)
    agent.followup(createUserMessage({ content: [{ type: 'text', text: 'x01' }], source: { kind: 'user' } }))
    await idle

    expect(starts.get('x01-valid')).toBe(1)
    expect(starts.get('x01-malformed') ?? 0).toBe(0)
    expect(starts.get('x01-schema') ?? 0).toBe(0)
    expect(resultFor(agent, 'x01-valid').data.error).toBeUndefined()
    expect(resultFor(agent, 'x01-malformed').data.error?.code).toBe('INVALID_ARGS')
    expect(resultFor(agent, 'x01-schema').data.error?.code).toBe('INVALID_ARGS')

    for (const call of calls) {
      const result = resultFor(agent, call.id)
      emit(record('35-X01', call.id, call.rawArgs, stages.get(call.id) ?? [], {
        body: { startCount: starts.get(call.id) ?? 0, settleCount: settles.get(call.id) ?? 0, sentinelCount: 0, signalObserved: false, signalAborted: false, drained: (settles.get(call.id) ?? 0) === (starts.get(call.id) ?? 0) },
        normalizedResult: normalizeResult(result),
        session: sessionReceipt(agent, call.id),
        nextHistory: nextProjection(adapter, agent, call.id),
      }))
    }
  })

  it('A35 recovery / 35-X02 / SAME-CALL', async () => {
    const calls = ['allow', 'deny', 'ask'].map(mode => ({ id: `x02-${mode}`, name: 'sentinel', rawArgs: `{"mode":"${mode}"}` }))
    const adapter = new MockAdapter([rawMultiCall(calls), textResponse('x02-done')])
    const { ctx } = await createHarness(adapter, { approval: true })
    const stages: StageMap = new Map()
    const starts = new Map<string, number>()
    installStageObservers(ctx, stages)
    ctx.on('tools/pre-execute', async (exec, next): Promise<PreToolDecision> => {
      const id = String(exec.callId)
      if (id === 'x02-deny') { pushStage(stages, id, 'policy', 'decision', 'deny'); return { kind: 'deny', reason: 'denied by x02 policy' } }
      if (id === 'x02-ask') { pushStage(stages, id, 'policy', 'decision', 'ask'); return { kind: 'ask', reason: 'ask x02 approval' } }
      pushStage(stages, id, 'policy', 'delegate', 'allow')
      return next()
    })
    ctx.on('approval/request', () => Promise.resolve<ApprovalOutcome>('rejected'))
    ctx.tools.register(defineContentToolFixture({
      name: 'sentinel', description: 'in-memory sentinel', parameters: { mode: { type: 'string', required: true } },
      isConcurrencySafe: () => true,
      async execute(_args, exec) {
        const id = String(exec.callId)
        starts.set(id, (starts.get(id) ?? 0) + 1)
        return [{ type: 'text', text: `sentinel:${id}` }]
      },
    }))
    const agent = ctx.agentLoop.create(SessionId('a35-x02'), { provider: 'mock', model: 'mock' })
    const idle = waitForIdle(ctx, agent)
    agent.followup(createUserMessage({ content: [{ type: 'text', text: 'x02' }], source: { kind: 'user' } }))
    await idle

    expect(starts.get('x02-allow')).toBe(1)
    expect(starts.get('x02-deny') ?? 0).toBe(0)
    expect(starts.get('x02-ask') ?? 0).toBe(0)
    const asked = events(agent).filter((event): event is Extract<SessionEvent, { type: 'approval/asked' }> => event.type === 'approval/asked')
    const decided = events(agent).filter((event): event is Extract<SessionEvent, { type: 'approval/decided' }> => event.type === 'approval/decided')
    expect(asked).toHaveLength(1)
    expect(decided).toHaveLength(1)
    expect(asked[0]!.data.callId).toBe(ToolCallId('x02-ask'))
    expect(decided[0]!.data).toMatchObject({ id: asked[0]!.data.id, outcome: 'rejected' })

    for (const call of calls) {
      const result = resultFor(agent, call.id)
      const isAsk = call.id === 'x02-ask'
      emit(record('35-X02', call.id, call.rawArgs, stages.get(call.id) ?? [], {
        policy: { pre: call.id.slice(4), approvalAsked: isAsk ? 1 : 0, approvalDecided: isAsk ? 1 : 0, approvalOutcome: isAsk ? 'rejected' : null, timeoutMs: null, spillCapBytes: null },
        body: { startCount: starts.get(call.id) ?? 0, settleCount: starts.get(call.id) ?? 0, sentinelCount: starts.get(call.id) ?? 0, signalObserved: false, signalAborted: false, drained: true },
        normalizedResult: normalizeResult(result),
        session: sessionReceipt(agent, call.id),
        nextHistory: nextProjection(adapter, agent, call.id),
      }))
    }
  })

  it('A35 recovery / 35-X03 / SAME-CALL', async () => {
    vi.useFakeTimers()
    const calls = [
      { id: 'x03-timeout', name: 'slow-timeout', rawArgs: '{}' },
      { id: 'x03-control', name: 'slow-control', rawArgs: '{}' },
    ]
    const adapter = new MockAdapter([rawMultiCall(calls), textResponse('x03-done')])
    const { ctx } = await createHarness(adapter, { timeout: true, maxParallelToolCalls: 2 })
    const stages: StageMap = new Map()
    const starts = new Map<string, number>()
    const settles = new Map<string, number>()
    const signalObserved = new Map<string, boolean>()
    const release = Promise.withResolvers<void>()
    const sawAbort = Promise.withResolvers<void>()
    installStageObservers(ctx, stages)
    ctx.on('session/event', (session, event) => {
      if (event.type === 'tool/result') pushStage(stages, String(event.data.message.source.callId), 'session.result', 'append', String(event.seq))
    })
    ctx.tools.register(defineContentToolFixture({
      name: 'slow-timeout', description: 'cooperative timeout', parameters: {}, timeoutMs: 100,
      isConcurrencySafe: () => true,
      async execute(_args, exec) {
        const id = String(exec.callId)
        starts.set(id, 1); pushStage(stages, id, 'body.start', 'observed')
        if (!exec.signal.aborted) await new Promise<void>(resolve => exec.signal.addEventListener('abort', () => resolve(), { once: true }))
        signalObserved.set(id, true); pushStage(stages, id, 'signal.abort', 'observed'); sawAbort.resolve()
        await release.promise; pushStage(stages, id, 'body.cleanup-release', 'observed')
        settles.set(id, 1); pushStage(stages, id, 'body.settle', 'observed')
        return [{ type: 'text', text: 'timeout cleanup complete' }]
      },
    }))
    ctx.tools.register(defineContentToolFixture({
      name: 'slow-control', description: 'long budget control', parameters: {}, timeoutMs: 10_000,
      isConcurrencySafe: () => true,
      async execute(_args, exec) {
        const id = String(exec.callId)
        starts.set(id, 1); pushStage(stages, id, 'body.start', 'observed')
        signalObserved.set(id, exec.signal.aborted)
        settles.set(id, 1); pushStage(stages, id, 'body.settle', 'observed')
        return [{ type: 'text', text: 'control success' }]
      },
    }))
    const agent = ctx.agentLoop.create(SessionId('a35-x03'), { provider: 'mock', model: 'mock' })
    const idle = waitForIdle(ctx, agent)
    agent.followup(createUserMessage({ content: [{ type: 'text', text: 'x03' }], source: { kind: 'user' } }))
    await poll(() => starts.get('x03-timeout') === 1 && starts.get('x03-control') === 1)
    await vi.advanceTimersByTimeAsync(100)
    await sawAbort.promise
    expect(events(agent).filter(event => event.type === 'tool/result' && String(event.data.message.source.callId) === 'x03-timeout')).toHaveLength(0)
    release.resolve()
    await idle

    expect(resultFor(agent, 'x03-timeout').data.error?.code).toBe(TOOL_TIMEOUT)
    expect(resultFor(agent, 'x03-control').data.error).toBeUndefined()
    expect(signalObserved.get('x03-timeout')).toBe(true)
    expect(signalObserved.get('x03-control')).toBe(false)
    for (const call of calls) {
      const result = resultFor(agent, call.id)
      emit(record('35-X03', call.id, call.rawArgs, stages.get(call.id) ?? [], {
        policy: { pre: 'allow', approvalAsked: 0, approvalDecided: 0, approvalOutcome: null, timeoutMs: call.id === 'x03-timeout' ? 100 : 10_000, spillCapBytes: null },
        body: { startCount: starts.get(call.id) ?? 0, settleCount: settles.get(call.id) ?? 0, sentinelCount: 0, signalObserved: signalObserved.get(call.id) ?? false, signalAborted: signalObserved.get(call.id) ?? false, drained: (settles.get(call.id) ?? 0) === 1 },
        normalizedResult: normalizeResult(result),
        session: sessionReceipt(agent, call.id),
        nextHistory: nextProjection(adapter, agent, call.id),
      }))
    }
  })

  it('A35 recovery / 35-X04 / SAME-CALL', async () => {
    const calls = [
      { id: 'x04-started', name: 'cancel-latch', rawArgs: '{"slot":"started"}' },
      { id: 'x04-held', name: 'cancel-latch', rawArgs: '{"slot":"held"}' },
    ]
    const adapter = new MockAdapter([rawMultiCall(calls), textResponse('x04-followup')])
    const { ctx } = await createHarness(adapter, { maxParallelToolCalls: 1 })
    const stages: StageMap = new Map()
    const starts = new Map<string, number>()
    const settles = new Map<string, number>()
    const signalObserved = new Map<string, boolean>()
    const started = Promise.withResolvers<void>()
    const sawAbort = Promise.withResolvers<void>()
    const release = Promise.withResolvers<void>()
    installStageObservers(ctx, stages)
    ctx.on('session/event', (_session, event) => {
      if (event.type === 'tool/result') pushStage(stages, String(event.data.message.source.callId), 'session.result', 'append', String(event.seq))
    })
    ctx.tools.register(defineContentToolFixture({
      name: 'cancel-latch', description: 'cooperative cancel latch', parameters: { slot: { type: 'string', required: true } },
      isConcurrencySafe: () => true,
      async execute(_args, exec) {
        const id = String(exec.callId)
        starts.set(id, (starts.get(id) ?? 0) + 1); pushStage(stages, id, 'body.start', 'observed'); started.resolve()
        if (!exec.signal.aborted) await new Promise<void>(resolve => exec.signal.addEventListener('abort', () => resolve(), { once: true }))
        signalObserved.set(id, true); pushStage(stages, id, 'signal.abort', 'observed'); sawAbort.resolve()
        await release.promise; pushStage(stages, id, 'body.cleanup-release', 'observed')
        settles.set(id, 1); pushStage(stages, id, 'body.settle', 'observed')
        return [{ type: 'text', text: 'started work drained' }]
      },
    }))
    const agent = ctx.agentLoop.create(SessionId('a35-x04'), { provider: 'mock', model: 'mock' })
    const firstIdle = waitForIdle(ctx, agent)
    agent.followup(createUserMessage({ content: [{ type: 'text', text: 'x04' }], source: { kind: 'user' } }))
    await started.promise
    agent.cancel({ kind: 'user' })
    await sawAbort.promise
    expect(starts.get('x04-started')).toBe(1)
    expect(starts.get('x04-held') ?? 0).toBe(0)
    expect(events(agent).filter(event => event.type === 'tool/result')).toHaveLength(0)
    release.resolve()
    await firstIdle
    expect(resultFor(agent, 'x04-started').data.error?.code).toBe(TOOL_ABORTED)
    expect(resultFor(agent, 'x04-held').data.error?.code).toBe(TOOL_ABORTED_BEFORE_DISPATCH)

    const secondIdle = waitForIdle(ctx, agent)
    agent.followup(createUserMessage({ content: [{ type: 'text', text: 'x04-followup' }], source: { kind: 'user' } }))
    await secondIdle
    expect(adapter.requests).toHaveLength(2)
    expect(events(agent).findLast(event => event.type === 'turn/end')?.data).toMatchObject({ reason: { kind: 'completed' } })

    for (const call of calls) {
      const result = resultFor(agent, call.id)
      const next = nextProjection(adapter, agent, call.id) as Record<string, unknown>
      next.followupCompleted = true
      emit(record('35-X04', call.id, call.rawArgs, stages.get(call.id) ?? [], {
        policy: { pre: call.id === 'x04-started' ? 'allow' : null, approvalAsked: 0, approvalDecided: 0, approvalOutcome: null, timeoutMs: null, spillCapBytes: null },
        body: { startCount: starts.get(call.id) ?? 0, settleCount: settles.get(call.id) ?? 0, sentinelCount: starts.get(call.id) ?? 0, signalObserved: signalObserved.get(call.id) ?? false, signalAborted: signalObserved.get(call.id) ?? false, drained: call.id === 'x04-started' ? settles.get(call.id) === 1 : true },
        normalizedResult: normalizeResult(result),
        session: sessionReceipt(agent, call.id),
        nextHistory: next,
      }))
    }
  })

  it('A35 recovery / 35-X05 / SAME-CALL', async () => {
    const small = 'tiny'
    const large = 'HEAD'.repeat(200) + 'TAIL'.repeat(200)
    const fallback = 'x'.repeat(1_000)
    const calls = [
      { id: 'x05-small', name: 'small', rawArgs: '{}' },
      { id: 'x05-spill', name: 'big-ok', rawArgs: '{}' },
      { id: 'x05-fallback', name: 'big-fail', rawArgs: '{}' },
    ]
    const adapter = new MockAdapter([rawMultiCall(calls), textResponse('x05-done')])
    const { ctx, spill } = await createHarness(adapter, { spill: true })
    const stages: StageMap = new Map()
    const starts = new Map<string, number>()
    installStageObservers(ctx, stages)
    for (const [name, payload] of [['small', small], ['big-ok', large], ['big-fail', fallback]] as const) {
      ctx.tools.register(defineContentToolFixture({
        name, description: name, parameters: {},
        async execute(_args, exec) {
          const id = String(exec.callId); starts.set(id, 1)
          return [{ type: 'text', text: payload }]
        },
      }))
    }
    const agent = ctx.agentLoop.create(SessionId('a35-x05'), { provider: 'mock', model: 'mock' })
    const idle = waitForIdle(ctx, agent)
    agent.followup(createUserMessage({ content: [{ type: 'text', text: 'x05' }], source: { kind: 'user' } }))
    await idle
    expect(spill).not.toBeNull()
    expect(spill!.attempts).toHaveLength(2)
    expect(spill!.saves).toHaveLength(1)
    expect(spill!.saves[0]!.content).toBe(large)
    expect(String(SpillLocator('/spill/big-ok.txt'))).toBe('/spill/big-ok.txt')

    const payloads = new Map([['x05-small', small], ['x05-spill', large], ['x05-fallback', fallback]])
    for (const call of calls) {
      const payload = payloads.get(call.id)!
      const result = resultFor(agent, call.id)
      const normalized = normalizeResult(result)
      const projected = normalized.contentText as string
      const attempt = spill!.attempts.find(item => item.toolName === call.name)
      const save = spill!.saves.find(item => item.source.toolName === call.name)
      if (call.id === 'x05-small') {
        expect(projected).toBe(small); expect(attempt).toBeUndefined()
      } else if (call.id === 'x05-spill') {
        expect(utf8Bytes(projected)).toBeLessThanOrEqual(200)
        expect(sha256(projected)).not.toBe(sha256(large))
        expect(projected).toContain('/spill/big-ok.txt')
        expect(attempt?.hash).toBe(sha256(large)); expect(save?.content).toBe(large)
      } else {
        expect(attempt?.storageError).toBe('injected storage failure')
        expect(save).toBeUndefined(); expect(projected).toBe(fallback)
      }
      const next = nextProjection(adapter, agent, call.id)
      expect(next.contentHash).toBe(sha256(projected))
      emit(record('35-X05', call.id, call.rawArgs, stages.get(call.id) ?? [], {
        policy: { pre: 'allow', approvalAsked: 0, approvalDecided: 0, approvalOutcome: null, timeoutMs: null, spillCapBytes: 200 },
        body: { startCount: starts.get(call.id) ?? 0, settleCount: starts.get(call.id) ?? 0, sentinelCount: 0, signalObserved: false, signalAborted: false, drained: true },
        normalizedResult: normalized,
        session: sessionReceipt(agent, call.id),
        nextHistory: next,
        spill: {
          inputBytes: utf8Bytes(payload), fullHash: sha256(payload), attemptCount: attempt ? 1 : 0, saveCount: save ? 1 : 0,
          storedHash: save ? sha256(save.content) : null,
          locator: save ? `/spill/${save.suggestedName}` : null,
          previewBytes: call.id === 'x05-spill' ? utf8Bytes(projected) : 0,
          previewHash: call.id === 'x05-spill' ? sha256(projected) : null,
          fallbackBytes: call.id === 'x05-fallback' ? utf8Bytes(projected) : 0,
          fallbackHash: call.id === 'x05-fallback' ? sha256(projected) : null,
          storageError: attempt?.storageError ?? null, semanticSummary: false,
        },
      }))
    }
  })
