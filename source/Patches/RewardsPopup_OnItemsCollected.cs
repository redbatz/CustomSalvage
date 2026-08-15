using System.Collections.Generic;
using System.Linq;
using BattleTech;
using BattleTech.UI;
using CustomComponents;
using CustomUnits;

namespace CustomSalvage.Patches;

[HarmonyPatch(typeof(RewardsPopup), "OnItemsCollected")]
public static class RewardsPopup_OnItemsCollected
{
    [HarmonyPrefix]
    [HarmonyWrapSafe]
    [HarmonyPriority(Priority.VeryHigh)]
    public static void Prefix(ref bool __runOriginal, RewardsPopup __instance, ItemCollectionResult result)
    {
        if (!__runOriginal)
        {
            return;
        }

        HashSet<string> foundUniquesWithBlockAssembly = new HashSet<string>();
        List<ShopDefItem> toParts = new List<ShopDefItem>();
        bool foundUnique = false;
        foreach (ShopDefItem item in result.items)
        {
            if (item.Type == ShopItemType.Mech)
            {
                string itemID = item.ID;
                if (foundUniquesWithBlockAssembly.Contains(itemID))
                {
                    Log.Main.Debug?.Log($"--- Mech {itemID} is unique with block assembly and already assembled, marking for forced disassembling.");
                    toParts.Add(item);
                }
                else if (__instance.dm.MechDefs.TryGet(itemID, out MechDef mechDef)
                         && mechDef.Chassis.Is<LootableUniqueMech>(out var lootableUniqueMech))
                {
                    foundUnique = true;
                    if (lootableUniqueMech.BlockAssembly)
                    {
                        foundUniquesWithBlockAssembly.Add(itemID);
                        if (__instance.sim.IsHaveActiveChassis(mechDef.ChassisID))
                        {
                            Log.Main.Debug?.Log($"--- Mech {itemID} is unique with block assembly and already assembled, marking for forced disassembling.");
                            toParts.Add(item);
                        }
                        else
                        {
                            Log.Main.Debug?.Log($"--- Mech {itemID} is unique with block assembly but none is assembled, keeping in result.");
                        }
                    }
                }
            }
        }

        if (toParts.Count == 0)
        {
            if (foundUnique)
            {
                ChassisHandler.SanitizeUniqueUnits(__instance.sim);
            }
            return;
        }

        foreach (ShopDefItem shopDefItem in toParts)
        {
            result.items.Remove(shopDefItem);
            if (!__instance.dm.MechDefs.TryGet(shopDefItem.ID, out MechDef mech))
            {
                Log.Main.Error?.Log($"--- Unable to find expected mech {shopDefItem.ID}");
                continue;
            }

            List<ShopDefItem> parts = ConvertToParts(shopDefItem, mech);
            result.items.AddRange(parts);
            Log.Main.Debug?.Log($"--- Replaced mech {mech.Description.Id} with {parts.Count} part(s) in result.");
        }
    }

    private static List<ShopDefItem> ConvertToParts(ShopDefItem shopDefItem, MechDef mech)
    {
        shopDefItem.Type = ShopItemType.MechPart;
        int numParts = mech.IsSquad() ? PartsNumCalculations.SquadPartsCount(mech) : Control.Instance.GetNumParts(mech);
        ShopDefItem part = new ShopDefItem(shopDefItem)
        {
            Type = ShopItemType.MechPart
        };
        return Enumerable.Repeat(part, numParts).ToList();
    }
}