
using System.Security.Cryptography;
using Wivuu.AspNetCore.APIKey;

namespace Wivuu.AspNetCore.APIKeyTests;

public class APIKeyTests : BaseTests
{
    [Fact]
    public void TestSuccessfulTokenParse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var key = new DefaultUserIdKey<Guid>(userId);

        // Act
        var tokenBytes = key.ToTokenBytes();

        if (DefaultUserIdKey<Guid>.TryParseTokenBytes(tokenBytes, out var newKeyParsed) &&
            newKeyParsed is DefaultUserIdKey<Guid> newKey)
        {
            // Assert
            Assert.NotEqual(userId.ToString(), key.ToString());
            Assert.Equal(userId, newKey.UserId);
        }
        else
        {
            Assert.True(false, "Failed to parse token bytes");
        }
    }

    [Fact]
    public void TestFailingTokenParse()
    {
        // Arrange - Generate random bytes
        var tokenBytes = new byte[16];
        RandomNumberGenerator.Fill(tokenBytes);

        // Act
        var parsed = DefaultUserIdKey<Guid>.TryParseTokenBytes(tokenBytes, out var newKeyParsed);
        Assert.False(parsed, "Parsed token bytes when it should have failed");
        Assert.Null(newKeyParsed);
    }

    [Fact]
    public void TestProtectKey()
    {
        var generator = Services.GetRequiredService<DataProtectedAPIKeyGenerator>();

        var key = generator.ProtectKey(new DefaultUserIdKey<Guid>(Guid.NewGuid()), TimeSpan.FromSeconds(1));
        Assert.NotNull(key);
    }
}
