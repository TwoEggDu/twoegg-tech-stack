using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ToolRuntimeLab;

public static class LabConstants
{
    public const int InlineThresholdBytes = 64;
    public const int MaxReadableBytes = 4096;
    public const string RunId = "lab-02-fixed-run";
    public const string TraceSchemaVersion = "lab-02-trace-v1";
}

public sealed record ToolDefinition(string Name, string ResultKind);

public sealed record PolicyInputs(string Global, string Tool, string Resource);

public sealed record InvocationRequest(
    string CaseId,
    int Attempt,
    string InvocationId,
    string ToolName,
    JsonElement Arguments,
    PolicyInputs Policy,
    string Fault,
    int TimeoutMs);

public sealed record ToolResult(
    string Kind,
    decimal? CalculationValue,
    byte[]? Content,
    int ByteCount,
    string Sha256);

public sealed record RenderMetadata(
    string Mode,
    string ModelPreview,
    string DisplayMode,
    int ByteCount,
    string Sha256,
    string SpillRef);

public sealed record InvocationOutcome(
    string ArgumentsSha256,
    string RegistryStatus,
    string CanonicalizeStatus,
    string ValidationStatus,
    string PolicyDecision,
    string IdempotencyStatus,
    string ExecuteStatus,
    int HandlerExecutionCount,
    string CancellationOrigin,
    string ResultValidationStatus,
    string RenderStatus,
    string TerminalStage,
    string TerminalCode,
    int ResultByteCount,
    string ResultSha256,
    string SpillRef,
    string ModelPreview,
    string UiDisplayMode);

public sealed record TraceRow(
    int Sequence,
    InvocationRequest Request,
    InvocationOutcome Outcome);

public sealed class ToolRegistry
{
    private readonly Dictionary<string, RegistryEntry> entries = new(StringComparer.Ordinal);

    public ToolRegistry Register(RegistryEntry entry)
    {
        if (!entries.TryAdd(entry.Definition.Name, entry))
        {
            throw new InvalidOperationException($"Duplicate tool registration: {entry.Definition.Name}");
        }

        return this;
    }

    public bool TryGet(string name, out RegistryEntry entry) => entries.TryGetValue(name, out entry!);
}

public sealed record RegistryEntry(
    ToolDefinition Definition,
    string SideEffect,
    int DefaultTimeoutMs,
    IToolHandler Handler);

public interface IToolHandler
{
    Task<ToolResult> ExecuteAsync(ToolExecutionContext context, CancellationToken cancellationToken);
}

public sealed record ToolExecutionContext(
    string ToolName,
    string Fault,
    string? Operation,
    decimal? Left,
    decimal? Right,
    string? ResolvedPath);

public sealed class CalculatorTool : IToolHandler
{
    public Task<ToolResult> ExecuteAsync(ToolExecutionContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (context.Fault == "INVALID_RESULT_KIND")
        {
            return Task.FromResult(new ToolResult("file_text", null, null, 0, "NONE"));
        }

        if (context.Operation is null || context.Left is null || context.Right is null)
        {
            throw new InvalidOperationException("Validated calculator arguments were not supplied to the handler.");
        }

        decimal value = context.Operation switch
        {
            "add" => context.Left.Value + context.Right.Value,
            "subtract" => context.Left.Value - context.Right.Value,
            _ => throw new InvalidOperationException("Unsupported validated operation.")
        };
        return Task.FromResult(new ToolResult("calculation", value, null, 0, "NONE"));
    }
}

