using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ScalingDailyQuota.Patches;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using System.Reflection;
using UnityEngine;
using System.Collections;

namespace ScalingDailyQuota
{
    [BepInPlugin("Angst-ScalingDailyQuota", "DailyQuota", "1.0.0.0")]
    public class ScalingDailyQuota : BaseUnityPlugin
    {
        public static ConfigEntry<bool>? config_playerScaling;
        public static ConfigEntry<int>? config_daysPerCycle;
        public static ConfigEntry<float>? config_commissionPercent; // untested
        public static ConfigEntry<bool>? config_quotaRollover; // untested

        public static ConfigEntry<int>? playerQuota_startingAmount;
        public static ConfigEntry<int>? playerQuota_dailyIncrease;
        public static ConfigEntry<int>? playerQuota_difficultyIncrease;

        public static ConfigEntry<int>? fixedQuota_startingAmount;
        public static ConfigEntry<int>? fixedQuota_dailyIncrease;
        public static ConfigEntry<int>? fixedQuota_difficultyIncrease;
        
     
        public static ConfigEntry<string>? playerRevive_source; // not implimented
        public static ConfigEntry<int>? playerRevive_creditCost; // not implimented
        public static ConfigEntry<int>? playerRevive_quotaPenaltyAmount; // not implimented
        public static ConfigEntry<int>? playerRevive_respawnTimer; // not implimented

        private readonly Harmony harmony = new Harmony("Angst-ScalingDailyQuota");
        private static ScalingDailyQuota? Instance;
        public static ManualLogSource? mls;


        private void Awake()
        {

            if (Instance = null)
            {
                Instance = this;
            }

            ConfigureConfig();

            mls = BepInEx.Logging.Logger.CreateLogSource("Angst-ScalingDailyQuota");
            mls.LogInfo("Robot is online.");
            mls.LogInfo("Reviewing primary directives...");

            NetcodePatcher(); // ONLY RUN ONCE

            harmony.PatchAll(typeof(GameNetworkManagerPatch));
            //harmony.PatchAll(typeof(HUDManagerPatch));
            //harmony.PatchAll(typeof(RoundManagerPatch));
            harmony.PatchAll(typeof(ScalingDailyQuota));
            harmony.PatchAll(typeof(StartOfRoundPatch));
            harmony.PatchAll(typeof(TimeOfDayPatch));
            harmony.PatchAll(typeof(DepositItemsDeskPatch));
        }


        public void ConfigureConfig()
        {
            config_playerScaling = base.Config.Bind(
            "Angst",
            "Player Scaling?",
            true,
            "Scale certain config settings depending on player count"
            );

            config_daysPerCycle = base.Config.Bind(
            "Angst",
            "Days per Quota Cycle",
            3,
            "Sets how many days are in a quota cycle. aka how often the Difficulty Increase happens."
            );

            config_commissionPercent = base.Config.Bind(
           "Angst",
           "Company Commission Percent",
           0.25f,
           "How much of the fulfilled quota does The Company pay the players and reinvest into future jobs?"
           );

            config_quotaRollover = base.Config.Bind(
            "Angst",
            "Quota-Cycle Quota Rollover?",
            true,
            "Collecting and selling more than required will be credited towards your next quota. This is how I intended this game mode to work. Disabling this will turn any extra quota into credits."
            );

            playerQuota_startingAmount = base.Config.Bind(
            "Player Scaling",
            "Quota Starting Amount, Per-Player",
            100,
            "If player scaling is enabled, the quota on the first day will start at this amount, Per-Player"
            );

            playerQuota_dailyIncrease = base.Config.Bind(
            "Player Scaling",
            "Daily Quota, Per-Player",
            100,
            "If player scaling is enabled, the quota will increase daily by this amount, Per-Player."
            );


            playerQuota_difficultyIncrease = base.Config.Bind(
            "Player Scaling",
            "Difficulty Increase, Per-Player, Per-Completed-Quota-Cycle",
            25,
            "If player scaling is enabled, the Per-Player Daily Quota will increase this much per-completed quota cycle."
            );

            fixedQuota_startingAmount = base.Config.Bind(
            "Fixed Quota",
            "Fixed Value Starting Quota",
            500,
            "If player scaling is disabled, the quota will start at this fixed amount."
            );

            fixedQuota_dailyIncrease = base.Config.Bind(
            "Fixed Quota",
            "Fixed Value Daily Quota Increase",
            500,
            "If player scaling is disabled, the quota will increase daily by this fixed amount."
            );

            fixedQuota_difficultyIncrease = base.Config.Bind(
            "Fixed Quota",
            "Fixed Difficulty Increase Per-Completed-Quota",
            500,
            "If player scaling is disabled, the Daily Quota will increase this much per-completed quota cycle."
            );

            playerRevive_source = base.Config.Bind(
            "Player Revive",
            "Player Revive Source",
            "PlayerAndCompany",
            "Determines if a dead player can be revived by Player, Company, PlayerAndCompany, or NONE"
            );

            playerRevive_creditCost = base.Config.Bind(
            "Player Revive",
            "Player Revive Credit Cost",
            100,
            "Credit cost to revive a dead player."
            );

            playerRevive_quotaPenaltyAmount = base.Config.Bind(
            "Player Revive",
            "Player Revive Quota Penalty",
            100,
            "If player dies, and is revived by The Company, the quota will increase this amount."
            );

            playerRevive_respawnTimer = base.Config.Bind(
            "Player Revive",
            "Player Revive Respawn Timer",
            300,
            "How long before The Company respawns a dead player?"
            );
        }


