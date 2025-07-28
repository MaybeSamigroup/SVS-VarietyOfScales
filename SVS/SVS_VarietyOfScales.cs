using HarmonyLib;
using BepInEx.Unity.IL2CPP;
using BepInEx.Configuration;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO.Compression;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UniRx;
using UniRx.Triggers;
using Character;
using CharacterCreation;
using CharacterCreation.UI;
using CharacterCreation.UI.View.Accessory;
using Fishbone;
using CoastalSmell;
using Parent = ChaAccessoryDefine.AccessoryParentKey;
using AcsNode = Character.HumanAccessory;
using AcsData = Character.HumanDataAccessory;
using AcsPart = Character.HumanDataAccessory.PartsInfo;
using CharaLimit = Character.HumanData.LoadLimited.Flags;
using CoordLimit = Character.HumanDataCoordinate.LoadLimited.Flags;
using UnityEngine.EventSystems;
using Cysharp.Threading.Tasks;

namespace VarietyOfScales
{
    internal static partial class Hooks
    {
        [HarmonyPrefix]
        [HarmonyWrapSafe]
        [HarmonyPatch(typeof(HumanCustom), nameof(HumanCustom.GetSlotName))]
        static bool GetSlotNamePrefix(int slotNo, ref string __result) =>
            (__result = HumanCustom.Instance.Human.coorde.nowCoordinate.Accessory.ToSlotName(slotNo)) == null;
    }
    static class SlotNameExtensions
    {
        internal static string ToSlotName(this AcsData acs, int slotNo) =>
            slotNo < acs.parts.Count ? ToSlotName(acs.parts[slotNo], slotNo) : HumanCustom.Instance.GetTLSlotTitle(slotNo);
        internal static string ToSlotName(AcsPart part, int slotNo) =>
            (part?.type, part?.id) switch
            {
                (null, null) or (120, _) => HumanCustom.Instance.GetTLSlotTitle(slotNo),
                _ => Human.lstCtrl.GetListInfo((ChaListDefine.CategoryNo)part.type, part.id)
              .GetInfo(ChaListDefine.KeyType.Name) ?? HumanCustom.Instance.GetTLSlotTitle(slotNo)
            };
    }
    internal static partial class Hooks
    {
        [HarmonyPrefix]
        [HarmonyWrapSafe]
        [HarmonyPatch(typeof(AcsEdit), nameof(AcsEdit.SetColorWindow))]
        static bool SetColorWindowPrefix(AcsEdit __instance, int slotNo, int index, ThumbnailColor acsColors, Il2CppSystem.Func<bool> updateUI) =>
            F.Apply(acsColors.InitAcs, __instance._humanAcs, slotNo, index, updateUI).Bypass(slotNo);
        [HarmonyPrefix]
        [HarmonyWrapSafe]
        [HarmonyPatch(typeof(PatternEdit), nameof(PatternEdit.SetColorPtnWindow))]
        static bool SetColorPtnWindowPrefix(PatternEdit __instance, int slotNo, int index, ThumbnailColor ptnColor, Il2CppSystem.Func<bool> updateUI) =>
            F.Apply(ptnColor.InitPtn, __instance._humanAcs, slotNo, index, updateUI).Bypass(slotNo);
    }
    static partial class AccessoryExtensions
    {
        internal static void InitAcs(this ThumbnailColor ui, AcsNode node, int slot, int index, Il2CppSystem.Func<bool> updateUI) =>
            ui.Initialize($"Slot{slot + 1}/Color{index}",
                AcsColorGetter(node.nowCoordinate.Accessory.parts[slot], index).With(getter => ui.SetColor(getter())),
                ColorSetter(
                    AcsColorSetter(node.human.data, slot, index) +
                    AcsColorSetter(node.nowCoordinate.Accessory.parts[slot], index) +
                    ColorSetter(node, slot, ChaShader.Accessory.GetMainColorID(index)), updateUI), index > 2, true);
        internal static void InitPtn(this ThumbnailColor ui, AcsNode node, int slot, int index, Il2CppSystem.Func<bool> updateUI) =>
            ui.Initialize($"Slot{slot + 1}/Pattern{index}",
                PtnColorGetter(node.nowCoordinate.Accessory.parts[slot], index).With(getter => ui.SetColor(getter())),
                ColorSetter(
                    PtnColorSetter(node.human.data, slot, index) +
                    PtnColorSetter(node.nowCoordinate.Accessory.parts[slot], index) +
                    ColorSetter(node, slot, ChaShader.Accessory.GetPatternColorID(index)), updateUI), index > 2, true);
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
        static Action<Color> ColorSetter(AcsNode node, int slot, int shaderId) =>
            color => ChangeColor(node.accessories[slot].renderers, shaderId, color);
    }
    internal static partial class Hooks
    {
        [HarmonyPrefix]
        [HarmonyWrapSafe]
        [HarmonyPatch(typeof(AccessoryParentWindow), nameof(AccessoryParentWindow.Get), typeof(int))]
        static bool GetAccessoryParent(AccessoryParentWindow __instance, int slotNo, ref int __result) =>
            (__result = __instance._acsData.parts[slotNo].parentKeyType) is not 0;
        [HarmonyPrefix]
        [HarmonyWrapSafe]
        [HarmonyPatch(typeof(ParentEdit), nameof(ParentEdit.ChangeAccessoryParent), typeof(int))]
        static bool ChangeAccessoryParentPrefix(ParentEdit __instance, int slotNo) =>
            F.Apply(__instance._humanAcs.ChangeParent, __instance._acsData,
                slotNo, __instance._accessoryParentWindow._toggleGroup.GetOnBit()).Bypass(slotNo);
    }
    static partial class AccessoryExtensions
    {
        static Parent ToggleBitsToParent(int bits, int value = 0) =>
            bits == 0 ? (Parent)AccessoryParentWindow._accessoryParentKeys[value] : ToggleBitsToParent(bits >> 1, value + 1);
        internal static void ChangeParent(this AcsNode node, AcsData data, int slot, int bits) =>
            node.ChangeParent(slot, ToggleBitsToParent(bits).With(data.parts[slot].ChangeParent));
    }
    internal static partial class Hooks
    {
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        [HarmonyPatch(typeof(AccessoryMoveWindow), nameof(AccessoryMoveWindow.UpdateCustomUI), [])]
        static void AccessoryMoveWindowUpdateCustomUIPostfix(AccessoryMoveWindow __instance) =>
            __instance.PrepareMoveEvents();

