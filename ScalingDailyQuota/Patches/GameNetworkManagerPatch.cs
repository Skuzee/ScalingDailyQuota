using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using ScalingDailyQuota.Modules;
using BepInEx.Logging;
using static ScalingDailyQuota.ScalingDailyQuota;

namespace ScalingDailyQuota.Patches
{

    [HarmonyPatch]
    public class GameNetworkManagerPatch
    {
        static GameObject networkPrefab;

        [HarmonyPostfix, HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Start))]
        public static void Init()
        {
            if (networkPrefab != null)
                return;

            networkPrefab = Modules.NetworkPrefabs.CreateNetworkPrefab("Angst-ScalingDailyQuota");
            networkPrefab.AddComponent<ScalingDailyQuota.SDQNetworkHandler>();

            NetworkManager.Singleton.AddNetworkPrefab(networkPrefab);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Awake))]
        static void SpawnNetworkHandler()
        {
            ManualLogSource mls = BepInEx.Logging.Logger.CreateLogSource("Angst-ScalingDailyQuota");

            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                mls.LogInfo("spawning network handler.");
                var networkHandlerHost = Object.Instantiate(networkPrefab, Vector3.zero, Quaternion.identity);
                networkHandlerHost.GetComponent<NetworkObject>().Spawn();
            }
        }
    }
}