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
    }
    public partial class Plugin : BasePlugin
    {
        public const string Process = "DigitalCraft";
        public override void Load()
        {
            Instance = this;
            Patch = Harmony.CreateAndPatchAll(typeof(Hooks), $"{Name}.Hooks");
            Extension.Register<CharaMods, CoordMods>();
            Extension.OnLoadChara += CharaMods.Load;
        }
    }

}