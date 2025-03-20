using System.Text;
using System.Text.Json;
using LagerClient.Blazor.Shared.Models;
using Microsoft.JSInterop;
using Blazored.Toast.Services;
using Client.Pages;

namespace LagerClient.Blazor.Client.Services
{
    public class DataExportImportService : IDataExportImportService
    {
        private readonly IOfflineArticleService _articleService;
        private readonly IJSRuntime _jsRuntime;
        private readonly IToastService _toastService;
        private readonly bool _isDebugMode;
        private readonly AppConfigService _configService;
        private readonly ModalServiceWrapper _modalService;

    private readonly IAppStateService _appStateService;

    public DataExportImportService(
        IOfflineArticleService articleService,
        IJSRuntime jsRuntime,
        IToastService toastService,
        AppConfigService configService,
        ModalServiceWrapper modalService,
        IAppStateService appStateService)
    {
        _articleService = articleService;
        _jsRuntime = jsRuntime;
        _toastService = toastService;
        _configService = configService;
        _modalService = modalService;
        _appStateService = appStateService;
    }

        public async Task ExportDataToJson()
        {
            try
            {
                // Get articles
                var articlesData = await _articleService.GetArticlesAsync();
                
                // Create a raw string builder for the data JSON
                var dataJsonBuilder = new StringBuilder("[");
                
                for (int i = 0; i < articlesData.Count; i++)
                {
                    var a = articlesData[i];
                    
                    // Ensure StylesJson is up to date
                    if (string.IsNullOrEmpty(a.StylesJson) && a.Styles != null && a.Styles.Count > 0)
                    {
                        a.StylesJson = JsonSerializer.Serialize(a.Styles);
                    }
                    
                    // Build each article JSON manually to match Java's exact format
                    var articleJson = $@"{{
                    ""Id"": {a.Id},
                    ""Name"": ""{EscapeJsonString(a.Name)}"",
                    ""Type"": ""{EscapeJsonString(a.Type)}"",
                    ""Stock"": {a.Stock},
                    ""Unit"": ""{EscapeJsonString(a.Unit)}"",
                    ""Price"": {a.Price.ToString(System.Globalization.CultureInfo.InvariantCulture)},
                    ""Location"": ""{EscapeJsonString(a.Location)}"",
                    ""Status"": ""{EscapeJsonString(a.Status)}"",
                    ""Link"": ""{EscapeJsonString(a.Link)}"",
                    ""Timestamp"": ""{a.Timestamp:yyyy-MM-ddTHH:mm:ss}"",
                    ""StylesJson"": ""{EscapeJsonString(a.StylesJson)}"",
                    ""Styles"": {SerializeStyles(a.Styles)}
                    }}";
                    
                    dataJsonBuilder.Append(articleJson);
                    
                    if (i < articlesData.Count - 1)
                    {
                        dataJsonBuilder.Append(",\n");
                    }
                }
                
                dataJsonBuilder.Append("]");
                
                var dataJson = dataJsonBuilder.ToString();
                
                // Compute hash as in Java
                var hash = ComputeHashJavaCompatible(dataJson);
                
