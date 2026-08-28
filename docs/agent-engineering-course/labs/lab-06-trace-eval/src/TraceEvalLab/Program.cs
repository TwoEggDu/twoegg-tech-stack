using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TraceEvalLab;

internal static class Program
{
    private const int ExitFailClosed = 2;
    private const int ExitIncomparable = 3;
    private const int ExitInvalidInput = 4;

    private static readonly JsonSerializerOptions InputJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = false
    };

    private static readonly JsonSerializerOptions OutputJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    public static int Main(string[] args)
    {
        try
        {
            if (args.Length == 0 || !string.Equals(args[0], "evaluate", StringComparison.Ordinal))
            {
                throw new InputException("usage: evaluate --corpus <path> --policy <path> --candidate <path> [--baseline <path>] --output <directory>");
            }

            var options = ParseOptions(args[1..]);
            var corpus = Read<Corpus>(RequireOption(options, "--corpus"));
            var policy = Read<ScorerPolicy>(RequireOption(options, "--policy"));
            var candidate = Read<Candidate>(RequireOption(options, "--candidate"));
            Candidate? baseline = options.TryGetValue("--baseline", out var baselinePath) ? Read<Candidate>(baselinePath) : null;
            var outputDirectory = RequireOption(options, "--output");

            ValidateCorpus(corpus);
            ValidatePolicy(policy);
            ValidateCandidate(candidate, corpus, allowMissing: true);
            if (baseline is not null)
            {
                ValidateCandidate(baseline, corpus, allowMissing: false);
                var baselineMismatches = GetManifestMismatches(baseline, corpus, policy);
                if (baselineMismatches.Count != 0)
                {
                    throw new InputException($"baseline manifest is invalid: {string.Join(',', baselineMismatches)}");
                }
            }

            var result = Evaluate(corpus, policy, candidate, baseline);
            WriteResult(outputDirectory, result);
            Console.WriteLine($"RESULT candidate={result.CandidateId} verdict={result.RunVerdict} overall={result.OverallGate}");

            return result.RunVerdict switch
            {
                "INCOMPARABLE" => ExitIncomparable,
                _ when result.OverallGate == "PASS" => 0,
                _ => ExitFailClosed
            };
        }
        catch (InputException ex)
        {
            Console.Error.WriteLine($"INVALID_INPUT: {ex.Message}");
            return ExitInvalidInput;
        }
        catch (JsonException ex)
        {
            Console.Error.WriteLine($"INVALID_INPUT: JSON parse failed: {ex.Message}");
            return ExitInvalidInput;
        }
        catch (IOException ex)
        {
            Console.Error.WriteLine($"IO_FAILURE: {ex.Message}");
            return ExitInvalidInput;
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.Error.WriteLine($"IO_FAILURE: {ex.Message}");
            return ExitInvalidInput;
        }
    }

    private static EvalResult Evaluate(Corpus corpus, ScorerPolicy policy, Candidate candidate, Candidate? baseline)
    {
        var manifestMismatches = GetManifestMismatches(candidate, corpus, policy);
        var missingCaseIds = corpus.Cases.Select(item => item.CaseId)
            .Except(candidate.Cases.Select(item => item.CaseId), StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        var nonCaseMismatches = manifestMismatches.Where(item => item != "case_id_set").ToArray();
        if (nonCaseMismatches.Length != 0)
        {
            return new EvalResult
            {
                CandidateId = candidate.CandidateId,
                DatasetId = candidate.DatasetId,
                DatasetRevision = candidate.DatasetRevision,
                ScorerId = candidate.ScorerId,
                ScorerVersion = candidate.ScorerVersion,
                CandidateSchemaVersion = candidate.CandidateSchemaVersion,
                ManifestComparable = false,
                ManifestMismatches = manifestMismatches,
                RunVerdict = "INCOMPARABLE",
                MissingCaseCount = missingCaseIds.Length,
                UnknownCaseCount = missingCaseIds.Length,
                OverallGate = "FAIL",
                Cases = []
            };
        }

        var candidateById = candidate.Cases.ToDictionary(item => item.CaseId, StringComparer.Ordinal);
        var baselinePass = baseline is null
            ? null
            : ScoreCases(corpus, baseline.Cases.ToDictionary(item => item.CaseId, StringComparer.Ordinal));
        var candidatePass = ScoreCases(corpus, candidateById);
        var caseResults = new List<CaseResult>(corpus.Cases.Count);

        foreach (var oracleCase in corpus.Cases)
        {
            if (!candidateById.ContainsKey(oracleCase.CaseId))
            {
                caseResults.Add(new CaseResult
                {
                    CaseId = oracleCase.CaseId,
                    Criticality = oracleCase.Criticality,
                    CandidatePresent = false,
                    Pass = null,
                    ChangeVerdict = "UNKNOWN"
                });
                continue;
            }

            var pass = candidatePass[oracleCase.CaseId];
            var change = baselinePass is null
                ? "UNCHANGED"
                : ChangeVerdict(baselinePass[oracleCase.CaseId], pass);
            caseResults.Add(new CaseResult
            {
                CaseId = oracleCase.CaseId,
                Criticality = oracleCase.Criticality,
                CandidatePresent = true,
                Pass = pass,
                ChangeVerdict = change
            });
        }

        var totalCases = corpus.Cases.Count;
        var totalCritical = corpus.Cases.Count(item => item.Criticality == "CRITICAL");
        var passedCases = caseResults.Count(item => item.Pass == true);
        var passedCritical = caseResults.Count(item => item.Criticality == "CRITICAL" && item.Pass == true);
        var aggregateAccuracy = totalCases == 0 ? 0m : (decimal)passedCases / totalCases;
        var criticalAccuracy = totalCritical == 0 ? 0m : (decimal)passedCritical / totalCritical;
        var aggregateThreshold = ParseAggregateThreshold(policy);
        var aggregateThresholdPass = aggregateAccuracy >= aggregateThreshold;
        var criticalGatePass = criticalAccuracy == 1.0m;
        var missingCount = caseResults.Count(item => !item.CandidatePresent);
        var unknownCount = caseResults.Count(item => item.ChangeVerdict == "UNKNOWN");
        var comparable = manifestMismatches.Count == 0;
        var overallPass = aggregateThresholdPass && criticalGatePass && missingCount == 0 && unknownCount == 0 && comparable;
        var runVerdict = missingCount > 0
            ? "UNKNOWN"
            : baseline is null
                ? (overallPass ? "PASS" : "FAIL")
                : caseResults.Any(item => item.ChangeVerdict == "REGRESSION")
                    ? "REGRESSION"
                    : caseResults.Any(item => item.ChangeVerdict == "IMPROVEMENT")
                        ? "IMPROVEMENT"
                        : "UNCHANGED";

        return new EvalResult
        {
            CandidateId = candidate.CandidateId,
            DatasetId = candidate.DatasetId,
            DatasetRevision = candidate.DatasetRevision,
            ScorerId = candidate.ScorerId,
            ScorerVersion = candidate.ScorerVersion,
            CandidateSchemaVersion = candidate.CandidateSchemaVersion,
            ManifestComparable = comparable,
            ManifestMismatches = manifestMismatches,
            RunVerdict = runVerdict,
            TotalCaseCount = totalCases,
            PassedCaseCount = passedCases,
            TotalCriticalCount = totalCritical,
            PassedCriticalCount = passedCritical,
            AggregateAccuracy = aggregateAccuracy,
            CriticalAccuracy = criticalAccuracy,
            AggregateThresholdPass = aggregateThresholdPass,
            CriticalGatePass = criticalGatePass,
            MissingCaseCount = missingCount,
            UnknownCaseCount = unknownCount,
            OverallGate = overallPass ? "PASS" : "FAIL",
            Cases = caseResults
        };
    }

    private static Dictionary<string, bool> ScoreCases(Corpus corpus, Dictionary<string, CandidateCase> candidates)
    {
        var scores = new Dictionary<string, bool>(StringComparer.Ordinal);
        foreach (var oracleCase in corpus.Cases)
        {
            if (!candidates.TryGetValue(oracleCase.CaseId, out var candidate))
            {
                continue;
            }

            scores[oracleCase.CaseId] =
                string.Equals(candidate.Decision, oracleCase.Oracle.Decision, StringComparison.Ordinal) &&
                string.Equals(candidate.FailureLayer, oracleCase.Oracle.FailureLayer, StringComparison.Ordinal) &&
                candidate.ReasonCodes.ToHashSet(StringComparer.Ordinal).SetEquals(oracleCase.Oracle.ReasonCodes);
        }
        return scores;
    }

    private static string ChangeVerdict(bool baselinePass, bool candidatePass) => (baselinePass, candidatePass) switch
    {
        (true, false) => "REGRESSION",
        (false, true) => "IMPROVEMENT",
        _ => "UNCHANGED"
    };

    private static List<string> GetManifestMismatches(Candidate candidate, Corpus corpus, ScorerPolicy policy)
    {
        var mismatches = new List<string>();
        if (candidate.DatasetId != corpus.DatasetId) mismatches.Add("dataset_id");
        if (candidate.DatasetRevision != corpus.DatasetRevision) mismatches.Add("dataset_revision");
        if (candidate.ScorerId != policy.ScorerId) mismatches.Add("scorer_id");
        if (candidate.ScorerVersion != policy.ScorerVersion) mismatches.Add("scorer_version");
        if (candidate.CandidateSchemaVersion != "lab06-candidate-v1") mismatches.Add("candidate_schema_version");
        var expected = corpus.Cases.Select(item => item.CaseId).ToHashSet(StringComparer.Ordinal);
        var actual = candidate.Cases.Select(item => item.CaseId).ToHashSet(StringComparer.Ordinal);
        if (!expected.SetEquals(actual)) mismatches.Add("case_id_set");
        return mismatches;
    }

    private static decimal ParseAggregateThreshold(ScorerPolicy policy)
    {
        const string prefix = "aggregate_accuracy >= ";
        var expression = policy.OverallGate.AllRequired.SingleOrDefault(item => item.StartsWith(prefix, StringComparison.Ordinal));
        if (expression is null || !decimal.TryParse(expression[prefix.Length..], System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var threshold))
        {
            throw new InputException("policy must contain a parseable aggregate_accuracy threshold");
        }
        return threshold;
    }

    private static void ValidateCorpus(Corpus corpus)
    {
        if (corpus.SchemaVersion != "lab06-corpus-v1" || corpus.DatasetId.Length == 0 || corpus.DatasetRevision.Length == 0)
        {
            throw new InputException("corpus manifest is missing or unsupported");
        }
        if (corpus.Cases.Count == 0 || corpus.Cases.Select(item => item.CaseId).Distinct(StringComparer.Ordinal).Count() != corpus.Cases.Count)
        {
            throw new InputException("corpus case IDs must be non-empty and unique");
        }
    }

    private static void ValidatePolicy(ScorerPolicy policy)
    {
        if (policy.SchemaVersion != "lab06-scorer-policy-v1" || policy.ScorerId.Length == 0 || policy.ScorerVersion.Length == 0)
        {
            throw new InputException("scorer policy manifest is missing or unsupported");
        }
        _ = ParseAggregateThreshold(policy);
    }

    private static void ValidateCandidate(Candidate candidate, Corpus corpus, bool allowMissing)
    {
        if (candidate.CandidateId.Length == 0)
        {
            throw new InputException("candidate_id is required");
        }
        if (candidate.Cases.Select(item => item.CaseId).Distinct(StringComparer.Ordinal).Count() != candidate.Cases.Count)
        {
            throw new InputException("candidate case IDs must be unique");
        }
        var expected = corpus.Cases.Select(item => item.CaseId).ToHashSet(StringComparer.Ordinal);
        var unknown = candidate.Cases.Select(item => item.CaseId).Where(item => !expected.Contains(item)).ToArray();
        if (unknown.Length != 0)
        {
            throw new InputException($"candidate contains unknown case IDs: {string.Join(',', unknown)}");
        }
        if (!allowMissing && candidate.Cases.Count != corpus.Cases.Count)
        {
            throw new InputException("baseline must contain the complete corpus case set");
        }
        var validDecisions = new HashSet<string>(["PASS", "FAIL"], StringComparer.Ordinal);
        var validLayers = new HashSet<string>(["NONE", "POLICY", "STATE", "PROVIDER", "TOOL", "BUDGET", "RUNTIME", "EVIDENCE"], StringComparer.Ordinal);
        foreach (var item in candidate.Cases)
        {
            if (!validDecisions.Contains(item.Decision) || !validLayers.Contains(item.FailureLayer))
            {
                throw new InputException($"candidate case {item.CaseId} contains an unknown enum value");
            }
            if (item.ReasonCodes.Distinct(StringComparer.Ordinal).Count() != item.ReasonCodes.Count)
            {
                throw new InputException($"candidate case {item.CaseId} contains duplicate reason codes");
            }
        }
    }

    private static Dictionary<string, string> ParseOptions(string[] args)
    {
        if (args.Length % 2 != 0)
        {
            throw new InputException("all options require a value");
        }
        var options = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var index = 0; index < args.Length; index += 2)
        {
            if (!args[index].StartsWith("--", StringComparison.Ordinal) || !options.TryAdd(args[index], args[index + 1]))
            {
                throw new InputException($"invalid or duplicate option: {args[index]}");
            }
        }
        var allowed = new HashSet<string>(["--corpus", "--policy", "--candidate", "--baseline", "--output"], StringComparer.Ordinal);
        var unknown = options.Keys.Where(item => !allowed.Contains(item)).ToArray();
        if (unknown.Length != 0)
        {
            throw new InputException($"unknown options: {string.Join(',', unknown)}");
        }
        return options;
    }

    private static string RequireOption(Dictionary<string, string> options, string name)
    {
        if (!options.TryGetValue(name, out var value) || string.IsNullOrWhiteSpace(value))
        {
            throw new InputException($"required option missing: {name}");
        }
        return value;
    }

    private static T Read<T>(string path)
    {
        var value = JsonSerializer.Deserialize<T>(File.ReadAllBytes(path), InputJson);
        return value ?? throw new InputException($"JSON document is null: {path}");
    }

    private static void WriteResult(string outputDirectory, EvalResult result)
    {
        Directory.CreateDirectory(outputDirectory);
        var json = JsonSerializer.Serialize(result, OutputJson).Replace("\r\n", "\n") + "\n";
        File.WriteAllBytes(Path.Combine(outputDirectory, "result.json"), new UTF8Encoding(false).GetBytes(json));
    }

    private sealed class InputException(string message) : Exception(message);

    private sealed class Corpus
    {
        public string SchemaVersion { get; init; } = "";
        public string FixtureVersion { get; init; } = "";
        public string DatasetId { get; init; } = "";
        public string DatasetRevision { get; init; } = "";
        public List<CorpusCase> Cases { get; init; } = [];
    }

    private sealed class CorpusCase
    {
        public string CaseId { get; init; } = "";
        public string Criticality { get; init; } = "";
        public Oracle Oracle { get; init; } = new();
    }

    private sealed class Oracle
    {
        public string Decision { get; init; } = "";
        public string FailureLayer { get; init; } = "";
        public List<string> ReasonCodes { get; init; } = [];
    }

    private sealed class Candidate
    {
        public string CandidateSchemaVersion { get; init; } = "";
        public string CandidateId { get; init; } = "";
        public string DatasetId { get; init; } = "";
        public string DatasetRevision { get; init; } = "";
        public string ScorerId { get; init; } = "";
        public string ScorerVersion { get; init; } = "";
        public List<CandidateCase> Cases { get; init; } = [];
    }

    private sealed class CandidateCase
    {
        public string CaseId { get; init; } = "";
        public string Decision { get; init; } = "";
        public string FailureLayer { get; init; } = "";
        public List<string> ReasonCodes { get; init; } = [];
    }

    private sealed class ScorerPolicy
    {
        public string SchemaVersion { get; init; } = "";
        public string ScorerId { get; init; } = "";
        public string ScorerVersion { get; init; } = "";
        public OverallGate OverallGate { get; init; } = new();
    }

    private sealed class OverallGate
    {
        public List<string> AllRequired { get; init; } = [];
    }

    private sealed class EvalResult
    {
        public string SchemaVersion { get; init; } = "lab06-eval-result-v1";
        public string CandidateId { get; init; } = "";
        public string DatasetId { get; init; } = "";
        public string DatasetRevision { get; init; } = "";
        public string ScorerId { get; init; } = "";
        public string ScorerVersion { get; init; } = "";
        public string CandidateSchemaVersion { get; init; } = "";
        public bool ManifestComparable { get; init; }
        public List<string> ManifestMismatches { get; init; } = [];
        public string RunVerdict { get; init; } = "";
        public int? TotalCaseCount { get; init; }
        public int? PassedCaseCount { get; init; }
        public int? TotalCriticalCount { get; init; }
        public int? PassedCriticalCount { get; init; }
        public decimal? AggregateAccuracy { get; init; }
        public decimal? CriticalAccuracy { get; init; }
        public bool? AggregateThresholdPass { get; init; }
        public bool? CriticalGatePass { get; init; }
        public int MissingCaseCount { get; init; }
        public int UnknownCaseCount { get; init; }
        public string OverallGate { get; init; } = "";
        public List<CaseResult> Cases { get; init; } = [];
    }

    private sealed class CaseResult
    {
        public string CaseId { get; init; } = "";
        public string Criticality { get; init; } = "";
        public bool CandidatePresent { get; init; }
        public bool? Pass { get; init; }
        public string ChangeVerdict { get; init; } = "";
    }
}
