using Npgsql;

namespace FlowCore.Data;

public static class PostgresConnectionStringResolver
{
    public static string ResolveFromConfiguration(IConfiguration configuration)
    {
        var configured = configuration.GetConnectionString("FlowCoreDbContext")
            ?? configuration["ConnectionStrings:FlowCoreDbContext"]
            ?? configuration["DATABASE_URL"]
            ?? throw new InvalidOperationException(
                "No database connection was configured. Set ConnectionStrings__FlowCoreDbContext or DATABASE_URL.");

        return Resolve(configured);
    }

    public static string Resolve(string rawConnection)
    {
        if (rawConnection.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
            rawConnection.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return ConvertDatabaseUrlToNpgsql(rawConnection);
        }

        return rawConnection;
    }

    private static string ConvertDatabaseUrlToNpgsql(string databaseUrl)
    {
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':', 2, StringSplitOptions.None);
        var username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : "";
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
        var database = uri.AbsolutePath.TrimStart('/');

        if (string.IsNullOrWhiteSpace(database))
        {
            throw new InvalidOperationException("DATABASE_URL is missing a database name path segment.");
        }

        var csb = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Username = username,
            Password = password,
            Database = database,
            SslMode = SslMode.Require
        };

        ApplyDatabaseUrlQueryOptions(uri.Query, csb);
        return csb.ConnectionString;
    }

    private static void ApplyDatabaseUrlQueryOptions(string queryString, NpgsqlConnectionStringBuilder csb)
    {
        if (string.IsNullOrWhiteSpace(queryString))
            return;

        var query = queryString.TrimStart('?');
        var pairs = query.Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var pair in pairs)
        {
            var parts = pair.Split('=', 2, StringSplitOptions.None);
            var key = Uri.UnescapeDataString(parts[0]).ToLowerInvariant();
            var value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : "";

            switch (key)
            {
                case "sslmode":
                    if (Enum.TryParse<SslMode>(value, true, out var parsedSslMode))
                        csb.SslMode = parsedSslMode;
                    break;
            }
        }
    }
}