                // Build the final JSON
                var exportJson = $@"{{
                ""data"": {dataJson},
                ""hash"": ""{hash}""
                }}";
                
                // Generate filename
                var fileName = "articles.json";
                
                // Trigger download
                await _jsRuntime.InvokeVoidAsync("downloadFile", fileName, exportJson);
                
                _toastService.ShowSuccess("Daten erfolgreich exportiert");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Export error: {ex.Message}");
                Console.Error.WriteLine($"Stack trace: {ex.StackTrace}");
                
                _toastService.ShowError($"Fehler beim Exportieren: {ex.Message}");
            }
        }

        public async Task<List<Article>?> ImportDataFromJson()
        {
            try
            {
                // Dateiinhalt abrufen
                var fileContent = await _jsRuntime.InvokeAsync<string>("importFile");
                
                if (string.IsNullOrEmpty(fileContent))
                {
                    _toastService.ShowWarning("Keine Datei ausgewählt oder Datei leer.");
                    return null;
                }
                
                // Debug-Modus dynamisch abfragen, wenn nötig
                bool isDebugMode = false;
                try
                {
                    var config = await _configService.GetConfigAsync();
                    isDebugMode = config.IsDebugMode;
                }
                catch
                {
                    // Ignorieren, wenn Konfiguration nicht verfügbar ist
                    Console.WriteLine($"Catch, Konfiguration nicht verfügbar");
                }
                
                // Debug-Ausgaben nur wenn Debug-Modus aktiviert
                if (isDebugMode) 
                {
                    Console.WriteLine($"Importierte JSON-Länge: {fileContent.Length}");
                }
                
                // JSON-Dokument parsen
                using var document = JsonDocument.Parse(fileContent);
                var root = document.RootElement;
                
                // Datenknoten abrufen
                var dataNode = root.GetProperty("data");
                
                // Artikelanzahl zählen
                int articleCount = 0;
                foreach (var _ in dataNode.EnumerateArray())
                {
                    articleCount++;
                }
                
                if (isDebugMode) 
                {
                    Console.WriteLine($"{articleCount} Artikel in importiertem JSON gefunden");
                }
                
                // Artikel deserialisieren
                var importedArticles = new List<Article>();
                foreach (JsonElement articleElement in dataNode.EnumerateArray())
                {
                    try
                    {
                        var article = new Article();
                        
                        // Eigenschaften mit sorgfältiger Fehlerbehandlung manuell setzen
                        if (articleElement.TryGetProperty("Id", out JsonElement idElement))
                        {
                            article.Id = idElement.GetInt32();
                        }
                        
                        article.Name = GetJsonStringProperty(articleElement, "Name");
                        article.Type = GetJsonStringProperty(articleElement, "Type");
                        
                        if (articleElement.TryGetProperty("Stock", out JsonElement stockElement))
                        {
                            article.Stock = stockElement.GetInt32();
                        }
                        
                        article.Unit = GetJsonStringProperty(articleElement, "Unit");
                        
                        if (articleElement.TryGetProperty("Price", out JsonElement priceElement))
                        {
                            article.Price = priceElement.GetDecimal();
                        }
                        
                        article.Location = GetJsonStringProperty(articleElement, "Location");
                        article.Status = GetJsonStringProperty(articleElement, "Status", "Auf Lager");
                        article.Link = GetJsonStringProperty(articleElement, "Link");
                        
                        // StylesJson parsen
                        article.StylesJson = GetJsonStringProperty(articleElement, "StylesJson");
                        
                        // Style-Dictionary parsen und konvertieren
                        if (articleElement.TryGetProperty("Styles", out JsonElement stylesElement))
                        {
                            var styles = new Dictionary<string, CellStyle>();
                            
                            foreach (var property in stylesElement.EnumerateObject())
                            {
                                var styleElement = property.Value;
                                var style = new CellStyle();
                                
                                // Style-Eigenschaften mit Standardwerten holen
                                if (styleElement.TryGetProperty("Bold", out JsonElement boldElement))
                                    style.Bold = boldElement.GetBoolean();
                                    
                                if (styleElement.TryGetProperty("Italic", out JsonElement italicElement))
                                    style.Italic = italicElement.GetBoolean();
                                    
                                if (styleElement.TryGetProperty("Color", out JsonElement colorElement))
                                    style.Color = colorElement.GetString() ?? "#000000";
                                
                                styles[property.Name] = style;
                            }
                            
                            article.Styles = styles;
                            if (isDebugMode) 
                            {
                                Console.WriteLine($"{styles.Count} Style-Einträge für Artikel {article.Id} verarbeitet");
                            }
                        }
                        else
                        {
                            article.Styles = new Dictionary<string, CellStyle>();
                            if (isDebugMode) 
                            {
                                Console.WriteLine($"Keine Styles für Artikel {article.Id} gefunden");
                            }
                        }
                        
                        // Zeitstempel parsen
                        var timestampStr = GetJsonStringProperty(articleElement, "Timestamp");
                        if (!string.IsNullOrEmpty(timestampStr) && DateTime.TryParse(timestampStr, out DateTime timestamp))
                        {
                            article.Timestamp = timestamp;
                        }
                        else
                        {
                            article.Timestamp = DateTime.Now;
                        }
                        
                        // Zur Liste hinzufügen
                        importedArticles.Add(article);
                        if (isDebugMode) 
                        {
                            Console.WriteLine($"Artikel verarbeitet: Id={article.Id}, Name={article.Name}, StylesAnzahl={article.Styles.Count}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Fehler beim Parsen eines Artikels: {ex.Message}");
                    }
                }
                
                if (isDebugMode) 
                {
                    Console.WriteLine($"Erfolgreich {importedArticles.Count} Artikel geparst");
                }
                
                // Import bestätigen
                var parameters = new Blazored.Modal.ModalParameters();
                parameters.Add("ContentText", $"Möchten Sie {importedArticles.Count} Artikel importieren? Bestehende Daten werden lokal überschrieben.");
                parameters.Add("ButtonText", "Importieren");
                parameters.Add("Color", "primary");
                
                var modalOptions = new Blazored.Modal.ModalOptions();
                var formModal = _modalService.Show<ConfirmationModal>("Daten importieren", parameters, modalOptions);
                var result = await formModal.Result;
                            
                if (!result.Cancelled)
                {
                    if (isDebugMode) 
                    {
                        Console.WriteLine("Benutzer hat Import bestätigt");
                    }
                    
                    try
                    {
                        // OfflineArticleService direkt für den Import verwenden
                        if (_articleService is OfflineArticleService offlineService)
                        {
                            // Wichtig: Diese Methode setzt den Offline-Modus
                            await offlineService.ImportArticlesAsync(importedArticles);

                            Console.WriteLine($"Offline-Status nach Import: {!offlineService.IsOnline}");
                            
                            // Aktualisiere den AppState nach dem Import
                            var config = await _configService.GetConfigAsync();
                            await _appStateService.UpdateConfigAsync(config);
                            
                            _toastService.ShowSuccess($"{importedArticles.Count} Artikel erfolgreich importiert.");
                            _toastService.ShowInfo("Offline-Modus aktiviert. Klicken Sie auf 'Online', um die Daten zu synchronisieren.");
                            
                            return importedArticles;
                        }
                        else
                        {
                            // Fallback für andere Service-Implementierungen
                            foreach (var article in importedArticles)
                            {
                                if (string.IsNullOrEmpty(article.StylesJson) && article.Styles != null && article.Styles.Count > 0)
                                {
                                    article.StylesJson = JsonSerializer.Serialize(article.Styles);
                                }
                                await _articleService.SaveArticleAsync(article);
                            }
                            
                            _toastService.ShowSuccess($"{importedArticles.Count} Artikel erfolgreich importiert.");
                            return importedArticles;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Fehler beim Importieren der Artikel: {ex.Message}");
                        Console.Error.WriteLine(ex.StackTrace);
                        _toastService.ShowError($"Fehler beim Importieren: {ex.Message}");
                        return null;
                    }
                }
                else
                {
                    return null; // Benutzer hat abgebrochen
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Import-Fehler: {ex.Message}");
                Console.Error.WriteLine($"Stack trace: {ex.StackTrace}");
                
                _toastService.ShowError($"Fehler beim Importieren: {ex.Message}");
                return null;
            }
        }

        // Escape string for JSON
        private string EscapeJsonString(string str)
        {
            if (string.IsNullOrEmpty(str)) return "";
            
            return str
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }

        // Serialize styles dictionary manually
        private string SerializeStyles(Dictionary<string, CellStyle> styles)
        {
            if (styles == null || styles.Count == 0)
                return "{}";
            
            var styleSb = new StringBuilder("{");
            var first = true;
            
            foreach (var kvp in styles)
            {
                if (!first) styleSb.Append(",");
                first = false;
                
                var style = kvp.Value;
                styleSb.Append($@"
            ""{EscapeJsonString(kvp.Key)}"": {{
              ""Bold"": {style.Bold.ToString().ToLower()},
              ""Italic"": {style.Italic.ToString().ToLower()},
              ""Color"": ""{EscapeJsonString(style.Color)}""
            }}");
            }
            
            styleSb.Append("\n      }");
            return styleSb.ToString();
        }
        
        // Hash calculation that matches Java's implementation
        private string ComputeHashJavaCompatible(string input)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            byte[] hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(hashBytes);
        }

        // Helper method to safely get a string property from a JsonElement
        private string GetJsonStringProperty(JsonElement element, string propertyName, string defaultValue = "")
        {
            if (element.TryGetProperty(propertyName, out JsonElement property))
            {
                if (property.ValueKind == JsonValueKind.String)
                {
                    return property.GetString() ?? defaultValue;
                }
            }
            return defaultValue;
        }
    }
}