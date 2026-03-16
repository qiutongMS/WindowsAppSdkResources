using System;
using System.Threading.Tasks;
using BlankApp.Data;
using BlankApp.Models;
using BlankApp.Services;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace BlankApp.Services.Tests;

/// <summary>
/// Unit tests for <see cref="UserServiceSample"/> sample service.
/// Test file is co-located with source: UserService.Sample.Test.cs beside UserService.Sample.cs
/// </summary>
[TestClass]
public class UserServiceSampleTests
{
    private Mock<ILogger<UserServiceSample>> _mockLogger = null!;
    private Mock<IUserRepositorySample> _mockUserRepository = null!;
    private UserServiceSample _sut = null!; // System Under Test

    [TestInitialize]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<UserServiceSample>>();
        _mockUserRepository = new Mock<IUserRepositorySample>();
        _sut = new UserServiceSample(_mockLogger.Object, _mockUserRepository.Object);
    }

    [TestMethod]
    public async Task GetUserAsync_WhenUserExists_ShouldReturnUser()
    {
        // Arrange
        var userId = 1;
        var expectedUser = new UserSample { Id = userId, Name = "John Doe" };
        _mockUserRepository.Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(expectedUser);

        // Act
        var result = await _sut.GetUserAsync(userId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(userId, result.Id);
        Assert.AreEqual("John Doe", result.Name);
        _mockUserRepository.Verify(r => r.GetByIdAsync(userId), Times.Once);
    }

    [TestMethod]
    public async Task GetUserAsync_WhenUserNotFound_ShouldReturnNull()
    {
        // Arrange
        var userId = 999;
        _mockUserRepository.Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync((UserSample?)null);

        // Act
        var result = await _sut.GetUserAsync(userId);

        // Assert
        Assert.IsNull(result);
        _mockUserRepository.Verify(r => r.GetByIdAsync(userId), Times.Once);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public async Task GetUserAsync_WhenInvalidUserId_ShouldThrowArgumentException(int userId)
    {
        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => _sut.GetUserAsync(userId));
        _mockUserRepository.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }
}
