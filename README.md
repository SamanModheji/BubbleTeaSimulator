# Bubble Tea Simulator

Ein Offline-Spiel für Android-Tablets (ab Android 6.0 / API 23). Kinder können einen virtuellen Bubble Tea einschenken und durch Neigen des Tablets trinken. Es gibt keine Konten, Werbung, In-App-Käufe oder externen Links im Spiel.

## Spielen

1. Das Tablet aufrecht halten und **Eingiessen** antippen.
2. Die obere Kante des Tablets zu dir kippen und kurz in dieser Position halten. Der Teepegel sinkt mit einem Schlürfgeräusch. Zurückkippen stoppt das Trinken.
3. Ohne Sensor: **Halten & trinken** gedrückt halten. Mit **◎** lässt sich die aufrechte Haltung neu kalibrieren.
4. Bei leerem Becher mit **Nochmal!** eine neue Runde starten.

## Build und Tests

Lokal mit .NET 10 LTS, JDK 17 und dem MAUI-Android-Workload bauen. Der Workflow **Android Debug APK** stellt nach erfolgreichem Build und Tests eine debug-signierte APK als Artefakt `bubble-tea-debug-apk` bereit.

- [Entwicklung, Build und Gerätecheck](docs/development.md)
- [Architektur und App-Factory-Bausteine](docs/architecture.md)
- [Roadmap und MVP-Grenzen](docs/roadmap.md)