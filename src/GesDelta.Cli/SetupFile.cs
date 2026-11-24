using System.Text.Json;
using GesDelta.Core.Enums;
using GesDelta.Core.Models;

namespace GesDelta.Cli;

public static class SetupFile
{
    public static StartMonitoringRequest Parse(string json)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new FormatException($"Setup file is not valid JSON: {ex.Message}", ex);
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                throw new FormatException("Setup file: root must be a JSON object");

            var topic = ReadString(root, "topic", "setup file");
            var days = ReadNumber(root, "checkIntervalDays", "setup file");

            TimeSpan interval;
            try
            {
                interval = TimeSpan.FromDays(days);
            }
            catch (OverflowException)
            {
                throw new FormatException(
                    $"Setup file: 'checkIntervalDays' is out of range ({days})");
            }

            if (!root.TryGetProperty("sources", out var sourcesElement)
                || sourcesElement.ValueKind != JsonValueKind.Array)
                throw new FormatException("Setup file: 'sources' must be an array");

            var sources = new List<SourceSetup>();
            var index = 0;
            foreach (var element in sourcesElement.EnumerateArray())
            {
                sources.Add(ParseSource(element, index));
                index++;
            }

            return new StartMonitoringRequest(topic, interval, sources);
        }
    }

    private static SourceSetup ParseSource(JsonElement element, int index)
    {
        var position = $"sources[{index}]";

        if (element.ValueKind != JsonValueKind.Object)
            throw new FormatException($"Setup file: {position} must be an object");

        var name = ReadString(element, "name", position);
        var where = $"source '{name}' ({position})";

        var typeText = ReadString(element, "type", where);
        
        if (!Enum.TryParse<SourceType>(typeText, ignoreCase: true, out var type)
            || !Enum.IsDefined(type))
        {
            throw new FormatException(
                $"Setup file: {where}: unknown type '{typeText}' " +
                $"(expected: {string.Join(", ", Enum.GetNames<SourceType>())})");
        }

        var url = ReadString(element, "url", where);

        string? configJson = null;
        if (element.TryGetProperty("config", out var config)
            && config.ValueKind != JsonValueKind.Null)
        {
            if (config.ValueKind != JsonValueKind.Object)
                throw new FormatException($"Setup file: {where}: 'config' must be an object");

            configJson = config.GetRawText();
        }

        return new SourceSetup(name, type, url, configJson);
    }

    private static string ReadString(JsonElement parent, string property, string where)
    {
        if (!parent.TryGetProperty(property, out var value))
            throw new FormatException($"Setup file: {where}: missing '{property}'");

        if (value.ValueKind != JsonValueKind.String)
            throw new FormatException($"Setup file: {where}: '{property}' must be a string");

        return value.GetString()!;
    }

    private static double ReadNumber(JsonElement parent, string property, string where)
    {
        if (!parent.TryGetProperty(property, out var value))
            throw new FormatException($"Setup file: {where}: missing '{property}'");

        if (value.ValueKind != JsonValueKind.Number || !value.TryGetDouble(out var number))
            throw new FormatException($"Setup file: {where}: '{property}' must be a number");

        return number;
    }
}