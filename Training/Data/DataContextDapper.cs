using Dapper;
using Microsoft.Data.SqlClient;

namespace Training.Data;

public class DataContextDapper
{
    private readonly IConfiguration _config;
    private string? _connectionString => _config.GetConnectionString("DefaultConnection");
    
    public DataContextDapper(IConfiguration config)
    {
        _config = config;
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? parameters = null)
    {
        using (SqlConnection sqlConnection = new(_connectionString))    // 'using' forces connection to close when logic is done
            return await sqlConnection.QueryAsync<T>(sql, parameters);
    }

    public async Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? parameters = null)
    {
        using (SqlConnection sqlConnection = new(_connectionString))    // 'using' forces connection to close when logic is done
            return await sqlConnection.QuerySingleOrDefaultAsync<T>(sql, parameters);
    }

    public async Task<bool> ExecuteAsyncBool(string sql, object? parameters = null)
    {
        using (SqlConnection sqlConnection = new(_connectionString))    // 'using' forces connection to close when logic is done
            return await sqlConnection.ExecuteAsync(sql, parameters) > 0;
    }

    public async Task<int> ExecuteAsync(string sql, object? parameters = null)
    {
        using (SqlConnection sqlConnection = new(_connectionString))    // 'using' forces connection to close when logic is done
            return await sqlConnection.ExecuteAsync(sql, parameters);
    }

    public SqlConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }
}