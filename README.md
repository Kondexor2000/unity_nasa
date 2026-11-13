# NASA - Magnetic N-Body / Orbital Lab

Projekt demonstracyjny Unity przedstawiajacy wizualna symulacje orbitalna wielu cial. Laczy uproszczona grawitacje, stylizowane pole magnetyczne oraz perturbacje falowe; nie jest to model naukowy kosmologii.

## Wymagania

- Unity `2021.3.10f1`
- Pakiety projektu zdefiniowane w `Packages/manifest.json`

## Uruchomienie

Otworz katalog projektu w Unity Hub i uruchom scene `Assets/Scenes/SampleScene.unity`. Symulacja tworzy obiekty w czasie uruchomienia.

## Sterowanie

- `Spacja` - pauza/wznowienie
- `F` - pokazanie/ukrycie linii pola
- `W` - pokazanie/ukrycie fali
- `T` lub przycisk na ekranie - wlaczenie/wylaczenie sladow
- `R` - reset symulacji

## Architektura i testy

- `OrbitalSimulation` przechowuje stan cial i oblicza ich ruch niezaleznie od obiektow sceny.
- `CosmicFieldView` tworzy i aktualizuje obiekty wizualne oraz wspoldzieli materialy.
- `CosmicFieldControls` obsluguje wejscie i interfejs, a `CosmicFieldSimulation` koordynuje komponenty.
- Testy modelu znajduja sie w `Assets/Tests/EditMode`. Uruchom je w Unity Test Runner, wybierajac EditMode.

## Bezpieczenstwo i repozytorium

Repozytorium nie powinno zawierac wygenerowanych katalogow Unity, lokalnych ustawien ani sekretow. Reguly w `.gitignore` wykluczaja m.in. cache, ustawienia uzytkownika, pliki srodowiskowe oraz typowe klucze i certyfikaty. Nie umieszczaj kluczy API ani danych logowania w kodzie lub ustawieniach projektu.
