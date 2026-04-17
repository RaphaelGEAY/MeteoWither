namespace WeatherApp.Services
{
    public async Task<string> GetWeather(string city, string lang, string apiKey)
    {
        // On utilise l'interpolation de chaîne ($"...") pour injecter les variables
        string url = $"https://api.openweathermap.org/data/2.5/weather?q={city}&appid={apiKey}&units=metric&lang={lang}";
    
        // HttpClient va chercher le résultat
        var client = new HttpClient();
        var response = await client.GetStringAsync(url);
        return response; // C'est ici que Newtonsoft.Json interviendra pour lire le texte
    }
}
