# Architektur

```text
BubbleTea.App (Android/MAUI, Eingabe, SkiaSharp, Audio)
├── BubbleTea.Game (.NET, Trinkregeln und Becherzustand)
│   └── AppFactory.Physics (.NET)
└── AppFactory.Sensors (.NET)

BubbleTea.Tests → BubbleTea.Game, AppFactory.Physics und AppFactory.Sensors
```

`MauiProgram` verbindet Einstellungen, Spiellogik, Sensorquelle, Audio und Seite per Dependency Injection. Jede Spielseite erhält eigene Zustandsinstanzen. Das Spielmodell kennt weder MAUI noch SkiaSharp oder Android. Tests liefern Eingabewinkel und Zeitschritte direkt; sie benötigen deshalb keine Plattform-Mocks oder zusätzlichen Interfaces.

## Vom Sensor zum Becher

- `MauiTiltSource` liest den MAUI-Beschleunigungssensor. `TiltFilter` rechnet die natürlichen Geräteachsen für die aktuelle Bildschirmrotation um, glättet den normierten Schwerkraftvektor und berechnet Pitch und Roll relativ zur ersten gültigen Messung. Bei einem Orientierungswechsel oder manueller Kalibrierung wird die Referenz verworfen. Ohne frische Daten liefert der Adapter neutrale Eingabe; die Halte-Taste steht immer zur Verfügung, sobald Tee im Becher ist.
- `DrinkGame` hält den Füllstand zwischen 0 und 1. `Full` bedeutet hier „serviert“: Nach einer Trinkpause kann der Becher auch nur noch teilweise gefüllt sein. Positiver Pitch startet das Trinken nach mindestens **40° für 0,35 s**. Unter **30°** endet der Trinkzustand, die Hysterese verhindert Flattern. Winkel beziehen sich auf die kalibrierte Position. Schwellen, Dauer sowie Füll- und Trinkraten sind über `DrinkSettings` konfigurierbar.
- Der UI-Timer zielt auf 60 Hz. Zwei gedämpfte Federn steuern Seitenneigung und Tiefe der Flüssigkeitsoberfläche. Eine kleine periodische Welle, Farbverläufe, Reflexe und die gezeichnete Oberseite erzeugen räumliche Tiefe ohne Fluidsimulation. SkiaSharp beschneidet Flüssigkeit und dekorative Perlen auf den Becherpfad; die Szene skaliert proportional zur Zeichenfläche.
- Die Seite steuert Ton nur bei Wechsel des Trinkzustands. Wenn das Fenster inaktiv wird, stoppt sie Timer und Sensor, vergisst die Haltezeit und gibt den Audioplayer frei. Der Becherinhalt bleibt während dieser Pause im Speicher.

## Wiederverwendung

`AppFactory.Physics` (gedämpfte Feder) und `AppFactory.Sensors` (Winkel, Orientierung, Glättung und Neutralposition) sind eigenständige Bibliotheken ohne Bubble-Tea-Abhängigkeiten. Der MAUI-Sensoradapter und der Android-Audioplayer sind Kandidaten für spätere plattformspezifische Bibliotheken. Im MVP bleiben sie in der App, ebenso SVG und synthetisch erzeugter WAV-Ton: Es gibt jeweils nur einen Verbraucher und der Build kann die Ressourcen direkt verpacken.

Zukünftige Rezepte, Mischregeln und Toppings gehören bei Bedarf in `BubbleTea.Game`; die Darstellung bleibt in der App. Die Trennung erlaubt es, Becherkonturen und Materialfarben zu variieren, ohne Regeln zu ändern. Es gibt noch keinen generischen Grafik-, Zutaten- oder Audio-Framework-Layer.
