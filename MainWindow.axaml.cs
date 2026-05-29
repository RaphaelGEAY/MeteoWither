using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using MétéoWither.Models;
using MétéoWither.Services;

namespace MétéoWither;

public partial class MainWindow : Window
{
    private static readonly string[] LanguageCodes = ["fr", "en", "es", "it", "de", "pt", "nl", "pl"];

    private static readonly Dictionary<string, string> LanguageLabels = new()
    {
        ["fr"] = "Français",
        ["en"] = "English",
        ["es"] = "Español",
        ["it"] = "Italiano",
        ["de"] = "Deutsch",
        ["pt"] = "Português",
        ["nl"] = "Nederlands",
        ["pl"] = "Polski"
    };

    private AppOptions _options = new();
    private WeatherService? _weatherService;
    private bool _isInitialized;

    public MainWindow()
    {
        InitializeComponent();
        ResetCurrentWeatherCard();
        RenderForecastPlaceholder("Les 5 cartes de prévision apparaîtront ici.");
        Opened += OnOpened;
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
        await InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        OptionsPathTextBlock.Text = $"Options : {AppStorageService.OptionsPath}";
        ConfigPathTextBlock.Text = $"Config : {AppStorageService.ConfigPath}";

        _options = await AppStorageService.LoadOptionsAsync();
        _options.Language = NormalizeLanguageCode(_options.Language);

        SearchCityTextBox.Text = _options.DefaultCity;
        ForecastCityTextBox.Text = _options.DefaultCity;
        DefaultCityTextBox.Text = _options.DefaultCity;
        LanguageComboBox.SelectedIndex = GetLanguageIndex(_options.Language);
        UpdateHeaderSummary();

        var config = await AppStorageService.LoadConfigAsync();
        if (!string.IsNullOrWhiteSpace(config?.ApiKey))
        {
            _weatherService = new WeatherService(config.ApiKey.Trim());
            SetStatus(ApiKeyStatusTextBlock, "Clé API : chargée.", false);
        }
        else
        {
            const string message = "Ajoute une clé API OpenWeatherMap valide dans config.json pour activer les recherches.";
            SearchButton.IsEnabled = false;
            ForecastButton.IsEnabled = false;
            SetStatus(ApiKeyStatusTextBlock, "Clé API : absente.", true);
            SetStatus(SearchStatusTextBlock, message, true);
            SetStatus(ForecastStatusTextBlock, message, true);
            SetStatus(SettingsStatusTextBlock, message, true);
            return;
        }

        SetStatus(SettingsStatusTextBlock, "Options chargées.", false);

        if (!string.IsNullOrWhiteSpace(_options.DefaultCity))
        {
            SetStatus(SearchStatusTextBlock, "Chargement de la ville par défaut...", false);
            await LoadCurrentWeatherAsync(_options.DefaultCity, true);
            await LoadForecastAsync(_options.DefaultCity, true);
        }
        else
        {
            SetStatus(SearchStatusTextBlock, "Saisis une ville pour afficher la météo actuelle.", false);
            SetStatus(ForecastStatusTextBlock, "Saisis une ville pour afficher les prévisions à 12:00.", false);
        }
    }

    private async void OnSearchWeatherClick(object? sender, RoutedEventArgs e)
    {
        await LoadCurrentWeatherAsync(SearchCityTextBox.Text?.Trim() ?? string.Empty, false);
    }

    private async void OnSearchForecastClick(object? sender, RoutedEventArgs e)
    {
        await LoadForecastAsync(ForecastCityTextBox.Text?.Trim() ?? string.Empty, false);
    }

