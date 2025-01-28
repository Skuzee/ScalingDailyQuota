using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace ScalingDailyQuota.Patches
{
    [HarmonyPatch(typeof(HUDManager))]
    internal class HUDManagerPatch
    {
        [HarmonyPatch("DisplayNewDeadline")]
        [HarmonyPrefix]
        static bool DisplayNewDeadline(int overtimeBonus)
        {
            // if it is the start of a new quota series display the new quota, else do not display.
            // not sure if time ticks while this is happening, might need to be something like.
            // if days left is > 3
            //if (TimeOfDay.Instance.timeUntilDeadline == TimeOfDay.Instance.totalTime * 4f)
            //{
                //ManualLogSource mls = BepInEx.Logging.Logger.CreateLogSource("Angst-ScalingDailyQuota");
                //mls.LogInfo("TimeOfDay.Instance.timeUntilDeadline: " + TimeOfDay.Instance.timeUntilDeadline);
                //mls.LogInfo("TimeOfDay.Instance.totalTime * 3f" + (TimeOfDay.Instance.totalTime * 4f));

            HUDManager.Instance.reachedProfitQuotaAnimator.SetBool("display", value: true);
            HUDManager.Instance.newProfitQuotaText.text = "$" + TimeOfDay.Instance.profitQuota;
            HUDManager.Instance.UIAudio.PlayOneShot(HUDManager.Instance.reachedQuotaSFX);
            HUDManager.Instance.displayingNewQuota = true;
            if (overtimeBonus < 0)
            {
                HUDManager.Instance.reachedProfitQuotaBonusText.text = "";
            }
            else
            {
                HUDManager.Instance.reachedProfitQuotaBonusText.text = $"Overtime bonus: ${overtimeBonus}";
            }

            //TimeOfDay.Instance.UpdateProfitQuotaCurrentTime();
            //HUDManager.Instance.displayingNewQuota = false;
            //HUDManager.Instance.reachedProfitQuotaAnimator.SetBool("display", value: false);
            HUDManager.Instance.StartCoroutine(HUDManager.Instance.rackUpNewQuotaText());
            //}
            return false;
        }


    }
}
