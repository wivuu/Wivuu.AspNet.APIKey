
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
        var user = Request.HttpContext.User;

        return Ok($"Hello {user.Identity?.Name ?? "anonymous"}");
    }

    [HttpGet("GetNewKey", Name = "Unprotected")]
    public string GenerateApiKey(
        [FromServices] DataProtectedAPIKeyGenerator generator,
        int? validMinutes = null, 
        string userId = "123")
    {
        var key = new MyDataKey(userId);

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