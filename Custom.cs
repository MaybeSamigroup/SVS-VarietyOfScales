using HarmonyLib;
using BepInEx.Unity.IL2CPP;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UniRx;
using Character;
using CharacterCreation;
using CharacterCreation.UI;
using CharacterCreation.UI.View.Accessory;
using Cysharp.Threading.Tasks;
using Fishbone;
using CoastalSmell;
using Parent = ChaAccessoryDefine.AccessoryParentKey;
using AcsNode = Character.HumanAccessory;
using AcsData = Character.HumanDataAccessory;
using AcsPart = Character.HumanDataAccessory.PartsInfo;

namespace VarietyOfScales
{
    internal static partial class Hooks
    {
        [HarmonyPrefix, HarmonyWrapSafe]
        [HarmonyPatch(typeof(HumanCustom), nameof(HumanCustom.GetSlotName))]
        static bool GetSlotNamePrefix(int slotNo, ref string __result) =>
            (__result = HumanCustom.Instance.Human.coorde.Now.Accessory.GetSlotName(slotNo)) == null;
    }
    static class SlotNameExtension
    {
        static string ToSlotName(AcsPart part, int slotNo) =>
            (part?.type, part?.id) switch
            {
                (null, null) or (120, _) => HumanCustom.Instance.GetTLSlotTitle(slotNo),
                _ => Human.lstCtrl.GetListInfo((ChaListDefine.CategoryNo)part.type, part.id)
              .GetInfo(ChaListDefine.KeyType.Name) ?? HumanCustom.Instance.GetTLSlotTitle(slotNo)
            };
        internal static string GetSlotName(this AcsData acs, int slotNo) =>
            slotNo < acs.parts.Count ? ToSlotName(acs.parts[slotNo], slotNo) : HumanCustom.Instance.GetTLSlotTitle(slotNo);
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
            go.gameObject.With(UpdateMoveUI("0.#", tf.localPosition * 100.0f));
        static Action<Transform> UpdateRotation(GameObject go) => tf =>
            go.gameObject.With(UpdateMoveUI("0", tf.localEulerAngles));
        static Action<Transform> UpdateScale(GameObject go) => tf =>
            go.gameObject.With(UpdateMoveUI("0.##", tf.localScale));
        static Action<GameObject> UpdateMoveUI(string format, Vector3 values) => go =>
            go.With(
                UGUI.ModifyAt("Controller", "Move", "X", "InputField_Decimal")
                    (UGUI.Cmp<TMP_InputField>(ui => ui.SetText(values.x.ToString(format), false))) +
                UGUI.ModifyAt("Controller", "Move", "Y", "InputField_Decimal")
                    (UGUI.Cmp<TMP_InputField>(ui => ui.SetText(values.y.ToString(format), false))) +
                UGUI.ModifyAt("Controller", "Move", "Z", "InputField_Decimal")
                    (UGUI.Cmp<TMP_InputField>(ui => ui.SetText(values.z.ToString(format), false))));
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
        static Action<Transform> PreparePositionEvent(MoveGroup ui, CompositeDisposable disps) => tf =>
            tf.With(UpdatePosition(ui.gameObject.With(UpdateMoveEvents(disps, "0",
                (value) => tf.localPosition = new(value / 100f, tf.localPosition.y, tf.localPosition.z),
                (value) => tf.localPosition = new(tf.localPosition.x, value / 100f, tf.localPosition.z),
                (value) => tf.localPosition = new(tf.localPosition.x, tf.localPosition.y, value / 100f)))));
        static Action<Transform> PrepareRotationEvent(MoveGroup ui, CompositeDisposable disps) => tf =>
            tf.With(UpdateRotation(ui.gameObject.With(UpdateMoveEvents(disps, "0",
                (value) => tf.localEulerAngles = new(value, tf.localEulerAngles.y, tf.localEulerAngles.z),
                (value) => tf.localEulerAngles = new(tf.localEulerAngles.x, value, tf.localEulerAngles.z),
                (value) => tf.localEulerAngles = new(tf.localEulerAngles.x, tf.localEulerAngles.y, value)))));
        static Action<Transform> PrepareScaleEvent(MoveGroup ui, CompositeDisposable disps) => tf =>
            tf.With(UpdateScale(ui.gameObject.With(UpdateMoveEvents(disps, "1",
                (value) => tf.localScale = new(value, tf.localScale.y, tf.localScale.z),
                (value) => tf.localScale = new(tf.localScale.x, value, tf.localScale.z),
                (value) => tf.localScale = new(tf.localScale.x, tf.localScale.y, value)))));
        static Action<GameObject> UpdateMoveEvents(CompositeDisposable disps,
            string value, Action<float> setX, Action<float> setY, Action<float> setZ) => go => 
            go.With(
                UGUI.ModifyAt("Controller", "Move", "X", "InputField_Decimal")
                    (UGUI.Cmp<TMP_InputField>(ui => disps.With(PrepareResetEvent(go, ui, "X", value))
                        .Add(ui.onValueChanged.AsObservable().Subscribe(PrepareOnValueChanged(setX))))) +
                UGUI.ModifyAt("Controller", "Move", "Y", "InputField_Decimal")
                    (UGUI.Cmp<TMP_InputField>(ui => disps.With(PrepareResetEvent(go, ui, "Y", value))
                        .Add(ui.onValueChanged.AsObservable().Subscribe(PrepareOnValueChanged(setY))))) +
                UGUI.ModifyAt("Controller", "Move", "Z", "InputField_Decimal")
                    (UGUI.Cmp<TMP_InputField>(ui => disps.With(PrepareResetEvent(go, ui, "Z", value))
                        .Add(ui.onValueChanged.AsObservable().Subscribe(PrepareOnValueChanged(setZ))))));
        static Action<CompositeDisposable> PrepareResetEvent(GameObject go, TMP_InputField text, string axis, string value) =>
            disps => go.With(UGUI.ModifyAt("Controller", "Move", axis, "btnDefault")
                (UGUI.Cmp<Button>(button => disps.Add(button
                    .OnClickAsObservable().Subscribe(F.Apply(text.SetText, value, true).Ignoring<Unit>())))));
        static Action<string> PrepareOnValueChanged(Action<float> setter) =>
            input => float.TryParse(input, out var value).Maybe(F.Apply(setter, value));
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

