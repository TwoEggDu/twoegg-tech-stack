# Article 34 Card｜Append-only Session Event：Replay、Resume、Fork 与 Projection

## Problem Space

同一条 Session event stream 必须同时支撑模型历史、UI transcript、domain state 与 trace，但这些 projection 的语义不同。若把 Replay、Resume、Fork 或 Compaction 混成“恢复聊天”，就无法说明哪些事实被继承、哪些外部副作用没有被复制。

## Required Questions

1. Durable Event 与 Live Event 的 owner 和边界是什么？
2. event type、sequence、Run/Turn/Step correlation 怎样表达？
3. Session event 的 write/read path 怎样闭合？
4. Model History、UI Transcript、Domain State、Trace 各怎样投影？
5. Replay、Resume、Fork 的真实差异是什么？
6. external world、permission、budget 哪些继承或不继承？
7. Compaction 是追加还是改写，Evidence/unverified 怎样保留？
8. 如何从 pinned event stream 重建 History 并 Fork 隔离分支？
