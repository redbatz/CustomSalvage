using BattleTech;
using CustomComponents;

namespace CustomSalvage;

[HarmonyPatch(typeof(SimGameState), "AddMechByID")]
public static class SimGameState_AddMechByID
{
    [HarmonyPrefix]
    [HarmonyWrapSafe]
    [HarmonyPriority(Priority.VeryHigh)]
    public static void Prefix(ref bool __runOriginal, SimGameState __instance, string id, bool active, out bool __state)
    {
        __state = false;
        if (!__runOriginal)
        {
            return;
        }

        if (id == null)
        {
            return;
        }

        if (UnityGameInstance.BattleTechGame.DataManager.MechDefs.TryGet(id, out MechDef mech)
            && mech.Chassis.Is(out LootableUniqueMech lootableUniqueMech) 
            && UnityGameInstance.BattleTechGame.Simulation.IsHaveActiveChassis(mech.ChassisID))
        {
            if (lootableUniqueMech.BlockAssembly)
            {
                Log.Main.Debug?.Log($"--- Mech {mech.Description.Id} is unique with block assembly and already assembled, will add parts instead of new mech.");
                ChassisHandler.AddMechParts(__instance, mech);
                string message = new Localize.Text("__/CS.UNITS_REPLACED.BLOCKED.ACQUIRE/__", mech.Description.UIName).ToString();
                __instance.interruptQueue.QueuePauseNotification("__/CS.UNITS_REPLACED.TITLE/__", message,
                    __instance.GetCrewPortrait(SimGameCrew.Crew_Yang), null, () => { });
                __runOriginal = false;
            }
            else
            {
                // unique that could have replacement, check after finished adding mech
                __state = true;
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyWrapSafe]
    [HarmonyPriority(Priority.VeryHigh)]
    public static void Postfix(SimGameState __instance, bool __state, string id, bool active)
    {
        if (id == null)
        {
            return;
        }

        if (__state)
        {
            ChassisHandler.SanitizeUniqueUnits(__instance);
        }
    }
}