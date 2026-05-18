using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using MétéoWither.Models;
using MétéoWither.Services;

namespace MétéoWither;

public partial class MainWindow : Window
{
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
        _options = await AppStorageService.LoadOptionsAsync();
        var config = await AppStorageService.LoadConfigAsync();

        if (!string.IsNullOrWhiteSpace(config?.ApiKey))
        {
            _weatherService = new WeatherService(config.ApiKey);
        }

        SearchCityTextBox.Text = _options.DefaultCity;
        ForecastCityTextBox.Text = _options.DefaultCity;
        DefaultCityTextBox.Text = _options.DefaultCity;
        LanguageComboBox.SelectedIndex = GetLanguageIndex(_options.Language);

        if (_weatherService is null)
        {
            const string message = "Ajoute une clé API valide dans config.json pour activer la météo.";
            SetStatus(SearchStatusTextBlock, message, true);
            SetStatus(ForecastStatusTextBlock, message, true);
            SetStatus(SettingsStatusTextBlock, message, true);
            return;
        }

        SetStatus(SettingsStatusTextBlock, "Options chargées.", false);
    }

    private async void OnSearchWeatherClick(object? sender, RoutedEventArgs e)
    {
        if (!TryGetWeatherService(out var weatherService))
        {
            return;
        }

        var city = SearchCityTextBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(city))
        {
            SetStatus(SearchStatusTextBlock, "Saisis une ville avant de lancer la recherche.", true);
            return;
        }

        SetStatus(SearchStatusTextBlock, "Recherche météo en cours...", false);

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
    }

    private async void OnSearchForecastClick(object? sender, RoutedEventArgs e)
    {
        if (!TryGetWeatherService(out var weatherService))
        {
            return;
        }

        var city = ForecastCityTextBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(city))
        {
            SetStatus(ForecastStatusTextBlock, "Saisis une ville avant de charger les prévisions.", true);
            return;
        }

        SetStatus(ForecastStatusTextBlock, "Chargement des prévisions en cours...", false);

        try
        {
            var forecastItems = await weatherService.GetForecastAsync(city, GetSelectedLanguageCode());
            var selectedItems = SelectForecastItems(forecastItems);

            if (selectedItems.Count == 0)
            {
                RenderForecastPlaceholder("Aucune prévision exploitable n'a été trouvée.");
                SetStatus(ForecastStatusTextBlock, "Les prévisions n'ont pas pu être affichées.", true);
                return;
            }

            var iconTasks = selectedItems
                .Select(item => weatherService.GetWeatherIconAsync(item.Weather.FirstOrDefault()?.Icon));
            var icons = await Task.WhenAll(iconTasks);

            RenderForecastCards(selectedItems, icons, GetSelectedLanguageCode());
            SetStatus(ForecastStatusTextBlock, $"Prévisions chargées pour {city}.", false);
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

            SetStatus(SettingsStatusTextBlock, "Options enregistrées.", false);
        }
        catch
        {
            SetStatus(SettingsStatusTextBlock, "Impossible d'enregistrer options.json.", true);
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
            .Where(entry => entry.Date.HasValue)
            .GroupBy(entry => entry.Date!.Value.Date)
            .Take(5)
            .Select(group => group
                .OrderBy(entry => Math.Abs((entry.Date!.Value.TimeOfDay - TimeSpan.FromHours(12)).TotalMinutes))
                .First()
                .Item)
            .ToList();
    }

    private void UpdateCurrentWeatherCard(WeatherResponse weather, Bitmap? icon)
    {
        var description = weather.Weather.FirstOrDefault()?.Description ?? "-";

        CurrentCityTextBlock.Text = weather.Name;
        CoordinatesTextBlock.Text = $"Latitude / Longitude : {weather.Coord.Lat:0.####} / {weather.Coord.Lon:0.####}";
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
        ForecastCardsPanel.Children.Clear();
        ForecastCardsPanel.Children.Add(new Border
        {
            Width = 280,
            Padding = new Thickness(16),
            CornerRadius = new CornerRadius(14),
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Color.Parse("#D7E3F4")),
            Background = new SolidColorBrush(Color.Parse("#F8FAFC")),
            Child = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap
            }
        });
    }

    private void RenderForecastCards(IReadOnlyList<ForecastItem> items, IReadOnlyList<Bitmap?> icons, string languageCode)
    {
        ForecastCardsPanel.Children.Clear();
        var culture = GetCulture(languageCode);

        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            var icon = index < icons.Count ? icons[index] : null;
            var date = ParseForecastDate(item.DateText);
            var description = item.Weather.FirstOrDefault()?.Description ?? "-";

            ForecastCardsPanel.Children.Add(new Border
            {
                Width = 190,
                Padding = new Thickness(16),
                CornerRadius = new CornerRadius(14),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.Parse("#D7E3F4")),
                Background = new SolidColorBrush(Color.Parse("#F8FAFC")),
                Child = new StackPanel
                {
                    Spacing = 8,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = date?.ToString("dddd dd/MM HH:mm", culture) ?? item.DateText,
                            FontWeight = FontWeight.SemiBold,
                            TextWrapping = TextWrapping.Wrap
                        },
                        new Border
                        {
                            Width = 72,
                            Height = 72,
                            CornerRadius = new CornerRadius(12),
                            Background = new SolidColorBrush(Color.Parse("#E2E8F0")),
                            HorizontalAlignment = HorizontalAlignment.Left,
                            Child = new Image
                            {
                                Margin = new Thickness(8),
                                Stretch = Stretch.Uniform,
                                Source = icon
                            }
                        },
                        new TextBlock
                        {
                            Text = $"Température : {item.Main.Temp:0.#} °C"
                        },
                        new TextBlock
                        {
                            Text = $"Description : {Capitalize(description)}",
                            TextWrapping = TextWrapping.Wrap
                        },
                        new TextBlock
                        {
                            Text = $"Humidité : {item.Main.Humidity}%"
                        }
                    }
                }
            });
        }
    }

    private static DateTime? ParseForecastDate(string? value)
    {
        if (DateTime.TryParse(value, out var date))
        {
            return date;
        }

        return null;
    }

    private static string Capitalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "-";
        }

        return char.ToUpper(value[0]) + value[1..];
    }

    private void SetStatus(TextBlock target, string message, bool isError)
    {
        target.Text = message;
        target.Foreground = isError ? Brushes.IndianRed : Brushes.ForestGreen;
    }

    private string GetSelectedLanguageCode()
    {
        return LanguageComboBox.SelectedIndex switch
        {
            1 => "en",
            2 => "es",
            3 => "it",
            4 => "de",
            _ => "fr"
        };
    }

    private static int GetLanguageIndex(string code)
    {
        return code switch
        {
            "en" => 1,
            "es" => 2,
            "it" => 3,
            "de" => 4,
            _ => 0
        };
    }

    private static CultureInfo GetCulture(string code)
    {
        return code switch
        {
            "en" => CultureInfo.GetCultureInfo("en-US"),
            "es" => CultureInfo.GetCultureInfo("es-ES"),
            "it" => CultureInfo.GetCultureInfo("it-IT"),
            "de" => CultureInfo.GetCultureInfo("de-DE"),
            _ => CultureInfo.GetCultureInfo("fr-FR")
        };
    }
}
