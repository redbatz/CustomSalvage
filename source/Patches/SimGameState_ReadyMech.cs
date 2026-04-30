using BattleTech;

namespace CustomSalvage.Patches
{
    [HarmonyPatch(typeof(SimGameState), "ReadyMech")]
    public static class SimGameState_ReadyMech_Patch
    {
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        public static void Postfix(int baySlot, string id, SimGameState __instance)
        {
            ChassisHandler.SanitizeUniqueUnits(__instance);
            CheckReadyingMech(__instance, baySlot, id);
        }

        private static void CheckReadyingMech(SimGameState __instance, int baySlot, string id)
        {
            MechDef readyingMech = __instance.ReadyingMechs[baySlot];
            if (readyingMech == null)
            {
                Log.Main.Debug?.Log($"--- Unable to find readying Mech {id}");
                return;
            }

            if (Control.Instance.Settings.RemoveArmorOnAssembly)
            {
                ChassisHandler.RemoveArmorFromMech(readyingMech);
            }
        }
    }
}