using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Descope;

var builder = WebApplication.CreateBuilder(args);

// Load local settings
builder.Configuration.AddJsonFile("localExampleSettings.json", optional: true, reloadOnChange: true);

// Configure Descope OIDC authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddDescopeOIDCAuthentication(
        // MANDATORY: set your Descope project ID (all other options are OPTIONAL)
        configureOidc: options =>
        {
            // Mandatory: Set your Descope Project ID
            options.ProjectId = builder.Configuration["Descope:ProjectId"]!;
            // Optional: Override base URL
            options.BaseUrl = builder.Configuration["Descope:BaseUrl"];
            // Optional: Set flow ID for specific authentication flow, e.g., "sign-up-or-in"
            options.FlowId = builder.Configuration["Descope:FlowId"];
        },
        // OPTIONAL: add custom OIDC event handlers
        configureEvents: events =>
        {
            // Example: Add custom logging for redirect to identity provider
            // This won't override the SDK's internal handler that sets the flow parameter
            events.OnRedirectToIdentityProvider += context =>
            {
                Console.WriteLine("=== Redirecting to Descope ===");
                Console.WriteLine($"Authorization Endpoint: {context.ProtocolMessage.AuthorizationEndpoint}");
                Console.WriteLine($"Client ID: {context.ProtocolMessage.ClientId}");
                Console.WriteLine($"Scope: {context.ProtocolMessage.Scope}");
                Console.WriteLine($"Flow Parameter: {context.ProtocolMessage.GetParameter("flow") ?? "not set"}");
                return Task.CompletedTask;
            };

            // Example: Add debug logging for token response (note: avoid logging sensitive info in production)
            events.OnTokenResponseReceived += context =>
            {
                Console.WriteLine("=== Descope Token Response Received ===");
                Console.WriteLine($"Access Token: {context.TokenEndpointResponse?.AccessToken?.Substring(0, Math.Min(50, context.TokenEndpointResponse?.AccessToken?.Length ?? 0))}...");
                Console.WriteLine($"ID Token: {(context.TokenEndpointResponse?.IdToken != null ? context.TokenEndpointResponse.IdToken.Substring(0, Math.Min(50, context.TokenEndpointResponse.IdToken.Length)) + "..." : "NULL")}");
                Console.WriteLine($"Refresh Token: {(context.TokenEndpointResponse?.RefreshToken != null ? "present" : "NULL")}");
                Console.WriteLine($"Token Type: {context.TokenEndpointResponse?.TokenType}");
                Console.WriteLine($"Expires In: {context.TokenEndpointResponse?.ExpiresIn}");
                Console.WriteLine($"Scope: {context.TokenEndpointResponse?.Scope}");
                return Task.CompletedTask;
            };
        },
        // OPTIONAL: customize cookie settings, created after successful OIDC authentication
        configureCookies: options =>
        {
            // Customize cookie settings if needed
            options.Cookie.Name = "ChocolateChipCookie";
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.ExpireTimeSpan = TimeSpan.FromHours(1);
            options.SlidingExpiration = true;
        });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Home page with login button
app.MapGet("/", (HttpContext context) =>
{
    var isAuthenticated = context.User.Identity?.IsAuthenticated ?? false;

    if (isAuthenticated)
    {
        return Results.Redirect("/success");
    }

    var html = """
        <!DOCTYPE html>
        <html lang="en">
        <head>
            <meta charset="UTF-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>Descope OIDC Example</title>
            <style>
                body {
                    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, sans-serif;
                    display: flex;
                    justify-content: center;
                    align-items: center;
                    min-height: 100vh;
                    margin: 0;
                    background: linear-gradient(135deg, #0a1628 0%, #0d3b4c 100%);
                }
                .container {
                    text-align: center;
                    background: white;
                    padding: 3rem;
                    border-radius: 16px;
                    box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
                    max-width: 400px;
                }
                h1 {
                    color: #333;
                    margin-bottom: 0.5rem;
                }
                p {
                    color: #666;
                    margin-bottom: 2rem;
                }
                .login-btn {
                    display: inline-block;
                    padding: 1rem 2.5rem;
                    font-size: 1.1rem;
                    font-weight: 600;
                    color: white;
                    background: linear-gradient(135deg, #00d4aa 0%, #38b2ac 100%);
                    border: none;
                    border-radius: 8px;
                    cursor: pointer;
                    text-decoration: none;
                    transition: transform 0.2s, box-shadow 0.2s;
                }
                .login-btn:hover {
                    transform: translateY(-2px);
                    box-shadow: 0 8px 25px rgba(0, 212, 170, 0.4);
                }
                .logo {
                    font-size: 3rem;
                    margin-bottom: 1rem;
                }
            </style>
        </head>
        <body>
            <div class="container">
                <div class="logo">🔐</div>
                <h1>Descope OIDC Demo</h1>
                <p>Sign in with Descope to continue</p>
                <a href="/login" class="login-btn">Login with Descope Flow</a>
            </div>
        </body>
        </html>
        """;

    return Results.Content(html, "text/html");
});