    private async void OnSearchCityKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        e.Handled = true;
        await LoadCurrentWeatherAsync(SearchCityTextBox.Text?.Trim() ?? string.Empty, false);
    }

    private async void OnForecastCityKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        e.Handled = true;
        await LoadForecastAsync(ForecastCityTextBox.Text?.Trim() ?? string.Empty, false);
    }

    private async void OnSaveOptionsClick(object? sender, RoutedEventArgs e)
    {
        _options.DefaultCity = DefaultCityTextBox.Text?.Trim() ?? string.Empty;
        _options.Language = GetSelectedLanguageCode();

        try
        {
            await AppStorageService.SaveOptionsAsync(_options);

            SearchCityTextBox.Text = _options.DefaultCity;
            ForecastCityTextBox.Text = _options.DefaultCity;
            UpdateHeaderSummary();

            SetStatus(SettingsStatusTextBlock, "Options enregistrées dans options.json.", false);
        }
        catch
        {
            SetStatus(SettingsStatusTextBlock, "Impossible d'enregistrer options.json.", true);
        }
    }

    private async Task LoadCurrentWeatherAsync(string city, bool fromDefaultCity)
    {
        if (!TryGetWeatherService(out var weatherService))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(city))
        {
            SetStatus(SearchStatusTextBlock, "Saisis une ville avant de lancer la recherche.", true);
            return;
        }

        SetSearchBusy(true);
        SetStatus(SearchStatusTextBlock, fromDefaultCity ? "Chargement automatique de la météo..." : "Recherche météo en cours...", false);

        try
        {
            var weather = await weatherService.GetCurrentWeatherAsync(city, GetSelectedLanguageCode());
            var icon = await weatherService.GetWeatherIconAsync(weather.Weather.FirstOrDefault()?.Icon);

            UpdateCurrentWeatherCard(weather, icon);
            SetStatus(SearchStatusTextBlock, $"Météo chargée pour {weather.Name}.", false);
        }
        catch (InvalidOperationException ex)
        {
            ResetCurrentWeatherCard();
            SetStatus(SearchStatusTextBlock, ex.Message, true);
        }
        catch (HttpRequestException)
        {
            ResetCurrentWeatherCard();
            SetStatus(SearchStatusTextBlock, "Connexion internet indisponible ou service météo inaccessible.", true);
        }
        catch (TaskCanceledException)
        {
            ResetCurrentWeatherCard();
            SetStatus(SearchStatusTextBlock, "La requête météo a expiré. Vérifie la connexion internet.", true);
        }
        finally
        {
            SetSearchBusy(false);
        }
    }

    private async Task LoadForecastAsync(string city, bool fromDefaultCity)
    {
        if (!TryGetWeatherService(out var weatherService))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(city))
        {
            SetStatus(ForecastStatusTextBlock, "Saisis une ville avant de charger les prévisions.", true);
            return;
        }

        SetForecastBusy(true);
        SetStatus(ForecastStatusTextBlock, fromDefaultCity ? "Chargement automatique des prévisions..." : "Chargement des prévisions en cours...", false);

        try
        {
            var forecast = await weatherService.GetForecastAsync(city, GetSelectedLanguageCode());
            var selectedItems = SelectForecastItems(forecast.List);

            if (selectedItems.Count == 0)
            {
                RenderForecastPlaceholder("Aucune prévision à 12:00 n'a été trouvée.");
                SetStatus(ForecastStatusTextBlock, "Les prévisions n'ont pas pu être affichées.", true);
                return;
            }

            var iconTasks = selectedItems
                .Select(item => weatherService.GetWeatherIconAsync(item.Weather.FirstOrDefault()?.Icon));
            var icons = await Task.WhenAll(iconTasks);

            RenderForecastCards(forecast, selectedItems, icons, GetSelectedLanguageCode());

            var suffix = selectedItems.Count == 5 ? string.Empty : $" ({selectedItems.Count}/5 disponibles)";
            SetStatus(ForecastStatusTextBlock, $"Prévisions chargées pour {GetForecastCityName(forecast)}{suffix}.", false);
        }
        catch (InvalidOperationException ex)
        {
            RenderForecastPlaceholder("Les 5 cartes de prévision apparaîtront ici.");
            SetStatus(ForecastStatusTextBlock, ex.Message, true);
        }
        catch (HttpRequestException)
        {
            RenderForecastPlaceholder("Les 5 cartes de prévision apparaîtront ici.");
            SetStatus(ForecastStatusTextBlock, "Connexion internet indisponible ou service météo inaccessible.", true);
        }
        catch (TaskCanceledException)
        {
            RenderForecastPlaceholder("Les 5 cartes de prévision apparaîtront ici.");
            SetStatus(ForecastStatusTextBlock, "La requête de prévisions a expiré. Vérifie la connexion internet.", true);
        }
        finally
        {
            SetForecastBusy(false);
        }
    }

    private bool TryGetWeatherService(out WeatherService weatherService)
    {
        if (_weatherService is not null)
        {
            weatherService = _weatherService;
            return true;
        }

        weatherService = null!;
        const string message = "La clé API est absente ou invalide dans config.json.";
        SetStatus(SearchStatusTextBlock, message, true);
        SetStatus(ForecastStatusTextBlock, message, true);
        SetStatus(ApiKeyStatusTextBlock, "Clé API : absente.", true);
        return false;
    }

    private static List<ForecastItem> SelectForecastItems(IEnumerable<ForecastItem> items)
    {
        return items
            .Select(item => new
            {
                Item = item,
                Date = ParseForecastDate(item.DateText)
            })
            .Where(entry => entry.Date.HasValue && entry.Date.Value.TimeOfDay == TimeSpan.FromHours(12))
            .OrderBy(entry => entry.Date!.Value)
            .GroupBy(entry => entry.Date!.Value.Date)
            .Take(5)
            .Select(group => group.First().Item)
            .ToList();
    }

    private void UpdateCurrentWeatherCard(WeatherResponse weather, Bitmap? icon)
    {
        var description = weather.Weather.FirstOrDefault()?.Description ?? "-";

        CurrentCityTextBlock.Text = weather.Name;
        CoordinatesTextBlock.Text = $"Latitude / Longitude : {FormatCoordinates(weather.Coord)}";
        TemperatureTextBlock.Text = $"Température : {weather.Main.Temp:0.#} °C";
        DescriptionTextBlock.Text = $"Description : {Capitalize(description)}";
        HumidityTextBlock.Text = $"Humidité : {weather.Main.Humidity}%";
        CurrentWeatherImage.Source = icon;
    }

    private void ResetCurrentWeatherCard()
    {
        CurrentCityTextBlock.Text = "Aucune recherche";
        CoordinatesTextBlock.Text = "Latitude / Longitude : -";
        TemperatureTextBlock.Text = "Température : -";
        DescriptionTextBlock.Text = "Description : -";
        HumidityTextBlock.Text = "Humidité : -";
        CurrentWeatherImage.Source = null;
    }

    private void RenderForecastPlaceholder(string message)
    {
        ForecastCitySummaryTextBlock.Text = "Les prévisions apparaîtront ici.";
        ForecastCardsGrid.Children.Clear();

        var placeholder = new Border
        {
            MinHeight = 170,
            Padding = new Thickness(20),
            CornerRadius = new CornerRadius(22),
            BorderThickness = new Thickness(1),
            BorderBrush = Solid("#AFC8D1"),
            Background = Solid("#FFFFFF"),
            Child = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Solid("#2D4A53")
            }
        };

        Grid.SetColumnSpan(placeholder, 5);
        ForecastCardsGrid.Children.Add(placeholder);
    }

    private void RenderForecastCards(
        ForecastResponse forecast,
        IReadOnlyList<ForecastItem> items,
        IReadOnlyList<Bitmap?> icons,
        string languageCode)
    {
        ForecastCardsGrid.Children.Clear();
        var culture = GetCulture(languageCode);
        var cityName = GetForecastCityName(forecast);
        var coordinates = FormatCoordinates(forecast.City.Coord);

        ForecastCitySummaryTextBlock.Text = $"{cityName} - {coordinates} - prévisions à 12:00";

        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            var icon = index < icons.Count ? icons[index] : null;
            var date = ParseForecastDate(item.DateText);
            var description = item.Weather.FirstOrDefault()?.Description ?? "-";

            var card = new Border
            {
                MinHeight = 285,
                Padding = new Thickness(16),
                CornerRadius = new CornerRadius(20),
                BorderThickness = new Thickness(1),
                BorderBrush = Solid("#AFC8D1"),
                Background = Solid(index % 2 == 0 ? "#FFFFFF" : "#F5FBF4"),
                Child = new StackPanel
                {
                    Spacing = 8,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = date?.ToString("dddd dd/MM HH:mm", culture) ?? item.DateText,
                            FontWeight = FontWeight.SemiBold,
                            FontSize = 16,
                            Foreground = Solid("#102A33"),
                            TextWrapping = TextWrapping.Wrap
                        },
                        new TextBlock
                        {
                            Text = cityName,
                            Foreground = Solid("#2D4A53"),
                            TextWrapping = TextWrapping.Wrap
                        },
                        new Border
                        {
                            Width = 86,
                            Height = 86,
                            CornerRadius = new CornerRadius(18),
                            Background = Solid("#DDEBFF"),
                            BorderBrush = Solid("#9FBDE4"),
                            BorderThickness = new Thickness(1),
                            Child = new Image
                            {
                                Margin = new Thickness(10),
                                Stretch = Stretch.Uniform,
                                Source = icon
                            }
                        },
                        new TextBlock
                        {
                            Text = $"Latitude / Longitude : {coordinates}",
                            Foreground = Solid("#2D4A53"),
                            TextWrapping = TextWrapping.Wrap
                        },
                        new TextBlock
                        {
                            Text = $"Température : {item.Main.Temp:0.#} °C",
                            FontWeight = FontWeight.SemiBold,
                            Foreground = Solid("#0A5667")
                        },
                        new TextBlock
                        {
                            Text = $"Description : {Capitalize(description)}",
                            Foreground = Solid("#1F3A43"),
                            TextWrapping = TextWrapping.Wrap
                        },
                        new TextBlock
                        {
                            Text = $"Humidité : {item.Main.Humidity}%",
                            FontWeight = FontWeight.SemiBold,
                            Foreground = Solid("#3E5C25")
                        }
                    }
                }
            };

            Grid.SetColumn(card, index);
            ForecastCardsGrid.Children.Add(card);
        }
    }

    private static DateTime? ParseForecastDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateTime.TryParseExact(
                value,
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var exactDate))
        {
            return exactDate;
        }

        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;
    }

    private static string Capitalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "-";
        }

        return char.ToUpper(value[0], CultureInfo.CurrentCulture) + value[1..];
    }

    private void SetSearchBusy(bool isBusy)
    {
        SearchButton.IsEnabled = !isBusy;
        SearchProgressBar.IsVisible = isBusy;
    }

    private void SetForecastBusy(bool isBusy)
    {
        ForecastButton.IsEnabled = !isBusy;
        ForecastProgressBar.IsVisible = isBusy;
    }

    private static void SetStatus(TextBlock target, string message, bool isError)
    {
        target.Text = message;
        target.Foreground = isError ? Solid("#9F2E2E") : Solid("#1F6B45");
    }

    private void UpdateHeaderSummary()
    {
        var city = string.IsNullOrWhiteSpace(_options.DefaultCity)
            ? "Aucune ville par défaut"
            : $"Ville : {_options.DefaultCity}";
        var language = LanguageLabels.GetValueOrDefault(NormalizeLanguageCode(_options.Language), "Français");

        HeaderSummaryTextBlock.Text = $"{city} | Langue : {language}";
    }

    private string GetSelectedLanguageCode()
    {
        var index = LanguageComboBox.SelectedIndex;
        if (index >= 0 && index < LanguageCodes.Length)
        {
            return LanguageCodes[index];
        }

        return "fr";
    }

    private static int GetLanguageIndex(string code)
    {
        var normalized = NormalizeLanguageCode(code);
        var index = Array.IndexOf(LanguageCodes, normalized);
        return index >= 0 ? index : 0;
    }

    private static string NormalizeLanguageCode(string? code)
    {
        return !string.IsNullOrWhiteSpace(code) && LanguageCodes.Contains(code) ? code : "fr";
    }

    private static CultureInfo GetCulture(string code)
    {
        return NormalizeLanguageCode(code) switch
        {
            "en" => CultureInfo.GetCultureInfo("en-US"),
            "es" => CultureInfo.GetCultureInfo("es-ES"),
            "it" => CultureInfo.GetCultureInfo("it-IT"),
            "de" => CultureInfo.GetCultureInfo("de-DE"),
            "pt" => CultureInfo.GetCultureInfo("pt-PT"),
            "nl" => CultureInfo.GetCultureInfo("nl-NL"),
            "pl" => CultureInfo.GetCultureInfo("pl-PL"),
            _ => CultureInfo.GetCultureInfo("fr-FR")
        };
    }

    private static string FormatCoordinates(Coord coord)
    {
        return $"{coord.Lat:0.####} / {coord.Lon:0.####}";
    }

    private static string GetForecastCityName(ForecastResponse forecast)
    {
        return string.IsNullOrWhiteSpace(forecast.City.Name) ? "Ville inconnue" : forecast.City.Name;
    }

    private static SolidColorBrush Solid(string color)
    {
        return new SolidColorBrush(Color.Parse(color));
    }
}
