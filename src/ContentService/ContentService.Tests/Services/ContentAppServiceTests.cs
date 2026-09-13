using ContentService.Application.DTOs;
using ContentService.Application.Exceptions;
using ContentService.Application.Interfaces;
using ContentService.Application.Services;
using ContentService.Domain.Entities;
using FluentAssertions;
using Moq;

namespace ContentService.Tests.Services;

public class ContentAppServiceTests
{
    private readonly Mock<IContentRepository> _repositoryMock = new();
    private readonly Mock<IUserServiceClient> _userClientMock = new();
    private readonly Mock<ICacheService> _cacheMock = new();
    private readonly ContentAppService _sut;

    public ContentAppServiceTests()
    {
        _cacheMock.Setup(c => c.GetAsync<ContentDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ContentDto?)null);
        _cacheMock.Setup(c => c.GetAsync<List<ContentDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<ContentDto>?)null);
        _cacheMock.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<ContentDto>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _cacheMock.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<List<ContentDto>>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _cacheMock.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _sut = new ContentAppService(_repositoryMock.Object, _userClientMock.Object, _cacheMock.Object);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnContentDto_WhenUserExists()
    {
        var request = new CreateContentRequest("Test Title", "Test Body", Guid.NewGuid());
        _userClientMock.Setup(c => c.UserExistsAsync(request.AuthorId, It.IsAny<string>(), default)).ReturnsAsync(true);

        var result = await _sut.CreateAsync(request, "corr-123");

        result.Should().NotBeNull();
        result.Title.Should().Be("Test Title");
        result.Body.Should().Be("Test Body");
        result.AuthorId.Should().Be(request.AuthorId);
        result.Status.Should().Be("Active");

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Content>(), default), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowAuthorNotFoundException_WhenUserDoesNotExist()
    {
        var authorId = Guid.NewGuid();
        var request = new CreateContentRequest("Title", "Body", authorId);
        _userClientMock.Setup(c => c.UserExistsAsync(authorId, It.IsAny<string>(), default)).ReturnsAsync(false);

        var act = async () => await _sut.CreateAsync(request, "corr-123");

        await act.Should().ThrowAsync<AuthorNotFoundException>()
            .WithMessage($"*{authorId}*");

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Content>(), default), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowUserServiceUnavailableException_WhenClientFails()
    {
        var request = new CreateContentRequest("Title", "Body", Guid.NewGuid());
        _userClientMock.Setup(c => c.UserExistsAsync(It.IsAny<Guid>(), It.IsAny<string>(), default))
            .ThrowsAsync(new UserServiceUnavailableException("Circuit breaker open"));

        var act = async () => await _sut.CreateAsync(request, "corr-123");

        await act.Should().ThrowAsync<UserServiceUnavailableException>()
            .WithMessage("*Circuit breaker open*");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnContent_WhenExists()
    {
        var content = Content.Create("Existing Title", "Body", Guid.NewGuid());
        _repositoryMock.Setup(r => r.GetByIdAsync(content.Id, default)).ReturnsAsync(content);

        var result = await _sut.GetByIdAsync(content.Id);

        result.Should().NotBeNull();
        result.Id.Should().Be(content.Id);
        result.Title.Should().Be("Existing Title");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldThrowContentNotFoundException_WhenNotExists()
    {
        var id = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync((Content?)null);

        var act = async () => await _sut.GetByIdAsync(id);

        await act.Should().ThrowAsync<ContentNotFoundException>()
            .WithMessage($"*{id}*");
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnUpdatedContent_WhenValid()
    {
        var content = Content.Create("Old Title", "Old Body", Guid.NewGuid());
        var request = new UpdateContentRequest("New Title", "New Body");
        _repositoryMock.Setup(r => r.GetByIdAsync(content.Id, default)).ReturnsAsync(content);

        var result = await _sut.UpdateAsync(content.Id, request);

        result.Title.Should().Be("New Title");
        result.Body.Should().Be("New Body");
    }

    [Fact]
    public async Task DeleteAsync_ShouldSoftDelete_WhenExists()
    {
        var content = Content.Create("To Delete", "Body", Guid.NewGuid());
        _repositoryMock.Setup(r => r.GetByIdAsync(content.Id, default)).ReturnsAsync(content);

        await _sut.DeleteAsync(content.Id);

        content.IsActive().Should().BeFalse();
        _repositoryMock.Verify(r => r.UpdateAsync(content, default), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task DeleteByAuthorAsync_ShouldSoftDeleteAllAuthorContent()
    {
        var authorId = Guid.NewGuid();
        var contents = new List<Content>
        {
            Content.Create("Content 1", "Body 1", authorId),
            Content.Create("Content 2", "Body 2", authorId)
        };
        _repositoryMock.Setup(r => r.GetByAuthorIdAsync(authorId, default)).ReturnsAsync(contents);

        await _sut.DeleteByAuthorAsync(authorId);

        contents.Should().AllSatisfy(c => c.IsActive().Should().BeFalse());
        _repositoryMock.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }
}
