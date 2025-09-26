using HarmonyLib;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using Character;
using Fishbone;
using CoastalSmell;
using Parent = ChaAccessoryDefine.AccessoryParentKey;
using AcsNode = Character.HumanAccessory;
using AcsLeaf = Character.HumanAccessory.Accessory;
using AcsData = Character.HumanDataAccessory;
using AcsPart = Character.HumanDataAccessory.PartsInfo;
using CharaLimit = Character.HumanData.LoadLimited.Flags;
using CoordLimit = Character.HumanDataCoordinate.LoadLimited.Flags;
using IlVector3Array = Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<UnityEngine.Vector3>;

namespace VarietyOfScales
{
    public struct Move
    {
        public Float3 Position { get; set; }
        public Float3 Rotation { get; set; }
        public Float3 Scale { get; set; }
        void Mods(Transform tf) =>
            (tf.localPosition, tf.localEulerAngles, tf.localScale) = (Position, Rotation, Scale);
        internal void Apply(Transform tf) =>
            (tf != null).Maybe(F.Apply(Mods, tf));
        internal static Move ToMod(Transform tf) => tf == null ? new() : new()
        {
            Position = tf.localPosition,
            Rotation = tf.localEulerAngles,
            Scale = tf.localScale
        };
    }
    public class MoveMod
    {
        public List<Move> Moves { get; set; }
        void Apply(AcsNode node, AcsPart part, int slot) =>
            node.With(F.Apply(node.ChangeAccessory, slot, part.type, part.id, (Parent)part.parentKeyType, true))
                .accessories[slot].objAcsMove.ForEachIndex((tf, index) => Moves.ElementAtOrDefault(index).Apply(tf));
        internal void Apply(Human human, int slot) =>
            Apply(human.acs, human.data.Coordinates[human.data.Status.coordinateType].Accessory.parts[slot], slot);
        internal static MoveMod ToMod(AcsLeaf leaf) => new()
        {
            Moves = leaf.objAcsMove.Select(Move.ToMod).ToList()
        };
        internal static readonly MoveMod Default = new MoveMod { Moves = new() };
    }
    public class CoordMods : CoordinateExtension<CoordMods>
    {
        public List<MoveMod> MoveMods { get; set; }
        public CoordMods() => MoveMods =
            Enumerable.Range(0, AccessoryExtension.ExtensionSlots).Select(_ => MoveMod.Default).ToList();
        public CoordMods Merge(CoordLimit limit, CoordMods mods) =>
            (limit & CoordLimit.Accessory) is CoordLimit.None ? this : mods;
        internal void ApplyMods(Human human) =>
            MoveMods.ForEachIndex((mods, index) => mods.Apply(human, index + 20));
        internal static void Apply(Human human) =>
            Extension.Coord<CharaMods, CoordMods>(human).ApplyMods(human);
        internal static CoordMods ToMods(Human human) => new()
        {
            MoveMods = Enumerable.Range(20, human.acs.accessories.Count - 20)
                .Select(slot => human.acs.accessories[slot])
                .Select(MoveMod.ToMod).ToList()
        };
        internal static void Store(Human human) =>
            Extension.Coord<CharaMods, CoordMods>(human, ToMods(human));
        internal static readonly CoordMods Default = new();
    }
    [Extension<CharaMods, CoordMods>(Plugin.Name, "modifications.json")]
    public class CharaMods : CharacterExtension<CharaMods>, ComplexExtension<CharaMods, CoordMods>
    {
        public Dictionary<int, CoordMods> Coordinates { get; set; }
        public CharaMods() => Coordinates = new();
        int ExtensionSlots =>
            Coordinates.Values
                .Select(mods => mods.MoveMods.Count)
                .Append(AccessoryExtension.ExtensionSlots).Max();

        void PrepareHuman(Human human) =>
            Enumerable.Repeat(human.Increase,
                Math.Max(0, ExtensionSlots + 20 - human.acs.accessories.Count))
                .Aggregate(F.DoNothing, (fst, snd) => fst + snd)();

        internal static void Prepare(Human human) =>
            Extension.Chara<CharaMods, CoordMods>(human).PrepareHuman(human);

