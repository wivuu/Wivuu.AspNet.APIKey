using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Wivuu.AspNetCore.APIKey;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = "x-api-key";
        options.DefaultChallengeScheme = "x-api-key";
    })
    .AddScheme<DataProtectedAPIKeyOptions<DefaultAPIKey>, DataProtectedAPIKeyHandler<DefaultAPIKey>>("x-api-key", options =>
    {
        options.UsagePurpose = "x-api-key";
        options.CacheDurationSuccess = TimeSpan.FromSeconds(10);
        options.CacheDurationFailure = TimeSpan.FromSeconds(1);
        options.BuildAuthenticationResponseAsync = (services, key) =>
        {
            var principal = new ClaimsPrincipal();
            principal.AddIdentity(new ClaimsIdentity("x-api-key"));

            var defaultTicket = new AuthenticationTicket(
                principal,
                new AuthenticationProperties(),
                "x-api-key");

            return Task.FromResult(AuthenticateResult.Success(defaultTicket));
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
builder.Services.AddControllers();

builder.Services.AddSwaggerGen(options => {
    // Include XML comments
    options.IncludeXmlComments(
        Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml")
    );
});

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