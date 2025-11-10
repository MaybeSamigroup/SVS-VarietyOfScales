using HarmonyLib;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using System;
using System.Linq;
using UnityEngine;
using Fishbone;
using Character;
using CoastalSmell;
using Parent = ChaAccessoryDefine.AccessoryParentKey;
using AcsNode = Character.HumanAccessory;
using AcsLeaf = Character.HumanAccessory.Accessory;
using AcsPart = Character.HumanDataAccessory.PartsInfo;
using IlVector3Array = Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<UnityEngine.Vector3>;

namespace VarietyOfScales
{
    static partial class AccessoryExtension
    {
        internal static (bool,T) Bypass<T>(this Func<T> action, int slotNo) =>
            slotNo < ChaFileDefine.AccessorySlotNum ? (true, default) : (false, action());
        internal static bool Bypass(this Action action, int slotNo) =>
            slotNo < ChaFileDefine.AccessorySlotNum || false.With(action);
    }

    internal static partial class Hooks
    {
        [HarmonyPrefix, HarmonyWrapSafe]
        [HarmonyPatch(typeof(AcsNode), nameof(AcsNode.GetAccessoryDefaultColorData), typeof(int))]
        static bool GetAccessoryDefaultColorDataPrefix(AcsNode __instance, int slotNo, ref AcsNode.DefaultColorData __result) =>
            ((_, __result) = F.Apply(__instance.GetDefaultColorData, slotNo).Bypass(slotNo)).Item1;
    }

    static partial class AccessoryExtension
    {
        internal static AcsNode.DefaultColorData GetDefaultColorData(this AcsNode node, int slot) =>
            node.Accessories[slot].cusAcsCmp == null
                ? new AcsNode.DefaultColorData()
                : new AcsNode.DefaultColorData(node.Accessories[slot].cusAcsCmp);
    }

    internal static partial class Hooks
    {
        [HarmonyPrefix, HarmonyWrapSafe]
        [HarmonyPatch(typeof(AcsNode), nameof(AcsNode.IsAccessory), typeof(int))]
        static bool IsAccessory(AcsNode __instance, int slotNo, ref bool __result) =>
            ((_, __result) = F.Apply(__instance.Check, slotNo).Bypass(slotNo)).Item1;
    }

    static partial class AccessoryExtension
    {
        internal static bool Check(this AcsNode node, int slot) =>
            slot < node.Accessories.Count && Check(node.Accessories[slot]);
        static bool Check(AcsLeaf leaf) =>
            ((ChaListDefine.CategoryNo?)(leaf?.infoAccessory?.Category)
                ?? ChaListDefine.CategoryNo.ao_none) is not ChaListDefine.CategoryNo.ao_none;
    }

    internal static partial class Hooks
    {
        [HarmonyPostfix, HarmonyWrapSafe]
        [HarmonyPatch(typeof(AcsNode), nameof(AcsNode.ChangeAccessory), typeof(bool))]
        static void ChangeAccessoryPostfix(AcsNode __instance, bool forceChange) =>
            Enumerable.Range(0, __instance.Accessories.Count)
                .Where(slot => slot >= 20).ForEach(slot => __instance.Change(slot, forceChange));

        [HarmonyPrefix, HarmonyWrapSafe]
        [HarmonyPatch(typeof(AcsNode), nameof(AcsNode.ChangeAccessory), typeof(int), typeof(int), typeof(int), typeof(Parent), typeof(bool))]
        static bool ChangeAccessoryPrefix(AcsNode __instance, int slotNo, int type, int id, Parent parentKey, bool forceChange) =>
            F.Apply(__instance.Change, slotNo, type, id, parentKey, forceChange).Bypass(slotNo);
    }

    static partial class AccessoryExtension
    {
        internal static void Change(this AcsNode node, int slot, bool force) =>
            node.Change(node.nowCoordinate.Accessory.parts[slot], slot, force);

        internal static void Change(this AcsNode node, AcsPart part, int slot, bool force) =>
            node.Change(slot, part.type, part.id, (Parent)part.parentKeyType, force);

