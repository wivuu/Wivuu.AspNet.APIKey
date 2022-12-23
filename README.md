# Wivuu.AspNet.APIKey

This project implements a secure API key authentication & authorization system built atop .NET's Data Protection APIs.

[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=wivuu_Wivuu.AspNetCore.APIKey&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=wivuu_Wivuu.AspNetCore.APIKey)

## Roadmap
- [x] Update sample to support Swagger doc authentication
- [x] Add option mechanism for extracting API key differently from request
- [x] Add tests
- [x] Add Sonarcloud CI actions
- [x] Document usage scenarios
- [ ] Add CD to nuget

## Usage

### 1. Install the package

```bash
dotnet add package Wivuu.AspNet.APIKey
```

### 2. Configure the API key provider

```csharp
builder.Services.AddControllers();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = "x-api-key";
        options.DefaultChallengeScheme = "x-api-key";
    })
    // The <T> parameter is used to pass in the structure of the encrypted key, it must be of type `IDataProtectedKey`.
    // this library comes with `DefaultUserIdKey<T>` which can be used as a basic wrapper around any string or unique id,
    // but can be extended to support more complex claims.
    .AddWivuuDataProtectedAPIKeySchema<DefaultUserIdKey<Guid>>("x-api-key", options =>
    {
        // Configure options here:

        // 1. Usage purpose; this is mapped to the DataProtection `purpose` parameter
        // options.UsagePurpose = "my-usage-purpose";

        // 2. The cache duration for successful authentications, typically not necessary except perhaps in
        //    high traffic scenarios
        // options.CacheDurationSuccess = TimeSpan.FromSeconds(10);

        // 3. The failure cache duration will cache failed authentications for a period of time to prevent
        //    excessive calls to decryption algorithm for bad keys. Defaults to 10 seconds
        // options.CacheDurationFailure = TimeSpan.FromSeconds(30);

        // 4. BuildAuthenticationResponseAsync allows you to customize the creation of your 
        //    AuthenticationTicket. This is useful if you want to add additional claims to the
        //    ticket or if you want to use a different authentication scheme

        // 5. GetKeyFromRequest allows you to retrieve the key from alternative locations in the request, such
        //    as a header, querystring, or cookie. By default, the key is expected to be in an `x-api-key` header.

        // 6. If you want to use a different scheme, you can configure the scheme name here
        // options.Scheme = "my-scheme-name";
    });
```

### 3. Use the scheme on a controller

```csharp
// Add the [Authorize] attribute to the controller in order to enforce authentication of the API key
[HttpGet(Name = "Test API Key")]
[Authorize]
public IActionResult Get()
{
}
```

### 4. Issue new keys
Keys can be generated using the `DataProtectedAPIKeyGenerator`, which is added automatically to your `ServiceProvider` by calling `AddWivuuDataProtectedAPIKeySchema`.

```csharp
// Generate a new key
var generator = serviceProvider.GetRequiredService<DataProtectedAPIKeyGenerator>();
var userId = Guid.NewGuid();
var key = new DefaultUserIdKey<Guid>(userId);

// Generates a protected API key string, optionally make it temporary.
var encryptedKeyIndefinite = generator.ProtectKey(key);
var encryptedKeyTemporary = generator.ProtectKey(key, TimeSpan.FromDays(30));

// Either of these keys can now be used in the x-api-key header to re-validate this user
```