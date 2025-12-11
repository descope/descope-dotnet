using Descope.Test.Helpers;
using Microsoft.Kiota.Abstractions;
using System.Net;
using Xunit;

namespace Descope.Test.UnitTests.Internal;

public class DescopeJwtOptionTests
{

    [Fact]
    public void AuthenticationContextOption_WithJwt_ShouldCreateCorrectContext()
    {
        // Arrange & Act
        var option = DescopeJwtOption.WithJwt("test-jwt-token");

        // Assert
        Assert.NotNull(option.GetContext());
        Assert.True(option.GetContext().ContainsKey("jwt"));
        Assert.Equal("test-jwt-token", option.GetContext()["jwt"]);
    }

}
