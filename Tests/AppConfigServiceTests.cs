using LagerClient.Blazor.Client.Services;
using LagerClient.Blazor.Shared.Models;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Text.Json;
using Xunit;
using System.Threading;

namespace Tests
{
    public class AppConfigServiceTests
    {
        [Fact]
        public async Task GetConfigAsync_WhenConfigExists_ReturnsStoredConfig()
        {
            // Arrange
            var mockConfig = new Mock<IConfiguration>();
            var mockLocalStorage = new Mock<Blazored.LocalStorage.ILocalStorageService>();
            var mockAppStateService = new Mock<IAppStateService>();
            
            var storedConfig = new AppConfig 
            { 
                ApiUrl = "https://test-api.com", 
                IsDebugMode = true,
                UiSettings = new UiSettings 
                { 
                    TableRowHeight = 30,
                    TableZebraColor = "#CCCCCC"
                }
            };
            
            mockLocalStorage.Setup(m => m.ContainKeyAsync("app_config", It.IsAny<CancellationToken>()))
                           .ReturnsAsync(true);
            mockLocalStorage.Setup(m => m.GetItemAsync<AppConfig>("app_config", It.IsAny<CancellationToken>()))
                           .ReturnsAsync(storedConfig);
            
            var service = new AppConfigService(mockConfig.Object, mockLocalStorage.Object, mockAppStateService.Object);
            
            // Act
            var result = await service.GetConfigAsync();
            
            // Assert
            Assert.Equal("https://test-api.com", result.ApiUrl);
            Assert.True(result.IsDebugMode);
            Assert.Equal(30, result.UiSettings.TableRowHeight);
            Assert.Equal("#CCCCCC", result.UiSettings.TableZebraColor);
        }
        
        [Fact]
        public async Task GetConfigAsync_WhenConfigDoesNotExist_CreatesDefaultConfig()
        {
            // Arrange
            var mockConfig = new Mock<IConfiguration>();
            var mockLocalStorage = new Mock<Blazored.LocalStorage.ILocalStorageService>();
            var mockAppStateService = new Mock<IAppStateService>();
            
            mockLocalStorage.Setup(m => m.ContainKeyAsync("app_config", It.IsAny<CancellationToken>()))
                            .ReturnsAsync(false);
            mockConfig.Setup(x => x["ApiUrl"]).Returns("https://default-api.com");
            mockConfig.Setup(x => x["DebugMode"]).Returns("true");
            mockConfig.Setup(x => x["UI:Table:RowHeight"]).Returns("25");
            mockConfig.Setup(x => x["UI:Table:ZebraColor"]).Returns("#F0F0F0");
            
            var service = new AppConfigService(mockConfig.Object, mockLocalStorage.Object, mockAppStateService.Object);
            
            // Act
            var result = await service.GetConfigAsync();
            
            // Assert
            Assert.Equal("https://default-api.com", result.ApiUrl);
            Assert.True(result.IsDebugMode);
            Assert.Equal(25, result.UiSettings.TableRowHeight);
            Assert.Equal("#F0F0F0", result.UiSettings.TableZebraColor);
            
            // Verify default config was saved to localStorage
            mockLocalStorage.Verify(m => m.SetItemAsync("app_config", It.IsAny<AppConfig>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact]
        public async Task UpdateConfigAsync_UpdatesAndCachesConfig()
        {
            // Arrange
            var mockConfig = new Mock<IConfiguration>();
            var mockLocalStorage = new Mock<Blazored.LocalStorage.ILocalStorageService>();
            var mockAppStateService = new Mock<IAppStateService>();
            
            var newConfig = new AppConfig 
            { 
                ApiUrl = "https://updated-api.com", 
                IsDebugMode = false,
                UiSettings = new UiSettings 
                { 
                    TableRowHeight = 35,
                    TableZebraColor = "#EEEEEE"
                }
            };
            
            var service = new AppConfigService(mockConfig.Object, mockLocalStorage.Object, mockAppStateService.Object);
            
            // Act
            await service.UpdateConfigAsync(newConfig);
            var result = await service.GetConfigAsync(); // Should return cached config
            
            // Assert
            Assert.Equal("https://updated-api.com", result.ApiUrl);
            Assert.False(result.IsDebugMode);
            Assert.Equal(35, result.UiSettings.TableRowHeight);
            Assert.Equal("#EEEEEE", result.UiSettings.TableZebraColor);
            
            // Verify localStorage was updated
            mockLocalStorage.Verify(m => m.SetItemAsync("app_config", It.IsAny<AppConfig>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}