# Entwicklung

## Toolchain

| Komponente | Version und Begründung |
|---|---|
| .NET SDK und Workload-Set | `10.0.404` in `global.json`. .NET 10 ist LTS und wird für App, Bibliotheken und Tests gemeinsam verwendet. Das Workload-Set bindet den .NET-Android- und MAUI-Build an kompatible Versionen. |
| Android | `net10.0-android36.0` mit SDK-Plattform und Build Tools 36.0.0, JDK 17. Minimale Geräteversion API 23. |
| Microsoft.Maui.Controls | `10.0.130`, stabiles MAUI-10-Update mit integrierter DI und plattformübergreifendem Sensorzugriff. |
| SkiaSharp.Views.Maui.Controls | `4.157.0`, stabile Version mit Assets für `net10.0-android36.0`. Bringt SkiaSharp und seine Android-Bibliotheken transitiv mit. |
| Tests | xUnit `2.9.3`, VS-Testadapter `4.0.1`, Microsoft.NET.Test.Sdk `18.12.0`. Diese Abhängigkeiten landen nicht in der APK. |

## Lokal bauen

Die folgenden Befehle im Repository-Stamm ausführen. Android SDK und JDK 17 installieren und `ANDROID_HOME` und `JAVA_HOME` setzen. Gegebenenfalls die Android-SDK-Lizenzen mit `sdkmanager --licenses` akzeptieren.

```sh
sdkmanager "platforms;android-36" "build-tools;36.0.0"
dotnet workload install maui-android
dotnet restore BubbleTeaSimulator.slnx
dotnet build BubbleTeaSimulator.slnx -c Debug --no-restore \
  -p:AndroidSdkDirectory="$ANDROID_HOME" -p:JavaSdkDirectory="$JAVA_HOME"
dotnet test tests/BubbleTea.Tests/BubbleTea.Tests.csproj -c Debug --no-build --no-restore
```

Nur für die hardwareunabhängigen Tests genügt das .NET SDK. Falls das in `global.json` fixierte Workload-Set noch nicht eingerichtet wurde, lässt sich die Workload-Auflösung für diesen Testlauf deaktivieren:

```sh
MSBuildEnableWorkloadResolver=false dotnet test tests/BubbleTea.Tests/BubbleTea.Tests.csproj
```

## APK installieren

Im erfolgreichen GitHub-Actions-Lauf unter **Artifacts** `bubble-tea-debug-apk` herunterladen und entpacken. Lokal liegt die gleiche, mit dem Debug-Schlüssel signierte APK unter `src/BubbleTea.App/bin/Debug/net10.0-android36.0/com.samanmodheji.bubbletea-Signed.apk`. Auf einem Tablet mit aktiviertem USB-Debugging installieren:

```sh
adb install -r src/BubbleTea.App/bin/Debug/net10.0-android36.0/com.samanmodheji.bubbletea-Signed.apk
```

Bei `INSTALL_FAILED_UPDATE_INCOMPATIBLE` wurde eine bestehende Installation wahrscheinlich mit einem anderen Debug-Schlüssel signiert. Vor dem Installieren die bestehende Test-App deinstallieren. Restore, Build, Tests und Signaturprüfung haben eigene CI-Schritte. Bei einem Fehler zuerst den betroffenen Schritt und bei SDK-Fehlern die Ausgabe von `dotnet --info`, `dotnet workload list` und `java -version` prüfen.

## Validierung

Der lokale Linux-Build hat die Solution ohne Buildwarnungen oder -fehler gebaut. Alle 14 Tests waren erfolgreich, darunter der Test für die Trinkrichtung vom Sensorvektor bis zum Pegel. `apksigner verify` hat die Debug-APK bestätigt; die Paketprüfung zeigte die mitgelieferte Audioressource, ARM64-/x64-Bibliotheken, einen optionalen Beschleunigungssensor und keine Internetberechtigung.

**Keine Gerätevalidierung:** In der Buildumgebung war kein Android-Gerät verbunden; der Download des Emulators und System-Images war nicht möglich. Vor einer Freigabe auf einem Tablet oder Emulator prüfen:

- Im Flugmodus starten und einschenken. Der Pegel steigt im transparenten Becher und bleibt beim seitlichen Neigen innerhalb der Kontur.
- Aufrecht neu kalibrieren. Kurzes Kippen, seitliches Neigen und Kippen in Gegenrichtung dürfen nicht trinken. Beim gehaltenen Kippen zu sich hin sinkt der Pegel; beim Zurückkippen stoppen Pegel und Ton.
- **Halten & trinken** testen, einschliesslich Loslassen vor Ablauf der Haltezeit. Vollständig austrinken und mit **Nochmal!** neu füllen.
- Während des Trinkens kurz die App verlassen: Der Ton und das Trinken sollen stoppen. Auf einem natürlich quer ausgerichteten Tablet zusätzlich die Sensorachsen prüfen.
