namespace WeatherApp.Models
{
    // Cette classe correspond à l'objet JSON principal renvoyé par l'API
    public class WeatherResponse
    {
        public string Name { get; set; }        // Nom de la ville
        public Coord Coord { get; set; }       // Latitude et Longitude
        public MainData Main { get; set; }     // Température et Humidité
        public Weather[] Weather { get; set; } // Description et Icône
    }

    public class Coord {
        public double Lat { get; set; }
        public double Lon { get; set; }
    }

    public class MainData {
        public double Temp { get; set; }
        public int Humidity { get; set; }
    }

    public class Weather {
        public string Description { get; set; }
        public string Icon { get; set; }
    }
}
