using System.Diagnostics;
using System.Reflection;
using Xunit;

namespace Descope.Test.Unit
{
    public class DescopeClientTest
    {
        [Fact]
        public void SdkInfo_Name_ShouldBeDotNet()
        {
            // Arrange & Act
            var sdkName = SdkInfo.Name;

            // Assert
            Assert.Equal("dotnet", sdkName);
        }

        [Fact]
        public void SdkInfo_Version_ShouldNotBeUnknown()
        {
            // Arrange & Act
            var sdkVersion = SdkInfo.Version;

            // Assert
            Assert.NotEqual("Unknown", sdkVersion);
        }

        [Fact]
        public void SdkInfo_Version_ShouldMatchAssemblyVersion()
        {
            // Arrange
            var expectedVersion = Assembly.GetAssembly(typeof(DescopeClient))?.GetName()?.Version?.ToString();

            // Act
            var sdkVersion = SdkInfo.Version;

            // Assert
            Assert.Equal(expectedVersion, sdkVersion);
        }
    }
}