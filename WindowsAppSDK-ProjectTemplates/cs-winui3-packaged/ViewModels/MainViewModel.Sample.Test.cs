using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlankApp.Models;
using BlankApp.Services;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace BlankApp.ViewModels.Tests;

/// <summary>
/// Unit tests for MainViewModelSample.
/// </summary>
[TestClass]
public class MainViewModelSampleTests
{
    private Mock<ILogger<MainViewModelSample>> _mockLogger = null!;
    private Mock<IUserServiceSample> _mockUserService = null!;
    private MainViewModelSample _sut = null!; // System Under Test

    [TestInitialize]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<MainViewModelSample>>();
        _mockUserService = new Mock<IUserServiceSample>();
        _sut = new MainViewModelSample(_mockLogger.Object, _mockUserService.Object);
    }

    [TestMethod]
    public async Task LoadDataAsync_WhenUsersExist_ShouldPopulateList()
    {
        // Arrange
        var expectedUser = new UserSample { Id = 1, Name = "John Doe" };
        _mockUserService.Setup(s => s.GetUsersAsync())
            .ReturnsAsync(new List<UserSample> { expectedUser });

        // Act
        await _sut.LoadDataAsync();

        // Assert
        Assert.AreEqual("Users (1)", _sut.Title);
        Assert.AreEqual(expectedUser, _sut.SelectedUser);
        Assert.IsFalse(_sut.IsLoading);
        _mockUserService.Verify(s => s.GetUsersAsync(), Times.Once);
    }

    [TestMethod]
    public async Task LoadDataAsync_WhenNoUsers_ShouldShowEmptyState()
    {
        // Arrange
        _mockUserService.Setup(s => s.GetUsersAsync())
            .ReturnsAsync(new List<UserSample>());

        // Act
        await _sut.LoadDataAsync();

        // Assert
        Assert.IsNull(_sut.SelectedUser);
        Assert.AreEqual("No users yet", _sut.StatusMessage);
        Assert.IsFalse(_sut.IsLoading);
    }

    [TestMethod]
    public async Task LoadDataAsync_WhenServiceFails_ShouldHandleError()
    {
        // Arrange
        _mockUserService.Setup(s => s.GetUsersAsync())
            .ThrowsAsync(new Exception("Service error"));

        // Act
        await _sut.LoadDataAsync();

        // Assert
        Assert.AreEqual("Error loading data", _sut.StatusMessage);
        Assert.IsFalse(_sut.IsLoading);
        _mockUserService.Verify(s => s.GetUsersAsync(), Times.Once);
    }

    [TestMethod]
    public void Title_WhenChanged_ShouldRaisePropertyChanged()
    {
        // Arrange
        var propertyChangedRaised = false;
        _sut.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(_sut.Title))
            {
                propertyChangedRaised = true;
            }
        };

        // Act
        _sut.Title = "New Title";

        // Assert
        Assert.IsTrue(propertyChangedRaised);
        Assert.AreEqual("New Title", _sut.Title);
    }
}
