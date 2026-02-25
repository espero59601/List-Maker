using System.Text.Json.Serialization;

namespace ChecklistApp.Models;

/// <summary>
/// Represents a list template that can be shared with other users.
/// </summary>
public class ListTemplate
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("coverImagePath")]
    public string CoverImagePath { get; set; } = string.Empty;

    [JsonPropertyName("coverImageUrl")]
    public string CoverImageUrl { get; set; } = string.Empty;

    [JsonPropertyName("items")]
    public List<ListItem> Items { get; set; } = new();

    [JsonPropertyName("createdDate")]
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [JsonPropertyName("modifiedDate")]
    public DateTime ModifiedDate { get; set; } = DateTime.Now;

    [JsonPropertyName("isPublished")]
    public bool IsPublished { get; set; } = false;

    /// <summary>
    /// Returns the best available cover image source (local file takes priority over URL).
    /// </summary>
    [JsonIgnore]
    public string CoverImageSource =>
        !string.IsNullOrEmpty(CoverImagePath) ? CoverImagePath :
        !string.IsNullOrEmpty(CoverImageUrl) ? CoverImageUrl :
        string.Empty;

    [JsonIgnore]
    public bool HasCoverImage => !string.IsNullOrEmpty(CoverImageSource);
}

/// <summary>
/// A single item within a list template.
/// </summary>
public class ListItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("sortOrder")]
    public int SortOrder { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; } = 0;

    [JsonPropertyName("imagePath")]
    public string ImagePath { get; set; } = string.Empty;

    [JsonPropertyName("imageUrl")]
    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>
    /// Returns the best available image source (local file takes priority over URL).
    /// </summary>
    [JsonIgnore]
    public string ImageSource =>
        !string.IsNullOrEmpty(ImagePath) ? ImagePath :
        !string.IsNullOrEmpty(ImageUrl) ? ImageUrl :
        string.Empty;

    [JsonIgnore]
    public bool HasImage => !string.IsNullOrEmpty(ImageSource);
}
