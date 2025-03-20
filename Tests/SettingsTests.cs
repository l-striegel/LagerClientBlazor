using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using LagerClient.Blazor.Client.Services;
using LagerClient.Blazor.Shared.Models;

namespace Tests
{
    public class SettingsTests : TestContext
    {
        [Fact]
        public void Settings_RendersCorrectly()
        {
            // Arrange
            var mockConfigService = new Mock<IAppConfigService>();
            var mockAppState = new Mock<IAppStateService>();
            var mockToast = new Mock<Blazored.Toast.Services.IToastService>();

            Services.AddSingleton<IAppConfigService>(mockConfigService.Object);
            Services.AddSingleton<IAppStateService>(mockAppState.Object);
            Services.AddSingleton<Blazored.Toast.Services.IToastService>(mockToast.Object);
            
            var config = new AppConfig 
            { 
                ApiUrl = "https://test-api.com",
                IsDebugMode = true,
                UiSettings = new UiSettings 
                { 
                    TableRowHeight = 30,
                    TableZebraColor = "#CCCCCC" 
                }
            };
            
            mockConfigService.Setup(m => m.GetConfigAsync()).ReturnsAsync(config);
            
            // Act
            var cut = RenderComponent<LagerClient.Blazor.Client.Pages.Settings>();
            
            // Wait for async operation to complete
            cut.WaitForState(() => cut.FindAll("input").Count > 0);
            
            // Assert
            Assert.Contains("API URL", cut.Markup);
            Assert.Contains("Debug-Modus aktivieren", cut.Markup);
            Assert.Contains("Tabellenzeilenhöhe", cut.Markup);
            Assert.Contains("Tabellen-Zebrafarbe", cut.Markup);
            
            // Verify the input fields have the correct values
            var apiUrlInput = cut.Find("#apiUrl");
            Assert.Equal("https://test-api.com", apiUrlInput.GetAttribute("value"));
            
            var debugModeCheckbox = cut.Find("#debugMode");
            Assert.NotNull(debugModeCheckbox.GetAttribute("checked"));
        }
        
        [Fact]
        public void SaveConfig_CallsUpdateConfigAsync()
        {
            // Arrange
            var mockConfigService = new Mock<IAppConfigService>();
            var mockAppState = new Mock<IAppStateService>();
            var mockToast = new Mock<Blazored.Toast.Services.IToastService>();
            
            Services.AddSingleton<IAppConfigService>(mockConfigService.Object);
            Services.AddSingleton<IAppStateService>(mockAppState.Object);
            Services.AddSingleton<Blazored.Toast.Services.IToastService>(mockToast.Object);
            
            var config = new AppConfig 
            { 
                ApiUrl = "https://test-api.com",
                IsDebugMode = true,
                UiSettings = new UiSettings 
                { 
                    TableRowHeight = 30,
                    TableZebraColor = "#CCCCCC" 
                }
            };
            
            mockConfigService.Setup(m => m.GetConfigAsync()).ReturnsAsync(config);
            mockConfigService.Setup(m => m.UpdateConfigAsync(It.IsAny<AppConfig>())).Returns(Task.CompletedTask);
            
            // Act
            var cut = RenderComponent<LagerClient.Blazor.Client.Pages.Settings>();
            cut.WaitForState(() => cut.FindAll("input").Count > 0);
            
            // Find and click the Submit button
            var form = cut.Find("form");
            form.Submit();
            
            // Assert
            mockConfigService.Verify(m => m.UpdateConfigAsync(It.IsAny<AppConfig>()), Times.Once);
            mockToast.Verify(m => m.ShowSuccess(It.IsAny<string>(), null), Times.Once);
        }
    }
}