        [HarmonyPrefix]
        [HarmonyWrapSafe]
        [HarmonyPatch(typeof(AccessoryMoveWindow), nameof(AccessoryMoveWindow.UpdateSelection))]
        [HarmonyPatch(typeof(AccessoryMoveWindow), nameof(AccessoryMoveWindow.SetControllerTransform))]
        static bool AccessoryMoveWindowUpdateCustomUIPostfix(AccessoryMoveWindow __instance, int slotNo, int editNo) =>
            F.Apply(__instance._humanAcs.UpdateMoveUI, slotNo, editNo, __instance._guidList[editNo],
                __instance._movePairs.Where(pair => pair.Active).Select(pair => pair.MoveGroup).ToArray()).Bypass(slotNo);
    }
    static partial class AccessoryExtensions
    {
        internal static void PrepareMoveEvents(this AccessoryMoveWindow ui) =>
            ui._movePairs.ForEachIndex((mp, index) => ((index % 3) switch
            {
                0 => F.Apply(PreparePositionEvent, TargetTransform(ui), mp._moveGroup, ui._disposables),
                1 => F.Apply(PrepareRotationEvent, TargetTransform(ui), mp._moveGroup, ui._disposables),
                2 => F.Apply(PrepareScaleEvent, TargetTransform(ui), mp._moveGroup, ui._disposables),
                _ => throw new NotImplementedException()
            })());
        static Func<Transform> TargetTransform(AccessoryMoveWindow ui) =>
            () => ui._humanAcs.accessories[ui._slotNo].objAcsMove[ui._editNo].transform;
        static void PreparePositionEvent(Func<Transform> getTf, MoveGroup ui, CompositeDisposable disps) =>
            UpdateMoveEvents(ui.gameObject, disps, getTf,
                (tf, value) => tf.localPosition = new(value / 100f, tf.localPosition.y, tf.localPosition.z),
                (tf, value) => tf.localPosition = new(tf.localPosition.x, value / 100f, tf.localPosition.z),
                (tf, value) => tf.localPosition = new(tf.localPosition.x, tf.localPosition.y, value / 100f));
        static void PrepareRotationEvent(Func<Transform> getTf, MoveGroup ui, CompositeDisposable disps) =>
            UpdateMoveEvents(ui.gameObject, disps, getTf,
                (tf, value) => tf.localEulerAngles = new(value, tf.localEulerAngles.y, tf.localEulerAngles.z),
                (tf, value) => tf.localEulerAngles = new(tf.localEulerAngles.x, value, tf.localEulerAngles.z),
                (tf, value) => tf.localEulerAngles = new(tf.localEulerAngles.x, tf.localEulerAngles.y, value));
        static void PrepareScaleEvent(Func<Transform> getTf, MoveGroup ui, CompositeDisposable disps) =>
            UpdateMoveEvents(ui.gameObject, disps, getTf,
                (tf, value) => tf.localScale = new(value, tf.localScale.y, tf.localScale.z),
                (tf, value) => tf.localScale = new(tf.localScale.x, value, tf.localScale.z),
                (tf, value) => tf.localScale = new(tf.localScale.x, tf.localScale.y, value));
        static void UpdateMoveEvents(GameObject go, CompositeDisposable disps, Func<Transform> getTf,
            Action<Transform, float> setX, Action<Transform, float> setY, Action<Transform, float> setZ) =>
             go.With(
                UGUI.ModifyAt("Controller", "Move", "X", "InputField_Decimal")
                    (UGUI.Cmp<TMP_InputField>(ui => disps.With(PrepareResetEvent( go, ui, "X"))
                        .Add(ui.onSubmit.AsObservable().Subscribe(PrepareOnSubmit(getTf, setX))))) +
                UGUI.ModifyAt("Controller", "Move", "Y", "InputField_Decimal")
                    (UGUI.Cmp<TMP_InputField>(ui => disps.With(PrepareResetEvent(go, ui, "Y"))
                        .Add(ui.onSubmit.AsObservable().Subscribe(PrepareOnSubmit(getTf, setY))))) +
                UGUI.ModifyAt("Controller", "Move", "Z", "InputField_Decimal")
                    (UGUI.Cmp<TMP_InputField>(ui => disps.With(PrepareResetEvent(go, ui, "Z"))
                        .Add(ui.onSubmit.AsObservable().Subscribe(PrepareOnSubmit(getTf, setZ))))));
        static Action<CompositeDisposable> PrepareResetEvent(GameObject go, TMP_InputField text, string axis) =>
            disps => go.With(UGUI.ModifyAt("Controller", "Move", axis, "btnDefault")
                (UGUI.Cmp<Button>(button => disps.Add(button
                    .OnClickAsObservable().Subscribe((F.Apply(text.SetText, "0", false) + text.SendOnSubmit).Ignoring<Unit>())))));
        static Action<string> PrepareOnSubmit(Func<Transform> getter, Action<Transform, float> setter) =>
            input => float.TryParse(input, out var value).Maybe(F.Apply(setter, getter(), value));
        internal static void UpdateMoveUI(this AcsNode node, int slot, int index, GuideObject guide, MoveGroup[] moves) =>
            UpdateMoveUI(node.accessories[slot].objAcsMove[index].With(guide.Amount.Set), moves);
        static void UpdateMoveUI(Transform tf, MoveGroup[] moves) =>
            moves.ForEachIndex((ui, index) => (index switch
            {
                0 => UpdateMoveUI("##0.#", tf.localPosition * 100.0f),
                1 => UpdateMoveUI("##0", tf.localEulerAngles),
                2 => UpdateMoveUI("##0.##", tf.localScale),
                _ => F.DoNothing.Ignoring<GameObject>()
            })(ui.gameObject));
        static Action<GameObject> UpdateMoveUI(string format, Vector3 values) => go =>
            go.With(
                UGUI.ModifyAt("Controller", "Move", "X", "InputField_Decimal")
                    (UGUI.Cmp<TMP_InputField>(ui => ui.SetText(values.x.ToString(format)))) +
                UGUI.ModifyAt("Controller", "Move", "Y", "InputField_Decimal")
                    (UGUI.Cmp<TMP_InputField>(ui => ui.SetText(values.y.ToString(format)))) +
                UGUI.ModifyAt("Controller", "Move", "Z", "InputField_Decimal")
                    (UGUI.Cmp<TMP_InputField>(ui => ui.SetText(values.z.ToString(format)))));
    }
    internal static partial class Hooks
    {
        [HarmonyPrefix]
        [HarmonyWrapSafe]
        [HarmonyPatch(typeof(AccessoryFKWindow), nameof(AccessoryFKWindow.UpdateCustomUI))]
        static void AccessoryFKWindowUpdateCustomUIPrefix(AccessoryFKWindow __instance) =>
            __instance._acsData
                .parts[__instance._slotNo].fkInfo.bones =
            __instance._humanAcs.nowCoordinate.Accessory
                .parts[__instance._slotNo].fkInfo.bones =
            __instance._human.data.Coordinates[__instance._human.data.Status.coordinateType].Accessory
                .parts[__instance._slotNo].fkInfo.bones =
                __instance._humanAcs.accessories[__instance._slotNo].objAcsFK.Select(tf => tf.localEulerAngles).ToArray();
        [HarmonyPrefix]
        [HarmonyWrapSafe]
        [HarmonyPatch(typeof(AccessoryFKWindow), nameof(AccessoryFKWindow.UpdateAcsAllReset))]
        static bool AccessoryFKWindowUpdateAcsAllResetPrefix(AccessoryFKWindow __instance, int slotNo, int editNo) =>
            F.Apply(__instance._humanAcs.ResetFK, slotNo, editNo).Bypass(slotNo);
        [HarmonyPrefix]
        [HarmonyWrapSafe]
        [HarmonyPatch(typeof(AccessoryFKWindow), nameof(AccessoryFKWindow.UpdateAcsMovePaste))]
        static bool AccessoryFKWindowUpdateAcsMovePastePrefix(AccessoryFKWindow __instance, int slotNo, int editNo, Vector3 value) =>
            F.Apply(__instance._humanAcs.SetFK, slotNo, editNo, value).Bypass(slotNo);
        [HarmonyPrefix]
        [HarmonyWrapSafe]
        [HarmonyPatch(typeof(AccessoryFKWindow), nameof(AccessoryFKWindow.UpdateAcsRotAdd))]
        static bool AccessoryFKAWindowUpdateAcsRotAddPrefix(AccessoryFKWindow __instance, int slotNo, int editNo, int xyz, bool add, float val) =>
            F.Apply(__instance._humanAcs.SetFK, slotNo, editNo, val, add, 1 << xyz).Bypass(slotNo);
    }
    static partial class AccessoryExtensions
    {
        internal static void ResetFK(this AcsNode node, int slot, int index) =>
            node.SetFK(slot, index, node.accessories[slot].cusAcsCmp.GetFKBonesDef()[index]);
        internal static void SetFK(this AcsNode node, int slot, int index, Vector3 value) =>
            ToFKParams(value).ForEach(ps => node.SetAccessoryFK(slot, index, ps.Item1, ps.Item2, ps.Item3));
        static IEnumerable<Tuple<float, bool, int>> ToFKParams(Vector3 value) =>
            [new(value.x, false, 1), new(value.y, false, 2), new(value.z, false, 4)];
    }
    class UI
    {
        GameObject RootPanel;
        GameObject SlotPanel;
        CategorySelection SelectionUI;
        Il2CppSystem.Collections.Generic.List<CategoryKindToggle> Toggles;
        Action<Unit> OnShow =>
            _ => RootPanel.SetActive(true);
        Action<Unit> OnHide =>
            _ => RootPanel.SetActive(false);
        Action<Unit> RemoveAll;
        UI() => RootPanel = new GameObject(Plugin.Name)
            .With(UGUI.Go(active: false, parent: UGUI.RootCanvas))
            .With(UGUI.Cmp(UGUI.Rt(
                anchoredPosition: new(5, -105),
                sizeDelta: new(240, 558),
                anchorMin: new(0, 1),
                anchorMax: new(0, 1),
                offsetMin: new(0, 0),
                offsetMax: new(0, 0),
                pivot: new(0, 1))))
            .With(UGUI.Cmp(UGUI.LayoutGroup<VerticalLayoutGroup>(spacing: 2, padding: new(5, 5, 5, 5))));
        UI(Il2CppSystem.IObservable<Unit> show, Il2CppSystem.IObservable<Unit> hide, Action<Unit> removeAll) : this() =>
            (_, _, RemoveAll) = (show.Subscribe(OnShow), hide.Subscribe(OnHide), removeAll);
        UI(ObservableEnableTrigger acs, GameObject index, Button button) :
            this(acs.OnEnableAsObservable(), acs.OnDisableAsObservable(), _ => button.onClick.Invoke()) =>
                index.AddComponent<ObservableEnableTrigger>().OnEnableAsObservable()
                    .Subscribe(F.Apply(index.SetActive, false).Ignoring<Unit>());
        UI(GameObject acs, Transform index) :
            this(acs.AddComponent<ObservableEnableTrigger>(), index.gameObject,
                index.GetChild(index.childCount - 1).gameObject.GetComponent<Button>()) =>
            (SelectionUI = acs.GetComponent<CategorySelection>())._kindToggles =
                new Il2CppSystem.Collections.Generic.IReadOnlyList<CategoryKindToggle>((Toggles = new()).Pointer);
        UI(Transform tf) : this(tf.gameObject, tf.Find("Index")) =>
            new GameObject("Controls").With(UGUI.Go(parent: RootPanel.transform))
                .With(UGUI.Cmp(UGUI.LayoutGroup<HorizontalLayoutGroup>(spacing: 2)))
                .With(PrepareDecrease).With(PrepareIncrease).With(PrepareContents)
                .With(PrepareOpenCopy).With(PrepareRemoveAll).With(PrepareSlots);
        void PrepareDecrease(GameObject parent) =>
            UGUI.Button(112, 24, "Slot-", parent)
                .GetComponent<Button>().OnClickAsObservable().Subscribe(F.Ignoring<Unit>(Decrease));
        void PrepareIncrease(GameObject parent) =>
            UGUI.Button(112, 24, "Slot+", parent)
                .GetComponent<Button>().OnClickAsObservable().Subscribe(F.Ignoring<Unit>(Increase));
        void PrepareOpenCopy() =>
            UGUI.Button(235, 24, "Make Copy", RootPanel)
                .With(UGUI.Cmp<CategoryKindToggle>(Toggles.Add))
                .With(UGUI.Cmp<CategoryViewBinderUnion>(ui => ui.file = "01_AcsCopy"))
                .With(UGUI.Cmp<CharacterCreation.Text>(ui => ui.gameObject.GetComponent<CategoryKindToggle>()._title = ui))
                .With(UGUI.ModifyAt("Make Copy.Label")(
                    UGUI.Cmp<TextMeshProUGUI, CharacterCreation.Text>((ui, txt) => txt._tmpText = ui)))
                .GetComponent<Button>().OnClickAsObservable().Subscribe(OpenCopyUI);
        void PrepareRemoveAll() =>
            UGUI.Button(235, 24, "Deselect All", RootPanel)
                .GetComponent<Button>().OnClickAsObservable().Subscribe(RemoveAll);
        void PrepareContents() =>
            SlotPanel = UGUI.ScrollView(235, 480, "Slots", RootPanel)
                .With(UGUI.Cmp(UGUI.LayoutGroup<VerticalLayoutGroup>()))
                .With(UGUI.Cmp(UGUI.ToggleGroup(allowSwitchOff: false)))
                .With(UGUI.Cmp(UGUI.Fitter()));
        void PrepareSlots() =>
            Enumerable.Range(0, 20 + CustomExtensions.Value).ForEach(PrepareSlot);
        Action<int> PrepareSlot =>
            index => ConfigureCategory(
                UGUI.Toggle(220, 24, $"Slot{index + 1}", SlotPanel),
                HumanCustom.Instance.GetSlotName(index)
            ).OnValueChangedAsObservable().Subscribe(OpenSlot(index));
        Action<bool> OpenSlot(int index) =>
            value => value.Maybe(F.Apply(SelectionUI.OpenView, index));
        Action<Unit> OpenCopyUI =>
            _ => SelectionUI.OpenView(Toggles.Count - 1);
        Action<CategoryKindToggle> IncreaseToggles =>
            item => Toggles.Insert(Toggles.Count - 1, item);
        Action DecreaseToggles =>
            F.Apply(UnityEngine.Object.Destroy,
                Toggles[Toggles.Count - 2].gameObject) +
            F.Apply(Toggles.RemoveAt, Toggles.Count - 2);
        Toggle ConfigureCategory(GameObject go, string title) => go
            .With(UGUI.Cmp<Toggle, ToggleGroup>((ui, group) => ui.group = group))
            .With(UGUI.Cmp<CategoryViewBinderUnion>(ui => ui.file = "00_AcsSlot"))
            .With(UGUI.Cmp(IncreaseToggles))
            .With(UGUI.ModifyAt($"{go.name}.State")(
                UGUI.Cmp<CharacterCreation.Text, CategoryKindToggle>((txt, ckt) => ckt._title = txt)))
            .With(UGUI.ModifyAt($"{go.name}.State", $"{go.name}.Label")(
                UGUI.Cmp<TextMeshProUGUI, CharacterCreation.Text>((ui, txt) => (txt._tmpText = ui).SetText(title))))
            .GetComponent<CategoryKindToggle>()._toggle = go.GetComponent<Toggle>();
        void Increase() =>
            PrepareSlot.With(HumanCustom.Instance.DefaultData.Increase)(Toggles.Count - 1);
        void Decrease() =>
            (Toggles.Count > 21).Maybe(HumanCustom.Instance.Human.Decrease + DecreaseToggles);
        static void UpdateSlotTitle(int index, string title) =>
            Instance.Toggles[index]._title._tmpText.SetText(title, false);
        static void UpdateSlotTitle(int index) =>
            UpdateSlotTitle(index, HumanCustom.Instance.Human.coorde.nowCoordinate.Accessory.ToSlotName(index));
        static UI Instance;
        internal static void UpdateSlotTitles() =>
            Enumerable.Range(20, Instance.Toggles.Count - 21).ForEach(UpdateSlotTitle);
        static Il2CppSystem.IDisposable RemoveAllEvent;
        static CompositeDisposable DialogEvents;
        static Action<Unit> PrepareEvents =
            _ => (DialogEvents = new CompositeDisposable()).With(PrepareDialogEvents);
        static Action<Unit> CleanupDialogEvents =
            _ => DialogEvents.Dispose();
        static Action<Unit> CleanupExtensions =
            _ => HumanCustom.Instance.Human.CleanupExtensions();
        static void PrepareDialogAccept((Button, Button) buttons) =>
            DialogEvents.Add(buttons.Item1.OnClickAsObservable().Subscribe(CleanupExtensions + CleanupDialogEvents));
        static void PrepareDialogCancel((Button, Button) buttons) =>
            DialogEvents.Add(buttons.Item2.OnClickAsObservable().Subscribe(CleanupDialogEvents));
        internal static int ExtensionSlots =>
            Instance == null ? HumanCustom.Instance == null ? 0 :
                CustomExtensions.Value : Instance.Toggles.Count - 21;
        static void PrepareDialogEvents() =>
            ToDialogButtons(HumanCustom.Instance.Dialog.gameObject.transform
                .Find("Dialog").Find("Dialog_Panel").Find("BaseFrame").Find("Buttons"))
                .With(PrepareDialogAccept).With(PrepareDialogCancel);
        static (Button, Button) ToDialogButtons(Transform tf) =>
            (tf.Find("btnEnter").gameObject.GetComponent<Button>(), tf.Find("btnCancel").gameObject.GetComponent<Button>());
        static void InitializeUI() =>
          Instance = new UI(HumanCustom.Instance.SelectionTop.transform.Find("04_Accessories"));
        static ConfigEntry<int> CustomExtensions;
        internal static void Initialize()
        {
            CustomExtensions = Plugin.Instance.Config.Bind("HumanCustom", "Default accessories extension slots", 20);
            Util<HumanCustom>.Hook(() =>
            {
                Util.OnCustomHumanReady(InitializeUI);
                RemoveAllEvent = HumanCustom.Instance.SelectionTop.OnAccessoryAllRemove().Subscribe(PrepareEvents);
                Event.OnPreCoordinateReload += OnPreCoordinateReload;
                Event.OnCoordinateSerialize += OnCoordinateSerialize;
                Event.OnCharacterSerialize += OnCharacterSerialize; 
            }, () =>
            {
                Instance = null;
                RemoveAllEvent.Dispose();
                Event.OnPreCoordinateReload -= OnPreCoordinateReload;
                Event.OnCoordinateSerialize -= OnCoordinateSerialize;
                Event.OnCharacterSerialize -= OnCharacterSerialize; 
            });
        }
        static void OnCoordinateSerialize(HumanDataCoordinate _, ZipArchive archive) =>
            CoordMods.ToMod(HumanCustom.Instance.Human).Save(archive);
        static void OnCharacterSerialize(HumanData _, ZipArchive archive) =>
            CharaMods.Load(archive).With(mods => mods.Store(HumanCustom.Instance.Human)).Save(archive); 
        static void OnPreCoordinateReload(Human human, int type, ZipArchive archive) =>
            CharaMods.Load(archive).With(mods => mods.Store(human)).Save(archive);
    }
    internal static partial class Hooks
    {
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        [HarmonyPatch(typeof(Accessory_00), nameof(Accessory_00.UpdateCustomUI), [])]
        static void UpdateCustomUIPostfix() =>
            UI.UpdateSlotTitles();
    }
    static partial class AccessoryExtensions
    {
        internal static int ExtensionSlots => UI.ExtensionSlots;
        static void OnPostCoordinateDeserialize(Human human, HumanDataCoordinate _, CoordLimit limit, ZipArchive archive, ZipArchive storage) =>
            ((limit & CoordLimit.Accessory) != 0).Maybe(F.Apply(CoordMods.Apply, archive, human));
        static void OnPostCharacterDeserialize(Human human, CharaLimit limit, ZipArchive archive, ZipArchive storage) =>
            ((limit & CharaLimit.Coorde) != 0).Maybe(ApplyCharaMods.Apply(archive).Apply(storage).Apply(human));
        static void OnPostCoordinateReload(Human human, int type, ZipArchive archive) =>
            CharaMods.Load(archive).Apply(human, type);
        static void OnPostActorHumanize(SaveData.Actor _, Human human, ZipArchive archive) =>
            CharaMods.Load(archive).Apply(human, human.data.Status.coordinateType);
        internal static Action<ZipArchive, ZipArchive, Human> ApplyCharaMods =
            (archive, storage, human) => CharaMods.Load(archive)
                .With(mods => mods.Save(storage)).Apply(human, human.data.Status.coordinateType);
        static bool GetInfo(Human _, ChaListDefine.CategoryNo category, int id, ChaListDefine.KeyType key, out string value) =>
            Human.lstCtrl.GetInfo(category, id, key, out value);
        internal static void Initialize()
        {
            Event.OnPostCoordinateDeserialize += OnPostCoordinateDeserialize;
            Event.OnPostCharacterDeserialize += OnPostCharacterDeserialize;
            Event.OnPostCoordinateReload += OnPostCoordinateReload;
            Event.OnPostActorHumanize += OnPostActorHumanize;
        }
    }
    public partial class Plugin : BasePlugin
    {
        public const string Process = "SamabakeScramble";
        public override void Load() =>
            ((Instance, Patch) = (this, Harmony.CreateAndPatchAll(typeof(Hooks), $"{Name}.Hooks")))
                .With(UI.Initialize).With(AccessoryExtensions.Initialize);
    }
}