using System;
using BattleTech;
using BattleTech.UI;
using CustomComponents;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CustomSalvage;

public class AAR_SalvageChosenCustom: MonoBehaviour
{
    private const int VisibleRows = 8;
    public AAR_SalvageChosen parent;
    private ScrollRect scrollRect;
    private LayoutElement scrollLayoutElement;
    public void Init(AAR_SalvageChosen parentElement)
    {
        parent = parentElement;
        while (parent.tempHoldingGridSpaces.Count > 1)
        {
            GameObject gameObjectElement = parent.tempHoldingGridSpaces[parent.tempHoldingGridSpaces.Count - 1];
            parent.tempHoldingGridSpaces.RemoveAt(parent.tempHoldingGridSpaces.Count - 1);
            DestroyImmediate(gameObjectElement);
        }
        InitScroll();
    }

    private void InitScroll()
    {
        if (scrollRect != null)
        {
            return;
        }

        RectTransform content = parent.chosenListParent as RectTransform;
        ScrollRect template = FindScrollRect(parent.leftoverListParent);
        if (content == null || template == null || template.verticalScrollbar == null)
        {
            Log.Main.Warning?.Log($"AAR_SalvageChosenCustom unable to make the salvage list scrollable. Content exist={content != null}. Template exist={template != null}.");
            return;
        }

        RectTransform scrollBox = CreateScrollBox(content);
        RectTransform viewport = CreateViewport(scrollBox);
        scrollBox.gameObject.SetActive(content.gameObject.activeSelf);
        MoveContentIntoViewport(content, viewport);
        if (parent.PriorityAreaBackground != null)
        {
            parent.PriorityAreaBackground.transform.SetParent(scrollBox, false);
            parent.PriorityAreaBackground.transform.SetAsFirstSibling();
        }

        scrollRect = CreateScrollRect(scrollBox, content, viewport, template);
        parent.PriorityArea = scrollBox.gameObject;
    }

    private RectTransform CreateScrollBox(RectTransform content)
    {
        GameObject scrollGameObject = new GameObject("PrioritySlots_scrollview", typeof(RectTransform));
        scrollGameObject.layer = content.gameObject.layer;
        RectTransform scrollTransform = scrollGameObject.GetComponent<RectTransform>();
        scrollTransform.SetParent(content.parent, false);
        scrollTransform.SetSiblingIndex(content.GetSiblingIndex());
        scrollTransform.sizeDelta = new Vector2(content.sizeDelta.x, 0f);
        scrollLayoutElement = scrollGameObject.AddComponent<LayoutElement>();
        return scrollTransform;
    }

    private static RectTransform CreateViewport(RectTransform scrollTransform)
    {
        GameObject viewportGameObject = new GameObject("Viewport", typeof(RectTransform));
        viewportGameObject.layer = scrollTransform.gameObject.layer;
        RectTransform viewport = viewportGameObject.GetComponent<RectTransform>();
        viewport.SetParent(scrollTransform, false);
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.pivot = new Vector2(0f, 1f);
        viewport.sizeDelta = Vector2.zero;
        viewport.anchoredPosition = Vector2.zero;
        viewportGameObject.AddComponent<Image>().color = Color.clear;
        viewportGameObject.AddComponent<RectMask2D>();
        return viewport;
    }

    private static void MoveContentIntoViewport(RectTransform content, RectTransform viewport)
    {
        content.gameObject.SetActive(true);
        content.SetParent(viewport, false);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(0f, 1f);
        content.pivot = new Vector2(0f, 1f);
        content.anchoredPosition = Vector2.zero;
    }

