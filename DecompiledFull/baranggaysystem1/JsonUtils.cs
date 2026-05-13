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
		int num = span.IndexOf('{');
		if (num < 0)
		{
			return false;
		}
		bool flag = false;
		bool flag2 = false;
		int num2 = 0;
		for (int i = num; i < span.Length; i++)
		{
			char c = span[i];
			if (flag)
			{
				if (flag2)
				{
					flag2 = false;
					continue;
				}
				switch (c)
				{
				case '\\':
					flag2 = true;
					break;
				case '"':
					flag = false;
					break;
				}
				continue;
			}
			switch (c)
			{
			case '"':
				flag = true;
				break;
			case '{':
				num2++;
				break;
			case '}':
				num2--;
				if (num2 == 0)
				{
					jsonObject = span.Slice(num, i - num + 1).ToString();
					return true;
				}
				break;
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
		string text2 = text.Trim();
		if (text2.StartsWith("```") && text2.EndsWith("```", StringComparison.Ordinal))
		{
			string[] array = text2.Split('\n');
			if (array.Length >= 2)
			{
				StringBuilder stringBuilder = new StringBuilder();
				for (int i = 1; i < array.Length - 1; i++)
				{
					stringBuilder.AppendLine(array[i]);
				}
				return stringBuilder.ToString().Trim();
			}
		}
		return text2;
	}

	public static T DeserializeStrict<T>(string json)
	{
		JsonSerializerOptions options = new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true
		};
		return JsonSerializer.Deserialize<T>(json, options) ?? throw new JsonException("Deserialized object is null.");
	}
}
