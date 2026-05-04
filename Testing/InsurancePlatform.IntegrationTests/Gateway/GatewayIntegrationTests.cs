using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using InsurancePlatform.IntegrationTests.Support;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Xunit;

namespace InsurancePlatform.IntegrationTests.Gateway;

public sealed class GatewayIntegrationTests
{
    [Fact]
    public async Task IdentityAuthRoute_ForwardsAnonymousRequest()
    {
        await using var authStub = await StartStubServerAsync(app =>
        {
            app.MapPost("/auth/login", () => Results.Json(new { accessToken = "stub-token" }));
        });

        var gateway = await StartGatewayAsync(BuildGatewayConfig(authStub.Port, authStub.Port));
        var client = gateway.GetTestClient();

        var response = await client.PostAsJsonAsync("/identity/auth/login", new { email = "user@test.com", password = "Password@1" });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("stub-token", body);
        await gateway.DisposeAsync();
    }

    [Fact]
    public async Task ProtectedTicketsRoute_RequiresAuthentication()
    {
        await using var ticketStub = await StartStubServerAsync(app =>
        {
            app.MapGet("/tickets/ping", () => Results.Ok(new { ok = true }));
        });

        var gateway = await StartGatewayAsync(BuildGatewayConfig(ticketStub.Port, ticketStub.Port));
        var client = gateway.GetTestClient();

        var response = await client.GetAsync("/tickets/ping");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await gateway.DisposeAsync();
    }

    [Fact]
    public async Task ProtectedTicketsRoute_ForwardsWhenAuthenticated()
    {
        await using var ticketStub = await StartStubServerAsync(app =>
        {
            app.MapGet("/tickets/ping", () => Results.Json(new { ok = true, route = "ticket" }));
        });

        var gateway = await StartGatewayAsync(BuildGatewayConfig(ticketStub.Port, ticketStub.Port));
        var client = gateway.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Test-UserId", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("X-Test-Role", "Customer");

        var response = await client.GetAsync("/tickets/ping");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("ticket", body);
        await gateway.DisposeAsync();
    }

    private static string BuildGatewayConfig(int authPort, int ticketPort) => $$"""
{
  "Routes": [
    {
      "DownstreamPathTemplate": "/auth/{everything}",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [
        { "Host": "127.0.0.1", "Port": {{authPort}} }
      ],
      "UpstreamPathTemplate": "/identity/auth/{everything}",
      "UpstreamHttpMethod": [ "GET", "POST" ]
    },
    {
      "DownstreamPathTemplate": "/tickets/{everything}",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [
        { "Host": "127.0.0.1", "Port": {{ticketPort}} }
      ],
      "UpstreamPathTemplate": "/tickets/{everything}",
      "UpstreamHttpMethod": [ "GET", "POST", "PUT" ],
      "AuthenticationOptions": { "AuthenticationProviderKey": "Bearer" }
    }
  ],
  "GlobalConfiguration": {
    "BaseUrl": "http://localhost"
  }
}
""";

    private static async Task<WebApplication> StartGatewayAsync(string configJson)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "InsurancePlatform.IntegrationTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var configPath = Path.Combine(tempDir, "ocelot.json");
        await File.WriteAllTextAsync(configPath, configJson, Encoding.UTF8);

        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddJsonFile(configPath, optional: false, reloadOnChange: false);
        builder.Services.AddAuthentication("Bearer")
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Bearer", _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddOcelot(builder.Configuration);

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        await app.UseOcelot();
        await app.StartAsync();
        return app;
    }

    private static async Task<StubServer> StartStubServerAsync(Action<WebApplication> configure)
    {
        var port = GetFreePort();
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls($"http://127.0.0.1:{port}");
        var app = builder.Build();
        configure(app);
        await app.StartAsync();
        return new StubServer(app, port);
    }

    private static int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private sealed class StubServer(WebApplication app, int port) : IAsyncDisposable
    {
        public int Port { get; } = port;

        public async ValueTask DisposeAsync()
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }
}
