using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using BlankApp.Data;
using BlankApp.Models;
using Microsoft.Extensions.Logging;

namespace BlankApp.Services;

/// <summary>
/// Interface for user service operations.
/// Following Dependency Inversion Principle - depend on abstractions.
/// </summary>
public interface IUserServiceSample
{
    Task<UserSample?> GetUserAsync(int userId);
    Task<IEnumerable<UserSample>> GetUsersAsync();
    Task<int> CreateUserAsync(UserSample user);
    Task UpdateUserAsync(UserSample user);
    Task DeleteUserAsync(int userId);
}

/// <summary>
/// Service for user-related business logic.
/// Demonstrates SOLID principles, proper logging, and error handling.
/// </summary>
public class UserServiceSample : IUserServiceSample
{
    private readonly ILogger<UserServiceSample> _logger;
    private readonly IUserRepositorySample _userRepository;

    public UserServiceSample(ILogger<UserServiceSample> logger, IUserRepositorySample userRepository)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    /// <summary>
    /// Retrieves a user by ID.
    /// Demonstrates logging (entry/exit/errors) and error handling.
    /// </summary>
    public async Task<UserSample?> GetUserAsync(int userId)
    {
        _logger.LogTrace("Entering GetUserAsync with userId: {UserId}", userId);
        var sw = Stopwatch.StartNew();
        
        try
        {
            // Validation (business rule)
            if (userId <= 0)
            {
                throw new ArgumentException("Invalid user ID", nameof(userId));
            }

            // Call data layer
            var user = await _userRepository.GetByIdAsync(userId);
            
            if (user == null)
            {
                _logger.LogWarning("User not found: {UserId}", userId);
                return null;
            }

            _logger.LogInformation("GetUserAsync completed in {Duration}ms for user: {UserId}", 
                sw.ElapsedMilliseconds, userId);
            
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetUserAsync failed after {Duration}ms for user: {UserId}", 
                sw.ElapsedMilliseconds, userId);
            throw;
        }
    }

    /// <summary>
    /// Validates user data according to business rules.
    /// Demonstrates Single Responsibility - only business logic, no data access.
    /// </summary>
    public async Task<bool> ValidateUserAsync(UserSample user)
    {
        _logger.LogTrace("Entering ValidateUserAsync");
        
        if (user == null)
        {
            _logger.LogWarning("ValidateUserAsync: User is null");
            return false;
        }

        // Business validation rules
        var isValid = !string.IsNullOrWhiteSpace(user.Name);
        
        _logger.LogInformation("ValidateUserAsync result: {IsValid} for user: {UserName}", 
            isValid, user.Name);
        
        return await Task.FromResult(isValid);
    }

    /// <summary>
    /// Creates a new user after validation.
    /// Demonstrates orchestration of validation and data operations.
    /// </summary>
    public async Task<int> CreateUserAsync(UserSample user)
    {
        _logger.LogTrace("Entering CreateUserAsync");
        var sw = Stopwatch.StartNew();
        
        try
        {
            // Validate first (business logic)
            if (!await ValidateUserAsync(user))
            {
                throw new ArgumentException("User validation failed");
            }

            // Create in data layer
            var userId = await _userRepository.CreateAsync(user);
            
            _logger.LogInformation("CreateUserAsync completed in {Duration}ms, new userId: {UserId}", 
                sw.ElapsedMilliseconds, userId);
            
            return userId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateUserAsync failed after {Duration}ms", 
                sw.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task<IEnumerable<UserSample>> GetUsersAsync()
    {
        _logger.LogTrace("Entering GetUsersAsync");
        var sw = Stopwatch.StartNew();

        try
        {
            var users = await _userRepository.GetAllAsync();
            _logger.LogInformation("GetUsersAsync completed in {Duration}ms", sw.ElapsedMilliseconds);
            return users;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetUsersAsync failed after {Duration}ms", sw.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task UpdateUserAsync(UserSample user)
    {
        _logger.LogTrace("Entering UpdateUserAsync for id {Id}", user?.Id);
        var sw = Stopwatch.StartNew();

        try
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (!await ValidateUserAsync(user))
            {
                throw new ArgumentException("User validation failed");
            }

            await _userRepository.UpdateAsync(user);
            _logger.LogInformation("UpdateUserAsync completed in {Duration}ms for id {Id}", sw.ElapsedMilliseconds, user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateUserAsync failed after {Duration}ms", sw.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task DeleteUserAsync(int userId)
    {
        _logger.LogTrace("Entering DeleteUserAsync for id {Id}", userId);
        var sw = Stopwatch.StartNew();

        try
        {
            if (userId <= 0)
            {
                throw new ArgumentException("Invalid user ID", nameof(userId));
            }

            await _userRepository.DeleteAsync(userId);
            _logger.LogInformation("DeleteUserAsync completed in {Duration}ms for id {Id}", sw.ElapsedMilliseconds, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteUserAsync failed after {Duration}ms", sw.ElapsedMilliseconds);
            throw;
        }
    }
}
