# ROUNDS — Isaac Addon + Round Settings (наша сборка)

Здесь обе DLL, которые должны стоять у **всех** игроков одинаковыми:

| Файл | Куда положить |
|---|---|
| `base-mod/RoundsSettingsMod.dll` | `ROUNDS\BepInEx\plugins\RoundsSettingsMod\` |
| `RoundsIsaacAddon.dll` | `ROUNDS\BepInEx\plugins\RoundsIsaacAddon\` |

`base-mod` — основной мод «Round Settings» (оригинал: github.com/dimahmelkov08-crypto/rounds-settings-mod) с нашими правками: исправлена пропажа карт у второго игрока, повторы карт очень редкие, убраны карты Хмеля и Водички, понерфлены сильные карты.

## Установка
1. Поставьте BepInEx 5.4.23 (x64) в папку игры ROUNDS.
2. Скопируйте две DLL по таблице выше (папки создайте, если их нет).
3. Запустите игру через Steam. Меню — F8.

## Аддон (Isaac / S.T.A.L.K.E.R.)
76 карт с настоящими механиками, активка на Q (геймпад — Y). Выключить: удалить DLL аддона или `Enabled = false` в `BepInEx\config\com.dmah.roundssettings.isaac.cfg` (нужен перезапуск, у всех одинаково).
Сборка аддона: `bash build.sh`.
