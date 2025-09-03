using Microsoft.EntityFrameworkCore;
using api.Models.Entities;
using api.Data;
using api.Data.Repositories.Implementations;
using Microsoft.Data.Sqlite;

namespace Api.Tests.Repositories
{

    public class TaskRepositoryTests
    {
        [Fact]
        public async Task GetTasksForUserAsync_ReturnsTasks_FilteredByUserId()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var tasks = new []
            {
                new TaskItem { Id = Guid.NewGuid(), Title = "A", CreatedByUserId = userId, AssignedToUserId = otherUserId, CreatedAt = DateTime.UtcNow },
                new TaskItem { Id = Guid.NewGuid(), Title = "B", CreatedByUserId = otherUserId, AssignedToUserId = userId, CreatedAt = DateTime.UtcNow },
                new TaskItem { Id = Guid.NewGuid(), Title = "C", CreatedByUserId = otherUserId, AssignedToUserId = otherUserId, CreatedAt = DateTime.UtcNow }
            };
            using var context = CreateSqliteContextWithTasks(tasks);
            var repo = new TaskRepository(context);

            // Act
            var result = await repo.GetTasksForUserAsync(userId);

            // Assert
            Assert.Equal(2, result.Items.Count());
            Assert.All(result.Items, t => Assert.True(t.CreatedByUserId == userId || t.AssignedToUserId == userId));
        }

