using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Wivuu.AspNetCore.APIKeyTests;

public abstract class BaseTests : IDisposable
{
    protected BaseTests()
    {
        Application = new WebApplicationFactory<Sample.Program>().WithWebHostBuilder(builder =>
        {
            BuildApplication(builder);
        });

        Services    = Application.Services;
        HttpClient  = Application.CreateClient();
    }

    public WebApplicationFactory<Sample.Program> Application { get; }
    public IServiceProvider Services { get; }
    public HttpClient HttpClient { get; }

    protected virtual void BuildApplication(IWebHostBuilder builder)
    {
    }

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);

        HttpClient.Dispose();
        Application.Dispose();
    }
}