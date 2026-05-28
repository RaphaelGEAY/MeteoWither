using System.Collections.Generic;
using Newtonsoft.Json;

namespace MétéoWither.Models;

public class WeatherResponse
{
    public string Name { get; set; } = string.Empty;

    public Coord Coord { get; set; } = new();

    public MainData Main { get; set; } = new();

    public List<WeatherCondition> Weather { get; set; } = [];
}

public class ForecastResponse
{
    public List<ForecastItem> List { get; set; } = [];

    public ForecastCity City { get; set; } = new();
}

public class ForecastItem
{
    public MainData Main { get; set; } = new();

    public List<WeatherCondition> Weather { get; set; } = [];

    [JsonProperty("dt_txt")]
    public string DateText { get; set; } = string.Empty;
}

public class ForecastCity
{
    public string Name { get; set; } = string.Empty;

    public Coord Coord { get; set; } = new();
}

public class Coord
{
    public double Lat { get; set; }

    public double Lon { get; set; }
}

public class MainData
{
    public double Temp { get; set; }

    public int Humidity { get; set; }
}

public class WeatherCondition
{
    public string Description { get; set; } = string.Empty;

    public string Icon { get; set; } = string.Empty;
}
