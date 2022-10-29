using Microsoft.AspNetCore.Authentication;
using Wivuu.AspNetCore.APIKey;

namespace Microsoft.Extensions.DependencyInjection;

public static class DataProtectedAPIKeyExtensions
{
    /// <summary>
    /// Adds Wivuu data protected API key authentication scheme
    /// </summary>
    /// <param name="builder">The authentication builder</param>
    /// <param name="authenticationScheme">The name of the scheme</param>
    /// <param name="configureOptions">Configuration options</param>
    /// <param name="displayName">The display name, if any</param>
    /// <typeparam name="TDataProtectionKey">The type of API key to use</typeparam>
    /// <returns>The authentication builder</returns>
    public static AuthenticationBuilder AddWivuuDataProtectedAPIKeySchema<TDataProtectionKey>(
        this AuthenticationBuilder builder, 
        string authenticationScheme,
        Action<DataProtectedAPIKeyOptions<TDataProtectionKey>> configureOptions,
        string? displayName = null)
        where TDataProtectionKey : IDataProtectedKey
    {
        return builder.AddScheme<DataProtectedAPIKeyOptions<TDataProtectionKey>, DataProtectedAPIKeyHandler<TDataProtectionKey>>(
            authenticationScheme, 
            displayName, 
            configureOptions);
    }
}