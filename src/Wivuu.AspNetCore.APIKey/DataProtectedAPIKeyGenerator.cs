using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace Wivuu.AspNetCore.APIKey;

public class DataProtectedAPIKeyGeneratorOptions
{
    public string UsagePurpose { get; set; } = "x-api-key";
}

public class DataProtectedAPIKeyGenerator
{
    public DataProtectedAPIKeyGenerator(
        IDataProtectionProvider protectionProvider,
        IOptionsMonitor<DataProtectedAPIKeyGeneratorOptions> options
    )
    {
        ProtectionProvider = protectionProvider;
        Options = options;
    }

    protected IDataProtectionProvider ProtectionProvider { get; }
    public IOptionsMonitor<DataProtectedAPIKeyGeneratorOptions> Options { get; }

    public string ProtectKey(IDataProtectedKey key, TimeSpan timeSpan)
    {
        var protector = ProtectionProvider
            .CreateProtector(Options.CurrentValue.UsagePurpose)
            .ToTimeLimitedDataProtector();

        var protectedBytes = protector.Protect(key.ToTokenBytes(), DateTimeOffset.UtcNow + timeSpan);

        return Convert.ToBase64String(protectedBytes);
    }

    public string ProtectKey(IDataProtectedKey key)
    {
        var protector = ProtectionProvider
            .CreateProtector(Options.CurrentValue.UsagePurpose)
            .ToTimeLimitedDataProtector();

        var protectedBytes = protector.Protect(key.ToTokenBytes());

        return Convert.ToBase64String(protectedBytes);
    }

    public string ProtectKey(IDataProtectedKey key, DateTimeOffset expires)
    {
        var protector = ProtectionProvider
            .CreateProtector(Options.CurrentValue.UsagePurpose)
            .ToTimeLimitedDataProtector();

        var protectedBytes = protector.Protect(key.ToTokenBytes(), expires);

        return Convert.ToBase64String(protectedBytes);
    }
}