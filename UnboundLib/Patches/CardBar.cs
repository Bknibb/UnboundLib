using HarmonyLib;
using UnityEngine;

namespace UnboundLib.Patches
{
    [HarmonyPatch(typeof(CardBar), "OnHover", argumentTypes: new System.Type[] { typeof(int) })]
    [HarmonyPatch(typeof(CardBar), "OnHover", argumentTypes: new System.Type[] { typeof(CardBarButton) })]
    class CardBar_Patch
    {
        static void Postfix(CardBar __instance, GameObject ___m_currentCard)
        {
            ___m_currentCard.SetActive(true);
        }
    }
}
