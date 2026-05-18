Weather App 1
Weather App
Sommaire
Rendu
Le rendu doit se faire sur un répo Gitea. (-2 dans le cas contraire)
Le répo doit contenir :
Un README expliquant le fonctionnement de l’application ;
Votre code fonctionnel ;
Des commits bien rédigés ;
Prérequis
Voici quelques prérequis avant de commencer à travailler sur le TP :
Avoir une clé API pour openweathermap (tout se trouve sur le site) ;
⚠ La clé API doit être dans un fichier config qui ne doit pas être sur le
répo ! (-1 dans le cas contraire)
La clé API est unique et ne doit pas être partagée.
Avoir installé le package NuGet Newtonsoft.Json ;
Une fois votre projet initié, faites la commande dotnet add package
Newtonsoft.Json ;
Avoir Avalonia pour faire l’interface graphique ;
Sommaire
Rendu
Prérequis
Le projet
Liens utiles
Weather App 2
Le projet
Vous devez créer une application météo en C# .NET en utilisant Avalonia et l’API de
openweathermap.
L’application devra contenir 3 parties séparées par des onglets dans l’appli :
Une partie recherche d’une ville :
Le résultat de la recherche contiendra :
Le nom de la ville ;
La latitude et la longitude de la ville ;
La température en degré Celsius ;
Courte description du temps (exemple : ciel dégagé) ;
L’humidité ;
Une image représentative de la météo ;
Une partie de prévision sur une ville :
Il y aura 5 colonnes avec sur chaque colonne, les mêmes détails que sur
une recherche unique mais pour les 5 prochains jours ;
Les prévisions météorologiques devront être à 12:00 (exemple : le 23
septembre à 12:00, ciel dégagé) ;
La date de la prévision et l’heure devront être affiché ;
Une partie paramètre (changement de la langue / ville par défaut) :
Si une ville par défaut est enregistrée, il faudra automatiquement remplir les
champs au démarrage de l’application ;
Un paramètre de la langue des requêtes devra être proposé ;
Vous trouverez la liste des langues disponible assez facilement sur la
documentation du site ;
Tous les paramètres devront être enregistré dans un fichier options.json ;
⚠ Ce fichier ne doit pas apparaitre dans votre répo git !
Il devra être créer au lancement de l’application s’il n’existe pas.
Weather App 3
Bien évidemment, il faudra gérer les cas où la ville n’existe pas et qu’il n’y a pas de
connexion internet.
Liens utiles
OpenWeatherMap
https://openweathermap.org/api
https://openweathermap.org/guide
https://openweathermap.org/appid
NewtonSoft.Json
https://www.newtonsoft.com/json
https://www.newtonsoft.com/json/help/html/SerializingJSON.htm
Avalonia
https://docs.avaloniaui.net