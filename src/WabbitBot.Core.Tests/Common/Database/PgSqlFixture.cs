using System;
using System.Threading.Tasks;
using Testcontainers.PostgreSql;
using Xunit;

namespace WabbitBot.Core.Common.Database.Tests
{
    public sealed class PgSqlFixture : IAsyncLifetime
    {
        private PostgreSqlContainer? _container;
        public string ConnectionString { get; private set; } = string.Empty;
        public bool Available { get; private set; }

        public async Task InitializeAsync()
        {
            try
            {
                _container = new PostgreSqlBuilder()
                    .WithImage("postgres:16-alpine")
                    .WithDatabase($"wabbit_tests_{Guid.NewGuid():N}")
                    .WithUsername("wabbit")
                    .WithPassword("wabbitpwd")
                    .Build();

                await _container.StartAsync();
                ConnectionString = _container.GetConnectionString();
                Available = true;
            }
            catch
            {
                Available = false;
                ConnectionString = string.Empty;
            }
        }

        public async Task DisposeAsync()
        {
            if (_container is not null)
            {
                try { await _container.StopAsync(); } catch { }
                try { await _container.DisposeAsync(); } catch { }
            }
        }
    }

    [CollectionDefinition("pgsql", DisableParallelization = true)]
    public sealed class PgSqlCollection : ICollectionFixture<PgSqlFixture>
    {
    }
}