        public CharaMods Merge(CharaLimit limit, CharaMods mods) =>
            (limit & CharaLimit.Coorde) is CharaLimit.None ? this : mods;

        public CoordMods Get(int coordinateType) =>
            Coordinates?.GetValueOrDefault(coordinateType) ?? new();

        public CharaMods Merge(int coordinateType, CoordMods mods) => new()
        {
            Coordinates = Coordinates.Merge(coordinateType, mods)
        };
    }
    internal static partial class Hooks
    {

        [HarmonyPostfix, HarmonyWrapSafe]
        [HarmonyPatch(typeof(Human), nameof(Human.Create))]
        static void HumanCreatePostfix(Human __result) =>
            CoordMods.Apply(__result);

        [HarmonyPostfix, HarmonyWrapSafe]
        [HarmonyPatch(typeof(Human), nameof(Human.Reload), [])]
        static void HumanReloadPostfix(Human __instance) =>
            CoordMods.Apply(__instance);

        [HarmonyPostfix, HarmonyWrapSafe]
        [HarmonyPatch(typeof(Human.Reloading), nameof(Human.Reloading.Dispose))]
        internal static void HumanReloadingDisposePostfix(Human.Reloading __instance) =>
            (!__instance._isReloading).Maybe(F.Apply(CoordMods.Apply, __instance._human));

        [HarmonyPrefix, HarmonyWrapSafe]
        [HarmonyPatch(typeof(HumanDataStatus), nameof(HumanDataStatus.Copy))]
        static void HumanDataStatusCopyPrefix(HumanDataStatus __instance, HumanDataStatus src) =>
            Adjust(__instance, src);

        static void Adjust(HumanDataStatus dst, HumanDataStatus src) =>
            ((dst.showAccessory.Count > src.showAccessory.Count, dst.showAccessory.Count < src.showAccessory.Count) switch
            {
                (true, false) => Enumerable.Repeat(src.Increase, dst.showAccessory.Count - src.showAccessory.Count),
                (false, true) => Enumerable.Repeat(dst.Increase, src.showAccessory.Count - dst.showAccessory.Count),
                _ => []
            }).Aggregate(F.DoNothing, (fst, snd) => fst + snd)();

        [HarmonyPrefix, HarmonyWrapSafe]
        [HarmonyPatch(typeof(AcsData), nameof(AcsData.Copy))]
        static void HumanDataAccessoryCopyPrefix(AcsData __instance, AcsData src) =>
            Adjust(__instance, src);

