using BepInEx.Configuration;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UniRx;
using UniRx.Triggers;
using Character;
using CharacterCreation;
using Fishbone;
using CoastalSmell;
using Parent = ChaAccessoryDefine.AccessoryParentKey;

namespace VarietyOfScales
{
    class UI
    {
        GameObject RootPanel;
        GameObject SlotPanel;
        CategorySelection SelectionUI;
        Il2CppSystem.Collections.Generic.List<CategoryKindToggle> SelectionToggles;
        Action<Unit> OnShow =>
            _ => RootPanel.SetActive(true);
        Action<Unit> OnHide =>
            _ => RootPanel.SetActive(false);
        Action<bool> OnCustomHideEvent =>
            value => RootPanel.SetActive(!value && HumanCustom.Instance.NowCategory.Category == 4);
        Action<Unit> RemoveAll;
        event Action OnSlotUpdate = delegate {};
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
            .With(UGUI.Cmp(UGUI.LayoutGroup<VerticalLayoutGroup>(
                spacing: 2, padding: new() { left = 5, right = 5, bottom = 5, top = 5 })));

        UI(ObservableEnableTrigger trigger, Button button) : this() =>
            (_, _, RemoveAll) = (
                trigger.OnEnableAsObservable().Subscribe(OnShow),
                trigger.OnDisableAsObservable().Subscribe(OnHide),
                _ => button.onClick.Invoke());
        UI(ObservableEnableTrigger trigger, GameObject index, Button button) : this(trigger, button) =>
            index.AddComponent<ObservableEnableTrigger>().OnEnableAsObservable()
                .Subscribe(F.Apply(index.SetActive, false).Ignoring<Unit>());
        UI(GameObject acs, Transform index) :
            this(acs.AddComponent<ObservableEnableTrigger>(), index.gameObject,
                index.GetChild(index.childCount - 1).gameObject.GetComponent<Button>()) =>
            (SelectionUI = acs.GetComponent<CategorySelection>())._kindToggles =
                new Il2CppSystem.Collections.Generic.IReadOnlyList<CategoryKindToggle>((SelectionToggles = new()).Pointer);
        UI(Transform tf) : this(tf.gameObject, tf.Find("Index")) =>
            new GameObject("Controls").With(UGUI.Go(parent: RootPanel.transform))
                .With(UGUI.Cmp(UGUI.LayoutGroup<HorizontalLayoutGroup>(spacing: 2)))
                .With(PrepareDecrease).With(PrepareIncrease).With(PrepareContents)
                .With(PrepareOpenCopy).With(PrepareRemoveAll)
                .With(SubscribeHumanCustomHideUI).With(OnSlotUpdate);
        void SubscribeHumanCustomHideUI() =>
            HumanCustom.Instance.HideUIEvent.Subscribe(OnCustomHideEvent);

        void PrepareDecrease(GameObject parent) =>
            UGUI.Button(112, 24, "Slot-", parent)
                .GetComponent<Button>().OnClickAsObservable()
                .Subscribe((Action<Unit>)(_ => SlotUpdate(HumanCustom.Instance.Human, -1)));

        void PrepareIncrease(GameObject parent) =>
            UGUI.Button(112, 24, "Slot+", parent)
                .GetComponent<Button>().OnClickAsObservable()
                .Subscribe((Action<Unit>)(_ => SlotUpdate(HumanCustom.Instance.Human, +1)));

        void SlotUpdate(Human human, int count) =>
            (F.Apply(human.PrepareSlots, human.acs.Accessories.Count + count) + OnSlotUpdate.Invoke).Invoke();

        void PrepareContents() =>
            SlotPanel = UGUI.ScrollView(235, 480, "Slots", RootPanel)
                .With(UGUI.Cmp(UGUI.LayoutGroup<VerticalLayoutGroup>()))
                .With(UGUI.Cmp(UGUI.ToggleGroup(allowSwitchOff: false)))
                .With(UGUI.Cmp(UGUI.Fitter())).With(PrepareSlotToggles);
        void PrepareSlotToggles(GameObject go) =>
            Enumerable.Range(0, 99).ForEach(slot =>
                PrepareSlotToggles(go, slot, HumanCustom.Instance.GetSlotName(slot)));

        void PrepareSlotToggles(GameObject go, int slot, string title) =>
            UGUI.Toggle(220, 24, $"Slot{slot}", go)
                .With(UGUI.Cmp<Toggle, ToggleGroup>((ui, group) => ui.group = group))
                .With(UGUI.Cmp<CategoryViewBinderUnion>(ui => ui.file = "00_AcsSlot"))
                .With(UGUI.Cmp<CategoryKindToggle>(SelectionToggles.Add))
                .With(UGUI.Cmp<CategoryKindToggle>(ui => ui._toggle = ui.gameObject.GetComponent<Toggle>()))
                .With(UGUI.ModifyAt($"Slot{slot}.State")(
                    UGUI.Cmp<CharacterCreation.Text, CategoryKindToggle>((txt, ckt) => ckt._title = txt)))
                .With(UGUI.ModifyAt($"Slot{slot}.State", $"Slot{slot}.Label")(
                    UGUI.Cmp<TextMeshProUGUI, CharacterCreation.Text>((ui, txt) => (txt._tmpText = ui).SetText(title))))
                .With(go => OnSlotUpdate += () => go.active = slot < HumanCustom.Instance.Human.acs.Accessories.Count)
                .GetComponent<Toggle>().OnValueChangedAsObservable()
                .Subscribe((Action<bool>)(value => value.Maybe(F.Apply(SelectionUI.OpenView, slot))));

