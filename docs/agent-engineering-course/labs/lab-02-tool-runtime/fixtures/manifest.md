# Lab 02 Fixture Manifest｜Design Only

- Status：`DESIGN_FROZEN / NO_FIXTURE_CREATED`
- Owner：`Article 06 Researcher`
- Frozen At：`2026-08-20（Asia/Shanghai）`
- Runtime：`net10.0 / .NET SDK 10.0.301 candidate`
- External Packages：`NONE`
- Provider / Network / Credentials：`NONE / NONE / NONE`
- Observation / Raw Trace：`NONE / NONE`

> 本文件只冻结 future fixture topology、exact bytes、case inputs与 expected mapping。当前目录没有 `cases.json`、temp fixture、link / junction、source、tests、spill或 JSONL output。

## Future run-root topology

每次 run使用新的、唯一的 temp root：

```text
%TEMP%/agent-engineering-lab-02-<guid>/
├─ .lab-02-owned
├─ allowed/
│  ├─ small.txt
│  ├─ large.txt
│  └─ link-out/              # junction first; symlink fallback
├─ outside/
│  └─ secret.txt
└─ spills/
```

- ReadOnlyFileTool allow-root只指向 `allowed/`。
- `link-out` 的 final target必须是同一 run-root内、allow-root外的 `outside/`。
- `outside/` 仍是 Lab-owned temp，但 Tool policy必须视为越界。
- unique absolute temp path只写 execution log；JSONL使用 logical placeholder / relative ref，不写 absolute path。
- cleanup只允许在四个条件同时成立时删除：fully-qualified path位于 `[IO.Path]::GetTempPath()` 下、basename以 `agent-engineering-lab-02-` 开头、sentinel `.lab-02-owned` 存在、目标不是 temp parent本身。

## Exact file bytes

所有 input files均为 UTF-8 without BOM、LF line endings。

| File | Exact content rule | Byte Count | SHA-256 |
|---|---|---:|---|
| `allowed/small.txt` | exact text `alpha\nbeta\n` | 11 | `E49C81E2D2F84E259D40E2FB8192F3BCD198B355184845D76D8F58807D0D78EE` |
| `allowed/large.txt` | exactly 1024 ASCII `L` bytes, no newline | 1024 | `26AD8132E3B544CAEFD85B30BF36DF8D012DC7245C9D2224E0F9F50A2AC55A61` |
| `outside/secret.txt` | exact text `outside-secret\n` | 15 | `A532F53598B8BB67609FD55670AA58B9A1DD5F3F77E9C4FA44321533C85BAF6B` |

Lab Engineer的 setup script必须按 bytes创建并立刻验证 size + SHA-256；mismatch时停止，不运行 cases。

## Link / junction setup decision

1. 优先尝试 PowerShell `New-Item -ItemType Junction`，`Path=allowed/link-out`、`Target=outside`。
2. 若 junction command失败或返回对象不是可由 `.NET Directory.ResolveLinkTarget(..., true)` 解析的 link，则保存 raw error，移除仅本次失败创建的 empty path，再尝试 `New-Item -ItemType SymbolicLink`。
3. SymbolicLink可能受 Developer Mode、elevation或 host policy限制；不得静默提权。
4. 只有一种 link实际创建、final target确认为 `outside/` 且目标文件存在，TR-04才能运行。
5. 两种方式都失败：保存两次 setup failure，Lab=`FAILED_LAB / 06-C05 remains BLOCKED`；不得生成 synthetic `link_kind` 或跳过 TR-04后仍 PASS。

Windows feasibility本轮只由 .NET API文档和 planned PowerShell surface支撑；当前没有创建 link，也没有验证本机权限。

## Future machine-readable case input

Future `fixtures/cases.json` 必须机械表达下表，不得增删 required case或改变字段语义。raw argument object在 hash前按固定 property order和 InvariantCulture canonicalize；hash algorithm=`SHA-256`。

| Group | Attempt | Invocation ID | Tool | Exact arguments | Policy `global/tool/resource` | Fault |
|---|---:|---|---|---|---|---|
| `TR-01` | 1 | `inv-calc-valid` | `calculate_binary` | `{"operation":"add","left":2,"right":3}` | `ALLOW/ALLOW/ALLOW` | `NONE` |
| `TR-02` | 1 | `inv-read-valid` | `read_text` | `{"relative_path":"small.txt"}` | `ALLOW/ALLOW/ALLOW` | `NONE` |
| `TR-03` | 1 | `inv-read-traversal` | `read_text` | `{"relative_path":"../outside/secret.txt"}` | `ALLOW/ALLOW/ALLOW` | `NONE` |
| `TR-04` | 1 | `inv-read-link` | `read_text` | `{"relative_path":"link-out/secret.txt"}` | `ALLOW/ALLOW/ALLOW` | `REAL_LINK_OUTSIDE` |
| `TR-05` | 1 | `inv-policy-deny` | `read_text` | `{"relative_path":"small.txt"}` | `ALLOW/ASK/DENY` | `NONE` |
| `TR-06` | 1 | `inv-policy-ask` | `read_text` | `{"relative_path":"small.txt"}` | `ALLOW/ASK/ALLOW` | `NONE` |
| `TR-07` | 1 | `inv-timeout` | `read_text` | `{"relative_path":"small.txt"}` | `ALLOW/ALLOW/ALLOW` | `NEVER_RELEASE_GATE / TIMEOUT_MS=50` |
| `TR-08` | 1 | `inv-caller-cancel` | `read_text` | `{"relative_path":"small.txt"}` | `ALLOW/ALLOW/ALLOW` | `CALLER_PRE_CANCELLED / TIMEOUT_MS=5000` |
| `TR-09` | 1 | `inv-invalid-result` | `calculate_binary` | `{"operation":"add","left":2,"right":3}` | `ALLOW/ALLOW/ALLOW` | `INVALID_RESULT_KIND=file_text` |
| `TR-10` | 1 | `inv-large-read` | `read_text` | `{"relative_path":"large.txt"}` | `ALLOW/ALLOW/ALLOW` | `INLINE_THRESHOLD=64` |
| `TR-11` | 1 | `inv-replay` | `calculate_binary` | `{"operation":"add","left":2,"right":3}` | `ALLOW/ALLOW/ALLOW` | `NONE` |
| `TR-11` | 2 | `inv-replay` | `calculate_binary` | `{"operation":"add","left":2,"right":3}` | `ALLOW/ALLOW/ALLOW` | `DUPLICATE_SAME_ARGS` |
| `TR-12` | 1 | `inv-conflict` | `calculate_binary` | `{"operation":"add","left":2,"right":3}` | `ALLOW/ALLOW/ALLOW` | `NONE` |
| `TR-12` | 2 | `inv-conflict` | `calculate_binary` | `{"operation":"subtract","left":2,"right":3}` | `ALLOW/ALLOW/ALLOW` | `DUPLICATE_DIFFERENT_ARGS` |