        [HarmonyPrefix, HarmonyWrapSafe]
        [HarmonyPatch(typeof(HumanData), nameof(HumanData.Copy))]
        [HarmonyPatch(typeof(HumanData), nameof(HumanData.CopyLimited))]
        static void HumanDataCopyPrefix(HumanData dst, HumanData src) =>
            Enumerable.Range(0, Math.Min(dst.Coordinates.Count, src.Coordinates.Count))
                .ForEach(index => Adjust(dst.Coordinates[index].Accessory, src.Coordinates[index].Accessory));
        static void Adjust(AcsData dst, AcsData src) =>
            ((dst.parts.Count > src.parts.Count, dst.parts.Count < src.parts.Count) switch
            {
                (true, false) => Enumerable.Repeat(src.Increase, dst.parts.Count - src.parts.Count),
                (false, true) => Enumerable.Repeat(dst.Increase, src.parts.Count - dst.parts.Count),
                _ => []
            }).Aggregate(F.DoNothing, (fst, snd) => fst + snd)();
    }
    static partial class AccessoryExtension
    {
        internal static T Bypass<T>(this Func<T> action, int slotNo) =>
            slotNo < ChaFileDefine.AccessorySlotNum ? default : action();
        internal static bool Bypass(this Action action, int slotNo) =>
            slotNo < ChaFileDefine.AccessorySlotNum || false.With(action);
        static void Dispose(this AcsNode node, int slot) =>
            node.accessories[slot].Dispose();
        internal static void Increase(this Human human) =>
            Increase(human.With(human.data.Increase).With(F.Apply(Increase, human.acs)).coorde.nowCoordinate);
        internal static void Decrease(this Human human) =>
            Decrease(human.With(human.data.Decrease).With(F.Apply(Decrease, human.acs)).coorde.nowCoordinate);
        internal static void Increase(this HumanData data) =>
            data.With(F.Apply(Increase, data.Status)).Coordinates.ForEach(Increase);
        internal static void Decrease(this HumanData data) =>
            data.With(F.Apply(Decrease, data.Status)).Coordinates.ForEach(Decrease);
        internal static void Increase(HumanDataCoordinate data) =>
            Increase(data.Accessory);
        internal static void Decrease(HumanDataCoordinate data) =>
            Decrease(data.Accessory);
        static void Increase(this AcsNode acs) =>
            acs._accessories_k__BackingField = acs.accessories.Append(new AcsLeaf()).ToArray();
        static void Decrease(this AcsNode node) =>
            node._accessories_k__BackingField = node
                .With(F.Apply(node.Dispose, node.accessories.Count - 1))
                .accessories.Take(node.accessories.Count - 1).ToArray();
        internal static void Increase(this AcsData data) =>
            data.parts = data.parts.Append(new AcsPart()).ToArray();
        internal static void Decrease(this AcsData data) =>
            data.parts = data.parts.Take(data.parts.Count - 1).ToArray();
        internal static void Increase(this HumanDataStatus status) =>
            status.showAccessory = status.showAccessory.Append(true).ToArray();
        internal static void Decrease(this HumanDataStatus status) =>
            status.showAccessory = status.showAccessory.Take(status.showAccessory.Count - 1).ToArray();
    }
    internal static partial class Hooks
    {
        [HarmonyPrefix, HarmonyWrapSafe]
        [HarmonyPatch(typeof(AcsNode), nameof(AcsNode.GetAccessoryDefaultColorData), typeof(int))]
        static bool GetAccessoryDefaultColorDataPrefix(AcsNode __instance, int slotNo, ref AcsNode.DefaultColorData __result) =>
            null == (__result = F.Apply(__instance.GetDefaultColorData, slotNo).Bypass(slotNo));
    }
    static partial class AccessoryExtension
    {
        internal static AcsNode.DefaultColorData GetDefaultColorData(this AcsNode node, int slot) =>
            node.accessories[slot].cusAcsCmp == null
                ? new AcsNode.DefaultColorData()
                : new AcsNode.DefaultColorData(node.accessories[slot].cusAcsCmp);
    }
    internal static partial class Hooks
    {
        [HarmonyPrefix, HarmonyWrapSafe]
        [HarmonyPatch(typeof(AcsNode), nameof(AcsNode.ChangeAccessory), typeof(int), typeof(int), typeof(int), typeof(Parent), typeof(bool))]
        static bool ChangeAccessoryPrefix(AcsNode __instance, int slotNo, int type, int id, Parent parentKey, bool forceChange) =>
            F.Apply(__instance.Change, slotNo, type, id, parentKey, forceChange).Bypass(slotNo);
    }
    static partial class AccessoryExtension
    {
        internal static bool UpdateWithDefaultColor = false;
        internal static void Change(this AcsNode node, int slot, int category, int id, Parent parent, bool force) =>
            (force || Different(node.accessories[slot].infoAccessory, node.nowCoordinate.Accessory.parts[slot], category, id))
                .Maybe(F.Apply(node.Dispose, slot) + F.Apply(Change, node, slot, (ChaListDefine.CategoryNo)category, id, parent));
        static bool Different(ListInfoBase info, AcsPart part, int category, int id) =>
            (info?.Category, info?.Id) != ((part.type, part.id) = (category, id));
        static void Change(AcsNode node, int slot, ChaListDefine.CategoryNo category, int id, Parent parent) =>
            (category is not ChaListDefine.CategoryNo.ao_none).Either(
                F.Apply(Remove, node, slot, category, id,
                    parent is Parent.none ? GetDefaultParent(node.human, category, id) : parent),
                F.Apply(node.Dispose, slot) + F.Apply(Assign, node, slot, category, id,
                    parent is Parent.none ? GetDefaultParent(node.human, category, id) : parent));
        static void Remove(AcsNode node, int slot, ChaListDefine.CategoryNo category, int id, Parent parent) =>
            (node.accessories[slot] = new AcsLeaf())
                .With(F.Apply(PostRemove, node.nowCoordinate.Accessory.parts[slot]));
        static void Assign(AcsNode node, int slot, ChaListDefine.CategoryNo category, int id, Parent parent) =>
            (node.accessories[slot] =
                new AcsLeaf(node.human, category, id, slot,
                    GetWeightType(node.human, category, id),
                    ToTransform(node, parent.ToString())))
                    .With(F.Apply(PostChange, node.nowCoordinate.Accessory.parts[slot], node, slot, parent));
        static Parent GetDefaultParent(Human human, ChaListDefine.CategoryNo category, int id) =>
            GetInfo(human, category, id, ChaListDefine.KeyType.Parent, out var value)
                && Enum.TryParse<Parent>(value, out var defaultParent) ? defaultParent : Parent.RootBone;
        static Human.UseCopyWeightType GetWeightType(Human human, ChaListDefine.CategoryNo category, int id) =>
            GetInfo(human, category, id, ChaListDefine.KeyType.WeightType, out var data)
                && Enum.TryParse<Human.UseCopyWeightType>(data, out var value) ? value : Human.UseCopyWeightType.None;
        static Transform ToTransform(AcsNode node, string parent) =>
            node.GetRefTransform(Enum.TryParse<Table.RefObjKey>(parent, out var value) ? value : Table.RefObjKey.RootBone);
        static void PostRemove(AcsPart part) =>
            part.Copy(AcsNode.NoneAcsData);
        static void PostChange(AcsPart part, AcsNode node, int slot, Parent parent) =>
            part.With(F.Apply(PostChange, node.human, part, node.accessories[slot]))
                .With(F.Apply(ApplyFKDefaults, node.accessories[slot].cusAcsCmp, part))
                .With(F.Apply(node.ChangeColor, slot))
                .With(F.Apply(node.ChangePatternTexture, slot, -1))
                .With(F.Apply(node.ChangePatternColor, slot, -1))
                .With(F.Apply(node.ChangePatternParams, slot, -1))
                .With(F.Apply(node.SetupFK, slot))
                .ChangeParent(parent);
        static void PostChange(Human human, AcsPart part, AcsLeaf leaf) =>
            (UpdateWithDefaultColor || human.IsLoadWithDefaultColorAndPtn()).Maybe(F.Apply(ApplyDefaults, leaf.cusAcsCmp, part));
        static void ApplyDefaults(ChaAccessoryComponent cmp, AcsPart part) =>
            (UpdateWithDefaultColor, part.color[0], part.color[1], part.color[2], part.color[3]) =
                (false, cmp.defColor01,
                    cmp.useColor01 ? cmp.defColor02 : part.color[1],
                    cmp.useColor02 ? cmp.defColor03 : part.color[2],
                    cmp.useColor03 ? cmp.defColor04 : part.color[3])
                .With(F.Apply(ApplyDefaults, cmp.pattern01, part.colorInfo[0]))
                .With(F.Apply(ApplyDefaults, cmp.pattern02, part.colorInfo[1]))
                .With(F.Apply(ApplyDefaults, cmp.pattern03, part.colorInfo[2]));
        static void ApplyDefaults(ChaAccessoryComponent.Pattern defaults, AcsPart.ColorInfo info) =>
            (info.pattern, info.patternColor, info.offset, info.rotate, info.tiling) =
                (defaults.patternID, defaults.defColor, defaults.offset, defaults.rotate, defaults.tiling);
        static void ApplyFKDefaults(ChaAccessoryComponent cmp, AcsPart part) =>
            part.fkInfo.bones = cmp.GetFKBonesDef();
        static void ChangeParent(this AcsPart part, Parent parent) =>
            (part.parentKeyType, part.partsOfHead) = ((int)parent, ChaAccessoryDefine.CheckPartsOfHead(parent));
    }
    internal static partial class Hooks
    {
        [HarmonyPrefix, HarmonyWrapSafe]
        [HarmonyPatch(typeof(AcsNode), nameof(AcsNode.ChangeAccessoryColor), typeof(int))]
        static bool ChangeAccessoryColorPrefix(AcsNode __instance, int slotNo, ref bool __result) =>
            (__result = true) && F.Apply(__instance.ChangeColor, slotNo).Bypass(slotNo);
    }
    static partial class AccessoryExtension
    {
        internal static void ChangeColor(this AcsNode node, int slot) =>
            Enumerable.Range(0, 4).ForEach(node.accessories[slot].ChangeColor(node.nowCoordinate.Accessory.parts[slot]));
        static Action<int> ChangeColor(this AcsLeaf leaf, AcsPart part) => index =>
            (leaf.cusAcsCmp is not null && leaf.cusAcsCmp.HasColor(index)).Maybe(F.Apply(ChangeColor,
                leaf.renderers.ToArray(), ChaShader.Accessory.GetMainColorID(index), part.color[index]));
        static void ChangeColor(Renderer[] renderers, int shaderId, Color color) =>
            renderers.ForEach(renderer => renderer?.material?.SetColor(shaderId, color));
        static bool HasColor(this ChaAccessoryComponent cmp, int index) =>
            cmp != null && index switch
            {
                1 => cmp.useColor01,
                2 => cmp.useColor02,
                3 => cmp.useColor03,
                _ => true
            };
    }
    internal static partial class Hooks
    {
        [HarmonyPrefix, HarmonyWrapSafe]
        [HarmonyPatch(typeof(AcsNode), nameof(AcsNode.ChangeAccessoryParent), typeof(int), typeof(Parent))]
        static bool ChangeAccessoryParentPrefix(AcsNode __instance, int slotNo, Parent parentKey, bool __result) =>
            (__result = true) && F.Apply(__instance.ChangeParent, slotNo, parentKey).Bypass(slotNo);
    }
    static partial class AccessoryExtension
    {
        internal static void ChangeParent(this AcsNode node, int slot, Parent parent) =>
            node.accessories[slot].objAccessory.transform.SetParent(ToTransform(node, parent.ToString()), false);
    }
    internal static partial class Hooks
    {
        [HarmonyPrefix, HarmonyWrapSafe]
        [HarmonyPatch(typeof(AcsNode), nameof(AcsNode.ChangeAccessoryPatternTexture), typeof(int), typeof(int))]
        static bool ChangeAccessoryPatternTexturePrefix(AcsNode __instance, int slotNo, int index, bool __result) =>
            (__result = true) && F.Apply(__instance.ChangePatternTexture, slotNo, index).Bypass(slotNo);
    }
    static partial class AccessoryExtension
    {
        internal static void ChangePatternTexture(this AcsNode node, int slot, int index) =>
            (index < 0 ? Enumerable.Range(0, 3) : [index]).ForEach(idx =>
                ChangePatternTexture(node.Accessories[slot],
                    node.nowCoordinate.Accessory.parts[slot], node.human, idx));
        static void ChangePatternTexture(AcsLeaf leaf, AcsPart part, Human human, int index) =>
            (leaf.cusAcsCmp != null && leaf.cusAcsCmp.HasPattern(index))
                .Maybe(F.Apply(ChangePatternTexture, leaf.renderers.ToArray(),
                    ChaShader.Accessory.GetPatternMaskID(index), human, part, index));
        static void ChangePatternTexture(Renderer[] renderers, int shaderId, Human human, AcsPart part, int index) =>
            ChangeTexture(renderers, shaderId, ToPatternTexture(human, part.colorInfo[index].pattern));
        static Texture2D ToPatternTexture(Human human, int id) =>
            human.GetTexture(ChaListDefine.CategoryNo.mt_pattern, id, ChaListDefine.KeyType.MainTexAB, ChaListDefine.KeyType.MainTex);
        static void ChangeTexture(Renderer[] renderers, int shaderId, Texture2D texture) =>
            renderers.ForEach(renderer => renderer?.material?.SetTexture(shaderId, texture));
    }
    internal static partial class Hooks
    {
        [HarmonyPrefix, HarmonyWrapSafe]
        [HarmonyPatch(typeof(AcsNode), nameof(AcsNode.ChangeAccessoryPatternColor), typeof(int), typeof(int))]
        static bool ChangeAccessoryPatternColorPrefix(AcsNode __instance, int slotNo, int index, ref bool __result) =>
            (__result = true) && F.Apply(__instance.ChangePatternColor, slotNo, index).Bypass(slotNo);
    }
    static partial class AccessoryExtension
    {
        internal static void ChangePatternColor(this AcsNode node, int slot, int index) =>
            (index < 0 ? Enumerable.Range(0, 3) : [index]).ForEach(idx =>
                ChangePatternColor(node.Accessories[slot], node.nowCoordinate.Accessory.parts[slot], idx));
        static void ChangePatternColor(AcsLeaf leaf, AcsPart part, int index) =>
            (leaf.cusAcsCmp != null && leaf.cusAcsCmp.HasPattern(index)).Maybe(F.Apply(ChangeColor,
                leaf.renderers.ToArray(), ChaShader.Accessory.GetPatternColorID(index), part.colorInfo[index].patternColor));
    }
    internal static partial class Hooks
    {
        [HarmonyPrefix, HarmonyWrapSafe]
        [HarmonyPatch(typeof(AcsNode), nameof(AcsNode.ChangeAccessoryPatternParameter), typeof(int), typeof(int))]
        static bool ChangeAccessoryPatternParameter(AcsNode __instance, int slotNo, int index, bool __result) =>
            (__result = true) && F.Apply(__instance.ChangePatternParams, slotNo, index).Bypass(slotNo);
    }
    static partial class AccessoryExtension
    {
        internal static void ChangePatternParams(this AcsNode node, int slot, int index) =>
            (index < 0).Either(
                ChangePatternParams(node.Accessories[slot], node.nowCoordinate.Accessory.parts[slot]).Apply(index),
                F.Apply(F.ForEach, Enumerable.Range(0, 3),
                    ChangePatternParams(node.Accessories[slot], node.nowCoordinate.Accessory.parts[slot])));
        static Action<int> ChangePatternParams(AcsLeaf leaf, AcsPart part) => index =>
            ChangePatternParams(leaf, ToParams(leaf.cusAcsCmp, part, index));
        static Tuple<int, float>[] ToParams(ChaAccessoryComponent cmp, AcsPart part, int index) =>
            cmp.HasPattern(index) ? ToParams(part.colorInfo[index], index) : [];
        static Tuple<int, float>[] ToParams(AcsPart.ColorInfo info, int index) =>
            info == null ? [] : [
                new (ChaShader.Accessory.GetPatternOffsetUID(index), info.offset.x),
                new (ChaShader.Accessory.GetPatternOffsetVID(index), info.offset.y),
                new (ChaShader.Accessory.GetPatternScaleUID(index), info.tiling.x),
                new (ChaShader.Accessory.GetPatternScaleVID(index), info.tiling.y),
                new (ChaShader.Accessory.GetPatternRotateID(index), info.rotate),
            ];
        static void ChangePatternParams(AcsLeaf leaf, params Tuple<int, float>[] pairs) =>
            pairs.ForEach(pair => ChangeParams(leaf.renderers, pair.Item1, pair.Item2));
        static void ChangeParams(Renderer[] renderers, int shaderId, float value) =>
            renderers.ForEach(renderer => renderer.material.SetFloat(shaderId, value));
    }
    internal static partial class Hooks
    {
        [HarmonyPrefix, HarmonyWrapSafe]
        [HarmonyPatch(typeof(AcsNode), nameof(AcsNode.SetAccessoryPos))]
        static bool SetAccessoryPosPrefix(AcsNode __instance, int slotNo, int correctNo, float value, bool add, int flag) =>
            F.Apply(__instance.SetPosition, slotNo, correctNo, value, add, flag).Bypass(slotNo);
        [HarmonyPrefix, HarmonyWrapSafe]
        [HarmonyPatch(typeof(AcsNode), nameof(AcsNode.SetAccessoryRot))]
        static bool SetAccessoryRotPrefix(AcsNode __instance, int slotNo, int correctNo, float value, bool add, int flag) =>
            F.Apply(__instance.SetRotation, slotNo, correctNo, value, add, flag).Bypass(slotNo);
        [HarmonyPrefix, HarmonyWrapSafe]
        [HarmonyPatch(typeof(AcsNode), nameof(AcsNode.SetAccessoryScl))]
        static bool SetAccessorySclPrefix(AcsNode __instance, int slotNo, int correctNo, float value, bool add, int flag) =>
            F.Apply(__instance.SetScale, slotNo, correctNo, value, add, flag).Bypass(slotNo);
    }
    static partial class AccessoryExtension
    {
        internal static void SetPosition(this AcsNode node, int slot, int correctNo, float value, bool add, int flag) =>
            (correctNo < node.accessories[slot].objAcsMove.Count)
                .Maybe(F.Apply(SetPosition, node.accessories[slot], correctNo, ToModifier(value, flag, PositionAdjust(add))));
        internal static void SetRotation(this AcsNode node, int slot, int correctNo, float value, bool add, int flag) =>
            (correctNo < node.accessories[slot].objAcsMove.Count)
                .Maybe(F.Apply(SetRotation, node.accessories[slot], correctNo, ToModifier(value, flag, RotationAdjust(add))));
        internal static void SetScale(this AcsNode node, int slot, int correctNo, float value, bool add, int flag) =>
            (correctNo < node.accessories[slot].objAcsMove.Count)
                .Maybe(F.Apply(SetScale, node.accessories[slot], correctNo, ToModifier(value, flag, ScaleAdjust(add))));
        static void SetPosition(AcsLeaf leaf, int correctNo, Func<Vector3, Vector3> modifier) =>
            leaf.objAcsMove[correctNo].localPosition = modifier(leaf.objAcsMove[correctNo].localPosition);
        static void SetRotation(AcsLeaf leaf, int correctNo, Func<Vector3, Vector3> modifier) =>
            leaf.objAcsMove[correctNo].localEulerAngles = modifier(leaf.objAcsMove[correctNo].localEulerAngles);
        static void SetScale(AcsLeaf leaf, int correctNo, Func<Vector3, Vector3> modifier) =>
         leaf.objAcsMove[correctNo].localScale = modifier(leaf.objAcsMove[correctNo].localScale);
        static float PositionAdjust(float value) =>
            value < -100 ? -100 : value > 100 ? 100 : value;
        static float RotationAdjust(float value) =>
            (value < 0 ? value + 360 : value) % 360;
        static float ScaleAdjust(float value) =>
            value < 0.1f ? 0.1f : value > 100 ? 100 : value;
        static Func<float, float, float> PositionAdjust(bool add) =>
            add ? (org, dst) => PositionAdjust(org + dst * 0.01f) : (_, dst) => PositionAdjust(dst * 0.01f);
        static Func<float, float, float> RotationAdjust(bool add) =>
            add ? (org, dst) => RotationAdjust(org + dst) : (_, dst) => RotationAdjust(dst);
        static Func<float, float, float> ScaleAdjust(bool add) =>
            add ? (org, dst) => ScaleAdjust(org + dst) : (_, dst) => ScaleAdjust(dst);
        static Func<Vector3, Vector3> ToModifier(float value, int flag, Func<float, float, float> adjust) =>
            flag switch
            {
                1 => (vector) => new(adjust(vector.x, value), vector.y, vector.z),
                2 => (vector) => new(vector.x, adjust(vector.y, value), vector.z),
                4 => (vector) => new(vector.x, vector.y, adjust(vector.z, value)),
                3 => (vector) => new(adjust(vector.x, value), adjust(vector.y, value), vector.z),
                5 => (vector) => new(adjust(vector.x, value), vector.y, adjust(vector.z, value)),
                6 => (vector) => new(vector.x, adjust(vector.y, value), adjust(vector.z, value)),
                7 => (vector) => new(adjust(vector.x, value), adjust(vector.y, value), adjust(vector.z, value)),
                _ => vector => vector
            };
    }
    internal static partial class Hooks
    {
        [HarmonyPrefix, HarmonyWrapSafe]
        [HarmonyPatch(typeof(AcsNode), nameof(AcsNode.SetupAccessoryFK), typeof(int))]
        static bool SetupAccessoryFKPrefix(AcsNode __instance, int slotNo) =>
            F.Apply(__instance.SetupFK, slotNo).Bypass(slotNo);
    }
    static partial class AccessoryExtension
    {
        internal static void SetupFK(this AcsNode node, int slot) =>
            SetupFK(node.accessories[slot], node.nowCoordinate.Accessory.parts[slot]);
        static void SetupFK(AcsLeaf leaf, AcsPart part) =>
            (leaf.cusAcsCmp != null).Maybe(F.Apply(SetupFK, leaf.cusAcsCmp, part.fkInfo));
        static void SetupFK(ChaAccessoryComponent cmp, AcsPart.FKInfo info) =>
            info.bones = cmp.FKBone.Select(tf => tf.localEulerAngles).ToArray();
    }
    internal static partial class Hooks
    {
        [HarmonyPrefix, HarmonyWrapSafe]
        [HarmonyPatch(typeof(AcsNode), nameof(AcsNode.UpdateAccessoryFK), typeof(int), typeof(IlVector3Array))]
        static bool UpdateAccessoryFKPrefix(AcsNode __instance, int slotNo, IlVector3Array values) =>
            F.Apply(__instance.UpdateFK, slotNo, values).Bypass(slotNo);
        [HarmonyPrefix, HarmonyWrapSafe]
        [HarmonyPatch(typeof(AcsNode), nameof(AcsNode.UpdateAccessoryFK), typeof(int))]
        static bool UpdateAccessoryFKPrefix(AcsNode __instance, int slotNo) =>
            F.Apply(__instance.UpdateFK, slotNo).Bypass(slotNo);
    }
    static partial class AccessoryExtension
    {
        internal static void UpdateFK(this AcsNode node, int slot, IlVector3Array values) =>
            (values != null).Maybe(F.Apply(node.accessories[slot].cusAcsCmp.UpdateFK, values));
        internal static void UpdateFK(this AcsNode node, int slot) =>
            UpdateFK(node.accessories[slot], node.nowCoordinate.Accessory.parts[slot].fkInfo);
        static void UpdateFK(AcsLeaf leaf, AcsPart.FKInfo info) =>
            F.Apply(leaf.cusAcsCmp.UpdateFK, info.bones);
    }
    internal static partial class Hooks
    {
        [HarmonyPrefix, HarmonyWrapSafe]
        [HarmonyPatch(typeof(AcsNode), nameof(AcsNode.SetAccessoryFK), typeof(int), typeof(int), typeof(float), typeof(bool), typeof(int))]
        static bool SetAccessoryFKPrefix(AcsNode __instance, int slotNo, int correctNo, float value, bool add, int flag) =>
            F.Apply(__instance.SetFK, slotNo, correctNo, value, add, flag).Bypass(slotNo);
    }
    static partial class AccessoryExtension
    {
        internal static void SetFK(this AcsNode node, int slot, int correctNo, float value, bool add, int flag) =>
            SetFK(node.accessories[slot], node.nowCoordinate.Accessory.parts[slot], correctNo, value, add, flag);
        static void SetFK(AcsLeaf leaf, AcsPart part, int correctNo, float value, bool add, int flag) =>
            (correctNo < leaf.objAcsFK.Count).Maybe(F.Apply(SetFK,
                leaf, part.fkInfo, correctNo, ToModifier(value, flag, RotationAdjust(add))));
        static void SetFK(AcsLeaf leaf, AcsPart.FKInfo info, int correctNo, Func<Vector3, Vector3> modifier) =>
            leaf.objAcsFK[correctNo].localEulerAngles = info.bones[correctNo] = modifier(info.bones[correctNo]);
    }
    internal static partial class Hooks
    {
        [HarmonyPrefix, HarmonyWrapSafe]
        [HarmonyPatch(typeof(AcsLeaf), nameof(AcsLeaf.ResetCloth))]
        static bool AccessoryResetCloth(AcsLeaf __instance) =>
            __instance != null;
    }

    [BepInProcess(Process)]
    [BepInDependency(Fishbone.Plugin.Guid)]
    [BepInPlugin(Guid, Name, Version)]
    public partial class Plugin : BasePlugin
    {
        internal static Plugin Instance;
        public const string Name = "VarietyOfScales";
        public const string Guid = $"{Process}.{Name}";
        public const string Version = "0.2.2";
        private Harmony Patch;
        public override bool Unload() =>
                true.With(Patch.UnpatchSelf) && base.Unload();
    }

}