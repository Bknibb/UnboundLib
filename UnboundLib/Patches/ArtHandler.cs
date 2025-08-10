using HarmonyLib;
using Photon.Pun;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnboundLib.Networking;
using UnityEngine;

namespace UnboundLib.Patches
{
    [HarmonyPatch(typeof(ArtHandler), "SetMenuArt")]
    class ArtHandler_SetMenuArt_Patch
    {
        static bool Prefix()
        {
            return false;
        }
    }
    [HarmonyPatch(typeof(ArtHandler), "NextArt")]
    class ArtHandler_NextArt_Patch
    {
        public static bool TEMP_ALLOW = false;
        static bool Prefix()
        {
            if (!Unbound.syncArtWithHost.Value) return true;
            if (TEMP_ALLOW)
            {
                TEMP_ALLOW = false;
                return true;
            }
            return PhotonNetwork.IsMasterClient || PhotonNetwork.OfflineMode || !PhotonNetwork.InRoom;
        }
    }
    [HarmonyPatch(typeof(ArtHandler), "ApplyArt")]
    class ArtHandler_ApplyArt_Patch
    {
        static void Postfix(ArtHandler __instance, ArtInstance art)
        {
            if (PhotonNetwork.IsMasterClient && !PhotonNetwork.OfflineMode)
            {
                string artName = generateName(art);
                if (artName == null) return;
                NetworkingManager.RPC_Others(typeof(ArtHandler_ApplyArt_Patch), nameof(ArtHandler_ApplyArt_Patch.RPC_ApplyArt), artName);
            }
        }
        static string generateName(ArtInstance art)
        {
            GameObject foreground = (GameObject) ArtHandler.instance.GetFieldValue("m_foreground");
            int forgroundID = foreground.GetInstanceID();
            ParticleSystem particleSystem = art.parts.FirstOrDefault(particleSystemI => particleSystemI.transform.parent.gameObject.GetInstanceID() == forgroundID);
            if (particleSystem == null)
            {
                if (art.profile == null)
                {
                    return null;
                }
                return art.profile.name;
            }
            else
            {
                return particleSystem.name;
            }
        }
        [UnboundRPC]
        static void RPC_ApplyArt(string artName)
        {
            if (!Unbound.syncArtWithHost.Value) return;
            ArtInstance art = ArtHandler.instance.arts.FirstOrDefault(artI => generateName(artI) == artName);
            if (art == null)
            {
                Debug.LogWarning($"RPC_ApplyArt: No art found with name {artName}");
                ArtHandler_NextArt_Patch.TEMP_ALLOW = true;
                ArtHandler.instance.NextArt();
                return;
            }
            ArtHandler.instance.InvokeMethod("TurnArtsOff");
            ArtHandler.instance.SetSpecificArt(art);
        }
    }
}