        internal static void Change(this AcsNode node, int slot, int category, int id, Parent parent, bool force) =>
            (force || Different(node.Accessories[slot].infoAccessory, node.nowCoordinate.Accessory.parts[slot], category, id))
                .Maybe(F.Apply(Dispose, node.Accessories[slot]) + F.Apply(Change, node, slot, (ChaListDefine.CategoryNo)category, id, parent));

        static bool Different(ListInfoBase info, AcsPart part, int category, int id) =>
            (info?.Category, info?.Id) != ((part.type, part.id) = (category, id));

        static void Change(AcsNode node, int slot, ChaListDefine.CategoryNo category, int id, Parent parent) =>
            (category is not ChaListDefine.CategoryNo.ao_none).Either(
                F.Apply(Remove, node, slot, category, id,
                    parent is Parent.none ? GetDefaultParent(node.human, category, id) : parent),
                F.Apply(Dispose, node.Accessories[slot]) + F.Apply(Assign, node, slot, category, id,
                    parent is Parent.none ? GetDefaultParent(node.human, category, id) : parent));

        static void Remove(AcsNode node, int slot, ChaListDefine.CategoryNo category, int id, Parent parent) =>
            (node.Accessories[slot] = new AcsLeaf()).With(F.Apply(PostRemove, node.nowCoordinate.Accessory.parts[slot]));

        static void Assign(AcsNode node, int slot, ChaListDefine.CategoryNo category, int id, Parent parent) =>
            (node.Accessories[slot] =
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

        static void PostChange(AcsPart part, AcsNode node, int slot, Parent parent) => (
            F.Apply(PostChange, node.human, part, node.Accessories[slot]) +
            F.Apply(ChangeAcsColor, node, slot) +
            F.Apply(ChangePtnTexture, node, slot, -1) +
            F.Apply(ChangePtnColor, node, slot, -1) +
            F.Apply(ChangePtnParams, node, slot, -1) +
            F.Apply(ChangeParent, part, parent) +
            F.Apply(ApplyMove, node, slot) +
            F.Apply(ApplyDynamicBones, node.Accessories[slot], !(part.noShake || part.fkInfo.use)) +
            F.Apply(SetupFK, node.Accessories[slot], part) +
            F.Apply(ApplyFK, node.Accessories[slot], part)
        ).Invoke();

        static void PostChange(Human human, AcsPart part, AcsLeaf leaf) =>
            human.IsLoadWithDefaultColorAndPtn().Maybe(F.Apply(ApplyDefaults, part, leaf));

        static void ApplyDefaults(AcsPart part, AcsLeaf leaf) => (
            F.Apply(ApplyDefaults, leaf.cusAcsCmp, part) +
            F.Apply(ApplyDefaults, leaf.cusAcsCmp.pattern01, part.colorInfo[0]) +
            F.Apply(ApplyDefaults, leaf.cusAcsCmp.pattern02, part.colorInfo[1]) +
            F.Apply(ApplyDefaults, leaf.cusAcsCmp.pattern03, part.colorInfo[2])
        ).Invoke();

        static void ApplyDefaults(ChaAccessoryComponent cmp, AcsPart part) =>
            (part.color[0], part.color[1], part.color[2], part.color[3]) =
                (cmp.defColor01, cmp.defColor02, cmp.defColor03, cmp.defColor04);

        static void ApplyDefaults(ChaAccessoryComponent.Pattern defaults, AcsPart.ColorInfo info) =>
            (info.pattern, info.patternColor, info.offset, info.rotate, info.tiling) =
                (defaults.patternID, defaults.defColor, defaults.offset, defaults.rotate, defaults.tiling);

        static void ApplyFK(AcsLeaf leaf, AcsPart part) =>
            part.fkInfo.use.Maybe(F.Apply(leaf.cusAcsCmp.UpdateFK, part.fkInfo.bones) + F.Apply(ApplyFK, leaf, part.fkInfo.bones));

        static void ApplyFK(AcsLeaf leaf, IlVector3Array bones) =>
            bones.ForEachIndex((v3, index) => leaf.objAcsFK[index].localEulerAngles = v3);

        static void ApplyDynamicBones(AcsLeaf leaf, bool state) =>
            leaf._dynamicBones.ForEach(bone => bone.enabled = state);

        static void ApplyMove(AcsNode node, int slot) =>
            Extension.Coord<CharaMods, CoordMods>(node.human).Slots.TryGetValue(slot, out var move)
                .Either(F.Apply(node.UpdateAccessoryMoveFromInfo, slot).Ignoring(), F.Apply(ApplyMove, node.Accessories[slot], move));

        static void ApplyMove(AcsLeaf leaf, MoveMod mod) => mod.Apply(leaf);

        static void ChangeParent(AcsPart part, Parent parent) =>
            (part.parentKeyType, part.partsOfHead) =
                ((int)parent, ChaAccessoryDefine.CheckPartsOfHead(parent));
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
            node.Accessories[slot].objAccessory.transform.SetParent(ToTransform(node, parent.ToString()), false);
    }

