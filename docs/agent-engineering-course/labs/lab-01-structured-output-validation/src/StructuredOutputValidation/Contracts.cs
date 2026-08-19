using System.Text.Json.Serialization;

namespace StructuredOutputValidation;

public sealed record FixtureCase(
    [property: JsonPropertyName("case_id")] string CaseId,
    [property: JsonPropertyName("declared_input_class")] string DeclaredInputClass,
    [property: JsonPropertyName("raw")] string Raw,
    [property: JsonPropertyName("expected_terminal_stage")] string ExpectedTerminalStage,
    [property: JsonPropertyName("expected_error_codes")] IReadOnlyList<string> ExpectedErrorCodes,
    [property: JsonPropertyName("expected_action")] string ExpectedAction);

public sealed record DiagnosisCandidate(
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("summary")] string Summary,
    [property: JsonPropertyName("evidence_ids")] IReadOnlyList<string> EvidenceIds);

public sealed record ObservationRecord(
    [property: JsonPropertyName("case_id")] string CaseId,
    [property: JsonPropertyName("declared_input_class")] string DeclaredInputClass,
    [property: JsonPropertyName("raw_sha256")] string RawSha256,
    [property: JsonPropertyName("parse_status")] string ParseStatus,
    [property: JsonPropertyName("schema_status")] string SchemaStatus,
    [property: JsonPropertyName("dto_status")] string DtoStatus,
    [property: JsonPropertyName("domain_status")] string DomainStatus,
    [property: JsonPropertyName("terminal_stage")] string TerminalStage,
    [property: JsonPropertyName("error_codes")] IReadOnlyList<string> ErrorCodes,
    [property: JsonPropertyName("recommended_action")] string RecommendedAction,
    [property: JsonPropertyName("automatic_repair_attempts")] int AutomaticRepairAttempts);
