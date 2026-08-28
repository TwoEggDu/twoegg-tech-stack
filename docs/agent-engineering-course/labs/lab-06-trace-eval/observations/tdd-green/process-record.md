# TDD GREEN Process Record

| Order | Exact command | Exit | stdout | stderr |
|---:|---|---:|---|---|
| 1 | `dotnet build -c Release --no-restore` | `0` | `build.stdout.txt` | empty (`0` bytes) |
| 2 | `dotnet run --project tests/TraceEvalLab.Specs -c Release --no-build -- --phase green` | `0` | `specs.stdout.txt` | empty (`0` bytes) |

The same five frozen behavioral assertions used for RED were used for GREEN. The Spec project has no Runtime project reference and observes only the public CLI plus `result.json`.
