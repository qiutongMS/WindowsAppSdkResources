using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using BlankApp.Models;
using BlankApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace BlankApp.ViewModels;

/// <summary>
/// ViewModel for the main page.
/// Demonstrates MVVM pattern, property change notifications, and command implementation.
/// </summary>
public class MainViewModelSample : ObservableObject
{
    private readonly ILogger<MainViewModelSample> _logger;
    private readonly IUserServiceSample _userService;
    
    private string _title;
    private string _statusMessage;
    private bool _isLoading;
    private ObservableCollection<UserSample> _users;
    private UserSample? _selectedUser;
    private string _nameInput;

    public MainViewModelSample(ILogger<MainViewModelSample> logger, IUserServiceSample userService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        
        // Initialize commands
        LoadDataCommand = new AsyncRelayCommand(LoadDataAsync, () => !IsLoading);
        RefreshCommand = new AsyncRelayCommand(RefreshAsync, () => !IsLoading);
        CreateSampleUserCommand = new AsyncRelayCommand(CreateSampleUserAsync, () => !IsLoading);
        UpdateUserCommand = new AsyncRelayCommand(UpdateUserAsync, () => !IsLoading && SelectedUser != null);
        DeleteUserCommand = new AsyncRelayCommand(DeleteUserAsync, () => !IsLoading && SelectedUser != null);
        
        // Set defaults
        _title = "Welcome";
        _statusMessage = string.Empty;
        _users = new ObservableCollection<UserSample>();
        _nameInput = string.Empty;
    }

    #region Properties

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (SetProperty(ref _isLoading, value))
            {
                // Notify commands to re-evaluate CanExecute
                LoadDataCommand.NotifyCanExecuteChanged();
                RefreshCommand.NotifyCanExecuteChanged();
                UpdateUserCommand.NotifyCanExecuteChanged();
                DeleteUserCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public ObservableCollection<UserSample> Users
    {
        get => _users;
        set => SetProperty(ref _users, value);
    }

    public UserSample? SelectedUser
    {
        get => _selectedUser;
        set
        {
            if (SetProperty(ref _selectedUser, value))
            {
                NameInput = value?.Name ?? string.Empty;
                UpdateUserCommand.NotifyCanExecuteChanged();
                DeleteUserCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string NameInput
    {
        get => _nameInput;
        set => SetProperty(ref _nameInput, value);
    }

    #endregion

    #region Commands

    public IAsyncRelayCommand LoadDataCommand { get; }
    public IAsyncRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand CreateSampleUserCommand { get; }
    public IAsyncRelayCommand UpdateUserCommand { get; }
    public IAsyncRelayCommand DeleteUserCommand { get; }

    #endregion

    #region Methods

    /// <summary>
    /// Loads users from the service.
    /// </summary>
    public async Task LoadDataAsync()
    {
        _logger.LogTrace("Entering LoadDataAsync");
        var sw = Stopwatch.StartNew();
        
        try
        {
            IsLoading = true;
            StatusMessage = "Loading...";

            var users = await _userService.GetUsersAsync();
            Users = new ObservableCollection<UserSample>(users);

            if (Users.Count > 0)
            {
                SelectedUser = Users[0];
                Title = $"Users ({Users.Count})";
                StatusMessage = "Data loaded successfully";
            }
            else
            {
                SelectedUser = null;
                Title = "Users";
                StatusMessage = "No users yet";
            }
            
            _logger.LogInformation("LoadDataAsync completed in {Duration}ms", 
                sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LoadDataAsync failed after {Duration}ms", 
                sw.ElapsedMilliseconds);
            StatusMessage = "Error loading data";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Refreshes the current data.
    /// </summary>
    public async Task RefreshAsync()
    {
        _logger.LogTrace("Entering RefreshAsync");
        
        SelectedUser = null;
        Title = "Users";
        await LoadDataAsync();
    }

    public bool ValidateInput(string input)
    {
        return !string.IsNullOrWhiteSpace(input);
    }

    public async Task CreateSampleUserAsync()
    {
        _logger.LogTrace("Entering CreateSampleUserAsync");
        var sw = Stopwatch.StartNew();

        try
        {
            IsLoading = true;
            StatusMessage = "Creating user...";

            var name = string.IsNullOrWhiteSpace(NameInput) ? "Sample User" : NameInput.Trim();

            var sample = new UserSample
            {
                Name = name,
                CreatedAt = DateTime.UtcNow
            };

            await _userService.CreateUserAsync(sample);

            StatusMessage = "User created";

            await LoadDataAsync();

            _logger.LogInformation("CreateSampleUserAsync completed in {Duration}ms", sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateSampleUserAsync failed after {Duration}ms", sw.ElapsedMilliseconds);
            StatusMessage = "Error creating sample user";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task UpdateUserAsync()
    {
        _logger.LogTrace("Entering UpdateUserAsync");
        var sw = Stopwatch.StartNew();

        try
        {
            if (SelectedUser == null)
            {
                return;
            }

            IsLoading = true;
            StatusMessage = "Updating user...";

            SelectedUser.Name = string.IsNullOrWhiteSpace(NameInput) ? SelectedUser.Name : NameInput.Trim();
            SelectedUser.UpdatedAt = DateTime.UtcNow;

            await _userService.UpdateUserAsync(SelectedUser);

            StatusMessage = "User updated";

            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateUserAsync failed after {Duration}ms", sw.ElapsedMilliseconds);
            StatusMessage = "Error updating user";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task DeleteUserAsync()
    {
        _logger.LogTrace("Entering DeleteUserAsync");
        var sw = Stopwatch.StartNew();

        try
        {
            if (SelectedUser == null)
            {
                return;
            }

            IsLoading = true;
            StatusMessage = "Deleting user...";

            await _userService.DeleteUserAsync(SelectedUser.Id);

            StatusMessage = "User deleted";

            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteUserAsync failed after {Duration}ms", sw.ElapsedMilliseconds);
            StatusMessage = "Error deleting user";
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion
}

