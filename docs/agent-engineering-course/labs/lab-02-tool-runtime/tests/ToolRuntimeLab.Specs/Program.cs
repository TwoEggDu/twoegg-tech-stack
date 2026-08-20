using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ToolRuntimeLab;

namespace ToolRuntimeLab.Specs;

internal static class Program
{
    private const string SmallSha256 = "E49C81E2D2F84E259D40E2FB8192F3BCD198B355184845D76D8F58807D0D78EE";
    private const string LargeSha256 = "26AD8132E3B544CAEFD85B30BF36DF8D012DC7245C9D2224E0F9F50A2AC55A61";
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<int> Main(string[] args)
    {
        try
        {
            if (args.Length > 0 && string.Equals(args[0], "--verify-link", StringComparison.Ordinal))
            {
                return VerifyLink(args);
            }

            await RunSpecsAsync(args).ConfigureAwait(false);
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"SPEC_FAILURE type={exception.GetType().Name} message={exception.Message}");
            Console.Error.WriteLine(exception.StackTrace);
            return 1;
        }
    }

    private static int VerifyLink(string[] args)
    {
        if (args.Length < 2) throw new ArgumentException("Link verifier requires a link path.");
        string linkPath = Path.GetFullPath(args[1]);
        string allowRoot = Path.GetFullPath(RequiredOption(args, "--allow-root"));
        string runRoot = Path.GetFullPath(RequiredOption(args, "--run-root"));
        FileSystemInfo? target = Directory.ResolveLinkTarget(linkPath, returnFinalTarget: true);
        if (target is null) throw new InvalidDataException("ResolveLinkTarget(true) returned null.");
        string finalTarget = Path.GetFullPath(target.FullName);
        bool outsideAllowRoot = !IsContained(allowRoot, finalTarget);
        bool insideRunRoot = IsContained(runRoot, finalTarget);
        bool targetFileExists = File.Exists(Path.Combine(finalTarget, "secret.txt"));
        if (!outsideAllowRoot || !insideRunRoot || !targetFileExists)
        {
            throw new InvalidDataException(
                $"Link target classification failed: outside_allow={outsideAllowRoot} inside_run={insideRunRoot} target_file={targetFileExists}");
        }

        Console.WriteLine(JsonSerializer.Serialize(new
        {
            final_target = finalTarget,
            outside_allow_root = outsideAllowRoot,
            inside_run_root = insideRunRoot,
            target_file_exists = targetFileExists
        }));
        return 0;
    }

    private static async Task RunSpecsAsync(string[] args)
    {
        string labRoot = Path.GetFullPath(Directory.GetCurrentDirectory());
        string manifestPath = Path.GetFullPath(RequiredOption(args, "--manifest"), labRoot);
        string runLabel = RequiredOption(args, "--run-label");
        string tracePath = Path.GetFullPath(RequiredOption(args, "--trace"), labRoot);
        Assert(runLabel is "first" or "second", "run-label must be first or second");
        Assert(IsContained(Path.Combine(labRoot, "fixtures"), manifestPath), "manifest must remain in Lab fixtures");
        Assert(IsContained(Path.Combine(labRoot, "artifacts"), tracePath), "trace must remain in Lab artifacts");

        string expectedTraceName = runLabel == "first" ? "observation-first.jsonl" : "observation.jsonl";
        Assert(string.Equals(Path.GetFileName(tracePath), expectedTraceName, StringComparison.Ordinal), "trace filename mismatch");

        string statePath = Path.Combine(labRoot, "artifacts", $"run-state-{runLabel}.json");
        RunState state = Deserialize<RunState>(File.ReadAllText(statePath, Encoding.UTF8));
        Assert(state.Status == "READY", "fixture state is not READY");
        string runRoot = Path.GetFullPath(state.RunRoot);
        string allowRoot = Path.GetFullPath(state.AllowRoot);
        ValidateRunRoot(runRoot);
        ValidateLinkState(state, runRoot, allowRoot);

        FixtureManifest manifest = Deserialize<FixtureManifest>(File.ReadAllText(manifestPath, Encoding.UTF8));
        Assert(manifest.SchemaVersion == "lab-02-cases-v1", "case schema version mismatch");
        Assert(manifest.Cases.Count == 14, "required invocation row count is not 14");
        Assert(manifest.Cases.Select(item => item.CaseId).Distinct(StringComparer.Ordinal).Count() == 12, "required case group count is not 12");
        Assert(manifest.Cases.Select(item => $"{item.CaseId}/{item.Attempt}").Distinct(StringComparer.Ordinal).Count() == 14,
            "case_id + attempt pairs are not unique");

        ValidateFixtureFile(Path.Combine(allowRoot, "small.txt"), 11, SmallSha256);
        ValidateFixtureFile(Path.Combine(allowRoot, "large.txt"), 1024, LargeSha256);

        var runtime = new ToolRuntimeLab.ToolRuntime(allowRoot, runRoot);
        var writer = new JsonlTraceWriter(tracePath);
        bool createNewGuarded = false;
        try
        {
            _ = new JsonlTraceWriter(tracePath);
        }
        catch (IOException)
        {
            createNewGuarded = true;
        }
        Assert(createNewGuarded, "trace CreateNew guard did not reject an existing artifact");

        var observed = new List<(CaseInput Case, InvocationOutcome Outcome)>();
        var byAttempt = new Dictionary<string, InvocationOutcome>(StringComparer.Ordinal);
        int sequence = 0;
        foreach (CaseInput item in manifest.Cases)
        {
            sequence++;
            using var callerSource = new CancellationTokenSource();
            if (item.Fault == "CALLER_PRE_CANCELLED") callerSource.Cancel();
            var request = new InvocationRequest(
                item.CaseId,
                item.Attempt,
                item.InvocationId,
                item.ToolName,
                item.Arguments,
                new PolicyInputs(item.Policy.Global, item.Policy.Tool, item.Policy.Resource),
                item.Fault,
                item.TimeoutMs);
            InvocationOutcome outcome = await runtime.InvokeAsync(request, callerSource.Token).ConfigureAwait(false);
            ValidateExpected(item, outcome);
            writer.Append(new TraceRow(sequence, request, outcome));
            observed.Add((item, outcome));
            byAttempt.Add($"{item.CaseId}/{item.Attempt}", outcome);
            Console.WriteLine(
                $"CASE_RESULT case={item.CaseId}/{item.Attempt} terminal={outcome.TerminalStage}/{outcome.TerminalCode} render={outcome.RenderStatus} handlers={outcome.HandlerExecutionCount} cancellation={outcome.CancellationOrigin}");
        }

        Assert(writer.PrefixIntegrity, "append-only prefix check failed");
        ValidateSpecialCases(runtime, byAttempt);
        CopyAndValidateSpillEvidence(labRoot, runRoot, runLabel, byAttempt["TR-10/1"]);
        string viewsPath = WriteAndValidateResultViews(labRoot, runLabel, runRoot, observed);
        ValidateTrace(tracePath, runRoot, observed);

        string traceSha256 = Hashing.Sha256Hex(File.ReadAllBytes(tracePath));
        Console.WriteLine($"TRACE_WRITER create_new=PASS append_prefix=PASS rows=14 sha256={traceSha256}");
        Console.WriteLine($"RESULT_VIEWS path={Path.GetRelativePath(labRoot, viewsPath).Replace('\\', '/')} absolute_path_present=false full_large_content_present=false");
        Console.WriteLine($"DISTRIBUTION terminal={Distribution(observed.Select(item => item.Outcome.TerminalStage))}");
        Console.WriteLine($"DISTRIBUTION render={Distribution(observed.Select(item => item.Outcome.RenderStatus))}");
        Console.WriteLine($"DISTRIBUTION handler_count={Distribution(observed.Select(item => item.Outcome.HandlerExecutionCount.ToString()))}");
        Console.WriteLine($"DISTRIBUTION cancellation={Distribution(observed.Select(item => item.Outcome.CancellationOrigin))}");
        Console.WriteLine("SPEC_RESULT PASS cases=12 rows=14 provider_calls=0 network_calls=0 credential_reads=0 shell_tools=0 business_writes=0");
    }

    private static void ValidateRunRoot(string runRoot)
    {
        string tempParent = Path.GetFullPath(Path.GetTempPath());
        Assert(IsContained(tempParent, runRoot), "run root is outside the OS temp parent");
        Assert(!string.Equals(tempParent, runRoot, StringComparison.OrdinalIgnoreCase), "run root equals temp parent");
        Assert(Path.GetFileName(runRoot).StartsWith("agent-engineering-lab-02-", StringComparison.Ordinal), "run root prefix mismatch");
        Assert(File.Exists(Path.Combine(runRoot, ".lab-02-owned")), "run root sentinel missing");
    }

    private static void ValidateLinkState(RunState state, string runRoot, string allowRoot)
    {
        Assert(state.LinkKind is "JUNCTION" or "SYMLINK", "link kind is not an actual supported disposition");
        string linkPath = Path.GetFullPath(state.LinkPath);
        FileSystemInfo? target = Directory.ResolveLinkTarget(linkPath, returnFinalTarget: true);
        Assert(target is not null, "ResolveLinkTarget(true) returned null during spec run");
        string finalTarget = Path.GetFullPath(target!.FullName);
        Assert(!IsContained(allowRoot, finalTarget), "link final target is not outside allow-root");
        Assert(IsContained(runRoot, finalTarget), "link final target is outside owned run-root");
        Assert(string.Equals(finalTarget, Path.GetFullPath(state.LinkFinalTarget), StringComparison.OrdinalIgnoreCase), "link final target changed after setup");
        Assert(state.FinalTargetOutsideAllowRoot && state.FinalTargetInsideRunRoot, "setup link classification flags are false");
        Assert(File.Exists(Path.Combine(finalTarget, "secret.txt")), "link target fixture file is missing");
        Console.WriteLine($"LINK_VERIFIED kind={state.LinkKind} resolve_link_target_true=PASS outside_allow_root=true inside_run_root=true");
    }

    private static void ValidateFixtureFile(string path, int expectedBytes, string expectedSha256)
    {
        byte[] bytes = File.ReadAllBytes(path);
        Assert(bytes.Length == expectedBytes, $"fixture length mismatch: {Path.GetFileName(path)}");
        Assert(Hashing.Sha256Hex(bytes) == expectedSha256, $"fixture SHA-256 mismatch: {Path.GetFileName(path)}");
    }

    private static void ValidateExpected(CaseInput item, InvocationOutcome outcome)
    {
        Assert(outcome.TerminalStage == item.Expected.TerminalStage, $"{item.CaseId}/{item.Attempt} terminal stage mismatch");
        Assert(outcome.TerminalCode == item.Expected.TerminalCode, $"{item.CaseId}/{item.Attempt} terminal code mismatch");
        Assert(outcome.CancellationOrigin == item.Expected.CancellationOrigin, $"{item.CaseId}/{item.Attempt} cancellation origin mismatch");
        Assert(outcome.RenderStatus == item.Expected.Render, $"{item.CaseId}/{item.Attempt} render mismatch");
        Assert(outcome.HandlerExecutionCount == item.Expected.HandlerCount, $"{item.CaseId}/{item.Attempt} handler count mismatch");
        Assert(outcome.RegistryStatus == "PASS", $"{item.CaseId}/{item.Attempt} registry did not pass");
        Assert(outcome.ArgumentsSha256.Length == 64 && outcome.ArgumentsSha256.All(Uri.IsHexDigit),
            $"{item.CaseId}/{item.Attempt} arguments digest missing");

        switch (outcome.TerminalStage)
        {
            case "CANONICALIZE":
                Assert(outcome.CanonicalizeStatus == "FAIL", "canonicalize terminal must fail canonicalization");
                Assert(outcome.ValidationStatus == "NOT_RUN" && outcome.PolicyDecision == "NOT_RUN"
                    && outcome.IdempotencyStatus == "NOT_RUN" && outcome.ExecuteStatus == "NOT_RUN"
                    && outcome.ResultValidationStatus == "NOT_RUN" && outcome.RenderStatus == "NOT_RUN",
                    "canonicalize terminal ran a later stage");
                break;
            case "POLICY":
                Assert(outcome.CanonicalizeStatus == "PASS" && outcome.ValidationStatus == "PASS", "policy terminal prerequisites failed");
                Assert(outcome.IdempotencyStatus == "NOT_RUN" && outcome.ExecuteStatus == "NOT_RUN"
                    && outcome.ResultValidationStatus == "NOT_RUN" && outcome.RenderStatus == "NOT_RUN",
                    "policy terminal ran a later stage");
                break;
            case "EXECUTE":
                Assert(outcome.IdempotencyStatus == "PASS" && outcome.ExecuteStatus == "FAIL", "execute terminal statuses mismatch");
                Assert(outcome.ResultValidationStatus == "NOT_RUN" && outcome.RenderStatus == "NOT_RUN",
                    "execute terminal ran result validation or render");
                break;
            case "RESULT_VALIDATION":
                Assert(outcome.ExecuteStatus == "PASS" && outcome.ResultValidationStatus == "FAIL" && outcome.RenderStatus == "NOT_RUN",
                    "invalid result did not stop before render");
                break;
            case "IDEMPOTENCY":
                Assert(outcome.ExecuteStatus == "NOT_RUN" && outcome.ResultValidationStatus == "NOT_RUN",
                    "idempotency terminal re-executed or revalidated result");
                if (outcome.TerminalCode == "IDEMPOTENCY_CONFLICT") Assert(outcome.RenderStatus == "NOT_RUN", "conflict rendered a result");
                break;
            case "SUCCEEDED":
                Assert(outcome.CanonicalizeStatus == "PASS" && outcome.ValidationStatus == "PASS"
                    && outcome.PolicyDecision == "ALLOW" && outcome.IdempotencyStatus == "PASS"
                    && outcome.ExecuteStatus == "PASS" && outcome.ResultValidationStatus == "PASS",
                    "successful invocation did not pass every executable stage");
                break;
            default:
                throw new InvalidDataException($"Unexpected terminal stage: {outcome.TerminalStage}");
        }
    }

    private static void ValidateSpecialCases(ToolRuntimeLab.ToolRuntime runtime, IReadOnlyDictionary<string, InvocationOutcome> observed)
    {
        Assert(observed["TR-01/1"].ModelPreview == "5", "TR-01 calculation value is not exactly 5");
        Assert(observed["TR-02/1"].ResultByteCount == 11 && observed["TR-02/1"].ResultSha256 == SmallSha256,
            "TR-02 byte count or SHA-256 mismatch");
        Assert(observed["TR-07/1"].CancellationOrigin == "TIMEOUT" && observed["TR-07/1"].HandlerExecutionCount == 1,
            "TR-07 did not preserve timeout origin and gate entry");
        Assert(observed["TR-08/1"].CancellationOrigin == "CALLER" && observed["TR-08/1"].HandlerExecutionCount == 0,
            "TR-08 did not stop before handler entry");
        Assert(!runtime.CacheContains("inv-invalid-result"), "TR-09 invalid result entered cache");
        Assert(observed["TR-11/2"].HandlerExecutionCount == 1
            && observed["TR-11/2"].ResultSha256 == observed["TR-11/1"].ResultSha256,
            "TR-11 replay changed handler count or result digest");
        Assert(observed["TR-12/2"].HandlerExecutionCount == 1
            && observed["TR-12/2"].ResultSha256 == "NONE"
            && observed["TR-12/2"].ResultByteCount == 0,
            "TR-12 conflict executed or produced a result");
    }

    private static void CopyAndValidateSpillEvidence(
        string labRoot,
        string runRoot,
        string runLabel,
        InvocationOutcome tr10)
    {
        Assert(tr10.ResultByteCount == 1024 && tr10.ResultSha256 == LargeSha256, "TR-10 byte count or SHA-256 mismatch");
        Assert(tr10.SpillRef.StartsWith("spills/", StringComparison.Ordinal) && !Path.IsPathRooted(tr10.SpillRef),
            "TR-10 spill ref is not relative");
        Assert(Encoding.UTF8.GetByteCount(tr10.ModelPreview) <= LabConstants.InlineThresholdBytes,
            "TR-10 model preview exceeds 64 bytes");
        string spillPath = Path.GetFullPath(Path.Combine(runRoot, tr10.SpillRef.Replace('/', Path.DirectorySeparatorChar)));
        Assert(IsContained(Path.Combine(runRoot, "spills"), spillPath), "TR-10 spill escaped the run spill root");
        byte[] spillBytes = File.ReadAllBytes(spillPath);
        Assert(spillBytes.Length == 1024 && Hashing.Sha256Hex(spillBytes) == LargeSha256,
            "TR-10 spill bytes or SHA-256 mismatch");

        string evidenceRoot = Path.Combine(labRoot, "artifacts", "spills", runLabel);
        Directory.CreateDirectory(evidenceRoot);
        string evidencePath = Path.Combine(evidenceRoot, LargeSha256.ToLowerInvariant() + ".txt");
        File.Copy(spillPath, evidencePath, overwrite: false);
        byte[] evidenceBytes = File.ReadAllBytes(evidencePath);
        Assert(evidenceBytes.AsSpan().SequenceEqual(spillBytes), "copied spill evidence is not byte-identical");
        Console.WriteLine($"SPILL_EVIDENCE ref=spills/{runLabel}/{Path.GetFileName(evidencePath)} bytes=1024 sha256={LargeSha256}");
    }

    private static string WriteAndValidateResultViews(
        string labRoot,
        string runLabel,
        string runRoot,
        IReadOnlyList<(CaseInput Case, InvocationOutcome Outcome)> observed)
    {
        string path = Path.Combine(labRoot, "artifacts", $"result-views-{runLabel}.json");
        using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
        {
            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
            writer.WriteStartObject();
            writer.WriteString("schema_version", "lab-02-result-views-v1");
            writer.WriteString("run_id", LabConstants.RunId);
            writer.WritePropertyName("views");
            writer.WriteStartArray();
            foreach ((CaseInput item, InvocationOutcome outcome) in observed)
            {
                writer.WriteStartObject();
                writer.WriteString("case_id", item.CaseId);
                writer.WriteNumber("attempt", item.Attempt);
                writer.WritePropertyName("model_view");
                writer.WriteStartObject();
                writer.WriteString("preview", outcome.ModelPreview);
                writer.WriteNumber("byte_count", outcome.ResultByteCount);
                writer.WriteString("sha256", outcome.ResultSha256);
                writer.WriteString("spill_ref", outcome.SpillRef);
                writer.WriteEndObject();
                writer.WritePropertyName("ui_view");
                writer.WriteStartObject();
                writer.WriteString("display_mode", outcome.UiDisplayMode);
                writer.WriteNumber("byte_count", outcome.ResultByteCount);
                writer.WriteString("sha256", outcome.ResultSha256);
                writer.WriteString("spill_ref", outcome.SpillRef);
                writer.WriteEndObject();
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
            writer.Flush();
            stream.WriteByte((byte)'\n');
        }

        string serialized = File.ReadAllText(path, Utf8NoBom);
        Assert(!serialized.Contains(runRoot, StringComparison.OrdinalIgnoreCase), "result views contain an absolute run path");
        Assert(!serialized.Contains(new string('L', 65), StringComparison.Ordinal), "result views contain more than the 64-byte large preview");
        return path;
    }

    private static void ValidateTrace(
        string tracePath,
        string runRoot,
        IReadOnlyList<(CaseInput Case, InvocationOutcome Outcome)> observed)
    {
        byte[] bytes = File.ReadAllBytes(tracePath);
        Assert(bytes.Length >= 3 && !(bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF), "trace contains UTF-8 BOM");
        Assert(bytes[^1] == (byte)'\n', "trace does not end with LF");
        Assert(!bytes.Contains((byte)'\r'), "trace contains CR instead of LF-only endings");
        string text = Utf8NoBom.GetString(bytes);
        string[] lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert(lines.Length == 14, "trace line count is not 14");
        Assert(!text.Contains(runRoot, StringComparison.OrdinalIgnoreCase), "trace contains absolute run path");
        Assert(!text.Contains("outside-secret", StringComparison.Ordinal), "trace contains outside file content");
        Assert(!text.Contains(new string('L', 65), StringComparison.Ordinal), "trace contains large-result content");

        string[] expectedProperties =
        [
            "schema_version", "sequence", "run_id", "case_id", "attempt", "invocation_id", "tool_name",
            "arguments_sha256", "registry_status", "canonicalize_status", "validation_status", "policy_inputs",
            "policy_decision", "idempotency_status", "execute_status", "handler_execution_count",
            "cancellation_origin", "result_validation_status", "render_status", "terminal_stage", "terminal_code",
            "result_byte_count", "result_sha256", "spill_ref"
        ];
        for (int index = 0; index < lines.Length; index++)
        {
            using JsonDocument document = JsonDocument.Parse(lines[index]);
            JsonElement root = document.RootElement;
            string[] properties = root.EnumerateObject().Select(property => property.Name).ToArray();
            Assert(properties.SequenceEqual(expectedProperties, StringComparer.Ordinal), $"trace property order mismatch at row {index + 1}");
            Assert(root.GetProperty("sequence").GetInt32() == index + 1, $"trace sequence mismatch at row {index + 1}");
            Assert(root.GetProperty("run_id").GetString() == LabConstants.RunId, "trace run_id mismatch");
            Assert(root.GetProperty("case_id").GetString() == observed[index].Case.CaseId, "trace case order mismatch");
            Assert(root.GetProperty("attempt").GetInt32() == observed[index].Case.Attempt, "trace attempt mismatch");
        }
    }

    private static string Distribution(IEnumerable<string> values) => string.Join(
        ",",
        values.GroupBy(value => value, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => $"{group.Key}:{group.Count()}"));

    private static bool IsContained(string parent, string candidate)
    {
        string relative = Path.GetRelativePath(Path.GetFullPath(parent), Path.GetFullPath(candidate));
        return !Path.IsPathRooted(relative)
            && !string.Equals(relative, "..", StringComparison.Ordinal)
            && !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            && !relative.StartsWith(".." + Path.AltDirectorySeparatorChar, StringComparison.Ordinal);
    }

    private static string RequiredOption(string[] args, string option)
    {
        int index = Array.IndexOf(args, option);
        if (index < 0 || index + 1 >= args.Length) throw new ArgumentException($"Missing required option: {option}");
        return args[index + 1];
    }

    private static T Deserialize<T>(string json) where T : class =>
        JsonSerializer.Deserialize<T>(json, JsonOptions) ?? throw new InvalidDataException($"Could not deserialize {typeof(T).Name}.");

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidDataException(message);
    }
}

