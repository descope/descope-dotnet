using Descope.Auth.Models.Onetimev1;
using Descope.Test.Helpers;
using FluentAssertions;
using Microsoft.Kiota.Abstractions;
using Xunit;

namespace Descope.Test.UnitTests.Authentication;

/// <summary>
/// Unit tests for recovery codes, security questions and me tenants extension methods, which require a refresh JWT to be passed explicitly.
/// These tests use a mock request adapter to simulate API responses without making actual HTTP calls.
/// </summary>
public class RefreshJwtExtensionsTests
{
    private const string TestRefreshJwt = "test_refresh_jwt";

    [Fact]
    public async Task RecoveryCodes_PostWithJwt_SendsRefreshJwt()
    {
        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<GenerateUserRecoveryCodesRequest, GenerateUserRecoveryCodesResponse>((requestInfo, requestBody) =>
        {
            requestInfo.HttpMethod.Should().Be(Method.POST);
            requestInfo.URI.AbsolutePath.Should().EndWith("/v1/auth/recovery-codes");
            AssertJwtOption(requestInfo, TestRefreshJwt);
            return new GenerateUserRecoveryCodesResponse();
        });

        var response = await descopeClient.Auth.V1.RecoveryCodes.PostWithJwtAsync(new GenerateUserRecoveryCodesRequest(), TestRefreshJwt);

        response.Should().NotBeNull();
    }

    [Fact]
    public async Task SecurityQuestionsSetup_PostWithJwt_SendsRefreshJwt()
    {
        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<SetupUserSecurityQuestionsRequest, SetupUserSecurityQuestionsResponse>((requestInfo, requestBody) =>
        {
            requestInfo.HttpMethod.Should().Be(Method.POST);
            requestInfo.URI.AbsolutePath.Should().EndWith("/v1/auth/security-questions/setup");
            AssertJwtOption(requestInfo, TestRefreshJwt);
            return new SetupUserSecurityQuestionsResponse();
        });

        var response = await descopeClient.Auth.V1.SecurityQuestions.Setup.PostWithJwtAsync(new SetupUserSecurityQuestionsRequest(), TestRefreshJwt);

        response.Should().NotBeNull();
    }

    [Fact]
    public async Task SecurityQuestionsVerify_GetWithJwt_SendsRefreshJwt()
    {
        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<GetUserSecurityVerifyQuestionsResponse>(requestInfo =>
        {
            requestInfo.HttpMethod.Should().Be(Method.GET);
            requestInfo.URI.AbsolutePath.Should().EndWith("/v1/auth/security-questions/verify");
            AssertJwtOption(requestInfo, TestRefreshJwt);
            return new GetUserSecurityVerifyQuestionsResponse();
        });

        var response = await descopeClient.Auth.V1.SecurityQuestions.Verify.GetWithJwtAsync(TestRefreshJwt);

        response.Should().NotBeNull();
    }

    [Fact]
    public async Task SecurityQuestionsVerify_PostWithJwt_SendsRefreshJwt()
    {
        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<VerifyUserSecurityQuestionsRequest, JWTResponse>((requestInfo, requestBody) =>
        {
            requestInfo.HttpMethod.Should().Be(Method.POST);
            requestInfo.URI.AbsolutePath.Should().EndWith("/v1/auth/security-questions/verify");
            AssertJwtOption(requestInfo, TestRefreshJwt);
            return new JWTResponse();
        });

        var response = await descopeClient.Auth.V1.SecurityQuestions.Verify.PostWithJwtAsync(new VerifyUserSecurityQuestionsRequest(), TestRefreshJwt);

        response.Should().NotBeNull();
    }

    [Fact]
    public async Task MeTenants_PostWithJwt_SendsRefreshJwt()
    {
        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<MeTenantsRequest, MeTenantsResponse>((requestInfo, requestBody) =>
        {
            requestInfo.HttpMethod.Should().Be(Method.POST);
            requestInfo.URI.AbsolutePath.Should().EndWith("/v1/auth/me/tenants");
            AssertJwtOption(requestInfo, TestRefreshJwt);
            return new MeTenantsResponse();
        });

        var response = await descopeClient.Auth.V1.Me.Tenants.PostWithJwtAsync(new MeTenantsRequest(), TestRefreshJwt);

        response.Should().NotBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task PostWithJwt_ThrowsWhenRefreshJwtMissing(string? refreshJwt)
    {
        var descopeClient = TestDescopeClientFactory.CreateWithEmptyResponse();

        await ((Func<Task>)(() => descopeClient.Auth.V1.RecoveryCodes.PostWithJwtAsync(new GenerateUserRecoveryCodesRequest(), refreshJwt!))).Should().ThrowAsync<DescopeException>();
        await ((Func<Task>)(() => descopeClient.Auth.V1.SecurityQuestions.Setup.PostWithJwtAsync(new SetupUserSecurityQuestionsRequest(), refreshJwt!))).Should().ThrowAsync<DescopeException>();
        await ((Func<Task>)(() => descopeClient.Auth.V1.SecurityQuestions.Verify.GetWithJwtAsync(refreshJwt!))).Should().ThrowAsync<DescopeException>();
        await ((Func<Task>)(() => descopeClient.Auth.V1.SecurityQuestions.Verify.PostWithJwtAsync(new VerifyUserSecurityQuestionsRequest(), refreshJwt!))).Should().ThrowAsync<DescopeException>();
        await ((Func<Task>)(() => descopeClient.Auth.V1.Me.Tenants.PostWithJwtAsync(new MeTenantsRequest(), refreshJwt!))).Should().ThrowAsync<DescopeException>();
    }

    private static void AssertJwtOption(RequestInformation requestInfo, string expectedJwt)
    {
        var jwtOption = requestInfo.RequestOptions.OfType<DescopeJwtOption>().SingleOrDefault();
        jwtOption.Should().NotBeNull();
        jwtOption!.GetContext()["jwt"].Should().Be(expectedJwt);
    }
}
