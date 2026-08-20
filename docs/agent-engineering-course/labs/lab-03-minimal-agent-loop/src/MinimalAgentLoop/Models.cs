using System.Text.Json;
using System.Text.Json.Serialization;

namespace MinimalAgentLoop;

public sealed class CaseSuite
{
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; init; } = string.Empty;

    [JsonPropertyName("cases")]
    public List<CaseConfig> Cases { get; init; } = [];
}

public sealed class CaseConfig
{
    [JsonPropertyName("case_id")]
    public string CaseId { get; init; } = string.Empty;

    [JsonPropertyName("goal_contract_id")]
    public string GoalContractId { get; init; } = string.Empty;

    [JsonPropertyName("max_steps")]
    public int MaxSteps { get; init; }

    [JsonPropertyName("named_fault_id")]
    public string NamedFaultId { get; init; } = string.Empty;

    [JsonPropertyName("fault_target_invocation")]
    public string FaultTargetInvocation { get; init; } = string.Empty;

    [JsonPropertyName("fixture_relative_paths")]
    public List<string> FixtureRelativePaths { get; init; } = [];

    [JsonPropertyName("decisions")]
    public List<DecisionConfig> Decisions { get; init; } = [];
}

public sealed class DecisionConfig
{
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; init; } = string.Empty;

    [JsonPropertyName("decision_id")]
    public string DecisionId { get; init; } = string.Empty;

    [JsonPropertyName("decision_source")]
    public string DecisionSource { get; init; } = string.Empty;

    [JsonPropertyName("kind")]
    public string Kind { get; init; } = string.Empty;

    [JsonPropertyName("invocation_id")]
    public string InvocationId { get; init; } = string.Empty;

    [JsonPropertyName("tool_name")]
    public string ToolName { get; init; } = string.Empty;

    [JsonPropertyName("arguments")]
    public JsonElement Arguments { get; init; }

    [JsonPropertyName("requested_outcome")]
    public string RequestedOutcome { get; init; } = string.Empty;

    [JsonPropertyName("output")]
    public OutputConfig Output { get; init; } = new();
}

public sealed class OutputConfig
{
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("summary")]
    public string Summary { get; init; } = string.Empty;

    [JsonPropertyName("evidence_ids")]
    public List<string> EvidenceIds { get; init; } = [];
}

internal sealed class AgentState
{
    public required string CaseId { get; init; }
    public required string RunId { get; init; }
    public int MaxSteps { get; init; }
    public int Revision { get; set; }
    public string Lifecycle { get; set; } = "RUNNING";
    public string Outcome { get; set; } = "NOT_RUN";
    public string TerminationReason { get; set; } = "NOT_RUN";
    public SortedDictionary<string, object?> Facts { get; } = new(StringComparer.Ordinal);
    public SortedSet<string> AcceptedGoalEvidenceIds { get; } = new(StringComparer.Ordinal);
    public SortedSet<string> NonGoalEvidenceIds { get; } = new(StringComparer.Ordinal);
    public SortedSet<string> RejectedEvidenceIds { get; } = new(StringComparer.Ordinal);
    public SortedSet<string> UnresolvedRequirementCodes { get; } = new(["REQ_LOG", "REQ_SOURCE"], StringComparer.Ordinal);
    public List<SortedDictionary<string, object?>> UnresolvedToolFailures { get; } = [];
    public int HistoryLength { get; set; }
    public string LastObservationKind { get; set; } = "NOT_RUN";
    public string LastObservationSourceDigest { get; set; } = "NOT_RUN";
    public string LastActionFingerprint { get; set; } = "NOT_RUN";
    public string RepeatActionFingerprint { get; set; } = "NOT_RUN";
    public string ProgressStatus { get; set; } = "NOT_RUN";
    public int StepsUsed { get; set; }
    public int DecisionCallsUsed { get; set; }
    public int ToolCallsUsed { get; set; }
    public string OutputContractStatus { get; set; } = "NOT_RUN";
    public string SuccessContractStatus { get; set; } = "NOT_RUN";
    public string FullStateSha256 { get; set; } = string.Empty;
    public string GoalStateSha256 { get; set; } = string.Empty;
}

internal sealed record ToolOutcome(
    string CaseId,
    int StepIndex,
    string InvocationId,
    string ToolName,
    string Disposition,
    string Code,
    SortedDictionary<string, object?> Data,
    SortedDictionary<string, object?> Error,
    string PayloadSha256,
    string RecordSha256);

internal sealed record NormalizedObservation(
    SortedDictionary<string, object?> Record,
    string ObservationSha256,
    string Kind,
    string SourceResultRecordSha256,
    IReadOnlyList<string> EvidenceIds,
    bool GoalRelevant);

public sealed record SuiteExecutionSummary(
    int CaseCount,
    int StepCount,
    int TerminalCount,
    int StateSnapshotCount,
    int ToolCallCount,
    int DecisionCallCount,
    int SucceededCount,
    IReadOnlyDictionary<string, string> ArtifactSha256);
