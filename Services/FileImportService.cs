using System.Text.Json;
using System.Text.RegularExpressions;
using ChecklistApp.Models;

namespace ChecklistApp.Services;

/// <summary>
/// Handles importing list items from text, CSV, and JSON files.
/// Supports smart parsing to strip numbering, bullets, and other formatting.
/// </summary>
public class FileImportService
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    /// <summary>
    /// Pick a file from the device and parse it into a ListTemplate.
    /// Returns null if the user cancels or the file can't be parsed.
    /// </summary>
    public async Task<ImportResult?> ImportFromFileAsync()
    {
        try
        {
            var customTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.Android, new[] { "text/plain", "text/csv", "text/comma-separated-values", "application/json", "application/octet-stream" } },
                { DevicePlatform.iOS, new[] { "public.plain-text", "public.comma-separated-values-text", "public.json", "public.text" } },
            });

            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Select a file to import",
                FileTypes = customTypes
            });

            if (result == null)
                return null;

            var content = await ReadFileContentAsync(result);
            if (string.IsNullOrWhiteSpace(content))
                return new ImportResult { Success = false, ErrorMessage = "File is empty." };

            var extension = Path.GetExtension(result.FileName)?.ToLowerInvariant() ?? "";
            var fileName = Path.GetFileNameWithoutExtension(result.FileName);

            return extension switch
            {
                ".json" => ParseJson(content, fileName),
                ".csv" => ParseCsv(content, fileName),
                ".tsv" => ParseCsv(content, fileName, '\t'),
                _ => ParseText(content, fileName) // .txt and anything else
            };
        }
        catch (Exception ex)
        {
            return new ImportResult
            {
                Success = false,
                ErrorMessage = $"Error picking file: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Read the full text content of a picked file.
    /// </summary>
    private async Task<string> ReadFileContentAsync(FileResult fileResult)
    {
        using var stream = await fileResult.OpenReadAsync();
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    // ========================================
    // TEXT PARSING (smart + fallback)
    // ========================================

    private ImportResult ParseText(string content, string defaultName)
    {
        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var items = new List<ListItem>();
        var order = 0;

        foreach (var rawLine in lines)
        {
            var cleaned = SmartCleanLine(rawLine);
            if (!string.IsNullOrWhiteSpace(cleaned))
            {
                items.Add(new ListItem
                {
                    Name = cleaned,
                    SortOrder = order++
                });
            }
        }

        if (items.Count == 0)
        {
            return new ImportResult { Success = false, ErrorMessage = "No items found in the file." };
        }

        var template = new ListTemplate
        {
            Name = CleanTemplateName(defaultName),
            Items = items
        };

        return new ImportResult
        {
            Success = true,
            Template = template,
            ItemCount = items.Count,
            Message = $"Imported {items.Count} items from text file."
        };
    }

    /// <summary>
    /// Smart line cleaning — strips numbering, bullets, dashes, asterisks, tabs, etc.
    /// Examples:
    ///   "1. Lincoln Penny 1909"    → "Lincoln Penny 1909"
    ///   "- Army Green"             → "Army Green"
    ///   "* Burnt Sienna"           → "Burnt Sienna"
    ///   "  • Item with bullet"     → "Item with bullet"
    ///   "42) Some item"            → "Some item"
    ///   "[x] Checked item"         → "Checked item"
    ///   "( ) Unchecked item"       → "Unchecked item"
    /// </summary>
    private string SmartCleanLine(string line)
    {
        var trimmed = line.Trim();

        // Skip obvious header/separator lines
        if (IsHeaderOrSeparator(trimmed))
            return string.Empty;

        // Strip common prefixes in order of specificity
        // Checkbox patterns: [x], [ ], [X], (x), ( )
        trimmed = Regex.Replace(trimmed, @"^\s*[\[\(]\s*[xX ]?\s*[\]\)]\s*", "");

        // Numbered patterns: "1.", "1)", "1:", "1-", "01."
        trimmed = Regex.Replace(trimmed, @"^\s*\d+[\.\)\:\-]\s*", "");

        // Lettered patterns: "a.", "a)", "A."
        trimmed = Regex.Replace(trimmed, @"^\s*[a-zA-Z][\.\)]\s+", "");

        // Bullet patterns: "- ", "* ", "• ", "· ", "→ ", "> "
        trimmed = Regex.Replace(trimmed, @"^\s*[\-\*\•\·\→\>]\s+", "");

        // Tab-indented (just strip leading tabs)
        trimmed = trimmed.TrimStart('\t');

        return trimmed.Trim();
    }

    /// <summary>
    /// Detect lines that are likely headers, separators, or other non-item lines.
    /// </summary>
    private bool IsHeaderOrSeparator(string line)
    {
        // All dashes, equals, underscores (separators)
        if (Regex.IsMatch(line, @"^[\-=_\*]{3,}$"))
            return true;

        // Empty after trimming
        if (string.IsNullOrWhiteSpace(line))
            return true;

        return false;
    }

    // ========================================
    // CSV PARSING
    // ========================================

    private ImportResult ParseCsv(string content, string defaultName, char delimiter = ',')
    {
        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0)
            return new ImportResult { Success = false, ErrorMessage = "CSV file is empty." };

        // Try to detect if first row is a header
        var firstRow = ParseCsvLine(lines[0], delimiter);
        var hasHeader = DetectCsvHeader(firstRow);
        var startIndex = hasHeader ? 1 : 0;

        // Map columns
        int nameCol = 0;
        int descCol = -1;
        int qtyCol = -1;

        if (hasHeader)
        {
            for (int i = 0; i < firstRow.Length; i++)
            {
                var header = firstRow[i].ToLowerInvariant().Trim();
                if (header is "name" or "item" or "title" or "product" or "color" or "coin" or "entry")
                    nameCol = i;
                else if (header is "description" or "desc" or "details" or "notes" or "note")
                    descCol = i;
                else if (header is "quantity" or "qty" or "count" or "amount" or "have" or "owned")
                    qtyCol = i;
            }
        }

        var items = new List<ListItem>();
        var order = 0;

        for (int i = startIndex; i < lines.Length; i++)
        {
            var fields = ParseCsvLine(lines[i], delimiter);
            if (fields.Length == 0) continue;

            var name = nameCol < fields.Length ? fields[nameCol].Trim() : "";
            if (string.IsNullOrWhiteSpace(name)) continue;

            var item = new ListItem
            {
                Name = name,
                SortOrder = order++
            };

            if (descCol >= 0 && descCol < fields.Length)
                item.Description = fields[descCol].Trim();

            if (qtyCol >= 0 && qtyCol < fields.Length && int.TryParse(fields[qtyCol].Trim(), out var qty))
                item.Quantity = Math.Max(0, qty);

            items.Add(item);
        }

        if (items.Count == 0)
            return new ImportResult { Success = false, ErrorMessage = "No items found in CSV." };

        var template = new ListTemplate
        {
            Name = CleanTemplateName(defaultName),
            Items = items
        };

        return new ImportResult
        {
            Success = true,
            Template = template,
            ItemCount = items.Count,
            Message = $"Imported {items.Count} items from CSV{(hasHeader ? " (header detected)" : "")}."
        };
    }

    /// <summary>
    /// Simple CSV line parser that handles quoted fields.
    /// </summary>
    private string[] ParseCsvLine(string line, char delimiter)
    {
        var fields = new List<string>();
        var current = "";
        var inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current += '"';
                    i++; // Skip escaped quote
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == delimiter && !inQuotes)
            {
                fields.Add(current);
                current = "";
            }
            else
            {
                current += c;
            }
        }

        fields.Add(current);
        return fields.ToArray();
    }

    /// <summary>
    /// Heuristic to detect if the first row of a CSV is a header.
    /// </summary>
    private bool DetectCsvHeader(string[] firstRow)
    {
        var headerKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "name", "item", "title", "description", "desc", "quantity", "qty",
            "count", "notes", "category", "type", "product", "color", "number",
            "id", "no", "num", "amount", "have", "owned", "coin", "entry", "note", "details"
        };

        var matchCount = firstRow.Count(f => headerKeywords.Contains(f.Trim()));
        return matchCount >= 1;
    }

    // ========================================
    // JSON PARSING
    // ========================================

    private ImportResult ParseJson(string content, string defaultName)
    {
        try
        {
            // First, try to deserialize as a full ListTemplate
            var template = JsonSerializer.Deserialize<ListTemplate>(content, _jsonOptions);
            if (template != null && template.Items.Count > 0)
            {
                // Give it a new ID so it doesn't collide
                template.Id = Guid.NewGuid().ToString();
                template.CreatedDate = DateTime.Now;
                template.ModifiedDate = DateTime.Now;

                // Re-assign item IDs too
                foreach (var item in template.Items)
                    item.Id = Guid.NewGuid().ToString();

                return new ImportResult
                {
                    Success = true,
                    Template = template,
                    ItemCount = template.Items.Count,
                    Message = $"Imported full list template \"{template.Name}\" with {template.Items.Count} items."
                };
            }
        }
        catch { /* Not a ListTemplate, try other formats */ }

        try
        {
            // Try as an array of objects with a "name" field
            var items = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(content, _jsonOptions);
            if (items != null && items.Count > 0)
            {
                var listItems = new List<ListItem>();
                var order = 0;

                foreach (var obj in items)
                {
                    var name = GetJsonStringField(obj, "name", "item", "title", "product", "color");
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    var item = new ListItem
                    {
                        Name = name,
                        SortOrder = order++
                    };

                    var desc = GetJsonStringField(obj, "description", "desc", "notes");
                    if (!string.IsNullOrEmpty(desc))
                        item.Description = desc;

                    var qty = GetJsonIntField(obj, "quantity", "qty", "count", "amount");
                    if (qty.HasValue)
                        item.Quantity = Math.Max(0, qty.Value);

                    listItems.Add(item);
                }

                if (listItems.Count > 0)
                {
                    var template = new ListTemplate
                    {
                        Name = CleanTemplateName(defaultName),
                        Items = listItems
                    };

                    return new ImportResult
                    {
                        Success = true,
                        Template = template,
                        ItemCount = listItems.Count,
                        Message = $"Imported {listItems.Count} items from JSON array."
                    };
                }
            }
        }
        catch { /* Not an array of objects */ }

        try
        {
            // Try as a simple array of strings
            var strings = JsonSerializer.Deserialize<List<string>>(content, _jsonOptions);
            if (strings != null && strings.Count > 0)
            {
                var listItems = strings
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select((s, i) => new ListItem
                    {
                        Name = s.Trim(),
                        SortOrder = i
                    })
                    .ToList();

                var template = new ListTemplate
                {
                    Name = CleanTemplateName(defaultName),
                    Items = listItems
                };

                return new ImportResult
                {
                    Success = true,
                    Template = template,
                    ItemCount = listItems.Count,
                    Message = $"Imported {listItems.Count} items from JSON string array."
                };
            }
        }
        catch { /* Not a string array */ }

        return new ImportResult
        {
            Success = false,
            ErrorMessage = "Could not parse JSON file. Expected a ListTemplate, array of objects with 'name' field, or array of strings."
        };
    }

    /// <summary>
    /// Try to find a string value from a dictionary using multiple possible key names.
    /// </summary>
    private string? GetJsonStringField(Dictionary<string, JsonElement> obj, params string[] possibleKeys)
    {
        foreach (var key in possibleKeys)
        {
            var match = obj.Keys.FirstOrDefault(k => k.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (match != null && obj[match].ValueKind == JsonValueKind.String)
                return obj[match].GetString();
        }
        return null;
    }

    /// <summary>
    /// Try to find an integer value from a dictionary using multiple possible key names.
    /// </summary>
    private int? GetJsonIntField(Dictionary<string, JsonElement> obj, params string[] possibleKeys)
    {
        foreach (var key in possibleKeys)
        {
            var match = obj.Keys.FirstOrDefault(k => k.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                if (obj[match].ValueKind == JsonValueKind.Number && obj[match].TryGetInt32(out var val))
                    return val;
                if (obj[match].ValueKind == JsonValueKind.String && int.TryParse(obj[match].GetString(), out var parsed))
                    return parsed;
            }
        }
        return null;
    }

    /// <summary>
    /// Clean up a filename into a reasonable template name.
    /// </summary>
    private string CleanTemplateName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return "Imported List";

        // Replace underscores and hyphens with spaces
        var cleaned = fileName.Replace('_', ' ').Replace('-', ' ');

        // Title case
        cleaned = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(cleaned.ToLower());

        return cleaned;
    }
}

/// <summary>
/// Result of a file import operation.
/// </summary>
public class ImportResult
{
    public bool Success { get; set; }
    public ListTemplate? Template { get; set; }
    public int ItemCount { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}
