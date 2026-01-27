using System;
using System.Linq;
using Character;
using HarmonyLib;
using BepInEx.Unity.IL2CPP;
using Fishbone;
using CoastalSmell;

namespace VarietyOfScales
{
    internal static partial class Hooks
    {
        [HarmonyPostfix, HarmonyWrapSafe]
        [HarmonyPatch(typeof(Human), nameof(Human.LoadAccessory))]
        static void LoadAccessoryPostfix(Human __instance) =>
            __instance.acs.ChangeAccessory(true);
        [HarmonyPostfix, HarmonyWrapSafe]
        [HarmonyPatch(typeof(HumanAccessory), nameof(HumanAccessory.ChangeAccessory), typeof(bool))]
        static void ChangeAccessoryPostfix(HumanAccessory __instance, bool forceChange) =>
            (__instance.human.data.Tag is "【SVChara】" || __instance.nowCoordinate.Tag is "【SVClothes】").Maybe(() =>
                Enumerable.Range(0, __instance.Accessories.Count)
                    .Where(slot => slot >= ChaFileDefine.AccessorySlotNum).ForEach(slot => __instance.Change(slot, forceChange)));
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
        internal static IDisposable[] Initialize(Plugin _) => [
            ..Extension.Register<CharaMods, CoordMods>(),
            Extension.OnLoadChara.Subscribe(human => CharaMods.Load(human, 20))
        ];
    }
    public partial class Plugin : BasePlugin
    {
        public const string Process = "DigitalCraft";
    }
}