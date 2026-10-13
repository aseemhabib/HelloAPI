using Npgsql;

public sealed class UserRepository : IUserRepository
{
    private readonly string _connectionString;

    public UserRepository(IConfiguration configuration)
    {
        // _connectionString =
        //     configuration.GetConnectionString("Postgres")
        //     ?? throw new InvalidOperationException(
        //         "Postgres connection string missing.");


        var dbHost = configuration.GetValue<string>("DB_HOST") ?? throw new InvalidOperationException(
            "DB_HOST configuration value missing.");
        var dbPort = configuration.GetValue<string>("DB_PORT") ?? throw new InvalidOperationException(
            "DB_PORT configuration value missing.");
        var dbName = configuration.GetValue<string>("DB_NAME") ?? throw new InvalidOperationException(
            "DB_NAME configuration value missing.");
        var dbUsername = configuration.GetValue<string>("DB_USERNAME") ?? throw new InvalidOperationException(
            "DB_USERNAME configuration value missing.");
        var dbPassword = configuration.GetValue<string>("DB_PASSWORD") ?? throw new InvalidOperationException(
            "DB_PASSWORD configuration value missing.");

        _connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUsername};Password={dbPassword}";
    }

    public async Task<User> CreateAsync(
    User user,
    CancellationToken cancellationToken)
    {
        await using var connection =
            new NpgsqlConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        await using var transaction =
            await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            const string insertUserSql = """
                INSERT INTO users
                    (id, first_name, last_name, email, password_hash)
                VALUES
                    (@id, @firstName, @lastName, @email, @passwordHash);
                """;

            await using (var userCommand =
                new NpgsqlCommand(
                    insertUserSql,
                    connection,
                    transaction))
            {
                userCommand.Parameters.AddWithValue(
                    "id",
                    user.Id);

                userCommand.Parameters.AddWithValue(
                    "firstName",
                    user.FirstName);

                userCommand.Parameters.AddWithValue(
                    "lastName",
                    user.LastName);

                userCommand.Parameters.AddWithValue(
                    "email",
                    user.Email);

                userCommand.Parameters.AddWithValue(
                    "passwordHash",
                    user.PasswordHash);

                await userCommand.ExecuteNonQueryAsync(
                    cancellationToken);
            }

            const string getRoleSql = """
                SELECT id
                FROM roles
                WHERE name = @roleName;
                """;

            Guid roleId;

            await using (var roleCommand =
                new NpgsqlCommand(
                    getRoleSql,
                    connection,
                    transaction))
            {
                roleCommand.Parameters.AddWithValue(
                    "roleName",
                    "User");

                var result =
                    await roleCommand.ExecuteScalarAsync(
                        cancellationToken);

                if (result is null)
                {
                    throw new InvalidOperationException(
                        "Default role 'User' was not found.");
                }

                roleId = (Guid)result;
            }

            const string insertUserRoleSql = """
                INSERT INTO user_roles
                    (user_id, role_id)
                VALUES
                    (@userId, @roleId);
                """;

            await using (var userRoleCommand =
                new NpgsqlCommand(
                    insertUserRoleSql,
                    connection,
                    transaction))
            {
                userRoleCommand.Parameters.AddWithValue(
                    "userId",
                    user.Id);

                userRoleCommand.Parameters.AddWithValue(
                    "roleId",
                    roleId);

                await userRoleCommand.ExecuteNonQueryAsync(
                    cancellationToken);
            }

            await transaction.CommitAsync(
                cancellationToken);

            user.Roles.Add("User");

            return user;
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, first_name, last_name, email, password_hash, created_utc
            FROM users
            WHERE email = @email;
            """;

        await using var connection =
            new NpgsqlConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);


        // get user
        await using var command =
            new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("email", email);

        var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        // get user roles
        var userRoles = await GetUserRolesAsync(reader.GetGuid(reader.GetOrdinal("id")), cancellationToken);

        return new User
        {
            Id = reader.GetGuid(reader.GetOrdinal("id")),
            FirstName = reader.GetString(reader.GetOrdinal("first_name")),
            LastName = reader.GetString(reader.GetOrdinal("last_name")),
            Email = reader.GetString(reader.GetOrdinal("email")),
            PasswordHash = reader.GetString(reader.GetOrdinal("password_hash")),
            CreatedUtc = reader.GetDateTime(reader.GetOrdinal("created_utc")),
            Roles = userRoles
        };
    }

    public async Task<DashboardSummary> GetDashboardSummaryAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                (SELECT COUNT(*) FROM users) AS total_users,
                (SELECT COUNT(*) FROM users WHERE created_utc >= NOW() - INTERVAL '7 days') AS users_last_seven_days,
                (SELECT COUNT(*) FROM user_roles ur JOIN roles r ON ur.role_id = r.id WHERE r.name = 'Admin') AS admin_users,
                (SELECT COUNT(*) FROM user_roles ur JOIN roles r ON ur.role_id = r.id WHERE r.name = 'User') AS normal_users;
            """;

        await using var connection =
            new NpgsqlConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        await using var command =
            new NpgsqlCommand(sql, connection);

        var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("Failed to retrieve dashboard summary.");
        }

        return new DashboardSummary(
            TotalUsers: reader.GetInt32(reader.GetOrdinal("total_users")),
            UsersLastSevenDays: reader.GetInt32(reader.GetOrdinal("users_last_seven_days")),
            AdminUsers: reader.GetInt32(reader.GetOrdinal("admin_users")),
            NormalUsers: reader.GetInt32(reader.GetOrdinal("normal_users")));
    }

    private async Task<List<string>> GetUserRolesAsync(Guid guid, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT r.name
            FROM roles r
            JOIN user_roles ur
                ON ur.role_id = r.id
            WHERE ur.user_id = @userId;
            """;

        await using var connection =
            new NpgsqlConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);


        // get user
        await using var command =
            new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("userId", guid);

        var reader = await command.ExecuteReaderAsync(cancellationToken);
        var roles = new List<string>();

        if (!await reader.ReadAsync(cancellationToken))
        {
            return roles;
        }

        do
        {
            roles.Add(reader.GetString(reader.GetOrdinal("name")));
        } while (await reader.ReadAsync(cancellationToken));
        
        return roles;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, first_name, last_name, email, password_hash, created_utc
            FROM users
            WHERE id = @id;
            """;

        await using var connection =
            new NpgsqlConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        await using var command =
            new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("id", id);

        var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new User
        {
            Id = reader.GetGuid(reader.GetOrdinal("id")),
            FirstName = reader.GetString(reader.GetOrdinal("first_name")),
            LastName = reader.GetString(reader.GetOrdinal("last_name")),
            Email = reader.GetString(reader.GetOrdinal("email")),
            PasswordHash = reader.GetString(reader.GetOrdinal("password_hash")),
            CreatedUtc = reader.GetDateTime(reader.GetOrdinal("created_utc"))
        };
    }
}
    