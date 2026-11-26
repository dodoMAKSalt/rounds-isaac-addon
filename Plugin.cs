using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

// Аддон к «ROUNDS — Round Settings»: карты The Binding of Isaac и S.T.A.L.K.E.R. с настоящими механиками.
// Отдельная DLL: удалите её (или поставьте Enabled = false в конфиге) — игра вернётся к картам основного мода.
namespace RoundsSettingsMod
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency("com.dmah.roundssettings", BepInDependency.DependencyFlags.HardDependency)]
    public class IsaacAddonPlugin : BaseUnityPlugin
    {
        public const string GUID = "com.dmah.roundssettings.isaac", NAME = "Rounds Isaac Addon", VERSION = "1.0.0";
        public static ConfigEntry<bool> Enabled;
        public static ConfigEntry<KeyCode> ActiveKey;
        public static bool On { get { return Enabled != null && Enabled.Value && RoundsSettingsPlugin.CustomCards.Value; } }

        void Awake()
        {
            Enabled = Config.Bind("General", "Enabled", true, "Включить аддон (карты Isaac и S.T.A.L.K.E.R.). Нужен перезапуск игры; у всех игроков в мультиплеере одинаково");
            ActiveKey = Config.Bind("General", "ActiveKey", KeyCode.Q, "Клавиша активной способности карт Isaac (на геймпаде — кнопка Y / треугольник)");
            if (!Enabled.Value) { Logger.LogInfo(NAME + ": выключен в конфиге"); return; }
            AddonCards.Add();
            var h = new Harmony(GUID); h.PatchAll(typeof(IsaacPatches));
            gameObject.AddComponent<IsaacRunner>();
            Logger.LogInfo(NAME + " " + VERSION + ": добавлено карт " + CardCatalog.All.Count + " (всего в каталоге)");
        }
    }
}