    private static ScrollRect CreateScrollRect(RectTransform scrollTransform, RectTransform content, RectTransform viewport, ScrollRect template)
    {
        ScrollRect result = scrollTransform.gameObject.AddComponent<ScrollRect>();
        result.content = content;
        result.viewport = viewport;
        result.horizontal = false;
        result.vertical = true;
        result.movementType = template.movementType;
        result.elasticity = template.elasticity;
        result.inertia = template.inertia;
        result.decelerationRate = template.decelerationRate;
        result.scrollSensitivity = template.scrollSensitivity;
        Scrollbar bar = Instantiate(template.verticalScrollbar, scrollTransform, false);
        bar.gameObject.layer = scrollTransform.gameObject.layer;
        result.verticalScrollbar = bar;
        result.verticalScrollbarVisibility = template.verticalScrollbarVisibility;
        result.verticalScrollbarSpacing = template.verticalScrollbarSpacing;
        return result;
    }

    private void UpdateScrollHeight()
    {
        if (scrollRect == null)
        {
            return;
        }

        GridLayoutGroup grid = scrollRect.content.GetComponent<GridLayoutGroup>();
        int rows = parent.PriorityInventory.Count + (parent.tempHoldingGridSpaces[0].activeSelf ? 1 : 0);
        int visibleRows = Mathf.Min(rows, VisibleRows);
        float padding = grid.padding.vertical;
        float slots = grid.cellSize.y * visibleRows;
        float spacing = grid.spacing.y * Mathf.Max(visibleRows - 1, 0);
        scrollLayoutElement.preferredHeight = padding + slots + spacing;
    }

    private static ScrollRect FindScrollRect(Transform transform)
    {
        while (transform != null)
        {
            ScrollRect result = transform.GetComponent<ScrollRect>();
            if (result != null)
            {
                return result;
            }

            transform = transform.parent;
        }
        return null;
    }
    
    public void Refresh(bool scrollToEnd = false)
    {
        int curSalvageWeight = this.parent.contract.GetSalvageWeight(this.parent.PriorityInventory);
        int restSalvage = this.parent.contract.FinalSalvageCount - curSalvageWeight;
        int restRandom = restSalvage - this.parent.contract.FinalPrioritySalvageCount + this.parent.PriorityInventory.Count;
        int maxPriority = this.parent.contract.FinalPrioritySalvageCount;
        if (restRandom < 0) { maxPriority += restRandom; restRandom = 0; }
        Log.Main.Debug?.Log($"AAR_SalvageChosenCustom.Refresh priority {this.parent.PriorityInventory.Count} weight:{curSalvageWeight}");
        this.parent.selectNumberText.SetText("SELECTED {0}/{1}", this.parent.PriorityInventory.Count, maxPriority);
        if(maxPriority <= this.parent.PriorityInventory.Count)
        {
            this.parent.tempHoldingGridSpaces[0].SetActive(false);
        }
        else
        {
            this.parent.tempHoldingGridSpaces[0].SetActive(true);
        }
        this.parent.leftoversText.SetText("AFTER CHOOSING PRIORITY ITEMS YOU WILL RECEIVE UP TO {0} ADDITIONAL PIECES OF SALVAGE", restRandom);
        this.parent.parent.salvageAgreement.TotalNumberText.SetText($"{restRandom + maxPriority}");
        this.parent.parent.salvageAgreement.PriorityNumberText.SetText($"{maxPriority}");
        this.parent.parent.gameObject.GetComponent<AAR_ScreenFullMechHelper>()?.CheckDisassemble();
        this.parent.ApplySorting();
        this.parent.TestHaveAllPriority();
        this.parent.PriorityAreaBackground.transform.SetAsFirstSibling();
        UpdateScrollHeight();
        this.parent.ForceRefreshImmediate();
        if (scrollToEnd && scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }
}

[HarmonyPatch(typeof(AAR_SalvageChosen), "HasAllPriority")]
[HarmonyPatch(new Type[] { })]
internal static class AAR_SalvageChosen_HasAllPriority
{
    public static void Prefix(ref bool __runOriginal, AAR_SalvageChosen __instance, ref bool __result)
    {
        if (__runOriginal == false) { return; }
        __result = true;
        __runOriginal = false;
    }
    public static void Postfix(AAR_SalvageChosen __instance, ref bool __result)
    {
        __result = true;
    }
}
public class AAR_SalvageChosen_Confirm : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public bool hovered = false;
    public bool clicked = false;
    public AAR_SalvageChosen parent = null;
    public AAR_SalvageChosenCustom parentCutom = null;
    public void Init(AAR_SalvageChosen p, AAR_SalvageChosenCustom pc)
    {
        this.parent = p;
        this.parentCutom = pc;
        hovered = false;
        clicked = false;
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        Log.Main.Debug?.Log($"AAR_SalvageChosen_Confirm.OnPointerClick");
        if(this.parent == null)
        {
            Log.Main.Debug?.Log($" -- no parent. skipping");
        }
        if (hovered == false) {
            Log.Main.Debug?.Log($" -- not hovered. skipping");
        }
        if(clicked == true)
        {
            Log.Main.Debug?.Log($" -- multiply clicks detected. skipping");
        }
        try
        {
            this.parentCutom.Refresh();
            this.clicked = true;
            if (this.parent.tempHoldingGridSpaces[0].activeSelf)
            {
                GenericPopupBuilder.Create(GenericPopupType.Warning, "You still not filled priority slots. They will be filled randomly. Are you sure to proceed?")
                    .AddButton("OK", () =>
                    {
                        this.parent.parent.SalvageConfirmed();
                    }, true).AddButton("Cancel", () =>
                    {
                        this.clicked = false;
                    }, true).SetOnClose(() => { this.clicked = false; }).AddFader().SetAlwaysOnTop().Render();
            }
            else
            {
                this.parent.parent.SalvageConfirmed();
            }
        }
        catch(Exception e)
        {
            UIManager.logger.LogException(e);
        }
        //throw new NotImplementedException();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Log.Main.Debug?.Log($"AAR_SalvageChosen_Confirm.OnPointerEnter");
        hovered = true;
        //throw new NotImplementedException();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Log.Main.Debug?.Log($"AAR_SalvageChosen_Confirm.OnPointerExit");
        hovered = false;
        //throw new NotImplementedException();
    }
}

