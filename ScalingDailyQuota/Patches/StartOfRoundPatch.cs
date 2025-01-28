using BepInEx.Logging;
using HarmonyLib;
using ScalingDailyQuota.Modules;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;

namespace ScalingDailyQuota.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    internal class StartOfRoundPatch
    {
        [HarmonyPatch("PassTimeToNextDay")]
        [HarmonyPrefix]
        static void PassTimeToNextDay()
        {
            ManualLogSource mls = BepInEx.Logging.Logger.CreateLogSource("Angst-ScalingDailyQuota");
            mls.LogInfo("Days Until Deadline: " + TimeOfDay.Instance.daysUntilDeadline);
            // this calculation is offset by +1 because it happens right before daysUntilDeadline is decremented.
            // I thhhink checking planetHasTime fixes quota increase when leaving company.
            if (TimeOfDay.Instance.daysUntilDeadline > 1 && StartOfRound.Instance.currentLevel.planetHasTime)
            {
                ScalingDailyQuota.SetDailyQuota();
                //HUDManager.Instance.UIAudio.PlayOneShot(HUDManager.Instance.newProfitQuotaSFX);
            }
 
        }
    }
}
