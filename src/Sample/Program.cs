using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Wivuu.AspNetCore.APIKey;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = "x-api-key";
        options.DefaultChallengeScheme = "x-api-key";
    })
    .AddWivuuDataProtectedAPIKeySchema<DefaultUserIdKey<Guid>>("x-api-key", options =>
    {
        options.UsagePurpose = "x-api-key";
        options.CacheDurationSuccess = TimeSpan.FromSeconds(10);
        options.CacheDurationFailure = TimeSpan.FromSeconds(1);
        options.GetKeyFromRequest = (req) =>
        {
            // Retrieve the token from bearer token
            if (AuthenticationHeaderValue.TryParse(req.Headers.Authorization, out var authHeader) &&
                string.Equals(authHeader.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase))
                return authHeader.Parameter;

            return null;
        };
        options.BuildAuthenticationResponseAsync = (services, key) =>
        {
            var ident = new ClaimsIdentity("x-api-key", ClaimTypes.NameIdentifier, ClaimTypes.Role);
            ident.AddClaim(new Claim(ClaimTypes.NameIdentifier, key.UserId.ToString()));

            var principal = new ClaimsPrincipal(ident);
            var ticket    = new AuthenticationTicket(principal, "x-api-key");

            return Task.FromResult(AuthenticateResult.Success(ticket));
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder(authenticationSchemes: "x-api-key")
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddMemoryCache();

builder.Services.AddSingleton<DataProtectedAPIKeyGenerator>();

builder.Services.AddLogging();
builder.Services.AddOptions();
builder.Services.AddControllers().AddControllersAsServices();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSwaggerGen(options => 
    {
        // Include XML comments
        options.IncludeXmlComments(
            Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml")
        );

        // Enable auth w/ swagger UI
        var authScheme = new OpenApiSecurityScheme()
        {
            In           = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Type         = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Name         = "Authorization",
            Scheme       = "Bearer",
            BearerFormat = "x-api-key",
            Reference = new ()
            {
                Type = ReferenceType.SecurityScheme,
                Id   = "Bearer",
            }
        };

        options.AddSecurityDefinition("Bearer", authScheme);

        options.AddSecurityRequirement(new ()
        {
            [authScheme] = Array.Empty<string>(),
        });
    });
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

namespace Sample
{
    public partial class Program { }
}