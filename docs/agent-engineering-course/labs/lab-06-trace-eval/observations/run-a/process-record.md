# Formal Run A

| Input | Exact Runtime command | Native exit | stdout | stderr | Result |
|---|---|---:|---|---|---|
| baseline | `dotnet src/TraceEvalLab/bin/Release/net10.0/TraceEvalLab.dll evaluate --corpus fixtures/golden-corpus.json --policy fixtures/scorer-policy.json --candidate fixtures/candidates/baseline.json --output observations/run-a/baseline` | `0` | `RESULT candidate=baseline verdict=PASS overall=PASS` | empty (`0` bytes) | `baseline/result.json` |
| known-regression | `dotnet src/TraceEvalLab/bin/Release/net10.0/TraceEvalLab.dll evaluate --corpus fixtures/golden-corpus.json --policy fixtures/scorer-policy.json --candidate fixtures/candidates/known-regression.json --baseline fixtures/candidates/baseline.json --output observations/run-a/known-regression` | `2` | `RESULT candidate=known-regression verdict=REGRESSION overall=FAIL` | empty (`0` bytes) | `known-regression/result.json` |
