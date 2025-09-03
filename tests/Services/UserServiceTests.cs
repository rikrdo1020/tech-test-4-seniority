using api.Data.Repositories.Interfaces;
using api.Models;
using api.Models.Dtos;
using api.Models.Entities;
using api.Services;
using api.Services.Implementations;
using api.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Api.Tests.Services
{
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _userReporMock = new();
        private readonly Mock<IGraphService> _graphServiceMock = new();
        //private readonly Mock<ILogger<UserService>> _loggerMock = new();

        private UserService CreateService() =>
            new UserService(_userReporMock.Object, _graphServiceMock.Object);

        [Fact]
        public async Task ProvisionCurrentUserAsync_ReturnsExistingUser_WhenUserExists()
        {
            // Arrange
            var externalId = Guid.NewGuid();
            var existingUser = new User
            {
                Id = Guid.NewGuid(),
                ExternalId = externalId,
                Name = "Ricardo",
                Email = "ricardo@test.com",
                CreatedAt = DateTime.UtcNow
            };

            _userReporMock
                .Setup(r => r.GetByExternalIdAsync(externalId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingUser);

            var service = CreateService();

            // Act
            var result = await service.ProvisionCurrentUserAsync(externalId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(existingUser.Id, result.Id);
            Assert.Equal(existingUser.Name, result.Name);
            Assert.Equal(existingUser.Email, result.Email);
        }

        [Fact]
        public async Task ProvisionCurrentUserAsync_CreatesUser_WhenUserDoesNotExist()
        {
            // Arrange
            var externalId = Guid.NewGuid();
            var graphUser = new UserDto
            {
                Name = "Ana",
                Email = "ana@test.com"
            };
            var newUser = new User
            {
                Id = Guid.NewGuid(),
                ExternalId = externalId,
                Name = graphUser.Name,
                Email = graphUser.Email,
                CreatedAt = DateTime.UtcNow
            };

            _userReporMock
                .Setup(r => r.GetByExternalIdAsync(externalId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            _graphServiceMock
                .Setup(g => g.GetUserFromGraphAsync(externalId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(graphUser);

            _userReporMock
                .Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(newUser);

            var service = CreateService();

            // Act
            var result = await service.ProvisionCurrentUserAsync(externalId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(newUser.Name, result.Name);
            Assert.Equal(newUser.Email, result.Email);
            Assert.Equal(newUser.ExternalId, result.ExternalId);
        }

        [Fact]
        public async Task GetCurrentUserAsync_ReturnsExistingUser_WhenUserExists()
        {
            // Arrange
            var externalId = Guid.NewGuid();
            var existingUser = new User
            {
                Id = Guid.NewGuid(),
                ExternalId = externalId,
                Name = "Ricardo",
                Email = "ricardo@test.com",
                CreatedAt = DateTime.UtcNow
            };

            _userReporMock
                .Setup(r => r.GetByExternalIdAsync(externalId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingUser);

            var service = CreateService();

            // Act
            var result = await service.GetCurrentUserAsync(externalId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(existingUser.Id, result.Id);
            Assert.Equal(existingUser.Name, result.Name);
            Assert.Equal(existingUser.Email, result.Email);
        }

        [Fact]
        public async Task GetCurrentUserAsync_ProvisionsUser_WhenUserDoesNotExist()
        {
            // Arrange
            var externalId = Guid.NewGuid();
            var graphUser = new UserDto
            {
                Name = "Ana",
                Email = "ana@test.com"
            };
            var createdUser = new User
            {
                Id = Guid.NewGuid(),
                ExternalId = externalId,
                Name = graphUser.Name,
                Email = graphUser.Email,
                CreatedAt = DateTime.UtcNow
            };

            _userReporMock
                .Setup(r => r.GetByExternalIdAsync(externalId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            _graphServiceMock
                .Setup(g => g.GetUserFromGraphAsync(externalId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(graphUser);

            _userReporMock
                .Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(createdUser);

            var service = CreateService();

            // Act
            var result = await service.GetCurrentUserAsync(externalId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(createdUser.Id, result.Id);
            Assert.Equal(createdUser.Name, result.Name);
            Assert.Equal(createdUser.Email, result.Email);
        }

        [Fact]
        public async Task GetUserSummaryByExternalIdAsync_ReturnsSummary_WhenUserExists()
        {
            // Arrange
            var externalId = Guid.NewGuid();
            var user = new User
            {
                Id = Guid.NewGuid(),
                ExternalId = externalId,
                Name = "Ricardo",
                Email = "ricardo@test.com",
                CreatedAt = DateTime.UtcNow
            };

            _userReporMock
                .Setup(r => r.GetByExternalIdAsync(externalId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var service = CreateService();

            // Act
            var result = await service.GetUserSummaryByExternalIdAsync(externalId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(user.Id, result.Id);
            Assert.Equal(user.Name, result.Name);
        }

        [Fact]
        public async Task GetUserSummaryByExternalIdAsync_ReturnsNull_WhenExternalIdIsEmpty()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = await service.GetUserSummaryByExternalIdAsync(Guid.Empty);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task SearchUsersAsync_ReturnsPagedResult_WithMatchingUsers()
        {
            // Arrange
            var search = "Ricardo";
            var page = 1;
            var pageSize = 2;
            var users = new[]
            {
                new User { Id = Guid.NewGuid(), Name = "Ricardo", Email = "ricardo@test.com" },
                new User { Id = Guid.NewGuid(), Name = "Ricardo Gomez", Email = "rgomez@test.com" }
            };
            var paged = new PagedResult<User>
            {
                Items = users,
                TotalCount = 2,
                Page = page,
                PageSize = pageSize
            };

            _userReporMock
                .Setup(r => r.QueryAsync(search, page, pageSize, It.IsAny<CancellationToken>()))
                .ReturnsAsync(paged);

            var service = CreateService();

            // Act
            var result = await service.SearchUsersAsync(search, page, pageSize);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.All(result.Items, i => Assert.Contains("Ricardo", i.Name));
        }

        [Fact]
        public async Task SearchUsersAsync_ReturnsEmptyResult_WhenNoUsersFound()
        {
            // Arrange
            var search = "NoMatch";
            var page = 1;
            var pageSize = 2;
            var paged = new PagedResult<User>
            {
                Items = Array.Empty<User>(),
                TotalCount = 0,
                Page = page,
                PageSize = pageSize
            };

            _userReporMock
                .Setup(r => r.QueryAsync(search, page, pageSize, It.IsAny<CancellationToken>()))
                .ReturnsAsync(paged);

            var service = CreateService();

            // Act
            var result = await service.SearchUsersAsync(search, page, pageSize);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Items);
            Assert.Equal(0, result.TotalCount);
        }

        [Fact]
        public async Task UpdateCurrentUserAsync_UpdatesNameAndEmail_WhenValid()
        {
            // Arrange
            var externalId = Guid.NewGuid();
            var user = new User
            {
                Id = Guid.NewGuid(),
                ExternalId = externalId,
                Name = "Old Name",
                Email = "old@email.com",
                CreatedAt = DateTime.UtcNow
            };
            var updatedUser = new User
            {
                Id = user.Id,
                ExternalId = externalId,
                Name = "New Name",
                Email = "new@email.com",
                CreatedAt = user.CreatedAt
            };

            _userReporMock
                .Setup(r => r.GetByExternalIdAsync(externalId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _userReporMock
                .Setup(r => r.UpdateAsync(It.Is<User>(u => u.Name == "New Name" && u.Email == "new@email.com"), It.IsAny<CancellationToken>()))
                .ReturnsAsync(updatedUser);

            var service = CreateService();

            // Act
            var result = await service.UpdateCurrentUserAsync(externalId, "New Name", "new@email.com");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("New Name", result.Name);
            Assert.Equal("new@email.com", result.Email);
        }

        [Fact]
        public async Task UpdateCurrentUserAsync_ThrowsKeyNotFoundException_WhenUserDoesNotExist()
        {
            // Arrange
            var externalId = Guid.NewGuid();

            _userReporMock
                .Setup(r => r.GetByExternalIdAsync(externalId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            var service = CreateService();

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.UpdateCurrentUserAsync(externalId, "Any Name", "any@email.com"));
        }

        [Fact]
        public async Task CreateNewUserAsync_CreatesUser_WhenValidData()
        {
            // Arrange
            var externalId = Guid.NewGuid();
            var name = "Ricardo";
            var email = "ricardo@test.com";
            var createdUser = new User
            {
                Id = Guid.NewGuid(),
                ExternalId = externalId,
                Name = name,
                Email = email,
                CreatedAt = DateTime.UtcNow
            };

            _userReporMock
                .Setup(r => r.GetByExternalIdAsync(externalId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            _userReporMock
                .Setup(r => r.AddAsync(It.Is<User>(u => u.ExternalId == externalId && u.Name == name && u.Email == email), It.IsAny<CancellationToken>()))
                .ReturnsAsync(createdUser);

            var service = CreateService();

            // Act
            var result = await service.CreateNewUserAsync(externalId, name, email);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(createdUser.Id, result.Id);
            Assert.Equal(name, result.Name);
            Assert.Equal(email, result.Email);
        }

        [Fact]
        public async Task CreateNewUserAsync_ThrowsInvalidOperationException_WhenUserAlreadyExists()
        {
            // Arrange
            var externalId = Guid.NewGuid();
            var name = "Ricardo";
            var email = "ricardo@test.com";
            var existingUser = new User
            {
                Id = Guid.NewGuid(),
                ExternalId = externalId,
                Name = name,
                Email = email,
                CreatedAt = DateTime.UtcNow
            };

            _userReporMock
                .Setup(r => r.GetByExternalIdAsync(externalId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingUser);

            var service = CreateService();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.CreateNewUserAsync(externalId, name, email));
        }
    }
}
