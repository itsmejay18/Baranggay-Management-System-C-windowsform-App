using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

// Clean up decompiled WPF code-behind files to work with XAML
// Removes: InitializeComponent, IComponentConnector.Connect, _contentLoaded, Main, named element fields
// Adds: partial keyword to class declarations

var baseDir = @"C:\Users\ADMIN\Desktop\files\dvo\Baranggay-Management-System-C-windowsform-App\Decompiled";
var files = Directory.GetFiles(baseDir, "*.xaml.cs", SearchOption.AllDirectories);

int processed = 0;
foreach (var file in files)
{
    var content = File.ReadAllText(file);
    var original = content;

    // 1. Add partial to class declaration
    content = Regex.Replace(content, @"public class (\w+)", "public partial class $1");

    // 2. Remove ", IComponentConnector" from class declaration
    content = content.Replace(", IComponentConnector", "");

    // 3. Remove the _contentLoaded field
    content = Regex.Replace(content, @"\s*private bool _contentLoaded;\s*\r?\n", "\n");

    // 4. Remove InitializeComponent method (with attributes)
    content = RemoveMethod(content, "InitializeComponent");

    // 5. Remove Main method (with [STAThread] and other attributes)
    content = RemoveMainMethod(content);

    // 6. Remove IComponentConnector.Connect method (with attributes)
    content = RemoveConnectMethod(content);

    // 7. Remove internal field declarations for named elements
    // These are fields like: internal TextBlock headerTitleText;
    content = RemoveNamedElementFields(content);

    // 8. Remove unused usings that are only for the removed code
    content = content.Replace("using System.CodeDom.Compiler;\r\n", "");
    content = content.Replace("using System.CodeDom.Compiler;\n", "");

    if (content != original)
    {
        File.WriteAllText(file, content);
        processed++;
    }
}

Console.WriteLine($"Processed {processed} files");

static string RemoveMethod(string content, string methodName)
{
    // Pattern: optional attributes, then method signature, then body
    var pattern = $@"(\s*\[[^\]]*\]\s*)*\s*(public|private|protected|internal)\s+void\s+{methodName}\s*\([^)]*\)\s*\{{";
    var match = Regex.Match(content, pattern);
    if (!match.Success) return content;

    int start = match.Index;
    int braceStart = content.IndexOf('{', match.Index + match.Length - 1);
    if (braceStart < 0) return content;

    int end = FindMatchingBrace(content, braceStart);
    if (end < 0) return content;

    // Remove from start to end (inclusive of closing brace and newline)
    int removeEnd = end + 1;
    while (removeEnd < content.Length && (content[removeEnd] == '\r' || content[removeEnd] == '\n'))
        removeEnd++;

    // Also remove leading whitespace/newlines before the method
    while (start > 0 && (content[start - 1] == '\r' || content[start - 1] == '\n' || content[start - 1] == '\t' || content[start - 1] == ' '))
        start--;

    return content.Substring(0, start) + content.Substring(removeEnd);
}

static string RemoveMainMethod(string content)
{
    // Find [STAThread] followed by Main method
    var pattern = @"\s*\[STAThread\]\s*(\[[^\]]*\]\s*)*\s*(public|private|internal)\s+static\s+void\s+Main\s*\(\s*\)\s*\{";
    var match = Regex.Match(content, pattern);
    if (!match.Success) return content;

    int start = match.Index;
    int braceStart = content.IndexOf('{', match.Index + match.Length - 1);
    if (braceStart < 0) return content;

    int end = FindMatchingBrace(content, braceStart);
    if (end < 0) return content;

    int removeEnd = end + 1;
    while (removeEnd < content.Length && (content[removeEnd] == '\r' || content[removeEnd] == '\n'))
        removeEnd++;

    while (start > 0 && (content[start - 1] == '\r' || content[start - 1] == '\n' || content[start - 1] == '\t' || content[start - 1] == ' '))
        start--;

    return content.Substring(0, start) + content.Substring(removeEnd);
}

static string RemoveConnectMethod(string content)
{
    // Find IComponentConnector.Connect or System.Windows.Markup.IComponentConnector.Connect
    var pattern = @"\s*(\[[^\]]*\]\s*)*\s*void\s+(System\.Windows\.Markup\.)?IComponentConnector\.Connect\s*\([^)]*\)\s*\{";
    var match = Regex.Match(content, pattern);
    if (!match.Success) return content;

    int start = match.Index;
    int braceStart = content.IndexOf('{', match.Index + match.Length - 1);
    if (braceStart < 0) return content;

    int end = FindMatchingBrace(content, braceStart);
    if (end < 0) return content;

    int removeEnd = end + 1;
    while (removeEnd < content.Length && (content[removeEnd] == '\r' || content[removeEnd] == '\n'))
        removeEnd++;

    while (start > 0 && (content[start - 1] == '\r' || content[start - 1] == '\n' || content[start - 1] == '\t' || content[start - 1] == ' '))
        start--;

    return content.Substring(0, start) + content.Substring(removeEnd);
}

static string RemoveNamedElementFields(string content)
{
    // Remove lines like: internal TextBlock headerTitleText;
    // But NOT lines with static, const, readonly, event, or initializers
    var lines = content.Split('\n');
    var sb = new StringBuilder();
    foreach (var line in lines)
    {
        var trimmed = line.TrimStart();
        if (trimmed.StartsWith("internal ") && trimmed.EndsWith(";\r") || trimmed.EndsWith(";"))
        {
            // Check if it's a simple field declaration (no static, const, readonly, event, =)
            if (!trimmed.Contains("static ") && !trimmed.Contains("const ") && 
                !trimmed.Contains("readonly ") && !trimmed.Contains("event ") &&
                !trimmed.Contains(" = ") && !trimmed.Contains("delegate"))
            {
                // It's likely a named element field - skip it
                continue;
            }
        }
        sb.Append(line);
        if (!line.EndsWith('\n'))
            sb.Append('\n');
    }
    return sb.ToString().TrimEnd('\n') + "\n";
}

static int FindMatchingBrace(string content, int openBraceIndex)
{
    int depth = 0;
    bool inString = false;
    bool inChar = false;
    bool inLineComment = false;
    bool inBlockComment = false;

    for (int i = openBraceIndex; i < content.Length; i++)
    {
        char c = content[i];
        char next = i + 1 < content.Length ? content[i + 1] : '\0';
        char prev = i > 0 ? content[i - 1] : '\0';

        if (inLineComment)
        {
            if (c == '\n') inLineComment = false;
            continue;
        }
        if (inBlockComment)
        {
            if (c == '*' && next == '/') { inBlockComment = false; i++; }
            continue;
        }
        if (inString)
        {
            if (c == '"' && prev != '\\') inString = false;
            continue;
        }
        if (inChar)
        {
            if (c == '\'' && prev != '\\') inChar = false;
            continue;
        }

        if (c == '/' && next == '/') { inLineComment = true; continue; }
        if (c == '/' && next == '*') { inBlockComment = true; continue; }
        if (c == '"') { inString = true; continue; }
        if (c == '\'') { inChar = true; continue; }

        if (c == '{') depth++;
        if (c == '}') { depth--; if (depth == 0) return i; }
    }
    return -1;
}
