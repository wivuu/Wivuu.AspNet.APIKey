
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
        int validMinutes = 5)
    {
        var key = new SampleDataProtectionKey();

        return generator.ProtectKey(key, TimeSpan.FromMinutes(validMinutes));
    }
}