
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wivuu.AspNetCore.APIKey;

namespace Sample.Controllers;

[ApiController]
[Route("[controller]")]
public class SampleController : ControllerBase
{
    [HttpGet(Name = "Test API Key")]
    [Authorize]
    public IActionResult Get()
    {
        var user = Request.HttpContext.User;

        return Ok($"Hello {user.Identity?.Name ?? "anonymous"}");
    }

    [HttpGet("GetNewKey", Name = "Unprotected")]
    public string GenerateApiKey(
        [FromServices] DataProtectedAPIKeyGenerator generator,
        Guid? userId,
        int? validMinutes = null
    )
    {
        var key = new DefaultUserIdKey<Guid>(userId ?? Guid.NewGuid());

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