public sealed class ReadOnlyFileTool : IToolHandler
{
    public async Task<ToolResult> ExecuteAsync(ToolExecutionContext context, CancellationToken cancellationToken)
    {
        if (context.Fault == "NEVER_RELEASE_GATE")
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
            throw new InvalidOperationException("The never-release gate unexpectedly completed.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (context.ResolvedPath is null)
        {
            throw new InvalidOperationException("Validated resolved path was not supplied to the handler.");
        }

        await using var stream = new FileStream(
            context.ResolvedPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        byte[] content = buffer.ToArray();
        return new ToolResult("file_text", null, content, content.Length, Hashing.Sha256Hex(content));
    }
}

public static class PolicyMerger
{
    private static readonly HashSet<string> Allowed = new(StringComparer.Ordinal)
    {
        "ALLOW", "DENY", "ASK", "MISSING"
    };

    public static string Merge(PolicyInputs inputs)
    {
        string[] decisions = [inputs.Global, inputs.Tool, inputs.Resource];
        if (decisions.Any(static value => !Allowed.Contains(value)))
        {
            return "DENY";
        }

        if (decisions.Contains("DENY", StringComparer.Ordinal)) return "DENY";
        if (decisions.Contains("MISSING", StringComparer.Ordinal)) return "DENY";
        if (decisions.Contains("ASK", StringComparer.Ordinal)) return "ASK";
        return "ALLOW";
    }
}

public static class Hashing
{
    public static string Sha256Hex(ReadOnlySpan<byte> bytes) => Convert.ToHexString(SHA256.HashData(bytes));
}

internal sealed record CanonicalArguments(
    byte[] Bytes,
    string Digest,
    string? Operation,
    decimal? Left,
    decimal? Right,
    string? RelativePath,
    string? ResolvedPath);

internal sealed record CanonicalizationResult(bool Success, string Code, CanonicalArguments? Arguments);

internal sealed record CachedInvocation(string ArgumentsDigest, ToolResult Result, RenderMetadata Render);

public sealed class ToolRuntime
{
    private readonly string allowRoot;
    private readonly string runRoot;
    private readonly string spillRoot;
    private readonly ToolRegistry registry;
    private readonly Dictionary<string, CachedInvocation> invocationCache = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> handlerCounts = new(StringComparer.Ordinal);

    public ToolRuntime(string allowRoot, string runRoot)
    {
        this.allowRoot = Path.GetFullPath(allowRoot);
        this.runRoot = Path.GetFullPath(runRoot);
        spillRoot = Path.Combine(this.runRoot, "spills");
        if (!Directory.Exists(this.allowRoot) || !Directory.Exists(spillRoot))
        {
            throw new DirectoryNotFoundException("Fixture allow-root or spill-root is missing.");
        }

        registry = new ToolRegistry()
            .Register(new RegistryEntry(
                new ToolDefinition("calculate_binary", "calculation"),
                "NONE",
                1000,
                new CalculatorTool()))
            .Register(new RegistryEntry(
                new ToolDefinition("read_text", "file_text"),
                "READ_ONLY",
                1000,
                new ReadOnlyFileTool()));
    }

    public bool CacheContains(string invocationId) => invocationCache.ContainsKey(invocationId);

    public async Task<InvocationOutcome> InvokeAsync(InvocationRequest request, CancellationToken callerToken)
    {
        string registryStatus = "NOT_RUN";
        string canonicalizeStatus = "NOT_RUN";
        string validationStatus = "NOT_RUN";
        string policyDecision = "NOT_RUN";
        string idempotencyStatus = "NOT_RUN";
        string executeStatus = "NOT_RUN";
        string resultValidationStatus = "NOT_RUN";
        string renderStatus = "NOT_RUN";
        string cancellationOrigin = "NONE";
        string argumentsDigest = "NONE";

        if (!registry.TryGet(request.ToolName, out RegistryEntry entry))
        {
            return Outcome("NONE", "FAIL", canonicalizeStatus, validationStatus, policyDecision,
                idempotencyStatus, executeStatus, request.InvocationId, cancellationOrigin,
                resultValidationStatus, renderStatus, "REGISTRY", "TOOL_NOT_FOUND");
        }

        registryStatus = "PASS";
        CanonicalizationResult canonicalization = Canonicalize(request.ToolName, request.Arguments);
        if (!canonicalization.Success || canonicalization.Arguments is null)
        {
            string rejectedDigest = canonicalization.Arguments?.Digest ?? "NONE";
            return Outcome(rejectedDigest, registryStatus, "FAIL", validationStatus, policyDecision,
                idempotencyStatus, executeStatus, request.InvocationId, cancellationOrigin,
                resultValidationStatus, renderStatus, "CANONICALIZE", canonicalization.Code);
        }

        canonicalizeStatus = "PASS";
        CanonicalArguments canonical = canonicalization.Arguments;
        argumentsDigest = canonical.Digest;
        if (!ValidateArguments(request.ToolName, canonical))
        {
            return Outcome(argumentsDigest, registryStatus, canonicalizeStatus, "FAIL", policyDecision,
                idempotencyStatus, executeStatus, request.InvocationId, cancellationOrigin,
                resultValidationStatus, renderStatus, "VALIDATION", "ARGUMENTS_INVALID");
        }

        validationStatus = "PASS";
        policyDecision = PolicyMerger.Merge(request.Policy);
        if (policyDecision == "DENY")
        {
            return Outcome(argumentsDigest, registryStatus, canonicalizeStatus, validationStatus, policyDecision,
                idempotencyStatus, executeStatus, request.InvocationId, cancellationOrigin,
                resultValidationStatus, renderStatus, "POLICY", "POLICY_DENIED");
        }

        if (policyDecision == "ASK")
        {
            return Outcome(argumentsDigest, registryStatus, canonicalizeStatus, validationStatus, policyDecision,
                idempotencyStatus, executeStatus, request.InvocationId, cancellationOrigin,
                resultValidationStatus, renderStatus, "POLICY", "APPROVAL_REQUIRED");
        }

        if (invocationCache.TryGetValue(request.InvocationId, out CachedInvocation? cached))
        {
            if (!string.Equals(cached.ArgumentsDigest, argumentsDigest, StringComparison.Ordinal))
            {
                return Outcome(argumentsDigest, registryStatus, canonicalizeStatus, validationStatus, policyDecision,
                    "FAIL", executeStatus, request.InvocationId, cancellationOrigin,
                    resultValidationStatus, renderStatus, "IDEMPOTENCY", "IDEMPOTENCY_CONFLICT");
            }

            return Outcome(argumentsDigest, registryStatus, canonicalizeStatus, validationStatus, policyDecision,
                "PASS", executeStatus, request.InvocationId, cancellationOrigin,
                resultValidationStatus, cached.Render.Mode, "IDEMPOTENCY", "REPLAYED",
                cached.Render.ByteCount, cached.Render.Sha256, cached.Render.SpillRef,
                cached.Render.ModelPreview, cached.Render.DisplayMode);
        }

        idempotencyStatus = "PASS";
        if (callerToken.IsCancellationRequested)
        {
            cancellationOrigin = "CALLER";
            return Outcome(argumentsDigest, registryStatus, canonicalizeStatus, validationStatus, policyDecision,
                idempotencyStatus, "FAIL", request.InvocationId, cancellationOrigin,
                resultValidationStatus, renderStatus, "EXECUTE", "CALLER_CANCELLED");
        }

        using var timeoutSource = new CancellationTokenSource();
        timeoutSource.CancelAfter(request.TimeoutMs);
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(callerToken, timeoutSource.Token);

        ToolResult candidate;
        IncrementHandlerCount(request.InvocationId);
        try
        {
            var context = new ToolExecutionContext(
                request.ToolName,
                request.Fault,
                canonical.Operation,
                canonical.Left,
                canonical.Right,
                canonical.ResolvedPath);
            candidate = await entry.Handler.ExecuteAsync(context, linkedSource.Token).ConfigureAwait(false);
            executeStatus = "PASS";
        }
        catch (OperationCanceledException) when (callerToken.IsCancellationRequested)
        {
            cancellationOrigin = "CALLER";
            return Outcome(argumentsDigest, registryStatus, canonicalizeStatus, validationStatus, policyDecision,
                idempotencyStatus, "FAIL", request.InvocationId, cancellationOrigin,
                resultValidationStatus, renderStatus, "EXECUTE", "CALLER_CANCELLED");
        }
        catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested)
        {
            cancellationOrigin = "TIMEOUT";
            return Outcome(argumentsDigest, registryStatus, canonicalizeStatus, validationStatus, policyDecision,
                idempotencyStatus, "FAIL", request.InvocationId, cancellationOrigin,
                resultValidationStatus, renderStatus, "EXECUTE", "TIMED_OUT");
        }

        if (!ValidateResult(entry.Definition.ResultKind, candidate))
        {
            return Outcome(argumentsDigest, registryStatus, canonicalizeStatus, validationStatus, policyDecision,
                idempotencyStatus, executeStatus, request.InvocationId, cancellationOrigin,
                "FAIL", renderStatus, "RESULT_VALIDATION", "RESULT_SCHEMA_INVALID");
        }

        resultValidationStatus = "PASS";
        RenderMetadata render = Render(candidate);
        renderStatus = render.Mode;
        invocationCache.Add(request.InvocationId, new CachedInvocation(argumentsDigest, candidate, render));
        return Outcome(argumentsDigest, registryStatus, canonicalizeStatus, validationStatus, policyDecision,
            idempotencyStatus, executeStatus, request.InvocationId, cancellationOrigin,
            resultValidationStatus, renderStatus, "SUCCEEDED", "OK",
            render.ByteCount, render.Sha256, render.SpillRef, render.ModelPreview, render.DisplayMode);
    }

    private CanonicalizationResult Canonicalize(string toolName, JsonElement arguments)
    {
        try
        {
            if (toolName == "calculate_binary")
            {
                string operation = arguments.GetProperty("operation").GetString() ?? string.Empty;
                decimal left = arguments.GetProperty("left").GetDecimal();
                decimal right = arguments.GetProperty("right").GetDecimal();
                byte[] bytes = WriteCanonicalArguments(writer =>
                {
                    writer.WriteString("operation", operation);
                    writer.WriteNumber("left", left);
                    writer.WriteNumber("right", right);
                });
                return Success(bytes, operation, left, right, null, null);
            }

            string relativePath = arguments.GetProperty("relative_path").GetString() ?? string.Empty;
            byte[] canonicalBytes = WriteCanonicalArguments(writer => writer.WriteString("relative_path", relativePath));
            if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
            {
                return Failure("PATH_OUTSIDE_ROOT", canonicalBytes, relativePath);
            }

            string lexicalCandidate = Path.GetFullPath(relativePath, allowRoot);
            if (!IsContained(allowRoot, lexicalCandidate))
            {
                return Failure("PATH_OUTSIDE_ROOT", canonicalBytes, relativePath);
            }

            string resolvedCandidate = ResolveExistingComponents(allowRoot, lexicalCandidate);
            if (!IsContained(allowRoot, resolvedCandidate))
            {
                return Failure("PATH_LINK_OUTSIDE_ROOT", canonicalBytes, relativePath);
            }

            return Success(canonicalBytes, null, null, null, relativePath, resolvedCandidate);
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or IOException or UnauthorizedAccessException or ArgumentException)
        {
            return new CanonicalizationResult(false, "ARGUMENTS_INVALID", null);
        }
    }

    private static CanonicalizationResult Success(
        byte[] bytes,
        string? operation,
        decimal? left,
        decimal? right,
        string? relativePath,
        string? resolvedPath) =>
        new(true, "OK", new CanonicalArguments(
            bytes,
            Hashing.Sha256Hex(bytes),
            operation,
            left,
            right,
            relativePath,
            resolvedPath));

    private static CanonicalizationResult Failure(string code, byte[] bytes, string? relativePath) =>
        new(false, code, new CanonicalArguments(
            bytes,
            Hashing.Sha256Hex(bytes),
            null,
            null,
            null,
            relativePath,
            null));

    private static byte[] WriteCanonicalArguments(Action<Utf8JsonWriter> writeProperties)
    {
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = false }))
        {
            writer.WriteStartObject();
            writeProperties(writer);
            writer.WriteEndObject();
        }

        return buffer.ToArray();
    }

    private static bool IsContained(string parent, string candidate)
    {
        string relative = Path.GetRelativePath(Path.GetFullPath(parent), Path.GetFullPath(candidate));
        return !Path.IsPathRooted(relative)
            && !string.Equals(relative, "..", StringComparison.Ordinal)
            && !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            && !relative.StartsWith(".." + Path.AltDirectorySeparatorChar, StringComparison.Ordinal);
    }

    private static string ResolveExistingComponents(string root, string candidate)
    {
        string relative = Path.GetRelativePath(root, candidate);
        string[] components = relative.Split(
            [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
            StringSplitOptions.RemoveEmptyEntries);
        string resolved = Path.GetFullPath(root);

        foreach (string component in components)
        {
            string current = Path.Combine(resolved, component);
            FileSystemInfo? target = null;
            if (Directory.Exists(current))
            {
                target = Directory.ResolveLinkTarget(current, returnFinalTarget: true);
            }
            else if (File.Exists(current))
            {
                target = File.ResolveLinkTarget(current, returnFinalTarget: true);
            }

            resolved = target is null ? current : target.FullName;
        }

        return Path.GetFullPath(resolved);
    }

    private static bool ValidateArguments(string toolName, CanonicalArguments arguments)
    {
        if (toolName == "calculate_binary")
        {
            return arguments.Operation is "add" or "subtract"
                && arguments.Left.HasValue
                && arguments.Right.HasValue;
        }

        if (arguments.RelativePath is null || arguments.ResolvedPath is null || !File.Exists(arguments.ResolvedPath))
        {
            return false;
        }

        var info = new FileInfo(arguments.ResolvedPath);
        return info.Length <= LabConstants.MaxReadableBytes;
    }

    private static bool ValidateResult(string expectedKind, ToolResult result)
    {
        if (!string.Equals(expectedKind, result.Kind, StringComparison.Ordinal)) return false;
        if (expectedKind == "calculation") return result.CalculationValue.HasValue && result.Content is null;
        if (result.Content is null || result.CalculationValue.HasValue) return false;
        return result.ByteCount == result.Content.Length
            && result.ByteCount <= LabConstants.MaxReadableBytes
            && string.Equals(result.Sha256, Hashing.Sha256Hex(result.Content), StringComparison.Ordinal);
    }

    private RenderMetadata Render(ToolResult result)
    {
        if (result.Kind == "calculation")
        {
            string value = result.CalculationValue!.Value.ToString(CultureInfo.InvariantCulture);
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            return new RenderMetadata(
                "INLINE",
                value,
                "INLINE",
                bytes.Length,
                Hashing.Sha256Hex(bytes),
                "NONE");
        }

        byte[] content = result.Content!;
        int previewLength = Math.Min(LabConstants.InlineThresholdBytes, content.Length);
        string preview = Encoding.UTF8.GetString(content, 0, previewLength);
        if (content.Length <= LabConstants.InlineThresholdBytes)
        {
            return new RenderMetadata(
                "INLINE",
                preview,
                "INLINE",
                content.Length,
                result.Sha256,
                "NONE");
        }

        string lowerDigest = result.Sha256.ToLowerInvariant();
        string spillRef = $"spills/{lowerDigest}.txt";
        string spillPath = Path.Combine(spillRoot, lowerDigest + ".txt");
        using (var stream = new FileStream(spillPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
        {
            stream.Write(content);
            stream.Flush(flushToDisk: true);
        }

        return new RenderMetadata(
            "SPILLED",
            preview,
            "SPILLED",
            content.Length,
            result.Sha256,
            spillRef);
    }

    private void IncrementHandlerCount(string invocationId)
    {
        handlerCounts.TryGetValue(invocationId, out int count);
        handlerCounts[invocationId] = count + 1;
    }

    private int HandlerCount(string invocationId) => handlerCounts.TryGetValue(invocationId, out int count) ? count : 0;

    private InvocationOutcome Outcome(
        string argumentsSha256,
        string registryStatus,
        string canonicalizeStatus,
        string validationStatus,
        string policyDecision,
        string idempotencyStatus,
        string executeStatus,
        string invocationId,
        string cancellationOrigin,
        string resultValidationStatus,
        string renderStatus,
        string terminalStage,
        string terminalCode,
        int resultByteCount = 0,
        string resultSha256 = "NONE",
        string spillRef = "NONE",
        string modelPreview = "NONE",
        string uiDisplayMode = "NOT_RUN") =>
        new(
            argumentsSha256,
            registryStatus,
            canonicalizeStatus,
            validationStatus,
            policyDecision,
            idempotencyStatus,
            executeStatus,
            HandlerCount(invocationId),
            cancellationOrigin,
            resultValidationStatus,
            renderStatus,
            terminalStage,
            terminalCode,
            resultByteCount,
            resultSha256,
            spillRef,
            modelPreview,
            uiDisplayMode);
}

public sealed class JsonlTraceWriter
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);
    private readonly string tracePath;

    public JsonlTraceWriter(string tracePath)
    {
        this.tracePath = Path.GetFullPath(tracePath);
        string? parent = Path.GetDirectoryName(this.tracePath);
        if (parent is null) throw new InvalidOperationException("Trace path has no parent directory.");
        Directory.CreateDirectory(parent);
        using var created = new FileStream(this.tracePath, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
    }

    public bool PrefixIntegrity { get; private set; } = true;

    public void Append(TraceRow row)
    {
        byte[] before = File.ReadAllBytes(tracePath);
        byte[] json = Serialize(row);
        using (var stream = new FileStream(tracePath, FileMode.Append, FileAccess.Write, FileShare.Read))
        {
            stream.Write(json);
            stream.WriteByte((byte)'\n');
            stream.Flush(flushToDisk: true);
        }

        byte[] after = File.ReadAllBytes(tracePath);
        bool prefixMatches = after.Length == before.Length + json.Length + 1
            && before.AsSpan().SequenceEqual(after.AsSpan(0, before.Length));
        PrefixIntegrity &= prefixMatches;
        if (!prefixMatches)
        {
            throw new InvalidDataException("Append-only prefix invariant failed.");
        }
    }

    private static byte[] Serialize(TraceRow row)
    {
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = false }))
        {
            InvocationRequest request = row.Request;
            InvocationOutcome outcome = row.Outcome;
            writer.WriteStartObject();
            writer.WriteString("schema_version", LabConstants.TraceSchemaVersion);
            writer.WriteNumber("sequence", row.Sequence);
            writer.WriteString("run_id", LabConstants.RunId);
            writer.WriteString("case_id", request.CaseId);
            writer.WriteNumber("attempt", request.Attempt);
            writer.WriteString("invocation_id", request.InvocationId);
            writer.WriteString("tool_name", request.ToolName);
            writer.WriteString("arguments_sha256", outcome.ArgumentsSha256);
            writer.WriteString("registry_status", outcome.RegistryStatus);
            writer.WriteString("canonicalize_status", outcome.CanonicalizeStatus);
            writer.WriteString("validation_status", outcome.ValidationStatus);
            writer.WritePropertyName("policy_inputs");
            writer.WriteStartObject();
            writer.WriteString("global", request.Policy.Global);
            writer.WriteString("tool", request.Policy.Tool);
            writer.WriteString("resource", request.Policy.Resource);
            writer.WriteEndObject();
            writer.WriteString("policy_decision", outcome.PolicyDecision);
            writer.WriteString("idempotency_status", outcome.IdempotencyStatus);
            writer.WriteString("execute_status", outcome.ExecuteStatus);
            writer.WriteNumber("handler_execution_count", outcome.HandlerExecutionCount);
            writer.WriteString("cancellation_origin", outcome.CancellationOrigin);
            writer.WriteString("result_validation_status", outcome.ResultValidationStatus);
            writer.WriteString("render_status", outcome.RenderStatus);
            writer.WriteString("terminal_stage", outcome.TerminalStage);
            writer.WriteString("terminal_code", outcome.TerminalCode);
            writer.WriteNumber("result_byte_count", outcome.ResultByteCount);
            writer.WriteString("result_sha256", outcome.ResultSha256);
            writer.WriteString("spill_ref", outcome.SpillRef);
            writer.WriteEndObject();
        }

        return buffer.ToArray();
    }
}