[HarmonyPatch(typeof(AAR_SalvageChosen), "Init")]
[HarmonyPatch(new Type[] { typeof(AAR_SalvageScreen), typeof(SimGameState), typeof(Contract) })]
internal static class AAR_SalvageChosen_Init
{
    public static void Postfix(AAR_SalvageChosen __instance, AAR_SalvageScreen theScreen, SimGameState sim, Contract theContract)
    {
        var custom = __instance.gameObject.GetComponent<AAR_SalvageChosenCustom>();
        if (custom == null) { custom = __instance.gameObject.AddComponent<AAR_SalvageChosenCustom>(); }
        custom.Init(__instance);
        AAR_SalvageChosen_Confirm confirm = __instance.confirmButton.gameObject.GetComponent<AAR_SalvageChosen_Confirm>();
        if (confirm == null) { confirm = __instance.confirmButton.gameObject.AddComponent<AAR_SalvageChosen_Confirm>(); }
        confirm.Init(__instance, custom);
        AAR_SalvageScreen_ReceiveButtonPress.ClearFlag();
    }
}


[HarmonyPatch(typeof(AAR_SalvageChosen), "ReceiveButtonPress")]
[HarmonyPriority(Priority.HigherThanNormal)]
internal static class AAR_SalvageScreen_ReceiveButtonPress
{
    private static bool ReceiveButtonPress_flag = false;
    public static void ClearFlag() { ReceiveButtonPress_flag = false; }
    public static void SetFlag() { ReceiveButtonPress_flag = true; }
    [HarmonyPrefix]
    [HarmonyWrapSafe]
    [HarmonyPriority(Priority.HigherThanNormal)]
    public static void Prefix(ref bool __runOriginal, AAR_SalvageChosen __instance, string button)
    {
        try
        {            
            if (__runOriginal == false) { return; }
            if (button != "ConfirmSalvage") { return; }
            __runOriginal = false;
            return;
            //Log.Main.Debug?.Log($"AAR_SalvageChosen.ReceiveButtonPress flag:{ReceiveButtonPress_flag}");
            //if (ReceiveButtonPress_flag) { __runOriginal = false; return; }
            //ReceiveButtonPress_flag = true;
        }
        catch (Exception e)
        {
            UIManager.logger.LogException(e);
        }
    }
}

