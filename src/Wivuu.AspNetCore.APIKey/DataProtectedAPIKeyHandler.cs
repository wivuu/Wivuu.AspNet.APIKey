using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Wivuu.AspNetCore.APIKey;

public class DataProtectedAPIKeyHandler<TDataProtectionKey> : AuthenticationHandler<DataProtectedAPIKeyOptions<TDataProtectionKey>>
    where TDataProtectionKey : IDataProtectedKey
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
        if (request.Headers.TryGetValue("x-api-key", out var key) && key is { Count: > 0 })
            return key[0];

        return null;
    }

    /// <summary>
    /// Handle each authentication request
    /// </summary>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Retrieve the token from the request header
        var getKey = Options.GetKeyFromRequest ?? GetKeyFromRequest;
        if (getKey(Context.Request) is not { } protectedValue)
            return Task.FromResult(AuthenticateResult.Fail("No key provided"));

        // Get cache
        if (Options.CacheDurationSuccess is {} successCache)
        {
            var cache    = Context.RequestServices.GetRequiredService<IMemoryCache>();
            var cacheKey = HashCode.Combine("wivuu-api-key", Options.UsagePurpose, protectedValue);

            return cache.GetOrCreateAsync(cacheKey, async (entry) =>
            {
                // Attempt to decrypt the token
                if (TryGetDataProtectionKeyFromValue(protectedValue, out var key))
                {
                    entry.AbsoluteExpirationRelativeToNow = successCache;
                    return await ValidateKeyAsync(key);
                }
                else
                {
                    entry.AbsoluteExpirationRelativeToNow = Options.CacheDurationFailure;
                    return AuthenticateResult.Fail("Invalid key");
                }
            })!;
        }
        else
        {
            // Attempt to decrypt the token
            if (TryGetDataProtectionKeyFromValue(protectedValue, out var key))
                return ValidateKeyAsync(key);
            else
                return Task.FromResult(AuthenticateResult.Fail("Invalid key"));
        }
    }

    // Unprotect the token and retrieve underlying structured key
    private bool TryGetDataProtectionKeyFromValue(string value, [NotNullWhen(true)] out TDataProtectionKey? result)
    {
        var protectionProvider = Context.RequestServices.GetRequiredService<IDataProtectionProvider>();
        var protector = protectionProvider.CreateProtector(Options.UsagePurpose).ToTimeLimitedDataProtector();

        try
        {
            var protectedBytes   = Convert.FromBase64String(value);
            var unprotectedValue = protector.Unprotect(protectedBytes);

            if (TDataProtectionKey.TryParseTokenBytes(unprotectedValue, out var key) &&
                key is TDataProtectionKey dataProtectionKey)
            {
                result = dataProtectionKey;
                return true;
            }

            result = default;
            return false;
        }
        catch (CryptographicException)
        {
            result = default;
            return false;
        }
    }

    // Provide option to validate key
    private Task<AuthenticateResult> ValidateKeyAsync(TDataProtectionKey key)
    {
        // Validate the key
        if (Options.BuildAuthenticationResponseAsync is not null)
            return Options.BuildAuthenticationResponseAsync(Context.RequestServices, key);

        var identity = new ClaimsIdentity(Options.Scheme, ClaimTypes.NameIdentifier, ClaimTypes.Role);
        var principal = new ClaimsPrincipal(identity);
        var defaultTicket = new AuthenticationTicket(principal, Options.Scheme);

        return Task.FromResult(AuthenticateResult.Success(defaultTicket));
    }
}