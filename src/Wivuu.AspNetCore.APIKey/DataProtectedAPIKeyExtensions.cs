using Microsoft.AspNetCore.Authentication;

namespace Wivuu.AspNetCore.APIKey
{
    public static class DataProtectedAPIKeyExtensions
    {
        public static AuthenticationBuilder AddDataProtectedAPIKey<TDataProtectionKey>(
            this AuthenticationBuilder builder, 
            string authenticationScheme, 
            string displayName, 
            Action<DataProtectedAPIKeyOptions<TDataProtectionKey>> configureOptions) 
            where TDataProtectionKey : IDataProtectionKey
        {
            return builder.AddScheme<DataProtectedAPIKeyOptions<TDataProtectionKey>, DataProtectedAPIKeyHandler<TDataProtectionKey>>(
                authenticationScheme, 
                displayName, 
                configureOptions);
        }
    }    
}