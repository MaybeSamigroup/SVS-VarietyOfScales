using System;
using System.Reactive.Disposables;
using Character;
using HarmonyLib;
using BepInEx.Unity.IL2CPP;
using Fishbone;

namespace VarietyOfScales
{
    internal static partial class Hooks
    {
        [HarmonyPostfix, HarmonyWrapSafe]
        [HarmonyPatch(typeof(Human), nameof(Human.LoadAccessory))]
        static void LoadAccessoryPostfix(Human __instance) =>
            __instance.acs.ChangeAccessory(true);
    }
    static partial class AccessoryExtension
    {
        internal static int SlotCount => 0;
        static bool GetInfo(Human human, ChaListDefine.CategoryNo category, int id, ChaListDefine.KeyType key, out string value) =>
            GetInfo(human.data.Tag, category, id, key, out value);
        static bool GetInfo(string tag, ChaListDefine.CategoryNo category, int id, ChaListDefine.KeyType key, out string value) =>
            Human.lstCtrl.GetInfo(ref tag, category, id, key, out value);
        static void NotifySlotState(int slot) { }
        static void NotifySlotRemove(int slot) { }
        static void NotifySlotAssign(int slot, HumanDataAccessory.PartsInfo part) { }

        internal static IDisposable[] Initialize() => [
            ..Extension.Register<CharaMods, CoordMods>(),
            Extension.OnLoadChara.Subscribe(CharaMods.Load)
        ];
    }
    public partial class Plugin : BasePlugin
    {
        public const string Process = "DigitalCraft";
    }
}