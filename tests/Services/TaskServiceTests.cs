using api.Models.Dtos;
using api.Models.Entities;
using api.Services.Implementations;
using api.Data.Repositories.Interfaces;
using api.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using api.Models;

namespace Api.Tests.Services;

public class TaskServiceTests
{
    private readonly Mock<ITaskRepository> _taskRepoMock = new();
    private readonly Mock<IUserService> _userServiceMock = new();
    private readonly Mock<ILogger<TaskService>> _loggerMock = new();

    private TaskService CreateService() =>
        new TaskService(_taskRepoMock.Object, _userServiceMock.Object, _loggerMock.Object);

    [Fact]
    public async Task GetByIdAsync_ReturnsTaskDetailDto_WhenUserIsOwner()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var externalId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        var user = new UserDto
        {
            Id = userId,
            Name = "Ricardo",
            Email = "ricardo@test.com"
        };
        var task = new TaskItem { Id = taskId, CreatedByUserId = userId };


        _userServiceMock
        .Setup(s => s.ProvisionCurrentUserAsync(
            externalId,
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(user);


        _taskRepoMock.Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        var service = CreateService();

        // Act
        var result = await service.GetByIdAsync(externalId, taskId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(taskId, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ThrowsUnauthorizedAccessException_WhenUserIsNotOwnerOrAssignee()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var externalId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        var user = new UserDto
        {
            Id = Guid.NewGuid(), // Not the owner
            Name = "Ana",
            Email = "ana@test.com"
        };
        var task = new TaskItem { Id = taskId, CreatedByUserId = ownerId };

        _userServiceMock
            .Setup(s => s.ProvisionCurrentUserAsync(externalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _taskRepoMock
            .Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.GetByIdAsync(externalId, taskId));
    }

    [Fact]
    public async Task GetTasksAsync_ReturnsAssignedTasks_ForAssignedScope()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var externalId = Guid.NewGuid();

        var user = new UserDto { Id = userId, Name = "Ricardo", Email = "ricardo@test.com" };
        var assignedTask = new TaskItem { Id = Guid.NewGuid(), AssignedToUserId = userId, CreatedByUserId = Guid.NewGuid() };
        var otherTask = new TaskItem { Id = Guid.NewGuid(), AssignedToUserId = Guid.NewGuid(), CreatedByUserId = userId };

        var paged = new PagedResult<TaskItem> { Items = new[] { assignedTask, otherTask }, TotalCount = 2, Page = 1, PageSize = 20 };

        _userServiceMock
            .Setup(s => s.ProvisionCurrentUserAsync(externalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _taskRepoMock
            .Setup(r => r.GetTasksForUserAsync(userId, null, null, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paged);

        _taskRepoMock
            .Setup(r => r.GetTaskCountForUserAsync(userId, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var service = CreateService();

        // Act
        var result = await service.GetTasksAsync(externalId, scope: "assigned");

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(assignedTask.Id, result.Items.ToList()[0].Id);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetTasksAsync_ReturnsCreatedTasks_ForCreatedScope()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var externalId = Guid.NewGuid();

        var user = new UserDto { Id = userId, Name = "Ricardo", Email = "ricardo@test.com" };
        var createdTask = new TaskItem { Id = Guid.NewGuid(), CreatedByUserId = userId, AssignedToUserId = Guid.NewGuid() };
        var otherTask = new TaskItem { Id = Guid.NewGuid(), CreatedByUserId = Guid.NewGuid(), AssignedToUserId = userId };

        var paged = new PagedResult<TaskItem> { Items = new[] { createdTask, otherTask }, TotalCount = 2, Page = 1, PageSize = 20 };
        var allPaged = new PagedResult<TaskItem> { Items = new[] { createdTask }, TotalCount = 1, Page = 1, PageSize = 1 };

        _userServiceMock
            .Setup(s => s.ProvisionCurrentUserAsync(externalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _taskRepoMock
            .Setup(r => r.GetTasksForUserAsync(userId, null, null, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paged);

        _taskRepoMock
            .Setup(r => r.GetTasksForUserAsync(userId, null, null, null, null, 1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allPaged);

        _taskRepoMock
            .Setup(r => r.GetTaskCountForUserAsync(userId, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var service = CreateService();

        // Act
        var result = await service.GetTasksAsync(externalId, scope: "created");

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(createdTask.Id, result.Items.ToList()[0].Id);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task CreateAsync_CreatesTask_WhenAssignedUserExists()
    {
        // Arrange
        var externalId = Guid.NewGuid();
        var assignedExternalId = Guid.NewGuid();
        var currentUser = new UserDto { Id = Guid.NewGuid(), Name = "Ricardo", Email = "ricardo@test.com" };
        var assignedUser = new UserSummaryDto { Id = Guid.NewGuid(), Name = "Ana" };

        var dto = new CreateTaskDto
        {
            Title = "Test Task",
            Description = "Test Description",
            DueDate = DateTime.UtcNow.AddDays(1),
            AssignedToExternalId = assignedExternalId
        };

        var createdTask = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = dto.Title,
            Description = dto.Description,
            DueDate = dto.DueDate,
            ItemStatus = TaskItemStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = currentUser.Id,
            AssignedToUserId = assignedUser.Id
        };

        _userServiceMock
            .Setup(s => s.ProvisionCurrentUserAsync(externalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentUser);

        _userServiceMock
            .Setup(s => s.GetUserSummaryByExternalIdAsync(assignedExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignedUser);

        _taskRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdTask);

        var service = CreateService();

        // Act
        var result = await service.CreateAsync(externalId, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(createdTask.Id, result.Id);
        Assert.Equal(assignedUser.Id, createdTask.AssignedToUserId);
    }

    [Fact]
    public async Task CreateAsync_ThrowsInvalidOperationException_WhenAssignedUserNotFound()
    {
        // Arrange
        var externalId = Guid.NewGuid();
        var assignedExternalId = Guid.NewGuid();
        var currentUser = new UserDto { Id = Guid.NewGuid(), Name = "Ricardo", Email = "ricardo@test.com" };

        var dto = new CreateTaskDto
        {
            Title = "Test Task",
            Description = "Test Description",
            DueDate = DateTime.UtcNow.AddDays(1),
            AssignedToExternalId = assignedExternalId
        };

        _userServiceMock
            .Setup(s => s.ProvisionCurrentUserAsync(externalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentUser);

        _userServiceMock
            .Setup(s => s.GetUserSummaryByExternalIdAsync(assignedExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSummaryDto?)null);

        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(externalId, dto));
    }

    [Fact]
    public async Task UpdateAsync_UpdatesTask_WhenUserIsOwnerAndAssignedUserExists()
    {
        // Arrange
        var externalId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var assignedExternalId = Guid.NewGuid();
        var currentUser = new UserDto { Id = Guid.NewGuid(), Name = "Ricardo", Email = "ricardo@test.com" };
        var assignedUser = new UserSummaryDto { Id = Guid.NewGuid(), Name = "Ana" };

        var task = new TaskItem
        {
            Id = taskId,
            Title = "Old Title",
            Description = "Old Description",
            DueDate = DateTime.UtcNow.AddDays(1),
            ItemStatus = TaskItemStatus.Pending,
            CreatedByUserId = currentUser.Id
        };

        var dto = new UpdateTaskDto
        {
            Title = "New Title",
            Description = "New Description",
            DueDate = DateTime.UtcNow.AddDays(2),
            ItemStatus = TaskItemStatus.Done,
            AssignedToExternalId = assignedExternalId
        };

        var updatedTask = new TaskItem
        {
            Id = taskId,
            Title = dto.Title,
            Description = dto.Description,
            DueDate = dto.DueDate,
            ItemStatus = dto.ItemStatus,
            CreatedByUserId = currentUser.Id,
            AssignedToUserId = assignedUser.Id
        };

        _userServiceMock
            .Setup(s => s.ProvisionCurrentUserAsync(externalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentUser);

        _taskRepoMock
            .Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        _userServiceMock
            .Setup(s => s.GetUserSummaryByExternalIdAsync(assignedExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignedUser);

        _taskRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedTask);

        var service = CreateService();

        // Act
        var result = await service.UpdateAsync(externalId, taskId, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.Title, result.Title);
        Assert.Equal(dto.Description, result.Description);
        Assert.Equal(dto.DueDate, result.DueDate);
        Assert.Equal(dto.ItemStatus, result.ItemStatus);
    }

    [Fact]
    public async Task UpdateAsync_ThrowsInvalidOperationException_WhenAssignedUserNotFound()
    {
        // Arrange
        var externalId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var assignedExternalId = Guid.NewGuid();
        var currentUser = new UserDto { Id = Guid.NewGuid(), Name = "Ricardo", Email = "ricardo@test.com" };

        var task = new TaskItem
        {
            Id = taskId,
            Title = "Old Title",
            Description = "Old Description",
            DueDate = DateTime.UtcNow.AddDays(1),
            ItemStatus = TaskItemStatus.Pending,
            CreatedByUserId = currentUser.Id
        };

        var dto = new UpdateTaskDto
        {
            Title = "New Title",
            Description = "New Description",
            DueDate = DateTime.UtcNow.AddDays(2),
            ItemStatus = TaskItemStatus.Done,
            AssignedToExternalId = assignedExternalId
        };

        _userServiceMock
            .Setup(s => s.ProvisionCurrentUserAsync(externalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentUser);

        _taskRepoMock
            .Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        _userServiceMock
            .Setup(s => s.GetUserSummaryByExternalIdAsync(assignedExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSummaryDto?)null);

        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateAsync(externalId, taskId, dto));
    }

    [Fact]
    public async Task UpdateStatusAsync_UpdatesStatus_WhenUserIsOwnerOrAssignee()
    {
        // Arrange
        var externalId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var currentUser = new UserDto { Id = Guid.NewGuid(), Name = "Ricardo", Email = "ricardo@test.com" };
        var task = new TaskItem
        {
            Id = taskId,
            ItemStatus = TaskItemStatus.Pending,
            CreatedByUserId = currentUser.Id
        };
        var updatedTask = new TaskItem
        {
            Id = taskId,
            ItemStatus = TaskItemStatus.Done,
            CreatedByUserId = currentUser.Id
        };

        _userServiceMock
            .Setup(s => s.ProvisionCurrentUserAsync(externalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentUser);

        _taskRepoMock
            .Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        _taskRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedTask);

        var service = CreateService();

        // Act
        var result = await service.UpdateStatusAsync(externalId, taskId, TaskItemStatus.Done);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TaskItemStatus.Done, result.ItemStatus);
    }

    [Fact]
    public async Task UpdateStatusAsync_ThrowsUnauthorizedAccessException_WhenUserIsNotOwnerOrAssignee()
    {
        // Arrange
        var externalId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var currentUser = new UserDto { Id = Guid.NewGuid(), Name = "Ana", Email = "ana@test.com" };
        var task = new TaskItem
        {
            Id = taskId,
            ItemStatus = TaskItemStatus.Pending,
            CreatedByUserId = ownerId
        };

        _userServiceMock
            .Setup(s => s.ProvisionCurrentUserAsync(externalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentUser);

        _taskRepoMock
            .Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.UpdateStatusAsync(externalId, taskId, TaskItemStatus.Done));
    }

    [Fact]
    public async Task DeleteAsync_DeletesTask_WhenUserIsOwnerOrAssignee()
    {
        // Arrange
        var externalId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var currentUser = new UserDto { Id = Guid.NewGuid(), Name = "Ricardo", Email = "ricardo@test.com" };
        var task = new TaskItem
        {
            Id = taskId,
            CreatedByUserId = currentUser.Id
        };

        _userServiceMock
            .Setup(s => s.ProvisionCurrentUserAsync(externalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentUser);

        _taskRepoMock
            .Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        _taskRepoMock
            .Setup(r => r.DeleteAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService();

        // Act
        var result = await service.DeleteAsync(externalId, taskId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task DeleteAsync_ThrowsUnauthorizedAccessException_WhenUserIsNotOwnerOrAssignee()
    {
        // Arrange
        var externalId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var currentUser = new UserDto { Id = Guid.NewGuid(), Name = "Ana", Email = "ana@test.com" };
        var task = new TaskItem
        {
            Id = taskId,
            CreatedByUserId = ownerId
        };

        _userServiceMock
            .Setup(s => s.ProvisionCurrentUserAsync(externalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentUser);

        _taskRepoMock
            .Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.DeleteAsync(externalId, taskId));
    }

}


