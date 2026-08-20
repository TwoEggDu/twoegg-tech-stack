using System.Text.Json;
using MinimalAgentLoop;

namespace MinimalAgentLoop.Tests;

internal static class Program
{
    private static readonly string[] NormalizedArtifacts =
    [
        "artifact-manifest.json",
        "case-results.jsonl",
        "observations.jsonl",
        "states.jsonl",
        "tool-outcomes.jsonl",
        "trace.jsonl"
    ];

    public static int Main(string[] args)
    {
        try
        {
            string labRoot = FindLabRoot();
            string fixtures = Path.Combine(labRoot, "fixtures");
            string casesPath = Path.Combine(fixtures, "cases.json");
            VerifyFrozenFixtures(fixtures);
            _ = LabRunner.LoadAndValidate(casesPath);

            if (args.Length == 3 && args[0] == "--verify-only")
            {
                VerifyRun(Path.GetFullPath(args[1], labRoot));
                VerifyRun(Path.GetFullPath(args[2], labRoot));
                VerifyByteEquality(Path.GetFullPath(args[1], labRoot), Path.GetFullPath(args[2], labRoot));
                Console.WriteLine("LAB03_SPEC PASS mode=verify-only");
                return 0;
            }

            Require(args.Length == 0, "Unexpected test arguments.");
            string preflightA = Path.Combine(labRoot, "observations", "test-preflight-a");
            string preflightB = Path.Combine(labRoot, "observations", "test-preflight-b");
            _ = LabRunner.Execute(casesPath, preflightA);
            _ = LabRunner.Execute(casesPath, preflightB);
            VerifyRun(preflightA);
            VerifyRun(preflightB);
            VerifyByteEquality(preflightA, preflightB);
            Require(Path.GetDirectoryName(preflightA) == Path.Combine(labRoot, "observations")
                && Path.GetDirectoryName(preflightB) == Path.Combine(labRoot, "observations"),
                "preflight cleanup targets escaped observations root");
            Directory.Delete(preflightA, recursive: true);
            Directory.Delete(preflightB, recursive: true);
            Console.WriteLine("LAB03_SPEC PASS mode=contract-tests cases=4 steps=10 terminals=4 states=10 tools=7 decisions=10 succeeded=1");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"LAB03_SPEC FAIL type={exception.GetType().Name} message={exception.Message}");
            Console.Error.WriteLine(exception.StackTrace);
            return 1;
        }
    }

    private static void VerifyRun(string runPath)
    {
        string fullRunPath = Path.GetFullPath(runPath);
        foreach (string file in NormalizedArtifacts)
        {
            Require(File.Exists(Path.Combine(fullRunPath, file)), $"artifact missing: {file}");
            string raw = File.ReadAllText(Path.Combine(fullRunPath, file), Canonical.Utf8NoBom);
            Require(!raw.Contains("\\", StringComparison.Ordinal), $"normalized artifact contains backslash/path-like data: {file}");
            Require(!raw.Contains(":\\", StringComparison.Ordinal), $"normalized artifact contains Windows absolute path: {file}");
        }

        List<JsonElement> trace = ReadJsonLines(Path.Combine(fullRunPath, "trace.jsonl"));
        List<JsonElement> states = ReadJsonLines(Path.Combine(fullRunPath, "states.jsonl"));
        List<JsonElement> results = ReadJsonLines(Path.Combine(fullRunPath, "case-results.jsonl"));
        List<JsonElement> toolOutcomes = ReadJsonLines(Path.Combine(fullRunPath, "tool-outcomes.jsonl"));
        List<JsonElement> observations = ReadJsonLines(Path.Combine(fullRunPath, "observations.jsonl"));

        Require(trace.Count == 14, "trace row count is not 14");
        Require(trace.Count(row => Text(row, "event_type") == "STEP") == 10, "STEP row count is not 10");
        Require(trace.Count(row => Text(row, "event_type") == "TERMINAL") == 4, "TERMINAL row count is not 4");
        Require(states.Count == 10, "state snapshot count is not 10");
        Require(results.Count == 4, "case result count is not 4");
        Require(toolOutcomes.Count == 7, "tool outcome count is not 7");
        Require(observations.Count == 7, "observation count is not 7");
        Require(trace.Select((row, index) => Number(row, "sequence") == index + 1).All(value => value), "trace sequence is not contiguous");

        string[] traceFields =
        [
            "schema_version", "sequence", "event_type", "case_id", "run_id", "turn_index", "step_index",
            "state_revision_before", "state_revision_after", "full_state_sha256_before", "full_state_sha256_after",
            "goal_state_sha256_before", "goal_state_sha256_after", "decision_source", "decision_id", "decision_kind",
            "decision_contract_status", "requested_outcome", "invocation_id", "tool_name", "arguments_sha256",
            "action_fingerprint", "tool_executed", "tool_result_disposition", "tool_result_code",
            "tool_result_record_sha256", "tool_result_payload_sha256", "observation_normalization_status",
            "observation_kind", "observation_sha256", "observation_source_result_sha256", "repeat_detected",
            "progress_status", "unresolved_requirement_codes", "unresolved_tool_failure_count", "steps_used",
            "max_steps", "remaining_steps", "decision_calls_used", "tool_calls_used", "model_stop_requested",
            "output_contract_status", "success_contract_status", "control_decision", "termination_reason", "run_outcome"
        ];
        foreach (JsonElement row in trace)
        {
            Require(traceFields.All(field => row.TryGetProperty(field, out _)), "trace row omits a frozen field");
            Require(Text(row, "schema_version") == "lab03-trace-v1", "trace schema mismatch");
        }

        var terminalExpected = new Dictionary<string, (string Termination, string Outcome)>(StringComparer.Ordinal)
        {
            ["AL-01"] = ("GOAL_SATISFIED", "SUCCEEDED"),
            ["AL-02"] = ("UNRESOLVED_TOOL_FAILURE", "FAILED"),
            ["AL-03"] = ("MAX_STEPS_EXHAUSTED", "INCOMPLETE"),
            ["AL-04"] = ("STOP_CONTRACT_FAILED", "FAILED")
        };
        Require(results.Select(row => Text(row, "case_id")).SequenceEqual(terminalExpected.Keys), "case result order mismatch");
        foreach (JsonElement result in results)
        {
            string caseId = Text(result, "case_id");
            Require(Text(result, "lifecycle") == "STOPPED", $"{caseId}: lifecycle is not STOPPED");
            Require(Text(result, "termination_reason") == terminalExpected[caseId].Termination, $"{caseId}: termination mismatch");
            Require(Text(result, "run_outcome") == terminalExpected[caseId].Outcome, $"{caseId}: outcome mismatch");
        }
        Require(results.Count(row => Text(row, "run_outcome") == "SUCCEEDED") == 1, "SUCCEEDED count is not exactly one");
        Require(results.Sum(row => Number(row, "steps_used")) == 10, "steps total is not 10");
        Require(results.Sum(row => Number(row, "tool_calls_used")) == 7, "tool-call total is not 7");
        Require(results.Sum(row => Number(row, "decision_calls_used")) == 10, "decision-call total is not 10");

        foreach (IGrouping<string, JsonElement> group in states.GroupBy(row => Text(row, "case_id"), StringComparer.Ordinal))
        {
            int expectedRevision = 1;
            foreach (JsonElement state in group)
            {
                Require(Number(state, "revision") == expectedRevision++, $"{group.Key}: state revisions not contiguous");
                VerifyStateDigest(state);
            }
        }
        foreach (JsonElement step in trace.Where(row => Text(row, "event_type") == "STEP"))
        {
            Require(Number(step, "state_revision_after") == Number(step, "state_revision_before") + 1,
                $"{Text(step, "case_id")}: Step did not commit exactly one revision");
            bool snapshotExists = states.Any(state => Text(state, "case_id") == Text(step, "case_id")
                && Number(state, "revision") == Number(step, "state_revision_after")
                && Text(state, "full_state_sha256") == Text(step, "full_state_sha256_after"));
            Require(snapshotExists, $"{Text(step, "case_id")}: Step after-state snapshot missing");
        }

        var toolByDigest = toolOutcomes.ToDictionary(row => Text(row, "tool_result_record_sha256"), StringComparer.Ordinal);
        var observationByDigest = observations.ToDictionary(
            row => Canonical.Sha256(Canonical.Json(row)),
            row => row,
            StringComparer.Ordinal);
        foreach (JsonElement act in trace.Where(row => Text(row, "event_type") == "STEP" && Text(row, "decision_kind") == "ACT"))
        {
            string resultDigest = Text(act, "tool_result_record_sha256");
            Require(toolByDigest.ContainsKey(resultDigest), "ACT trace does not resolve to Tool Outcome");
            Require(Text(act, "observation_source_result_sha256") == resultDigest, "Observation source does not reference Tool Outcome");
            string observationDigest = Text(act, "observation_sha256");
            Require(observationByDigest.TryGetValue(observationDigest, out JsonElement observation), "ACT trace does not resolve to Observation");
            Require(Text(observation, "source_result_record_sha256") == resultDigest, "Observation record source digest mismatch");
        }

        JsonElement al02Result = toolOutcomes.Single(row => Text(row, "case_id") == "AL-02");
        JsonElement al02Observation = observations.Single(row => CaseFromObservation(row) == "AL-02");
        Require(Text(al02Result, "disposition") == "FAILED" && Text(al02Result, "code") == "MOCK_PARSE_FAILED",
            "AL-02 named fault did not produce typed failed Tool Outcome");
        Require(Text(al02Observation, "normalization_status") == "PASS" && Text(al02Observation, "kind") == "TOOL_FAILURE",
            "AL-02 failed outcome was not normalized as TOOL_FAILURE");
        Require(Text(al02Observation, "source_result_record_sha256") == Text(al02Result, "tool_result_record_sha256"),
            "AL-02 failure Observation does not reference failed Tool Outcome");

        JsonElement al03 = results.Single(row => Text(row, "case_id") == "AL-03");
        Require(Number(al03, "decision_calls_used") == 2 && Number(al03, "tool_calls_used") == 2, "AL-03 guard counters mismatch");
        Require(al03.GetProperty("remaining_decision_ids").EnumerateArray().Select(item => item.GetString())
            .SequenceEqual(["al03-decision-03"]), "AL-03 third candidate was not preserved as unconsumed");
        Require(!trace.Any(row => Text(row, "decision_id") == "al03-decision-03"), "AL-03 third candidate was consumed");

        JsonElement[] al04Acts = trace.Where(row => Text(row, "case_id") == "AL-04" && Text(row, "decision_kind") == "ACT").ToArray();
        Require(al04Acts.Length == 2, "AL-04 ACT count mismatch");
        Require(Text(al04Acts[0], "invocation_id") != Text(al04Acts[1], "invocation_id"), "AL-04 invocation IDs are not distinct");
        Require(Text(al04Acts[0], "action_fingerprint") == Text(al04Acts[1], "action_fingerprint"), "AL-04 action fingerprints differ");
        Require(Text(al04Acts[0], "tool_result_payload_sha256") == Text(al04Acts[1], "tool_result_payload_sha256"), "AL-04 semantic payload digests differ");
        Require(Text(al04Acts[0], "tool_result_record_sha256") != Text(al04Acts[1], "tool_result_record_sha256"), "AL-04 correlated record digests match");
        Require(Text(al04Acts[0], "full_state_sha256_before") != Text(al04Acts[0], "full_state_sha256_after")
            && Text(al04Acts[1], "full_state_sha256_before") != Text(al04Acts[1], "full_state_sha256_after"),
            "AL-04 full-state digest did not change for both reads");
        Require(Text(al04Acts[0], "goal_state_sha256_before") == Text(al04Acts[0], "goal_state_sha256_after")
            && Text(al04Acts[1], "goal_state_sha256_before") == Text(al04Acts[1], "goal_state_sha256_after"),
            "AL-04 goal-state digest changed for irrelevant reads");
        Require(al04Acts.All(row => Text(row, "progress_status") == "NO_PROGRESS"), "AL-04 progress is not NO_PROGRESS");
        Require(al04Acts[0].GetProperty("repeat_detected").ValueKind == JsonValueKind.False
            && al04Acts[1].GetProperty("repeat_detected").ValueKind == JsonValueKind.True, "AL-04 repeat flags mismatch");
        JsonElement al04FinalState = states.Last(row => Text(row, "case_id") == "AL-04");
        Require(al04FinalState.GetProperty("rejected_evidence_ids").EnumerateArray().Select(item => item.GetString()).Contains("EV-FAKE"),
            "AL-04 fake evidence was not rejected");

        foreach (JsonElement stopStep in trace.Where(row => Text(row, "event_type") == "STEP" && Text(row, "decision_kind") == "REQUEST_STOP"))
        {
            Require(Text(stopStep, "tool_name") == "NOT_RUN" && Text(stopStep, "tool_result_disposition") == "NOT_RUN"
                && Text(stopStep, "observation_kind") == "NOT_RUN", "REQUEST_STOP has entered tool/observation fields");
        }
        foreach (JsonElement terminal in trace.Where(row => Text(row, "event_type") == "TERMINAL"))
        {
            Require(Text(terminal, "decision_id") == "NOT_RUN" && Text(terminal, "tool_name") == "NOT_RUN"
                && Text(terminal, "observation_kind") == "NOT_RUN", "TERMINAL has entered phase fields");
        }

        VerifyManifest(fullRunPath);
    }

    private static void VerifyStateDigest(JsonElement state)
    {
        var full = new SortedDictionary<string, object?>(StringComparer.Ordinal);
        foreach (JsonProperty property in state.EnumerateObject())
        {
            if (property.Name is "full_state_sha256" or "goal_state_sha256") continue;
            full[property.Name] = JsonElementToObject(property.Value);
        }
        string computedFull = Canonical.Sha256(Canonical.Json(full));
        Require(computedFull == Text(state, "full_state_sha256"),
            $"full-state digest mismatch expected={Text(state, "full_state_sha256")} computed={computedFull}");

        var goal = new SortedDictionary<string, object?>(StringComparer.Ordinal)
        {
            ["accepted_goal_evidence_ids"] = JsonElementToObject(state.GetProperty("accepted_goal_evidence_ids")),
            ["sorted_facts"] = JsonElementToObject(state.GetProperty("sorted_facts")),
            ["unresolved_requirement_codes"] = JsonElementToObject(state.GetProperty("unresolved_requirement_codes")),
            ["unresolved_tool_failures"] = JsonElementToObject(state.GetProperty("unresolved_tool_failures"))
        };
        string computedGoal = Canonical.Sha256(Canonical.Json(goal));
        Require(computedGoal == Text(state, "goal_state_sha256"),
            $"goal-state digest mismatch expected={Text(state, "goal_state_sha256")} computed={computedGoal}");
    }

    private static void VerifyManifest(string runPath)
    {
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(Path.Combine(runPath, "artifact-manifest.json"), Canonical.Utf8NoBom));
        JsonElement root = document.RootElement;
        Require(Text(root, "schema_version") == "lab03-artifact-manifest-v1", "manifest schema mismatch");
        foreach (JsonElement artifact in root.GetProperty("artifacts").EnumerateArray())
        {
            string path = Text(artifact, "path");
            Require(!Path.IsPathRooted(path) && !path.Contains('/', StringComparison.Ordinal) && !path.Contains('\\', StringComparison.Ordinal),
                "manifest artifact path is not stable relative filename");
            Require(Canonical.FileSha256(Path.Combine(runPath, path)) == Text(artifact, "sha256"), $"manifest hash mismatch: {path}");
        }
    }

    private static void VerifyByteEquality(string first, string second)
    {
        foreach (string file in NormalizedArtifacts)
        {
            byte[] firstBytes = File.ReadAllBytes(Path.Combine(first, file));
            byte[] secondBytes = File.ReadAllBytes(Path.Combine(second, file));
            Require(firstBytes.AsSpan().SequenceEqual(secondBytes), $"fresh-process normalized artifact differs: {file}");
            Require(Canonical.Sha256(firstBytes) == Canonical.Sha256(secondBytes), $"fresh-process hash differs: {file}");
        }
    }

    private static void VerifyFrozenFixtures(string fixturesRoot)
    {
        const string buildLog = "BuildMenu.cs(3,5): error CS0103: The name 'missingIdentifier' does not exist in the current context\n";
        const string buildMenu = "public static class BuildMenu\n{\n    missingIdentifier();\n}\n";
        const string unrelated = "public static class Unrelated\n{\n    public static void NoOp() { }\n}\n";
        Require(File.ReadAllText(Path.Combine(fixturesRoot, "build.log"), Canonical.Utf8NoBom).Replace("\r\n", "\n", StringComparison.Ordinal) == buildLog,
            "build.log differs from frozen content");
        Require(File.ReadAllText(Path.Combine(fixturesRoot, "BuildMenu.cs"), Canonical.Utf8NoBom).Replace("\r\n", "\n", StringComparison.Ordinal) == buildMenu,
            "BuildMenu.cs differs from frozen content");
        Require(File.ReadAllText(Path.Combine(fixturesRoot, "Unrelated.cs"), Canonical.Utf8NoBom).Replace("\r\n", "\n", StringComparison.Ordinal) == unrelated,
            "Unrelated.cs differs from frozen content");
    }

    private static List<JsonElement> ReadJsonLines(string path)
    {
        var rows = new List<JsonElement>();
        foreach (string line in File.ReadAllLines(path, Canonical.Utf8NoBom))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            using JsonDocument document = JsonDocument.Parse(line);
            rows.Add(document.RootElement.Clone());
        }
        return rows;
    }

    private static string FindLabRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "fixtures", "cases.json"))
                && File.Exists(Path.Combine(directory.FullName, "README.md")))
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate Lab root from test binary.");
    }

    private static string Text(JsonElement row, string property)
    {
        return row.GetProperty(property).GetString() ?? throw new InvalidDataException($"{property} is null");
    }

    private static string CaseFromObservation(JsonElement row) => Text(row, "observation_id").Split('/')[0];

    private static int Number(JsonElement row, string property) => row.GetProperty(property).GetInt32();

    private static object? JsonElementToObject(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => new SortedDictionary<string, object?>(
                element.EnumerateObject().ToDictionary(item => item.Name, item => JsonElementToObject(item.Value), StringComparer.Ordinal),
                StringComparer.Ordinal),
            JsonValueKind.Array => element.EnumerateArray().Select(JsonElementToObject).ToArray(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt32(out int value) => value,
            JsonValueKind.Number => element.GetDecimal(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => throw new InvalidDataException("Unsupported JSON value kind.")
        };
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidDataException(message);
    }
}
