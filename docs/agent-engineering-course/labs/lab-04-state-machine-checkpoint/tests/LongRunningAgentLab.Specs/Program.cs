using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

internal static class Program
{
    private static readonly UTF8Encoding Utf8NoBom = new(false);
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static readonly string LabRoot = FindLabRoot();
    private static readonly string RuntimeDll = Path.Combine(LabRoot, "src", "LongRunningAgentLab", "bin", "Release", "net10.0", "LongRunningAgentLab.dll");
    private static readonly string CasesPath = Path.Combine(LabRoot, "fixtures", "cases.json");
    private static readonly Dictionary<string, CaseExpectation> Expected = new(StringComparer.Ordinal)
    {
        ["LR-01"] = new(0, null, "SUCCEEDED", "GOAL_SATISFIED", 1, 1, 0, false),
        ["LR-02"] = new(10, 0, "SUCCEEDED", "GOAL_SATISFIED", 1, 1, 0, true),
        ["LR-03"] = new(0, null, "SUCCEEDED", "GOAL_SATISFIED", 1, 2, 1, false),
        ["LR-04"] = new(11, 0, "SUCCEEDED", "GOAL_SATISFIED", 1, 2, 1, true),
        ["LR-05"] = new(11, 14, "DUPLICATE_SIDE_EFFECT_DETECTED", "FAILED", 2, 2, 1, true),
        ["LR-06"] = new(11, 12, "RECOVERY_REFUSED", "IN_FLIGHT_ACTION_MISSING", 1, 1, 0, true),
        ["LR-07"] = new(13, null, "RETRY_BUDGET_EXHAUSTED", "INCOMPLETE", 0, 2, 1, false),
        ["LR-08"] = new(0, null, "TIMED_OUT", "INCOMPLETE", 0, 0, 0, false),
    };