[HarmonyPatch(typeof(AAR_SalvageChosen), "SetInitialText")]
internal static class AAR_SalvageChosen_SetInitialText
{
    public static void Prefix(ref bool __runOriginal, AAR_SalvageChosen __instance)
    {
        if (__runOriginal == false) { return; }
        //__instance.contract.FinalSalvageCount = 15;
        //__instance.contract.FinalPrioritySalvageCount = 5;
        var custom = __instance.gameObject.GetComponent<AAR_SalvageChosenCustom>();
        if (custom == null) { return; }
        __runOriginal = false;
        __instance.currentSort = new Comparison<InventoryItemElement_NotListView>(__instance.SortBy_SalvageTypeThenName);
        custom.Refresh();
    }
}


[HarmonyPatch(typeof(AAR_SalvageChosen), "OnAddItem")]
internal static class AAR_SalvageChosen_OnAddItem
{
    public static void Prefix(ref bool __runOriginal, AAR_SalvageChosen __instance, IMechLabDraggableItem item, bool validate, ref bool __result)
    {
        if (!__runOriginal) { return; }
        try
        {
            InventoryItemElement_NotListView elementNotListView = item as InventoryItemElement_NotListView;
            ListElementController_BASE_NotListView controller = elementNotListView.controller;
            if (controller == null) { goto main_process; }
            if (controller.salvageDef == null) { goto main_process; }
            if (controller.salvageDef.Type != SalvageDef.SalvageType.MECH) { goto main_process; }

            if (controller.salvageDef.mechDef.Chassis.Is<LootableUniqueMech>(out var ulm) && ulm.BlockAssembly)
            {
                string chassisID = controller.salvageDef.mechDef.ChassisID;
                if (__instance.Sim.IsHaveActiveChassis(chassisID) || IsChassisAlreadyInSelectedSalvage(chassisID, __instance))
                {
                    __runOriginal = false;
                    GenericPopupBuilder.Create("UNABLE TO COMPLY", $"Only allowed to have one assembled copy of this unit").CancelOnEscape().Render();
                    __result = false;
                    return;
                }
            }

            if (Control.Instance.Settings.MaxFullUnitsInSalvage > 0)
            {
                int fullunits = 0;
                foreach (var salvage in __instance.PriorityInventory)
                {
                    if (salvage.controller.salvageDef.Type == SalvageDef.SalvageType.MECH) { ++fullunits; }
                }
                if (fullunits >= Control.Instance.Settings.MaxFullUnitsInSalvage)
                {
                    __runOriginal = false;
                    GenericPopupBuilder.Create("UNABLE TO COMPLY", $"You can only have up to {Control.Instance.Settings.MaxFullUnitsInSalvage} units in priority salvage").CancelOnEscape().Render();
                    __result = false;
                    return;
                }
            }
            int restSlots = __instance.contract.GetFinalSalvageCount(__instance.PriorityInventory) - __instance.PriorityInventory.Count;
            int neededSlots = controller.salvageDef.mechDef.RandomSlotsUsing(__instance.Sim.Constants) + 1;
            if (restSlots < neededSlots)
            {
                __runOriginal = false;
                GenericPopupBuilder.Create("UNABLE TO COMPLY", $"You have only {restSlots} of salvage. But to get this unit you need {neededSlots}").CancelOnEscape().Render();
                __result = false;
                return;
            }
        main_process:
            Log.Main.Debug?.Log($"AAR_SalvageChosen.OnAddItem");
            if (__instance.tempHoldingGridSpaces[0].activeSelf == false) {
                __runOriginal = false;
                __result = false;
                return;
            }
            __instance.PriorityInventory.Add(elementNotListView);
            __instance.parent.RemoveFromInventoryList(elementNotListView);
            elementNotListView.DropParent = (IMechLabDropTarget)__instance;
            HBSDOTweenToggle component = elementNotListView.GetComponent<HBSDOTweenToggle>();
            if (component != null) { component.SetState(ButtonState.Enabled); }
            elementNotListView.gameObject.transform.SetParent(__instance.chosenListParent, false);
            elementNotListView.gameObject.transform.localScale = Vector3.one;
            AAR_SalvageChosenCustom custom = __instance.gameObject.GetComponent<AAR_SalvageChosenCustom>();
            if (custom != null)
            {
                custom.Refresh(true);
            }
            else
            {
                Log.Main.Warning?.Log($"can't find AAR_SalvageChosenCustom component on {__instance.gameObject.name}");
            }
            __runOriginal = false;
            __result = true;
        }catch(Exception e)
        {
            __runOriginal = false;
            __result = false;
            UIManager.logger.LogException(e);
        }
    }
    public static void Postfix(AAR_SalvageChosen __instance, IMechLabDraggableItem item, bool __result)
    {
        if (__result == false) { return; }
        try
        {
            InventoryItemElement_NotListView elementNotListView = item as InventoryItemElement_NotListView;
            FullMechSalvageInfo info = elementNotListView.gameObject.GetComponentInChildren<FullMechSalvageInfo>(true);
            if (info != null)
            {
                info.allowDisassemble = false;
            }
            AAR_ScreenFullMechHelper helper = __instance.parent.gameObject.GetComponent<AAR_ScreenFullMechHelper>();
            helper?.CheckDisassemble();
        }catch(Exception e)
        {
            UIManager.logger.LogException(e);
        }
    }

