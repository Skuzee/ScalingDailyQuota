using BepInEx.Logging;
using HarmonyLib;
using ScalingDailyQuota.Modules;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using System.Linq;
using static ScalingDailyQuota.ScalingDailyQuota;

namespace ScalingDailyQuota.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    internal class StartOfRoundPatch
    {
        [HarmonyPatch("PassTimeToNextDay")]
        [HarmonyPrefix] // runs original
        static void PassTimeToNextDayPrefix()
        {
            // this calculation is offset by +1 because it happens right before daysUntilDeadline is decremented.
            if (NetworkManager.Singleton.IsServer && TimeOfDay.Instance.daysUntilDeadline > 1 && StartOfRound.Instance.currentLevel.planetHasTime)
            {
                ScalingDailyQuota.SetDailyQuota();
                //HUDManager.Instance.UIAudio.PlayOneShot(HUDManager.Instance.newProfitQuotaSFX);
                }
        }

        [HarmonyPatch("OnClientConnect")]
        [HarmonyPostfix]
        static void OnClientConnectPostfix(ref StartOfRound __instance, ref ulong clientId)
        {
            IEnumerator delayedSyncQuota = DelayedSyncQuota();

            // when a new player joins, increase the current days quota.
            var dailyIncrease = ScalingDailyQuota.playerQuota_dailyIncrease.Value;
            var difficultyIncrease = ScalingDailyQuota.playerQuota_difficultyIncrease.Value;
            var quotasFulfilled = TimeOfDay.Instance.timesFulfilledQuota;

            TimeOfDay.Instance.profitQuota += ScalingDailyQuota.playerQuota_dailyIncrease.Value + (difficultyIncrease * quotasFulfilled);


            StartOfRound.Instance.allPlayerScripts[__instance.ClientPlayerList[clientId]].StartCoroutine(delayedSyncQuota);
        }
        
        static IEnumerator DelayedSyncQuota()
        {
            yield return new WaitForSeconds(5f);
            SDQNetworkHandler.Instance.SyncDailyQuotaClientRPC(TimeOfDay.Instance.profitQuota, TimeOfDay.Instance.quotaFulfilled, TimeOfDay.Instance.timesFulfilledQuota, TimeOfDay.Instance.timeUntilDeadline);
        }

        [HarmonyPatch("OnClientDisconnect")]
        [HarmonyPostfix]
        static void OnClientDisconnectPostfix()
        {
            // when a player leaves, decrease the current days quota.
            // this could technically be abused if all the players leave before a quota is due?
            // without this, OnClientConnect would increase the quota twice if someone disconnected and then reconnected again.
            var dailyIncrease = ScalingDailyQuota.playerQuota_dailyIncrease.Value;
            var difficultyIncrease = ScalingDailyQuota.playerQuota_difficultyIncrease.Value;
            var quotasFulfilled = TimeOfDay.Instance.timesFulfilledQuota;

            TimeOfDay.Instance.profitQuota -= ScalingDailyQuota.playerQuota_dailyIncrease.Value + (difficultyIncrease * quotasFulfilled);

            SDQNetworkHandler.Instance.SyncDailyQuotaClientRPC(TimeOfDay.Instance.profitQuota, TimeOfDay.Instance.quotaFulfilled, TimeOfDay.Instance.timesFulfilledQuota, TimeOfDay.Instance.timeUntilDeadline);

        }
    }
}
