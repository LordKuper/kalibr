using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

[assembly: AssemblyFixture(typeof(Kalibr.Tests.Infrastructure.TestEnvironment))]

namespace Kalibr.Tests.Infrastructure;

/// <summary>
/// The environment shared by the whole test run: one PostgreSQL container, from which every API host gets a database of its own
/// owned by an application role without superuser rights, as in deployment, and one mail server that every host sends its mail to unless a test points it elsewhere.
/// Tests that only create their own data share one lazily started host; tests that need a database to themselves start another.
/// </summary>
public sealed class TestEnvironment : IAsyncLifetime
{
    private const string PostgresImage = "postgres:18.6-trixie";
    private const string GssSetting = "GSS Encryption Mode=Disable";
    private const string AppRole = "kalibr";
    private const string AppPassword = "kalibr-test-password";

    /// <summary>
    /// The server's connection limit. The default of 100 runs out while the hosts of the classes that run in parallel start together
    /// and keep their pooled connections, and a host or a test then fails with "too many clients".
    /// </summary>
    private const string ConnectionLimitSetting = "max_connections=500";

    private readonly PostgreSqlContainer container = new PostgreSqlBuilder(PostgresImage).WithCommand("-c", ConnectionLimitSetting).Build();
    private readonly Lazy<Task<ApiHost>> shared;

    public TestEnvironment()
    {
        shared = new Lazy<Task<ApiHost>>(() => StartHostAsync());
    }

    /// <summary>
    /// The mail server that the hosts of this environment send to, where the tests read the mail back.
    /// </summary>
    public MailCatcher Mail { get; } = new();

    /// <summary>
    /// The host shared by tests that never depend on the set of accounts or units existing besides their own.
    /// </summary>
    public Task<ApiHost> SharedHostAsync() => shared.Value;

    /// <summary>
    /// The configuration of a host that sends its mail to <see cref="Mail"/>, with the given overrides on top.
    /// </summary>
    /// <param name="overrides">Settings that replace the mail server's, as environment variable names with <c>__</c> separators.</param>
    /// <returns>The complete settings of the host.</returns>
    public IReadOnlyDictionary<string, string> WithMail(IReadOnlyDictionary<string, string>? overrides = null)
    {
        var settings = new Dictionary<string, string>(Mail.HostSettings);
        foreach (var (name, value) in overrides ?? new Dictionary<string, string>())
        {
            settings[name] = value;
        }

        return settings;
    }

    /// <summary>
    /// Starts a host on a new empty database, so migrations and the first administrator are created by that host.
    /// </summary>
    /// <param name="settings">Configuration overrides for this host, as environment variable names with <c>__</c> separators.</param>
    public async Task<ApiHost> StartHostAsync(IReadOnlyDictionary<string, string>? settings = null) =>
        await ApiHost.StartAsync(await CreateDatabaseAsync(), WithMail(settings));

    /// <summary>
    /// Creates an empty database owned by the application role in the shared container, for a host to migrate.
    /// </summary>
    /// <returns>The Npgsql connection string of the new database, as the application role.</returns>
    private async Task<string> CreateDatabaseAsync()
    {
        var database = $"t{Guid.NewGuid():N}";
        await using (var admin = new NpgsqlConnection(container.GetConnectionString()))
        {
            await admin.OpenAsync(TestContext.Current.CancellationToken);
            await using var create = new NpgsqlCommand($"CREATE DATABASE {database} OWNER {AppRole}", admin);
            await create.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }

        var connectionString = new NpgsqlConnectionStringBuilder(container.GetConnectionString())
        {
            Database = database,
            Username = AppRole,
            Password = AppPassword,
        }.ConnectionString;
        return $"{connectionString};{GssSetting}";
    }

    /// <summary>
    /// The connection string of the same database as the container's superuser, for checks that the application role is not allowed to make.
    /// </summary>
    /// <param name="host">The host whose database is reached.</param>
    /// <returns>The Npgsql connection string of the host's database, as the superuser.</returns>
    public string AdminConnectionString(ApiHost host) =>
        new NpgsqlConnectionStringBuilder(container.GetConnectionString())
        {
            Database = new NpgsqlConnectionStringBuilder(host.ConnectionString).Database,
        }.ConnectionString;

    /// <summary>
    /// Drops the database of a running host, so that the host stays up while its database is gone.
    /// </summary>
    /// <param name="host">The host whose database is dropped.</param>
    public async Task DropDatabaseAsync(ApiHost host)
    {
        var database = new NpgsqlConnectionStringBuilder(host.ConnectionString).Database;
        await using var admin = new NpgsqlConnection(container.GetConnectionString());
        await admin.OpenAsync(TestContext.Current.CancellationToken);
        await using var drop = new NpgsqlCommand($"DROP DATABASE {database} WITH (FORCE)", admin);
        await drop.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask InitializeAsync() => await Task.WhenAll(StartContainerAsync(), Mail.StartAsync());

    private async Task StartContainerAsync()
    {
        await container.StartAsync();
        await using var admin = new NpgsqlConnection(container.GetConnectionString());
        await admin.OpenAsync();
        await using var create = new NpgsqlCommand($"CREATE ROLE {AppRole} LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE PASSWORD '{AppPassword}'", admin);
        await create.ExecuteNonQueryAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (shared.IsValueCreated && shared.Value.IsCompletedSuccessfully)
        {
            await shared.Value.Result.DisposeAsync();
        }

        await container.DisposeAsync();
        await Mail.DisposeAsync();
    }
}
