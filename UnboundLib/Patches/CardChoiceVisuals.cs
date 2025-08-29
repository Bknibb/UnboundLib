using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnboundLib.Extensions;
using UnboundLib.StatsViewer;
using UnityEngine;

namespace UnboundLib.Patches
{
    [HarmonyPatch(typeof(CardChoiceVisuals), "Awake")]
    class CardChoiceVisuals_Patch_Awake
    {
        static void Postfix(CardChoiceVisuals __instance)
        {
            Unbound.Instance.ExecuteAfterSeconds(0.1f, () =>
            {
                GameObject attachedCardChoiceUI = new GameObject("AttachedCardChoiceUI");
                attachedCardChoiceUI.transform.SetParent(GameObject.Find("Game/UI/UI_Game/Canvas").transform);
                RectTransform rectTransform = attachedCardChoiceUI.AddComponent<RectTransform>();
                rectTransform.anchorMin = Vector3.zero;
                rectTransform.anchorMax = Vector3.one;
                rectTransform.offsetMin = Vector3.zero;
                rectTransform.offsetMax = Vector3.zero;
                rectTransform.localScale = Vector3.one;
                attachedCardChoiceUI.AddComponent<AttachedCardChoiceUI>();
                attachedCardChoiceUI.SetActive(false);
            });
        }
    }
    [HarmonyPatch(typeof(CardChoiceVisuals), "Show")]
    class CardChoiceVisuals_Patch_Show
    {
        static int GetColorIDFromPlayerID(int playerID)
        {
            return PlayerManager.instance.players[playerID].colorID();
        }
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();

            int colorIdx = -1;

            var m_GetPlayerSkinColors = ExtensionMethods.GetMethodInfo(typeof(PlayerSkinBank), nameof(PlayerSkinBank.GetPlayerSkinColors));
            var m_getColorID = ExtensionMethods.GetMethodInfo(typeof(CardChoiceVisuals_Patch_Show), nameof(GetColorIDFromPlayerID));

            // replace GetPlayerSkinColors(playerID) with GetPlayerSkinColors(player.colorID()) always
            for (int i = 1; i < instructions.Count() - 1; i++)
            {
                if (codes[i].opcode != OpCodes.Ldarg_1 || codes[i - 1].opcode != OpCodes.Ldarg_0 || !codes[i + 1].Calls(m_GetPlayerSkinColors)) continue;
                colorIdx = i + 1;
                break;
            }
            if (colorIdx == -1)
            {
                throw new Exception("[CardChoiceVisuals.Show PATCH] COLOR INSTRUCTION NOT FOUND");
            }

            codes.Insert(colorIdx, new CodeInstruction(OpCodes.Call, m_getColorID)); // calls GetColorIDFromPlayerID, taking the pickerID off the stack and leaving the colorID [colorID, ...]

            return codes.AsEnumerable();
        }
        static void Postfix(CardChoiceVisuals __instance, int pickerID)
        {
            AttachedCardChoiceUI.instance.gameObject.SetActive(true);
            StatsViewer.StatsViewer.ChangePlayer(PlayerManager.instance.players[pickerID]);
        }
    }
    [HarmonyPatch(typeof(CardChoiceVisuals), "Hide")]
    class CardChoiceVisuals_Patch_Hide
    {
        static void Postfix()
        {
            AttachedCardChoiceUI.instance.gameObject.SetActive(false);
        }
    }
}
