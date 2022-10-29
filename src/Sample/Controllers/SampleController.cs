
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wivuu.AspNetCore.APIKey;

[ApiController]
[Route("[controller]")]
public class SampleController : ControllerBase
{
    [HttpGet(Name = "Test API Key")]
    [Authorize(AuthenticationSchemes = "x-api-key")]
    public IActionResult Get()
    {
        return Ok("Secret!");
    }

    [HttpGet("GetNewKey", Name = "Unprotected")]
    public string GenerateApiKey(
        [FromServices] DataProtectedAPIKeyGenerator generator,
        int? validMinutes = null)
    {
        var key = new DefaultAPIKey();

        if (validMinutes.HasValue)
        {
            return generator.ProtectKey(key, TimeSpan.FromMinutes(validMinutes.Value));
        }
        else
        {
            return generator.ProtectKey(key);
        }
    }
}