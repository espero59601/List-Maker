using System.Text.Json;
using ChecklistApp.Models;

namespace ChecklistApp.Services;

/// <summary>
/// Handles reading and writing list templates to JSON files in app storage.
/// Each template is saved as a separate JSON file for simplicity.
/// </summary>
public class ListTemplateService
{
    private readonly string _dataDirectory;
    private readonly JsonSerializerOptions _jsonOptions;

    public ListTemplateService()
    {
        // Use the app's local data directory (works on both Android and iOS)
        _dataDirectory = Path.Combine(FileSystem.AppDataDirectory, "templates");
        Directory.CreateDirectory(_dataDirectory);

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };
    }

    /// <summary>
    /// Gets all saved list templates.
    /// </summary>
    public async Task<List<ListTemplate>> GetAllTemplatesAsync()
    {
        var templates = new List<ListTemplate>();

        if (!Directory.Exists(_dataDirectory))
            return templates;

        var files = Directory.GetFiles(_dataDirectory, "*.json");

        foreach (var file in files)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                var template = JsonSerializer.Deserialize<ListTemplate>(json, _jsonOptions);
                if (template != null)
                    templates.Add(template);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reading template file {file}: {ex.Message}");
            }
        }

        return templates.OrderByDescending(t => t.ModifiedDate).ToList();
    }

    /// <summary>
    /// Gets a single template by its ID.
    /// </summary>
    public async Task<ListTemplate?> GetTemplateAsync(string id)
    {
        var filePath = GetFilePath(id);

        if (!File.Exists(filePath))
            return null;

        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<ListTemplate>(json, _jsonOptions);
    }

    /// <summary>
    /// Saves a template (creates new or updates existing).
    /// </summary>
    public async Task SaveTemplateAsync(ListTemplate template)
    {
        template.ModifiedDate = DateTime.Now;
        var json = JsonSerializer.Serialize(template, _jsonOptions);
        var filePath = GetFilePath(template.Id);
        await File.WriteAllTextAsync(filePath, json);
    }

    /// <summary>
    /// Deletes a template by its ID.
    /// </summary>
    public Task DeleteTemplateAsync(string id)
    {
        var filePath = GetFilePath(id);

        if (File.Exists(filePath))
            File.Delete(filePath);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Exports a template as a JSON string (for sharing).
    /// </summary>
    public async Task<string> ExportTemplateAsync(string id)
    {
        var template = await GetTemplateAsync(id);
        if (template == null)
            throw new FileNotFoundException($"Template {id} not found.");

        // Create a clean copy for export (reset ID so the importer gets a fresh one)
        var exportCopy = new ListTemplate
        {
            Id = Guid.NewGuid().ToString(),
            Name = template.Name,
            Description = template.Description,
            Category = template.Category,
            Items = template.Items.Select(i => new ListItem
            {
                Id = Guid.NewGuid().ToString(),
                Name = i.Name,
                Description = i.Description,
                SortOrder = i.SortOrder
            }).ToList(),
            CreatedDate = DateTime.Now,
            ModifiedDate = DateTime.Now,
            IsPublished = false
        };

        return JsonSerializer.Serialize(exportCopy, _jsonOptions);
    }

    /// <summary>
    /// Imports a template from a JSON string.
    /// </summary>
    public async Task<ListTemplate?> ImportTemplateAsync(string json)
    {
        try
        {
            var template = JsonSerializer.Deserialize<ListTemplate>(json, _jsonOptions);
            if (template != null)
            {
                template.Id = Guid.NewGuid().ToString();
                template.CreatedDate = DateTime.Now;
                template.ModifiedDate = DateTime.Now;
                await SaveTemplateAsync(template);
            }
            return template;
        }
        catch
        {
            return null;
        }
    }

    private string GetFilePath(string id)
    {
        // Sanitize the ID for use as a filename
        var safeId = string.Join("_", id.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(_dataDirectory, $"{safeId}.json");
    }
}
