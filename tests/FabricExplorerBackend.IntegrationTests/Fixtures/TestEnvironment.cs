using System;
using System.Collections.Generic;
using System.Text;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace FabricExplorerBackend.IntegrationTests.Fixtures
{
    public sealed class TestEnvironment : IAsyncLifetime
    {
        public PostgreSqlContainer PostgreSql { get; }

        public RedisContainer Redis { get; }

        public TestEnvironment()
        {
            PostgreSql = new PostgreSqlBuilder("postgres:17")
                .WithDatabase("fabric_explorer_test")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();

            Redis = new RedisBuilder("redis:8")
                .Build();
        }

        public async Task InitializeAsync()
            => await Task.WhenAll(PostgreSql.StartAsync(), Redis.StartAsync());

        public async Task DisposeAsync()
            => await Task.WhenAll(PostgreSql.DisposeAsync().AsTask(), Redis.DisposeAsync().AsTask());
    }
}