// Login endpoint - triggers OIDC flow
app.MapGet("/login", () =>
{
    return Results.Challenge(new AuthenticationProperties
    {
        RedirectUri = "/success"
    }, ["Descope"]);
});

// Success page after authentication
app.MapGet("/success", (HttpContext context) =>
{
    var user = context.User;
    var isAuthenticated = user.Identity?.IsAuthenticated ?? false;

    if (!isAuthenticated)
    {
        return Results.Redirect("/");
    }

    var name = user.FindFirst("name")?.Value
               ?? user.FindFirst("preferred_username")?.Value
               ?? user.Identity?.Name
               ?? "User";

    var email = user.FindFirst("email")?.Value ?? "N/A";
    var sub = user.FindFirst("sub")?.Value ?? "N/A";

    var html = $$"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
            <meta charset="UTF-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>Login Successful - Descope OIDC Example</title>
            <style>
                body {
                    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, sans-serif;
                    display: flex;
                    justify-content: center;
                    align-items: center;
                    min-height: 100vh;
                    margin: 0;
                    background: linear-gradient(135deg, #11998e 0%, #38ef7d 100%);
                }
                .container {
                    text-align: center;
                    background: white;
                    padding: 3rem;
                    border-radius: 16px;
                    box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
                    max-width: 500px;
                }
                h1 {
                    color: #333;
                    margin-bottom: 0.5rem;
                }
                .success-icon {
                    font-size: 4rem;
                    margin-bottom: 1rem;
                }
                .user-info {
                    background: #f8f9fa;
                    border-radius: 8px;
                    padding: 1.5rem;
                    margin: 1.5rem 0;
                    text-align: left;
                }
                .user-info h3 {
                    margin: 0 0 1rem 0;
                    color: #333;
                }
                .info-row {
                    display: flex;
                    margin-bottom: 0.5rem;
                }
                .info-label {
                    font-weight: 600;
                    color: #666;
                    width: 80px;
                }
                .info-value {
                    color: #333;
                    word-break: break-all;
                }
                .logout-btn {
                    display: inline-block;
                    padding: 0.8rem 2rem;
                    font-size: 1rem;
                    font-weight: 600;
                    color: #666;
                    background: #f8f9fa;
                    border: 2px solid #ddd;
                    border-radius: 8px;
                    cursor: pointer;
                    text-decoration: none;
                    transition: all 0.2s;
                }
                .logout-btn:hover {
                    background: #e9ecef;
                    border-color: #ccc;
                }
            </style>
        </head>
        <body>
            <div class="container">
                <div class="success-icon">✅</div>
                <h1>Welcome, {{name}}!</h1>
                <p>You have successfully authenticated with Descope</p>
                <div class="user-info">
                    <h3>User Information</h3>
                    <div class="info-row">
                        <span class="info-label">Name:</span>
                        <span class="info-value">{{name}}</span>
                    </div>
                </div>
                <a href="/logout" class="logout-btn">Logout</a>
            </div>
        </body>
        </html>
        """;

    return Results.Content(html, "text/html");
}).RequireAuthorization();

// Logout endpoint
app.MapGet("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/");
});

app.Run();
