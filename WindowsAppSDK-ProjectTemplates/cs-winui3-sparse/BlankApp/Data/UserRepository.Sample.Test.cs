using System;
using System.Threading.Tasks;
using BlankApp.Data;
using BlankApp.Models;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace BlankApp.Data.Tests;

/// <summary>
/// Unit tests for <see cref="UserRepositorySample"/> sample repository.
/// Demonstrates data layer testing with proper mocking.
/// </summary>
[TestClass]
public class UserRepositorySampleTests
{
    private Mock<ILogger<UserRepositorySample>> _mockLogger = null!;
    private UserRepositorySample _sut = null!; // System Under Test

    [TestInitialize]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<UserRepositorySample>>();
        _sut = new UserRepositorySample(_mockLogger.Object);
    }

    [TestMethod]
    public async Task GetByIdAsync_WhenUserExists_ShouldReturnUser()
    {
        // Arrange
        var user = new UserSample { Id = 1, Name = "John Doe", CreatedAt = DateTime.UtcNow };
        await _sut.CreateAsync(user);

        // Act
        var result = await _sut.GetByIdAsync(1);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Id);
        Assert.AreEqual("John Doe", result.Name);
    }

    [TestMethod]
    public async Task GetByIdAsync_WhenUserNotFound_ShouldReturnNull()
    {
        // Act
        var result = await _sut.GetByIdAsync(999);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task CreateAsync_WhenValidUser_ShouldAssignIdAndReturnIt()
    {
        // Arrange
        var user = new UserSample { Name = "Jane Doe", CreatedAt = DateTime.UtcNow };

        // Act
        var userId = await _sut.CreateAsync(user);

        // Assert
        Assert.IsTrue(userId > 0);
        Assert.AreEqual(userId, user.Id);
        Assert.IsTrue(user.CreatedAt > DateTime.MinValue);
    }

    [TestMethod]
    public async Task CreateAsync_WhenUserIsNull_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => _sut.CreateAsync(null!));
    }

    [TestMethod]
    public void Constructor_WhenLoggerIsNull_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new UserRepositorySample(null!));
    }
}