        [HarmonyPostfix]
        [HarmonyWrapSafe]
        [HarmonyPatch(typeof(Accessory_00), nameof(Accessory_00.UpdateCustomUI), [])]
        static void UpdateCustomUIPostfix() =>
            UI.UpdateSlotTitles();
    }
    static partial class AccessoryExtension
    {
        internal static int SlotCount => UI.SlotCount;

        static bool GetInfo(Human _, ChaListDefine.CategoryNo category, int id, ChaListDefine.KeyType key, out string value) =>
            Human.lstCtrl.GetInfo(category, id, key, out value);

        internal static void SetDefaultAcsColor(AcsNode node, int slot) =>
            Check(node, slot).Maybe(
                F.Apply(ApplyDefaults, node.nowCoordinate.Accessory.parts[slot], node.Accessories[slot]) +
                F.Apply(ChangePtnTexture, node, slot, -1) +
                F.Apply(ChangePtnColor, node, slot, -1) +
                F.Apply(ChangePtnParams, node, slot, -1));
    }
    public partial class Plugin : BasePlugin
    {
        public override void Load()
        {
            Instance = this;
            Patch = Harmony.CreateAndPatchAll(typeof(Hooks), $"{Name}.Hooks");
            Extension.PrepareSaveChara += () => CoordMods.Save(HumanCustom.Instance.Human);
            Extension.PrepareSaveCoord += () => CoordMods.Save(HumanCustom.Instance.Human);
            Extension.Register<CharaMods, CoordMods>();
            Extension.OnLoadChara += CharaMods.Load;
            UI.Initialize();
        }
    }
}