using System.Linq;
using BattleTech;
using CustomComponents;

namespace CustomSalvage;

public static class SalvageChanceHelper
{
    public static void RollInventory(MechDef mech, SimGameState sim)
    {
        MechComponentRef[] inventory = mech.Inventory.ToArray();
        bool changed = false;
        for (int i = 0; i < inventory.Length; i++)
        {
            MechComponentRef inventoryComponentRef = inventory[i];
            if (!TryGetSalvageChance(inventoryComponentRef, out SalvageChanceCustom salvageChance) || RollSalvageChance($"{mech.Description.Id} {inventoryComponentRef.ComponentDefID}", salvageChance, sim))
            {
                continue;
            }

            MechComponentRef replacement = salvageChance.DropLootableDefault ? CreateLootableReplacement(inventoryComponentRef, sim) : null;
            if (replacement != null)
            {
                inventory[i] = replacement;
                changed = true;
                Log.Main.Debug?.Log($"--- {inventoryComponentRef.ComponentDefID} replaced with {replacement.ComponentDefID}");
                continue;
            }

            inventoryComponentRef.DamageLevel = ComponentDamageLevel.Destroyed;
            Log.Main.Debug?.Log($"--- {inventoryComponentRef.ComponentDefID} destroyed");
        }

        if (changed)
        {
            mech.SetInventory(inventory);
        }
    }

    public static bool RollSalvageChance(string id, SalvageChanceCustom salvageChance, SimGameState sim)
    {
        float roll = sim.NetworkRandom.Float();
        bool success = roll < salvageChance.Chance;
        Log.Main.Debug?.Log($"--- {id} salvage chance roll {roll:0.000} vs {salvageChance.Chance:0.00}: {(success ? "passed" : "failed")}");
        return success;
    }

    public static MechComponentDef GetLootableDefault(MechComponentDef def)
    {
        if (!def.Is(out LootableDefault lootable))
        {
            return null;
        }

        MechComponentDef lootableDef = UnityGameInstance.BattleTechGame.DataManager.GetMechComponentDef(def.ComponentType, lootable.ItemID);
        if (lootableDef == null || lootableDef.CCFlags().NoSalvage)
        {
            return null;
        }

        return lootableDef;
    }

    private static bool TryGetSalvageChance(MechComponentRef inventoryComponentRef, out SalvageChanceCustom salvageChance)
    {
        salvageChance = null;
        if (inventoryComponentRef?.Def == null || inventoryComponentRef.DamageLevel == ComponentDamageLevel.Destroyed || inventoryComponentRef.Def.CCFlags().NoSalvage)
        {
            return false;
        }

        return inventoryComponentRef.Def.Is(out salvageChance);
    }

    private static MechComponentRef CreateLootableReplacement(MechComponentRef inventoryComponentRef, SimGameState sim)
    {
        MechComponentDef lootableDef = GetLootableDefault(inventoryComponentRef.Def);
        if (lootableDef == null)
        {
            return null;
        }

        MechComponentRef replacement = new MechComponentRef(lootableDef.Description.Id, inventoryComponentRef.SimGameUID, lootableDef.ComponentType, inventoryComponentRef.MountedLocation, inventoryComponentRef.HardpointSlot, ComponentDamageLevel.Functional, inventoryComponentRef.IsFixed);
        replacement.DataManager = sim.DataManager;
        replacement.RefreshComponentDef();
        return replacement;
    }
}