        [Fact]
        public async Task GetTasksForUserAsync_FiltersByStatusAndSearch()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tasks = new[]
            {
        new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "Alpha",
            Description = "Test",
            CreatedByUserId = userId,
            ItemStatus = TaskItemStatus.Pending,
            CreatedAt = DateTime.UtcNow
        },
        new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "Beta",
            Description = "Other",
            CreatedByUserId = userId,
            ItemStatus = TaskItemStatus.Done,
            CreatedAt = DateTime.UtcNow
        }
    };

            using var context = CreateSqliteContextWithTasks(tasks);
            var repo = new TaskRepository(context);

            // Act
            var result = await repo.GetTasksForUserAsync(userId, search: "Alpha", status: TaskItemStatus.Pending);

            // Assert
            Assert.Single(result.Items);
            Assert.Equal("Alpha", result.Items.First().Title);
            Assert.Equal(TaskItemStatus.Pending, result.Items.First().ItemStatus);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsTask_WhenExists()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var task = new TaskItem
            {
                Id = taskId,
                Title = "Alpha",
                Description = "Test task",
                CreatedByUserId = userId,
                ItemStatus = TaskItemStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            using var context = CreateSqliteContextWithTasks(new[] { task });
            var repo = new TaskRepository(context);

            // Act
            var result = await repo.GetByIdAsync(taskId, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(taskId, result!.Id);
            Assert.Equal("Alpha", result.Title);
            Assert.Equal(TaskItemStatus.Pending, result.ItemStatus);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
        {
            // Arrange
            var existingTask = new TaskItem
            {
                Id = Guid.NewGuid(),
                Title = "Beta",
                Description = "Other task",
                CreatedByUserId = Guid.NewGuid(),
                ItemStatus = TaskItemStatus.Done,
                CreatedAt = DateTime.UtcNow
            };

            using var context = CreateSqliteContextWithTasks(new[] { existingTask });
            var repo = new TaskRepository(context);

            var missingId = Guid.NewGuid();

            // Act
            var result = await repo.GetByIdAsync(missingId, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetTaskCountForUserAsync_ReturnsTotalCount_WhenNoFilters()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tasks = new[]
            {
            new TaskItem
            {
                Id = Guid.NewGuid(),
                Title = "Task 1",
                CreatedByUserId = userId,
                ItemStatus = TaskItemStatus.Pending,
                DueDate = DateTime.UtcNow.AddDays(1),
                CreatedAt = DateTime.UtcNow
            },
            new TaskItem
            {
                Id = Guid.NewGuid(),
                Title = "Task 2",
                AssignedToUserId = userId,
                ItemStatus = TaskItemStatus.Done,
                DueDate = DateTime.UtcNow.AddDays(-1),
                CreatedAt = DateTime.UtcNow
            }
        };

            using var context = CreateSqliteContextWithTasks(tasks);
            var repo = new TaskRepository(context);

            // Act
            var count = await repo.GetTaskCountForUserAsync(userId, null, null, null, CancellationToken.None);

            // Assert
            Assert.Equal(2, count);
        }

        [Fact]
        public async Task GetTaskCountForUserAsync_ReturnsZero_WhenNoTasksMatchFilters()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tasks = new[]
            {
        new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "Old Task",
            CreatedByUserId = userId,
            ItemStatus = TaskItemStatus.Pending,
            DueDate = DateTime.UtcNow.AddDays(-10),
            CreatedAt = DateTime.UtcNow
        },
        new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "OtherUser Task",
            CreatedByUserId = Guid.NewGuid(),
            ItemStatus = TaskItemStatus.Pending,
            DueDate = DateTime.UtcNow.AddDays(-10),
            CreatedAt = DateTime.UtcNow
        }
    };

            using var context = CreateSqliteContextWithTasks(tasks);
            var repo = new TaskRepository(context);

            var from = DateTime.UtcNow.AddDays(-5);
            var to = DateTime.UtcNow.AddDays(-1);

            // Act
            var count = await repo.GetTaskCountForUserAsync(
                userId,
                status: TaskItemStatus.Pending,
                dueDateFrom: from,
                dueDateTo: to,
                ct: CancellationToken.None);

            // Assert
            Assert.Equal(0, count);
        }

        [Fact]
        public async Task CreateAsync_SavesTask_WhenUserExists()
        {
            // Arrange
            var userId = Guid.NewGuid();

            using var context = CreateSqliteContextWithTasks(Array.Empty<TaskItem>());

            context.Users.Add(new User
            {
                Id = userId,
                ExternalId = Guid.NewGuid(),
                Name = "Test User",
                Email = $"{userId}@example.test",
                CreatedAt = DateTime.UtcNow
            });
            context.SaveChanges();

            var repo = new TaskRepository(context);

            var newTask = new TaskItem
            {
                Id = Guid.NewGuid(),
                Title = "New Task",
                Description = "Created in test",
                CreatedByUserId = userId,
                ItemStatus = TaskItemStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            // Act
            var result = await repo.CreateAsync(newTask, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(newTask.Id, result.Id);

            var saved = await context.Tasks.FindAsync(newTask.Id);
            Assert.NotNull(saved);
            Assert.Equal("New Task", saved!.Title);
            Assert.Equal(userId, saved.CreatedByUserId);
        }

        [Fact]
        public async Task CreateAsync_ThrowsDbUpdateException_WhenUserDoesNotExist()
        {
            // Arrange
            using var context = CreateSqliteContextWithTasks(Array.Empty<TaskItem>());
            var repo = new TaskRepository(context);

            var newTask = new TaskItem
            {
                Id = Guid.NewGuid(),
                Title = "Task Without User",
                Description = "Should fail due to FK",
                CreatedByUserId = Guid.NewGuid(),
                ItemStatus = TaskItemStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            // Act & Assert
            await Assert.ThrowsAsync<DbUpdateException>(() => repo.CreateAsync(newTask, CancellationToken.None));
        }

        [Fact]
        public async Task UpdateAsync_UpdatesTask_WhenTaskExists()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var taskId = Guid.NewGuid();

            var existingTask = new TaskItem
            {
                Id = taskId,
                Title = "Original Title",
                Description = "Original Description",
                CreatedByUserId = userId,
                ItemStatus = TaskItemStatus.Pending,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            };

            using var context = CreateSqliteContextWithTasks(new[] { existingTask });
            var repo = new TaskRepository(context);

            existingTask.Title = "Updated Title";
            existingTask.Description = "Updated Description";
            existingTask.ItemStatus = TaskItemStatus.Done;

            // Act
            var result = await repo.UpdateAsync(existingTask, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Updated Title", result.Title);
            Assert.Equal("Updated Description", result.Description);
            Assert.Equal(TaskItemStatus.Done, result.ItemStatus);

            var updatedInDb = await context.Tasks.FindAsync(taskId);
            Assert.NotNull(updatedInDb);
            Assert.Equal("Updated Title", updatedInDb!.Title);
            Assert.Equal(TaskItemStatus.Done, updatedInDb.ItemStatus);
        }

        [Fact]
        public async Task UpdateAsync_ThrowsDbUpdateConcurrencyException_WhenTaskDoesNotExist()
        {
            // Arrange
            using var context = CreateSqliteContextWithTasks(Array.Empty<TaskItem>());
            var repo = new TaskRepository(context);

            var nonExistentTask = new TaskItem
            {
                Id = Guid.NewGuid(),
                Title = "Non-existent Task",
                Description = "This task was never saved",
                CreatedByUserId = Guid.NewGuid(),
                ItemStatus = TaskItemStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            // Act & Assert
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
                repo.UpdateAsync(nonExistentTask, CancellationToken.None));
        }

        [Fact]
        public async Task DeleteAsync_ReturnsTrue_AndRemovesTask_WhenTaskExists()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var taskId = Guid.NewGuid();

            var existingTask = new TaskItem
            {
                Id = taskId,
                Title = "Task to delete",
                Description = "Will be deleted",
                CreatedByUserId = userId,
                ItemStatus = TaskItemStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            using var context = CreateSqliteContextWithTasks(new[] { existingTask });
            var repo = new TaskRepository(context);

            Assert.Equal(1, context.Tasks.Count());

            // Act
            var result = await repo.DeleteAsync(taskId, CancellationToken.None);

            // Assert
            Assert.True(result);
            Assert.Null(await context.Tasks.FindAsync(taskId));
            Assert.Equal(0, context.Tasks.Count());
        }

        [Fact]
        public async Task DeleteAsync_ReturnsFalse_WhenTaskDoesNotExist()
        {
            // Arrange
            using var context = CreateSqliteContextWithTasks(Array.Empty<TaskItem>());
            var repo = new TaskRepository(context);

            var missingId = Guid.NewGuid();

            // Act
            var result = await repo.DeleteAsync(missingId, CancellationToken.None);

            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// Creates an in-memory SQLite <see cref="DataContext"/>, seeds any referenced <see cref="User"/> rows, and inserts the given tasks.
        /// </summary>
        /// <param name="tasks">Tasks to insert. May mutate tasks to ensure valid CreatedByUserId.</param>
        /// <returns>An open <see cref="DataContext"/> containing the seeded users and tasks. Dispose when finished.</returns>
        private DataContext CreateSqliteContextWithTasks(IEnumerable<TaskItem> tasks)
        {
            var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<DataContext>()
                .UseSqlite(connection)
                .Options;

            var context = new DataContext(options);
            context.Database.EnsureCreated();

            var taskList = tasks.ToList();

            foreach (var t in taskList)
            {
                if (t.CreatedByUserId == Guid.Empty)
                {
                    if (t.AssignedToUserId.HasValue && t.AssignedToUserId.Value != Guid.Empty)
                    {
                        t.CreatedByUserId = t.AssignedToUserId.Value;
                    }
                    else
                    {
                        t.CreatedByUserId = Guid.NewGuid();
                    }
                }
            }

            var userIds = taskList
                .SelectMany(t => new[] { t.CreatedByUserId, t.AssignedToUserId ?? Guid.Empty })
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToList();

            foreach (var id in userIds)
            {
                if (!context.Users.Any(u => u.Id == id))
                {
                    context.Users.Add(new User
                    {
                        Id = id,
                        ExternalId = Guid.NewGuid(),
                        Name = $"TestUser_{id.ToString().Substring(0, 8)}",
                        Email = $"{id}@example.test",
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
            context.SaveChanges();

            context.Tasks.AddRange(taskList);
            context.SaveChanges();

            return context;
        }

    }
}
