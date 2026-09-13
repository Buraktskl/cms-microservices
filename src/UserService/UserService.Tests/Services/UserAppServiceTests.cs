using FluentAssertions;
using Moq;
using UserService.Application.DTOs;
using UserService.Application.Exceptions;
using UserService.Application.Interfaces;
using UserService.Application.Services;
using UserService.Domain.Entities;

namespace UserService.Tests.Services;

public class UserAppServiceTests
{
    private readonly Mock<IUserRepository> _repositoryMock = new();
    private readonly Mock<IUserEventPublisher> _publisherMock = new();
    private readonly Mock<ICacheService> _cacheMock = new();
    private readonly UserAppService _sut;

    public UserAppServiceTests()
    {
        _cacheMock.Setup(c => c.GetAsync<UserDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserDto?)null);
        _cacheMock.Setup(c => c.GetAsync<List<UserDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<UserDto>?)null);
        _cacheMock.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<UserDto>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _cacheMock.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<List<UserDto>>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _cacheMock.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _sut = new UserAppService(_repositoryMock.Object, _publisherMock.Object, _cacheMock.Object);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnUserDto_WhenValidRequest()
    {
        var request = new CreateUserRequest("johndoe", "john@example.com", "John Doe");
        _repositoryMock.Setup(r => r.ExistsByUsernameAsync(request.Username, default)).ReturnsAsync(false);
        _repositoryMock.Setup(r => r.ExistsByEmailAsync(request.Email, default)).ReturnsAsync(false);

        var result = await _sut.CreateAsync(request);

        result.Should().NotBeNull();
        result.Username.Should().Be("johndoe");
        result.Email.Should().Be("john@example.com");
        result.FullName.Should().Be("John Doe");
        result.Status.Should().Be("Active");

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<User>(), default), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowUserAlreadyExistsException_WhenUsernameIsTaken()
    {
        var request = new CreateUserRequest("existing", "new@example.com", "New User");
        _repositoryMock.Setup(r => r.ExistsByUsernameAsync("existing", default)).ReturnsAsync(true);

        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<UserAlreadyExistsException>()
            .WithMessage("*username*existing*");
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowUserAlreadyExistsException_WhenEmailIsTaken()
    {
        var request = new CreateUserRequest("newuser", "taken@example.com", "New User");
        _repositoryMock.Setup(r => r.ExistsByUsernameAsync("newuser", default)).ReturnsAsync(false);
        _repositoryMock.Setup(r => r.ExistsByEmailAsync("taken@example.com", default)).ReturnsAsync(true);

        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<UserAlreadyExistsException>()
            .WithMessage("*email*taken@example.com*");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnUser_WhenExists()
    {
        var user = User.Create("testuser", "test@example.com", "Test User");
        _repositoryMock.Setup(r => r.GetByIdAsync(user.Id, default)).ReturnsAsync(user);

        var result = await _sut.GetByIdAsync(user.Id);

        result.Should().NotBeNull();
        result.Id.Should().Be(user.Id);
        result.Username.Should().Be("testuser");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldThrowUserNotFoundException_WhenNotExists()
    {
        var id = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync((User?)null);

        var act = async () => await _sut.GetByIdAsync(id);

        await act.Should().ThrowAsync<UserNotFoundException>()
            .WithMessage($"*{id}*");
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnUpdatedUser_WhenValid()
    {
        var user = User.Create("updateme", "old@example.com", "Old Name");
        var request = new UpdateUserRequest("new@example.com", "New Name");

        _repositoryMock.Setup(r => r.GetByIdAsync(user.Id, default)).ReturnsAsync(user);
        _repositoryMock.Setup(r => r.ExistsByEmailAsync("new@example.com", default)).ReturnsAsync(false);

        var result = await _sut.UpdateAsync(user.Id, request);

        result.Email.Should().Be("new@example.com");
        result.FullName.Should().Be("New Name");
    }

    [Fact]
    public async Task DeleteAsync_ShouldPublishEvent_AndMarkDeleted_WhenSuccessful()
    {
        var user = User.Create("todelete", "delete@example.com", "Delete Me");
        _repositoryMock.Setup(r => r.GetByIdAsync(user.Id, default)).ReturnsAsync(user);
        _publisherMock.Setup(p => p.PublishUserDeletedAsync(user.Id, user.Username, "corr-123", default))
            .Returns(Task.CompletedTask);

        await _sut.DeleteAsync(user.Id, "corr-123");

        _publisherMock.Verify(p => p.PublishUserDeletedAsync(user.Id, user.Username, "corr-123", default), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(default), Times.AtLeastOnce);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRestoreUser_WhenEventPublishFails()
    {
        var user = User.Create("faildelete", "fail@example.com", "Fail Delete");
        _repositoryMock.Setup(r => r.GetByIdAsync(user.Id, default)).ReturnsAsync(user);
        _publisherMock.Setup(p => p.PublishUserDeletedAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), default))
            .ThrowsAsync(new Exception("RabbitMQ connection failed"));

        var act = async () => await _sut.DeleteAsync(user.Id, "corr-456");

        await act.Should().ThrowAsync<UserDeletionFailedException>();

        user.IsActive().Should().BeTrue();
    }
}
