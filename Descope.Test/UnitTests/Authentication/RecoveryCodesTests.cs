using Descope.Auth.Models.Onetimev1;
using Descope.Test.Helpers;
using FluentAssertions;
using Microsoft.Kiota.Abstractions;
using Xunit;

namespace Descope.Test.UnitTests.Authentication;

/// <summary>
/// Unit tests for the recovery codes extension method, which requires a refresh JWT to be passed explicitly.
/// These tests use a mock request adapter to simulate API responses without making actual HTTP calls.
/// </summary>
public class RecoveryCodesTests
{
    private const string TestRefreshJwt = "test_refresh_jwt";

    [Fact]
    public async Task RecoveryCodes_PostWithJwt_SendsRefreshJwt()
    {
        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<GenerateUserRecoveryCodesRequest, GenerateUserRecoveryCodesResponse>((requestInfo, requestBody) =>
        {
            requestInfo.HttpMethod.Should().Be(Method.POST);
            requestInfo.URI.AbsolutePath.Should().EndWith("/v1/auth/recovery-codes");
            var jwtOption = requestInfo.RequestOptions.OfType<DescopeJwtOption>().SingleOrDefault();
            jwtOption.Should().NotBeNull();
            jwtOption!.GetContext()["jwt"].Should().Be(TestRefreshJwt);
            return new GenerateUserRecoveryCodesResponse();
        });

        var response = await descopeClient.Auth.V1.RecoveryCodes.PostWithJwtAsync(new GenerateUserRecoveryCodesRequest(), TestRefreshJwt);

        response.Should().NotBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task RecoveryCodes_PostWithJwt_ThrowsWhenRefreshJwtMissing(string? refreshJwt)
    {
        var descopeClient = TestDescopeClientFactory.CreateWithEmptyResponse();

        Func<Task> act = () => descopeClient.Auth.V1.RecoveryCodes.PostWithJwtAsync(new GenerateUserRecoveryCodesRequest(), refreshJwt!);

        await act.Should().ThrowAsync<DescopeException>();
    }
}
