using System.Net;
using System.Text;
using System.Text.Json;

namespace Rms.Risk.Mango.Pivot.UI.Services;

/// <summary>
/// Utility to convert JSON text to an HTML-formatted, colorized representation suitable for embedding inside a pre.
/// </summary>
public static class JsonHtmlFormatter
{
    private const string KeyColor = "#FF8700";    // orange
    private const string StringColor = "#008B8B"; // teal
    private const string NumberColor = "#00d700"; // green
    private const string BoolColor = "#af87ff";   // purple
    private const string NullColor = "#af87ff";   // purple

    public static string ToHtml(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return "<pre></pre>";

        try
        {
            using var doc = JsonDocument.Parse(json);
            var sb = new StringBuilder();
            sb.Append("{");
            sb.AppendLine();
            WriteObjectMembers(sb, doc.RootElement, 1);
            sb.AppendLine();
            sb.Append("}");

            return "<pre>" + sb.ToString() + "</pre>";
        }
        catch (Exception)
        {
            // If parsing fails, return safely encoded content inside <pre>
            return "<pre>" + WebUtility.HtmlEncode(json) + "</pre>";
        }
    }

    private static void WriteObjectMembers(StringBuilder sb, JsonElement element, int indent)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return;

        var first = true;
        foreach (var prop in element.EnumerateObject())
        {
            if (!first)
            {
                sb.AppendLine(",");
            }
            first = false;

            sb.Append(new string(' ', indent * 2));
            // key
            var encodedName = WebUtility.HtmlEncode(prop.Name);
            sb.Append($"<span style=\"color:{KeyColor};\">\"{encodedName}\"</span> : ");

            WriteElement(sb, prop.Value, indent);
        }
    }

    private static void WriteElement(StringBuilder sb, JsonElement el, int indent)
    {
        switch (el.ValueKind)
        {
            case JsonValueKind.Object:
                sb.Append("{");
                sb.AppendLine();
                WriteObjectMembers(sb, el, indent + 1);
                sb.AppendLine();
                sb.Append(new string(' ', indent * 2));
                sb.Append('}');
                break;
            case JsonValueKind.Array:
                WriteArray(sb, el, indent);
                break;
            case JsonValueKind.String:
                var s = el.GetString() ?? string.Empty;
                sb.Append($"<span style=\"color:{StringColor};\">\"{WebUtility.HtmlEncode(s)}\"</span>");
                break;
            case JsonValueKind.Number:
                // Use raw text to preserve formatting (ints vs floats, exponent)
                sb.Append($"<span style=\"color:{NumberColor};\">{WebUtility.HtmlEncode(el.GetRawText())}</span>");
                break;
            case JsonValueKind.True:
            case JsonValueKind.False:
                sb.Append($"<span style=\"color:{BoolColor};\">{WebUtility.HtmlEncode(el.GetRawText())}</span>");
                break;
            case JsonValueKind.Null:
                sb.Append($"<span style=\"color:{NullColor};\">null</span>");
                break;
            default:
                sb.Append(WebUtility.HtmlEncode(el.GetRawText()));
                break;
        }
    }

    private static void WriteArray(StringBuilder sb, JsonElement array, int indent)
    {
        var length = 0;
        foreach (var _ in array.EnumerateArray()) length++;

        if (length == 0)
        {
            sb.Append("[]");
            return;
        }

        // Decide whether to render inline: single primitive element
        if (length == 1)
        {
            var first = true;
            foreach (var item in array.EnumerateArray())
            {
                if (first)
                {
                    first = false;
                    if (IsPrimitive(item))
                    {
                        sb.Append("[");
                        WriteElement(sb, item, indent);
                        sb.Append("]");
                        return;
                    }
                }
            }
        }

        sb.Append("[");
        sb.AppendLine();

        var firstItem = true;
        foreach (var item in array.EnumerateArray())
        {
            if (!firstItem)
                sb.AppendLine(",");
            firstItem = false;

            sb.Append(new string(' ', (indent + 1) * 2));
            WriteElement(sb, item, indent + 1);
        }

        sb.AppendLine();
        sb.Append(new string(' ', indent * 2));
        sb.Append(']');
    }

    private static bool IsPrimitive(JsonElement el)
    {
        return el.ValueKind == JsonValueKind.String
            || el.ValueKind == JsonValueKind.Number
            || el.ValueKind == JsonValueKind.True
            || el.ValueKind == JsonValueKind.False
            || el.ValueKind == JsonValueKind.Null;
    }
}
