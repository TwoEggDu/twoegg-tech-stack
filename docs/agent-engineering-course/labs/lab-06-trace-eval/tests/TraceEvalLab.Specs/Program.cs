using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TraceEvalLab.Specs;

internal static class Program
{
    private sealed record ProcessResult(int ExitCode, string Stdout, string Stderr, JsonDocument? Result);

    private static int Main(string[] args)
    {
        if (args.Length == 2 && args[0] == "--verify")
        {
            return VerifyExistingResults(Path.GetFullPath(args[1]));
        }

        var phase = ReadOption(args, "--phase") ?? "unspecified";
        var labRoot = FindLabRoot();
        var outputRoot = Path.Combine(labRoot, "observations", "spec-temp", phase);
        Directory.CreateDirectory(outputRoot);

        var baseline = Path.Combine(labRoot, "fixtures", "candidates", "baseline.json");
        var regression = Path.Combine(labRoot, "fixtures", "candidates", "known-regression.json");
        var missing = CreateCandidateVariant(regression, Path.Combine(outputRoot, "missing-n06.json"), node =>
        {
            var cases = node["cases"]!.AsArray();
            var target = cases.Single(item => item!["case_id"]!.GetValue<string>() == "N06");
            cases.Remove(target);
        });
        var mismatch = CreateCandidateVariant(regression, Path.Combine(outputRoot, "scorer-v2.json"), node =>
        {
            node["scorer_version"] = "v2";
        });

        var specs = new (string Name, Action Body)[]
        {
            ("baseline is 8/8 and passes both gates", () => VerifyBaseline(Evaluate(labRoot, baseline, null, Path.Combine(outputRoot, "baseline")))),
            ("known regression keeps aggregate threshold but fails critical gate and marks C01", () => VerifyKnownRegression(Evaluate(labRoot, regression, baseline, Path.Combine(outputRoot, "known-regression")))),
            ("missing N06 is UNKNOWN and fails closed", () => VerifyMissing(Evaluate(labRoot, missing, baseline, Path.Combine(outputRoot, "missing")))),
            ("scorer manifest mismatch is INCOMPARABLE and fails closed", () => VerifyMismatch(Evaluate(labRoot, mismatch, baseline, Path.Combine(outputRoot, "mismatch")))),
            ("normalized artifacts are byte-identical across repeated runs", () => VerifyRepeatability(labRoot, regression, baseline, outputRoot))
        };

        var failures = 0;
        Console.WriteLine($"PHASE={phase}");
        foreach (var spec in specs)
        {
            try
            {
                spec.Body();
                Console.WriteLine($"PASS {spec.Name}");
            }
            catch (Exception ex)
            {
                failures++;
                Console.WriteLine($"FAIL {spec.Name}");
                Console.WriteLine($"  {ex.Message}");
            }
        }

        Console.WriteLine($"SUMMARY total={specs.Length} passed={specs.Length - failures} failed={failures}");
        return failures == 0 ? 0 : 1;
    }

    private static int VerifyExistingResults(string resultRoot)
    {
        try
        {
            VerifyBaseline(ReadExisting(Path.Combine(resultRoot, "baseline", "result.json"), 0));
            VerifyKnownRegression(ReadExisting(Path.Combine(resultRoot, "known-regression", "result.json"), 2));
            Console.WriteLine("PASS verify baseline formal result");
            Console.WriteLine("PASS verify known-regression formal result");
            Console.WriteLine("SUMMARY total=2 passed=2 failed=0");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FAIL formal result verification: {ex.Message}");
            Console.WriteLine("SUMMARY total=2 passed=0 failed=2");
            return 1;
        }
    }

    private static void VerifyBaseline(ProcessResult run)
    {
        Require(run.ExitCode == 0, $"expected exit 0, got {run.ExitCode}; stderr={run.Stderr.Trim()}");
        var root = RequireResult(run);
        Require(root.GetProperty("passed_case_count").GetInt32() == 8, "expected passed_case_count=8");
        Require(root.GetProperty("total_case_count").GetInt32() == 8, "expected total_case_count=8");
        Require(root.GetProperty("aggregate_accuracy").GetDecimal() == 1.0m, "expected aggregate_accuracy=1.0");
        Require(root.GetProperty("critical_accuracy").GetDecimal() == 1.0m, "expected critical_accuracy=1.0");
        Require(root.GetProperty("overall_gate").GetString() == "PASS", "expected overall_gate=PASS");
    }

    private static void VerifyKnownRegression(ProcessResult run)
    {
        Require(run.ExitCode == 2, $"expected fail-closed exit 2, got {run.ExitCode}; stderr={run.Stderr.Trim()}");
        var root = RequireResult(run);
        Require(root.GetProperty("passed_case_count").GetInt32() == 7, "expected passed_case_count=7");
        Require(root.GetProperty("aggregate_accuracy").GetDecimal() == 0.875m, "expected aggregate_accuracy=0.875");
        Require(root.GetProperty("aggregate_threshold_pass").GetBoolean(), "expected aggregate_threshold_pass=true");
        Require(root.GetProperty("critical_accuracy").GetDecimal() == 0.5m, "expected critical_accuracy=0.5");
        Require(!root.GetProperty("critical_gate_pass").GetBoolean(), "expected critical_gate_pass=false");
        Require(root.GetProperty("overall_gate").GetString() == "FAIL", "expected overall_gate=FAIL");
        var cases = root.GetProperty("cases").EnumerateArray().ToDictionary(item => item.GetProperty("case_id").GetString()!);
        Require(cases["C01"].GetProperty("change_verdict").GetString() == "REGRESSION", "expected C01=REGRESSION");
        Require(cases.Where(pair => pair.Key != "C01").All(pair => pair.Value.GetProperty("change_verdict").GetString() == "UNCHANGED"), "expected seven UNCHANGED verdicts");
        Require(!cases.Values.Any(item => item.GetProperty("change_verdict").GetString() == "IMPROVEMENT"), "unexpected IMPROVEMENT verdict");
    }

