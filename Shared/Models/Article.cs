using System.Text.Json;
using System.Text.Json.Serialization;

namespace LagerClient.Blazor.Shared.Models;

public class Article
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int Stock { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Location { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string StylesJson { get; set; } = string.Empty;
    public Dictionary<string, CellStyle> Styles { get; set; } = new();

    public void DeserializeStyles()
    {
        if (!string.IsNullOrEmpty(StylesJson))
        {
            try 
            {
                Styles = JsonSerializer.Deserialize<Dictionary<string, CellStyle>>(StylesJson, 
                    new JsonSerializerOptions 
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new Dictionary<string, CellStyle>();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Fehler bei Style-Deserialisierung: {ex.Message}");
                Styles = new Dictionary<string, CellStyle>();
            }
        }
    }
    
}

public class CellStyle
{
    [JsonPropertyName("Bold")]
    public bool Bold { get; set; }
    
    [JsonPropertyName("Italic")]
    public bool Italic { get; set; }
    
    [JsonPropertyName("Color")]
    public string Color { get; set; } = "#000000";
}