// IndexTests.cs
using Bunit;
using LagerClient.Blazor.Client.Services;
using LagerClient.Blazor.Shared.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.Configuration;
using Blazored.LocalStorage;
using Blazored.Modal.Services;

namespace Tests
{
    public class IndexTests : TestContext
    {
        [Fact]
        public void Index_RenderCorrectly()
        {
            // Arrange
            var mockArticleService = new Mock<IOfflineArticleService>();
            var mockConfigService = new Mock<IAppConfigService>();
            var mockModalService = new Mock<ModalServiceWrapper>(Mock.Of<IModalService>());
            var mockToastService = new Mock<Blazored.Toast.Services.IToastService>();
            var mockJsRuntime = new Mock<IJSRuntime>();
            var mockAppState = new Mock<AppStateService>(Mock.Of<IConfiguration>());
            var mockExportImportService = new Mock<IDataExportImportService>();
            
            mockArticleService.Setup(m => m.GetArticlesAsync())
                .ReturnsAsync(new List<Article> { new Article { Id = 1, Name = "Test Article" } });
            
            mockConfigService.Setup(m => m.GetConfigAsync())
                .ReturnsAsync(new AppConfig 
                { 
                    IsDebugMode = false,
                    UiSettings = new UiSettings 
                    { 
                        TableRowHeight = 30,
                        TableZebraColor = "#F0F0F0" 
                    }
                });
            
            Services.AddSingleton<IOfflineArticleService>(mockArticleService.Object);
            Services.AddSingleton<IAppConfigService>(mockConfigService.Object);
            Services.AddSingleton<ModalServiceWrapper>(mockModalService.Object);
            Services.AddSingleton<Blazored.Toast.Services.IToastService>(mockToastService.Object);
            Services.AddSingleton<IJSRuntime>(mockJsRuntime.Object);
            Services.AddSingleton<AppStateService>(mockAppState.Object);
            Services.AddSingleton<IDataExportImportService>(mockExportImportService.Object);
            
            // Act
            var cut = RenderComponent<Client.Pages.Index>();
            
            // Wait for async operation to complete
            cut.WaitForState(() => cut.FindAll("table").Count > 0);
            
            // Assert
            Assert.Contains("LagerClient - Artikelverwaltung", cut.Markup);
            Assert.Contains("Test Article", cut.Markup);
            Assert.Contains("Neuer Artikel", cut.Markup);
            Assert.DoesNotContain("Keine Artikel gefunden", cut.Markup);
        }
        
        [Fact]
        public void Index_ShowsNoArticlesMessage_WhenEmpty()
        {
            // Arrange
            var mockArticleService = new Mock<IOfflineArticleService>();
            var mockConfigService = new Mock<IAppConfigService>();
            var mockModalService = new Mock<ModalServiceWrapper>(Mock.Of<IModalService>());
            var mockToastService = new Mock<Blazored.Toast.Services.IToastService>();
            var mockJsRuntime = new Mock<IJSRuntime>();
            var mockAppState = new Mock<AppStateService>(Mock.Of<IConfiguration>());
            var mockExportImportService = new Mock<IDataExportImportService>();
            
            mockArticleService.Setup(m => m.GetArticlesAsync())
                .ReturnsAsync(new List<Article>());
            
            mockConfigService.Setup(m => m.GetConfigAsync())
                .ReturnsAsync(new AppConfig 
                { 
                    IsDebugMode = false,
                    UiSettings = new UiSettings 
                    { 
                        TableRowHeight = 30,
                        TableZebraColor = "#F0F0F0" 
                    }
                });
            
            Services.AddSingleton<IOfflineArticleService>(mockArticleService.Object);
            Services.AddSingleton<IAppConfigService>(mockConfigService.Object);
            Services.AddSingleton<ModalServiceWrapper>(mockModalService.Object);
            Services.AddSingleton<Blazored.Toast.Services.IToastService>(mockToastService.Object);
            Services.AddSingleton<IJSRuntime>(mockJsRuntime.Object);
            Services.AddSingleton<AppStateService>(mockAppState.Object);
            Services.AddSingleton<IDataExportImportService>(mockExportImportService.Object);
            
            // Act
            var cut = RenderComponent<Client.Pages.Index>();
            
            // Wait for async operation to complete
            cut.WaitForState(() => !cut.FindAll(".spinner-border").Any());
            
            // Assert
            Assert.Contains("Keine Artikel gefunden", cut.Markup);
        }
        
