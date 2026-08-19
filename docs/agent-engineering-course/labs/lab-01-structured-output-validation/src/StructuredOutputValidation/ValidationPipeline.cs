using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NJsonSchema;

namespace StructuredOutputValidation;

public sealed class ValidationPipeline
{
    public const int AutomaticRepairAttempts = 0;

    private const string Passed = "PASS";
    private const string Failed = "FAIL";
    private const string NotRun = "NOT_RUN";

    private readonly JsonSchema _schema;
    private readonly IReadOnlySet<string> _evidenceAllowlist;

    private ValidationPipeline(JsonSchema schema, IReadOnlySet<string> evidenceAllowlist)
    {
        _schema = schema;
        _evidenceAllowlist = evidenceAllowlist;
    }

    public static async Task<ValidationPipeline> CreateAsync(
        string schemaJson,
        IReadOnlySet<string> evidenceAllowlist)
    {
        var schema = await JsonSchema.FromJsonAsync(schemaJson).ConfigureAwait(false);
        return new ValidationPipeline(schema, evidenceAllowlist);
    }

    public ObservationRecord Evaluate(FixtureCase input)
    {
        var rawHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input.Raw)))
            .ToLowerInvariant();

        try
        {
            using var _ = JsonDocument.Parse(
                input.Raw,
                new JsonDocumentOptions { AllowDuplicateProperties = false });
        }
        catch (JsonException)
        {
            return Create(
                input,
                rawHash,
                Failed,
                NotRun,
                NotRun,
                NotRun,
                "PARSE_FAILED",
                ["INVALID_JSON"],
                ParseFailureAction(input.DeclaredInputClass));
        }

        var schemaErrors = _schema.Validate(input.Raw);
        if (schemaErrors.Count > 0)
        {
            var errorCodes = schemaErrors
                .Select(error => MapSchemaError(error.Kind.ToString()))
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToArray();

            return Create(
                input,
                rawHash,
                Passed,
                Failed,
                NotRun,
                NotRun,
                "SCHEMA_FAILED",
                errorCodes,
                "UPSTREAM_RETRY_ELIGIBLE");
        }

        DiagnosisCandidate? candidate;
        try
        {
            candidate = JsonSerializer.Deserialize<DiagnosisCandidate>(
                input.Raw,
                JsonSerializerOptions.Strict);

            if (candidate is null)
            {
                throw new JsonException("Strict DTO materialization produced null.");
            }
        }
        catch (JsonException)
        {
            return Create(
                input,
                rawHash,
                Passed,
                Passed,
                Failed,
                NotRun,
                "DTO_FAILED",
                ["DTO_MATERIALIZATION_FAILED"],
                "STOP_CONTRACT_MISMATCH");
        }

        var domainErrors = ValidateDomain(candidate);
        if (domainErrors.Count > 0)
        {
            return Create(
                input,
                rawHash,
                Passed,
                Passed,
                Passed,
                Failed,
                "DOMAIN_FAILED",
                domainErrors,
                "STOP_AND_RECHECK_DOMAIN_INPUT");
        }

        return Create(
            input,
            rawHash,
            Passed,
            Passed,
            Passed,
            Passed,
            "ACCEPTED",
            ["NONE"],
            "ACCEPT");
    }

    private IReadOnlyList<string> ValidateDomain(DiagnosisCandidate candidate)
    {
        var errors = new SortedSet<string>(StringComparer.Ordinal);

        if (string.Equals(candidate.Status, "SUPPORTED", StringComparison.Ordinal))
        {
            if (candidate.EvidenceIds.Count == 0)
            {
                errors.Add("EVIDENCE_REQUIRED");
            }

            if (candidate.EvidenceIds.Any(id => !_evidenceAllowlist.Contains(id)))
            {
                errors.Add("UNKNOWN_EVIDENCE_ID");
            }
        }
        else if (string.Equals(candidate.Status, "INSUFFICIENT_EVIDENCE", StringComparison.Ordinal)
                 && candidate.EvidenceIds.Count != 0)
        {
            errors.Add("EVIDENCE_MUST_BE_EMPTY");
        }

        return errors.ToArray();
    }

    private static string MapSchemaError(string kind) => kind switch
    {
        "PropertyRequired" => "REQUIRED",
        "NoAdditionalPropertiesAllowed" => "ADDITIONAL_PROPERTY",
        "StringExpected" or
        "ArrayExpected" or
        "IntegerExpected" or
        "NumberExpected" or
        "BooleanExpected" or
        "ObjectExpected" or
        "NullExpected" => "TYPE",
        "NotInEnumeration" => "ENUM",
        "StringTooShort" => "MIN_LENGTH",
        "ArrayTooShort" => "MIN_ITEMS",
        _ => $"SCHEMA_{ToStableUpperSnakeCase(kind)}"
    };

    private static string ToStableUpperSnakeCase(string value)
    {
        var result = new StringBuilder(value.Length + 8);
        for (var index = 0; index < value.Length; index++)
        {
            var current = value[index];
            if (index > 0 && char.IsUpper(current) && !char.IsUpper(value[index - 1]))
            {
                result.Append('_');
            }

            result.Append(char.ToUpperInvariant(current));
        }

        return result.ToString();
    }

    private static string ParseFailureAction(string declaredInputClass) => declaredInputClass switch
    {
        "SYNTHETIC_TRUNCATED" => "UPSTREAM_CAUSE_REQUIRED",
        "SYNTHETIC_NON_CONTRACT_INPUT" => "STOP_NON_CONTRACT_INPUT",
        _ => "UPSTREAM_RETRY_ELIGIBLE"
    };

    private static ObservationRecord Create(
        FixtureCase input,
        string rawHash,
        string parseStatus,
        string schemaStatus,
        string dtoStatus,
        string domainStatus,
        string terminalStage,
        IReadOnlyList<string> errorCodes,
        string action) => new(
            input.CaseId,
            input.DeclaredInputClass,
            rawHash,
            parseStatus,
            schemaStatus,
            dtoStatus,
            domainStatus,
            terminalStage,
            errorCodes,
            action,
            AutomaticRepairAttempts);
}
