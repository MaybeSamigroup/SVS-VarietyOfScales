using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Disposables;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Character;
using CharacterCreation;
using CharacterCreation.UI;
using CharacterCreation.UI.View.Accessory;
using HarmonyLib;
using BepInEx.Unity.IL2CPP;
using BepInEx.Configuration;
using Fishbone;
using CoastalSmell;
using Parent = ChaAccessoryDefine.AccessoryParentKey;
using AcsNode = Character.HumanAccessory;
using AcsLeaf = Character.HumanAccessory.Accessory;
using AcsData = Character.HumanDataAccessory;
using AcsPart = Character.HumanDataAccessory.PartsInfo;
using IlVector3Array = Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<UnityEngine.Vector3>;

namespace VarietyOfScales
{
    static partial class AccessoryExtension
    {
        internal static (bool, T) Bypass<T>(this Func<T> action, int slotNo) =>
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
            node.Accessories[slot].cusAcsCmp == null ? new AcsNode.DefaultColorData() : new AcsNode.DefaultColorData(node.Accessories[slot].cusAcsCmp);
    }
    internal static partial class Hooks
    {
        [HarmonyPrefix, HarmonyWrapSafe]
        [HarmonyPatch(typeof(AcsNode), nameof(AcsNode.IsAccessory), typeof(int))]
        static bool IsAccessory(AcsNode __instance, int slotNo, ref bool __result) =>
            ((_, __result) = F.Apply(__instance.Check, slotNo).Bypass(slotNo)).Item1;
    }
    internal static partial class Hooks
    {
        [HarmonyPostfix, HarmonyWrapSafe]
        [HarmonyPatch(typeof(AcsNode), nameof(AcsNode.ChangeAccessory), typeof(bool))]
        static void ChangeAccessoryPostfix(AcsNode __instance, bool forceChange) =>
            Enumerable.Range(0, __instance.Accessories.Count)
                .Where(slot => slot >= ChaFileDefine.AccessorySlotNum).ForEach(slot => __instance.Change(slot, forceChange));

        [HarmonyPrefix, HarmonyWrapSafe]
        [HarmonyPatch(typeof(AcsNode), nameof(AcsNode.ChangeAccessory), typeof(int), typeof(int), typeof(int), typeof(Parent), typeof(bool))]
        static bool ChangeAccessoryPrefix(AcsNode __instance, int slotNo, int type, int id, Parent parentKey, bool forceChange) =>
            F.Apply(__instance.Change, slotNo, type, id, parentKey, forceChange).Bypass(slotNo);
    }
    internal static partial class Hooks
    {
        [HarmonyPrefix, HarmonyWrapSafe]
        [HarmonyPatch(typeof(AcsNode), nameof(AcsNode.ChangeAccessoryParent), typeof(int), typeof(Parent))]
        static bool ChangeAccessoryParentPrefix(AcsNode __instance, int slotNo, Parent parentKey, bool __result) =>
            (__result = true) && F.Apply(__instance.ChangeParent, slotNo, parentKey).Bypass(slotNo);
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
    internal static partial class Hooks
    {
        [HarmonyPrefix, HarmonyWrapSafe]
        [HarmonyPatch(typeof(AcsNode), nameof(AcsNode.ChangeAccessoryPatternTexture), typeof(int), typeof(int))]
        static bool ChangeAccessoryPatternTexturePrefix(AcsNode __instance, int slotNo, int index, bool __result) =>
            (__result = true) && F.Apply(__instance.ChangePtnTexture, slotNo, index).Bypass(slotNo);
    }
    internal static partial class Hooks
    {
        [HarmonyPrefix, HarmonyWrapSafe]
        [HarmonyPatch(typeof(AcsNode), nameof(AcsNode.ChangeAccessoryPatternParameter), typeof(int), typeof(int))]
        static bool ChangeAccessoryPatternParameter(AcsNode __instance, int slotNo, int index, bool __result) =>
            (__result = true) && F.Apply(__instance.ChangePtnParams, slotNo, index).Bypass(slotNo);
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
    internal static partial class Hooks
    {
        [HarmonyPrefix, HarmonyWrapSafe]
        [HarmonyPatch(typeof(AcsLeaf), nameof(AcsLeaf.ResetCloth))]
        static bool AccessoryResetCloth(AcsLeaf __instance) =>
            __instance._dynamicBones?.Where(bone => bone != null)
                ?.Any(bone => true.With(bone.ResetParticlesPosition)) ?? false;
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
        [HarmonyPatch(typeof(AcsEdit), nameof(AcsEdit.SetColorWindow))]
        static bool SetColorWindowPrefix(AcsEdit __instance, int slotNo, int index, ThumbnailColor acsColors, Il2CppSystem.Func<bool> updateUI) =>
            F.Apply(acsColors.InitAcs, __instance._humanAcs, slotNo, index, updateUI).Bypass(slotNo);
        [HarmonyPrefix, HarmonyWrapSafe]
        [HarmonyPatch(typeof(PatternEdit), nameof(PatternEdit.SetColorPtnWindow))]
        static bool SetColorPtnWindowPrefix(PatternEdit __instance, int slotNo, int index, ThumbnailColor ptnColor, Il2CppSystem.Func<bool> updateUI) =>
            F.Apply(ptnColor.InitPtn, __instance._humanAcs, slotNo, index, updateUI).Bypass(slotNo);
    }
    static partial class AccessoryExtension
    {
        internal static void InitAcs(this ThumbnailColor ui, AcsNode node, int slot, int index, Il2CppSystem.Func<bool> updateUI) =>
            ui.Initialize($"Slot{slot + 1}/Color{index}",
                AcsColorGetter(node.nowCoordinate.Accessory.parts[slot], index).With(ui.SetThumbnailColor),
                ColorSetter(
                    AcsColorSetter(node.human.data, slot, index) +
                    AcsColorSetter(node.nowCoordinate.Accessory.parts[slot], index) +
                    ColorSetter(node, slot, ChaShader.Accessory.GetMainColorID(index)), updateUI), index > 2, false);
        internal static void InitPtn(this ThumbnailColor ui, AcsNode node, int slot, int index, Il2CppSystem.Func<bool> updateUI) =>
            ui.Initialize($"Slot{slot + 1}/Pattern{index}",
                PtnColorGetter(node.nowCoordinate.Accessory.parts[slot], index).With(ui.SetThumbnailColor),
                ColorSetter(
                    PtnColorSetter(node.human.data, slot, index) +
                    PtnColorSetter(node.nowCoordinate.Accessory.parts[slot], index) +
                    ColorSetter(node, slot, ChaShader.Accessory.GetPatternColorID(index)), updateUI), true, false);
        static void SetThumbnailColor(this ThumbnailColor ui, Func<Color> getter) =>
            ui.SetGraphic(getter().With(HumanCustom.Instance.ColorPicker.SetPickerColor));
        static void SetPickerColor(this CustomColorPicker ui, Color color) =>
            ui.SetColor(ref color);
        static Func<Color> AcsColorGetter(AcsPart part, int index) =>
            () => part.color[index];
        static Action<Color> AcsColorSetter(HumanData data, int slot, int index) =>
            AcsColorSetter(data.Coordinates[data.Status.coordinateType].Accessory.parts[slot], index);
        static Action<Color> AcsColorSetter(AcsPart part, int index) =>
            color => part.color[index] = color;
        static Func<Color> PtnColorGetter(AcsPart part, int index) =>
            () => part.colorInfo[index].patternColor;
        static Action<Color> PtnColorSetter(HumanData data, int slot, int index) =>
            PtnColorSetter(data.Coordinates[data.Status.coordinateType].Accessory.parts[slot], index);
        static Action<Color> PtnColorSetter(AcsPart part, int index) =>
            color => part.colorInfo[index].patternColor = color;
        static Func<Color, bool> ColorSetter(Action<Color> action, Il2CppSystem.Func<bool> update) =>
            color => update.With(action.Apply(color)).Invoke();
    }
    internal static partial class Hooks
    {
        static Func<int, Func<HumanData, float>, Func<HumanData, float>> DefaultReset = (_, f) => f;
        static Func<int, Func<HumanData, float>, Func<HumanData, float>> PatternReset = DefaultReset;
        static int ParameterIndex = -1;

