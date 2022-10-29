
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
public class SampleController : ControllerBase
{
    [HttpGet(Name = "Test API Key")]
    [Authorize(AuthenticationSchemes = "x-api-key")]
    public IActionResult Get()
    {
        return Ok("Hello World!");
    }

    [HttpGet("GetNewKey", Name = "Unprotected")]
    public string GenerateApiKey(
        [FromServices] DataProtectedAPIKeyGenerator generator,
        int? validMinutes = null)
    {
        var key = new SampleDataProtectionKey();

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