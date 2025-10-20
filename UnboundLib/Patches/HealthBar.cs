using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnboundLib.Utils;
using UnityEngine;

namespace UnboundLib.Patches
{
    [HarmonyPatch(typeof(HealthBar), "Start")]
    class HealthBar_Patch_Start
    {
        static void Postfix(HealthBar __instance)
        {
            Transform playerName = __instance.transform.Find("Canvas/PlayerName");
            GameObject health = GameObject.Instantiate(playerName.gameObject, playerName.parent);
            health.name = "Health";
            playerName.AddYPosition(120);
            UnityEngine.Object.Destroy(health.GetComponent<PlayerName>());
            TextMeshProUGUI textUI = health.GetComponent<TextMeshProUGUI>();
            textUI.text = "100%";
            textUI.fontSize = 120;
            health.AddComponent<HealthPercent>();
            HealthBar_Patch_Update.ogColor = __instance.hp.color;
        }
    }
    [HarmonyPatch(typeof(HealthBar), "Update")]
    class HealthBar_Patch_Update
    {
        public static Color ogColor;
        public static Color respawnsColor = new Color(1f, 0.4f, 1f);
        static void Postfix(HealthBar __instance, CharacterData ___data)
        {
            if (___data.stats.remainingRespawns > 0) {
                __instance.hp.color = respawnsColor;
            } else
            {
                if (__instance.hp.color == respawnsColor) __instance.hp.color = ogColor;
            }
        }
    }
}
