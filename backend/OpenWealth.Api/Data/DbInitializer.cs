using Microsoft.EntityFrameworkCore;

namespace OpenWealth.Api.Data;

public static class DbInitializer
{
    /// <summary>
    /// Brings the database up to date with EF Core migrations. Databases created
    /// by earlier versions of the app (via EnsureCreated, which writes no
    /// migration history) are first baselined: the InitialCreate migration is
    /// recorded as already applied so only later migrations run against them.
    /// </summary>
    public static void Initialize(AppDbContext db)
    {
        if (HasLegacySchemaWithoutHistory(db))
        {
            var initialMigration = db.Database.GetMigrations().First();
            db.Database.ExecuteSqlRaw(
                """
                CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                    "ProductVersion" TEXT NOT NULL
                );
                """);
            db.Database.ExecuteSql(
                $"""INSERT OR IGNORE INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES ({initialMigration}, '8.0.0');""");
        }

        db.Database.Migrate();
    }

    private static bool HasLegacySchemaWithoutHistory(AppDbContext db)
    {
        if (!db.Database.CanConnect())
            return false;
        var conn = db.Database.GetDbConnection();
        conn.Open();
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
                "SELECT " +
                "  EXISTS (SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = 'Users'), " +
                "  EXISTS (SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = '__EFMigrationsHistory')";
            using var reader = cmd.ExecuteReader();
            reader.Read();
            return reader.GetInt64(0) == 1 && reader.GetInt64(1) == 0;
        }
        finally
        {
            conn.Close();
        }
    }
}