    // Not really needed to check as any extra copies should have been made to parts, but extra safe guard
    private static bool IsChassisAlreadyInSelectedSalvage(string chassisId, AAR_SalvageChosen aarSalvageChosen)
    {
        foreach (InventoryItemElement_NotListView inventoryItemElementNotListView in aarSalvageChosen.PriorityInventory)
        {
            SalvageDef salvageDef = inventoryItemElementNotListView.controller?.salvageDef;
            if (salvageDef == null) { continue; }
            if (salvageDef.Type != SalvageDef.SalvageType.MECH) { continue; }
            if (salvageDef.ComponentType != ComponentType.MechFull) { continue; }
            if (salvageDef.mechDef == null) { continue; }
            if (salvageDef.mechDef.chassisID != chassisId) { continue; }
            return true;
        }
        return false;
    }
}

[HarmonyPatch(typeof(AAR_SalvageChosen), "OnRemoveItem")]
internal static class AAR_SalvageChosen_OnRemoveItem
{
    public static void Prefix(ref bool __runOriginal, AAR_SalvageChosen __instance, IMechLabDraggableItem item, ref bool __result)
    {
        if (__runOriginal == false) { return; }
        try
        {
            if (!__instance.IsSimGame) { __result = false; __runOriginal = false; return; }
            Log.Main.Debug?.Log($"AAR_SalvageChosen.OnRemoveItem");
            __instance.PriorityInventory.Remove(item as InventoryItemElement_NotListView);
            AAR_SalvageChosenCustom custom = __instance.gameObject.GetComponent<AAR_SalvageChosenCustom>();
            if (custom != null)
            {
                custom.Refresh();
            }
            else
            {
                Log.Main.Warning?.Log($"can't find AAR_SalvageChosenCustom component on {__instance.gameObject.name}");
            }
            __result = false; __runOriginal = false;
        }
        catch(Exception e)
        {
            __result = false; __runOriginal = false;
            UIManager.logger.LogException(e);
        }
    }
    public static void Postfix(AAR_SalvageChosen __instance, IMechLabDraggableItem item)
    {
        InventoryItemElement_NotListView elementNotListView = item as InventoryItemElement_NotListView;
        FullMechSalvageInfo info = elementNotListView.gameObject.GetComponentInChildren<FullMechSalvageInfo>(true);
        if (info != null)
        {
            info.allowDisassemble = true;
        }
    }
}