        [HarmonyPrefix, HarmonyWrapSafe]
        [HarmonyPatch(typeof(PatternEdit), nameof(PatternEdit.Setting))]
        static void PatternEditSettingPrefix(PatternEdit __instance, int slotNo, int index) =>
            (ParameterIndex, PatternReset) = (0, (value, original) => (value, index) switch
            {
                (0, 0) => data => __instance._humanAcs.Accessories[slotNo].cusAcsCmp.pattern01._offset.x,
                (1, 0) => data => __instance._humanAcs.Accessories[slotNo].cusAcsCmp.pattern01._offset.y,
                (2, 0) => data => __instance._humanAcs.Accessories[slotNo].cusAcsCmp.pattern01._rotate,
                (3, 0) => data => __instance._humanAcs.Accessories[slotNo].cusAcsCmp.pattern01._tiling.x,
                (4, 0) => data => __instance._humanAcs.Accessories[slotNo].cusAcsCmp.pattern01._tiling.y,
                (0, 1) => data => __instance._humanAcs.Accessories[slotNo].cusAcsCmp.pattern02._offset.x,
                (1, 1) => data => __instance._humanAcs.Accessories[slotNo].cusAcsCmp.pattern02._offset.y,
                (2, 1) => data => __instance._humanAcs.Accessories[slotNo].cusAcsCmp.pattern02._rotate,
                (3, 1) => data => __instance._humanAcs.Accessories[slotNo].cusAcsCmp.pattern02._tiling.x,
                (4, 1) => data => __instance._humanAcs.Accessories[slotNo].cusAcsCmp.pattern02._tiling.y,
                (0, 2) => data => __instance._humanAcs.Accessories[slotNo].cusAcsCmp.pattern03._offset.x,
                (1, 2) => data => __instance._humanAcs.Accessories[slotNo].cusAcsCmp.pattern03._offset.y,
                (2, 2) => data => __instance._humanAcs.Accessories[slotNo].cusAcsCmp.pattern03._rotate,
                (3, 2) => data => __instance._humanAcs.Accessories[slotNo].cusAcsCmp.pattern03._tiling.x,
                (4, 2) => data => __instance._humanAcs.Accessories[slotNo].cusAcsCmp.pattern03._tiling.y,
                _ => original
            });
        [HarmonyPrefix, HarmonyWrapSafe]
        [HarmonyPatch(typeof(CategoryEdit), nameof(CategoryEdit.CreateCategory))]
        static void CategoryEditCreateCategoryPrefix(CategoryEdit.NowCategory nowCategory) =>
            (ParameterIndex, PatternReset) = HumanCustom.Instance.NowCategory.Category == 4 &&
                nowCategory.DataList.Yield().Select(item => item.Title)
                    .ToArray().CheckEditTitles() ? (ParameterIndex, PatternReset) : (-1, DefaultReset);
        [HarmonyPrefix, HarmonyWrapSafe]
        [HarmonyPatch(typeof(InputSliderButton), nameof(InputSliderButton.Initialize))]
        static void InputSliderButtonInitializePrefix(ref Il2CppSystem.Func<HumanData, float> resetValue) =>
            (ParameterIndex, resetValue) = (ParameterIndex + 1, PatternReset(ParameterIndex, resetValue.Invoke));
    }
    static partial class AccessoryExtension
    {
        internal static bool CheckEditTitles(this string[] titles) =>
            3 == titles.Length
                && titles[0].Equals(CategoryEdit.CategoryData.GetTitle(CategoryEdit.CategoryData.TitleID.Kind))
                && titles[1].Equals(CategoryEdit.CategoryData.GetTitle(CategoryEdit.CategoryData.TitleID.Color))
                && titles[2].Equals(CategoryEdit.CategoryData.GetTitle(CategoryEdit.CategoryData.TitleID.Correct));
    }
    internal static partial class Hooks
    {
        [HarmonyPrefix, HarmonyWrapSafe]
        [HarmonyPatch(typeof(AccessoryParentWindow), nameof(AccessoryParentWindow.Get), typeof(int))]
        static bool GetAccessoryParent(AccessoryParentWindow __instance, int slotNo, ref int __result) =>
            (__result = __instance._acsData.parts[slotNo].parentKeyType) is not 0;
        [HarmonyPrefix, HarmonyWrapSafe]
        [HarmonyPatch(typeof(ParentEdit), nameof(ParentEdit.ChangeAccessoryParent), typeof(int))]
        static bool ChangeAccessoryParentPrefix(ParentEdit __instance, int slotNo) =>
            F.Apply(__instance._humanAcs.ChangeParent, __instance._acsData,
                slotNo, __instance._accessoryParentWindow.CurrentSelection()).Bypass(slotNo);
    }
    static partial class AccessoryExtension
    {
        internal static Parent CurrentSelection(this AccessoryParentWindow ui) =>
            Enum.TryParse<Parent>(ui._toggleGroup.onList.ToArray().Last().name.Split("_").Last(), out var value) ? value + 1 : Parent.RootBone;
        internal static void ChangeParent(this AcsNode node, AcsData data, int slot, Parent parent) =>
            (F.Apply(ChangeParent, data.parts[slot], parent) + F.Apply(node.ChangeParent, slot, parent)).Invoke();
    }
    internal static partial class Hooks
    {
        [HarmonyPostfix, HarmonyWrapSafe]
        [HarmonyPatch(typeof(AccessoryMoveWindow), nameof(AccessoryMoveWindow.SetControllerTransform))]
        static void AccessoryMoveWindowSetControllerTransformPostfix(AccessoryMoveWindow __instance, int slotNo, int editNo) =>
            F.Apply(__instance._humanAcs.UpdateMoveUI, slotNo, editNo, __instance._guidList[editNo],
                __instance._movePairs.Where(pair => pair.Active).Select(pair => pair.MoveGroup).ToArray()).Bypass(slotNo);
    }
    static partial class AccessoryExtension
    {
        internal static void UpdateMoveUI(this AcsNode node, int slot, int index, GuideObject guide, MoveGroup[] moves) =>
            UpdateMoveUI(node.Accessories[slot].objAcsMove[index].With(guide.Amount.Set), moves);
        static void UpdateMoveUI(Transform tf, MoveGroup[] moves) => tf
            .With(UpdatePosition(moves[0].gameObject))
            .With(UpdateRotation(moves[1].gameObject))
            .With(UpdateScale(moves[2].gameObject));
        static Action<Transform> UpdatePosition(GameObject go) => tf =>
            go.With(UpdateMoveUI("0.#", tf.localPosition * 100.0f));
        static Action<Transform> UpdateRotation(GameObject go) => tf =>
            go.With(UpdateMoveUI("0", tf.localEulerAngles));
        static Action<Transform> UpdateScale(GameObject go) => tf =>
            go.With(UpdateMoveUI("0.##", tf.localScale));
        static UIAction UpdateMoveUI(string format, Vector3 values) =>
            UpdateMoveUI(values.x.ToString(format), "Controller", "Move", "X", "InputField_Decimal") +
            UpdateMoveUI(values.y.ToString(format), "Controller", "Move", "Y", "InputField_Decimal") +
            UpdateMoveUI(values.z.ToString(format), "Controller", "Move", "Z", "InputField_Decimal");
        static UIAction UpdateMoveUI(string value, params string[] paths) => go =>
            UGUI.Component<TMP_InputField>(cmp => cmp.SetText(value, false)).At(paths);
    }
    internal static partial class Hooks
    {
        [HarmonyPostfix, HarmonyWrapSafe]
        [HarmonyPatch(typeof(AccessoryMoveWindow), nameof(AccessoryMoveWindow.UpdateCustomUI), [])]
        static void AccessoryMoveWindowUpdateCustomUIPostfix(AccessoryMoveWindow __instance) =>
            AccessoryExtension.Bypass(__instance.PrepareMoveEvents, __instance._slotNo);
    }
    static partial class AccessoryExtension
    {
        internal static void PrepareMoveEvents(this AccessoryMoveWindow ui) =>
            ui._humanAcs.Accessories[ui._slotNo].objAcsMove[ui._editNo]
                .With(PreparePositionEvent(ui._movePairs[0]._moveGroup, ui._disposables))
                .With(PrepareRotationEvent(ui._movePairs[1]._moveGroup, ui._disposables))
                .With(PrepareScaleEvent(ui._movePairs[2]._moveGroup, ui._disposables));
        static Action<Transform> PreparePositionEvent(MoveGroup ui, UniRx.CompositeDisposable disps) => tf =>
            tf.With(UpdatePosition(ui.gameObject.With(UpdateMoveEvents(disps, "0",
                (value) => tf.localPosition = new(value / 100f, tf.localPosition.y, tf.localPosition.z),
                (value) => tf.localPosition = new(tf.localPosition.x, value / 100f, tf.localPosition.z),
                (value) => tf.localPosition = new(tf.localPosition.x, tf.localPosition.y, value / 100f)))));
        static Action<Transform> PrepareRotationEvent(MoveGroup ui, UniRx.CompositeDisposable disps) => tf =>
            tf.With(UpdateRotation(ui.gameObject.With(UpdateMoveEvents(disps, "0",
                (value) => tf.localEulerAngles = new(value, tf.localEulerAngles.y, tf.localEulerAngles.z),
                (value) => tf.localEulerAngles = new(tf.localEulerAngles.x, value, tf.localEulerAngles.z),
                (value) => tf.localEulerAngles = new(tf.localEulerAngles.x, tf.localEulerAngles.y, value)))));
        static Action<Transform> PrepareScaleEvent(MoveGroup ui, UniRx.CompositeDisposable disps) => tf =>
            tf.With(UpdateScale(ui.gameObject.With(UpdateMoveEvents(disps, "1",
                (value) => tf.localScale = new(value, tf.localScale.y, tf.localScale.z),
                (value) => tf.localScale = new(tf.localScale.x, value, tf.localScale.z),
                (value) => tf.localScale = new(tf.localScale.x, tf.localScale.y, value)))));
        static Action<GameObject> UpdateMoveEvents(UniRx.CompositeDisposable disps,
            string value, Action<float> setX, Action<float> setY, Action<float> setZ) => go =>
            disps.Add(UniRx.Disposable.Create((Action)new CompositeDisposable([
                ..SubscribeEvents(go, "X", value, setX),
                ..SubscribeEvents(go, "Y", value, setY),
                ..SubscribeEvents(go, "Z", value, setZ),
            ]).Dispose));
        static IDisposable[] SubscribeEvents(GameObject go, string axis, string value, Action<float> update) =>
            SubscribeEvents(value, update,
                go.TransformAt("Controller", "Move", axis, "btnDefault").GetComponent<Button>(),
                go.TransformAt("Controller", "Move", axis, "InputField_Decimal").GetComponent<TMP_InputField>());
        static IDisposable[] SubscribeEvents(string value, Action<float> update, Button reset, TMP_InputField input) => [
            reset.OnClickAsObservable().Subscribe(_ => input.SetText(value, true)),
            input.OnValueChangedAsObservable().Subscribe(text => float.TryParse(text, out var value).Maybe(F.Apply(update, value)))
        ];
    }
    internal static partial class Hooks
    {
        [HarmonyPrefix, HarmonyWrapSafe]
        [HarmonyPatch(typeof(AccessoryFKWindow), nameof(AccessoryFKWindow.UpdateCustomUI))]
        static void AccessoryFKWindowUpdateCustomUIPrefix(AccessoryFKWindow __instance) =>
            AccessoryExtension.PrepareFK(__instance._human, __instance._humanAcs, __instance._acsData, __instance._slotNo);
        [HarmonyPrefix, HarmonyWrapSafe]
        [HarmonyPatch(typeof(AccessoryFKWindow), nameof(AccessoryFKWindow.UpdateAcsAllReset))]
        static bool AccessoryFKWindowUpdateAcsAllResetPrefix(AccessoryFKWindow __instance, int slotNo, int editNo) =>
            F.Apply(__instance._humanAcs.ResetFK, slotNo, editNo).Bypass(slotNo);
        [HarmonyPrefix, HarmonyWrapSafe]
        [HarmonyPatch(typeof(AccessoryFKWindow), nameof(AccessoryFKWindow.UpdateAcsMovePaste))]
        static bool AccessoryFKWindowUpdateAcsMovePastePrefix(AccessoryFKWindow __instance, int slotNo, int editNo, Vector3 value) =>
            F.Apply(__instance._humanAcs.SetFK, slotNo, editNo, value).Bypass(slotNo);
        [HarmonyPrefix, HarmonyWrapSafe]
        [HarmonyPatch(typeof(AccessoryFKWindow), nameof(AccessoryFKWindow.UpdateAcsRotAdd))]
        static bool AccessoryFKAWindowUpdateAcsRotAddPrefix(AccessoryFKWindow __instance, int slotNo, int editNo, int xyz, bool add, float val) =>
            F.Apply(__instance._humanAcs.SetFK, slotNo, editNo, val, add, 1 << xyz).Bypass(slotNo);
    }
    static partial class AccessoryExtension
    {
        internal static void PrepareFK(Human human, AcsNode node, AcsData data, int slot) =>
            human.data.Coordinates[human.data.Status.coordinateType].Accessory.parts[slot].fkInfo.bones =
            data.parts[slot].fkInfo.bones = node.nowCoordinate.Accessory.parts[slot].fkInfo.bones =
                node.Accessories[slot].cusAcsCmp.FKBone.Select(tf => tf.localEulerAngles).ToArray(); 
        internal static void ResetFK(this AcsNode node, int slot, int index) =>
            node.SetFK(slot, index, node.Accessories[slot].cusAcsCmp.GetFKBonesDef()[index]);
        internal static void SetFK(this AcsNode node, int slot, int index, Vector3 value) =>
            ToFKParams(value).ForEach(ps => node.SetAccessoryFK(slot, index, ps.Item1, ps.Item2, ps.Item3));
        static IEnumerable<Tuple<float, bool, int>> ToFKParams(Vector3 value) =>
            [new(value.x, false, 1), new(value.y, false, 2), new(value.z, false, 4)];
    }
    internal static partial class Hooks
    {
        [HarmonyPrefix]
        [HarmonyWrapSafe]
        [HarmonyPatch(typeof(Accessory_00), nameof(Accessory_00.SetDefaultAcsColor))]
        static void UpdateAccessoryPrefix(int slotNo) =>
            AccessoryExtension.SetDefaultAcsColor(HumanCustom.Instance.Human.acs, slotNo);
    }
    static partial class AccessoryExtension
    {
        internal static void SetDefaultAcsColor(AcsNode node, int slot) =>
            Check(node, slot).Maybe(
                F.Apply(ApplyDefaults, node.nowCoordinate.Accessory.parts[slot], node.Accessories[slot]) +
                F.Apply(ChangePtnTexture, node, slot, -1) +
                F.Apply(ChangePtnColor, node, slot, -1) +
                F.Apply(ChangePtnParams, node, slot, -1));
    }
    internal static partial class Hooks
    {
        [HarmonyPrefix]
        [HarmonyWrapSafe]
        [HarmonyPatch(typeof(Accessory_01), nameof(Accessory_01.CopyAccessory))]
        static void CopyAccessoryPrefix(Accessory_01 __instance) =>
            __instance._humanAcs.StoreCurrentMove(__instance._selDst, __instance._selSrc);
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        [HarmonyPatch(typeof(Accessory_01), nameof(Accessory_01.CopyAccessory))]
        static void CopyAccessoryPostfix(Accessory_01 __instance) =>
            __instance._humanAcs.CopyAndApplyMove(__instance._selDst, __instance._selSrc, -1, +1f, +1f);
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        [HarmonyPatch(typeof(Accessory_01), nameof(Accessory_01.CopyAcsCorrect))]
        static void CopyAcsMCorrectPostfix(Accessory_01 __instance, int editNo) =>
            __instance._humanAcs.CopyAndApplyMove(__instance._selDst, __instance._selSrc, editNo, +1f, +1f);
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        [HarmonyPatch(typeof(Accessory_01), nameof(Accessory_01.CopyAcsCorrectRevLR))]
        static void CopyAcsMCorrectRevLRPostfix(Accessory_01 __instance, int editNo) =>
            __instance._humanAcs.CopyAndApplyMove(__instance._selDst, __instance._selSrc, editNo, -1f, +1f);
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        [HarmonyPatch(typeof(Accessory_01), nameof(Accessory_01.CopyAcsCorrectRevUD))]
        static void CopyAcsMCorrectRevUDPostfix(Accessory_01 __instance, int editNo) =>
            __instance._humanAcs.CopyAndApplyMove(__instance._selDst, __instance._selSrc, editNo, +1f, -1f);
    }
    static partial class AccessoryExtension
    {
        internal static void StoreCurrentMove(this AcsNode node, int dstSlot, int srcSlot) =>
            Extension<CharaMods, CoordMods>.Humans.NowCoordinate[node.human].Store(node, new int[] { dstSlot, srcSlot }.Where(slot => Check(node, slot)));
        internal static void CopyAndApplyMove(this AcsNode node, int dstSlot, int srcSlot,  int editNo, float mx, float my) =>
            (ChaFileDefine.AccessorySlotNum < srcSlot || ChaFileDefine.AccessorySlotNum < dstSlot)
                .Maybe(F.Apply(CopyAndApplyMove, node.Accessories[dstSlot], node.Accessories[srcSlot], editNo, ToModifier(mx, my)));
        static Func<Vector3, Vector3> ToModifier(float mx, float my) => v3 => new(v3.x * mx, v3.y * my, v3.z);
        static void CopyAndApplyMove(AcsLeaf dst, AcsLeaf src, int editNo, Func<Vector3, Vector3> modifier) =>
            (editNo < 0 ? Enumerable.Range(0, Math.Min(dst.objAcsMove.Length, src.objAcsMove.Length)) : [editNo]).ForEach(index =>
                (dst.objAcsMove[index].localScale, dst.objAcsMove[index].localEulerAngles, dst.objAcsMove[index].localPosition) =
                (src.objAcsMove[index].localScale, src.objAcsMove[index].localEulerAngles, modifier(src.objAcsMove[index].localPosition)));
        internal static void CopyMove(this Human human, int dstCoord, int srcCoord, int slot) =>
            Extension<CharaMods, CoordMods>.Humans[human].CopyMove(dstCoord, srcCoord, slot);
    }
    internal static partial class Hooks
    {
        [HarmonyPrefix]
        [HarmonyWrapSafe]
        [HarmonyPatch(typeof(CoordinateCopyAccessory), nameof(CoordinateCopyAccessory.OnCopy))]
        static void CoordinateCopyAccessoryOnCopy(CoordinateCopyAccessory __instance) =>
            (__instance.GetSrcIndex() == HumanCustom.Instance.Human.data.Status.coordinateType)
                .Maybe(() => F.Apply(CoordMods.Save, HumanCustom.Instance.Human));
        [HarmonyPrefix]
        [HarmonyWrapSafe]
        [HarmonyPatch(typeof(CoordinateCopyAccessory), nameof(CoordinateCopyAccessory.CopyInstance))]
        static bool CoordinateCopyAccessoryCopyInstancePrefix(CoordinateCopyAccessory __instance, int index, AcsData dst, AcsData src) =>
            index < dst.parts.Count && index < src.parts.Count &&
                true.With(F.Apply(HumanCustom.Instance.Human.CopyMove, __instance.GetDstIndex(), __instance.GetSrcIndex(), index));
    }
    static partial class AccessoryExtension
    {
        static bool GetInfo(Human _, ChaListDefine.CategoryNo category, int id, ChaListDefine.KeyType key, out string value) =>
            Human.lstCtrl.GetInfo(category, id, key, out value);
        static Subject<(int Slot, bool State)> SlotStateChange = new();
        static void NotifySlotState(int slots) =>
            Enumerable.Range(20, 79).ForEach(index => SlotStateChange.OnNext((index, index < slots)));
        internal static IObservable<(int Slot, bool State)> OnSlotStateChange => SlotStateChange.AsObservable();
        static string ToSlotLabel(AcsPart part) =>
            Human.lstCtrl.GetListInfo((ChaListDefine.CategoryNo)part.type, part.id)?.GetInfo(ChaListDefine.KeyType.Name) ?? "";
        static Subject<(int Slot, string Name)> SlotLabelChange = new();
        static void NotifySlotRemove(int slot) =>
            SlotLabelChange.OnNext((slot, ""));
        static void NotifySlotAssign(int slot, AcsPart part) =>
            SlotLabelChange.OnNext((slot, ToSlotLabel(part)));
        internal static IObservable<(int Slot, string Name)> OnSlotLabelChange =>
            SlotLabelChange.AsObservable();
        internal static string ToSlotLabel(this Human human, int slot) =>
            ToSlotLabel(human, human.data.Status.coordinateType, slot);
        internal static string ToSlotLabel(this Human human, int index, int slot) =>
            Check(human.acs, slot) ? ToSlotLabel(human.data.Coordinates[index].Accessory.parts[slot]) : "";
    }
    internal static class UI
    {
        static void DisposeOnCustomDestroy(IDisposable disposable) =>
            HumanCustom.Instance.OnDestroyAsObservable().Subscribe(F.Ignoring<Unit>(disposable.Dispose));
        static UIAction SubscribeSlotState(int slot) =>
            go => AccessoryExtension.OnSlotStateChange
                .Where(tuple => tuple.Slot == slot)
                .Select(tuple => tuple.State)
                .Subscribe(go.SetActive).With(DisposeOnCustomDestroy);
        static GameObject DuplicateSlotUI(GameObject reference, int slot) =>
            UnityEngine.Object.Instantiate(reference, reference.transform.parent)
                .With(UGUI.GameObject(active: slot < HumanCustom.Instance.Human.acs.Accessories.Count))
                .With(SubscribeSlotState(slot));
        internal static void PrepareAcsUI(CategorySelectionTop top) =>
            DisposeOnCustomDestroy(PrepareAcsUI(top.transform.Find("04_Accessories").GetComponent<CategorySelection>()));
       static IDisposable PrepareAcsUI(CategorySelection selection) =>
            selection.With(PrepareScrollPanel).OnEnableAsObservable()
                .FirstAsync().Select(_ => selection)
                .Subscribe(ConfigureAcsUI + ConfigureAcsUIIndices);
        static Action<CategorySelection> ConfigureAcsUI =
            selection => selection.gameObject
                .With("Control".AsChild(
                    UGUI.Size(width: 250, height: 650) +
                    UGUI.Rt(sizeDelta: new(250, 650)) +
                    UGUI.LayoutV(padding: UGUI.Offset(0, 0, 0, 10)) +
                    selection.transform.GetChild(1).AsChild() +
                    PrepareIndices(selection._root)));
        static Action<CategorySelection> ConfigureAcsUIIndices =
            selection => selection._root.With(UGUI.LayoutV(childControlWidth: false) + UGUI.Size(width: 245));
        static UIAction PrepareIndices(Transform index) =>
            RelocateControls(index) + ExchangeBackground(index.GetChild(0).GetComponent<Image>());
        static UIAction ExchangeBackground(Image image) =>
            UGUI.Image(color: image.color, type: image.type, sprite: image.sprite) +
                new UIAction(_ => UnityEngine.Object.Destroy(image.gameObject));
        static UIAction RelocateControls(Transform index) =>
            Enumerable.Range(index.childCount - 3, 3).Select(index.GetChild)
                .Select(tf => UGUI.AsChild(tf.With(UGUI.Size(width: 240, height: 26)))).Aggregate();
        static void PrepareScrollPanel(CategorySelection selection) =>
            selection._root.With(DuplicateSlotUI).parent
                .With("Slots".AsChild(UGUI.ClearPanel + UGUI.Scroll(250, 550, selection._root.AsChild())));
        static void DuplicateSlotUI(Transform tf) =>
            Enumerable.Range(20, 79).ForEach(slot =>
                DuplicateSlotUI(tf.GetChild(2).gameObject, slot)
                    .With(SetSlotLabel(slot))
                    .With(SubscribeSlotLabel(slot))
                    .transform.SetSiblingIndex(slot + 2));
        static UIAction SetSlotLabel(int slot) =>
            SetSlotLabel(HumanCustom.Instance.Human.ToSlotLabel(slot), slot);
        static UIAction SetSlotLabel(string name, int slot) =>
            go => go.GetComponent<CategoryKindToggle>().Title =
                name is "" ? HumanCustom.Instance.GetTLSlotTitle(slot) : name;
        static UIAction SubscribeSlotLabel(int slot) =>
            go => AccessoryExtension.OnSlotLabelChange
                .Where(tuple => tuple.Slot == slot)
                .Subscribe(tuple => go.With(SetSlotLabel(tuple.Name, tuple.Slot)))
                .With(DisposeOnCustomDestroy);
        static string NoneSlotLabel =>
            Human.lstCtrl.GetListInfo(ChaListDefine.CategoryNo.ao_none, 0)?.GetInfo(ChaListDefine.KeyType.Name) ?? "";
        static UIAction SetSlotCopyLabel(int slot) =>
            SetSlotCopyLabel(HumanCustom.Instance.Human.ToSlotLabel(slot));
        static UIAction SetSlotCopySrcLabel(int index, int slot) =>
            SetSlotCopySrcLabel(HumanCustom.Instance.Human.ToSlotLabel(index, slot));
        static UIAction SetSlotCopyDstLabel(int index, int slot) =>
            SetSlotCopyDstLabel(HumanCustom.Instance.Human.ToSlotLabel(index, slot));
        static UIAction SetSlotCopyLabel(string name) =>
            UGUI.Component<AccessoryCopyComponent>(cmp => cmp.SetText(name is "" ? NoneSlotLabel : name));
        static UIAction SetSlotCopySrcLabel(string name) =>
            UGUI.Component<CoordinateCopyComponent>(cmp => cmp.SetTextSrc(name is "" ? NoneSlotLabel : name));
        static UIAction SetSlotCopyDstLabel(string name) =>
            UGUI.Component<CoordinateCopyComponent>(cmp => cmp.SetTextDst(name is "" ? NoneSlotLabel : name));
        internal static void PrepareAcsCopy(Accessory_01 ui) =>
            (ui._kindSrcs, ui._kindDsts) = (
                ui._kindSrcs.Concat(Enumerable.Range(20, 79).Select(DuplicateSlotUI(ui._kindSrcs[0]))).ToArray(),
                ui._kindDsts.Concat(Enumerable.Range(20, 79).Select(DuplicateSlotUI(ui._kindDsts[0]))).ToArray());
        static Func<int, AccessoryCopyComponent> DuplicateSlotUI(AccessoryCopyComponent cmp) =>
            slot => DuplicateSlotUI(cmp.gameObject, slot)
                .With(UGUI.Text(text: $" {slot + 1}").At("Toggle", "txtNo"))
                .With(SetSlotCopyLabel(slot))
                .With(SubscribeCopySlotLabel(slot, SetSlotCopyLabel))
                .GetComponent<AccessoryCopyComponent>();
        static UIAction SubscribeCopySlotLabel(int slot, Func<string, UIAction> set) =>
            go => AccessoryExtension.OnSlotLabelChange
                .Where(tuple => tuple.Slot == slot)
                .Subscribe(tuple => go.With(set(tuple.Name)))
                .With(DisposeOnCustomDestroy);
        internal static void PrepareAcsCopy(CoordinateCopyAccessory ui) =>
            ui._mainKind = ui.With(PrepareScrollPanel)._mainKind
                .Concat(Enumerable.Range(20, 79).Select(slot => DuplicateUI(ui, ui._mainKind[0], slot))).ToArray();
        static CoordinateCopyComponent DuplicateUI(CoordinateCopyAccessory ui, CoordinateCopyComponent cmp, int slot) =>
            DuplicateSlotUI(cmp.gameObject, slot)
                .With(UGUI.Text(text: $" {slot + 1}").At("ST02"))
                .With(SetSlotCopySrcLabel(ui.GetSrcIndex(), slot))
                .With(SetSlotCopyDstLabel(ui.GetDstIndex(), slot))
                .With(SubscribeCopySlotLabel(ui, slot, SetSlotCopySrcLabel))
                .With(SubscribeCopySlotLabel(ui, slot, SetSlotCopyDstLabel))
                .GetComponent<CoordinateCopyComponent>()
                .With(SubscribeSrcDdUpdate(ui, slot))
                .With(SubscribeDstDdUpdate(ui, slot));
        internal static UIAction SubscribeCopySlotLabel(CoordinateCopyAccessory ui, int slot, Func<string, UIAction> set) =>
            go => AccessoryExtension.OnSlotLabelChange
                .Where(_ => ui.GetSrcIndex() == HumanCustom.Instance.Human.data.Status.coordinateType)
                .Where(tuple => tuple.Slot == slot)
                .Subscribe(tuple => go.With(set(tuple.Name))) 
                .With(DisposeOnCustomDestroy);
        internal static Action<CoordinateCopyComponent> SubscribeSrcDdUpdate(CoordinateCopyAccessory ui, int slot) =>
            cmp => Observable.Defer(() => Observable.Return(ui.GetSrcIndex()))
                .Concat(ui._ddSrcCoordeType.Observable().Wrap())
                .Where(_ => slot < HumanCustom.Instance.Human.acs.Accessories.Count)
                .Subscribe(UpdateSrcLabel(ui, slot, cmp)).With(DisposeOnCustomDestroy); 
        internal static Action<CoordinateCopyComponent> SubscribeDstDdUpdate(CoordinateCopyAccessory ui, int slot) =>
            cmp => Observable.Defer(() => Observable.Return(ui.GetDstIndex()))
                .Concat(ui._ddDstCoordeType.Observable().Wrap())
                .Where(_ => slot < HumanCustom.Instance.Human.acs.Accessories.Count)
                .Subscribe(UpdateDstLabel(ui, slot, cmp)).With(DisposeOnCustomDestroy); 
        static Action<int> UpdateSrcLabel(CoordinateCopyAccessory ui, int slot, CoordinateCopyComponent cmp) =>
            coordinateType => cmp.SetTextSrc(ui.GetName(true, slot, ui.coordinate[coordinateType]));
        static Action<int> UpdateDstLabel(CoordinateCopyAccessory ui, int slot, CoordinateCopyComponent cmp) =>
            coordinateType => cmp.SetTextDst(ui.GetName(false, slot, ui.coordinate[coordinateType]));
        static void PrepareScrollPanel(CoordinateCopyAccessory ui) =>
            ui._contentRoot._root.Find("AreaBG_CopyCoorde")
                .With(UGUI.LayoutV(padding: UGUI.Offset(0, 0, 10, 56)))
                .Find("grpClothes").With(PrepareScrollPanel);
        static void PrepareScrollPanel(Transform tf) =>
            tf.parent.With("Slots".AsChild(UGUI.Scroll(335, 510,
                tf.With(UGUI.LayoutV(childControlWidth: false) + UGUI.Size(width: 330)).AsChild())));
        internal static IDisposable[] Initialize() => [
            HumanCustomExtension.OnUIPrefab("categorytop.unity3d", "CategoryTop")
                .Subscribe(UGUI.Component<CategorySelectionTop>(PrepareAcsUI).Invoke),
            HumanCustomExtension.OnUIPrefab("custom/ui.unity3d", "01_AcsCopy")
                .Subscribe(UGUI.Component<Accessory_01>(PrepareAcsCopy).Invoke),
            HumanCustomExtension.OnUIPrefab("custom/ui.unity3d", "04_CopyAccessory")
                .Subscribe(UGUI.Component<CoordinateCopyAccessory>(PrepareAcsCopy).Invoke),
            SingletonInitializerExtension<HumanCustom>.OnStartup.Subscribe()
        ];
    }
    static partial class AccessoryExtension
    {
        internal static IDisposable[] Initialize(ConfigEntry<int> defaultSlots) => [
            ..UI.Initialize(),
            ..Extension.Register<CharaMods, CoordMods>(),
            Extension.OnPrepareSaveChara.Subscribe(CoordMods.Save),
            Extension.OnPrepareSaveCoord.Subscribe(CoordMods.Save),
            Extension.OnLoadCustomChara.Subscribe(human => CharaMods.Load(human, defaultSlots.Value)),
            Extension.OnActorHumanize.Subscribe(tuple => CharaMods.Load(tuple.Human, 20)),
            defaultSlots.AsObservable().Where(_ => HumanCustom.Instance.Human != null)
                .Subscribe(slots => PrepareSlots(HumanCustom.Instance.Human, slots))
        ];
        internal static IDisposable[] Initialize(Plugin plugin) =>
            Initialize(plugin.Config.Bind("Character Creation", "Default slots", 40,
                new ConfigDescription("Initial slots in character creation", new AcceptableValueRange<int>(20, 99))));
    }
    public partial class Plugin : BasePlugin
    {
        public const string Process = "SamabakeScramble";
    }
}