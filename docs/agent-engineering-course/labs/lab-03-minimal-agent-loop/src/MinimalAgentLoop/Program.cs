namespace MinimalAgentLoop;

internal static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            string casesPath = RequiredOption(args, "--cases");
            string outputPath = RequiredOption(args, "--out");
            SuiteExecutionSummary summary = LabRunner.Execute(casesPath, outputPath);
            Console.WriteLine(
                $"LAB03_RUN PASS cases={summary.CaseCount} steps={summary.StepCount} terminals={summary.TerminalCount} " +
                $"states={summary.StateSnapshotCount} tool_calls={summary.ToolCallCount} " +
                $"decision_calls={summary.DecisionCallCount} succeeded={summary.SucceededCount}");
            foreach ((string path, string sha256) in summary.ArtifactSha256.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                Console.WriteLine($"ARTIFACT path={path} sha256={sha256}");
            }

            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"LAB03_RUN FAIL type={exception.GetType().Name} message={exception.Message}");
            Console.Error.WriteLine(exception.StackTrace);
            return 1;
        }
    }

    private static string RequiredOption(string[] args, string option)
    {
        int index = Array.IndexOf(args, option);
        if (index < 0 || index + 1 >= args.Length)
        {
            throw new ArgumentException($"Missing required option {option}.");
        }

        return args[index + 1];
    }
}
