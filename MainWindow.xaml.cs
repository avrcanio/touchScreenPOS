using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TouchScreenPOS.Api;
using TouchScreenPOS.ViewModels;
using TouchScreenPOS.Utils;

namespace TouchScreenPOS;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private static readonly StringComparer HrComparer = StringComparer.Create(new CultureInfo("hr-HR"), ignoreCase: true);
    private readonly ApiClient _apiClient = new();
    private readonly ObservableCollection<ArtiklDisplay> _artikli = new();
    private readonly ObservableCollection<RepresentationCartItem> _cartItems = new();
    private readonly ObservableCollection<CategoryCard> _categoryCards = new();
    private readonly ImageCache _imageCache;
    private List<ArtiklDisplay> _allArtikli = new();
    private int? _activeCategoryId;

    public MainWindow()
    {
        _imageCache = new ImageCache(_apiClient.GetBytesAsync);
        InitializeComponent();
        ArtikliItemsControl.ItemsSource = _artikli;
        CartDataGrid.ItemsSource = _cartItems;
        CategoryItemsControl.ItemsSource = _categoryCards;
        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private async void Login_Click(object sender, RoutedEventArgs e)
    {
        var username = UsernameTextBox.Text.Trim();
        var password = PasswordBox.Password;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
        {
            LoginStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E53935"));
            LoginStatusText.Text = "Unesi korisničko ime i lozinku.";
            return;
        }

        LoginStatusText.Text = "";
        if (sender is Button loginButton)
        {
            loginButton.IsEnabled = false;
        }

        try
        {
            var loginResponse = await _apiClient.LoginAsync(username, password);
            if (loginResponse.success)
            {
                LoginStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#43A047"));
                LoginStatusText.Text = "Prijava uspješna.";
                var sessionId = _apiClient.GetSessionId();
                if (string.IsNullOrWhiteSpace(sessionId))
                {
                    LoginStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E53935"));
                    LoginStatusText.Text = "Prijava je uspjela, ali session cookie nije spremljen.";
                    return;
                }
                await LoadMeAsync();
                await LoadRepresentationsAsync();
            }
            else
            {
                LoginStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E53935"));
                LoginStatusText.Text = loginResponse.message;
            }
        }
        catch (HttpRequestException)
        {
            LoginStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E53935"));
            LoginStatusText.Text = "Greška mreže. Provjeri vezu i pokušaj ponovo.";
        }
        catch (TaskCanceledException)
        {
            LoginStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E53935"));
            LoginStatusText.Text = "Zahtjev je istekao. Pokušaj ponovo.";
        }
        finally
        {
            if (sender is Button loginButtonRestore)
            {
                loginButtonRestore.IsEnabled = true;
            }
        }
    }

    private async Task LoadRepresentationsAsync()
    {
        try
        {
            ListStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E53935"));
            ListStatusText.Text = "Učitavanje...";

            var reasonsTask = _apiClient.GetRepresentationReasonsAsync();
            var representationsTask = _apiClient.GetRepresentationsAsync();
            await Task.WhenAll(reasonsTask, representationsTask);

            var reasons = reasonsTask.Result;
            var representations = representationsTask.Result;

            var reasonMap = reasons
                .Where(r => r.Name != null)
                .ToDictionary(r => r.Id, r => r.Name!);

            foreach (var representation in representations)
            {
                if (string.IsNullOrWhiteSpace(representation.ReasonName) &&
                    reasonMap.TryGetValue(representation.ReasonId, out var reasonName))
                {
                    representation.ReasonName = reasonName;
                }
            }

            var userIds = representations.Select(r => r.User).Distinct().ToList();
            var userMap = new Dictionary<int, ApiUser>();
            foreach (var userId in userIds)
            {
                try
                {
                    var user = await _apiClient.GetUserByIdAsync(userId);
                    if (user != null)
                    {
                        userMap[userId] = user;
                    }
                }
                catch
                {
                }
            }

            var views = representations.Select(r =>
            {
                var userName = userMap.TryGetValue(r.User, out var user)
                    ? string.Join(" ", new[] { user.FirstName, user.LastName }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim()
                    : null;
                if (string.IsNullOrWhiteSpace(userName))
                {
                    userName = userMap.TryGetValue(r.User, out user) ? user.Username : $"#{r.User}";
                }

                return new RepresentationView
                {
                    Id = r.Id,
                    OccurredAtDisplay = r.OccurredAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm"),
                    UserId = r.User,
                    UserName = userName,
                    ReasonName = r.ReasonName,
                    Note = r.Note,
                    ItemCount = r.Items?.Count ?? 0,
                    Source = r
                };
            }).ToList();

            RepresentationsListView.ItemsSource = views;
            LoginGrid.Visibility = Visibility.Collapsed;
            ListGrid.Visibility = Visibility.Visible;
            CreateGrid.Visibility = Visibility.Collapsed;
            ListStatusText.Text = "";
        }
        catch (HttpRequestException)
        {
            ListStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E53935"));
            ListStatusText.Text = "Ne mogu dohvatiti listu.";
        }
        catch (TaskCanceledException)
        {
            ListStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E53935"));
            ListStatusText.Text = "Učitavanje je isteklo.";
        }
    }

    private async void MainWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        if (_apiClient.LoadCookies())
        {
            try
            {
                var me = await _apiClient.GetMeAsync();
                if (me != null)
                {
                    var fullName = string.Join(" ", new[] { me.FirstName, me.LastName }
                        .Where(s => !string.IsNullOrWhiteSpace(s))).Trim();
                    UserInfoText.Text = string.IsNullOrWhiteSpace(fullName)
                        ? (string.IsNullOrWhiteSpace(me.Username) ? "Korisnik" : me.Username)
                        : fullName;
                    LoginGrid.Visibility = Visibility.Collapsed;
                    ListGrid.Visibility = Visibility.Visible;
                    CreateGrid.Visibility = Visibility.Collapsed;
                    await LoadRepresentationsAsync();
                    return;
                }
            }
            catch
            {
            }

            _apiClient.ClearCookies();
        }
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        _imageCache.Dispose();
    }

    private async Task LoadMeAsync()
    {
        try
        {
            var me = await _apiClient.GetMeAsync();
            if (me != null)
            {
                var fullName = string.Join(" ", new[] { me.FirstName, me.LastName }
                    .Where(s => !string.IsNullOrWhiteSpace(s))).Trim();
                var displayName = string.IsNullOrWhiteSpace(fullName)
                    ? (string.IsNullOrWhiteSpace(me.Username) ? "Korisnik" : me.Username)
                    : fullName;
                UserInfoText.Text = displayName;
            }
        }
        catch (HttpRequestException)
        {
        }
        catch (TaskCanceledException)
        {
        }
    }

    private async void OpenCreateRepresentation_Click(object sender, RoutedEventArgs e)
    {
        await LoadCreateScreenAsync();
    }

    private async Task LoadCreateScreenAsync()
    {
        CreateStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E53935"));
        CreateStatusText.Text = "Učitavanje...";

        try
        {
            var artikliTask = _apiClient.GetArtikliAsync();
            var reasonsTask = _apiClient.GetRepresentationReasonsAsync();
            var warehousesTask = _apiClient.GetWarehousesAsync();
            var categoriesTask = _apiClient.GetDrinkCategoriesAsync();
            await Task.WhenAll(artikliTask, reasonsTask, warehousesTask, categoriesTask);

            var categoryMap = categoriesTask.Result.ToDictionary(c => c.Id, c => c);
            var artikli = artikliTask.Result
                .Where(a => a.IsSellable)
                .Select(a => new ArtiklDisplay
                {
                    RmId = a.RmId,
                    Name = a.Name,
                    Image = !string.IsNullOrWhiteSpace(a.Image125x200)
                        ? a.Image125x200
                        : a.Image,
                    CategoryName = GetLevel2CategoryName(a.DrinkCategoryId, categoryMap),
                    CategoryId = GetLevel2CategoryId(a.DrinkCategoryId, categoryMap),
                    LeafCategoryName = a.DrinkCategoryName,
                    CategorySortOrder = GetLevel2CategorySortOrder(a.DrinkCategoryId, categoryMap),
                    LeafSortOrder = GetLeafCategorySortOrder(a.DrinkCategoryId, categoryMap)
                })
                .ToList();

            _allArtikli = artikli;
            var firstCategoryId = BuildCategoryCards(_allArtikli);
            ApplyCategoryFilter(firstCategoryId);

            _ = CacheImagesAsync(artikli);

            var warehouses = warehousesTask.Result.Where(w => !w.Hidden).ToList();
            WarehouseComboBox.ItemsSource = warehouses;
            if (WarehouseComboBox.SelectedIndex < 0 && WarehouseComboBox.Items.Count > 0)
            {
                WarehouseComboBox.SelectedIndex = 0;
            }

            ReasonComboBox.ItemsSource = reasonsTask.Result.Where(r => r.IsActive).ToList();
            if (ReasonComboBox.SelectedIndex < 0 && ReasonComboBox.Items.Count > 0)
            {
                ReasonComboBox.SelectedIndex = 0;
            }

            _cartItems.Clear();
            NoteTextBox.Text = string.Empty;

            ListGrid.Visibility = Visibility.Collapsed;
            CreateGrid.Visibility = Visibility.Visible;
            CreateStatusText.Text = "";
        }
        catch (HttpRequestException)
        {
            CreateStatusText.Text = "Ne mogu dohvatiti artikle.";
        }
        catch (TaskCanceledException)
        {
            CreateStatusText.Text = "Učitavanje je isteklo.";
        }
    }

    private static string? GetLevel2CategoryName(int? categoryId, Dictionary<int, DrinkCategory> categoryMap)
    {
        if (categoryId == null || !categoryMap.TryGetValue(categoryId.Value, out var current))
        {
            return null;
        }

        var chain = new List<DrinkCategory> { current };
        var guard = 0;
        while (current.ParentId != null && categoryMap.TryGetValue(current.ParentId.Value, out var parent))
        {
            chain.Add(parent);
            current = parent;
            guard++;
            if (guard > 20)
            {
                break;
            }
        }

        chain.Reverse(); // root -> leaf
        return chain.Count >= 2 ? chain[1].Name : chain[0].Name;
    }

    private static int? GetLevel2CategoryId(int? categoryId, Dictionary<int, DrinkCategory> categoryMap)
    {
        if (categoryId == null || !categoryMap.TryGetValue(categoryId.Value, out var current))
        {
            return null;
        }

        var chain = new List<DrinkCategory> { current };
        var guard = 0;
        while (current.ParentId != null && categoryMap.TryGetValue(current.ParentId.Value, out var parent))
        {
            chain.Add(parent);
            current = parent;
            guard++;
            if (guard > 20)
            {
                break;
            }
        }

        chain.Reverse();
        return chain.Count >= 2 ? chain[1].Id : chain[0].Id;
    }

    private static int GetLevel2CategorySortOrder(int? categoryId, Dictionary<int, DrinkCategory> categoryMap)
    {
        if (categoryId == null || !categoryMap.TryGetValue(categoryId.Value, out var current))
        {
            return int.MaxValue;
        }

        var chain = new List<DrinkCategory> { current };
        var guard = 0;
        while (current.ParentId != null && categoryMap.TryGetValue(current.ParentId.Value, out var parent))
        {
            chain.Add(parent);
            current = parent;
            guard++;
            if (guard > 20)
            {
                break;
            }
        }

        chain.Reverse();
        var level2 = chain.Count >= 2 ? chain[1] : chain[0];
        return level2.SortOrder;
    }

    private static int GetLeafCategorySortOrder(int? categoryId, Dictionary<int, DrinkCategory> categoryMap)
    {
        if (categoryId == null || !categoryMap.TryGetValue(categoryId.Value, out var current))
        {
            return int.MaxValue;
        }

        return current.SortOrder;
    }

    private async Task CacheImagesAsync(List<ArtiklDisplay> artikli)
    {
        foreach (var artikl in artikli)
        {
            if (string.IsNullOrWhiteSpace(artikl.Image))
            {
                continue;
            }

            var cachedPath = await _imageCache.GetOrDownloadAsync(artikl.Image);
            if (string.IsNullOrWhiteSpace(cachedPath))
            {
                continue;
            }

            Dispatcher.Invoke(() =>
            {
                artikl.Image = cachedPath;
            });
        }
    }

    private int? BuildCategoryCards(List<ArtiklDisplay> artikli)
    {
        var cards = artikli
            .Where(a => a.CategoryId != null && !string.IsNullOrWhiteSpace(a.CategoryName))
            .GroupBy(a => new { a.CategoryId, a.CategoryName })
            .Select(g => new CategoryCard
            {
                Id = g.Key.CategoryId,
                Name = g.Key.CategoryName ?? string.Empty,
                Count = g.Count(),
                SortOrder = g.Min(x => x.CategorySortOrder)
            })
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name, HrComparer)
            .ToList();

        _categoryCards.Clear();
        foreach (var card in cards)
        {
            _categoryCards.Add(card);
        }

        return cards.Count > 0 ? cards[0].Id : null;
    }

    private void ApplyCategoryFilter(int? categoryId)
    {
        _activeCategoryId = categoryId;
        var filtered = categoryId == null
            ? _allArtikli
                .OrderBy(a => a.CategorySortOrder)
                .ThenBy(a => a.CategoryName ?? string.Empty, HrComparer)
                .ThenBy(a => a.LeafSortOrder)
                .ThenBy(a => a.LeafCategoryName ?? string.Empty, HrComparer)
                .ThenBy(a => a.Name, HrComparer)
                .ToList()
            : _allArtikli
                .Where(a => a.CategoryId == categoryId)
                .OrderBy(a => a.LeafSortOrder)
                .ThenBy(a => a.LeafCategoryName ?? string.Empty, HrComparer)
                .ThenBy(a => a.Name, HrComparer)
                .ToList();

        _artikli.Clear();
        foreach (var item in filtered)
        {
            _artikli.Add(item);
        }
    }

    private void BackToList_Click(object sender, RoutedEventArgs e)
    {
        CreateGrid.Visibility = Visibility.Collapsed;
        ListGrid.Visibility = Visibility.Visible;
    }

    private void ClearCategoryFilter_Click(object sender, RoutedEventArgs e)
    {
        ApplyCategoryFilter(null);
    }

    private void CategoryFilter_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.DataContext is not CategoryCard card)
        {
            return;
        }

        ApplyCategoryFilter(card.Id);
    }

    private async void RepresentationsListView_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (RepresentationsListView.SelectedItem is not RepresentationView view)
        {
            return;
        }

        try
        {
            var details = await _apiClient.GetRepresentationByIdAsync(view.Id);
            if (details == null)
            {
                return;
            }

            try
            {
                var artikli = await _apiClient.GetArtikliAsync();
                var nameMap = artikli.ToDictionary(a => a.RmId, a => a.Name ?? $"#{a.RmId}");
                foreach (var item in details.Items)
                {
                    if (nameMap.TryGetValue(item.Artikl, out var name))
                    {
                        item.ArtiklName = name;
                    }
                    else
                    {
                        item.ArtiklName = $"#{item.Artikl}";
                    }
                }
            }
            catch
            {
            }

            var dialog = new RepresentationDetailsWindow(details)
            {
                Owner = this
            };
            dialog.ShowDialog();
        }
        catch
        {
        }
        finally
        {
            RepresentationsListView.SelectedItem = null;
        }
    }

    private async void Logout_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _apiClient.LogoutAsync();
        }
        catch
        {
        }

        _apiClient.ClearCookies();
        UserInfoText.Text = "";
        LoginGrid.Visibility = Visibility.Visible;
        ListGrid.Visibility = Visibility.Collapsed;
        CreateGrid.Visibility = Visibility.Collapsed;
        UsernameTextBox.Text = "";
        PasswordBox.Password = "";
    }

    private void AddArtikl_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.DataContext is not ArtiklDisplay artikl)
        {
            return;
        }

        var existing = _cartItems.FirstOrDefault(i => i.ArtiklId == artikl.RmId);
        if (existing != null)
        {
            existing.Quantity += 1;
            return;
        }

        _cartItems.Add(new RepresentationCartItem
        {
            ArtiklId = artikl.RmId,
            Name = artikl.Name,
            Image = artikl.Image,
            Quantity = 1,
            Price = 0
        });
    }

    private void CartDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CartDataGrid.SelectedItem is not RepresentationCartItem item)
        {
            return;
        }

        var dialog = new CartItemEditWindow(item)
        {
            Owner = this
        };
        dialog.ShowDialog();

        if (dialog.Removed)
        {
            _cartItems.Remove(item);
        }

        CartDataGrid.SelectedItem = null;
    }

    private async void SaveRepresentation_Click(object sender, RoutedEventArgs e)
    {
        if (WarehouseComboBox.SelectedValue is not int warehouseId)
        {
            CreateStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E53935"));
            CreateStatusText.Text = "Odaberi skladište.";
            return;
        }

        if (ReasonComboBox.SelectedValue is not int reasonId)
        {
            CreateStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E53935"));
            CreateStatusText.Text = "Odaberi razlog.";
            return;
        }

        if (_cartItems.Count == 0)
        {
            CreateStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E53935"));
            CreateStatusText.Text = "Dodaj barem jedan artikl.";
            return;
        }

        var request = new RepresentationCreateRequest
        {
            Warehouse = warehouseId,
            ReasonId = reasonId,
            Note = NoteTextBox.Text.Trim(),
            Items = _cartItems
                .Select(i => new RepresentationCreateItem
                {
                    Artikl = i.ArtiklId,
                    Quantity = i.Quantity,
                    Price = i.Price
                })
                .ToList()
        };

        CreateStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E53935"));
        CreateStatusText.Text = "Spremanje...";

        try
        {
            var result = await _apiClient.CreateRepresentationAsync(request);
            if (result.success)
            {
                CreateStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#43A047"));
                CreateStatusText.Text = "Spremljeno.";
                await LoadRepresentationsAsync();
            }
            else
            {
                CreateStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E53935"));
                CreateStatusText.Text = $"{result.message} (HTTP {result.statusCode})";

                var debugPath = System.IO.Path.Combine(AppContext.BaseDirectory, "last-create-representation.txt");
                var debugText =
                    "REQUEST JSON:\n" + result.requestBody + "\n\n" +
                    "RESPONSE BODY:\n" + result.responseBody + "\n";
                System.IO.File.WriteAllText(debugPath, debugText);
            }
        }
        catch (HttpRequestException)
        {
            CreateStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E53935"));
            CreateStatusText.Text = "Greška mreže.";
        }
        catch (TaskCanceledException)
        {
            CreateStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E53935"));
            CreateStatusText.Text = "Zahtjev je istekao.";
        }
    }
}
