# Formal Fault Injection Record

| Fault | Exact Runtime command | Native exit | stdout | stderr | Result |
|---|---|---:|---|---|---|
| FI-02 remove N06 | `dotnet src/TraceEvalLab/bin/Release/net10.0/TraceEvalLab.dll evaluate --corpus fixtures/golden-corpus.json --policy fixtures/scorer-policy.json --candidate observations/fault-injection/inputs/missing-n06.json --baseline fixtures/candidates/baseline.json --output observations/fault-injection/missing-n06` | `2` | `RESULT candidate=known-regression-missing-n06 verdict=UNKNOWN overall=FAIL` | empty (`0` bytes) | `missing-n06/result.json` |
| FI-03 scorer v2 | `dotnet src/TraceEvalLab/bin/Release/net10.0/TraceEvalLab.dll evaluate --corpus fixtures/golden-corpus.json --policy fixtures/scorer-policy.json --candidate observations/fault-injection/inputs/scorer-v2.json --baseline fixtures/candidates/baseline.json --output observations/fault-injection/scorer-v2` | `3` | `RESULT candidate=known-regression-scorer-v2 verdict=INCOMPARABLE overall=FAIL` | empty (`0` bytes) | `scorer-v2/result.json` |

Both injected candidates are Lab-owned copies under `observations/`; no frozen fixture was modified.
