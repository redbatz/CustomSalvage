using System;
using BattleTech;
using BattleTech.UI;
using UnityEngine;

namespace CustomSalvage;

[HarmonyPatch(typeof(SGContractsWidget), "UpdateCurrentValues")]
internal static class SGContractsWidget_UpdateCurrentValues
{
    public static void Postfix(SGContractsWidget __instance)
    {
        try
        {
            Contract contract = __instance.SelectedContract;
            if (contract == null)
            {
                return;
            }

            SimGameConstants constants = __instance.Sim.Constants;
            int salvage = constants.Salvage.DefaultSalvagePotential;
            if (contract.Override.salvagePotential > -1)
            {
                salvage = contract.Override.salvagePotential;
            }
            else if (contract.SalvagePotential > -1)
            {
                salvage = contract.SalvagePotential;
            }

            if (salvage > 0)
            {
                float fraction = __instance.NegSalvageSlider.Value / __instance.NegSalvageSlider.ValueMax;
                salvage = Mathf.FloorToInt(salvage * fraction) + constants.Finances.ContractFloorSalvageBonus;
            }

            int picks = Mathf.FloorToInt(salvage * constants.Salvage.PrioritySalvageModifier);
            __instance.NegSalvageCurrent.SetText($"{picks} / {salvage}");
        }
        catch (Exception e)
        {
            Log.Main.Error?.Log(e.ToString());
        }
    }
}