    internal static partial class Hooks
    {
        [HarmonyPrefix, HarmonyWrapSafe]
        [HarmonyPatch(typeof(AcsNode), nameof(AcsNode.ChangeAccessoryColor), typeof(int))]
        static bool ChangeAccessoryColorPrefix(AcsNode __instance, int slotNo, ref bool __result) =>
            (__result = true) && F.Apply(__instance.ChangeAcsColor, slotNo).Bypass(slotNo);

        [HarmonyPrefix, HarmonyWrapSafe]
        [HarmonyPatch(typeof(AcsNode), nameof(AcsNode.ChangeAccessoryPatternColor), typeof(int), typeof(int))]
        static bool ChangeAccessoryPatternColorPrefix(AcsNode __instance, int slotNo, int index, ref bool __result) =>
            (__result = true) && F.Apply(__instance.ChangePtnColor, slotNo, index).Bypass(slotNo);
    }

    static partial class AccessoryExtension
    {
        internal static void ChangeAcsColor(this AcsNode node, int slot) =>
            Check(node, slot).Maybe(Enumerable.Range(0, 4)
                .Where(index => HasColor(node, slot, index))
                .Select(index => UpdateAction(AcsColorSetter(node, slot, index), AcsColorGetter(node, slot, index)))
                .Aggregate(F.DoNothing, (a, b) => a + b));

        internal static void ChangePtnColor(this AcsNode node, int slot, int index) =>
            Check(node, slot).Maybe((index < 0 ? Enumerable.Range(0, 3) : [index])
                .Where(index => HasPattern(node, slot, index))
                .Select(index => UpdateAction(PtnColorSetter(node, slot, index), PtnColorGetter(node, slot, index)))
                .Aggregate(F.DoNothing, (a, b) => a + b));

        static bool HasPattern(AcsNode node, int slot, int index) =>
            node?.Accessories?[slot]?.cusAcsCmp?.HasPattern(index) ?? false;

        static bool HasColor(AcsNode node, int slot, int index) =>
            node?.Accessories?[slot]?.cusAcsCmp?.HasColor(index) ?? false;

        static bool HasColor(this ChaAccessoryComponent cmp, int index) =>
            cmp != null && index switch
            {
                0 => cmp.useColor01,
                1 => cmp.useColor02,
                2 => cmp.useColor03,
                _ => cmp.rendAlpha.Length > 0
            };
        static Action UpdateAction(Action<Color> setter, Func<Color> getter) => () => setter(getter());

        static Func<Color> AcsColorGetter(AcsNode node, int slot, int index) =>
            () => node.nowCoordinate.Accessory.parts?[slot]?.color?[index] ?? default;

        static Action<Color> AcsColorSetter(AcsNode node, int slot, int index) =>
            ColorSetter(node, slot, ChaShader.Accessory.GetMainColorID(index));

        static Func<Color> PtnColorGetter(AcsNode node, int slot, int index) =>
            () => node.nowCoordinate.Accessory.parts?[slot]?.colorInfo?[index].patternColor ?? default;

        static Action<Color> PtnColorSetter(AcsNode node, int slot, int index) =>
            ColorSetter(node, slot, ChaShader.Accessory.GetPatternColorID(index));
        
        static Action<Color> ColorSetter(AcsNode node, int slot, int shaderId) =>
            node.Accessories[slot].renderers
                .Select(renderer => ColorSetter(renderer, shaderId))
                .Aggregate(F.DoNothing.Ignoring<Color>(), (a, b) => a + b);

        static Action<Color> ColorSetter(Renderer renderer, int shaderId) =>
            color => renderer?.material?.SetColor(shaderId, color);
    }

