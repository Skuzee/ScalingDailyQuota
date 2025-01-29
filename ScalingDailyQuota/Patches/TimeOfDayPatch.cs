using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
//using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using BepInEx.Logging;
using BepInEx;

namespace ScalingDailyQuota.Patches
{
    [HarmonyPatch(typeof(TimeOfDay))]
    internal class TimeOfDayPatch //: DailyQuota // : NetworkBehaviour
    {
        [HarmonyPatch("Awake")]
        [HarmonyPrefix] // runs original
        public static void AwakePrefix(ref TimeOfDay __instance)
        {
            var __quotaVariables = __instance.quotaVariables;
            __quotaVariables.startingQuota = ScalingDailyQuota.playerScaling.Value ? ScalingDailyQuota.playerQuota_startingAmount.Value : ScalingDailyQuota.fixedQuota_startingAmount.Value;
            __quotaVariables.deadlineDaysAmount = ScalingDailyQuota.config_daysPerCycle.Value;
        }

        // game method that sets a new quota at end of quota cycle
        // we are hijacking it to set own quota, but we still use this to
        // display the new quota at the end of a cycle.
        [HarmonyPatch("SetNewProfitQuota")]
        [HarmonyPrefix] // does not run original

        static bool SetNewProfitQuotaPrefix() 
        {
            if (!NetworkManager.Singleton.IsServer)
            {
                return false;
            }

            // save overtime bonus for display
            // we do these calculations now, because daysUntilDeadline is reset when we set a new quota
            int oversellAmount = TimeOfDay.Instance.quotaFulfilled - TimeOfDay.Instance.profitQuota;
            int overtimeBonus = oversellAmount / 5 + 15 * TimeOfDay.Instance.daysUntilDeadline;

            // update quota at end of cycle
            ScalingDailyQuota.SetNewQuotaAtEndOfCycle(overtimeBonus);
   

            return false;
        }


        //[HarmonyPatch("SyncNewProfitQuotaClientRpc")]
        //[HarmonyPrefix]
        //static void SyncNewProfitQuotaClientRpcPRE(out float __state)
        //{
        //    __state = TimeOfDay.Instance.timeUntilDeadline;
        //}

        //[HarmonyPatch("SyncNewProfitQuotaClientRpc")]
        //[HarmonyPostfix]
        //static void SyncNewProfitQuotaClientRpcPOST(float __state)
        //{
        //    TimeOfDay.Instance.timeUntilDeadline = __state;
        //}


        //[HarmonyPatch("OnDayChanged")]
        //[HarmonyPrefix]
        //static bool OnDayChanged()
        //{

        //    if (!StartOfRound.Instance.isChallengeFile)
        //    {
        //        SetDailyQuota();
        //        StartOfRound.Instance.SetPlanetsWeather();
        //        StartOfRound.Instance.SetPlanetsMold();
        //        TimeOfDay.Instance.SetBuyingRateForDay();
        //        TimeOfDay.Instance.SetCompanyMood();
        //    }
        //    return false;
        //}

    }
}