        void PrepareOpenCopy() =>
            UGUI.Toggle(235, 24, "Make Copy", RootPanel)
                .With(UGUI.Cmp<Toggle>(ui => UGUI.Cmp<ToggleGroup>(group => ui.group = group)(SlotPanel)))
                .With(UGUI.Cmp(UGUI.Image(color: new Color(0.5f, 0.5f, 0.5f, 0.7f), sprite: BorderSprites.ColorBg.Get())))
                .With(UGUI.Cmp<CategoryViewBinderUnion>(ui => ui.file = "01_AcsCopy"))
                .With(UGUI.Cmp<CategoryKindToggle>(SelectionToggles.Add))
                .With(UGUI.Cmp<CategoryKindToggle>(ui => ui._toggle = ui.gameObject.GetComponent<Toggle>()))
                .With(UGUI.ModifyAt($"Make Copy.State")(
                    UGUI.Cmp<HorizontalLayoutGroup>(ui => ui.childAlignment = TextAnchor.MiddleCenter) +
                    UGUI.Cmp<CharacterCreation.Text, CategoryKindToggle>((txt, ckt) => ckt._title = txt)))
                .With(UGUI.ModifyAt("Make Copy.State", "Make Copy.Label")(
                    UGUI.Cmp<TextMeshProUGUI, CharacterCreation.Text>((ui, txt) =>
                        (txt._tmpText = ui).horizontalAlignment = HorizontalAlignmentOptions.Center)))
                .GetComponent<Toggle>().OnValueChangedAsObservable()
                .Subscribe((Action<bool>)(value => value.Maybe(F.Apply(SelectionUI.OpenView, 99))));

        void PrepareRemoveAll() =>
            UGUI.Button(235, 24, "Deselect All", RootPanel)
                .GetComponent<Button>().OnClickAsObservable().Subscribe(RemoveAll);

        static void UpdateSlotTitle(int index, string title) =>
            Instance.SelectionToggles[index]._title._tmpText.SetText(title, false);

        static void UpdateSlotTitle(int index) =>
            UpdateSlotTitle(index, HumanCustom.Instance.GetSlotName(index));

        static UI Instance;

        internal static void UpdateSlotTitles() =>
            Enumerable.Range(20, Instance.SelectionToggles.Count - 21).ForEach(UpdateSlotTitle);

        static Il2CppSystem.IDisposable RemoveAllEvent;

        static CompositeDisposable DialogEvents;

        static Action<Unit> PrepareEvents =
            _ => (DialogEvents = new CompositeDisposable()).With(PrepareDialogEvents);

        static Action<Unit> CleanupDialogEvents =
            _ => DialogEvents.Dispose();

        static Action<Unit> CleanupExtensions =
            _ => Enumerable.Range(20, HumanCustom.Instance.Human.acs.Accessories.Count - 20)
                .ForEach(slot => HumanCustom.Instance.Human.acs.Change(slot,
                    (int)ChaListDefine.CategoryNo.ao_none, 0, Parent.RootBone, true));

        static void PrepareDialogAccept((Button, Button) buttons) =>
            DialogEvents.Add(buttons.Item1.OnClickAsObservable().Subscribe(CleanupExtensions + CleanupDialogEvents));

        static void PrepareDialogCancel((Button, Button) buttons) =>
            DialogEvents.Add(buttons.Item2.OnClickAsObservable().Subscribe(CleanupDialogEvents));

        static void PrepareDialogEvents() =>
            ToDialogButtons(HumanCustom.Instance.Dialog.gameObject.transform
                .Find("Dialog").Find("Dialog_Panel").Find("BaseFrame").Find("Buttons"))
                .With(PrepareDialogAccept).With(PrepareDialogCancel);

        static (Button, Button) ToDialogButtons(Transform tf) => (
            tf.Find("btnEnter").gameObject.GetComponent<Button>(),
            tf.Find("btnCancel").gameObject.GetComponent<Button>()
        );

        static void InitializeUI() {
            Instance = new UI(HumanCustom.Instance.SelectionTop.transform.Find("04_Accessories"));
            Extension.OnLoadChara += _ => Instance.OnSlotUpdate();
            Extension.OnLoadCoord += _ => Instance.OnSlotUpdate();
        }

        internal static int SlotCount =>
            HumanCustom.Instance == null ? 20 : CustomExtensions.Value;

        static ConfigEntry<int> CustomExtensions;

        internal static void Initialize()
        {
            CustomExtensions = Plugin.Instance.Config
                .Bind("Character Creation", "Default slots", 40,
                    new ConfigDescription("Initial slots in character creation", new AcceptableValueRange<int>(20, 99)));

            Util<HumanCustom>.Hook(() =>
            {
                Util.OnCustomHumanReady(InitializeUI);
                RemoveAllEvent = HumanCustom.Instance.SelectionTop.OnAccessoryAllRemove().Subscribe(PrepareEvents);
            }, () =>
            {
                Instance = null;
                RemoveAllEvent.Dispose();
            });
        }
    }
}