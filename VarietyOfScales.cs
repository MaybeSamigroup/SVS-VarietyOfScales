using System;
using System.Linq;
using System.Reactive.Disposables;
using UnityEngine;
using Character;
using HarmonyLib;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using Fishbone;
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
        internal static bool Check(this AcsNode node, int slot) =>
            slot < node.Accessories.Count && Check(node.Accessories[slot]);
        static bool Check(AcsLeaf leaf) =>
            ((ChaListDefine.CategoryNo?)(leaf?.infoAccessory?.Category) ?? ChaListDefine.CategoryNo.ao_none) is not ChaListDefine.CategoryNo.ao_none;
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
                F.Apply(Remove, node, slot) +
                F.Apply(Remove, node.nowCoordinate.Accessory.parts[slot], slot) +
                F.Apply(NotifySlotRemove, slot),
                F.Apply(Assign, node, slot, category, id, parent is Parent.none ?
                    GetInfo(node.human, category, id, ChaListDefine.KeyType.Parent, out var value)
                        && Enum.TryParse<Parent>(value, out var defaultParent) ? defaultParent : Parent.RootBone : parent));
        static void Remove(AcsNode node, int slot) =>
            node.Accessories[slot] = new AcsLeaf();
        static void Remove(AcsPart part, int slot) =>
            part.Copy(AcsNode.NoneAcsData);
        static void Assign(AcsNode node, int slot, ChaListDefine.CategoryNo category, int id, Parent parent) =>
            Assign(node.nowCoordinate.Accessory.parts[slot], slot, node,
                node.Accessories[slot] = new AcsLeaf(node.human, category, id, slot,
                GetInfo(node.human, category, id, ChaListDefine.KeyType.WeightType, out var data)
                    && Enum.TryParse<Human.UseCopyWeightType>(data, out var value) ?
                        value : Human.UseCopyWeightType.None, ToTransform(node, parent.ToString())), parent);
        static void Assign(AcsPart part, int slot, AcsNode node, AcsLeaf leaf, Parent parent) => (
            F.Apply(PostChange, node.human, part, leaf) +
            F.Apply(ChangeAcsColor, node, slot) +
            F.Apply(ChangePtnTexture, node, slot, -1) +
            F.Apply(ChangePtnColor, node, slot, -1) +
            F.Apply(ChangePtnParams, node, slot, -1) +
            F.Apply(ChangeParent, part, parent) +
            F.Apply(ApplyMove, node, slot) +
            F.Apply(ApplyDynamicBones, node.Accessories[slot], !(part.noShake || part.fkInfo.use)) +
            F.Apply(SetupFK, leaf, part) +
            F.Apply(ApplyFK, leaf, part) +
            F.Apply(NotifySlotAssign, slot, part)
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
            Extension<CharaMods, CoordMods>.Humans.NowCoordinate[node.human].Slots.TryGetValue(slot, out var move)
                .Either(F.Apply(node.UpdateAccessoryMoveFromInfo, slot).Ignoring(), F.Apply(ApplyMove, node.Accessories[slot], move));
        static void ApplyMove(AcsLeaf leaf, MoveMod mod) => mod.Apply(leaf);
        static void ChangeParent(AcsPart part, Parent parent) =>
            (part.parentKeyType, part.partsOfHead) = ((int)parent, ChaAccessoryDefine.CheckPartsOfHead(parent));
        internal static void ChangeParent(this AcsNode node, int slot, Parent parent) =>
            node.Accessories[slot].objAccessory.transform.SetParent(ToTransform(node, parent.ToString()), false);
        static Transform ToTransform(AcsNode node, string parent) =>
            node.GetRefTransform(Enum.TryParse<Table.RefObjKey>(parent, out var value) ? value : Table.RefObjKey.RootBone);
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
        internal static void SetupFK(this AcsNode node, int slot) =>
            SetupFK(node.Accessories[slot], node.nowCoordinate.Accessory.parts[slot]);
        static void SetupFK(AcsLeaf leaf, AcsPart part) =>
            (leaf.cusAcsCmp != null).Maybe(F.Apply(SetupFK, part, leaf.cusAcsCmp.GetFKBonesDef()));
        static void SetupFK(AcsPart part, IlVector3Array bones) =>
            part.fkInfo.bones = part.fkInfo.bones is null || part.fkInfo.bones.Count != bones.Count ? bones : part.fkInfo.bones;
    }

    internal static partial class Hooks
    {
        internal static IDisposable Initialize() =>
            Disposable.Create(Harmony.CreateAndPatchAll(typeof(Hooks), $"{Plugin.Name}.Hooks").UnpatchSelf);
    }

    [BepInProcess(Process)]
    [BepInDependency(Fishbone.Plugin.Guid)]
    [BepInPlugin(Guid, Name, Version)]
    public partial class Plugin : BasePlugin
    {
        internal static Plugin Instance;
        public const string Name = "VarietyOfScales";
        public const string Guid = $"{Process}.{Name}";
        public const string Version = "0.8.0";
        CompositeDisposable Subscriptions = new (); 
        public Plugin() : base() => Instance = this;
        public override void Load() => Subscriptions = [
            Hooks.Initialize(), ..AccessoryExtension.Initialize(this)
        ];
        public override bool Unload() =>
            true.With(Subscriptions.Dispose) && base.Unload();
    }
}