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

- `MauiTiltSource` liest den MAUI-Beschleunigungssensor. `TiltFilter` rechnet die natürlichen Geräteachsen für die aktuelle Bildschirmrotation um, glättet den normierten Beschleunigungsvektor und berechnet Pitch und Roll relativ zur ersten gültigen Messung. Die Umrechnung folgt der Android-Konvention für `Surface.ROTATION_*`: Bei 90° zeigt die natürliche +X-Achse zum oberen Bildschirmrand, die +Y-Achse nach links. Das betrifft vor allem Tablets mit natürlicher Querausrichtung, die im erzwungenen Hochformat 90° oder 270° melden. Bei einem Orientierungswechsel oder manueller Kalibrierung wird die Referenz verworfen. Ohne frische gültige Daten liefert der Adapter neutrale Eingabe; verworfene Messwerte halten keine alte Neigung aktiv. Die Halte-Taste steht immer zur Verfügung, sobald Tee im Becher ist.
- `DrinkGame` hält den Füllstand zwischen 0 und 1. `Full` bedeutet hier „serviert“: Nach einer Trinkpause kann der Becher auch nur noch teilweise gefüllt sein. Positiver Pitch startet das Trinken nach mindestens **40° für 0,35 s**. Unter **30°** endet der Trinkzustand, die Hysterese verhindert Flattern. Winkel beziehen sich auf die kalibrierte Position. Die Ausflussmenge hängt vom Winkel ab: `MinFlow` (45 %) an der Stoppschwelle, linear steigend bis 100 % bei `FullFlowDegrees` (65°); `DrainPerSecond` gilt für vollen Ausfluss. Schwellen, Dauer sowie Füll- und Trinkraten sind über `DrinkSettings` konfigurierbar.
- `PourStream` (in `BubbleTea.Game`) beschreibt den Ausgiessstrahl ohne Grafikbezug. `DrinkGame.Step` übergibt ihm genau den Ausfluss, der im selben Schritt den Pegel gesenkt hat: `IsPouring` ist also genau dann wahr, wenn der Pegel sinkt. `Head` und `Tail` sind Anteile des Falls vom Rand bis zum Tisch (`FallSeconds` = 0,45 s). Beim Zurückkippen, Kalibrieren, Pausieren oder Leertrinken endet der Ausfluss sofort; der bereits ausgetretene Rest löst sich vom Rand und fällt innerhalb von `FallSeconds` ab. `Trajectory` liefert die normierte Flugbahn, deren Reichweite und Breite mit dem Ausfluss und damit mit dem Neigungswinkel wachsen. Der Strahl verlässt den Becher über den Rand, an dem die Oberfläche höher steht; die Seite bleibt fest, solange der Strahl sichtbar ist.
- Der UI-Timer zielt auf 60 Hz. Zwei gedämpfte Federn steuern Seitenneigung und Tiefe der Flüssigkeitsoberfläche. Eine kleine periodische Welle, Farbverläufe, Reflexe und die gezeichnete Oberseite erzeugen räumliche Tiefe ohne Fluidsimulation. SkiaSharp beschneidet Flüssigkeit und dekorative Perlen auf den Becherpfad. Der Strahl wird erst nach Wiederherstellen dieses Clippings als einzelner Pfad mit Glanzlinie und kleinem Spritzer gezeichnet und kann so über den Rand hinaus fallen. Die Szene ist 420 Einheiten breit (Becher 340 plus Platz für den Strahl) und skaliert proportional zur Zeichenfläche.
- Die Seite zeichnet neu, solange Tee im Becher oder ein Strahl sichtbar ist, und steuert Ton nur bei Wechsel des Trinkzustands. Wenn das Fenster inaktiv wird, stoppt sie Timer und Sensor, vergisst die Haltezeit und gibt den Audioplayer frei. Der Becherinhalt bleibt während dieser Pause im Speicher.

## Hochformat

`MainActivity` fordert `ScreenOrientation.Portrait` an. Seit Android 16 ignoriert das System bei Apps mit `targetSdk` 36 auf Displays mit einer kleinsten Breite ab 600 dp jedoch `screenOrientation`, `setRequestedOrientation()` und Seitenverhältnis-Grenzen. Ausgenommen sind Spiele. Deshalb drehte sich die App auf Tablets trotz Attribut ins Querformat. Das Manifest deklariert die App mit `android:appCategory="game"` als Spiel und setzt zusätzlich die API-36-Ausnahme `PROPERTY_COMPAT_ALLOW_RESTRICTED_RESIZABILITY`. Diese Eigenschaft muss beim Wechsel auf `targetSdk` 37 entfernt werden, die Spielkategorie bleibt wirksam. Die CI prüft im gepackten Manifest der APK Hochformat, Spielkategorie und Eigenschaft. Ein Nutzer kann die Ausrichtung weiterhin über **Einstellungen › Apps › Seitenverhältnis** überschreiben.

## Wiederverwendung

`AppFactory.Physics` (gedämpfte Feder) und `AppFactory.Sensors` (Winkel, Orientierung, Glättung und Neutralposition) sind eigenständige Bibliotheken ohne Bubble-Tea-Abhängigkeiten. Der MAUI-Sensoradapter und der Android-Audioplayer sind Kandidaten für spätere plattformspezifische Bibliotheken. Im MVP bleiben sie in der App, ebenso SVG und synthetisch erzeugter WAV-Ton: Es gibt jeweils nur einen Verbraucher und der Build kann die Ressourcen direkt verpacken.

Zukünftige Rezepte, Mischregeln und Toppings gehören bei Bedarf in `BubbleTea.Game`; die Darstellung bleibt in der App. Die Trennung erlaubt es, Becherkonturen und Materialfarben zu variieren, ohne Regeln zu ändern. Es gibt noch keinen generischen Grafik-, Zutaten- oder Audio-Framework-Layer.
