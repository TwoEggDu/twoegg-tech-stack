# Article 35 Card｜Tool Registry 与 Tool Execution Pipeline

## Canonical identity

- Canonical source: `docs/agent-engineering-series-plan.md`
- Part: `VI｜DeepSeek Harness`
- Required: `YES`
- Course weight: `L`
- Canonical title: `Tool Registry 与 Tool Execution Pipeline`
- Article type: `SOURCE_TRACE / REQUIRED_SOURCE_EXPERIMENT`

## Problem Space

模型能看到一个 Tool schema，并不等于这个 Tool 已获授权、参数已按统一规则验证、body 已执行，或同一份结果已进入模型、UI 与 durable Session。本文要把 Registry 的可见性与 Tool call 的执行、归一化、持久化和投影拆成可审计的 owner chain。

## Required Questions

1. Tool discovery、scope、deduplication、Provider 与 model-facing schema 各由谁拥有？
2. raw arguments、canonical snapshot、typed value 与 Host metadata 怎样分账？
3. Allow / Deny / Ask 在固定 DSH 中是怎样组合的，为什么不是 vote merge？
4. pre/post hook、timeout、caller cancellation、concurrency 与 error 各在哪一层生效？
5. canonical result、model content、UI presentation、persisted result 与 additional context 怎样分离？
6. 大结果 spill、bounded preview、locator 与 storage-failure fallback 的真实行为是什么？
7. bad arguments、deny/ask、timeout、cancel、large result 五类负例能否在同一次调用内闭合到 Session 与 next-model projection？

## Dependencies and reader change

- Reuses Article 05/06 的 `Tool Call != Executed` 与 Tool Runtime 分层、Article 19 的 authority 分账、Article 33 的 scheduler seam、Article 34 的 Session / Projection 边界。
- After reading, the reader should be able to trace `Registry -> Model View` and `Call -> Parse/Canonicalize -> Policy/Validate -> Execute -> Normalize -> Persist -> Model/UI views`, identify the owner of each transition, and state what the five negative traces do and do not prove.
- Article 36 owns run-level Cost / Compaction / Trace / Cancellation / Recovery; Article 37 owns the final extension mapping and decision matrix.

## Non-goals

- No real Provider wire behavior, production Tool/service/deployment, external side effect, actual client UI render, hard kill, rollback, remote quiescence, billing stop, retention/access guarantee, or universal semantic summary claim.
- No claim that raw registration inherits typed `defineTool` validation.
- No BuildPilot ADR, Part VII Architecture, Design v1, Runtime, or Article 36/37 conclusion.

## Evidence and experiment contract

- Mode: `DSH_SOURCE_MODE / REQUIRED_SOURCE_EXPERIMENT`.
- Repository: `https://github.com/deepseek-ai/deepseek-harness`.
- Tag / commit: `dsh-v0.1.2-alpha.1` / `cd5ef8148158c3a752a658978873241fdf8e2bbc`.
- Source evidence: `repository-map.md` + `call-path.md`, with file, symbol, call path, counter-evidence, and limitation.
- Required experiment: `35-X01—X05` SAME-CALL traces for bad arguments, deny/ask, timeout, cancellation, and large result.
- Accepted receipt: Recovery Cycle 1 capture=`1 file / 5 tests / exit 0 / 13 records` with distribution `3/3/2/2/3`; fixture post-cleanup HEAD/status/staged/unstaged checks clean.
- Preserved failures: Cycle 0=`22 passed / 0 failed / NOT_ACCEPTED`; Recovery Attempt 1=`exit 0 / 0 of 5 selected / NOT_ACCEPTED`.
- Environment caveat: accepted experiment/Provider/tool-body network requests were zero, but an earlier wrong-cwd Corepack preflight attempted npm registry access and was blocked by `EACCES`.
- Final evidence disposition: `12 CLAIMS / 12 CARDS / 0 BLOCKED`; locked writing boundary is recorded in `evidence.md`.

## Teaching structure

`Problem Space -> Abstract two-chain/five-ledger model -> pinned DSH Registry/model view -> call execution owner path -> five negative traces -> result/spill boundaries -> bounded engineering transfer`.

## Current transaction boundary

- Review Cycle 1 closed F01/F02/F03/F05 and kept F04 open; Master Cycle 2 preserved invalid/missing history and established fresh current-time Research/Source Map/Outline/Author Draft authority.
- Review Recheck Cycle 2 closed `A35-R1-F04`; all five findings are closed and the current candidate Gate is `FINAL_GATE`.
- Article 35 remains the only active Article transaction. Article 36—37 are unstarted; Article 38—44 remain forbidden and zero-assets.
