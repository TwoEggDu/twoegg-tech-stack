using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

internal static class Program
{
    private const string FixtureVersion = "lab05-fixture-v1";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false
    };

    private sealed record AssertionResult(string Id, string CaseId, bool Passed, string Message, string ArtifactRef);

    public static int Main(string[] args)
    {
        try
        {
            if (args.Length == 0)
            {
                Console.Error.WriteLine("Usage: verify-runtime | verify-artifacts | compare");
                return 2;
            }

            return args[0] switch
            {
                "verify-runtime" => VerifyRuntime(args),
                "verify-artifacts" => VerifyArtifactsCommand(args),
                "compare" => Compare(args),
                _ => FailUsage($"Unknown command: {args[0]}")
            };
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.ToString());
            return 2;
        }
    }

    private static int VerifyRuntime(string[] args)
    {
        string runtime = RequiredOption(args, "--runtime");
        string fixtures = RequiredOption(args, "--fixtures");
        string output = RequiredOption(args, "--output");
        ValidateObservationPath(output);
        Directory.CreateDirectory(output);

        string runtimeOutput = Path.Combine(output, "runtime-output");
        Directory.CreateDirectory(runtimeOutput);
        ProcessResult process = RunProcess("dotnet", new[] { runtime, "run", "--fixtures", fixtures, "--output", runtimeOutput });
        WriteText(Path.Combine(output, "runtime-stdout.txt"), process.Stdout);
        WriteText(Path.Combine(output, "runtime-stderr.txt"), process.Stderr);

        List<AssertionResult> assertions = new();
        if (process.ExitCode != 0)
        {
            foreach (string caseId in new[] { "A", "B", "C", "D", "E", "F", "G" })
            {
                assertions.Add(new AssertionResult(
                    $"{caseId}-PUBLIC-ARTIFACTS",
                    caseId,
                    false,
                    $"Runtime exit {process.ExitCode}; mandatory public artifacts for Case {caseId} are absent.",
                    $"runtime-output/{caseId}"));
            }
        }
        else
        {
            assertions.AddRange(VerifyArtifactSet(runtimeOutput));
        }

        WriteSpecResult(Path.Combine(output, "result.json"), assertions, process.ExitCode);
        PrintAssertions(assertions);
        return assertions.All(assertion => assertion.Passed) ? 0 : 1;
    }

    private static int VerifyArtifactsCommand(string[] args)
    {
        string input = RequiredOption(args, "--input");
        string report = RequiredOption(args, "--report");
        ValidateObservationPath(input);
        ValidateObservationPath(report);

        List<AssertionResult> assertions = VerifyArtifactSet(input);
        WriteSpecResult(report, assertions, 0);
        BuildManifest(input);
        PrintAssertions(assertions);
        return assertions.All(assertion => assertion.Passed) ? 0 : 1;
    }

    private static int Compare(string[] args)
    {
        string left = RequiredOption(args, "--left");
        string right = RequiredOption(args, "--right");
        string report = RequiredOption(args, "--report");
        ValidateObservationPath(left);
        ValidateObservationPath(right);
        ValidateObservationPath(report);

        Dictionary<string, string> leftFiles = EnumerateRelativeFiles(left);
        Dictionary<string, string> rightFiles = EnumerateRelativeFiles(right);
        SortedSet<string> paths = new(leftFiles.Keys, StringComparer.Ordinal);
        paths.UnionWith(rightFiles.Keys);

        JsonArray records = new();
        bool allEqual = true;
        foreach (string path in paths)
        {
            bool existsLeft = leftFiles.TryGetValue(path, out string? leftPath);
            bool existsRight = rightFiles.TryGetValue(path, out string? rightPath);
            byte[] leftBytes = existsLeft ? File.ReadAllBytes(leftPath!) : Array.Empty<byte>();
            byte[] rightBytes = existsRight ? File.ReadAllBytes(rightPath!) : Array.Empty<byte>();
            string leftHash = existsLeft ? Sha256(leftBytes) : "ABSENT";
            string rightHash = existsRight ? Sha256(rightBytes) : "ABSENT";
            bool directBytesEqual = existsLeft && existsRight && leftBytes.AsSpan().SequenceEqual(rightBytes);
            bool hashEqual = existsLeft && existsRight && string.Equals(leftHash, rightHash, StringComparison.Ordinal);
            bool lengthEqual = existsLeft && existsRight && leftBytes.Length == rightBytes.Length;
            bool equal = directBytesEqual && hashEqual && lengthEqual;
            allEqual &= equal;
            records.Add(new JsonObject
            {
                ["relative_path"] = path,
                ["left_length"] = existsLeft ? leftBytes.Length : -1,
                ["right_length"] = existsRight ? rightBytes.Length : -1,
                ["left_sha256"] = leftHash,
                ["right_sha256"] = rightHash,
                ["length_equal"] = lengthEqual,
                ["sha256_equal"] = hashEqual,
                ["direct_bytes_equal"] = directBytesEqual,
                ["status"] = equal ? "PASS" : "FAIL"
            });
        }

        string leftAggregate = ReadString(Path.Combine(left, "artifact-manifest.json"), "payload", "aggregate_sha256");
        string rightAggregate = ReadString(Path.Combine(right, "artifact-manifest.json"), "payload", "aggregate_sha256");
        bool aggregateEqual = string.Equals(leftAggregate, rightAggregate, StringComparison.Ordinal);
        allEqual &= aggregateEqual;

        JsonObject payload = new()
        {
            ["relative_file_set_equal"] = leftFiles.Keys.OrderBy(value => value, StringComparer.Ordinal)
                .SequenceEqual(rightFiles.Keys.OrderBy(value => value, StringComparer.Ordinal), StringComparer.Ordinal),
            ["left_aggregate_sha256"] = leftAggregate,
            ["right_aggregate_sha256"] = rightAggregate,
            ["aggregate_sha256_equal"] = aggregateEqual,
            ["all_direct_bytes_equal"] = records.All(node => node!["direct_bytes_equal"]!.GetValue<bool>()),
            ["files"] = records
        };
        WriteEnvelope(report, "lab05-repeatability-v1", "SUITE", "repeatability", payload, allEqual ? "PASS" : "FAIL");
        Console.WriteLine(allEqual ? "PASS repeatability" : "FAIL repeatability");
        return allEqual ? 0 : 1;
    }

    private static List<AssertionResult> VerifyArtifactSet(string root)
    {
        List<AssertionResult> results = new();
        foreach (string caseId in new[] { "A", "B", "C", "D", "E", "F", "G" })
        {
            Assert(results, $"{caseId}-FILES", caseId, $"{caseId}/case-result.json", () =>
            {
                foreach (string file in new[] { "contributors.json", "snapshot.json", "receipt.json", "diagnostics.json", "transform-events.json", "budget-result.json", "reconstruction-verdict.json", "case-result.json" })
                {
                    Require(File.Exists(Path.Combine(root, caseId, file)), $"Missing {caseId}/{file}");
                }
            });
        }

        Assert(results, "A-GOOD-CONTEXT", "A", "A/diagnostics.json", () =>
        {
            string[] codes = DiagnosticCodes(root, "A");
            Require(codes.SequenceEqual(new[] { "GOOD_CONTEXT" }), $"Expected only GOOD_CONTEXT, got {string.Join(',', codes)}");
            string[] selected = Payload(root, "A", "snapshot.json")["selected_contributor_ids"]!.AsArray().Select(Value).ToArray();
            Require(selected.Contains("A-GOAL") && selected.Contains("A-STATE") && selected.Contains("A-EVIDENCE") && selected.Contains("A-CAPABILITY"), "Baseline required contributors not retained.");
            Require(Payload(root, "A", "snapshot.json")["budget"]!["output_reserve"]!.GetValue<int>() == 5, "Baseline output reserve changed.");
        });

        Assert(results, "B-STALE-REVISION", "B", "B/diagnostics.json", () =>
        {
            string[] codes = DiagnosticCodes(root, "B");
            Require(codes.Contains("STALE") && codes.Contains("REVISION_MISMATCH"), "Both STALE and REVISION_MISMATCH are required.");
            JsonNode mismatch = Diagnostic(root, "B", "REVISION_MISMATCH");
            Require(Value(mismatch["expected"]) == "rev17" && Value(mismatch["actual"]) == "rev14", "Revision evidence must preserve expected rev17 and actual rev14.");
            JsonArray contributors = Records(root, "B", "contributors.json");
            JsonNode summary = contributors.Single(node => Value(node!["contributor_id"]) == "B-SUMMARY")!;
            Require(Value(summary["source_ref"]) == "state-summary" && Value(summary["source_revision"]) == "rev14", "Stale contributor provenance was lost.");
        });

        Assert(results, "C-THREE-POLLUTANTS", "C", "C/diagnostics.json", () =>
        {
            JsonNode pollution = Diagnostic(root, "C", "POLLUTION");
            string[] ids = pollution["contributor_ids"]!.AsArray().Select(Value).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            string[] expected = { "C-OBSOLETE-PLAN", "C-OLD-TOOL", "C-UNRELATED-HISTORY" };
            Require(ids.SequenceEqual(expected, StringComparer.Ordinal), $"Expected all three irrelevant contributors, got {string.Join(',', ids)}");
        });

        Assert(results, "D-CONFLICT-RETAINED", "D", "D/snapshot.json", () =>
        {
            Require(DiagnosticCodes(root, "D").Contains("CONFLICT_UNRESOLVED"), "Conflict diagnostic missing.");
            JsonNode snapshot = Payload(root, "D", "snapshot.json");
            string[] conflicts = snapshot["unresolved_conflict_ids"]!.AsArray().Select(Value).ToArray();
            Require(conflicts.SequenceEqual(new[] { "BUILD-OUTCOME-D" }), "Unresolved conflict marker missing.");
            JsonArray blocks = snapshot["materialized_blocks"]!.AsArray();
            Require(blocks.Any(node => Value(node!["content_bytes_utf8"]) == "Build failed." && Value(node["source_ref"]) == "build-job-41"), "Build failed provenance missing.");
            Require(blocks.Any(node => Value(node!["content_bytes_utf8"]) == "Build succeeded." && Value(node["source_ref"]) == "build-job-42"), "Build succeeded provenance missing.");
        });

        Assert(results, "E-COMPRESSION-LOSS", "E", "E/transform-events.json", () =>
        {
            Require(DiagnosticCodes(root, "E").Contains("COMPRESSION_LOSS"), "Compression loss was not detected.");
            JsonNode transform = Records(root, "E", "transform-events.json").Single()!;
            Require(Value(transform["mechanism"]) == "BAD_COMPRESSOR_V1", "Named fault mechanism changed.");
            Require(Value(transform["output_bytes_utf8"]) == "Root cause confirmed.", "Bad compressor output changed.");
            string[] loss = transform["lost_invariant_ids"]!.AsArray().Select(Value).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            string[] expectedLoss = { "CLAIM_STRENGTH", "CONFLICT", "PROVENANCE", "UNCERTAINTY" };
            Require(loss.SequenceEqual(expectedLoss, StringComparer.Ordinal), $"Expected uncertainty/conflict/provenance/claim-strength loss, got {string.Join(',', loss)}");
            JsonArray contributors = Records(root, "E", "contributors.json");
            Require(contributors.Any(node => Value(node!["stance"]) == "SUPPORTED") && contributors.Any(node => Value(node!["stance"]) == "CONTRADICTS") && contributors.Any(node => Value(node!["stance"]) == "UNKNOWN"), "Pre-transform structured evidence was not preserved.");
        });

        Assert(results, "F-BUDGET-FAIL-CLOSED", "F", "F/budget-result.json", () =>
        {
            JsonArray budgets = Records(root, "F", "budget-result.json");
            JsonNode packed = budgets.Single(node => Value(node!["scenario_id"]) == "optional-pressure")!;
            JsonNode overflow = budgets.Single(node => Value(node!["scenario_id"]) == "required-overflow")!;
            Require(Value(packed["status"]) == "PACKED" && packed["output_reserve"]!.GetValue<int>() == 4, "Optional-pressure path must pack while preserving reserve.");
            string[] selected = packed["selected_contributor_ids"]!.AsArray().Select(Value).ToArray();
            Require(selected.Contains("F-GOAL") && selected.Contains("F-STATE") && selected.Contains("F-EVIDENCE"), "P0/P1 contributors were trimmed.");
            Require(packed["omitted_contributor_ids"]!.AsArray().Select(Value).SequenceEqual(new[] { "F-HISTORY" }), "Optional history was not dropped first.");
            Require(Value(overflow["status"]) == "FAIL_CLOSED" && Value(overflow["failure_code"]) == "REQUIRED_EVIDENCE_BUDGET_EXCEEDED", "Required-overflow path did not fail closed.");
            Require(Value(Payload(root, "F", "snapshot-required-overflow.json")["snapshot"]) == "ABSENT", "Required-overflow path fabricated a Snapshot.");
            string[] codes = DiagnosticCodes(root, "F");
            Require(codes.Contains("BUDGET_OPTIONAL_OMITTED") && codes.Contains("REQUIRED_EVIDENCE_BUDGET_EXCEEDED"), "Budget diagnostics incomplete.");
        });

        Assert(results, "G-AUDITABLE-NOT-RECONSTRUCTABLE", "G", "G/reconstruction-verdict.json", () =>
        {
            JsonNode verdict = Payload(root, "G", "reconstruction-verdict.json");
            Require(Value(verdict["metadata_audit"]) == "AUDITABLE", "Receipt metadata should remain auditable.");
            Require(Value(verdict["byte_reconstruction"]) == "NOT_RECONSTRUCTABLE", "Missing bytes/locator must not be reconstructable.");
            Require(Value(verdict["provider_internal_context"]) == "UNKNOWN_UNSUPPORTED", "Provider/full-token ceiling was exceeded.");
            string[] reasons = verdict["reason_codes"]!.AsArray().Select(Value).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            string[] expected = { "DIGEST_NOT_CONTENT", "LOCATOR_UNRESOLVABLE", "ORIGINAL_BYTES_ABSENT" };
            Require(reasons.SequenceEqual(expected, StringComparer.Ordinal), $"Reconstruction reasons incomplete: {string.Join(',', reasons)}");
        });

        Assert(results, "SUITE-MANIFEST", "SUITE", "artifact-manifest.json", () => VerifyManifest(root));
        return results;
    }

    private static void VerifyManifest(string root)
    {
        string manifestPath = Path.Combine(root, "artifact-manifest.json");
        Require(File.Exists(manifestPath), "artifact-manifest.json missing.");
        JsonNode payload = PayloadFile(manifestPath);
        JsonArray files = payload["files"]!.AsArray();
        foreach (JsonNode? entry in files)
        {
            string relative = Value(entry!["relative_path"]);
            string fullPath = Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
            Require(File.Exists(fullPath), $"Manifest target missing: {relative}");
            byte[] bytes = File.ReadAllBytes(fullPath);
            Require(bytes.Length == entry["byte_length"]!.GetValue<int>(), $"Manifest length mismatch: {relative}");
            Require(Sha256(bytes) == Value(entry["sha256"]), $"Manifest hash mismatch: {relative}");
        }

        string aggregateInput = string.Concat(files.Select(entry => $"{Value(entry!["relative_path"])}\t{entry["byte_length"]!.GetValue<int>().ToString(CultureInfo.InvariantCulture)}\t{Value(entry["sha256"])}\n"));
        Require(Sha256(Encoding.UTF8.GetBytes(aggregateInput)) == Value(payload["aggregate_sha256"]), "Manifest aggregate mismatch.");
    }

    private static void BuildManifest(string root)
    {
        Dictionary<string, string> files = EnumerateRelativeFiles(root);
        files.Remove("artifact-manifest.json");
        JsonArray entries = new();
        StringBuilder aggregate = new();
        foreach ((string relative, string fullPath) in files.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            byte[] bytes = File.ReadAllBytes(fullPath);
            string hash = Sha256(bytes);
            entries.Add(new JsonObject
            {
                ["relative_path"] = relative,
                ["byte_length"] = bytes.Length,
                ["sha256"] = hash
            });
            aggregate.Append(relative).Append('\t').Append(bytes.Length.ToString(CultureInfo.InvariantCulture)).Append('\t').Append(hash).Append('\n');
        }

        JsonObject payload = new()
        {
            ["files"] = entries,
            ["aggregate_sha256"] = Sha256(Encoding.UTF8.GetBytes(aggregate.ToString()))
        };
        WriteEnvelope(Path.Combine(root, "artifact-manifest.json"), "lab05-artifact-manifest-v1", "SUITE", "artifact_manifest", payload, "PASS");
    }

    private static Dictionary<string, string> EnumerateRelativeFiles(string root)
    {
        return Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .ToDictionary(
                path => Path.GetRelativePath(root, path).Replace('\\', '/'),
                path => path,
                StringComparer.Ordinal);
    }

    private static string[] DiagnosticCodes(string root, string caseId) =>
        Records(root, caseId, "diagnostics.json").Select(node => Value(node!["code"])).OrderBy(value => value, StringComparer.Ordinal).ToArray();

    private static JsonNode Diagnostic(string root, string caseId, string code) =>
        Records(root, caseId, "diagnostics.json").Single(node => Value(node!["code"]) == code)!;

    private static JsonArray Records(string root, string caseId, string fileName) =>
        JsonNode.Parse(File.ReadAllBytes(Path.Combine(root, caseId, fileName)))!["records"]!.AsArray();

    private static JsonNode Payload(string root, string caseId, string fileName) =>
        PayloadFile(Path.Combine(root, caseId, fileName));

    private static JsonNode PayloadFile(string path) => JsonNode.Parse(File.ReadAllBytes(path))!["payload"]!;

    private static string ReadString(string path, params string[] segments)
    {
        JsonNode node = JsonNode.Parse(File.ReadAllBytes(path))!;
        foreach (string segment in segments)
        {
            node = node[segment]!;
        }
        return Value(node);
    }

    private static void Assert(List<AssertionResult> results, string id, string caseId, string artifactRef, Action assertion)
    {
        try
        {
            assertion();
            results.Add(new AssertionResult(id, caseId, true, "Behavioral assertion passed.", artifactRef));
        }
        catch (Exception exception)
        {
            results.Add(new AssertionResult(id, caseId, false, exception.Message, artifactRef));
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void WriteSpecResult(string path, IReadOnlyList<AssertionResult> assertions, int runtimeExit)
    {
        JsonArray records = new();
        foreach (AssertionResult assertion in assertions)
        {
            records.Add(new JsonObject
            {
                ["assertion_id"] = assertion.Id,
                ["case_id"] = assertion.CaseId,
                ["status"] = assertion.Passed ? "PASS" : "FAIL",
                ["message"] = assertion.Message,
                ["artifact_ref"] = assertion.ArtifactRef
            });
        }
        JsonObject payload = new()
        {
            ["runtime_exit_code"] = runtimeExit,
            ["assertion_total"] = assertions.Count,
            ["assertion_passed"] = assertions.Count(assertion => assertion.Passed),
            ["assertion_failed"] = assertions.Count(assertion => !assertion.Passed),
            ["assertions"] = records
        };
        WriteEnvelope(path, "lab05-spec-result-v1", "SUITE", "spec_result", payload, assertions.All(assertion => assertion.Passed) ? "PASS" : "FAIL");
    }

    private static void WriteEnvelope(string path, string schema, string caseId, string kind, JsonNode payload, string status)
    {
        JsonObject envelope = new()
        {
            ["schema_version"] = schema,
            ["fixture_version"] = FixtureVersion,
            ["case_id"] = caseId,
            ["artifact_kind"] = kind,
            ["payload"] = payload,
            ["status"] = status,
            ["unexpected_failures"] = new JsonArray()
        };
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(envelope, JsonOptions);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllBytes(path, AppendLf(bytes));
    }

    private static ProcessResult RunProcess(string fileName, IReadOnlyList<string> arguments)
    {
        ProcessStartInfo start = new(fileName)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Environment.CurrentDirectory
        };
        foreach (string argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }
        using Process process = Process.Start(start) ?? throw new InvalidOperationException("Unable to start Runtime process.");
        string stdout = process.StandardOutput.ReadToEnd();
        string stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return new ProcessResult(process.ExitCode, stdout, stderr);
    }

    private static void PrintAssertions(IEnumerable<AssertionResult> assertions)
    {
        foreach (AssertionResult assertion in assertions)
        {
            Console.WriteLine($"{(assertion.Passed ? "PASS" : "FAIL")} [{assertion.CaseId}] {assertion.Id}: {assertion.Message}");
        }
    }

    private static string RequiredOption(string[] args, string name)
    {
        int index = Array.IndexOf(args, name);
        if (index < 0 || index + 1 >= args.Length)
        {
            throw new ArgumentException($"Missing required option {name}");
        }
        return args[index + 1];
    }

    private static void ValidateObservationPath(string path)
    {
        string labRoot = Path.GetFullPath(Environment.CurrentDirectory);
        string observationsRoot = Path.GetFullPath(Path.Combine(labRoot, "observations")) + Path.DirectorySeparatorChar;
        string fullPath = Path.GetFullPath(path);
        if (!fullPath.StartsWith(observationsRoot, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(fullPath, observationsRoot.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Output path must remain below observations/: {path}");
        }
    }

    private static string Value(JsonNode? node) => node?.GetValue<string>() ?? throw new InvalidOperationException("Expected string value.");
    private static byte[] AppendLf(byte[] bytes) => bytes.Length > 0 && bytes[^1] == (byte)'\n' ? bytes : bytes.Concat(new[] { (byte)'\n' }).ToArray();
    private static string Sha256(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    private static void WriteText(string path, string value) => File.WriteAllText(path, value.Replace("\r\n", "\n"), new UTF8Encoding(false));
    private static int FailUsage(string message) { Console.Error.WriteLine(message); return 2; }
    private sealed record ProcessResult(int ExitCode, string Stdout, string Stderr);
}
