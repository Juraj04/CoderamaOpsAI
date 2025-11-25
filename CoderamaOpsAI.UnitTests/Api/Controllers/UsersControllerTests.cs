using CoderamaOpsAI.Api.Controllers;
using CoderamaOpsAI.Api.Models;
using CoderamaOpsAI.Dal.Entities;
using CoderamaOpsAI.UnitTests.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace CoderamaOpsAI.UnitTests.Api.Controllers;

public class UsersControllerTests : DatabaseTestBase
{
    private readonly ILogger<UsersController> _logger;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _logger = Substitute.For<ILogger<UsersController>>();
        _controller = new UsersController(DbContext, _logger);
    }

    [Fact]
    public async Task GetAll_WithNoUsers_ReturnsEmptyList()
    {
        // Act
        var result = await _controller.GetAll(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as List<UserResponse>;
        response.Should().NotBeNull();
        response.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAll_WithUsers_ReturnsUserListWithoutPasswords()
    {
        // Arrange
        var user = new User
        {
            Name = "Test User",
            Email = "test@example.com",
            Password = BCrypt.Net.BCrypt.HashPassword("password"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await DbContext.Users.AddAsync(user);
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();

        // Act
        var result = await _controller.GetAll(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as List<UserResponse>;
        response.Should().NotBeNull();
        response.Should().HaveCount(1);
        response![0].Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task GetAll_UsesAsNoTracking()
    {
        // Arrange
        var user = new User
        {
            Name = "Test",
            Email = "test@example.com",
            Password = "hashedpassword",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await DbContext.Users.AddAsync(user);
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();

        // Act
        await _controller.GetAll(CancellationToken.None);

        // Assert
        DbContext.ChangeTracker.Entries().Should().BeEmpty();
    }

    [Fact]
    public async Task GetById_WithExistingUser_ReturnsUser()
    {
        // Arrange
        var user = new User
        {
            Name = "Test User",
            Email = "test@example.com",
            Password = "hashedpassword",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await DbContext.Users.AddAsync(user);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetById(user.Id, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as UserResponse;
        response.Should().NotBeNull();
        response!.Id.Should().Be(user.Id);
        response.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task GetById_WithNonExistentUser_ReturnsNotFound()
    {
        // Act
        var result = await _controller.GetById(999, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_WithValidData_ReturnsCreatedUser()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Name = "New User",
            Email = "newuser@example.com",
            Password = "password123"
        };

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = (CreatedAtActionResult)result;
        var response = createdResult.Value as UserResponse;
        response.Should().NotBeNull();
        response!.Name.Should().Be("New User");
        response.Email.Should().Be("newuser@example.com");
    }

    [Fact]
    public async Task Create_HashesPassword_DoesNotReturnPlainText()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Name = "Test",
            Email = "test@example.com",
            Password = "plainpassword"
        };

        // Act
        await _controller.Create(request, CancellationToken.None);

        // Assert
        var user = await DbContext.Users.FindAsync(1);
        user.Should().NotBeNull();
        user!.Password.Should().NotBe("plainpassword");
        BCrypt.Net.BCrypt.Verify("plainpassword", user.Password).Should().BeTrue();
    }

    [Fact]
    public async Task Create_SetsAuditFields_CreatedAtAndUpdatedAt()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Name = "Test",
            Email = "test@example.com",
            Password = "password"
        };
        var beforeCreate = DateTime.UtcNow;

        // Act
        await _controller.Create(request, CancellationToken.None);

        // Assert
        var user = await DbContext.Users.FindAsync(1);
        user.Should().NotBeNull();
        user!.CreatedAt.Should().BeCloseTo(beforeCreate, TimeSpan.FromSeconds(2));
        user.UpdatedAt.Should().BeCloseTo(beforeCreate, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task Create_WithDuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        var existingUser = new User
        {
            Name = "Existing",
            Email = "existing@example.com",
            Password = "hash",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await DbContext.Users.AddAsync(existingUser);
        await DbContext.SaveChangesAsync();

        var request = new CreateUserRequest
        {
            Name = "New",
            Email = "existing@example.com",
            Password = "password"
        };

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Update_WithExistingUser_ReturnsUpdatedUser()
    {
        // Arrange
        var user = new User
        {
            Name = "Original",
            Email = "original@example.com",
            Password = "hash",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await DbContext.Users.AddAsync(user);
        await DbContext.SaveChangesAsync();

        var request = new UpdateUserRequest
        {
            Name = "Updated"
        };

        // Act
        var result = await _controller.Update(user.Id, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as UserResponse;
        response.Should().NotBeNull();
        response!.Name.Should().Be("Updated");
    }

    [Fact]
    public async Task Update_WithNonExistentUser_ReturnsNotFound()
    {
        // Arrange
        var request = new UpdateUserRequest
        {
            Name = "Updated"
        };

        // Act
        var result = await _controller.Update(999, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Update_WithNewPassword_HashesPassword()
    {
        // Arrange
        var user = new User
        {
            Name = "Test",
            Email = "test@example.com",
            Password = BCrypt.Net.BCrypt.HashPassword("oldpassword"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await DbContext.Users.AddAsync(user);
        await DbContext.SaveChangesAsync();

        var request = new UpdateUserRequest
        {
            Password = "newpassword"
        };

        // Act
        await _controller.Update(user.Id, request, CancellationToken.None);

        // Assert
        var updatedUser = await DbContext.Users.FindAsync(user.Id);
        BCrypt.Net.BCrypt.Verify("newpassword", updatedUser!.Password).Should().BeTrue();
    }

    [Fact]
    public async Task Update_UpdatesUpdatedAtField_DoesNotChangeCreatedAt()
    {
        // Arrange
        var originalCreatedAt = DateTime.UtcNow.AddDays(-1);
        var user = new User
        {
            Name = "Test",
            Email = "test@example.com",
            Password = "hash",
            CreatedAt = originalCreatedAt,
            UpdatedAt = originalCreatedAt
        };
        await DbContext.Users.AddAsync(user);
        await DbContext.SaveChangesAsync();

        var request = new UpdateUserRequest
        {
            Name = "Updated"
        };

        // Act
        await _controller.Update(user.Id, request, CancellationToken.None);

        // Assert
        var updatedUser = await DbContext.Users.FindAsync(user.Id);
        updatedUser!.CreatedAt.Should().Be(originalCreatedAt);
        updatedUser.UpdatedAt.Should().BeAfter(originalCreatedAt);
    }

    [Fact]
    public async Task Update_WithPartialData_OnlyUpdatesProvidedFields()
    {
        // Arrange
        var user = new User
        {
            Name = "Original Name",
            Email = "original@example.com",
            Password = "originalhash",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await DbContext.Users.AddAsync(user);
        await DbContext.SaveChangesAsync();

        var request = new UpdateUserRequest
        {
            Name = "Updated Name"
            // Email and Password not provided
        };

        // Act
        await _controller.Update(user.Id, request, CancellationToken.None);

        // Assert
        var updatedUser = await DbContext.Users.FindAsync(user.Id);
        updatedUser!.Name.Should().Be("Updated Name");
        updatedUser.Email.Should().Be("original@example.com");
        updatedUser.Password.Should().Be("originalhash");
    }

    [Fact]
    public async Task Delete_WithExistingUser_ReturnsNoContent()
    {
        // Arrange
        var user = new User
        {
            Name = "Test",
            Email = "test@example.com",
            Password = "hash",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await DbContext.Users.AddAsync(user);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _controller.Delete(user.Id, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        var deletedUser = await DbContext.Users.FindAsync(user.Id);
        deletedUser.Should().BeNull();
    }

    [Fact]
    public async Task Delete_WithNonExistentUser_ReturnsNotFound()
    {
        // Act
        var result = await _controller.Delete(999, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
