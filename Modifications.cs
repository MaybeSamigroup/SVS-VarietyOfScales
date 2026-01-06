using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Character;
using Fishbone;
using CoastalSmell;
using AcsNode = Character.HumanAccessory;
using AcsLeaf = Character.HumanAccessory.Accessory;
using AcsData = Character.HumanDataAccessory;
using AcsPart = Character.HumanDataAccessory.PartsInfo;
using CharaLimit = Character.HumanData.LoadLimited.Flags;
using CoordLimit = Character.HumanDataCoordinate.LoadLimited.Flags;
using HarmonyLib;

namespace VarietyOfScales
{
    #region Fishbone Interfaces
    public partial class Move
    {
        public Float3 Position { get; set; }
        public Float3 Rotation { get; set; }
        public Float3 Scale { get; set; }
    }

    public partial class MoveMod
    {
        public List<Move> Moves { get; set; }
    }

    public partial class CoordMods : CoordinateExtension<CoordMods>
    {
        public Dictionary<int, MoveMod> Slots { get; set; } = new();

        public CoordMods Merge(CoordLimit limit, CoordMods mods) =>
            (limit & CoordLimit.Accessory) is CoordLimit.None ? this : mods;
    }

    [Extension<CharaMods, CoordMods>(Plugin.Name, "modifications.json")]
    public partial class CharaMods : CharacterExtension<CharaMods>, ComplexExtension<CharaMods, CoordMods>
    {
        public Dictionary<int, CoordMods> Coordinates { get; set; } = new();

        public CharaMods() => Coordinates = new();

        public CharaMods Merge(CharaLimit limit, CharaMods mods) =>
            (limit & CharaLimit.Coorde) is CharaLimit.None ? this : mods;

        public CoordMods Get(int coordinateType) =>
            Coordinates?.GetValueOrDefault(coordinateType, new());

        public CharaMods Merge(int coordinateType, CoordMods mods) => new()
        {
            Coordinates = Coordinates.Merge(coordinateType, mods)
        };

        internal void CopyMove(int dst, int src, int slot) =>
            Coordinates[dst].Slots[slot] = Coordinates.GetValueOrDefault(src, new()).Slots.GetValueOrDefault(slot, new());
    }
    #endregion

    #region Save
    public partial class CoordMods
    {
        internal static void Save(Human human) =>
            Extension<CharaMods, CoordMods>.Humans.NowCoordinate[human] = Store(human);

        static CoordMods Store(Human human) => new()
        {
            Slots = Enumerable.Range(0, human.acs.Accessories.Count)
                .Where(slot => slot >= 20 && human.acs.IsAccessory(slot))
                .ToDictionary(slot => slot, slot => MoveMod.Store(human.acs.Accessories[slot]))
        };

        internal void Store(AcsNode node, IEnumerable<int> slots) =>
            slots.ForEach(slot => Slots[slot] = MoveMod.Store(node.Accessories[slot]));
    }

    public partial class MoveMod
    {
        internal static MoveMod Store(AcsLeaf leaf) => new()
        {
            Moves = leaf.objAcsMove.Where(obj => obj != null).Select(Move.Store).ToList()
        };
    }

    public partial class Move
    {
        internal static Move Store(Transform tf) => new()
        {
            Position = tf.localPosition,
            Rotation = tf.localEulerAngles,
            Scale = tf.localScale
        };
    }
    #endregion

    #region Load
    public partial class Move
    {
        internal void Apply(Transform tf) =>
            (tf.localPosition, tf.localEulerAngles, tf.localScale) = (Position, Rotation, Scale);
    }

    public partial class MoveMod
    {
        internal void Apply(AcsLeaf leaf) => (Moves is not null)
            .Maybe(() =>leaf.objAcsMove
                .Where((tf, index) => tf is not null && index < Moves.Count)
                .ForEachIndex((tf, index) => Moves[index].Apply(tf)));
    }

    public partial class CoordMods
    {
        internal int SlotCount => Slots.Keys.Aggregate(19, Math.Max) + 1;
    }

    public partial class CharaMods
    {
        internal static void Load(Human human) =>
            Extension<CharaMods, CoordMods>.Humans[human].Prepare(human);

        void Prepare(Human human) =>
            AccessoryExtension.PrepareSlots(human, Coordinates.Values.Select(mods => mods.SlotCount).Aggregate(20, Math.Max));
    }

