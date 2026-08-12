using System.Text.Json.Serialization;
using Amazon.Lambda.AspNetCoreServer.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using NationalWid.Api.Auth;
using NationalWid.Api.Data;
using NationalWid.Api.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddConsole();

// API Gateway HTTP API (payload v2) fronts this Lambda.
builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);

// --- Authentication: ULMITA Cognito user pool (JWT bearer) ---
var cognito = builder.Configuration.GetSection("Cognito");
var cognitoRegion = cognito["Region"] ?? "us-gov-west-1";
var userPoolId = cognito["UserPoolId"];
var clientId = cognito["ClientId"];
var configuredClientIds = cognito.GetSection("ClientIds").Get<string[]>() ?? [];
var allowedClientIds = configuredClientIds
    .Concat(string.IsNullOrWhiteSpace(clientId) ? [] : [clientId])
    .Where(id => !string.IsNullOrWhiteSpace(id))
    .Distinct(StringComparer.Ordinal)
    .ToArray();
var issuer = $"https://cognito-idp.{cognitoRegion}.amazonaws.com/{userPoolId}";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // JWKS are resolved from {issuer}/.well-known/jwks.json via OIDC discovery.
        options.Authority = issuer;
        options.MetadataAddress = $"{issuer}/.well-known/openid-configuration";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudiences = allowedClientIds,
            // Cognito access tokens carry the app client id in "client_id" rather than "aud".
            AudienceValidator = (audiences, token, _) =>
                (audiences?.Any(a => allowedClientIds.Contains(a, StringComparer.Ordinal)) == true)
                || (token is JsonWebToken jwt
                    && jwt.TryGetPayloadValue<string>("client_id", out var tokenClientId)
                    && allowedClientIds.Contains(tokenClientId, StringComparer.Ordinal)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2),
        };
    })
    // Allow external systems to authenticate with a long-lived API key instead of a JWT.
    // The ApiKeyAuthHandler returns NoResult when no ApiKey header is present, so the
    // JWT bearer scheme remains the default for browser / interactive callers.
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, ApiKeyAuthHandler>(
        ApiKeyAuthHandler.SchemeName, _ => { });

builder.Services.AddAuthorization(options =>
{
    // For the national public API serving BLS labor market data,
    // authorization is not strictly required since the data is public.
    // However, we maintain the [Authorize] attribute on the controller
    // and use [AllowAnonymous] on specific endpoints to allow public access.
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// --- Data access ---
builder.Services.AddDbContext<WIDDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("WidDb")));

// --- Application services ---
builder.Services.AddSingleton<IQueryService, QueryService>();
builder.Services.AddSingleton<IDownloadService, DownloadService>();
builder.Services.AddScoped<IStatusService, StatusService>();
builder.Services.AddScoped<ApiKeyService>();

builder.Services.AddResponseCaching();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().WithMethods("GET", "POST", "PATCH", "DELETE")));

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);

var app = builder.Build();

app.UseCors();
app.UseResponseCaching();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Logger.LogInformation("Starting National WID API...");
app.Run();

// Make Program class accessible to tests
public partial class Program { }
