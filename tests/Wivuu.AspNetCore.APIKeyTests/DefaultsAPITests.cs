using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Wivuu.AspNetCore.APIKey;

namespace Wivuu.AspNetCore.APIKeyTests;

public class DefaultsAPITests : BaseTests
{
    protected override void BuildApplication(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove 'DataProtectedAPIKeyHandler from services
            for (var i = services.Count - 1; i >= 0; i--)
            {
                var type = services[i].ServiceType;

                if (type == typeof(DataProtectedAPIKeyHandler<DefaultUserIdKey<Guid>>))
                    services.RemoveAt(i);
                else if (type == typeof(IConfigureOptions<DataProtectedAPIKeyOptions<DefaultUserIdKey<Guid>>>))
                    services.RemoveAt(i);
                else if (type == typeof(IConfigureOptions<AuthenticationOptions>))
                    services.RemoveAt(i);
                else if (type == typeof(IAuthenticationService))
                    services.RemoveAt(i);
                else if (type == typeof(IAuthenticationHandlerProvider))
                    services.RemoveAt(i);
                else if (type == typeof(IAuthenticationSchemeProvider))
                    services.RemoveAt(i);
                else if (type == typeof(IOptionsMonitorCache<AuthenticationOptions>))
                    services.RemoveAt(i);
                else if (type == typeof(IOptionsFactory<AuthenticationOptions>))
                    services.RemoveAt(i);
                else if (type == typeof(IOptionsMonitor<AuthenticationOptions>))
                    services.RemoveAt(i);
            }

            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "x-api-key";
                    options.DefaultChallengeScheme = "x-api-key";
                })
                .AddWivuuDataProtectedAPIKeySchema<DefaultUserIdKey<Guid>>("x-api-key", options =>
                {
                    options.Scheme = "x-api-key";
                });
        });
    }

    [Fact]
    public async Task TestGenerateKeyAndUseKey()
    {
        // Arrange
        var userId = Guid.NewGuid();
        string newKey;

        {
            // Act
            newKey = await HttpClient.GetStringAsync(
                $"/Sample/GetNewKey?userId={userId}&validMinutes=1");

            // Assert
            Assert.NotNull(newKey);
            Assert.NotEqual(userId.ToString(), newKey);
        }

        {
            // Act
            newKey = await HttpClient.GetStringAsync(
                $"/Sample/GetNewKey?userId={userId}");

            // Assert
            Assert.NotNull(newKey);
            Assert.NotEqual(userId.ToString(), newKey);
        }

        for (var i = 0; i < 10; ++i)
        {
            // Act
            var response = await HttpClient.GetAsync("/Sample");

            // Assert
            Assert.False(response.IsSuccessStatusCode, "Expected 401");
        }

        HttpClient.DefaultRequestHeaders.Add("x-api-key", newKey);
        for (var i = 0; i < 10; ++i)
        {
            // Act
            var response = await HttpClient.GetAsync("/Sample");

            // Assert
            if (!response.IsSuccessStatusCode)
            {
                var rsponseBody = await response.Content.ReadAsStringAsync();
            }
            Assert.True(response.IsSuccessStatusCode, "Expected 200");
        }

        HttpClient.DefaultRequestHeaders.Remove("x-api-key");
        HttpClient.DefaultRequestHeaders.Add("x-api-key", "BogusKey");
        for (var i = 0; i < 10; ++i)
        {
            // Act
            var response = await HttpClient.GetAsync("/Sample");

            // Assert
            Assert.False(response.IsSuccessStatusCode, "Expected NOT 200");
        }
    }
}