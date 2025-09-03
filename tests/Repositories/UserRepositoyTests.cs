using api.Data;
using api.Data.Repositories.Implementations;
using api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Api.Tests.Repositories
{
    public class UserRepositoyTests
    {
        [Fact]
        public async Task QueryAsync_ReturnsUsers_WhenExist()
        {
            // Arrange
            var users = new[]
            {
                new User { Id = Guid.NewGuid(), ExternalId = Guid.NewGuid(), Name = "Alice", Email = "alice@test.com", CreatedAt = DateTime.UtcNow },
                new User { Id = Guid.NewGuid(), ExternalId = Guid.NewGuid(), Name = "Bob",   Email = "bob@test.com",   CreatedAt = DateTime.UtcNow }
            };

            using var context = CreateSqliteContextWithUsers(users);
            var repo = new UserRepository(context, NullLogger<UserRepository>.Instance);

            // Act
            var result = await repo.QueryAsync(search: null, page: 1, pageSize: 10, ct: CancellationToken.None);

            // Assert
            Assert.Equal(2, result.TotalCount);
            Assert.Contains(result.Items, u => u.Name == "Alice");
            Assert.Contains(result.Items, u => u.Name == "Bob");
        }

        [Fact]
        public async Task QueryAsync_ClampsPageSize_ToMaxAndReturnsCorrectCounts()
        {
            // Arrange
            var users = Enumerable.Range(1, 150)
                .Select(i => new User
                {
                    Id = Guid.NewGuid(),
                    ExternalId = Guid.NewGuid(),
                    Name = $"User_{i:D3}",
                    Email = $"user{i}@example.test",
                    CreatedAt = DateTime.UtcNow
                })
                .ToArray();

            using var context = CreateSqliteContextWithUsers(users);
            var repo = new UserRepository(context, NullLogger<UserRepository>.Instance);

            // Act
            var result = await repo.QueryAsync(search: null, page: 1, pageSize: 200, ct: CancellationToken.None);

            // Assert
            Assert.Equal(150, result.TotalCount);
            Assert.Equal(1, result.Page);
            Assert.Equal(100, result.PageSize);
            Assert.Equal(100, result.Items.Count());
            Assert.Equal("User_001", result.Items.First().Name);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsUser_WhenExists()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                ExternalId = Guid.NewGuid(),
                Name = "Test User",
                Email = "test@example.com",
                CreatedAt = DateTime.UtcNow
            };

            using var context = CreateSqliteContextWithUsers(new[] { user });
            var repo = new UserRepository(context, NullLogger<UserRepository>.Instance);

            // Act
            var result = await repo.GetByIdAsync(userId, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(userId, result!.Id);
            Assert.Equal("Test User", result.Name);
            Assert.Equal("test@example.com", result.Email);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsNull_WhenUserDoesNotExist()
        {
            // Arrange
            using var context = CreateSqliteContextWithUsers(Array.Empty<User>());
            var repo = new UserRepository(context, NullLogger<UserRepository>.Instance);

            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await repo.GetByIdAsync(nonExistentId, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByExternalIdAsync_ReturnsUser_WhenExists()
        {
            // Arrange
            var externalId = Guid.NewGuid();
            var user = new User
            {
                Id = Guid.NewGuid(),
                ExternalId = externalId,
                Name = "External User",
                Email = "external@example.test",
                CreatedAt = DateTime.UtcNow
            };

            using var context = CreateSqliteContextWithUsers(new[] { user });
            var repo = new UserRepository(context, NullLogger<UserRepository>.Instance);

            // Act
            var result = await repo.GetByExternalIdAsync(externalId, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(user.Id, result!.Id);
            Assert.Equal(externalId, result.ExternalId);
            Assert.Equal("External User", result.Name);
        }

        [Fact]
        public async Task GetByExternalIdAsync_ReturnsNull_WhenExternalIdIsEmpty()
        {
            // Arrange
            using var context = CreateSqliteContextWithUsers(Array.Empty<User>());
            var repo = new UserRepository(context, NullLogger<UserRepository>.Instance);

            // Act
            var result = await repo.GetByExternalIdAsync(Guid.Empty, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task AddAsync_CreatesNewUser_WhenUserDoesNotExist()
        {
            // Arrange
            using var context = CreateSqliteContextWithUsers(Array.Empty<User>());
            var repo = new UserRepository(context, NullLogger<UserRepository>.Instance);

            var newUser = new User
            {
                Id = Guid.NewGuid(),
                ExternalId = Guid.NewGuid(),
                Name = "New User",
                Email = "newuser@example.test",
                CreatedAt = DateTime.UtcNow
            };

            // Act
            var result = await repo.AddAsync(newUser, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(newUser.Id, result.Id);
            Assert.Equal(newUser.ExternalId, result.ExternalId);
            Assert.Equal("New User", result.Name);

            // Verify user was saved to database
            var savedUser = await context.Users.FindAsync(newUser.Id);
            Assert.NotNull(savedUser);
            Assert.Equal("newuser@example.test", savedUser!.Email);
        }

        [Fact]
        public async Task AddAsync_ReturnsExistingUser_WhenExternalIdAlreadyExists()
        {
            // Arrange
            var existingUser = new User
            {
                Id = Guid.NewGuid(),
                ExternalId = Guid.NewGuid(),
                Name = "Existing User",
                Email = "existing@example.test",
                CreatedAt = DateTime.UtcNow
            };

            using var context = CreateSqliteContextWithUsers(new[] { existingUser });
            var repo = new UserRepository(context, NullLogger<UserRepository>.Instance);

            var duplicateUser = new User
            {
                Id = Guid.NewGuid(),
                ExternalId = existingUser.ExternalId,
                Name = "Duplicate User",
                Email = "duplicate@example.test",
                CreatedAt = DateTime.UtcNow
            };

            // Act
            var result = await repo.AddAsync(duplicateUser, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(existingUser.Id, result.Id);
            Assert.Equal(existingUser.ExternalId, result.ExternalId);
            Assert.Equal("Existing User", result.Name);
            Assert.Equal("existing@example.test", result.Email);

            Assert.Equal(1, context.Users.Count());
        }

        [Fact]
        public async Task UpdateAsync_UpdatesUser_WhenUserExists()
        {
            // Arrange
            var existingUser = new User
            {
                Id = Guid.NewGuid(),
                ExternalId = Guid.NewGuid(),
                Name = "Original Name",
                Email = "original@example.test",
                CreatedAt = DateTime.UtcNow
            };

            using var context = CreateSqliteContextWithUsers(new[] { existingUser });
            var repo = new UserRepository(context, NullLogger<UserRepository>.Instance);

            existingUser.Name = "Updated Name";
            existingUser.Email = "updated@example.test";

            // Act
            var result = await repo.UpdateAsync(existingUser, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(existingUser.Id, result.Id);
            Assert.Equal("Updated Name", result.Name);
            Assert.Equal("updated@example.test", result.Email);

            var updatedUser = await context.Users.FindAsync(existingUser.Id);
            Assert.NotNull(updatedUser);
            Assert.Equal("Updated Name", updatedUser!.Name);
            Assert.Equal("updated@example.test", updatedUser.Email);
        }

        [Fact]
        public async Task UpdateAsync_ThrowsDbUpdateConcurrencyException_WhenUserDoesNotExist()
        {
            // Arrange
            using var context = CreateSqliteContextWithUsers(Array.Empty<User>());
            var repo = new UserRepository(context, NullLogger<UserRepository>.Instance);

            var nonExistentUser = new User
            {
                Id = Guid.NewGuid(),
                ExternalId = Guid.NewGuid(),
                Name = "Non-existent User",
                Email = "nonexistent@example.test",
                CreatedAt = DateTime.UtcNow
            };

            // Act & Assert
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
                repo.UpdateAsync(nonExistentUser, CancellationToken.None));
        }

        [Fact]
        public async Task DeleteAsync_ReturnsTrue_AndRemovesUser_WhenUserExists()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                ExternalId = Guid.NewGuid(),
                Name = "User To Delete",
                Email = "delete@example.test",
                CreatedAt = DateTime.UtcNow
            };

            using var context = CreateSqliteContextWithUsers(new[] { user });
            var repo = new UserRepository(context, NullLogger<UserRepository>.Instance);

            // Act
            var result = await repo.DeleteAsync(userId, CancellationToken.None);

            // Assert
            Assert.True(result);
            Assert.Null(await context.Users.FindAsync(userId));
            Assert.Equal(0, context.Users.Count());
        }

        [Fact]
        public async Task DeleteAsync_ReturnsFalse_WhenUserDoesNotExist()
        {
            // Arrange
            using var context = CreateSqliteContextWithUsers(Array.Empty<User>());
            var repo = new UserRepository(context, NullLogger<UserRepository>.Instance);
            var missingId = Guid.NewGuid();

            // Act
            var result = await repo.DeleteAsync(missingId, CancellationToken.None);

            // Assert
            Assert.False(result);
        }



        /// <summary>
        /// Creates an in-memory SQLite <see cref="DataContext"/> and inserts the given users.
        /// </summary>
        /// <param name="users">Users to insert.</param>
        /// <returns>An open <see cref="DataContext"/> containing the seeded users. Dispose when finished.</returns>
        private DataContext CreateSqliteContextWithUsers(IEnumerable<User> users)
        {
            var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<DataContext>()
                .UseSqlite(connection)
                .Options;

            var context = new DataContext(options);
            context.Database.EnsureCreated();

            context.Users.AddRange(users);
            context.SaveChanges();

            return context;
        }
    }
}