        [Fact]
        public void Index_CallsExportData_WhenButtonClicked()
        {
            // Arrange
            var mockArticleService = new Mock<IOfflineArticleService>();
            var mockConfigService = new Mock<IAppConfigService>();
            var mockModalService = new Mock<ModalServiceWrapper>(Mock.Of<IModalService>());
            var mockToastService = new Mock<Blazored.Toast.Services.IToastService>();
            var mockJsRuntime = new Mock<IJSRuntime>();
            var mockAppState = new Mock<AppStateService>(Mock.Of<IConfiguration>());
            var mockExportImportService = new Mock<IDataExportImportService>();
            
            mockArticleService.Setup(m => m.GetArticlesAsync())
                .ReturnsAsync(new List<Article> { new Article { Id = 1, Name = "Test Article" } });
            
            mockConfigService.Setup(m => m.GetConfigAsync())
                .ReturnsAsync(new AppConfig { IsDebugMode = false });
            
            mockExportImportService.Setup(m => m.ExportDataToJson())
                .Returns(Task.CompletedTask);
            
            Services.AddSingleton<IOfflineArticleService>(mockArticleService.Object);
            Services.AddSingleton<IAppConfigService>(mockConfigService.Object);
            Services.AddSingleton<ModalServiceWrapper>(mockModalService.Object);
            Services.AddSingleton<Blazored.Toast.Services.IToastService>(mockToastService.Object);
            Services.AddSingleton<IJSRuntime>(mockJsRuntime.Object);
            Services.AddSingleton<AppStateService>(mockAppState.Object);
            Services.AddSingleton<IDataExportImportService>(mockExportImportService.Object);
            
            // Act
            var cut = RenderComponent<Client.Pages.Index>();
            cut.WaitForState(() => cut.FindAll("button:contains('Als JSON exportieren')").Count > 0);
            
            var exportButton = cut.Find("button:contains('Als JSON exportieren')");
            exportButton.Click();
            
            // Assert
            mockExportImportService.Verify(m => m.ExportDataToJson(), Times.Once);
        }

        [Fact]
        public void Index_ShowsFormattingToolbar()
        {
            // Arrange
            var mockArticleService = new Mock<IOfflineArticleService>();
            var mockConfigService = new Mock<IAppConfigService>(); // Hier geändert
            var mockModalService = new Mock<ModalServiceWrapper>(Mock.Of<IModalService>());
            var mockToastService = new Mock<Blazored.Toast.Services.IToastService>();
            var mockJsRuntime = new Mock<IJSRuntime>();
            var mockAppState = new Mock<AppStateService>(Mock.Of<IConfiguration>());
            var mockExportImportService = new Mock<IDataExportImportService>();
            
            mockArticleService.Setup(m => m.GetArticlesAsync())
                .ReturnsAsync(new List<Article> { new Article { Id = 1, Name = "Test Article" } });
            
            mockConfigService.Setup(m => m.GetConfigAsync())
                .ReturnsAsync(new AppConfig { IsDebugMode = false });
            
            Services.AddSingleton<IOfflineArticleService>(mockArticleService.Object);
            Services.AddSingleton<IAppConfigService>(mockConfigService.Object); // Hier geändert
            Services.AddSingleton<ModalServiceWrapper>(mockModalService.Object);
            Services.AddSingleton<Blazored.Toast.Services.IToastService>(mockToastService.Object);
            Services.AddSingleton<IJSRuntime>(mockJsRuntime.Object);
            Services.AddSingleton<AppStateService>(mockAppState.Object);
            Services.AddSingleton<IDataExportImportService>(mockExportImportService.Object);
            
            // Act
            var cut = RenderComponent<Client.Pages.Index>();
            cut.WaitForState(() => cut.FindAll("table").Count > 0);
            
            // Assert
            // Überprüft, ob die Formatierungstoolbar angezeigt wird, indem nach einem Element "Farbe" gesucht wird
            Assert.Contains("Farbe:", cut.Markup);
        }
        
        [Fact]
        public void Index_FiltersArticles_WhenSearchTermEntered()
        {
            // Arrange
            var mockArticleService = new Mock<IOfflineArticleService>();
            var mockConfigService = new Mock<IAppConfigService>(); // Hier geändert
            var mockModalService = new Mock<ModalServiceWrapper>(Mock.Of<IModalService>());
            var mockToastService = new Mock<Blazored.Toast.Services.IToastService>();
            var mockJsRuntime = new Mock<IJSRuntime>();
            var mockAppState = new Mock<AppStateService>(Mock.Of<IConfiguration>());
            var mockExportImportService = new Mock<IDataExportImportService>();
            
            var articles = new List<Article> 
            { 
                new Article { Id = 1, Name = "Test Article", Type = "Type1" },
                new Article { Id = 2, Name = "Another Article", Type = "Type2" }
            };
            
            mockArticleService.Setup(m => m.GetArticlesAsync())
                .ReturnsAsync(articles);
            
            mockConfigService.Setup(m => m.GetConfigAsync())
                .ReturnsAsync(new AppConfig { IsDebugMode = false });
            
            Services.AddSingleton<IOfflineArticleService>(mockArticleService.Object);
            Services.AddSingleton<IAppConfigService>(mockConfigService.Object); // Hier geändert
            Services.AddSingleton<ModalServiceWrapper>(mockModalService.Object);
            Services.AddSingleton<Blazored.Toast.Services.IToastService>(mockToastService.Object);
            Services.AddSingleton<IJSRuntime>(mockJsRuntime.Object);
            Services.AddSingleton<AppStateService>(mockAppState.Object);
            Services.AddSingleton<IDataExportImportService>(mockExportImportService.Object);
            
            // Act
            var cut = RenderComponent<Client.Pages.Index>();
            cut.WaitForState(() => cut.FindAll("table").Count > 0);
            
            // Finde das Suchfeld und gib einen Suchbegriff ein
            var searchInput = cut.Find("input[placeholder='Suchen...']");
            searchInput.Input("Test");
            
            // Assert
            Assert.Contains("Test Article", cut.Markup);
            Assert.DoesNotContain("Another Article", cut.Markup);
        }
    }
}