    internal static partial class Hooks
    {
        [HarmonyPrefix, HarmonyWrapSafe]
        [HarmonyPatch(typeof(HumanDataStatus), nameof(HumanDataStatus.Copy))]
        static void HumanDataStatusCopyPrefix(HumanDataStatus __instance, HumanDataStatus src) =>
            AccessoryExtension.PrepareSlots(__instance, src);

        [HarmonyPrefix, HarmonyWrapSafe]
        [HarmonyPatch(typeof(AcsData), nameof(AcsData.Copy))]
        static void HumanDataAccessoryCopyPrefix(AcsData __instance, AcsData src) =>
            AccessoryExtension.PrepareSlots(__instance, src);

        [HarmonyPrefix, HarmonyWrapSafe]
        [HarmonyPatch(typeof(HumanData), nameof(HumanData.Copy))]
        [HarmonyPatch(typeof(HumanData), nameof(HumanData.CopyLimited))]
        static void HumanDataCopyPrefix(HumanData dst, HumanData src) =>
            AccessoryExtension.PrepareSlots(dst, src.Coordinates
                .Select(coord => coord.Accessory.parts.Length).Aggregate(20, Math.Max));
    }

    static partial class AccessoryExtension
    {
        internal static void PrepareSlots(this Human human, int slots) => (
            F.Apply(PrepareSlots, human.acs, slots) +
            F.Apply(PrepareSlots, human.data, slots) +
            F.Apply(NotifySlotState, slots)
        ).Invoke();

        static void PrepareSlots(AcsNode node, int slots) =>
            (node._accessories_k__BackingField, node.nowCoordinate.Accessory.parts) = (
                PrepareSlots(() => new AcsLeaf(), Dispose, node.Accessories, slots),
                PrepareSlots(() => new AcsPart(), F.DoNothing.Ignoring<AcsPart>(), node.nowCoordinate.Accessory.parts, slots));

        internal static void PrepareSlots(HumanData data, int slots) =>
            data.Coordinates.Select(coord => F.Apply(PrepareSlots, coord.Accessory, slots))
                .Aggregate(F.Apply(PrepareSlots, data.Status, slots), (a, b) => a + b).Invoke();

        internal static void PrepareSlots(AcsData dst, AcsData src) =>
            ((dst.parts.Count, src.parts.Count) switch
            {
                var (min, max) when min < max => F.Apply(PrepareSlots, dst, max),
                var (max, min) when min < max => F.Apply(PrepareSlots, src, max),
                _ => F.DoNothing
            }).Invoke();

        internal static void PrepareSlots(HumanDataStatus dst, HumanDataStatus src) =>
            ((dst.showAccessory.Count, src.showAccessory.Count) switch
            {
                var (min, max) when min < max => F.Apply(PrepareSlots, dst, max),
                var (max, min) when min < max => F.Apply(PrepareSlots, src, max),
                _ => F.DoNothing
            }).Invoke();

        static void PrepareSlots(this AcsData data, int slots) =>
            data.parts = PrepareSlots(() => new AcsPart(), data.parts, slots);

        static void PrepareSlots(HumanDataStatus status, int slots) =>
            status.showAccessory = PrepareSlots(() => true, status.showAccessory, slots);

        static T[] PrepareSlots<T>(Func<T> create, T[] items, int slots) =>
            slots is < 20 or > 99 ? items : items.Length < slots ?
                ExtendSlots(create, items, slots) : items.Where((_, index) => index < slots).ToArray();

        static T[] PrepareSlots<T>(Func<T> create, Action<T> destroy, T[] items, int slots) =>
            slots is < 20 or > 99 ? items : items.Length < slots ? ExtendSlots(create, items, slots) :
                items.Where((_, index) => index < slots).ToArray().With(F.Apply(ReduceSlots, destroy, items, slots));

        static T[] ExtendSlots<T>(Func<T> create, T[] items, int slots) =>
            items.Concat(Enumerable.Repeat(0, slots - items.Length).Select(_ => create())).ToArray();

        static void ReduceSlots<T>(Action<T> destroy, T[] items, int slots) =>
            items.Where((_, index) => index >= slots).ForEach(destroy);

        internal static void Dispose(AcsLeaf leaf) =>
            (F.Apply(Human.Destroy, leaf.objAccessory).Ignoring() + leaf.Dispose).Invoke();
    }
    #endregion

}