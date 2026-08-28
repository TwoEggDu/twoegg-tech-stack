# TDD RED Process Record

| Order | Exact command | Exit | stdout | stderr |
|---:|---|---:|---|---|
| 1 | `dotnet restore --locked-mode` | `0` | `restore.stdout.txt` | empty (`0` bytes) |
| 2 | `dotnet build -c Release --no-restore` | `0` | `build.stdout.txt` | empty (`0` bytes) |
| 3 | `dotnet run --project tests/TraceEvalLab.Specs -c Release --no-build -- --phase red` | `1` | `specs.stdout.txt` | empty (`0` bytes) |

RED validity: the Runtime was a compilable shell whose only behavior was stderr `NOT_IMPLEMENTED...` and native exit `64`. All five mandatory behavior assertions failed for the missing implementation after a successful Release build; no compile error caused RED.
