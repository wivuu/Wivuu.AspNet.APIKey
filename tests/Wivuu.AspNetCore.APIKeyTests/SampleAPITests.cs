namespace Wivuu.AspNetCore.APIKeyTests;

public class SampleAPITests : BaseTests
{
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

        for (var i = 0; i < 10; ++i)
        {
            // Act
            var response = await HttpClient.GetAsync("/Sample");

            // Assert
            Assert.False(response.IsSuccessStatusCode, "Expected 401");
        }

        for (var i = 0; i < 10; ++i)
        {
            // Act
            HttpClient.DefaultRequestHeaders.Authorization = new ("Bearer", newKey);
            var response = await HttpClient.GetAsync("/Sample");

            // Assert
            Assert.True(response.IsSuccessStatusCode, "Expected 200");
        }
    }
}