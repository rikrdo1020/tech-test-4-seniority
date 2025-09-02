using api.Models.Dtos;
using api.Models.Entities;
using api.Services.Implementations;
using api.Data.Repositories.Interfaces;
using api.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace Api.Tests;

public class TaskServices
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

}
