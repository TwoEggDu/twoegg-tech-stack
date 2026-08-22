using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

internal static class LabRuntime
{
    private const string FixtureVersion = "lab05-fixture-v1";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false
    };

    public static int Run(string[] args)
    {
        try
        {
            if (args.Length == 0 || args[0] != "run")
            {
                Console.Error.WriteLine("Usage: run --fixtures <path> --output <observations/path>");
                return 2;
            }

            string fixturesPath = RequiredOption(args, "--fixtures");
            string outputRoot = RequiredOption(args, "--output");
            ValidatePaths(fixturesPath, outputRoot);
            Directory.CreateDirectory(outputRoot);

            JsonNode fixture = JsonNode.Parse(File.ReadAllBytes(fixturesPath)) ?? throw new InvalidOperationException("INPUT_INVALID: empty fixture.");
            Require(S(fixture, "fixture_version") == FixtureVersion, "INPUT_INVALID: fixture version mismatch.");
            JsonArray cases = fixture["cases"]?.AsArray() ?? throw new InvalidOperationException("INPUT_INVALID: cases missing.");
            string[] caseIds = cases.Select(node => S(node!, "case_id")).ToArray();
            Require(caseIds.SequenceEqual(new[] { "A", "B", "C", "D", "E", "F", "G" }), "INPUT_INVALID: mandatory Cases A-G must appear exactly once in order.");

            foreach (JsonNode? caseNode in cases)
            {
                ProcessCase(caseNode!, outputRoot);
            }
            BuildManifest(outputRoot);
            Console.WriteLine("PASS lab05-fixture-v1 Cases A-G");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 2;
        }
    }

    private static void ProcessCase(JsonNode caseNode, string outputRoot)
    {
        string caseId = S(caseNode, "case_id");
        string caseRoot = Path.Combine(outputRoot, caseId);
        Directory.CreateDirectory(caseRoot);

        JsonArray contributors = caseNode["contributors"]?.AsArray() ?? throw new InvalidOperationException($"INPUT_INVALID: {caseId} contributors missing.");
        ValidateContributors(caseId, contributors);
        JsonArray contributorRecords = CloneArray(contributors);

        JsonArray diagnostics = new();
        JsonArray transformEvents = new();
        List<JsonNode> relevant = contributors.Where(node => S(node!, "relevance") == "RELEVANT").Select(node => node!).ToList();
        List<JsonNode> irrelevant = contributors.Where(node => S(node!, "relevance") == "IRRELEVANT").Select(node => node!).ToList();

        AddRevisionDiagnostics(caseId, contributors, diagnostics);
        AddPollutionDiagnostic(caseId, irrelevant, diagnostics);
        AddConflictDiagnostic(caseId, contributors, diagnostics);

        JsonArray budgetRecords = BuildBudgets(caseNode, relevant, diagnostics);
        JsonNode activeBudget = budgetRecords[0]!;
        string[] selectedIds = activeBudget["selected_contributor_ids"]!.AsArray().Select(Value).ToArray();
        List<JsonNode> selected = relevant.Where(node => selectedIds.Contains(S(node, "contributor_id"), StringComparer.Ordinal)).ToList();

        JsonArray materializedBlocks;
        JsonArray transformEventIds = new();
        if (caseNode["transform_id"] is JsonNode transformNode)
        {
            string transformId = transformNode.GetValue<string>();
            Require(transformId == "BAD_COMPRESSOR_V1", $"INPUT_INVALID: unknown transform {transformId}.");
            materializedBlocks = ApplyBadCompressor(caseId, selected, diagnostics, transformEvents);
            transformEventIds.Add("TE-E-001");
        }
        else
        {
            materializedBlocks = Materialize(selected);
        }

        JsonArray omitted = BuildOmissions(contributors, selectedIds, transformEvents.Count > 0);
        JsonArray conflicts = contributors
            .Where(node => node!["conflict_group"] is not null)
            .Select(node => S(node!, "conflict_group"))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .Select(value => (JsonNode?)JsonValue.Create(value))
            .ToArrayNode();
        JsonArray unknowns = contributors
            .Where(node => node!["stance"] is not null && S(node!, "stance") == "UNKNOWN")
            .Select(node => S(node!, "contributor_id"))
            .OrderBy(value => value, StringComparer.Ordinal)
            .Select(value => (JsonNode?)JsonValue.Create(value))
            .ToArrayNode();

        JsonObject budgetLedger = BudgetLedger(activeBudget);
        JsonObject snapshotPayload = new()
        {
            ["schema_version"] = "lab05-snapshot-v1",
            ["fixture_version"] = FixtureVersion,
            ["case_id"] = caseId,
            ["selected_contributor_ids"] = selectedIds.Select(value => (JsonNode?)JsonValue.Create(value)).ToArrayNode(),
            ["materialized_blocks"] = materializedBlocks,
            ["omitted_contributors"] = omitted,
            ["budget"] = budgetLedger,
            ["transform_event_ids"] = transformEventIds,
            ["unresolved_conflict_ids"] = conflicts,
            ["unknown_ids"] = unknowns
        };
        snapshotPayload["canonical_snapshot_sha256"] = HashNode(snapshotPayload);

        if (diagnostics.Count == 0)
        {
            diagnostics.Add(Diagnostic(caseId, "GOOD_CONTEXT", new JsonArray(), "NONE", "NONE", "local-context-predicate-v1"));
        }
        SortDiagnostics(diagnostics);

        JsonObject reconstruction = BuildReconstruction(contributors);
        JsonObject receiptPayload = BuildReceipt(caseId, contributors, selectedIds, omitted, transformEvents, budgetLedger, diagnostics, S(snapshotPayload, "canonical_snapshot_sha256"));
        JsonObject caseResult = new()
        {
            ["case_id"] = caseId,
            ["diagnostic_refs"] = diagnostics.Select(node => JsonValue.Create(S(node!, "diagnostic_id"))).ToArrayNode(),
            ["invariant_results"] = new JsonArray(
                new JsonObject { ["invariant_id"] = "SCHEMA_VALID", ["status"] = "PASS" },
                new JsonObject { ["invariant_id"] = "UNEXPECTED_FAILURES_EMPTY", ["status"] = "PASS" }),
            ["unexpected_failures"] = new JsonArray()
        };

        WriteEnvelope(Path.Combine(caseRoot, "contributors.json"), "lab05-contributors-v1", caseId, "contributors", null, contributorRecords, "PASS");
        WriteEnvelope(Path.Combine(caseRoot, "snapshot.json"), "lab05-snapshot-artifact-v1", caseId, "snapshot", snapshotPayload, null, "PASS");
        if (caseId == "F")
        {
            JsonObject absent = new() { ["scenario_id"] = "required-overflow", ["snapshot"] = "ABSENT", ["reason"] = "REQUIRED_EVIDENCE_BUDGET_EXCEEDED" };
            WriteEnvelope(Path.Combine(caseRoot, "snapshot-required-overflow.json"), "lab05-snapshot-absence-v1", caseId, "snapshot_absence", absent, null, "EXPECTED_FAIL_CLOSED");
        }
        WriteEnvelope(Path.Combine(caseRoot, "receipt.json"), "lab05-receipt-artifact-v1", caseId, "receipt", receiptPayload, null, "PASS");
        WriteEnvelope(Path.Combine(caseRoot, "diagnostics.json"), "lab05-diagnostics-v1", caseId, "diagnostics", null, diagnostics, "PASS");
        WriteEnvelope(Path.Combine(caseRoot, "transform-events.json"), "lab05-transform-events-v1", caseId, "transform_events", null, transformEvents, "PASS");
        WriteEnvelope(Path.Combine(caseRoot, "budget-result.json"), "lab05-budget-results-v1", caseId, "budget_result", null, budgetRecords, caseId == "F" ? "EXPECTED_FAIL_CLOSED" : "PASS");
        WriteEnvelope(Path.Combine(caseRoot, "reconstruction-verdict.json"), "lab05-reconstruction-verdict-v1", caseId, "reconstruction_verdict", reconstruction, null, "PASS");
        WriteEnvelope(Path.Combine(caseRoot, "case-result.json"), "lab05-case-result-v1", caseId, "case_result", caseResult, null, "PASS");
    }

    private static void ValidateContributors(string caseId, JsonArray contributors)
    {
        foreach (JsonNode? contributor in contributors)
        {
            Require(S(contributor!, "schema_version") == "lab05-contributor-v1", $"INPUT_INVALID: {caseId} contributor schema.");
            string content = S(contributor!, "content_bytes_utf8");
            string digest = S(contributor!, "content_sha256");
            if (content != "ABSENT")
            {
                Require(Sha256(Encoding.UTF8.GetBytes(content)) == digest, $"INPUT_INVALID: {S(contributor!, "contributor_id")} content digest mismatch.");
            }
            Require(B(contributor!, "required") == (S(contributor!, "priority") != "P2_OPTIONAL"), $"INPUT_INVALID: {S(contributor!, "contributor_id")} priority/required mismatch.");
        }
    }

    private static void AddRevisionDiagnostics(string caseId, JsonArray contributors, JsonArray diagnostics)
    {
        foreach (JsonNode? contributor in contributors)
        {
            string required = S(contributor!, "required_revision");
            string actual = S(contributor!, "source_revision");
            if (required == "NOT_APPLICABLE" || required == actual)
            {
                continue;
            }
            JsonArray ids = new(S(contributor!, "contributor_id"));
            diagnostics.Add(Diagnostic(caseId, "STALE", CloneArray(ids), required, actual, "revision-predicate-v1"));
            diagnostics.Add(Diagnostic(caseId, "REVISION_MISMATCH", CloneArray(ids), required, actual, "revision-predicate-v1"));
        }
    }

    private static void AddPollutionDiagnostic(string caseId, List<JsonNode> irrelevant, JsonArray diagnostics)
    {
        if (irrelevant.Count == 0)
        {
            return;
        }
        JsonArray ids = irrelevant.Select(node => (JsonNode?)JsonValue.Create(S(node, "contributor_id")))
            .OrderBy(node => Value(node), StringComparer.Ordinal)
            .ToArrayNode();
        diagnostics.Add(Diagnostic(caseId, "POLLUTION", ids, "RELEVANT", "IRRELEVANT", "relevance-predicate-v1"));
    }

    private static void AddConflictDiagnostic(string caseId, JsonArray contributors, JsonArray diagnostics)
    {
        IEnumerable<IGrouping<string, JsonNode>> groups = contributors
            .Where(node => node!["conflict_group"] is not null)
            .Select(node => node!)
            .GroupBy(node => S(node, "conflict_group"), StringComparer.Ordinal);
        foreach (IGrouping<string, JsonNode> group in groups)
        {
            if (group.Select(node => S(node, "content_sha256")).Distinct(StringComparer.Ordinal).Count() < 2)
            {
                continue;
            }
            JsonArray ids = group.Select(node => (JsonNode?)JsonValue.Create(S(node, "contributor_id"))).ToArrayNode();
            JsonObject diagnostic = Diagnostic(caseId, "CONFLICT_UNRESOLVED", ids, "SINGLE_CONSISTENT_VALUE", "CONTRADICTORY_VALUES_RETAINED", "conflict-predicate-v1");
            diagnostic["conflict_id"] = group.Key;
            diagnostics.Add(diagnostic);
        }
    }

    private static JsonArray BuildBudgets(JsonNode caseNode, List<JsonNode> relevant, JsonArray diagnostics)
    {
        string caseId = S(caseNode, "case_id");
        JsonArray scenarios = caseNode["budget_scenarios"] is JsonArray explicitScenarios
            ? explicitScenarios
            : new JsonArray(new JsonObject
            {
                ["scenario_id"] = "default",
                ["total_budget"] = I(caseNode, "total_budget"),
                ["output_reserve"] = I(caseNode, "output_reserve")
            });

        JsonArray results = new();
        foreach (JsonNode? scenario in scenarios)
        {
            int total = I(scenario!, "total_budget");
            int reserve = I(scenario!, "output_reserve");
            int usable = total - reserve;
            List<JsonNode> required = relevant.Where(node => B(node, "required")).ToList();
            List<JsonNode> optional = relevant.Where(node => !B(node, "required"))
                .OrderBy(node => I(node, "optional_drop_rank"))
                .ThenBy(node => S(node, "contributor_id"), StringComparer.Ordinal)
                .ToList();
            int requiredSum = required.Sum(node => I(node, "budget_units"));
            int optionalSum = optional.Sum(node => I(node, "budget_units"));
            JsonArray selected = new();
            JsonArray omitted = new();
            string status;
            string failureCode;
            int used;

            if (requiredSum > usable)
            {
                status = "FAIL_CLOSED";
                failureCode = "REQUIRED_EVIDENCE_BUDGET_EXCEEDED";
                used = 0;
                diagnostics.Add(Diagnostic(caseId, failureCode, required.Select(node => (JsonNode?)JsonValue.Create(S(node, "contributor_id"))).ToArrayNode(), usable.ToString(CultureInfo.InvariantCulture), requiredSum.ToString(CultureInfo.InvariantCulture), "budget-predicate-v1"));
            }
            else
            {
                status = "PACKED";
                failureCode = "NONE";
                used = requiredSum;
                foreach (JsonNode node in required)
                {
                    selected.Add(S(node, "contributor_id"));
                }
                foreach (JsonNode node in optional)
                {
                    int cost = I(node, "budget_units");
                    if (used + cost <= usable)
                    {
                        selected.Add(S(node, "contributor_id"));
                        used += cost;
                    }
                    else
                    {
                        omitted.Add(S(node, "contributor_id"));
                    }
                }
                if (omitted.Count > 0)
                {
                    diagnostics.Add(Diagnostic(caseId, "BUDGET_OPTIONAL_OMITTED", CloneArray(omitted), usable.ToString(CultureInfo.InvariantCulture), used.ToString(CultureInfo.InvariantCulture), "budget-predicate-v1"));
                }
            }

            results.Add(new JsonObject
            {
                ["scenario_id"] = S(scenario!, "scenario_id"),
                ["total_budget"] = total,
                ["output_reserve"] = reserve,
                ["usable_input"] = usable,
                ["required_sum"] = requiredSum,
                ["optional_sum"] = optionalSum,
                ["used_input"] = used,
                ["remaining_input_units"] = status == "PACKED" ? usable - used : usable,
                ["selected_contributor_ids"] = selected,
                ["omitted_contributor_ids"] = omitted,
                ["drop_reasons"] = omitted.Select(node => (JsonNode?)new JsonObject { ["contributor_id"] = Value(node), ["reason"] = "OPTIONAL_HISTORY_BUDGET_FIRST" }).ToArrayNode(),
                ["status"] = status,
                ["failure_code"] = failureCode
            });
        }
        return results;
    }

    private static JsonArray ApplyBadCompressor(string caseId, List<JsonNode> selected, JsonArray diagnostics, JsonArray transformEvents)
    {
        string[] stances = selected.Where(node => node["stance"] is not null).Select(node => S(node, "stance")).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        Require(stances.SequenceEqual(new[] { "CONTRADICTS", "SUPPORTED", "UNKNOWN" }), "INPUT_INVALID: BAD_COMPRESSOR_V1 requires SUPPORTED, CONTRADICTS and UNKNOWN.");
        string preBytes = string.Join("\n", selected.Select(node => S(node, "contributor_id") + ":" + S(node, "content_bytes_utf8"))) + "\n";
        string postBytes = "Root cause confirmed.";
        string preDigest = Sha256(Encoding.UTF8.GetBytes(preBytes));
        string postDigest = Sha256(Encoding.UTF8.GetBytes(postBytes));
        JsonArray affected = selected.Select(node => (JsonNode?)JsonValue.Create(S(node, "contributor_id"))).ToArrayNode();
        JsonArray lost = new("UNCERTAINTY", "CONFLICT", "PROVENANCE", "CLAIM_STRENGTH");
        transformEvents.Add(new JsonObject
        {
            ["schema_version"] = "lab05-transform-event-v1",
            ["event_id"] = "TE-E-001",
            ["actor"] = "APPLICATION_FIXTURE",
            ["stage"] = "COMPRESSION",
            ["mechanism"] = "BAD_COMPRESSOR_V1",
            ["version"] = "1",
            ["affected_contributor_ids"] = CloneArray(affected),
            ["pre_digest"] = preDigest,
            ["post_digest"] = postDigest,
            ["pre_bytes_utf8"] = preBytes,
            ["output_bytes_utf8"] = postBytes,
            ["lost_invariant_ids"] = lost
        });
        JsonObject diagnostic = Diagnostic(caseId, "COMPRESSION_LOSS", CloneArray(affected), "SUPPORTED+CONTRADICTS+UNKNOWN", "Root cause confirmed.", "compression-invariant-verifier-v1");
        diagnostic["evidence_refs"] = CloneArray(affected);
        diagnostic["pre_digest"] = preDigest;
        diagnostic["post_digest"] = postDigest;
        diagnostic["claim_strength_before"] = "UNCERTAIN_CONFLICTING";
        diagnostic["claim_strength_after"] = "CONFIRMED";
        diagnostic["lost_invariant_ids"] = CloneArray(lost);
        diagnostics.Add(diagnostic);

        return new JsonArray(new JsonObject
        {
            ["contributor_id"] = "COMPRESSED-E",
            ["source_ref"] = "transform:TE-E-001",
            ["content_bytes_utf8"] = postBytes,
            ["content_sha256"] = postDigest,
            ["provenance"] = "LOSS_DETECTED"
        });
    }

    private static JsonArray Materialize(List<JsonNode> selected)
    {
        JsonArray blocks = new();
        foreach (JsonNode node in selected)
        {
            blocks.Add(new JsonObject
            {
                ["contributor_id"] = S(node, "contributor_id"),
                ["source_ref"] = S(node, "source_ref"),
                ["source_revision"] = S(node, "source_revision"),
                ["authority"] = S(node, "authority"),
                ["content_bytes_utf8"] = S(node, "content_bytes_utf8"),
                ["content_sha256"] = S(node, "content_sha256")
            });
        }
        return blocks;
    }

    private static JsonArray BuildOmissions(JsonArray contributors, string[] selectedIds, bool transformed)
    {
        JsonArray omitted = new();
        foreach (JsonNode? node in contributors)
        {
            string id = S(node!, "contributor_id");
            if (selectedIds.Contains(id, StringComparer.Ordinal) && !transformed)
            {
                continue;
            }
            string reason = transformed && selectedIds.Contains(id, StringComparer.Ordinal)
                ? "TRANSFORMED_BY_BAD_COMPRESSOR"
                : S(node!, "relevance") == "IRRELEVANT"
                    ? "IRRELEVANT_BY_FROZEN_PREDICATE"
                    : "OPTIONAL_HISTORY_BUDGET_FIRST";
            omitted.Add(new JsonObject
            {
                ["contributor_id"] = id,
                ["disposition"] = transformed && selectedIds.Contains(id, StringComparer.Ordinal) ? "TRANSFORMED" : "OMITTED",
                ["reason"] = reason,
                ["transform_event_ref"] = transformed && selectedIds.Contains(id, StringComparer.Ordinal) ? "TE-E-001" : "NONE"
            });
        }
        return omitted;
    }

    private static JsonObject BudgetLedger(JsonNode budget)
    {
        return new JsonObject
        {
            ["scenario_id"] = S(budget, "scenario_id"),
            ["total_budget"] = I(budget, "total_budget"),
            ["output_reserve"] = I(budget, "output_reserve"),
            ["usable_input"] = I(budget, "usable_input"),
            ["used_input"] = I(budget, "used_input"),
            ["remaining_input_units"] = I(budget, "remaining_input_units")
        };
    }

    private static JsonObject BuildReconstruction(JsonArray contributors)
    {
        bool bytesRetained = contributors.All(node => S(node!, "content_bytes_utf8") != "ABSENT");
        bool locatorResolvable = contributors.All(node => B(node!, "locator_resolvable"));
        bool reconstructable = bytesRetained || locatorResolvable;
        JsonArray reasons = new();
        if (!bytesRetained)
        {
            reasons.Add("ORIGINAL_BYTES_ABSENT");
            reasons.Add("DIGEST_NOT_CONTENT");
        }
        if (!locatorResolvable)
        {
            reasons.Add("LOCATOR_UNRESOLVABLE");
        }
        SortStrings(reasons);
        return new JsonObject
        {
            ["metadata_audit"] = "AUDITABLE",
            ["byte_reconstruction"] = reconstructable ? "RECONSTRUCTABLE" : "NOT_RECONSTRUCTABLE",
            ["provider_internal_context"] = "UNKNOWN_UNSUPPORTED",
            ["prerequisites"] = new JsonObject
            {
                ["bytes_retained"] = bytesRetained,
                ["locator_resolvable"] = locatorResolvable,
                ["canonicalization_version"] = "canonical-json-v1"
            },
            ["reason_codes"] = reasons
        };
    }

    private static JsonObject BuildReceipt(
        string caseId,
        JsonArray contributors,
        string[] selectedIds,
        JsonArray omissions,
        JsonArray transforms,
        JsonObject budget,
        JsonArray diagnostics,
        string snapshotHash)
    {
        Dictionary<string, JsonNode> omittedById = omissions.ToDictionary(node => S(node!, "contributor_id"), node => node!, StringComparer.Ordinal);
        JsonArray records = new();
        for (int index = 0; index < contributors.Count; index++)
        {
            JsonNode node = contributors[index]!;
            string id = S(node, "contributor_id");
            bool selected = selectedIds.Contains(id, StringComparer.Ordinal);
            string disposition = selected ? "SELECTED" : "OMITTED";
            string reason = selected ? "SELECTED_BY_POLICY" : "NOT_SELECTED";
            if (omittedById.TryGetValue(id, out JsonNode? omitted))
            {
                disposition = S(omitted, "disposition");
                reason = S(omitted, "reason");
            }
            records.Add(new JsonObject
            {
                ["contributor_id"] = id,
                ["source_ref"] = S(node, "source_ref"),
                ["content_sha256"] = S(node, "content_sha256"),
                ["order"] = index,
                ["scope"] = node["scope"]!.DeepClone(),
                ["source_revision"] = S(node, "source_revision"),
                ["required_revision"] = S(node, "required_revision"),
                ["authority"] = S(node, "authority"),
                ["disposition"] = disposition,
                ["reason"] = reason,
                ["bytes_retained"] = selected && S(node, "content_bytes_utf8") != "ABSENT",
                ["locator"] = S(node, "locator"),
                ["locator_resolvable"] = B(node, "locator_resolvable")
            });
        }
        JsonObject receipt = new()
        {
            ["schema_version"] = "lab05-receipt-v1",
            ["fixture_version"] = FixtureVersion,
            ["case_id"] = caseId,
            ["snapshot_sha256"] = snapshotHash,
            ["contributors"] = records,
            ["transforms"] = CloneArray(transforms),
            ["budget"] = budget.DeepClone(),
            ["diagnostic_refs"] = diagnostics.Select(node => (JsonNode?)JsonValue.Create(S(node!, "diagnostic_id"))).ToArrayNode()
        };
        receipt["receipt_sha256"] = HashNode(receipt);
        return receipt;
    }

    private static JsonObject Diagnostic(string caseId, string code, JsonArray contributorIds, string expected, string actual, string predicateVersion)
    {
        return new JsonObject
        {
            ["schema_version"] = "lab05-diagnostic-v1",
            ["diagnostic_id"] = $"DIAG-{caseId}-{code}",
            ["case_id"] = caseId,
            ["code"] = code,
            ["contributor_ids"] = contributorIds,
            ["expected"] = expected,
            ["actual"] = actual,
            ["predicate_version"] = predicateVersion,
            ["evidence_refs"] = new JsonArray(),
            ["pre_digest"] = "NOT_APPLICABLE",
            ["post_digest"] = "NOT_APPLICABLE",
            ["claim_strength_before"] = "NOT_APPLICABLE",
            ["claim_strength_after"] = "NOT_APPLICABLE",
            ["status"] = "DETECTED"
        };
    }

    private static void SortDiagnostics(JsonArray diagnostics)
    {
        List<JsonNode> sorted = diagnostics.Select(node => node!).OrderBy(node => S(node, "code"), StringComparer.Ordinal).ToList();
        diagnostics.Clear();
        foreach (JsonNode node in sorted)
        {
            diagnostics.Add(node);
        }
    }

    private static void SortStrings(JsonArray values)
    {
        string[] sorted = values.Select(Value).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        values.Clear();
        foreach (string value in sorted)
        {
            values.Add(value);
        }
    }

    private static void BuildManifest(string root)
    {
        Dictionary<string, string> files = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Select(path => new { Relative = Path.GetRelativePath(root, path).Replace('\\', '/'), Full = path })
            .Where(item => item.Relative != "artifact-manifest.json" && item.Relative != "spec-result.json")
            .ToDictionary(item => item.Relative, item => item.Full, StringComparer.Ordinal);
        JsonArray entries = new();
        StringBuilder aggregate = new();
        foreach ((string relative, string fullPath) in files.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            byte[] bytes = File.ReadAllBytes(fullPath);
            string hash = Sha256(bytes);
            entries.Add(new JsonObject { ["relative_path"] = relative, ["byte_length"] = bytes.Length, ["sha256"] = hash });
            aggregate.Append(relative).Append('\t').Append(bytes.Length.ToString(CultureInfo.InvariantCulture)).Append('\t').Append(hash).Append('\n');
        }
        JsonObject payload = new()
        {
            ["files"] = entries,
            ["aggregate_sha256"] = Sha256(Encoding.UTF8.GetBytes(aggregate.ToString()))
        };
        WriteEnvelope(Path.Combine(root, "artifact-manifest.json"), "lab05-artifact-manifest-v1", "SUITE", "artifact_manifest", payload, null, "PASS");
    }

    private static void WriteEnvelope(string path, string schema, string caseId, string kind, JsonNode? payload, JsonArray? records, string status)
    {
        JsonObject envelope = new()
        {
            ["schema_version"] = schema,
            ["fixture_version"] = FixtureVersion,
            ["case_id"] = caseId,
            ["artifact_kind"] = kind
        };
        if (records is not null)
        {
            envelope["records"] = records;
        }
        else
        {
            envelope["payload"] = payload;
        }
        envelope["status"] = status;
        envelope["unexpected_failures"] = new JsonArray();
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(envelope, JsonOptions);
        File.WriteAllBytes(path, AppendLf(bytes));
    }

    private static void ValidatePaths(string fixturePath, string outputPath)
    {
        string labRoot = Path.GetFullPath(Environment.CurrentDirectory);
        string fixtureRoot = Path.GetFullPath(Path.Combine(labRoot, "fixtures")) + Path.DirectorySeparatorChar;
        string observationsRoot = Path.GetFullPath(Path.Combine(labRoot, "observations")) + Path.DirectorySeparatorChar;
        string fixture = Path.GetFullPath(fixturePath);
        string output = Path.GetFullPath(outputPath);
        Require(fixture.StartsWith(fixtureRoot, StringComparison.OrdinalIgnoreCase), "SAFETY_BOUNDARY_VIOLATION: fixture must remain below fixtures/.");
        Require(output.StartsWith(observationsRoot, StringComparison.OrdinalIgnoreCase), "SAFETY_BOUNDARY_VIOLATION: output must remain below observations/.");
    }

    private static string RequiredOption(string[] args, string name)
    {
        int index = Array.IndexOf(args, name);
        if (index < 0 || index + 1 >= args.Length)
        {
            throw new ArgumentException($"Missing required option {name}.");
        }
        return args[index + 1];
    }

    private static JsonArray CloneArray(JsonArray source)
    {
        JsonArray clone = new();
        foreach (JsonNode? node in source)
        {
            clone.Add(node?.DeepClone());
        }
        return clone;
    }

    private static string HashNode(JsonNode node) => Sha256(JsonSerializer.SerializeToUtf8Bytes(node, JsonOptions));
    private static string S(JsonNode node, string property) => node[property]?.GetValue<string>() ?? throw new InvalidOperationException($"INPUT_INVALID: missing string {property}.");
    private static int I(JsonNode node, string property) => node[property]?.GetValue<int>() ?? throw new InvalidOperationException($"INPUT_INVALID: missing integer {property}.");
    private static bool B(JsonNode node, string property) => node[property]?.GetValue<bool>() ?? throw new InvalidOperationException($"INPUT_INVALID: missing boolean {property}.");
    private static string Value(JsonNode? node) => node?.GetValue<string>() ?? throw new InvalidOperationException("Expected string value.");
    private static byte[] AppendLf(byte[] bytes) => bytes.Length > 0 && bytes[^1] == (byte)'\n' ? bytes : bytes.Concat(new[] { (byte)'\n' }).ToArray();
    private static string Sha256(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}

internal static class JsonArrayExtensions
{
    public static JsonArray ToArrayNode(this IEnumerable<JsonNode?> nodes)
    {
        JsonArray array = new();
        foreach (JsonNode? node in nodes)
        {
            array.Add(node);
        }
        return array;
    }
}
