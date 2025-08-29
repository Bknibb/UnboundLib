using HarmonyLib;
using UnityEngine;

namespace UnboundLib.Patches
{
    [HarmonyPatch(typeof(CardBar), "OnHover", argumentTypes: new System.Type[] { typeof(int) })]
    [HarmonyPatch(typeof(CardBar), "OnHover", argumentTypes: new System.Type[] { typeof(CardBarButton) })]
    class CardBar_Patch_OnHover
    {
        static void Postfix(CardBar __instance, GameObject ___m_currentCard)
        {
            ___m_currentCard.SetActive(true);
        }
    }
    [HarmonyPatch(typeof(CardBar), "Start")]
    class CardBar_Patch_Start
    {
        static void Postfix(CardBar __instance)
        {
            __instance.GetComponent<RectTransform>().sizeDelta = new Vector2(1920, __instance.GetComponent<RectTransform>().sizeDelta.y);
        }
    }
}
