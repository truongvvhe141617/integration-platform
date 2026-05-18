using System.Globalization;
using System.Text.Json;
using BuildingBlocks.Abstractions.Connectors;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Core.Mapping;

/// <summary>
/// Engine thực hiện mapping request/response dựa trên config.
/// 
/// ❗ FIX #2: Giới hạn scope rõ ràng
/// ✔ Config-driven: field mapping, default values, simple transforms
/// ❌ KHÔNG hỗ trợ: conditional logic, loops, complex expressions
/// → Logic phức tạp phải dùng custom DLL connector
/// 
/// Supported transforms (whitelist, không mở rộng tùy ý):
/// - ToUpper, ToLower, Trim
/// - Format:{pattern} (cho DateTime)
/// - Prefix:{value}, Suffix:{value}
/// - Replace:{old}|{new}
/// 
/// Nếu cần transform phức tạp hơn → viết custom connector.
/// </summary>
public interface IMappingEngine
{
    Dictionary<string, object> MapRequest(
        List<FieldMapping> mappings, Dictionary<string, object> source);

    Dictionary<string, object> MapResponse(
        List<FieldMapping> mappings, Dictionary<string, object> source);

    List<string> Validate(
        List<ValidationRule> rules, Dictionary<string, object> data);
}

public class MappingEngine : IMappingEngine
{
    private readonly ILogger<MappingEngine> _logger;

    /// <summary>Max nesting depth cho dot notation (tránh abuse)</summary>
    private const int MaxNestingDepth = 5;

    /// <summary>Max số mappings per operation (tránh config quá phức tạp)</summary>
    private const int MaxMappingsPerOperation = 50;

    /// <summary>Whitelist transforms được phép (tránh thành mini programming language)</summary>
    private static readonly HashSet<string> AllowedTransforms = new(StringComparer.OrdinalIgnoreCase)
    {
        "toupper", "tolower", "trim", "format", "prefix", "suffix", "replace"
    };

    public MappingEngine(ILogger<MappingEngine> logger)
    {
        _logger = logger;
    }

    public Dictionary<string, object> MapRequest(
        List<FieldMapping> mappings, Dictionary<string, object> source)
    {
        ValidateMappingCount(mappings, "request");
        return ApplyMappings(mappings, source, "request");
    }

    public Dictionary<string, object> MapResponse(
        List<FieldMapping> mappings, Dictionary<string, object> source)
    {
        ValidateMappingCount(mappings, "response");
        return ApplyMappings(mappings, source, "response");
    }

    public List<string> Validate(List<ValidationRule> rules, Dictionary<string, object> data)
    {
        var errors = new List<string>();

        foreach (var rule in rules)
        {
            var value = GetNestedValue(data, rule.Field);
            var valueStr = value?.ToString() ?? "";

            var parts = rule.Rule.Split(':');
            var ruleName = parts[0].ToLowerInvariant();
            var ruleParam = parts.Length > 1 ? parts[1] : "";

            var isValid = ruleName switch
            {
                "required" => !string.IsNullOrWhiteSpace(valueStr),
                "maxlength" => int.TryParse(ruleParam, out var max) && valueStr.Length <= max,
                "minlength" => int.TryParse(ruleParam, out var min) && valueStr.Length >= min,
                "regex" => System.Text.RegularExpressions.Regex.IsMatch(valueStr, ruleParam),
                _ => true
            };

            if (!isValid)
            {
                errors.Add(rule.ErrorMessage ?? $"Validation failed for '{rule.Field}': {rule.Rule}");
            }
        }

        return errors;
    }

    private void ValidateMappingCount(List<FieldMapping> mappings, string direction)
    {
        if (mappings.Count > MaxMappingsPerOperation)
        {
            throw new InvalidOperationException(
                $"Too many {direction} mappings ({mappings.Count}). " +
                $"Max allowed: {MaxMappingsPerOperation}. " +
                $"Consider using a custom DLL connector for complex transformations.");
        }
    }

