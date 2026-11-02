using AuPairGo.API.Data;
using AuPairGo.API.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AuPairGo.API.Tests;

public class DatabaseTests
{
    [Fact]
    public async Task SQLite_test_database_can_save_and_read_a_user()
    {
        await using var connection = new SqliteConnection(
            "Data Source=:memory:");

        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new ApplicationDbContext(options);

        await context.Database.EnsureCreatedAsync();

        var user = new User
        {
            Email = "test@example.com",
            Password = "hashed-test-password",
            FirstName = "Test",
            LastName = "User",
            CellNumber = "0000000000",
            Role = "Parent",
            OneSignalPlayerId = string.Empty
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var savedUser = await context.Users
            .SingleAsync(existingUser =>
                existingUser.Email == "test@example.com");

        Assert.Equal("Test", savedUser.FirstName);
        Assert.Equal("Parent", savedUser.Role);
    }
}