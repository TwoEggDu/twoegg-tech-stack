using System.Globalization;
using System.Text;
using System.Text.Json;
using StructuredOutputValidation;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

try
{
    var paths = CommandLinePaths.Parse(args);
    var casesJson = await File.ReadAllTextAsync(paths.CasesPath, Encoding.UTF8);
    var schemaJson = await File.ReadAllTextAsync(paths.SchemaPath, Encoding.UTF8);
    var allowlistJson = await File.ReadAllTextAsync(paths.AllowlistPath, Encoding.UTF8);

    var fixtureCases = JsonSerializer.Deserialize<List<FixtureCase>>(
                           casesJson,
                           JsonSerializerOptions.Strict)
                       ?? throw new InvalidDataException("cases.json produced no cases.");

    var duplicateCaseIds = fixtureCases
        .GroupBy(item => item.CaseId, StringComparer.Ordinal)
        .Where(group => group.Count() > 1)
        .Select(group => group.Key)
        .ToArray();
    if (duplicateCaseIds.Length > 0)
    {
        throw new InvalidDataException($"Duplicate case IDs: {string.Join(", ", duplicateCaseIds)}");
    }

    var allowlistValues = JsonSerializer.Deserialize<List<string>>(
                              allowlistJson,
                              JsonSerializerOptions.Strict)
                          ?? throw new InvalidDataException("evidence-allowlist.json produced no values.");
    var allowlist = new HashSet<string>(allowlistValues, StringComparer.Ordinal);
    if (allowlist.Count != allowlistValues.Count)
    {
        throw new InvalidDataException("Evidence allowlist contains duplicate values.");
    }

    var pipeline = await ValidationPipeline.CreateAsync(schemaJson, allowlist);
    var observations = fixtureCases.Select(pipeline.Evaluate).ToArray();

    var mismatches = fixtureCases
        .Zip(observations)
        .Where(pair =>
            !string.Equals(pair.First.ExpectedTerminalStage, pair.Second.TerminalStage, StringComparison.Ordinal)
            || !pair.First.ExpectedErrorCodes.SequenceEqual(pair.Second.ErrorCodes, StringComparer.Ordinal)
            || !string.Equals(pair.First.ExpectedAction, pair.Second.RecommendedAction, StringComparison.Ordinal))
        .Select(pair =>
            $"{pair.First.CaseId}: expected "
            + $"{pair.First.ExpectedTerminalStage}/{string.Join(',', pair.First.ExpectedErrorCodes)}/{pair.First.ExpectedAction}, "
            + $"observed {pair.Second.TerminalStage}/{string.Join(',', pair.Second.ErrorCodes)}/{pair.Second.RecommendedAction}")
        .ToArray();

    var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(paths.OutputPath));
    if (!string.IsNullOrEmpty(outputDirectory))
    {
        Directory.CreateDirectory(outputDirectory);
    }

    var serializedLines = observations.Select(observation => JsonSerializer.Serialize(observation));
    await File.WriteAllTextAsync(
        paths.OutputPath,
        string.Join('\n', serializedLines) + "\n",
        new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

    Console.WriteLine($"Wrote {observations.Length} observation rows to {paths.OutputPath}.");
    Console.WriteLine($"Accepted cases: {observations.Count(item => item.TerminalStage == "ACCEPTED")}.");
    Console.WriteLine($"Automatic repair attempts: {ValidationPipeline.AutomaticRepairAttempts}.");

    if (mismatches.Length > 0)
    {
        foreach (var mismatch in mismatches)
        {
            Console.Error.WriteLine(mismatch);
        }

        return 2;
    }

    return 0;
}
catch (Exception exception) when (exception is ArgumentException or IOException or JsonException)
{
    Console.Error.WriteLine(exception.Message);
    return 1;
}

internal sealed record CommandLinePaths(
    string CasesPath,
    string SchemaPath,
    string AllowlistPath,
    string OutputPath)
{
    public static CommandLinePaths Parse(string[] args)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var index = 0; index < args.Length; index += 2)
        {
            if (index + 1 >= args.Length || !args[index].StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException("Expected --name value command-line pairs.");
            }

            values.Add(args[index], args[index + 1]);
        }

        return new CommandLinePaths(
            Require(values, "--cases"),
            Require(values, "--schema"),
            Require(values, "--allowlist"),
            Require(values, "--output"));
    }

    private static string Require(IReadOnlyDictionary<string, string> values, string key) =>
        values.TryGetValue(key, out var value)
            ? value
            : throw new ArgumentException($"Missing required argument {key}.");
}
