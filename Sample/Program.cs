using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = "x-api-key";
        options.DefaultChallengeScheme = "x-api-key";
    })
    .AddScheme<DataProtectedAPIKeyOptions<SampleDataProtectionKey>, 
               DataProtectedAPIKeyHandler<SampleDataProtectionKey>
    >("x-api-key", options =>
    {
        options.UsagePurpose = "x-api-key";
        options.CacheDurationSuccess = TimeSpan.FromSeconds(10);
        options.CacheDurationFailure = TimeSpan.FromSeconds(1);
        options.BuildAuthenticationResponseAsync = (key) =>
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

public class DataProtectedAPIKeyOptions<TDataProtectionKey> : AuthenticationSchemeOptions
    where TDataProtectionKey : IDataProtectionKey
{
    /// <summary>
    /// Provides isolated purpose between different usages using the same key
    /// </summary>
    public string UsagePurpose { get; set; } = "x-api-key";

    /// <summary>
    /// The duration for which to cache successfully authenticated tokens
    /// </summary>
    public TimeSpan? CacheDurationSuccess { get; set; }

    /// <summary>
    /// The duration for which to cache failed authentication attempts, default 10 seconds
    /// </summary>
    public TimeSpan CacheDurationFailure { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Opportunity
    /// </summary>
    public Func<TDataProtectionKey, Task<AuthenticateResult>>? BuildAuthenticationResponseAsync { get; set; } = null;
}

public interface IDataProtectionKey
{
    /// <summary>
    /// Represent your API key as bytes
    /// </summary>
    byte[] ToTokenBytes();

    /// <summary>
    /// Deserialize your API key from bytes
    /// </summary>
    static abstract bool TryParseTokenBytes(byte[] tokenBytes, out IDataProtectionKey? key);
}

public class SampleDataProtectionKey : IDataProtectionKey
{
    public static bool TryParseTokenBytes(byte[] tokenBytes, out IDataProtectionKey? key)
    {
        if (tokenBytes[0] == 0x01)
        {
            key = new SampleDataProtectionKey();
            return true;
        }
        else
        {
            key = null;
            return false;
        }
    }

    public byte[] ToTokenBytes()
    {
        return new byte[] { 0x01 };
    }
}

public class DataProtectedAPIKeyHandler<TDataProtectionKey> : AuthenticationHandler<DataProtectedAPIKeyOptions<TDataProtectionKey>>
    where TDataProtectionKey : IDataProtectionKey
{
    public DataProtectedAPIKeyHandler(
        IOptionsMonitor<DataProtectedAPIKeyOptions<TDataProtectionKey>> options, 
        ILoggerFactory logger, 
        UrlEncoder encoder, 
        ISystemClock clock) 
        : base(options, logger, encoder, clock)
    {
    }

    /// <summary>
    /// Retrieve key used in request - by default from the `x-api-key` header
    /// </summary>
    protected virtual string? GetKeyFromRequest(HttpRequest request)
    {
        var authorization = request.Headers.Authorization;

        if (request.Headers.TryGetValue("x-api-key", out var key) && key is { Count: > 0 })
            return key[0];

        return null;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Retrieve the token from the request header
        if (GetKeyFromRequest(Context.Request) is not { } protectedValue)
            return Task.FromResult(AuthenticateResult.Fail("No key provided"));

        // Get cache
        if (Options.CacheDurationSuccess is {} successCache)
        {
            var cache = Context.RequestServices.GetRequiredService<IMemoryCache>();
            var cacheKey = HashCode.Combine("wivuu-api-key", Options.UsagePurpose, protectedValue);

            return cache.GetOrCreateAsync(cacheKey, async (entry) =>
            {
                // Attempt to decrypt the token
                if (GetDataProtectionKeyFromValue(protectedValue) is not { } key)
                {
                    entry.AbsoluteExpirationRelativeToNow = Options.CacheDurationFailure;
                    return AuthenticateResult.Fail("Invalid key");
                }
                else
                {
                    entry.AbsoluteExpirationRelativeToNow = successCache;
                    return await ValidateKeyAsync(key);
                }
            })!;
        }
        else
        {
            // Attempt to decrypt the token
            if (GetDataProtectionKeyFromValue(protectedValue) is not { } key)
                return Task.FromResult(AuthenticateResult.Fail("Invalid key"));
            else
                return ValidateKeyAsync(key);
        }

        // Unprotect the token and retrieve underlying structured key
        TDataProtectionKey? GetDataProtectionKeyFromValue(string value)
        {
            var protectionProvider = Context.RequestServices.GetRequiredService<IDataProtectionProvider>();
            var protector = protectionProvider.CreateProtector(Options.UsagePurpose).ToTimeLimitedDataProtector();

            try
            {
                var protectedBytes   = Convert.FromBase64String(value);
                var unprotectedValue = protector.Unprotect(protectedBytes);

                if (TDataProtectionKey.TryParseTokenBytes(unprotectedValue, out var key) &&
                    key is TDataProtectionKey dataProtectionKey)
                    return dataProtectionKey;

                return default;
            }
            catch (CryptographicException)
            {
                return default;
            }
        }

        // Provide option to validate key
        Task<AuthenticateResult> ValidateKeyAsync(TDataProtectionKey key)
        {
            // Validate the key
            if (Options.BuildAuthenticationResponseAsync is not null)
                return Options.BuildAuthenticationResponseAsync(key);

            var identity = new ClaimsIdentity("x-api-key");

            var defaultTicket = new AuthenticationTicket(
                new ClaimsPrincipal(identity), 
                "x-api-key");

            return Task.FromResult(AuthenticateResult.Success(defaultTicket));
        }
    }
}

public class DataProtectedAPIKeyGeneratorOptions
{
    public string UsagePurpose { get; set; } = "x-api-key";
}

public class DataProtectedAPIKeyGenerator
{
    public DataProtectedAPIKeyGenerator(
        IDataProtectionProvider protectionProvider,
        IOptions<DataProtectedAPIKeyGeneratorOptions> options
    )
    {
        ProtectionProvider = protectionProvider;
        Options = options.Value;
    }

    protected IDataProtectionProvider ProtectionProvider { get; }
    protected DataProtectedAPIKeyGeneratorOptions Options { get; }

    public string ProtectKey(SampleDataProtectionKey key, TimeSpan timeSpan)
    {
        var protector = ProtectionProvider
            .CreateProtector(Options.UsagePurpose)
            .ToTimeLimitedDataProtector();

        var protectedBytes = protector.Protect(key.ToTokenBytes(), DateTimeOffset.UtcNow + timeSpan);

        return Convert.ToBase64String(protectedBytes);
    }

    public string ProtectKey(SampleDataProtectionKey key)
    {
        var protector = ProtectionProvider
            .CreateProtector(Options.UsagePurpose)
            .ToTimeLimitedDataProtector();

        var protectedBytes = protector.Protect(key.ToTokenBytes());

        return Convert.ToBase64String(protectedBytes);
    }

    public string ProtectKey(SampleDataProtectionKey key, DateTimeOffset expires)
    {
        var protector = ProtectionProvider
            .CreateProtector(Options.UsagePurpose)
            .ToTimeLimitedDataProtector();

        var protectedBytes = protector.Protect(key.ToTokenBytes(), expires);

        return Convert.ToBase64String(protectedBytes);
    }
}