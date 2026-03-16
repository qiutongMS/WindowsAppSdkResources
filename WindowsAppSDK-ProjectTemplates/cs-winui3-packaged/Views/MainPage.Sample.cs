using BlankApp.Models;
using BlankApp.ViewModels;
using Microsoft.UI;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Markup;

namespace BlankApp.Views;

/// <summary>
/// Main page of the application.
/// Demonstrates MVVM pattern with minimal code-behind.
/// </summary>
public sealed partial class MainPageSample : Page
{
    private readonly ILogger<MainPageSample> _logger;

    /// <summary>
    /// Gets the ViewModel for this page.
    /// ViewModel is injected via dependency injection.
    /// </summary>
    public MainViewModelSample ViewModel { get; }

    /// <summary>
    /// Initializes a new instance of MainPage.
    /// </summary>
    public MainPageSample()
    {
        // Get ViewModel from DI container
        _logger = App.GetService<ILogger<MainPageSample>>();
        ViewModel = App.GetService<MainViewModelSample>();

        // Set DataContext for bindings
        DataContext = ViewModel;

        // Build a minimal UI in code so samples render without XAML assets
        InitializeComponent();

        _logger.LogDebug("MainPage constructed");
    }

    // Build minimal UI without XAML
    private void InitializeComponent()
    {
        var root = new Grid
        {
            Padding = new Thickness(32)
        };

        var content = new StackPanel
        {
            Spacing = 16,
            MaxWidth = 760,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var title = new TextBlock
        {
            FontSize = 26,
            FontWeight = FontWeights.SemiBold,
            TextAlignment = TextAlignment.Center
        };
        title.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath(nameof(ViewModel.Title)), Mode = BindingMode.OneWay });

        var status = new TextBlock { TextAlignment = TextAlignment.Center };
        status.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath(nameof(ViewModel.StatusMessage)), Mode = BindingMode.OneWay });

        var progress = new ProgressRing
        {
            Width = 36,
            Height = 36,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        progress.SetBinding(ProgressRing.IsActiveProperty, new Binding { Path = new PropertyPath(nameof(ViewModel.IsLoading)), Mode = BindingMode.OneWay });

        var listCard = new Border
        {
            Background = new SolidColorBrush(Colors.White),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 8, 0, 0)
        };

        var list = new ListView
        {
            Height = 260,
            SelectionMode = ListViewSelectionMode.Single
        };
        list.SetBinding(ItemsControl.ItemsSourceProperty, new Binding { Path = new PropertyPath(nameof(ViewModel.Users)), Mode = BindingMode.OneWay });
        list.SelectionChanged += OnSelectionChanged;
        list.ItemTemplate = BuildTemplate();
        listCard.Child = list;

        var inputRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var nameBox = new TextBox
        {
            PlaceholderText = "Name",
            Width = 320,
            Padding = new Thickness(12, 8, 12, 8)
        };
        nameBox.SetBinding(TextBox.TextProperty, new Binding { Path = new PropertyPath(nameof(ViewModel.NameInput)), Mode = BindingMode.TwoWay });
        inputRow.Children.Add(nameBox);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var add = new Button { Content = "Add", Width = 90, Padding = new Thickness(12, 8, 12, 8) };
        add.Click += OnCreateSampleUserClick;

        var update = new Button { Content = "Update", Width = 90, Padding = new Thickness(12, 8, 12, 8) };
        update.Click += OnUpdateClick;

        var delete = new Button { Content = "Delete", Width = 90, Padding = new Thickness(12, 8, 12, 8) };
        delete.Click += OnDeleteClick;

        var refresh = new Button { Content = "Refresh", Width = 100, Padding = new Thickness(12, 8, 12, 8) };
        refresh.Click += OnRefreshClick;

        buttons.Children.Add(add);
        buttons.Children.Add(update);
        buttons.Children.Add(delete);
        buttons.Children.Add(refresh);

        content.Children.Add(title);
        content.Children.Add(status);
        content.Children.Add(progress);
        content.Children.Add(listCard);
        content.Children.Add(inputRow);
        content.Children.Add(buttons);

        root.Children.Add(content);

        Content = root;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private static DataTemplate BuildTemplate()
    {
        const string templateXaml =
            "<DataTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" " +
            "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">" +
            "<StackPanel Orientation=\"Vertical\" Spacing=\"2\">" +
            "  <TextBlock Text=\"{Binding Name}\" />" +
            "  <TextBlock Text=\"{Binding CreatedAt}\" Opacity=\"0.7\" FontSize=\"12\" />" +
            "</StackPanel>" +
            "</DataTemplate>";

        return (DataTemplate)XamlReader.Load(templateXaml);
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadDataAsync();
    }

    private async void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.RefreshAsync();
    }

    private async void OnCreateSampleUserClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.CreateSampleUserAsync();
    }

    private async void OnUpdateClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.UpdateUserAsync();
    }

    private async void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.DeleteUserAsync();
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListView lv && lv.SelectedItem is Models.UserSample user)
        {
            ViewModel.SelectedUser = user;
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        // UI cleanup placeholder
    }
}

/// <summary>
/// Example XAML structure for this page:
/// 
/// <Page
///     x:Class="BlankApp.Views.MainPage"
///     xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
///     xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
///     Loaded="OnLoaded"
///     Unloaded="OnUnloaded">
///     
///     <Grid>
///         <StackPanel Spacing="16" Padding="24">
///             <TextBlock Text="{x:Bind ViewModel.Title, Mode=OneWay}" 
///                        Style="{StaticResource TitleTextBlockStyle}"/>
///             
///             <TextBlock Text="{x:Bind ViewModel.StatusMessage, Mode=OneWay}" />
///             
///             <ProgressRing IsActive="{x:Bind ViewModel.IsLoading, Mode=OneWay}" />
///             
///             <Button Content="Refresh" Click="OnRefreshClick" 
///                     IsEnabled="{x:Bind ViewModel.IsLoading, Mode=OneWay, Converter={StaticResource InverseBoolConverter}}" />
///         </StackPanel>
///     </Grid>
/// </Page>
/// 
/// </summary>
/// 