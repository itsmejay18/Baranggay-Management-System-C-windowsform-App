using System;
using System.Text;
using System.Text.Json;

namespace baranggaysystem1;

internal static class JsonUtils
{
    public static bool TryExtractFirstJsonObject(string input, out string jsonObject)
    {
        jsonObject = string.Empty;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        ReadOnlySpan<char> span = input.AsSpan();
        int start = span.IndexOf('{');
        if (start < 0)
        {
            return false;
        }

        bool inString = false;
        bool escaped = false;
        int depth = 0;

        for (int i = start; i < span.Length; i++)
        {
            char ch = span[i];

            if (inString)
            {
                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                if (ch == '\\')
                {
                    escaped = true;
                    continue;
                }

                if (ch == '"')
                {
                    inString = false;
                }

                continue;
            }

            if (ch == '"')
            {
                inString = true;
                continue;
            }

            if (ch == '{')
            {
                depth++;
            }
            else if (ch == '}')
            {
                depth--;
                if (depth == 0)
                {
                    jsonObject = span.Slice(start, i - start + 1).ToString();
                    return true;
                }
            }
        }

        return false;
    }

    public static string TrimCodeFences(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        string trimmed = text.Trim();
        if (trimmed.StartsWith("```") && trimmed.EndsWith("```", StringComparison.Ordinal))
        {
            string[] lines = trimmed.Split('\n', StringSplitOptions.None);
            if (lines.Length >= 2)
            {
                var builder = new StringBuilder();
                for (int i = 1; i < lines.Length - 1; i++)
                {
                    builder.AppendLine(lines[i]);
                }
                return builder.ToString().Trim();
            }
        }

        return trimmed;
    }

    public static T DeserializeStrict<T>(string json)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        T? result = JsonSerializer.Deserialize<T>(json, options);
        if (result == null)
        {
            throw new JsonException("Deserialized object is null.");
        }

        return result;
    }
}