    private Dictionary<string, object> ApplyMappings(
        List<FieldMapping> mappings, Dictionary<string, object> source, string direction)
    {
        var result = new Dictionary<string, object>();

        foreach (var mapping in mappings)
        {
            // Validate nesting depth
            if (mapping.Source.Split('.').Length > MaxNestingDepth ||
                mapping.Target.Split('.').Length > MaxNestingDepth)
            {
                _logger.LogWarning(
                    "Mapping path exceeds max depth ({Max}): {Source} → {Target}",
                    MaxNestingDepth, mapping.Source, mapping.Target);
                continue;
            }

            var value = GetNestedValue(source, mapping.Source);

            if (value == null && mapping.Required)
            {
                throw new InvalidOperationException(
                    $"Required field '{mapping.Source}' is missing in {direction} mapping");
            }

            value ??= mapping.DefaultValue;
            if (value == null) continue;

            // Apply transform (chỉ whitelist)
            if (!string.IsNullOrEmpty(mapping.Transform))
            {
                var transformName = mapping.Transform.Split(':')[0];
                if (!AllowedTransforms.Contains(transformName))
                {
                    _logger.LogWarning(
                        "Unknown transform '{Transform}' ignored. Allowed: {Allowed}",
                        mapping.Transform, string.Join(", ", AllowedTransforms));
                }
                else
                {
                    value = ApplyTransform(value, mapping.Transform);
                }
            }

            SetNestedValue(result, mapping.Target, value);
        }

        return result;
    }

    private object? GetNestedValue(Dictionary<string, object> source, string path)
    {
        var parts = path.Split('.');
        object? current = source;

        foreach (var part in parts)
        {
            if (current is Dictionary<string, object> dict)
            {
                current = dict.TryGetValue(part, out var val) ? val : null;
            }
            else if (current is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.Object &&
                    jsonElement.TryGetProperty(part, out var prop))
                {
                    current = prop;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        // Extract actual value from JsonElement
        return current is JsonElement je ? UnwrapJsonElement(je) : current;
    }

    /// <summary>
    /// Convert JsonElement thành C# primitive (string, int, bool, etc.)
    /// để serializer xử lý đúng thay vì serialize raw JsonElement.
    /// </summary>
    private static object? UnwrapJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => element.ToString(),
            JsonValueKind.Object => element.ToString(),
            _ => element.ToString()
        };
    }

    private void SetNestedValue(Dictionary<string, object> target, string path, object value)
    {
        var parts = path.Split('.');

        if (parts.Length == 1)
        {
            target[parts[0]] = value;
            return;
        }

        var current = target;
        for (int i = 0; i < parts.Length - 1; i++)
        {
            if (!current.ContainsKey(parts[i]) || current[parts[i]] is not Dictionary<string, object>)
            {
                current[parts[i]] = new Dictionary<string, object>();
            }
            current = (Dictionary<string, object>)current[parts[i]];
        }

        current[parts[^1]] = value;
    }

    private object ApplyTransform(object value, string transform)
    {
        var str = value.ToString() ?? "";
        var parts = transform.Split(':');
        var transformName = parts[0].ToLowerInvariant();
        var param = parts.Length > 1 ? string.Join(":", parts.Skip(1)) : "";

        return transformName switch
        {
            "toupper" => str.ToUpperInvariant(),
            "tolower" => str.ToLowerInvariant(),
            "trim" => str.Trim(),
            "format" when DateTime.TryParse(str, out var dt)
                => dt.ToString(param, CultureInfo.InvariantCulture),
            "prefix" => $"{param}{str}",
            "suffix" => $"{str}{param}",
            "replace" => ApplyReplace(str, param),
            _ => value
        };
    }

    private static string ApplyReplace(string value, string param)
    {
        var replaceParts = param.Split('|');
        return replaceParts.Length == 2
            ? value.Replace(replaceParts[0], replaceParts[1])
            : value;
    }
}
