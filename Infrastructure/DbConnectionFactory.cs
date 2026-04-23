using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Data;

public class DbConnectionFactory
{
    private readonly string _connectionString;
    private readonly ILogger<DbConnectionFactory> _logger;

    public DbConnectionFactory(IConfiguration config, ILogger<DbConnectionFactory> logger)
    {
        _connectionString = config.GetConnectionString("SupabaseDb")
            ?? throw new Exception("SupabaseDb connection string is missing");
        _logger = logger;
    }

    public IDbConnection Create()
    {
        var connection = new NpgsqlConnection(_connectionString);
        connection.Open();
        return connection;
    }

    public async Task<IDbConnection> CreateAsync()
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        return connection;
    }
}