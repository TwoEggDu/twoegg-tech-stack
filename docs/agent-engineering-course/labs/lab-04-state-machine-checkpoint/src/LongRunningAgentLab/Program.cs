using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

internal static class Program
{
    private static readonly UTF8Encoding Utf8NoBom = new(false);
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private static int Main(string[] args)
    {
        try
        {
            if (args.Length == 0 || args[0] != "phase") return 2;
            var casesPath = RequiredOption(args, "--cases");
            var caseId = RequiredOption(args, "--case");
            var phase = RequiredOption(args, "--phase");
            var caseRoot = RequiredOption(args, "--case-root");
            var config = LoadCase(casesPath, caseId);
            ValidateBoundary(casesPath, caseRoot, caseId, phase, config);
            Directory.CreateDirectory(caseRoot);
            return phase == "START" ? Start(config, caseRoot) : Resume(config, caseRoot);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"RUNTIME_FAILURE {exception.GetType().Name}: {exception.Message}");
            return 2;
        }
    }

    private static int Start(CaseConfig config, string caseRoot)
    {
        Require(!File.Exists(Path.Combine(caseRoot, "checkpoint.json")), "START requires a fresh case root");
        WriteStore(caseRoot, EmptyStore());
        AppendTrace(caseRoot, "START", "INTAKE_ACCEPTED", "INTAKE", config.ActionId, "NONE", 0, "ACCEPTED", "fixture:scripted_action_inputs.diagnostic");
        AppendTrace(caseRoot, "START", "EVIDENCE_ACTION_COMPLETED", "EVIDENCE_COLLECTED", "read-fixture-diagnostic", "NONE", 0, "COMMITTED", "trace:INTAKE_ACCEPTED");
        var checkpoint = NewCheckpoint(config);
        WriteCheckpoint(caseRoot, checkpoint, false);

        if (config.NamedFaultId == "FI_CANCEL_AFTER_SAFE_CHECKPOINT")
        {
            checkpoint["cancellation"] = Cancellation(true, true, "CALLER");
            checkpoint["continuation"] = Continuation("EVIDENCE_COLLECTED", "REGISTER_FINDING");
            var partial = Partial(
                Entries(("fixture-diagnostic", "checkpoint:completed_actions/read-fixture-diagnostic")),
                Entries(),
                Entries(("register-finding", "checkpoint:remaining_actions/register-finding"), ("verify-finding", "checkpoint:remaining_actions/verify-finding"), ("goal-satisfaction", "checkpoint:remaining_actions/goal-satisfaction")),
                "REGISTER_FINDING");
            checkpoint["partial_result"] = partial.DeepClone();
            WriteCheckpoint(caseRoot, checkpoint, false);
            AppendTrace(caseRoot, "START", "CALLER_CANCELLATION_OBSERVED", "CANCELLED_INCOMPLETE", config.ActionId, "CALLER", 0, "STOP_BEFORE_SIDE_EFFECT", "checkpoint:EVIDENCE_COLLECTED");
            WriteTerminalArtifacts(config, caseRoot, "START", "CANCELLED", "INCOMPLETE", partial, checkpoint, false, 0);
            return 10;
        }

        if (config.NamedFaultId == "FI_TIMEOUT_BEFORE_APPLY")
        {
            checkpoint["cancellation"] = Cancellation(true, true, "TIMEOUT");
            checkpoint["continuation"] = Continuation("EVIDENCE_COLLECTED", "ASK_OR_STOP");
            var partial = Partial(
                Entries(("fixture-diagnostic", "checkpoint:completed_actions/read-fixture-diagnostic")),
                Entries(),
                Entries(("register-finding", "checkpoint:remaining_actions/register-finding"), ("verify-finding", "checkpoint:remaining_actions/verify-finding"), ("goal-satisfaction", "checkpoint:remaining_actions/goal-satisfaction")),
                "ASK_OR_STOP");
            checkpoint["partial_result"] = partial.DeepClone();
            WriteCheckpoint(caseRoot, checkpoint, false);
            AppendTrace(caseRoot, "START", "TIMEOUT_SIGNAL_OBSERVED", "TIMED_OUT", config.ActionId, "TIMEOUT", 0, "STOP_BEFORE_SIDE_EFFECT", "fixture:named_fault_id/FI_TIMEOUT_BEFORE_APPLY");
            WriteTerminalArtifacts(config, caseRoot, "START", "TIMED_OUT", "INCOMPLETE", partial, checkpoint, false, 0);
            return 0;
        }

        return ExecuteRegistration(config, caseRoot, "START", checkpoint);
    }

    private static int Resume(CaseConfig config, string caseRoot)
    {
        var checkpointPath = Path.Combine(caseRoot, "checkpoint.json");
        Require(File.Exists(checkpointPath), "RESUME checkpoint is missing");
        var checkpoint = ReadObject(checkpointPath);
        Require(ValidateIntegrity(checkpoint), "checkpoint integrity mismatch");
        var state = String(checkpoint, "state");
        if (state == "REGISTERING_FINDING" && checkpoint["in_flight_action"] is null)
        {
            var actionId = ActionFromTrace(caseRoot);
            AppendTrace(caseRoot, "RESUME", "RECOVERY_VALIDATION_REFUSED", "RECOVERY_REFUSED", actionId, "NONE", Int(checkpoint["retry_budget"]!.AsObject(), "attempts_used"), "IN_FLIGHT_ACTION_MISSING", "checkpoint:state-invariant");
            var partial = Partial(
                Entries(("fixture-diagnostic", "checkpoint:completed_actions/read-fixture-diagnostic")),
                Entries((actionId, "trace:REGISTER_ACTION_STARTED")),
                Entries(("register-finding", "checkpoint:state/REGISTERING_FINDING"), ("verify-finding", "checkpoint:remaining_actions/verify-finding"), ("goal-satisfaction", "checkpoint:remaining_actions/goal-satisfaction")),
                "NONE");
            checkpoint["continuation"] = Continuation("RECOVERY_REFUSED", "NONE");
            checkpoint["partial_result"] = partial.DeepClone();
            WriteCheckpoint(caseRoot, checkpoint, true);
            WriteTerminalArtifacts(config, caseRoot, "RESUME", "RECOVERY_REFUSED", "IN_FLIGHT_ACTION_MISSING", partial, checkpoint, true, 1);
            return 12;
        }
        Require(state is "EVIDENCE_COLLECTED" or "REGISTERING_FINDING", $"state {state} is not resumable");
        AppendTrace(caseRoot, "RESUME", "CHECKPOINT_LOADED", state, config.ActionId, "NONE", Int(checkpoint["retry_budget"]!.AsObject(), "attempts_used"), "VALIDATED", "checkpoint:integrity");
        checkpoint["cancellation"] = Cancellation(false, false, "NONE");
        return ExecuteRegistration(config, caseRoot, "RESUME", checkpoint);
    }

    private static int ExecuteRegistration(CaseConfig config, string caseRoot, string phase, JsonObject checkpoint)
    {
        var budget = checkpoint["retry_budget"]!.AsObject();
        var attempt = Int(budget, "attempts_used") + 1;
        while (attempt <= config.MaxAttempts)
        {
            checkpoint["state"] = "REGISTERING_FINDING";
            checkpoint["state_revision"] = Int(checkpoint, "state_revision") + 1;
            budget["attempts_used"] = attempt;
            budget["remaining"] = config.MaxAttempts - attempt;
            checkpoint["in_flight_action"] = InFlight(config, attempt, "STARTED");
            checkpoint["continuation"] = Continuation("REGISTERING_FINDING", "RECONCILE_OR_RETRY");
            WriteCheckpoint(caseRoot, checkpoint, false);
            AppendTrace(caseRoot, phase, "REGISTER_ACTION_STARTED", "REGISTERING_FINDING", config.ActionId, "NONE", attempt, "PRE_CALL_CHECKPOINT_FLUSHED", "checkpoint:in_flight_action");

            var transient = config.NamedFaultId == "FI_TRANSIENT_BEFORE_APPLY_ALWAYS" || (config.NamedFaultId == "FI_TRANSIENT_BEFORE_APPLY_ONCE" && attempt == 1);
            if (transient)
            {
                RecordStoreAccessWithoutEffect(caseRoot);
                checkpoint["last_failure"] = new JsonObject { ["class"] = "TRANSIENT", ["code"] = config.RetryableFaultCode, ["retryable"] = true };
                AppendTrace(caseRoot, phase, "STORE_REJECTED_BEFORE_APPLY", "REGISTERING_FINDING", config.ActionId, "NONE", attempt, config.RetryableFaultCode, "fake-store:access_count");
                if (attempt < config.MaxAttempts)
                {
                    AppendTrace(caseRoot, phase, "RETRY_APPROVED", "REGISTERING_FINDING", config.ActionId, "NONE", attempt, "WITHIN_BUDGET", "checkpoint:retry_budget");
                    attempt++;
                    continue;
                }

                checkpoint["in_flight_action"] = null;
                checkpoint["state"] = "RETRY_BUDGET_EXHAUSTED";
                checkpoint["continuation"] = Continuation("NONE", "ASK_OR_STOP");
                var exhausted = Partial(
                    Entries(("fixture-diagnostic", "checkpoint:completed_actions/read-fixture-diagnostic")),
                    Entries(),
                    Entries(("register-finding", "checkpoint:last_failure/TRANSIENT_STORE_UNAVAILABLE"), ("verify-finding", "checkpoint:remaining_actions/verify-finding"), ("goal-satisfaction", "checkpoint:remaining_actions/goal-satisfaction")),
                    "ASK_OR_STOP");
                checkpoint["partial_result"] = exhausted.DeepClone();
                WriteCheckpoint(caseRoot, checkpoint, false);
                AppendTrace(caseRoot, phase, "RETRY_BUDGET_EXHAUSTED", "RETRY_BUDGET_EXHAUSTED", config.ActionId, "NONE", attempt, "STOP", "checkpoint:retry_budget");
                WriteTerminalArtifacts(config, caseRoot, phase, "RETRY_BUDGET_EXHAUSTED", "INCOMPLETE", exhausted, checkpoint, false, phase == "RESUME" ? 1 : 0);
                return 13;
            }

            var disposition = ApplyStore(config, caseRoot, phase, attempt);
            var loseResponse = phase == "START" && config.NamedFaultId is "FI_APPLY_THEN_LOSE_RESPONSE" or "FI_UNSAFE_BLIND_REDELIVERY" or "FI_OMIT_IN_FLIGHT_ACTION";
            if (loseResponse)
            {
                AppendTrace(caseRoot, phase, "STORE_APPLIED_RESPONSE_LOST", "REGISTERING_FINDING", config.ActionId, "NONE", attempt, "RESULT_UNKNOWN", "fake-store:durable-record");
                if (config.CheckpointCompleteness == "OMIT_IN_FLIGHT_ACTION") checkpoint["in_flight_action"] = null;
                else checkpoint["in_flight_action"] = InFlight(config, attempt, "RESULT_UNKNOWN");
                checkpoint["continuation"] = Continuation("REGISTERING_FINDING", "RECONCILE_SAME_IDENTITY");
                var unknown = Partial(
                    Entries(("fixture-diagnostic", "checkpoint:completed_actions/read-fixture-diagnostic")),
                    Entries((config.ActionId, "trace:STORE_APPLIED_RESPONSE_LOST")),
                    Entries(("register-finding", "checkpoint:in_flight_action/RESULT_UNKNOWN"), ("verify-finding", "checkpoint:remaining_actions/verify-finding"), ("goal-satisfaction", "checkpoint:remaining_actions/goal-satisfaction")),
                    config.CheckpointCompleteness == "OMIT_IN_FLIGHT_ACTION" ? "VALIDATE_CHECKPOINT" : "RECONCILE_SAME_IDENTITY");
                checkpoint["partial_result"] = unknown.DeepClone();
                WriteCheckpoint(caseRoot, checkpoint, config.CheckpointCompleteness == "OMIT_IN_FLIGHT_ACTION");
                var reason = config.CheckpointCompleteness == "OMIT_IN_FLIGHT_ACTION" ? "INVALID_CHECKPOINT_CANDIDATE" : "UNKNOWN_SIDE_EFFECT";
                WriteTerminalArtifacts(config, caseRoot, phase, "INTERRUPTED", reason, unknown, checkpoint, config.CheckpointCompleteness == "OMIT_IN_FLIGHT_ACTION", 0);
                return 11;
            }

            if (config.EffectMode == "UNSAFE_APPEND_COMPARATOR" && phase == "RESUME")
            {
                var store = ReadStore(caseRoot);
                if (store["records"]!.AsArray().Count > 1)
                {
                    AppendTrace(caseRoot, phase, "DUPLICATE_DETECTED_FROM_STORE", "DUPLICATE_SIDE_EFFECT_DETECTED", config.ActionId, "NONE", attempt, "FAILED", "fake-store:records");
                    checkpoint["state"] = "DUPLICATE_SIDE_EFFECT_DETECTED";
                    checkpoint["in_flight_action"] = null;
                    checkpoint["continuation"] = Continuation("NONE", "NONE");
                    var duplicate = Partial(
                        Entries(("fixture-diagnostic", "checkpoint:completed_actions/read-fixture-diagnostic"), ("duplicate-effect-1", "fake-store:records/0"), ("duplicate-effect-2", "fake-store:records/1")),
                        Entries(),
                        Entries(("single-finding-registration", "fake-store:records/count-2"), ("verify-finding", "checkpoint:remaining_actions/verify-finding"), ("goal-satisfaction", "checkpoint:remaining_actions/goal-satisfaction")),
                        "NONE");
                    checkpoint["partial_result"] = duplicate.DeepClone();
                    WriteCheckpoint(caseRoot, checkpoint, false);
                    WriteTerminalArtifacts(config, caseRoot, phase, "DUPLICATE_SIDE_EFFECT_DETECTED", "FAILED", duplicate, checkpoint, false, 1);
                    return 14;
                }
            }

            AppendTrace(caseRoot, phase, disposition, "FINDING_REGISTERED", config.ActionId, "NONE", attempt, "COMMITTED", "fake-store:records");
            checkpoint["completed_actions"]!.AsArray().Add(new JsonObject
            {
                ["action_id"] = config.ActionId,
                ["intent_digest"] = IntentDigest(config),
                ["result_ref"] = "fake-store:effect-001",
                ["evidence_refs"] = new JsonArray("fake-store-view.json")
            });
            checkpoint["remaining_actions"] = new JsonArray("verify-finding", "goal-satisfaction");
            checkpoint["in_flight_action"] = null;
            checkpoint["state"] = "FINDING_REGISTERED";
            checkpoint["last_failure"] = null;
            checkpoint["state_revision"] = Int(checkpoint, "state_revision") + 1;
            AppendTrace(caseRoot, phase, "FINDING_IDENTITY_VERIFIED", "VERIFIED", config.ActionId, "NONE", attempt, "MATCH", "fake-store:effect-001");
            checkpoint["state"] = "VERIFIED";
            checkpoint["remaining_actions"] = new JsonArray("goal-satisfaction");
            AppendTrace(caseRoot, phase, "GOAL_CONTRACT_SATISFIED", "SUCCEEDED", config.ActionId, "NONE", attempt, "SUPPORTED_COMPLETION", "trace:FINDING_IDENTITY_VERIFIED");
            checkpoint["state"] = "SUCCEEDED";
            checkpoint["remaining_actions"] = new JsonArray();
            checkpoint["continuation"] = Continuation("NONE", "NONE");
            var success = Partial(
                Entries(("fixture-diagnostic", "checkpoint:completed_actions/read-fixture-diagnostic"), ("finding-effect-001", "checkpoint:completed_actions/register-finding"), ("finding-identity-verified", "trace:FINDING_IDENTITY_VERIFIED"), ("goal-satisfaction", "trace:GOAL_CONTRACT_SATISFIED")),
                Entries(), Entries(), "NONE");
            checkpoint["partial_result"] = success.DeepClone();
            WriteCheckpoint(caseRoot, checkpoint, false);
            WriteTerminalArtifacts(config, caseRoot, phase, "SUCCEEDED", "GOAL_SATISFIED", success, checkpoint, false, phase == "RESUME" ? 1 : 0);
            return 0;
        }
        throw new InvalidOperationException("registration loop ended without terminal");
    }

    private static string ApplyStore(CaseConfig config, string caseRoot, string phase, int attempt)
    {
        var store = ReadStore(caseRoot);
        store["access_count"] = Int(store, "access_count") + 1;
        var records = store["records"]!.AsArray();
        if (config.EffectMode == "CONTROLLED_CREATE_OR_GET")
        {
            var existing = records.FirstOrDefault(node => String(node!.AsObject(), "idempotency_key") == config.IdempotencyKey);
            if (existing is not null)
            {
                WriteStore(caseRoot, store);
                return "CREATE_OR_GET_EXISTING";
            }
        }
        records.Add(new JsonObject
        {
            ["effect_id"] = $"effect-{records.Count + 1:000}",
            ["action_id"] = config.ActionId,
            ["intent_digest"] = IntentDigest(config),
            ["idempotency_key"] = config.IdempotencyKey,
            ["delivery_id"] = config.EffectMode == "CONTROLLED_CREATE_OR_GET" ? config.IdempotencyKey : $"delivery-{phase.ToLowerInvariant()}-{attempt}",
            ["payload"] = config.FindingPayload
        });
        WriteStore(caseRoot, store);
        return config.EffectMode == "CONTROLLED_CREATE_OR_GET" ? "CREATE_OR_GET_CREATED" : "UNSAFE_APPEND_CREATED";
    }

    private static void RecordStoreAccessWithoutEffect(string caseRoot)
    {
        var store = ReadStore(caseRoot);
        store["access_count"] = Int(store, "access_count") + 1;
        WriteStore(caseRoot, store);
    }

    private static void WriteTerminalArtifacts(CaseConfig config, string caseRoot, string phase, string terminalStatus, string terminalReason, JsonObject partial, JsonObject checkpoint, bool invalidCheckpoint, int resumeCount)
    {
        var store = ReadStore(caseRoot);
        var attempts = Int(checkpoint["retry_budget"]!.AsObject(), "attempts_used");
        var result = new JsonObject
        {
            ["schema_version"] = "case-result-v1",
            ["fixture_version"] = config.FixtureVersion,
            ["case_id"] = config.CaseId,
            ["run_id"] = config.RunId,
            ["process_phase"] = phase,
            ["terminal_status"] = terminalStatus,
            ["terminal_reason"] = terminalReason,
            ["cancellation_origin"] = String(checkpoint["cancellation"]!.AsObject(), "origin"),
            ["effect_count"] = store["records"]!.AsArray().Count,
            ["store_access_count"] = Int(store, "access_count"),
            ["attempts_used"] = attempts,
            ["retry_count"] = Math.Max(0, attempts - 1),
            ["resume_count"] = resumeCount,
            ["network_attempts"] = 0,
            ["provider_attempts"] = 0,
            ["credential_reads"] = 0
        };
        WriteJson(Path.Combine(caseRoot, "partial-result.json"), partial);
        WriteJson(Path.Combine(caseRoot, $"partial-result-{phase.ToLowerInvariant()}.json"), partial);
        WriteJson(Path.Combine(caseRoot, "case-result.json"), result);
        WriteJson(Path.Combine(caseRoot, $"phase-{phase.ToLowerInvariant()}-result.json"), result);
        WriteJson(Path.Combine(caseRoot, "fake-store-view.json"), store);
        WriteJson(Path.Combine(caseRoot, $"fake-store-view-{phase.ToLowerInvariant()}.json"), store);
        var checkpointSource = Path.Combine(caseRoot, invalidCheckpoint ? "checkpoint-invalid.json" : "checkpoint.json");
        File.Copy(checkpointSource, Path.Combine(caseRoot, $"checkpoint-{phase.ToLowerInvariant()}.json"), true);
        Console.WriteLine($"PHASE_RESULT case={config.CaseId} phase={phase} terminal={terminalStatus}/{terminalReason} effects={store["records"]!.AsArray().Count} attempts={attempts}");
    }

    private static JsonObject NewCheckpoint(CaseConfig config)
    {
        return new JsonObject
        {
            ["schema_version"] = "checkpoint-v1",
            ["fixture_version"] = config.FixtureVersion,
            ["run_id"] = config.RunId,
            ["case_id"] = config.CaseId,
            ["goal_contract_id"] = config.GoalContractId,
            ["state"] = "EVIDENCE_COLLECTED",
            ["state_revision"] = 1,
            ["last_committed_sequence"] = 2,
            ["completed_actions"] = new JsonArray(new JsonObject
            {
                ["action_id"] = "read-fixture-diagnostic",
                ["intent_digest"] = Sha256(config.Diagnostic),
                ["result_ref"] = "fixture-diagnostic",
                ["evidence_refs"] = new JsonArray("trace:EVIDENCE_ACTION_COMPLETED")
            }),
            ["remaining_actions"] = new JsonArray("register-finding", "verify-finding", "goal-satisfaction"),
            ["in_flight_action"] = null,
            ["retry_budget"] = new JsonObject { ["max_attempts"] = config.MaxAttempts, ["attempts_used"] = 0, ["remaining"] = config.MaxAttempts },
            ["last_failure"] = null,
            ["cancellation"] = Cancellation(false, false, "NONE"),
            ["continuation"] = Continuation("EVIDENCE_COLLECTED", "REGISTER_FINDING"),
            ["partial_result"] = Partial(Entries(("fixture-diagnostic", "trace:EVIDENCE_ACTION_COMPLETED")), Entries(), Entries(("register-finding", "checkpoint:remaining_actions"), ("verify-finding", "checkpoint:remaining_actions"), ("goal-satisfaction", "checkpoint:remaining_actions")), "REGISTER_FINDING")
        };
    }

    private static JsonObject InFlight(CaseConfig config, int attempt, string resultStatus) => new()
    {
        ["action_id"] = config.ActionId,
        ["intent_digest"] = IntentDigest(config),
        ["idempotency_key"] = config.IdempotencyKey,
        ["phase"] = "REGISTERING_FINDING",
        ["attempt"] = attempt,
        ["result_status"] = resultStatus
    };

    private static JsonObject Cancellation(bool requested, bool observed, string origin) => new() { ["requested"] = requested, ["observed"] = observed, ["origin"] = origin };
    private static JsonObject Continuation(string resumeState, string nextSafeAction) => new() { ["resume_state"] = resumeState, ["next_safe_action"] = nextSafeAction };
    private static JsonObject Partial(JsonArray known, JsonArray unknown, JsonArray unverified, string nextSafeAction) => new()
    {
        ["known_refs"] = known,
        ["unknown_actions"] = unknown,
        ["unverified_requirements"] = unverified,
        ["next_safe_action"] = nextSafeAction
    };

    private static JsonArray Entries(params (string Value, string Provenance)[] items)
    {
        var array = new JsonArray();
        foreach (var item in items) array.Add(new JsonObject { ["value"] = item.Value, ["provenance"] = item.Provenance });
        return array;
    }

    private static JsonObject EmptyStore() => new() { ["schema_version"] = "fake-store-v1", ["records"] = new JsonArray(), ["access_count"] = 0 };

    private static void WriteCheckpoint(string caseRoot, JsonObject checkpoint, bool alsoInvalid)
    {
        var clone = checkpoint.DeepClone().AsObject();
        clone.Remove("integrity");
        var digest = Sha256(Serialize(clone));
        checkpoint["integrity"] = new JsonObject { ["canonical_payload_sha256"] = digest };
        WriteJson(Path.Combine(caseRoot, "checkpoint.json"), checkpoint);
        if (alsoInvalid) WriteJson(Path.Combine(caseRoot, "checkpoint-invalid.json"), checkpoint);
    }

    private static bool ValidateIntegrity(JsonObject checkpoint)
    {
        var recorded = String(checkpoint["integrity"]?.AsObject() ?? new JsonObject(), "canonical_payload_sha256");
        var clone = checkpoint.DeepClone().AsObject();
        clone.Remove("integrity");
        return recorded == Sha256(Serialize(clone));
    }

    private static void AppendTrace(string caseRoot, string phase, string eventName, string state, string actionId, string origin, int attempt, string decision, string provenance)
    {
        var path = Path.Combine(caseRoot, "trace.jsonl");
        var sequence = File.Exists(path) ? File.ReadLines(path, Utf8NoBom).Count() + 1 : 1;
        var row = new JsonObject
        {
            ["sequence"] = sequence,
            ["process_phase"] = phase,
            ["event"] = eventName,
            ["state"] = state,
            ["action_id"] = actionId,
            ["origin"] = origin,
            ["attempt"] = attempt,
            ["decision"] = decision,
            ["provenance"] = provenance
        };
        File.AppendAllText(path, row.ToJsonString() + "\n", Utf8NoBom);
    }

    private static string ActionFromTrace(string caseRoot)
    {
        var lines = File.ReadAllLines(Path.Combine(caseRoot, "trace.jsonl"), Utf8NoBom);
        foreach (var line in lines.Reverse())
        {
            var row = JsonNode.Parse(line)!.AsObject();
            if (String(row, "event") == "REGISTER_ACTION_STARTED") return String(row, "action_id");
        }
        throw new InvalidOperationException("in-flight action cannot be recovered from trace");
    }

    private static void WriteStore(string caseRoot, JsonObject store) => WriteJsonDurable(Path.Combine(caseRoot, "fake-store.json"), store);
    private static JsonObject ReadStore(string caseRoot) => ReadObject(Path.Combine(caseRoot, "fake-store.json"));

    private static void WriteJson(string path, JsonNode node)
    {
        var text = Serialize(node);
        File.WriteAllText(path, text, Utf8NoBom);
    }

    private static void WriteJsonDurable(string path, JsonNode node)
    {
        var bytes = Utf8NoBom.GetBytes(Serialize(node));
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        stream.Write(bytes);
        stream.Flush(true);
    }

    private static string Serialize(JsonNode node) => node.ToJsonString(JsonOptions).Replace("\r\n", "\n") + "\n";
    private static JsonObject ReadObject(string path) => JsonNode.Parse(File.ReadAllText(path, Utf8NoBom))!.AsObject();
    private static string Sha256(string value) => Convert.ToHexString(SHA256.HashData(Utf8NoBom.GetBytes(value))).ToLowerInvariant();
    private static string IntentDigest(CaseConfig config) => Sha256(config.ActionId + "|" + config.FindingPayload);
    private static string String(JsonObject obj, string property) => obj[property]?.GetValue<string>() ?? string.Empty;
    private static int Int(JsonObject obj, string property) => obj[property]?.GetValue<int>() ?? 0;

    private static CaseConfig LoadCase(string casesPath, string caseId)
    {
        var root = ReadObject(casesPath);
        var item = root["cases"]!.AsArray().SingleOrDefault(node => String(node!.AsObject(), "case_id") == caseId) ?? throw new InvalidOperationException("case is not in fixture");
        var obj = item.AsObject();
        var inputs = obj["scripted_action_inputs"]!.AsObject();
        var retry = obj["retry_policy"]!.AsObject();
        return new CaseConfig(
            String(obj, "fixture_version"), String(obj, "case_id"), String(obj, "goal_contract_id"), String(obj, "run_id"),
            String(obj, "action_id"), String(obj, "idempotency_key"), String(inputs, "diagnostic"), String(inputs, "finding_payload"),
            String(obj, "named_fault_id"), String(obj, "fault_boundary"), String(obj, "fault_cardinality"), Int(retry, "max_attempts"),
            String(retry, "retryable_fault_code"), String(obj, "effect_mode"), String(obj, "cancellation_origin"), String(obj, "checkpoint_completeness"));
    }

    private static void ValidateBoundary(string casesPath, string caseRoot, string caseId, string phase, CaseConfig config)
    {
        Require(config.FixtureVersion == "lab04-fixture-v1", "fixture version mismatch");
        Require(config.CaseId == caseId, "case identity mismatch");
        Require(phase is "START" or "RESUME", "phase must be START or RESUME");
        var labRoot = Directory.GetParent(Path.GetDirectoryName(Path.GetFullPath(casesPath))!)!.FullName;
        var observations = Path.GetFullPath(Path.Combine(labRoot, "observations")) + Path.DirectorySeparatorChar;
        var fullCaseRoot = Path.GetFullPath(caseRoot);
        Require(fullCaseRoot.StartsWith(observations, StringComparison.OrdinalIgnoreCase), "case root is outside Lab observations");
        Require(Path.GetFileName(fullCaseRoot).Equals(caseId, StringComparison.Ordinal), "case root identity mismatch");
        Require(!Directory.Exists(fullCaseRoot) || (File.GetAttributes(fullCaseRoot) & FileAttributes.ReparsePoint) == 0, "case root cannot be a reparse point");
    }

    private static string RequiredOption(string[] args, string option)
    {
        var index = Array.IndexOf(args, option);
        Require(index >= 0 && index + 1 < args.Length, $"missing {option}");
        return args[index + 1];
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed record CaseConfig(
        string FixtureVersion,
        string CaseId,
        string GoalContractId,
        string RunId,
        string ActionId,
        string IdempotencyKey,
        string Diagnostic,
        string FindingPayload,
        string NamedFaultId,
        string FaultBoundary,
        string FaultCardinality,
        int MaxAttempts,
        string RetryableFaultCode,
        string EffectMode,
        string CancellationOrigin,
        string CheckpointCompleteness);
}