## Frozen Expected mapping

| Group / Attempt | Terminal Stage | Terminal Code | Cancellation Origin | Render | Result / Special Check |
|---|---|---|---|---|---|
| `TR-01/1` | `SUCCEEDED` | `OK` | `NONE` | `INLINE` | calculation value=`5` |
| `TR-02/1` | `SUCCEEDED` | `OK` | `NONE` | `INLINE` | 11 bytes + small SHA-256 |
| `TR-03/1` | `CANONICALIZE` | `PATH_OUTSIDE_ROOT` | `NONE` | `NOT_RUN` | handler count 0 |
| `TR-04/1` | `CANONICALIZE` | `PATH_LINK_OUTSIDE_ROOT` | `NONE` | `NOT_RUN` | actual link final target outside；handler count 0 |
| `TR-05/1` | `POLICY` | `POLICY_DENIED` | `NONE` | `NOT_RUN` | final policy DENY；handler count 0 |
| `TR-06/1` | `POLICY` | `APPROVAL_REQUIRED` | `NONE` | `NOT_RUN` | final policy ASK；handler count 0 |
| `TR-07/1` | `EXECUTE` | `TIMED_OUT` | `TIMEOUT` | `NOT_RUN` | handler gate entered；no file read/result |
| `TR-08/1` | `EXECUTE` | `CALLER_CANCELLED` | `CALLER` | `NOT_RUN` | precheck cancels before handler；count 0 |
| `TR-09/1` | `RESULT_VALIDATION` | `RESULT_SCHEMA_INVALID` | `NONE` | `NOT_RUN` | handler count 1；cache not written |
| `TR-10/1` | `SUCCEEDED` | `OK` | `NONE` | `SPILLED` | 1024 bytes + large SHA-256；relative spill ref |
| `TR-11/1` | `SUCCEEDED` | `OK` | `NONE` | `INLINE` | handler count 1 |
| `TR-11/2` | `IDEMPOTENCY` | `REPLAYED` | `NONE` | `INLINE` | handler count remains 1；same result digest |
| `TR-12/1` | `SUCCEEDED` | `OK` | `NONE` | `INLINE` | handler count 1 |
| `TR-12/2` | `IDEMPOTENCY` | `IDEMPOTENCY_CONFLICT` | `NONE` | `NOT_RUN` | handler count remains 1；no result |

## Determinism controls

- case order固定为上表顺序。
- JSON property order固定；numbers用 InvariantCulture；不输出 elapsed time、thread ID、exception message、absolute path或 randomized ID。
- `run_id` 固定为 `lab-02-fixed-run`；execution time / temp root / link kind写独立 log。
- two runs都从 fresh process、fresh temp root、new JSONL artifact开始；same link strategy应在 setup phase稳定选定。
- JSONL UTF-8 no BOM、LF；恰14行。两份文件必须 byte-identical。
- spill ref固定为 `spills/<lowercase-sha256>.txt`；JSONL不记录 unique temp prefix。

## Safety and cleanup

- setup / cleanup只能操作 unique temp root；禁止使用 repository root、`$HOME`、`~`、drive root或 unresolved glob。
- cleanup script先解析 absolute path，再核对 temp parent、name prefix、sentinel与非-parent；任一失败就停止删除并报告。
- link target和spill都在同一 Lab-owned temp root；allow-root外不等于 Lab ownership外。
- artifacts复制回 Lab目录前不得 cleanup；cleanup失败不伪装成功，需报告 exact temp path供人工恢复。
- ReadOnlyFileTool没有 write method；spill writer属于 Host runtime internal renderer，只能写 `spills/`。

## Stop Line

Lab Engineer只能把本 manifest机械实现为 future `cases.json`、setup / cleanup、source、specs与 raw artifacts。任何需要改变 exact bytes、case order、link fallback、threshold、terminal code、trace schema或 cleanup guard 的问题都必须返回 Researcher；不得就地改 Design。
