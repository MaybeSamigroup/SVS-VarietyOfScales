using HarmonyLib;
using BepInEx.Unity.IL2CPP;
using System;
using System.IO.Compression;
using Character;
using Fishbone;
using CoastalSmell;
using CoordLimit = Character.HumanDataCoordinate.LoadLimited.Flags;

namespace VarietyOfScales
{
    static partial class AccessoryExtensions
    {
        internal static int ExtensionSlots => 0;
        static void OnPostCoordinateDeserialize(Human human, HumanDataCoordinate _, CoordLimit limit, ZipArchive archive, ZipArchive storage) =>
            ((limit & CoordLimit.Accessory) != 0).Maybe(F.Apply(CoordMods.Apply, archive, human));
        static void OnPostCharacterDeserialize(Human human, ZipArchive archive) =>
            CharaMods.Load(archive).Apply(human, human.data.Status.coordinateType);
        static void OnPostCoordinateReload(Human human, int type, ZipArchive archive) =>
            CharaMods.Load(archive).Apply(human, type);
        static bool GetInfo(Human human, ChaListDefine.CategoryNo category, int id, ChaListDefine.KeyType key, out string value) =>
            GetInfo(human.data.Tag, category, id, key, out value);
        static bool GetInfo(string tag, ChaListDefine.CategoryNo category, int id, ChaListDefine.KeyType key, out string value) =>
            Human.lstCtrl.GetInfo(ref tag, category, id, key, out value);
        internal static void Initialize()
        {
            Event.OnPostCoordinateDeserialize += OnPostCoordinateDeserialize;
            Event.OnPostCharacterDeserialize += OnPostCharacterDeserialize;
            Event.OnPostCoordinateReload += OnPostCoordinateReload;
        }
    }
    public partial class Plugin : BasePlugin
    {
        public const string Process = "DigitalCraft";
        public override void Load() =>
            (Instance, Patch) = (this, Harmony.CreateAndPatchAll(typeof(Hooks), $"{Name}.Hooks"))
                .With(AccessoryExtensions.Initialize);
    }

}