    private static int Main(string[] args)
    {
        try
        {
            if (args.Length == 0) return Fail("command is required");
            return args[0] switch
            {
                "static-contract" => StaticContract(),
                "formal-suite" => FormalSuite(args),
                "compare" => Compare(args),
                _ => Fail($"unknown command: {args[0]}")
            };
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"SPEC_FAIL {exception.GetType().Name}: {exception.Message}");
            return 1;
        }
    }

    private static int StaticContract()
    {
        Require(File.Exists(RuntimeDll), "Release Runtime binary is missing");
        Require(File.Exists(CasesPath), "fixtures/cases.json is missing");
        var nuget = File.ReadAllText(Path.Combine(LabRoot, "NuGet.Config"));
        Require(nuget.Contains("<clear />", StringComparison.Ordinal), "NuGet.Config does not clear package sources");
        Require(!nuget.Contains("http:", StringComparison.OrdinalIgnoreCase) && !nuget.Contains("https:", StringComparison.OrdinalIgnoreCase), "NuGet.Config contains a network source");

        var runtimeProject = File.ReadAllText(Path.Combine(LabRoot, "src", "LongRunningAgentLab", "LongRunningAgentLab.csproj"));
        Require(!runtimeProject.Contains("ProjectReference", StringComparison.Ordinal), "Runtime project references another project");
        Require(!runtimeProject.Contains("PackageReference", StringComparison.Ordinal), "Runtime project has a third-party package");
        var specProject = File.ReadAllText(Path.Combine(LabRoot, "tests", "LongRunningAgentLab.Specs", "LongRunningAgentLab.Specs.csproj"));
        Require(!specProject.Contains("ProjectReference", StringComparison.Ordinal), "Spec project must launch, not reference, Runtime");
        Require(!specProject.Contains("PackageReference", StringComparison.Ordinal), "Spec project has a third-party package");

        foreach (var source in Directory.GetFiles(Path.Combine(LabRoot, "src", "LongRunningAgentLab"), "*.cs", SearchOption.TopDirectoryOnly))
        {
            var text = File.ReadAllText(source);
            foreach (var forbidden in new[] { "README", "LongRunningAgentLab.Specs", "System.Net", "HttpClient", "Socket", "GetEnvironmentVariable", "expected_terminal", "expected_count", "expected_hash" })
                Require(!text.Contains(forbidden, StringComparison.OrdinalIgnoreCase), $"Runtime source contains forbidden token {forbidden}");
        }

        using (var stream = File.OpenRead(RuntimeDll))
        using (var pe = new PEReader(stream))
        {
            var metadata = pe.GetMetadataReader();
            var references = metadata.AssemblyReferences.Select(handle => metadata.GetString(metadata.GetAssemblyReference(handle).Name)).ToArray();
            Require(!references.Any(name => name.StartsWith("System.Net", StringComparison.Ordinal)), "Runtime assembly references System.Net");
            Require(!references.Any(name => name.Contains("Specs", StringComparison.Ordinal)), "Runtime assembly references Specs");
        }

        var fixture = ReadObject(CasesPath);
        var allowedTop = new HashSet<string>(new[] { "fixture_version", "case_id", "goal_contract_id", "run_id", "action_id", "idempotency_key", "scripted_action_inputs", "named_fault_id", "fault_boundary", "fault_cardinality", "retry_policy", "effect_mode", "cancellation_origin", "checkpoint_completeness" }, StringComparer.Ordinal);
        var allowedRoot = new HashSet<string>(new[] { "fixture_version", "cases" }, StringComparer.Ordinal);
        Require(fixture.Select(pair => pair.Key).All(allowedRoot.Contains), "fixture root contains a forbidden field");
        var cases = fixture["cases"]!.AsArray();
        Require(cases.Count == 8, "fixture must contain eight cases");
        foreach (var item in cases)
        {
            var obj = item!.AsObject();
            Require(obj.Select(pair => pair.Key).All(allowedTop.Contains), $"{obj["case_id"]} contains an expected/assertion field");
        }
        Require(cases.Select(item => item!["case_id"]!.GetValue<string>()).SequenceEqual(Expected.Keys), "case IDs are not LR-01 through LR-08 in order");
        Console.WriteLine("STATIC_CONTRACT PASS runtime_isolated=true bcl_only=true fixture_has_no_expected_answers=true network_surface=0 provider_surface=0 cases=8");
        return 0;
    }

    private static int FormalSuite(string[] args)
    {
        var suite = RequiredOption(args, "--suite");
        var output = ValidateRunRoot(RequiredOption(args, "--output"), suite);
        PrepareRunRoot(output, suite);
        var evidence = new JsonArray();
        var fixtureCases = ReadObject(CasesPath)["cases"]!.AsArray();
        foreach (var item in fixtureCases)
        {
            var caseId = item!["case_id"]!.GetValue<string>();
            var expectation = Expected[caseId];
            var caseRoot = Path.Combine(output, caseId);
            Directory.CreateDirectory(caseRoot);
            var start = LaunchRuntime(caseId, "START", caseRoot);
            evidence.Add(ProcessEvidence(caseId, "START", start));
            Require(start.ExitCode == expectation.StartExit, $"{caseId} START exit {start.ExitCode}, expected {expectation.StartExit}; stderr={start.StandardError}");
            ProcessRun? resume = null;
            if (expectation.ResumeExit is int resumeExit)
            {
                resume = LaunchRuntime(caseId, "RESUME", caseRoot);
                evidence.Add(ProcessEvidence(caseId, "RESUME", resume));
                Require(resume.ExitCode == resumeExit, $"{caseId} RESUME exit {resume.ExitCode}, expected {resumeExit}; stderr={resume.StandardError}");
                Require(resume.ProcessId != start.ProcessId, $"{caseId} START and RESUME reused PID");
            }
            VerifyCase(caseId, caseRoot, expectation, start, resume);
            WriteManifest(caseRoot);
            Console.WriteLine($"CASE {caseId} PASS start_pid={start.ProcessId} start_exit={start.ExitCode}" + (resume is null ? string.Empty : $" resume_pid={resume.ProcessId} resume_exit={resume.ExitCode}"));
        }
        var processEvidencePath = Path.Combine(Path.GetDirectoryName(output)!, $"process-evidence-{suite}.json");
        WriteJson(processEvidencePath, new JsonObject { ["suite"] = suite, ["processes"] = evidence });
        Console.WriteLine($"FORMAL_SUITE PASS suite={suite} cases=8 process_evidence={processEvidencePath}");
        return 0;
    }

    private static int Compare(string[] args)
    {
        var left = Path.GetFullPath(RequiredOption(args, "--left"), Directory.GetCurrentDirectory());
        var right = Path.GetFullPath(RequiredOption(args, "--right"), Directory.GetCurrentDirectory());
        var leftFiles = Directory.GetFiles(left, "*", SearchOption.AllDirectories).Where(IsNormalizedFile).Select(path => Path.GetRelativePath(left, path).Replace('\\', '/')).Order(StringComparer.Ordinal).ToArray();
        var rightFiles = Directory.GetFiles(right, "*", SearchOption.AllDirectories).Where(IsNormalizedFile).Select(path => Path.GetRelativePath(right, path).Replace('\\', '/')).Order(StringComparer.Ordinal).ToArray();
        Require(leftFiles.SequenceEqual(rightFiles, StringComparer.Ordinal), "normalized file sets differ");
        var aggregate = new StringBuilder();
        foreach (var relative in leftFiles)
        {
            var leftBytes = File.ReadAllBytes(Path.Combine(left, relative.Replace('/', Path.DirectorySeparatorChar)));
            var rightBytes = File.ReadAllBytes(Path.Combine(right, relative.Replace('/', Path.DirectorySeparatorChar)));
            Require(leftBytes.AsSpan().SequenceEqual(rightBytes), $"byte mismatch: {relative}");
            aggregate.Append(relative).Append(':').Append(Convert.ToHexString(SHA256.HashData(leftBytes)).ToLowerInvariant()).Append('\n');
        }
        var aggregateHash = Convert.ToHexString(SHA256.HashData(Utf8NoBom.GetBytes(aggregate.ToString()))).ToLowerInvariant();
        Console.WriteLine($"COMPARE PASS files={leftFiles.Length} aggregate_sha256={aggregateHash}");
        return 0;
    }

    private static void VerifyCase(string caseId, string caseRoot, CaseExpectation expected, ProcessRun start, ProcessRun? resume)
    {
        var result = ReadObject(Path.Combine(caseRoot, "case-result.json"));
        Require(Value(result, "terminal_status") == expected.TerminalStatus, $"{caseId} terminal status mismatch");
        Require(Value(result, "terminal_reason") == expected.TerminalReason, $"{caseId} terminal reason mismatch");
        Require(Int(result, "effect_count") == expected.EffectCount, $"{caseId} effect count mismatch");
        Require(Int(result, "attempts_used") == expected.Attempts, $"{caseId} attempts mismatch");
        Require(Int(result, "retry_count") == expected.RetryCount, $"{caseId} retry count mismatch");
        Require(Int(result, "network_attempts") == 0 && Int(result, "provider_attempts") == 0 && Int(result, "credential_reads") == 0, $"{caseId} external surface was used");
        var store = ReadObject(Path.Combine(caseRoot, "fake-store-view.json"));
        Require(store["records"]!.AsArray().Count == expected.EffectCount, $"{caseId} store records mismatch");
        var traceLines = File.ReadAllLines(Path.Combine(caseRoot, "trace.jsonl"), Utf8NoBom);
        Require(traceLines.Length > 0 && traceLines.All(line => JsonNode.Parse(line) is JsonObject), $"{caseId} trace is invalid");
        Require(traceLines.Count(line => line.Contains("EVIDENCE_ACTION_COMPLETED", StringComparison.Ordinal)) == 1, $"{caseId} evidence action reran or is missing");
        Require(!Directory.GetFiles(caseRoot, "*", SearchOption.AllDirectories).Where(IsNormalizedFile).Any(path => File.ReadAllText(path).Contains(caseRoot, StringComparison.OrdinalIgnoreCase)), $"{caseId} normalized artifact contains absolute run root");

        if (expected.HasResume)
        {
            Require(resume is not null && start.ProcessId != resume.ProcessId, $"{caseId} lacks fresh resume process");
            Require(File.Exists(Path.Combine(caseRoot, "phase-start-result.json")) && File.Exists(Path.Combine(caseRoot, "phase-resume-result.json")), $"{caseId} phase evidence missing");
        }
        if (caseId == "LR-02")
        {
            var startResult = ReadObject(Path.Combine(caseRoot, "phase-start-result.json"));
            Require(Value(startResult, "terminal_status") == "CANCELLED" && Int(startResult, "effect_count") == 0, "LR-02 was not cancelled before effect");
            Require(Value(startResult, "cancellation_origin") == "CALLER", "LR-02 origin is not CALLER");
            Require(Int(result, "resume_count") == 1, "LR-02 resume count mismatch");
        }
        if (caseId == "LR-03") Require(traceLines.Any(line => line.Contains("RETRY_APPROVED", StringComparison.Ordinal)), "LR-03 retry decision missing");
        if (caseId == "LR-04")
        {
            var startCheckpoint = ReadObject(Path.Combine(caseRoot, "checkpoint-start.json"));
            Require(Value(startCheckpoint["in_flight_action"]!.AsObject(), "result_status") == "RESULT_UNKNOWN", "LR-04 did not preserve RESULT_UNKNOWN");
            Require(traceLines.Any(line => line.Contains("CREATE_OR_GET_EXISTING", StringComparison.Ordinal)), "LR-04 did not reconcile existing effect");
            Require(traceLines.Where(line => line.Contains("REGISTER_ACTION_STARTED", StringComparison.Ordinal)).Select(line => Value(JsonNode.Parse(line)!.AsObject(), "action_id")).Distinct(StringComparer.Ordinal).Count() == 1, "LR-04 action identity changed");
        }
        if (caseId == "LR-05")
        {
            Require(store["records"]!.AsArray().Select(node => Value(node!.AsObject(), "delivery_id")).Distinct(StringComparer.Ordinal).Count() == 2, "LR-05 duplicate was not created by distinct deliveries");
            Require(traceLines.Any(line => line.Contains("DUPLICATE_DETECTED_FROM_STORE", StringComparison.Ordinal)), "LR-05 duplicate flag lacks store observation");
        }
        if (caseId == "LR-06")
        {
            Require(File.Exists(Path.Combine(caseRoot, "checkpoint-invalid.json")), "LR-06 invalid checkpoint artifact missing");
            Require(Int(store, "access_count") == 1, "LR-06 resume accessed fake store before refusal");
            Require(traceLines.Any(line => line.Contains("RECOVERY_VALIDATION_REFUSED", StringComparison.Ordinal)), "LR-06 refusal trace missing");
        }
        if (caseId == "LR-07")
        {
            var partial = ReadObject(Path.Combine(caseRoot, "partial-result.json"));
            Require(partial["known_refs"]!.AsArray().Count > 0 && partial["unverified_requirements"]!.AsArray().Count >= 2, "LR-07 partial result lost known/unverified data");
            Require(Value(partial, "next_safe_action") == "ASK_OR_STOP", "LR-07 next safe action is unsafe");
        }
        if (caseId == "LR-08")
        {
            Require(Value(result, "cancellation_origin") == "TIMEOUT", "LR-08 origin is not TIMEOUT");
            Require(!traceLines.Any(line => line.Contains("CALLER", StringComparison.Ordinal)), "LR-08 reused caller cancellation trace");
        }

        VerifyPartialProvenance(caseRoot);
    }

    private static void VerifyPartialProvenance(string caseRoot)
    {
        foreach (var path in Directory.GetFiles(caseRoot, "partial-result*.json", SearchOption.TopDirectoryOnly))
        {
            var partial = ReadObject(path);
            var known = Values(partial["known_refs"]!.AsArray()).ToHashSet(StringComparer.Ordinal);
            var unknown = Values(partial["unknown_actions"]!.AsArray()).ToHashSet(StringComparer.Ordinal);
            var unverified = Values(partial["unverified_requirements"]!.AsArray()).ToHashSet(StringComparer.Ordinal);
            Require(!known.Overlaps(unknown) && !known.Overlaps(unverified), $"{Path.GetFileName(path)} mixes uncertainty into known");
            foreach (var entry in partial["known_refs"]!.AsArray().Concat(partial["unknown_actions"]!.AsArray()).Concat(partial["unverified_requirements"]!.AsArray()))
                Require(!string.IsNullOrWhiteSpace(Value(entry!.AsObject(), "provenance")), $"{Path.GetFileName(path)} has missing provenance");
        }
    }

    private static IEnumerable<string> Values(JsonArray array) => array.Select(node => Value(node!.AsObject(), "value"));

    private static ProcessRun LaunchRuntime(string caseId, string phase, string caseRoot)
    {
        var start = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = LabRoot,
        };
        start.ArgumentList.Add(RuntimeDll);
        start.ArgumentList.Add("phase");
        start.ArgumentList.Add("--cases");
        start.ArgumentList.Add(CasesPath);
        start.ArgumentList.Add("--case");
        start.ArgumentList.Add(caseId);
        start.ArgumentList.Add("--phase");
        start.ArgumentList.Add(phase);
        start.ArgumentList.Add("--case-root");
        start.ArgumentList.Add(caseRoot);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("failed to start Runtime child process");
        var pid = process.Id;
        var standardOutput = process.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return new ProcessRun(pid, process.ExitCode, standardOutput.Trim(), standardError.Trim());
    }

    private static JsonObject ProcessEvidence(string caseId, string phase, ProcessRun run) => new()
    {
        ["case_id"] = caseId,
        ["process_phase"] = phase,
        ["pid"] = run.ProcessId,
        ["exit_code"] = run.ExitCode,
        ["stdout"] = run.StandardOutput,
        ["stderr"] = run.StandardError
    };

    private static string ValidateRunRoot(string raw, string suite)
    {
        Require(suite is "run-a" or "run-b", "suite must be run-a or run-b");
        var output = Path.GetFullPath(raw, Directory.GetCurrentDirectory());
        var observations = Path.GetFullPath(Path.Combine(LabRoot, "observations"));
        Require(Path.GetDirectoryName(output)!.Equals(observations, StringComparison.OrdinalIgnoreCase), "output must be a direct child of Lab observations");
        Require(Path.GetFileName(output).Equals(suite, StringComparison.Ordinal), "output directory must match suite name");
        return output;
    }

    private static void PrepareRunRoot(string output, string suite)
    {
        var sentinel = Path.Combine(output, ".lab04-run-root");
        if (Directory.Exists(output))
        {
            Require(File.Exists(sentinel), "existing output has no Lab sentinel; refusing cleanup");
            Require(File.ReadAllText(sentinel, Utf8NoBom) == suite + "\n", "Lab sentinel mismatch; refusing cleanup");
            Require((File.GetAttributes(output) & FileAttributes.ReparsePoint) == 0, "run root is a reparse point; refusing cleanup");
            Directory.Delete(output, true);
        }
        Directory.CreateDirectory(output);
        File.WriteAllText(sentinel, suite + "\n", Utf8NoBom);
    }

    private static void WriteManifest(string caseRoot)
    {
        var entries = new JsonArray();
        foreach (var path in Directory.GetFiles(caseRoot, "*", SearchOption.AllDirectories).Where(path => !Path.GetFileName(path).Equals("artifact-manifest.json", StringComparison.Ordinal)).Order(StringComparer.Ordinal))
        {
            var bytes = File.ReadAllBytes(path);
            entries.Add(new JsonObject
            {
                ["relative_path"] = Path.GetRelativePath(caseRoot, path).Replace('\\', '/'),
                ["byte_count"] = bytes.Length,
                ["sha256"] = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()
            });
        }
        WriteJson(Path.Combine(caseRoot, "artifact-manifest.json"), new JsonObject { ["schema_version"] = "artifact-manifest-v1", ["artifacts"] = entries });
    }

    private static bool IsNormalizedFile(string path) => !Path.GetFileName(path).Equals(".lab04-run-root", StringComparison.Ordinal);

    private static string RequiredOption(string[] args, string option)
    {
        var index = Array.IndexOf(args, option);
        Require(index >= 0 && index + 1 < args.Length, $"missing {option}");
        return args[index + 1];
    }

    private static JsonObject ReadObject(string path) => JsonNode.Parse(File.ReadAllText(path, Utf8NoBom))!.AsObject();
    private static string Value(JsonObject obj, string property) => obj[property]?.GetValue<string>() ?? string.Empty;
    private static int Int(JsonObject obj, string property) => obj[property]?.GetValue<int>() ?? 0;

    private static void WriteJson(string path, JsonNode node)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var text = node.ToJsonString(JsonOptions).Replace("\r\n", "\n") + "\n";
        File.WriteAllText(path, text, Utf8NoBom);
    }

    private static string FindLabRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "LongRunningAgentLab.slnx"))) current = current.Parent;
        return current?.FullName ?? throw new InvalidOperationException("Lab root not found");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static int Fail(string message)
    {
        Console.Error.WriteLine(message);
        return 1;
    }

    private sealed record CaseExpectation(int StartExit, int? ResumeExit, string TerminalStatus, string TerminalReason, int EffectCount, int Attempts, int RetryCount, bool HasResume);
    private sealed record ProcessRun(int ProcessId, int ExitCode, string StandardOutput, string StandardError);
}
