using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using MétéoWither.Models;

namespace MétéoWither.Services;

public class WeatherService
{
    private static readonly HttpClient HttpClient = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly string _apiKey;

    public WeatherService(string apiKey)
    {
        _apiKey = apiKey;
    }

    public async Task<WeatherResponse> GetCurrentWeatherAsync(string city, string language)
    {
        var url = BuildUrl("weather", city, language);
        using var response = await HttpClient.GetAsync(url);
        return await DeserializeResponseAsync<WeatherResponse>(response, "Ville introuvable.");
    }

    public async Task<List<ForecastItem>> GetForecastAsync(string city, string language)
    {
        var url = BuildUrl("forecast", city, language);
        using var response = await HttpClient.GetAsync(url);
        var forecast = await DeserializeResponseAsync<ForecastResponse>(response, "Ville introuvable.");
        return forecast.List;
    }

    public async Task<Bitmap?> GetWeatherIconAsync(string? iconCode)
    {
        if (string.IsNullOrWhiteSpace(iconCode))
        {
            return null;
        }

        try
        {
            var bytes = await HttpClient.GetByteArrayAsync($"https://openweathermap.org/img/wn/{iconCode}@2x.png");
            using var stream = new MemoryStream(bytes);
            return new Bitmap(stream);
        }
        catch
        {
            return null;
        }
    }

    private string BuildUrl(string endpoint, string city, string language)
    {
        var escapedCity = Uri.EscapeDataString(city);
        return $"https://api.openweathermap.org/data/2.5/{endpoint}?q={escapedCity}&appid={_apiKey}&units=metric&lang={language}";
    }

    private static async Task<T> DeserializeResponseAsync<T>(HttpResponseMessage response, string notFoundMessage)
    {
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new InvalidOperationException(notFoundMessage);
        }

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<T>(json, JsonOptions);

        if (result is null)
        {
            throw new InvalidOperationException("La réponse météo est invalide.");
        }

        return result;
    }
}
