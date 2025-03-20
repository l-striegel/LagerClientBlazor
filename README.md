# LagerClient Blazor - Artikelverwaltung
Eine moderne Blazor WebAssembly-Anwendung zur Verwaltung von Lagerartikeln mit Excel-ähnlicher Bearbeitung, Offline-Fähigkeit und reaktivem State-Management.
![.NET Version](https://img.shields.io/badge/.NET-8.0-purple)
![Status](https://img.shields.io/badge/Status-Stable-green)
![Version](https://img.shields.io/badge/Version-1.0.0-blue)
![Build Status](https://github.com/l-striegel/LagerClientBlazor/actions/workflows/ci.yml/badge.svg)
## Features
- **Excel-ähnliche Tabellendarstellung**: Inline-Bearbeitung mit Tastatur-Navigation
- **Zellformatierung**: Mehrfachauswahl von Zellen (STRG+Klick, SHIFT+Klick) zum Formatieren (Fett, Kursiv, Farbe)
- **Offline-Fähigkeit**: Vollständige Funktionalität auch ohne Backend-Verbindung
- **Synchronisierung**: Automatische Konfliktmeldung und -auflösung bei Änderungen an denselben Daten
- **Konfigurierbarkeit**: Anpassbare UI-Einstellungen (Zebrafarben, Zeilenhöhe, Debug-Modus)
- **Responsive Design**: Optimiert für Desktop und mobile Geräte
- **Import/Export**: Daten können als JSON exportiert und importiert werden
## Screenshots
![image](https://github.com/user-attachments/assets/f2a51122-a0fd-4311-b632-695a4208d634)
![image](https://github.com/user-attachments/assets/34a0f9ff-af9e-4518-b579-8f3a6b3648a3)
![image](https://github.com/user-attachments/assets/66e185c1-2946-4c61-9c8b-bffd4264b132)

## Installation
### Voraussetzungen
- .NET 8.0 SDK oder höher
- Ein moderner Webbrowser (Chrome, Firefox, Edge)
### Installation und Start
1. Repository klonen: `git clone https://github.com/l-striegel/LagerClientBlazor.git`
2. In das Projektverzeichnis wechseln: `cd LagerClientBlazor`
3. Anwendung starten: `dotnet run --project Client/Client.csproj`
4. Im Browser öffnen: http://localhost:5148
### Online-Demo
Eine live-Demo der Anwendung ist verfügbar unter: [https://l-striegel.github.io/LagerClientBlazor/](https://l-striegel.github.io/LagerClientBlazor/)
Dabei kann die article.json importiert werden um Demodaten zur Verfügung zu haben.
### Offline-Modus ohne Backend-API
Die Anwendung kann vollständig ohne Backend-API verwendet werden:
1. Die `articles.json` im Hauptverzeichnis enthält Beispieldaten
2. Die Daten werden automatisch in den localStorage des Browsers geladen
3. Alle Änderungen werden lokal gespeichert und können später synchronisiert werden
## Architektur
Die Anwendung ist nach einem Service-basierten Pattern mit reaktivem State-Management strukturiert:
- **Models**: Repräsentieren Lagerartikel und ihre Eigenschaften (Article, CellStyle)
- **Components**: Blazor-Komponenten für die UI (Index, Settings, CellFormattingToolbar)
- **Services**: State-Management und Business-Logik (AppStateService, OfflineArticleService)
## Technologie-Stack
- **.NET 8.0**: Moderne C#-Features und Performance
- **Blazor WebAssembly**: Client-seitiges .NET im Browser
- **Reactive Extensions (Rx.NET)**: Reaktives State-Management
- **Blazored Libraries**: Für lokalem Speicher, Toasts und Modale Dialoge
- **xUnit & bUnit**: Für automatisierte Tests
## Konfiguration
Die Anwendung wird über den Einstellungsbereich konfiguriert, der diese Optionen bietet:
| Einstellung | Beschreibung | Standardwert |
|-------------|--------------|--------------|
| API URL | URL der Backend-API | https://localhost:5001/api/article |
| Debug-Modus | Debug-Modus aktivieren | false |
| Tabellenzeilenhöhe | Zeilenhöhe der Tabelle | 25 |
| Tabellen-Zebrafarbe | Farbe für Zebrastreifen | #F0F0F0 |
## Entwicklung
### Voraussetzungen für die Entwicklung
- .NET 8.0 SDK oder höher
- Eine IDE (empfohlen: Visual Studio 2022, Visual Studio Code mit C# Extension)
### Setup-Anweisungen
1. Repository klonen: `git clone https://github.com/l-striegel/LagerClientBlazor.git`
2. Projekt in der IDE öffnen
3. Abhängigkeiten wiederherstellen: `dotnet restore`
4. Tests ausführen: `dotnet test`
5. Anwendung starten: `dotnet run --project Client/Client.csproj`
## Änderungshistorie
### v1.0.0
- Erste stabile Version mit vollständiger Offline-Unterstützung
- Excel-ähnliche Tabellendarstellung mit Inline-Bearbeitung
- Mehrfachauswahl von Zellen und Formatierungsoptionen
- Konfigurierbare UI-Einstellungen
- Import/Export von Daten als JSON
