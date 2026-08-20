using System.Text.Json;

namespace MinimalAgentLoop;

public static class LabRunner
{
    private static readonly JsonSerializerOptions InputJsonOptions = new()
    {
        PropertyNameCaseInsensitive = false
    };

    private static readonly HashSet<string> EvidenceDomain = new(
        ["EV-FILE-001", "EV-LOG-001", "EV-UNRELATED-001"],
        StringComparer.Ordinal);

    public static SuiteExecutionSummary Execute(string casesPath, string outputPath)
    {
        string fullCasesPath = Path.GetFullPath(casesPath);
        string fixturesRoot = Path.GetDirectoryName(fullCasesPath)
            ?? throw new InvalidDataException("cases.json has no fixture directory.");
        string labRoot = Directory.GetParent(fixturesRoot)?.FullName
            ?? throw new InvalidDataException("fixtures directory has no Lab root.");
        string observationsRoot = Path.Combine(labRoot, "observations");
        string fullOutputPath = Path.GetFullPath(outputPath);
        RequireContained(observationsRoot, fullOutputPath, "output directory");

        CaseSuite suite = LoadAndValidate(fullCasesPath);
        Directory.CreateDirectory(fullOutputPath);

        var traces = new List<SortedDictionary<string, object?>>();
        var states = new List<SortedDictionary<string, object?>>();
        var results = new List<SortedDictionary<string, object?>>();
        var toolOutcomes = new List<SortedDictionary<string, object?>>();
        var observations = new List<SortedDictionary<string, object?>>();
        int sequence = 0;

        foreach (CaseConfig caseConfig in suite.Cases)
        {
            ExecuteCase(caseConfig, fixturesRoot, traces, states, results, toolOutcomes, observations, ref sequence);
        }

        string tracePath = Path.Combine(fullOutputPath, "trace.jsonl");
        string statesPath = Path.Combine(fullOutputPath, "states.jsonl");
        string resultsPath = Path.Combine(fullOutputPath, "case-results.jsonl");
        string toolOutcomesPath = Path.Combine(fullOutputPath, "tool-outcomes.jsonl");
        string observationsPath = Path.Combine(fullOutputPath, "observations.jsonl");
        Canonical.WriteJsonLines(tracePath, traces);
        Canonical.WriteJsonLines(statesPath, states);
        Canonical.WriteJsonLines(resultsPath, results);
        Canonical.WriteJsonLines(toolOutcomesPath, toolOutcomes);
        Canonical.WriteJsonLines(observationsPath, observations);

        var artifactHashes = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["case-results.jsonl"] = Canonical.FileSha256(resultsPath),
            ["observations.jsonl"] = Canonical.FileSha256(observationsPath),
            ["states.jsonl"] = Canonical.FileSha256(statesPath),
            ["tool-outcomes.jsonl"] = Canonical.FileSha256(toolOutcomesPath),
            ["trace.jsonl"] = Canonical.FileSha256(tracePath)
        };

