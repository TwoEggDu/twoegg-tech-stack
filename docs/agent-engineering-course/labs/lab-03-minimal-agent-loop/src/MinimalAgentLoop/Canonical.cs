using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MinimalAgentLoop;

public static class Canonical
{
    public static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false
    };

    public static string Json(object? value)
    {
        JsonNode? node = JsonSerializer.SerializeToNode(value, JsonOptions);
        return Sort(node)?.ToJsonString(JsonOptions) ?? "null";
    }

    public static string Json(JsonElement value)
    {
        JsonNode? node = JsonNode.Parse(value.GetRawText());
        return Sort(node)?.ToJsonString(JsonOptions) ?? "null";
    }

    public static string Sha256(string value) => Sha256(Utf8NoBom.GetBytes(value));

    public static string Sha256(byte[] value) => Convert.ToHexString(SHA256.HashData(value));

    public static string FileSha256(string path) => Sha256(File.ReadAllBytes(path));

    public static void WriteJsonLines(string path, IEnumerable<SortedDictionary<string, object?>> records)
    {
        string text = string.Join('\n', records.Select(Json)) + "\n";
        File.WriteAllText(path, text, Utf8NoBom);
    }

    public static void WriteJson(string path, object value)
    {
        File.WriteAllText(path, Json(value) + "\n", Utf8NoBom);
    }

    private static JsonNode? Sort(JsonNode? node)
    {
        return node switch
        {
            null => null,
            JsonObject obj => SortObject(obj),
            JsonArray array => new JsonArray(array.Select(item => Sort(item)).ToArray()),
            _ => node.DeepClone()
        };
    }

    private static JsonObject SortObject(JsonObject source)
    {
        var result = new JsonObject();
        foreach ((string key, JsonNode? value) in source.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            result.Add(key, Sort(value));
        }

        return result;
    }
}
