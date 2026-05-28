# MétéoWither

Application météo en C# .NET avec Avalonia et OpenWeatherMap.

## Fonctionnalités

- Recherche météo actuelle par ville.
- Affichage du nom de la ville, latitude, longitude, température en degrés Celsius, description, humidité et icône météo.
- Prévisions sur 5 jours, filtrées sur les prévisions de 12:00.
- Onglet paramètres avec ville par défaut et langue des requêtes.
- Chargement automatique de la ville par défaut au démarrage.
- Gestion des villes introuvables, de l'absence de connexion, des délais dépassés et d'une clé API invalide.
- Sauvegarde des options dans `options.json`.

## Prérequis

- .NET 10 SDK.
- Une clé API OpenWeatherMap.
- Les packages NuGet du projet, dont Avalonia et Newtonsoft.Json.

## Configuration

Créer un fichier `config.json` à la racine du projet avec le contenu suivant :

```json
{
  "ApiKey": "VOTRE_CLE_API_OPENWEATHERMAP"
}
```

`config.json` et `options.json` sont ignorés par Git pour éviter de publier la clé API et les préférences locales.

## Lancement

```bash
dotnet restore
dotnet run
```

Au premier lancement, si `options.json` n'existe pas, l'application le crée automatiquement avec les valeurs par défaut.
