using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using StructuredOutputValidation;
using Xunit;

namespace StructuredOutputValidation.Tests;

public sealed class ValidationPipelineTests
{
    private static readonly string FixtureRoot = AppContext.BaseDirectory;

    [Fact]
    public void SchemaAndDtoContractsAreInParity()
    {
        using var schema = JsonDocument.Parse(File.ReadAllText(PathFor("schema", "diagnosis-candidate.schema.json")));
        var schemaProperties = schema.RootElement.GetProperty("properties")
            .EnumerateObject()
            .Select(property => property.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var schemaRequired = schema.RootElement.GetProperty("required")
            .EnumerateArray()
            .Select(item => item.GetString()!)
            .Order(StringComparer.Ordinal)
            .ToArray();

        var dtoProperties = typeof(DiagnosisCandidate)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(property => new
            {
                Property = property,
                JsonName = property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
            })
            .ToArray();
        var dtoJsonNames = dtoProperties
            .Select(item => item.JsonName)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(schemaProperties, dtoJsonNames);
        Assert.Equal(schemaRequired, dtoJsonNames);
        Assert.All(dtoProperties, item => Assert.NotNull(item.JsonName));
        Assert.All(
            dtoProperties,
            item => Assert.Equal(
                NullabilityState.NotNull,
                new NullabilityInfoContext().Create(item.Property).ReadState));
        Assert.Equal(typeof(IReadOnlyList<string>), typeof(DiagnosisCandidate).GetProperty(nameof(DiagnosisCandidate.EvidenceIds))!.PropertyType);
    }

    [Fact]
    public async Task DomainRulesMatchFrozenContract()
    {
        var pipeline = await CreatePipelineAsync();

        Assert.Equal("ACCEPTED", pipeline.Evaluate(Case("supported-one", "{\"status\":\"SUPPORTED\",\"summary\":\"ok\",\"evidence_ids\":[\"EV-001\"]}")).TerminalStage);
        Assert.Equal(["EVIDENCE_REQUIRED"], pipeline.Evaluate(Case("supported-empty", "{\"status\":\"SUPPORTED\",\"summary\":\"empty\",\"evidence_ids\":[]}")).ErrorCodes);
        Assert.Equal(["UNKNOWN_EVIDENCE_ID"], pipeline.Evaluate(Case("supported-unknown", "{\"status\":\"SUPPORTED\",\"summary\":\"unknown\",\"evidence_ids\":[\"EV-999\"]}")).ErrorCodes);
        Assert.Equal("ACCEPTED", pipeline.Evaluate(Case("insufficient-empty", "{\"status\":\"INSUFFICIENT_EVIDENCE\",\"summary\":\"none\",\"evidence_ids\":[]}")).TerminalStage);
        Assert.Equal(["EVIDENCE_MUST_BE_EMPTY"], pipeline.Evaluate(Case("insufficient-nonempty", "{\"status\":\"INSUFFICIENT_EVIDENCE\",\"summary\":\"bad\",\"evidence_ids\":[\"EV-001\"]}")).ErrorCodes);
    }

    [Fact]
    public async Task AllEightCasesMatchFrozenMatrix()
    {
        var pipeline = await CreatePipelineAsync();
        var cases = LoadCases();

        Assert.Equal(8, cases.Count);
        Assert.Equal(8, cases.Select(item => item.CaseId).Distinct(StringComparer.Ordinal).Count());

        foreach (var fixtureCase in cases)
        {
            var observation = pipeline.Evaluate(fixtureCase);
            Assert.Equal(fixtureCase.ExpectedTerminalStage, observation.TerminalStage);
            Assert.Equal(fixtureCase.ExpectedErrorCodes, observation.ErrorCodes);
            Assert.Equal(fixtureCase.ExpectedAction, observation.RecommendedAction);
        }

        Assert.Single(
            cases.Select(pipeline.Evaluate),
            item => item.TerminalStage == "ACCEPTED");
    }

    [Fact]
    public async Task FirstFailureShortCircuitsEveryLaterStage()
    {
        var pipeline = await CreatePipelineAsync();
        var observations = LoadCases().Select(pipeline.Evaluate).ToDictionary(item => item.CaseId, StringComparer.Ordinal);

        Assert.All(
            new[] { "invalid-json", "truncated-json", "synthetic-refusal-text" },
            id => Assert.Equal(["FAIL", "NOT_RUN", "NOT_RUN", "NOT_RUN"], Statuses(observations[id])));
        Assert.All(
            new[] { "missing-required", "wrong-type", "extra-property" },
            id => Assert.Equal(["PASS", "FAIL", "NOT_RUN", "NOT_RUN"], Statuses(observations[id])));
        Assert.Equal(["PASS", "PASS", "PASS", "FAIL"], Statuses(observations["nonexistent-evidence"]));
        Assert.Equal(["PASS", "PASS", "PASS", "PASS"], Statuses(observations["valid-accepted"]));
    }

    [Fact]
    public void AutomaticRepairAttemptsIsFrozenAtZero()
    {
        Assert.Equal(0, ValidationPipeline.AutomaticRepairAttempts);
    }

    private static string[] Statuses(ObservationRecord observation) =>
        [observation.ParseStatus, observation.SchemaStatus, observation.DtoStatus, observation.DomainStatus];

    private static async Task<ValidationPipeline> CreatePipelineAsync()
    {
        var schema = await File.ReadAllTextAsync(PathFor("schema", "diagnosis-candidate.schema.json"));
        var allowlist = JsonSerializer.Deserialize<List<string>>(
                            await File.ReadAllTextAsync(PathFor("fixtures", "evidence-allowlist.json")),
                            JsonSerializerOptions.Strict)
                        ?? throw new InvalidDataException("Missing allowlist.");
        return await ValidationPipeline.CreateAsync(schema, new HashSet<string>(allowlist, StringComparer.Ordinal));
    }

    private static List<FixtureCase> LoadCases() =>
        JsonSerializer.Deserialize<List<FixtureCase>>(
            File.ReadAllText(PathFor("fixtures", "cases.json")),
            JsonSerializerOptions.Strict)
        ?? throw new InvalidDataException("Missing cases.");

    private static FixtureCase Case(string caseId, string raw) =>
        new(caseId, "CONTRACT_CANDIDATE", raw, string.Empty, [], string.Empty);

    private static string PathFor(params string[] parts) =>
        Path.Combine([FixtureRoot, .. parts]);
}