    private static void VerifyMissing(ProcessResult run)
    {
        Require(run.ExitCode == 2, $"expected fail-closed exit 2, got {run.ExitCode}; stderr={run.Stderr.Trim()}");
        var root = RequireResult(run);
        Require(root.GetProperty("run_verdict").GetString() == "UNKNOWN", "expected run_verdict=UNKNOWN");
        Require(root.GetProperty("missing_case_count").GetInt32() == 1, "expected missing_case_count=1");
        Require(root.GetProperty("unknown_case_count").GetInt32() == 1, "expected unknown_case_count=1");
        Require(root.GetProperty("overall_gate").GetString() == "FAIL", "expected overall_gate=FAIL");
        var n06 = root.GetProperty("cases").EnumerateArray().Single(item => item.GetProperty("case_id").GetString() == "N06");
        Require(n06.GetProperty("change_verdict").GetString() == "UNKNOWN", "expected N06=UNKNOWN");
    }

    private static void VerifyMismatch(ProcessResult run)
    {
        Require(run.ExitCode == 3, $"expected incomparable exit 3, got {run.ExitCode}; stderr={run.Stderr.Trim()}");
        var root = RequireResult(run);
        Require(root.GetProperty("run_verdict").GetString() == "INCOMPARABLE", "expected run_verdict=INCOMPARABLE");
        Require(!root.GetProperty("manifest_comparable").GetBoolean(), "expected manifest_comparable=false");
        Require(root.GetProperty("overall_gate").GetString() == "FAIL", "expected overall_gate=FAIL");
        Require(!root.TryGetProperty("aggregate_accuracy", out _), "incomparable run must not publish aggregate_accuracy");
    }

    private static void VerifyRepeatability(string labRoot, string candidate, string baseline, string outputRoot)
    {
        var runA = Evaluate(labRoot, candidate, baseline, Path.Combine(outputRoot, "repeat-a"));
        var runB = Evaluate(labRoot, candidate, baseline, Path.Combine(outputRoot, "repeat-b"));
        Require(runA.Result is not null && runB.Result is not null, "both normalized artifacts must exist");
        var bytesA = File.ReadAllBytes(Path.Combine(outputRoot, "repeat-a", "result.json"));
        var bytesB = File.ReadAllBytes(Path.Combine(outputRoot, "repeat-b", "result.json"));
        Require(bytesA.SequenceEqual(bytesB), "normalized result bytes differ across run A/B");
    }

    private static ProcessResult Evaluate(string labRoot, string candidate, string? baseline, string output)
    {
        Directory.CreateDirectory(output);
        var runtimeDll = Path.Combine(labRoot, "src", "TraceEvalLab", "bin", "Release", "net10.0", "TraceEvalLab.dll");
        var arguments = new List<string>
        {
            runtimeDll,
            "evaluate",
            "--corpus", Path.Combine(labRoot, "fixtures", "golden-corpus.json"),
            "--policy", Path.Combine(labRoot, "fixtures", "scorer-policy.json"),
            "--candidate", candidate,
            "--output", output
        };
        if (baseline is not null)
        {
            arguments.Add("--baseline");
            arguments.Add(baseline);
        }

        var start = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = labRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        using var process = Process.Start(start) ?? throw new InvalidOperationException("failed to start runtime");
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();
        var resultPath = Path.Combine(output, "result.json");
        var result = File.Exists(resultPath) ? JsonDocument.Parse(File.ReadAllBytes(resultPath)) : null;
        return new ProcessResult(process.ExitCode, stdout, stderr, result);
    }

    private static ProcessResult ReadExisting(string path, int expectedExitCode)
    {
        Require(File.Exists(path), $"formal result missing: {path}");
        return new ProcessResult(expectedExitCode, "", "", JsonDocument.Parse(File.ReadAllBytes(path)));
    }

    private static string CreateCandidateVariant(string source, string target, Action<JsonObject> mutation)
    {
        var node = JsonNode.Parse(File.ReadAllText(source))!.AsObject();
        mutation(node);
        File.WriteAllText(target, node.ToJsonString(new JsonSerializerOptions { WriteIndented = true }).Replace("\r\n", "\n") + "\n");
        return target;
    }

    private static JsonElement RequireResult(ProcessResult run)
    {
        Require(run.Result is not null, $"result.json missing; stdout={run.Stdout.Trim()}; stderr={run.Stderr.Trim()}");
        return run.Result!.RootElement;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static string? ReadOption(string[] args, string name)
    {
        var index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }

    private static string FindLabRoot()
    {
        var current = new DirectoryInfo(Environment.CurrentDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "fixtures", "golden-corpus.json")))
            {
                return current.FullName;
            }
            current = current.Parent;
        }
        throw new DirectoryNotFoundException("lab root containing fixtures/golden-corpus.json was not found");
    }
}
