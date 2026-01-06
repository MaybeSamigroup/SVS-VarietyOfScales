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
using ILLGames.Extensions;

namespace VarietyOfScales
{
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
        static Dictionary<int, Subject<bool>> SlotStateSubjects = new();
        static void NotifySlotState(int slot) =>
            SlotStateSubjects.ForEach((index, subject) => subject.OnNext(index < slot));
        internal static IObservable<bool> SlotStateObservable(int slot) =>
            Observable.Defer(() => Observable.Return(slot < HumanCustom.Instance.Human.acs.Accessories.Count))
                .Concat((SlotStateSubjects[slot] = SlotStateSubjects.GetValueOrDefault(slot, new ())).AsObservable()).DistinctUntilChanged();
        static string ToSlotName(AcsPart part) =>
            Human.lstCtrl.GetListInfo((ChaListDefine.CategoryNo)part.type, part.id)?.GetInfo(ChaListDefine.KeyType.Name) ?? "";
        static string ToSlotName(HumanDataCoordinate coord, int slot) => ToSlotName(coord.Accessory.parts[slot]);
        static string ToSlotName(this Human human, int slot) =>
            Check(human.acs, slot) ? ToSlotName(human.acs.nowCoordinate, slot) : "";
        static Dictionary<int, Subject<string>> SlotNameSubjects = new();
        static void NotifySlotRemove(int slot) =>
            SlotNameSubjects.TryGetValue(slot, out var subject).Maybe(() => subject.OnNext(""));
        static void NotifySlotAssign(int slot, AcsPart part) =>
            SlotNameSubjects.TryGetValue(slot, out var subject).Maybe(() => subject.OnNext(ToSlotName(part)));
        internal static string NoneSlotName =>
            Human.lstCtrl.GetListInfo(ChaListDefine.CategoryNo.ao_none, 0)?.GetInfo(ChaListDefine.KeyType.Name) ?? "";
        internal static IObservable<string> SlotNameObservable(int slot) =>
            Observable.Defer(() => Observable.Return(HumanCustom.Instance.Human.ToSlotName(slot)))
                .Concat(SlotNameSubjects[slot] = SlotNameSubjects.GetValueOrDefault(slot, new ())).AsObservable().DistinctUntilChanged();
        internal static void CleanupSlotSubjects() =>
            (SlotNameSubjects, SlotStateSubjects) = (new(), new());
    }
    internal static class UI
    {
        static void DisposeOnCustomDestroy(IDisposable disposable) =>
            HumanCustom.Instance.OnDestroyAsObservable().Subscribe(F.Ignoring<Unit>(disposable.Dispose));
        static Action<GameObject> SubscribeSlotState(int slot) =>
            go => AccessoryExtension.SlotStateObservable(slot).Subscribe(go.SetActive).With(DisposeOnCustomDestroy);
        static GameObject DuplicateSlotUI(GameObject reference, int slot) =>
            UnityEngine.Object.Instantiate(reference, reference.transform.parent).With(SubscribeSlotState(slot));
        internal static void PrepareAcsUI(CategorySelectionTop top) =>
            DisposeOnCustomDestroy(PrepareAcsUI(top.transform.Find("04_Accessories").GetComponent<CategorySelection>()));
        static IDisposable PrepareAcsUI(CategorySelection selection) =>
            selection.With(PrepareScrollPanel).OnEnableAsObservable().FirstAsync()
                .Subscribe(_ => selection.transform.With("Control".AsChild(
                    UGUI.Size(height: 620) +
                    UGUI.Rt(sizeDelta: new(233, 610)) +
                    UGUI.LayoutV(padding: UGUI.Offset(0, 0, 0, 10)) +
                    selection.transform.GetChild(1).AsChild() +
                    PrepareIndices(selection._root))));

        static UIAction PrepareIndices(Transform index) =>
            RelocateControls(index) + ExchangeBackground(index.GetChild(0).GetComponent<Image>());
        
        static UIAction ExchangeBackground(Image image) =>
            UGUI.Image(color: image.color, type: image.type, sprite: image.sprite) +
                new UIAction(_ => UnityEngine.Object.Destroy(image.gameObject));

        static UIAction RelocateControls(Transform index) =>
            Enumerable.Range(index.childCount - 3, 3).Select(index.GetChild)
                .Select(tf => UGUI.AsChild(tf.With(UGUI.Size(width: 233, height: 26)))).Aggregate();

        static void PrepareScrollPanel(CategorySelection selection) =>
            selection._root.With(DuplicateSlotUI).parent
                .With("Slots".AsChild(UGUI.ClearPanel + UGUI.ScrollV(233, 520, selection._root.AsChild())));

        static void DuplicateSlotUI(Transform tf) =>
            Enumerable.Range(20, 79).ForEach(slot =>
                DuplicateSlotUI(tf.GetChild(2).gameObject, slot)
                    .With(SubscribeSlotName(slot)).transform.SetSiblingIndex(slot + 2));

        static Action<GameObject> SubscribeSlotName(int slot) =>
            go => AccessoryExtension.SlotNameObservable(slot)
                .Subscribe(SetSlotName(go.GetComponent<CategoryKindToggle>(), slot)).With(DisposeOnCustomDestroy);

        static Action<string> SetSlotName(CategoryKindToggle cmp, int slot) =>
            name => cmp.Title = name is "" ? HumanCustom.Instance.GetTLSlotTitle(slot) : name;

        static Action<string> SetSlotName(Action<string> action) =>
            name => action(name is "" ? AccessoryExtension.NoneSlotName : name);

        internal static void PrepareAcsCopy(Accessory_01 ui) =>
            (ui._kindSrcs, ui._kindDsts) = (
                ui._kindSrcs.Concat(Enumerable.Range(20, 79).Select(DuplicateSlotUI(ui._kindSrcs[0]))).ToArray(),
                ui._kindDsts.Concat(Enumerable.Range(20, 79).Select(DuplicateSlotUI(ui._kindDsts[0]))).ToArray());

        static Func<int, AccessoryCopyComponent> DuplicateSlotUI(AccessoryCopyComponent cmp) =>
            slot => DuplicateSlotUI(cmp.gameObject, slot)
                .With(UGUI.Text(text: $" {slot + 1}").At("Toggle", "txtNo"))
                .GetComponent<AccessoryCopyComponent>().With(SubscribeCopySlotName(slot));

        static Action<AccessoryCopyComponent> SubscribeCopySlotName(int slot) =>
            cmp => AccessoryExtension.SlotNameObservable(slot).Subscribe(SetSlotName(cmp.SetText)).With(DisposeOnCustomDestroy);

        internal static void PrepareAcsCopy(CoordinateCopyAccessory ui) =>
            ui._mainKind = ui.With(PrepareScrollPanel)._mainKind
                .Concat(Enumerable.Range(20, 79).Select(slot => DuplicateUI(ui, ui._mainKind[0], slot))).ToArray();

        static CoordinateCopyComponent DuplicateUI(CoordinateCopyAccessory ui, CoordinateCopyComponent cmp, int slot) =>
            DuplicateSlotUI(cmp.gameObject, slot)
                .With(UGUI.Text(text: $" {slot + 1}").At("ST02"))
                .GetComponent<CoordinateCopyComponent>()
                .With(SubscribeSrcSlotName(ui, slot))
                .With(SubscribeDstSlotName(ui, slot))
                .With(SubscribeSrcDdUpdate(ui, slot))
                .With(SubscribeDstDdUpdate(ui, slot));

        internal static Action<CoordinateCopyComponent> SubscribeSrcSlotName(CoordinateCopyAccessory ui, int slot) =>
            cmp => AccessoryExtension.SlotNameObservable(slot)
                .Where(_ => ui.GetSrcIndex() == HumanCustom.Instance.Human.data.Status.coordinateType)
                .Subscribe(SetSlotName(cmp.SetTextSrc)).With(DisposeOnCustomDestroy);

        internal static Action<CoordinateCopyComponent> SubscribeDstSlotName(CoordinateCopyAccessory ui, int slot) =>
            cmp => AccessoryExtension.SlotNameObservable(slot)
                .Where(_ => ui.GetDstIndex() == HumanCustom.Instance.Human.data.Status.coordinateType)
                .Subscribe(SetSlotName(cmp.SetTextDst)).With(DisposeOnCustomDestroy);

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
            tf.parent.With("Slots".AsChild(UGUI.ScrollV(335, 510, tf.AsChild())));

        internal static IDisposable[] Initialize() => [
            HumanCustomExtension.OnUIPrefab("categorytop.unity3d", "CategoryTop")
                .Subscribe(UGUI.Component<CategorySelectionTop>(PrepareAcsUI).Invoke),
            HumanCustomExtension.OnUIPrefab("custom/ui.unity3d", "01_AcsCopy")
                .Subscribe(UGUI.Component<Accessory_01>(PrepareAcsCopy).Invoke),
            HumanCustomExtension.OnUIPrefab("custom/ui.unity3d", "04_CopyAccessory")
                .Subscribe(UGUI.Component<CoordinateCopyAccessory>(PrepareAcsCopy).Invoke)
        ];
    }
    static partial class AccessoryExtension
    {
        internal static IDisposable[] Initialize() => [
            ..UI.Initialize(),
            ..Extension.Register<CharaMods, CoordMods>(),
            Extension.OnPrepareSaveChara.Subscribe(CoordMods.Save),
            Extension.OnPrepareSaveCoord.Subscribe(CoordMods.Save),
            Extension.OnLoadChara.Subscribe(CharaMods.Load),
        ];
    }

    public partial class Plugin : BasePlugin
    {
        public const string Process = "SamabakeScramble";
        public IObservable<int> CustomExtensions => Config.Bind("Character Creation", "Default slots", 40,
            new ConfigDescription("Initial slots in character creation", new AcceptableValueRange<int>(20, 99))).AsObservable();
    }

}