    internal static partial class Hooks
    {
        [HarmonyPrefix, HarmonyWrapSafe]
        [HarmonyPatch(typeof(AcsNode), nameof(AcsNode.ChangeAccessoryPatternTexture), typeof(int), typeof(int))]
        static bool ChangeAccessoryPatternTexturePrefix(AcsNode __instance, int slotNo, int index, bool __result) =>
            (__result = true) && F.Apply(__instance.ChangePtnTexture, slotNo, index).Bypass(slotNo);
    }
    static partial class AccessoryExtension
    {
        internal static void ChangePtnTexture(this AcsNode node, int slot, int index) =>
            Check(node, slot).Maybe((index < 0 ? Enumerable.Range(0, 3) : [index])
                .Select(idx => F.Apply(ChangePtnTexture, node.Accessories[slot],
                    node.nowCoordinate.Accessory.parts[slot], node.human, idx))
                .Aggregate(F.DoNothing, (a, b) => a + b));
        static void ChangePtnTexture(AcsLeaf leaf, AcsPart part, Human human, int index) =>
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
        [HarmonyPatch(typeof(AcsNode), nameof(AcsNode.ChangeAccessoryPatternParameter), typeof(int), typeof(int))]
        static bool ChangeAccessoryPatternParameter(AcsNode __instance, int slotNo, int index, bool __result) =>
            (__result = true) && F.Apply(__instance.ChangePtnParams, slotNo, index).Bypass(slotNo);
    }
    static partial class AccessoryExtension
    {
        internal static void ChangePtnParams(this AcsNode node, int slot, int index) =>
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
            (correctNo < node.Accessories[slot].objAcsMove.Count)
                .Maybe(F.Apply(SetPosition, node.Accessories[slot], correctNo, ToModifier(value, flag, PositionAdjust(add))));
        internal static void SetRotation(this AcsNode node, int slot, int correctNo, float value, bool add, int flag) =>
            (correctNo < node.Accessories[slot].objAcsMove.Count)
                .Maybe(F.Apply(SetRotation, node.Accessories[slot], correctNo, ToModifier(value, flag, RotationAdjust(add))));
        internal static void SetScale(this AcsNode node, int slot, int correctNo, float value, bool add, int flag) =>
            (correctNo < node.Accessories[slot].objAcsMove.Count)
                .Maybe(F.Apply(SetScale, node.Accessories[slot], correctNo, ToModifier(value, flag, ScaleAdjust(add))));
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
            SetupFK(node.Accessories[slot], node.nowCoordinate.Accessory.parts[slot]);
        
        static void SetupFK(AcsLeaf leaf, AcsPart part) =>
            (leaf.cusAcsCmp != null).Maybe(F.Apply(SetupFK, part, leaf.cusAcsCmp.GetFKBonesDef()));

        static void SetupFK(AcsPart part, IlVector3Array bones) =>
            part.fkInfo.bones = part.fkInfo.bones is null || part.fkInfo.bones.Count != bones.Count ? bones : part.fkInfo.bones;
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
            node.Accessories[slot].cusAcsCmp.UpdateFK(values);

        internal static void UpdateFK(this AcsNode node, int slot) =>
            UpdateFK(node.Accessories[slot], node.nowCoordinate.Accessory.parts[slot].fkInfo);

        static void UpdateFK(AcsLeaf leaf, AcsPart.FKInfo info) =>
            leaf.cusAcsCmp.UpdateFK(info.bones);
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
            SetFK(node.Accessories[slot], node.nowCoordinate.Accessory.parts[slot], correctNo, value, add, flag);
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
            __instance._dynamicBones?.Where(bone => bone != null)
                ?.Any(bone => true.With(bone.ResetParticlesPosition)) ?? false;
    }

    [BepInProcess(Process)]
    [BepInDependency(Fishbone.Plugin.Guid)]
    [BepInPlugin(Guid, Name, Version)]
    public partial class Plugin : BasePlugin
    {
        internal static Plugin Instance;
        public const string Name = "VarietyOfScales";
        public const string Guid = $"{Process}.{Name}";
        public const string Version = "0.3.0";
        private Harmony Patch;
        public override bool Unload() =>
                true.With(Patch.UnpatchSelf) && base.Unload();
    }
}