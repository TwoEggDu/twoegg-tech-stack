# Verification Command Notes

- Formal verifier command: `dotnet run --project tests/TraceEvalLab.Specs -c Release --no-build -- --verify observations/run-a`; native exit `0`; stdout in `formal-specs.stdout.txt`; stderr empty (`0` bytes).
- Byte verifier: PowerShell static `System.Linq.Enumerable.SequenceEqual<byte>` over Run A/B `result.json`; command exit `0`; stdout in `repeatability.stdout.txt`; stderr empty (`0` bytes).
- A first ad-hoc PowerShell verification expression incorrectly called `SequenceEqual` as an instance method and emitted `InvalidOperation`; it did not alter any result. The corrected static call above passed. This tooling mistake is retained here rather than hidden.
- A bare non-zero native process appeared as shell command exit `1` in one outer runner response. Explicit PowerShell `$LASTEXITCODE` checks proved evaluator/native exits `2` for fail-closed regression/unknown and `3` for incomparable. No Runtime behavior changed.