        // Currenly only runs on server
        // At this point we can assuming quotaFulfilled > profitQuota
        public static void SetNewQuotaAtEndOfCycle(int otb)
        {
            // reduntant, but here for clarity and safety
            if (!NetworkManager.Singleton.IsServer)
            {
                return;
            }

            // find terminal
            Terminal term = UnityEngine.Object.FindFirstObjectByType<Terminal>();

            // Overtime Bonus
            // ???

            // Increment quotas-completed counter
            TimeOfDay.Instance.timesFulfilledQuota++;

            // Reset quota tracker, this should reset or roll over quota depending on config
            mls.LogWarning("test this part of the code");
            mls.LogWarning("credits may be added twice if original code is still running. better check this!");

            if (config_quotaRollover.Value)
            {
                // Add to group credits based on company kickback commission percentage
                //term.groupCredits += (int)((float)TimeOfDay.Instance.quotaFulfilled * config_commissionPercent.Value);
                // Rollover quota
                TimeOfDay.Instance.quotaFulfilled -= TimeOfDay.Instance.profitQuota;
            }
            else
            {
                // Add to group credits based on company kickback commission percentage
                //term.groupCredits += (int)((float)TimeOfDay.Instance.quotaFulfilled * config_commissionPercent.Value);

                // Everything over quota is credited at 100% commission percentage
                //term.groupCredits += (TimeOfDay.Instance.quotaFulfilled - TimeOfDay.Instance.profitQuota);
                TimeOfDay.Instance.quotaFulfilled = 0;
            }

            // Reset time until next quota
            TimeOfDay.Instance.timeUntilDeadline = TimeOfDay.Instance.totalTime * TimeOfDay.Instance.quotaVariables.deadlineDaysAmount;

            // Increase Quota (will also sync with clients)
            SetDailyQuota();

            //rackup quota?
            //???

            // update shop
            Terminal terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
            terminal.RotateShipDecorSelection();
        }


        public static void SetDailyQuota()
        {
            // reduntant, but here for clarity and safety.
            if (!NetworkManager.Singleton.IsServer)
            {
                return;
            }

            // make variable readable...
            var dailyIncrease = ScalingDailyQuota.config_playerScaling.Value ? ScalingDailyQuota.playerQuota_dailyIncrease.Value : ScalingDailyQuota.fixedQuota_dailyIncrease.Value;
            var difficultyIncrease = ScalingDailyQuota.config_playerScaling.Value ? ScalingDailyQuota.playerQuota_difficultyIncrease.Value : ScalingDailyQuota.fixedQuota_difficultyIncrease.Value;
            var numberOfPlayer = RoundManager.Instance.playersManager.connectedPlayersAmount + 1;
            var quotasFulfilled = TimeOfDay.Instance.timesFulfilledQuota;

            // calculate new daily quota
            TimeOfDay.Instance.profitQuota +=
                (dailyIncrease +
                (difficultyIncrease * quotasFulfilled)) * numberOfPlayer;
            mls.LogInfo("New Quota: " + TimeOfDay.Instance.profitQuota);

            // sync with clients
            mls.LogInfo("Sending new quota info to clients.");
            SDQNetworkHandler.Instance.SyncDailyQuotaClientRPC(TimeOfDay.Instance.profitQuota, TimeOfDay.Instance.quotaFulfilled, TimeOfDay.Instance.timesFulfilledQuota, TimeOfDay.Instance.timeUntilDeadline);
        }


        // ---------------------------- NETCODE PATCHER ---------------------------- 
        private static void NetcodePatcher()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
        }
    }


    public class SDQNetworkHandler : NetworkBehaviour
    {
        public static SDQNetworkHandler Instance { get; private set; }
        public static ManualLogSource mls;

        public override void OnNetworkSpawn()
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
                Instance?.gameObject.GetComponent<NetworkObject>().Despawn();
            Instance = this;

            base.OnNetworkSpawn();
            mls = BepInEx.Logging.Logger.CreateLogSource("Angst-ScalingDailyQuota");
        }

        // ---------------------------- QUOTA SYNC RPC ---------------------------- 
        [ClientRpc]
        public void SyncDailyQuotaClientRPC(int pq, int qf, int tfq, float tud)
        {
            mls.LogInfo("New quota info received from Server.");
            TimeOfDay.Instance.profitQuota = pq;
            TimeOfDay.Instance.quotaFulfilled = qf;
            TimeOfDay.Instance.timesFulfilledQuota = tfq;
            TimeOfDay.Instance.timeUntilDeadline = tud;

            // update monitor. This already happens via PassTimeToNextDay->UpdateProfitQuotaCurrentTime,
            // but sometimes we need to update it when a player joins/leaves.
            StartOfRound.Instance.profitQuotaMonitorText.text = $"PROFIT QUOTA:\n${qf} / ${pq}";
        }
    }
}
