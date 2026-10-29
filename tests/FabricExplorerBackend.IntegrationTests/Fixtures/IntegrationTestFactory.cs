using FabricExplorerBackend.Persistences;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using StackExchange.Redis;
using Testcontainers.PostgreSql;

namespace FabricExplorerBackend.IntegrationTests.Fixtures
{
    public class IntegrationTestFactory(TestEnvironment env) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                ReplaceDatabase(services);
                ConfigureRedis(services);
                ConfigureAuthentication(services);
            });
        }

        private void ReplaceDatabase(IServiceCollection services)
        {
            services.AddDbContext<FabricExplorerDbContext>(options =>
            {
                options.UseNpgsql(env.PostgreSql.GetConnectionString());
            });
            // Remove application DbContext registration
            // Register PostgreSQL using
            // _environment.PostgreSql.GetConnectionString()
        }

        private void ConfigureRedis(IServiceCollection services)
        {
            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var options = ConfigurationOptions.Parse(env.Redis.GetConnectionString());
                options.AbortOnConnectFail = false;
                return ConnectionMultiplexer.Connect(options);
            });
            // Configure Redis using
            // _environment.Redis.GetConnectionString()
        }

        private static void ConfigureAuthentication(IServiceCollection services)
        {
            // TestAuthenticationHandler
        }
    }
}