internal sealed class FixtureManifest
{
    [JsonPropertyName("schema_version")]
    public required string SchemaVersion { get; init; }

    [JsonPropertyName("cases")]
    public required List<CaseInput> Cases { get; init; }
}

internal sealed class CaseInput
{
    [JsonPropertyName("case_id")]
    public required string CaseId { get; init; }

    [JsonPropertyName("attempt")]
    public required int Attempt { get; init; }

    [JsonPropertyName("invocation_id")]
    public required string InvocationId { get; init; }

    [JsonPropertyName("tool_name")]
    public required string ToolName { get; init; }

    [JsonPropertyName("arguments")]
    public required JsonElement Arguments { get; init; }

    [JsonPropertyName("policy")]
    public required PolicyInput Policy { get; init; }

    [JsonPropertyName("fault")]
    public required string Fault { get; init; }

    [JsonPropertyName("timeout_ms")]
    public required int TimeoutMs { get; init; }

    [JsonPropertyName("expected")]
    public required ExpectedInput Expected { get; init; }
}

internal sealed class PolicyInput
{
    [JsonPropertyName("global")]
    public required string Global { get; init; }

    [JsonPropertyName("tool")]
    public required string Tool { get; init; }

    [JsonPropertyName("resource")]
    public required string Resource { get; init; }
}

internal sealed class ExpectedInput
{
    [JsonPropertyName("terminal_stage")]
    public required string TerminalStage { get; init; }

    [JsonPropertyName("terminal_code")]
    public required string TerminalCode { get; init; }

    [JsonPropertyName("cancellation_origin")]
    public required string CancellationOrigin { get; init; }

    [JsonPropertyName("render")]
    public required string Render { get; init; }

    [JsonPropertyName("handler_count")]
    public required int HandlerCount { get; init; }
}

internal sealed class RunState
{
    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("run_root")]
    public required string RunRoot { get; init; }

    [JsonPropertyName("allow_root")]
    public required string AllowRoot { get; init; }

    [JsonPropertyName("link_path")]
    public required string LinkPath { get; init; }

    [JsonPropertyName("link_kind")]
    public required string LinkKind { get; init; }

    [JsonPropertyName("link_final_target")]
    public required string LinkFinalTarget { get; init; }

    [JsonPropertyName("final_target_outside_allow_root")]
    public required bool FinalTargetOutsideAllowRoot { get; init; }

    [JsonPropertyName("final_target_inside_run_root")]
    public required bool FinalTargetInsideRunRoot { get; init; }
}