        var fixtureHashes = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["BuildMenu.cs"] = Canonical.FileSha256(ResolveFixture(fixturesRoot, "BuildMenu.cs")),
            ["Unrelated.cs"] = Canonical.FileSha256(ResolveFixture(fixturesRoot, "Unrelated.cs")),
            ["build.log"] = Canonical.FileSha256(ResolveFixture(fixturesRoot, "build.log")),
            ["cases.json"] = Canonical.FileSha256(fullCasesPath)
        };

        var manifest = new SortedDictionary<string, object?>(StringComparer.Ordinal)
        {
            ["artifacts"] = artifactHashes.Select(pair => new SortedDictionary<string, object?>(StringComparer.Ordinal)
            {
                ["path"] = pair.Key,
                ["sha256"] = pair.Value
            }).ToArray(),
            ["fixture_sha256"] = fixtureHashes,
            ["schema_version"] = "lab03-artifact-manifest-v1",
            ["schema_versions"] = new SortedDictionary<string, object?>(StringComparer.Ordinal)
            {
                ["case_result"] = "lab03-case-result-v1",
                ["observation"] = "lab03-observation-v1",
                ["state"] = "lab03-state-v1",
                ["tool_outcome"] = "lab03-tool-outcome-v1",
                ["trace"] = "lab03-trace-v1"
            }
        };
        Canonical.WriteJson(Path.Combine(fullOutputPath, "artifact-manifest.json"), manifest);
        artifactHashes["artifact-manifest.json"] = Canonical.FileSha256(Path.Combine(fullOutputPath, "artifact-manifest.json"));

        int stepCount = traces.Count(row => Equals(row["event_type"], "STEP"));
        int terminalCount = traces.Count(row => Equals(row["event_type"], "TERMINAL"));
        int toolCallCount = results.Sum(row => Convert.ToInt32(row["tool_calls_used"], System.Globalization.CultureInfo.InvariantCulture));
        int decisionCallCount = results.Sum(row => Convert.ToInt32(row["decision_calls_used"], System.Globalization.CultureInfo.InvariantCulture));
        int succeededCount = results.Count(row => Equals(row["run_outcome"], "SUCCEEDED"));
        return new SuiteExecutionSummary(
            results.Count,
            stepCount,
            terminalCount,
            states.Count,
            toolCallCount,
            decisionCallCount,
            succeededCount,
            artifactHashes);
    }

    public static CaseSuite LoadAndValidate(string casesPath)
    {
        string json = File.ReadAllText(casesPath, Canonical.Utf8NoBom);
        using JsonDocument document = JsonDocument.Parse(json);
        ValidateAntiSelfFulfilling(document.RootElement);
        CaseSuite suite = JsonSerializer.Deserialize<CaseSuite>(json, InputJsonOptions)
            ?? throw new InvalidDataException("cases.json deserialized to null.");
        Require(suite.SchemaVersion == "lab03-cases-v1", "cases schema version mismatch");
        Require(suite.Cases.Select(item => item.CaseId).SequenceEqual(["AL-01", "AL-02", "AL-03", "AL-04"]),
            "case order or IDs differ from frozen matrix");
        Require(suite.Cases.Select(item => item.CaseId).Distinct(StringComparer.Ordinal).Count() == 4, "case IDs are not unique");

        foreach (CaseConfig caseConfig in suite.Cases)
        {
            Require(caseConfig.GoalContractId == "goal-contract-v1", $"{caseConfig.CaseId}: goal contract mismatch");
            Require(caseConfig.MaxSteps > 0, $"{caseConfig.CaseId}: max_steps must be positive");
            Require(caseConfig.Decisions.Count > 0, $"{caseConfig.CaseId}: decisions missing");
            Require(caseConfig.Decisions.Select(item => item.DecisionId).Distinct(StringComparer.Ordinal).Count() == caseConfig.Decisions.Count,
                $"{caseConfig.CaseId}: decision IDs are not unique");
            foreach (DecisionConfig decision in caseConfig.Decisions)
            {
                Require(decision.SchemaVersion == "lab03-decision-v1", $"{decision.DecisionId}: schema mismatch");
                Require(decision.DecisionSource == "SCRIPTED_V1", $"{decision.DecisionId}: source mismatch");
                Require(decision.Kind is "ACT" or "REQUEST_STOP", $"{decision.DecisionId}: invalid kind");
                if (decision.Kind == "ACT")
                {
                    Require(decision.InvocationId != "NOT_RUN", $"{decision.DecisionId}: ACT invocation missing");
                    Require(decision.ToolName is "parse_mock_log" or "read_mock_file", $"{decision.DecisionId}: tool not allowed");
                    Require(decision.Arguments.ValueKind == JsonValueKind.Object, $"{decision.DecisionId}: ACT arguments must be object");
                    Require(decision.RequestedOutcome == "NOT_RUN", $"{decision.DecisionId}: ACT requested outcome must be NOT_RUN");
                }
                else
                {
                    Require(decision.InvocationId == "NOT_RUN" && decision.ToolName == "NOT_RUN",
                        $"{decision.DecisionId}: stop tool fields must be NOT_RUN");
                    Require(decision.RequestedOutcome == "SUCCEEDED", $"{decision.DecisionId}: frozen stop request mismatch");
                }
            }
        }

        Require(suite.Cases.Single(item => item.CaseId == "AL-01").MaxSteps == 3, "AL-01 max_steps mismatch");
        Require(suite.Cases.Single(item => item.CaseId == "AL-02").NamedFaultId == "FI_PARSE_TYPED_FAILURE", "AL-02 fault missing");
        Require(suite.Cases.Single(item => item.CaseId == "AL-02").FaultTargetInvocation == "al02-call-01", "AL-02 fault target mismatch");
        Require(suite.Cases.Single(item => item.CaseId == "AL-03").MaxSteps == 2, "AL-03 max_steps mismatch");
        Require(suite.Cases.Single(item => item.CaseId == "AL-03").Decisions.Count == 3, "AL-03 third candidate missing");
        Require(suite.Cases.Single(item => item.CaseId == "AL-04").MaxSteps == 4, "AL-04 max_steps mismatch");
        return suite;
    }

    private static void ExecuteCase(
        CaseConfig caseConfig,
        string fixturesRoot,
        List<SortedDictionary<string, object?>> traces,
        List<SortedDictionary<string, object?>> states,
        List<SortedDictionary<string, object?>> results,
        List<SortedDictionary<string, object?>> toolOutcomes,
        List<SortedDictionary<string, object?>> observations,
        ref int sequence)
    {
        var state = new AgentState
        {
            CaseId = caseConfig.CaseId,
            RunId = $"lab03-suite-v1/{caseConfig.CaseId}",
            MaxSteps = caseConfig.MaxSteps
        };
        RefreshStateDigests(state);
        int cursor = 0;

        while (state.Lifecycle == "RUNNING")
        {
            if (state.StepsUsed >= state.MaxSteps)
            {
                string guardBeforeFull = state.FullStateSha256;
                string guardBeforeGoal = state.GoalStateSha256;
                int guardBeforeRevision = state.Revision;
                state.Lifecycle = "STOPPED";
                state.Outcome = "INCOMPLETE";
                state.TerminationReason = "MAX_STEPS_EXHAUSTED";
                state.OutputContractStatus = "NOT_RUN";
                state.SuccessContractStatus = "FAIL";
                state.ProgressStatus = "NOT_RUN";
                RefreshStateDigests(state);
                traces.Add(TerminalTrace(++sequence, state, guardBeforeRevision, guardBeforeFull, guardBeforeGoal));
                break;
            }

            Require(cursor < caseConfig.Decisions.Count, $"{caseConfig.CaseId}: script exhausted before terminal");
            DecisionConfig decision = caseConfig.Decisions[cursor++];
            state.DecisionCallsUsed++;
            int beforeRevision = state.Revision;
            string beforeFull = state.FullStateSha256;
            string beforeGoal = state.GoalStateSha256;

            if (decision.Kind == "ACT")
            {
                ExecuteAct(caseConfig, decision, fixturesRoot, state, traces, states, toolOutcomes, observations,
                    ref sequence, beforeRevision, beforeFull, beforeGoal);
            }
            else
            {
                ExecuteStop(decision, state, traces, states, ref sequence, beforeRevision, beforeFull, beforeGoal);
            }
        }

        IReadOnlyList<string> consumed = caseConfig.Decisions.Take(cursor).Select(item => item.DecisionId).ToArray();
        IReadOnlyList<string> remaining = caseConfig.Decisions.Skip(cursor).Select(item => item.DecisionId).ToArray();
        results.Add(new SortedDictionary<string, object?>(StringComparer.Ordinal)
        {
            ["case_id"] = state.CaseId,
            ["consumed_decision_ids"] = consumed,
            ["decision_calls_used"] = state.DecisionCallsUsed,
            ["final_full_state_sha256"] = state.FullStateSha256,
            ["final_goal_state_sha256"] = state.GoalStateSha256,
            ["lifecycle"] = state.Lifecycle,
            ["output_contract_status"] = state.OutputContractStatus,
            ["remaining_decision_ids"] = remaining,
            ["run_id"] = state.RunId,
            ["run_outcome"] = state.Outcome,
            ["schema_version"] = "lab03-case-result-v1",
            ["scripted_decision_count"] = caseConfig.Decisions.Count,
            ["steps_used"] = state.StepsUsed,
            ["success_contract_status"] = state.SuccessContractStatus,
            ["termination_reason"] = state.TerminationReason,
            ["tool_calls_used"] = state.ToolCallsUsed
        });
    }

    private static void ExecuteAct(
        CaseConfig caseConfig,
        DecisionConfig decision,
        string fixturesRoot,
        AgentState state,
        List<SortedDictionary<string, object?>> traces,
        List<SortedDictionary<string, object?>> states,
        List<SortedDictionary<string, object?>> toolOutcomes,
        List<SortedDictionary<string, object?>> observations,
        ref int sequence,
        int beforeRevision,
        string beforeFull,
        string beforeGoal)
    {
        int stepIndex = state.StepsUsed + 1;
        string canonicalArguments = Canonical.Json(decision.Arguments);
        string argumentsSha256 = Canonical.Sha256(canonicalArguments);
        string actionFingerprint = Canonical.Sha256(decision.ToolName + canonicalArguments);
        bool repeat = state.LastActionFingerprint == actionFingerprint;
        ToolOutcome outcome = ExecuteTool(caseConfig, decision, fixturesRoot, stepIndex);
        state.ToolCallsUsed++;
        NormalizedObservation observation = Normalize(outcome);
        toolOutcomes.Add(ToolOutcomeRecord(outcome));
        observations.Add(observation.Record);

        ApplyObservation(state, decision, outcome, observation, actionFingerprint, repeat);
        state.Revision++;
        state.StepsUsed++;
        state.HistoryLength++;
        state.ProgressStatus = "NOT_RUN";
        RefreshStateDigests(state);
        state.ProgressStatus = state.GoalStateSha256 == beforeGoal ? "NO_PROGRESS" : "PROGRESS";
        RefreshStateDigests(state);
        states.Add(StateRecord(state));

        traces.Add(new SortedDictionary<string, object?>(StringComparer.Ordinal)
        {
            ["action_fingerprint"] = actionFingerprint,
            ["arguments_sha256"] = argumentsSha256,
            ["case_id"] = state.CaseId,
            ["control_decision"] = "CONTINUE",
            ["decision_calls_used"] = state.DecisionCallsUsed,
            ["decision_contract_status"] = "PASS",
            ["decision_id"] = decision.DecisionId,
            ["decision_kind"] = decision.Kind,
            ["decision_source"] = decision.DecisionSource,
            ["event_type"] = "STEP",
            ["full_state_sha256_after"] = state.FullStateSha256,
            ["full_state_sha256_before"] = beforeFull,
            ["goal_state_sha256_after"] = state.GoalStateSha256,
            ["goal_state_sha256_before"] = beforeGoal,
            ["invocation_id"] = decision.InvocationId,
            ["max_steps"] = state.MaxSteps,
            ["model_stop_requested"] = false,
            ["observation_kind"] = observation.Kind,
            ["observation_normalization_status"] = "PASS",
            ["observation_sha256"] = observation.ObservationSha256,
            ["observation_source_result_sha256"] = observation.SourceResultRecordSha256,
            ["output_contract_status"] = "NOT_RUN",
            ["progress_status"] = state.ProgressStatus,
            ["remaining_steps"] = state.MaxSteps - state.StepsUsed,
            ["repeat_detected"] = repeat,
            ["requested_outcome"] = "NOT_RUN",
            ["run_id"] = state.RunId,
            ["run_outcome"] = "NOT_RUN",
            ["schema_version"] = "lab03-trace-v1",
            ["sequence"] = ++sequence,
            ["state_revision_after"] = state.Revision,
            ["state_revision_before"] = beforeRevision,
            ["step_index"] = stepIndex,
            ["steps_used"] = state.StepsUsed,
            ["success_contract_status"] = "NOT_RUN",
            ["termination_reason"] = "NOT_RUN",
            ["tool_calls_used"] = state.ToolCallsUsed,
            ["tool_executed"] = true,
            ["tool_name"] = decision.ToolName,
            ["tool_result_code"] = outcome.Code,
            ["tool_result_disposition"] = outcome.Disposition,
            ["tool_result_payload_sha256"] = outcome.PayloadSha256,
            ["tool_result_record_sha256"] = outcome.RecordSha256,
            ["turn_index"] = 1,
            ["unresolved_requirement_codes"] = state.UnresolvedRequirementCodes.ToArray(),
            ["unresolved_tool_failure_count"] = state.UnresolvedToolFailures.Count
        });
    }

    private static void ExecuteStop(
        DecisionConfig decision,
        AgentState state,
        List<SortedDictionary<string, object?>> traces,
        List<SortedDictionary<string, object?>> states,
        ref int sequence,
        int beforeRevision,
        string beforeFull,
        string beforeGoal)
    {
        int stepIndex = state.StepsUsed + 1;
        bool sortedUnique = decision.Output.EvidenceIds.SequenceEqual(
            decision.Output.EvidenceIds.Distinct(StringComparer.Ordinal).OrderBy(item => item, StringComparer.Ordinal));
        bool evidenceDomainPass = decision.Output.EvidenceIds.All(EvidenceDomain.Contains);
        bool outputPass = decision.Output.Status == "SUPPORTED"
            && !string.IsNullOrWhiteSpace(decision.Output.Summary)
            && sortedUnique
            && evidenceDomainPass;

        foreach (string rejected in decision.Output.EvidenceIds.Where(item => !EvidenceDomain.Contains(item)))
        {
            state.RejectedEvidenceIds.Add(rejected);
        }

        var producedEvidence = new HashSet<string>(state.AcceptedGoalEvidenceIds, StringComparer.Ordinal);
        producedEvidence.UnionWith(state.NonGoalEvidenceIds);
        bool provenancePass = decision.Output.EvidenceIds.All(producedEvidence.Contains);
        bool factsPass = state.Facts.TryGetValue("diagnostic.code", out object? code) && Equals(code, "CS0103")
            && state.Facts.TryGetValue("diagnostic.path", out object? path) && Equals(path, "BuildMenu.cs")
            && state.Facts.TryGetValue("diagnostic.line", out object? line) && Convert.ToInt32(line, System.Globalization.CultureInfo.InvariantCulture) == 3
            && state.Facts.TryGetValue("diagnostic.symbol", out object? symbol) && Equals(symbol, "missingIdentifier")
            && state.Facts.TryGetValue("source_match", out object? sourceMatch) && Equals(sourceMatch, true);
        bool exactGoalEvidence = state.AcceptedGoalEvidenceIds.SetEquals(["EV-FILE-001", "EV-LOG-001"]);
        bool successPass = decision.RequestedOutcome == "SUCCEEDED"
            && outputPass
            && provenancePass
            && factsPass
            && exactGoalEvidence
            && state.UnresolvedRequirementCodes.Count == 0
            && state.UnresolvedToolFailures.Count == 0;

        state.OutputContractStatus = outputPass ? "PASS" : "FAIL";
        state.SuccessContractStatus = successPass ? "PASS" : "FAIL";
        if (state.UnresolvedToolFailures.Count > 0)
        {
            state.TerminationReason = "UNRESOLVED_TOOL_FAILURE";
            state.Outcome = "FAILED";
        }
        else if (!successPass)
        {
            state.TerminationReason = "STOP_CONTRACT_FAILED";
            state.Outcome = "FAILED";
        }
        else
        {
            state.TerminationReason = "GOAL_SATISFIED";
            state.Outcome = "SUCCEEDED";
        }

        state.Lifecycle = "STOPPED";
        state.Revision++;
        state.StepsUsed++;
        state.HistoryLength++;
        state.LastObservationKind = "NOT_RUN";
        state.LastObservationSourceDigest = "NOT_RUN";
        state.ProgressStatus = "NOT_RUN";
        RefreshStateDigests(state);
        states.Add(StateRecord(state));

        traces.Add(new SortedDictionary<string, object?>(StringComparer.Ordinal)
        {
            ["action_fingerprint"] = "NOT_RUN",
            ["arguments_sha256"] = "NOT_RUN",
            ["case_id"] = state.CaseId,
            ["control_decision"] = "STOP",
            ["decision_calls_used"] = state.DecisionCallsUsed,
            ["decision_contract_status"] = "PASS",
            ["decision_id"] = decision.DecisionId,
            ["decision_kind"] = decision.Kind,
            ["decision_source"] = decision.DecisionSource,
            ["event_type"] = "STEP",
            ["full_state_sha256_after"] = state.FullStateSha256,
            ["full_state_sha256_before"] = beforeFull,
            ["goal_state_sha256_after"] = state.GoalStateSha256,
            ["goal_state_sha256_before"] = beforeGoal,
            ["invocation_id"] = "NOT_RUN",
            ["max_steps"] = state.MaxSteps,
            ["model_stop_requested"] = true,
            ["observation_kind"] = "NOT_RUN",
            ["observation_normalization_status"] = "NOT_RUN",
            ["observation_sha256"] = "NOT_RUN",
            ["observation_source_result_sha256"] = "NOT_RUN",
            ["output_contract_status"] = state.OutputContractStatus,
            ["progress_status"] = "NOT_RUN",
            ["remaining_steps"] = state.MaxSteps - state.StepsUsed,
            ["repeat_detected"] = "NOT_RUN",
            ["requested_outcome"] = decision.RequestedOutcome,
            ["run_id"] = state.RunId,
            ["run_outcome"] = state.Outcome,
            ["schema_version"] = "lab03-trace-v1",
            ["sequence"] = ++sequence,
            ["state_revision_after"] = state.Revision,
            ["state_revision_before"] = beforeRevision,
            ["step_index"] = stepIndex,
            ["steps_used"] = state.StepsUsed,
            ["success_contract_status"] = state.SuccessContractStatus,
            ["termination_reason"] = state.TerminationReason,
            ["tool_calls_used"] = state.ToolCallsUsed,
            ["tool_executed"] = "NOT_RUN",
            ["tool_name"] = "NOT_RUN",
            ["tool_result_code"] = "NOT_RUN",
            ["tool_result_disposition"] = "NOT_RUN",
            ["tool_result_payload_sha256"] = "NOT_RUN",
            ["tool_result_record_sha256"] = "NOT_RUN",
            ["turn_index"] = 1,
            ["unresolved_requirement_codes"] = state.UnresolvedRequirementCodes.ToArray(),
            ["unresolved_tool_failure_count"] = state.UnresolvedToolFailures.Count
        });
        traces.Add(TerminalTrace(++sequence, state, state.Revision, state.FullStateSha256, state.GoalStateSha256));
    }

    private static ToolOutcome ExecuteTool(CaseConfig caseConfig, DecisionConfig decision, string fixturesRoot, int stepIndex)
    {
        statePathArgument(decision, out string relativePath);
        SortedDictionary<string, object?> data;
        SortedDictionary<string, object?> error;
        string disposition;
        string code;

        if (caseConfig.NamedFaultId == "FI_PARSE_TYPED_FAILURE"
            && decision.InvocationId == caseConfig.FaultTargetInvocation)
        {
            disposition = "FAILED";
            code = "MOCK_PARSE_FAILED";
            data = new(StringComparer.Ordinal);
            error = new(StringComparer.Ordinal)
            {
                ["fault_id"] = "FI_PARSE_TYPED_FAILURE",
                ["message"] = "Deterministic typed parse failure injected at fixture-tool seam."
            };
        }
        else if (decision.ToolName == "parse_mock_log")
        {
            Require(relativePath == "build.log", "parse_mock_log path is not allowlisted");
            string path = ResolveFixture(fixturesRoot, relativePath);
            string content = File.ReadAllText(path, Canonical.Utf8NoBom).Replace("\r\n", "\n", StringComparison.Ordinal);
            const string expected = "BuildMenu.cs(3,5): error CS0103: The name 'missingIdentifier' does not exist in the current context\n";
            Require(content == expected, "build.log content differs from frozen fixture");
            disposition = "SUCCEEDED";
            code = "LOG_PARSED";
            error = new(StringComparer.Ordinal);
            data = new(StringComparer.Ordinal)
            {
                ["diagnostic"] = new SortedDictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["code"] = "CS0103",
                    ["column"] = 5,
                    ["line"] = 3,
                    ["path"] = "BuildMenu.cs",
                    ["symbol"] = "missingIdentifier"
                },
                ["semantic_payload_sha256"] = Canonical.Sha256("CS0103|BuildMenu.cs|3|5|missingIdentifier")
            };
        }
        else
        {
            Require(decision.ToolName == "read_mock_file", "unexpected fixture tool");
            Require(relativePath is "BuildMenu.cs" or "Unrelated.cs", "read_mock_file path is not allowlisted");
            string path = ResolveFixture(fixturesRoot, relativePath);
            string content = File.ReadAllText(path, Canonical.Utf8NoBom).Replace("\r\n", "\n", StringComparison.Ordinal);
            string[] lines = content.Split('\n');
            Require(lines.Length >= 3, "source fixture has fewer than three lines");
            disposition = "SUCCEEDED";
            code = "FILE_READ";
            error = new(StringComparer.Ordinal);
            data = new(StringComparer.Ordinal)
            {
                ["content_sha256"] = Canonical.Sha256(Canonical.Utf8NoBom.GetBytes(content)),
                ["relative_path"] = relativePath,
                ["requested_line_number"] = 3,
                ["requested_line_text"] = lines[2]
            };
        }

        var payload = new SortedDictionary<string, object?>(StringComparer.Ordinal)
        {
            ["code"] = code,
            ["data"] = data,
            ["disposition"] = disposition,
            ["error"] = error
        };
        string payloadSha256 = Canonical.Sha256(Canonical.Json(payload));
        var correlated = new SortedDictionary<string, object?>(payload, StringComparer.Ordinal)
        {
            ["case_id"] = caseConfig.CaseId,
            ["invocation_id"] = decision.InvocationId,
            ["schema_version"] = "lab03-tool-outcome-v1",
            ["step_index"] = stepIndex,
            ["tool_name"] = decision.ToolName
        };
        string recordSha256 = Canonical.Sha256(Canonical.Json(correlated));
        return new ToolOutcome(caseConfig.CaseId, stepIndex, decision.InvocationId, decision.ToolName,
            disposition, code, data, error, payloadSha256, recordSha256);
    }

    private static NormalizedObservation Normalize(ToolOutcome outcome)
    {
        string kind;
        string code;
        bool goalRelevant;
        string[] evidenceIds;
        SortedDictionary<string, object?> normalizedData;

        if (outcome.Disposition == "FAILED")
        {
            kind = "TOOL_FAILURE";
            code = outcome.Code;
            goalRelevant = true;
            evidenceIds = [];
            normalizedData = new(StringComparer.Ordinal)
            {
                ["error_code"] = outcome.Code,
                ["failure_type"] = "TYPED_TOOL_FAILURE"
            };
        }
        else if (outcome.Code == "LOG_PARSED")
        {
            kind = "LOG_PARSED";
            code = "LOG_PARSED";
            goalRelevant = true;
            evidenceIds = ["EV-LOG-001"];
            normalizedData = new(outcome.Data, StringComparer.Ordinal);
        }
        else
        {
            string relativePath = Convert.ToString(outcome.Data["relative_path"], System.Globalization.CultureInfo.InvariantCulture)
                ?? throw new InvalidDataException("read result path missing");
            kind = "SOURCE_READ";
            code = "SOURCE_READ";
            goalRelevant = relativePath == "BuildMenu.cs";
            evidenceIds = relativePath == "BuildMenu.cs" ? ["EV-FILE-001"] : ["EV-UNRELATED-001"];
            normalizedData = new(outcome.Data, StringComparer.Ordinal);
        }

        var record = new SortedDictionary<string, object?>(StringComparer.Ordinal)
        {
            ["code"] = code,
            ["evidence_ids"] = evidenceIds,
            ["goal_relevant"] = goalRelevant,
            ["kind"] = kind,
            ["normalization_status"] = "PASS",
            ["normalized_data_sha256"] = Canonical.Sha256(Canonical.Json(normalizedData)),
            ["observation_id"] = $"{outcome.CaseId}/step-{outcome.StepIndex:D2}",
            ["schema_version"] = "lab03-observation-v1",
            ["source"] = "TOOL_RUNTIME",
            ["source_invocation_id"] = outcome.InvocationId,
            ["source_result_payload_sha256"] = outcome.PayloadSha256,
            ["source_result_record_sha256"] = outcome.RecordSha256
        };
        string observationSha256 = Canonical.Sha256(Canonical.Json(record));
        return new NormalizedObservation(record, observationSha256, kind, outcome.RecordSha256, evidenceIds, goalRelevant);
    }

    private static void ApplyObservation(
        AgentState state,
        DecisionConfig decision,
        ToolOutcome outcome,
        NormalizedObservation observation,
        string actionFingerprint,
        bool repeat)
    {
        state.LastObservationKind = observation.Kind;
        state.LastObservationSourceDigest = observation.SourceResultRecordSha256;
        state.RepeatActionFingerprint = repeat ? actionFingerprint : "NOT_RUN";
        state.LastActionFingerprint = actionFingerprint;

        if (outcome.Disposition == "FAILED")
        {
            state.UnresolvedToolFailures.Add(new SortedDictionary<string, object?>(StringComparer.Ordinal)
            {
                ["code"] = outcome.Code,
                ["invocation_id"] = outcome.InvocationId,
                ["tool_name"] = outcome.ToolName,
                ["tool_result_record_sha256"] = outcome.RecordSha256
            });
            return;
        }

        if (observation.Kind == "LOG_PARSED")
        {
            var diagnostic = (SortedDictionary<string, object?>)outcome.Data["diagnostic"]!;
            state.Facts["diagnostic.code"] = diagnostic["code"];
            state.Facts["diagnostic.line"] = diagnostic["line"];
            state.Facts["diagnostic.path"] = diagnostic["path"];
            state.Facts["diagnostic.symbol"] = diagnostic["symbol"];
            state.AcceptedGoalEvidenceIds.Add("EV-LOG-001");
            state.UnresolvedRequirementCodes.Remove("REQ_LOG");
            return;
        }

        string relativePath = Convert.ToString(outcome.Data["relative_path"], System.Globalization.CultureInfo.InvariantCulture)
            ?? throw new InvalidDataException("source observation path missing");
        if (relativePath == "BuildMenu.cs"
            && state.Facts.TryGetValue("diagnostic.symbol", out object? symbol)
            && Convert.ToString(outcome.Data["requested_line_text"], System.Globalization.CultureInfo.InvariantCulture)!.Contains(
                Convert.ToString(symbol, System.Globalization.CultureInfo.InvariantCulture)!, StringComparison.Ordinal))
        {
            state.Facts["source_match"] = true;
            state.AcceptedGoalEvidenceIds.Add("EV-FILE-001");
            state.UnresolvedRequirementCodes.Remove("REQ_SOURCE");
        }
        else if (relativePath == "Unrelated.cs")
        {
            state.NonGoalEvidenceIds.Add("EV-UNRELATED-001");
        }
    }

    private static SortedDictionary<string, object?> ToolOutcomeRecord(ToolOutcome outcome)
    {
        return new SortedDictionary<string, object?>(StringComparer.Ordinal)
        {
            ["case_id"] = outcome.CaseId,
            ["code"] = outcome.Code,
            ["data"] = outcome.Data,
            ["disposition"] = outcome.Disposition,
            ["error"] = outcome.Error,
            ["invocation_id"] = outcome.InvocationId,
            ["schema_version"] = "lab03-tool-outcome-v1",
            ["step_index"] = outcome.StepIndex,
            ["tool_name"] = outcome.ToolName,
            ["tool_result_payload_sha256"] = outcome.PayloadSha256,
            ["tool_result_record_sha256"] = outcome.RecordSha256
        };
    }

    private static SortedDictionary<string, object?> TerminalTrace(
        int sequence,
        AgentState state,
        int beforeRevision,
        string beforeFull,
        string beforeGoal)
    {
        return new SortedDictionary<string, object?>(StringComparer.Ordinal)
        {
            ["action_fingerprint"] = "NOT_RUN",
            ["arguments_sha256"] = "NOT_RUN",
            ["case_id"] = state.CaseId,
            ["control_decision"] = "STOP",
            ["decision_calls_used"] = state.DecisionCallsUsed,
            ["decision_contract_status"] = "NOT_RUN",
            ["decision_id"] = "NOT_RUN",
            ["decision_kind"] = "NOT_RUN",
            ["decision_source"] = "NOT_RUN",
            ["event_type"] = "TERMINAL",
            ["full_state_sha256_after"] = state.FullStateSha256,
            ["full_state_sha256_before"] = beforeFull,
            ["goal_state_sha256_after"] = state.GoalStateSha256,
            ["goal_state_sha256_before"] = beforeGoal,
            ["invocation_id"] = "NOT_RUN",
            ["max_steps"] = state.MaxSteps,
            ["model_stop_requested"] = "NOT_RUN",
            ["observation_kind"] = "NOT_RUN",
            ["observation_normalization_status"] = "NOT_RUN",
            ["observation_sha256"] = "NOT_RUN",
            ["observation_source_result_sha256"] = "NOT_RUN",
            ["output_contract_status"] = state.OutputContractStatus,
            ["progress_status"] = "NOT_RUN",
            ["remaining_steps"] = state.MaxSteps - state.StepsUsed,
            ["repeat_detected"] = "NOT_RUN",
            ["requested_outcome"] = "NOT_RUN",
            ["run_id"] = state.RunId,
            ["run_outcome"] = state.Outcome,
            ["schema_version"] = "lab03-trace-v1",
            ["sequence"] = sequence,
            ["state_revision_after"] = state.Revision,
            ["state_revision_before"] = beforeRevision,
            ["step_index"] = state.StepsUsed,
            ["steps_used"] = state.StepsUsed,
            ["success_contract_status"] = state.SuccessContractStatus,
            ["termination_reason"] = state.TerminationReason,
            ["tool_calls_used"] = state.ToolCallsUsed,
            ["tool_executed"] = "NOT_RUN",
            ["tool_name"] = "NOT_RUN",
            ["tool_result_code"] = "NOT_RUN",
            ["tool_result_disposition"] = "NOT_RUN",
            ["tool_result_payload_sha256"] = "NOT_RUN",
            ["tool_result_record_sha256"] = "NOT_RUN",
            ["turn_index"] = 1,
            ["unresolved_requirement_codes"] = state.UnresolvedRequirementCodes.ToArray(),
            ["unresolved_tool_failure_count"] = state.UnresolvedToolFailures.Count
        };
    }

    private static void RefreshStateDigests(AgentState state)
    {
        state.GoalStateSha256 = Canonical.Sha256(Canonical.Json(GoalStateRecord(state)));
        state.FullStateSha256 = Canonical.Sha256(Canonical.Json(StateRecordWithoutDigests(state)));
    }

    private static SortedDictionary<string, object?> StateRecord(AgentState state)
    {
        SortedDictionary<string, object?> result = StateRecordWithoutDigests(state);
        result["full_state_sha256"] = state.FullStateSha256;
        result["goal_state_sha256"] = state.GoalStateSha256;
        return result;
    }

    private static SortedDictionary<string, object?> StateRecordWithoutDigests(AgentState state)
    {
        return new SortedDictionary<string, object?>(StringComparer.Ordinal)
        {
            ["accepted_goal_evidence_ids"] = state.AcceptedGoalEvidenceIds.ToArray(),
            ["case_id"] = state.CaseId,
            ["decision_calls_used"] = state.DecisionCallsUsed,
            ["goal_contract_version"] = "goal-contract-v1",
            ["history_length"] = state.HistoryLength,
            ["last_action_fingerprint"] = state.LastActionFingerprint,
            ["last_observation_kind"] = state.LastObservationKind,
            ["last_observation_source_digest"] = state.LastObservationSourceDigest,
            ["lifecycle"] = state.Lifecycle,
            ["max_steps"] = state.MaxSteps,
            ["non_goal_evidence_ids"] = state.NonGoalEvidenceIds.ToArray(),
            ["outcome"] = state.Outcome,
            ["output_contract_status"] = state.OutputContractStatus,
            ["progress_status"] = state.ProgressStatus,
            ["rejected_evidence_ids"] = state.RejectedEvidenceIds.ToArray(),
            ["remaining_steps"] = state.MaxSteps - state.StepsUsed,
            ["repeat_action_fingerprint"] = state.RepeatActionFingerprint,
            ["revision"] = state.Revision,
            ["run_id"] = state.RunId,
            ["schema_version"] = "lab03-state-v1",
            ["sorted_facts"] = new SortedDictionary<string, object?>(state.Facts, StringComparer.Ordinal),
            ["steps_used"] = state.StepsUsed,
            ["success_contract_status"] = state.SuccessContractStatus,
            ["termination_reason"] = state.TerminationReason,
            ["tool_calls_used"] = state.ToolCallsUsed,
            ["turn_index"] = 1,
            ["unresolved_requirement_codes"] = state.UnresolvedRequirementCodes.ToArray(),
            ["unresolved_tool_failures"] = state.UnresolvedToolFailures
                .Select(item => new SortedDictionary<string, object?>(item, StringComparer.Ordinal)).ToArray()
        };
    }

    private static SortedDictionary<string, object?> GoalStateRecord(AgentState state)
    {
        return new SortedDictionary<string, object?>(StringComparer.Ordinal)
        {
            ["accepted_goal_evidence_ids"] = state.AcceptedGoalEvidenceIds.ToArray(),
            ["sorted_facts"] = new SortedDictionary<string, object?>(state.Facts, StringComparer.Ordinal),
            ["unresolved_requirement_codes"] = state.UnresolvedRequirementCodes.ToArray(),
            ["unresolved_tool_failures"] = state.UnresolvedToolFailures
                .Select(item => new SortedDictionary<string, object?>(item, StringComparer.Ordinal)).ToArray()
        };
    }

    private static string ResolveFixture(string fixturesRoot, string relativePath)
    {
        Require(!Path.IsPathRooted(relativePath), "fixture path must be relative");
        Require(relativePath is "build.log" or "BuildMenu.cs" or "Unrelated.cs", "fixture path is not allowlisted");
        string fullPath = Path.GetFullPath(Path.Combine(fixturesRoot, relativePath));
        RequireContained(fixturesRoot, fullPath, "fixture path");
        Require(File.Exists(fullPath), $"fixture missing: {relativePath}");
        return fullPath;
    }

    private static void RequireContained(string root, string candidate, string label)
    {
        string fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        string fullCandidate = Path.GetFullPath(candidate);
        Require(fullCandidate.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase), $"{label} escaped allowed root");
    }

    private static void ValidateAntiSelfFulfilling(JsonElement root)
    {
        var forbidden = new HashSet<string>(
            ["assertion_result", "expected_counts", "expected_digests", "expected_evidence_mapping",
             "expected_outcome", "expected_run_outcome", "expected_success", "expected_termination_reason", "success_bool"],
            StringComparer.Ordinal);
        Walk(root);
        return;

        void Walk(JsonElement node)
        {
            if (node.ValueKind == JsonValueKind.Object)
            {
                foreach (JsonProperty property in node.EnumerateObject())
                {
                    Require(!forbidden.Contains(property.Name), $"cases.json contains forbidden self-fulfilling field: {property.Name}");
                    Require(!property.Name.StartsWith("expected_", StringComparison.Ordinal),
                        $"cases.json contains forbidden expected field: {property.Name}");
                    Walk(property.Value);
                }
            }
            else if (node.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement item in node.EnumerateArray()) Walk(item);
            }
        }
    }

    private static void statePathArgument(DecisionConfig decision, out string relativePath)
    {
        Require(decision.Arguments.TryGetProperty("path", out JsonElement pathElement), $"{decision.DecisionId}: path argument missing");
        relativePath = pathElement.GetString() ?? throw new InvalidDataException($"{decision.DecisionId}: path argument null");
        Require(decision.Arguments.EnumerateObject().Count() == 1, $"{decision.DecisionId}: unexpected arguments");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidDataException(message);
    }
}
