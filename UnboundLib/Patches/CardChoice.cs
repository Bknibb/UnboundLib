using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnboundLib.StatsViewer;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace UnboundLib.Patches
{
    [HarmonyPatch(typeof(CardChoice))]
    public class CardChoice_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(CardChoice.GetSourceCard))]
        private static bool CheckHiddenCards(CardChoice __instance, CardInfo info, ref CardInfo __result)
        {
            for (int i = 0; i < __instance.cards.Length; i++)
            {
                if ((__instance.cards[i].gameObject.name + "(Clone)") == info.gameObject.name)
                {
                    __result = __instance.cards[i];
                    return false;
                }
            }
            __result = null;

            return false;
        }
        [HarmonyTranspiler]
        [HarmonyPatch("DoPlayerSelect")]
        private static IEnumerable<CodeInstruction> CardSelected(IEnumerable<CodeInstruction> instructions)
        {
            var m_SetCurrentSelected = ExtensionMethods.GetMethodInfo(typeof(CardChoiceVisuals), "SetCurrentSelected");
            var f_spawnedCards = ExtensionMethods.GetFieldInfo(typeof(CardChoice), "spawnedCards");
            var f_currentlySelectedCard = ExtensionMethods.GetFieldInfo(typeof(CardChoice), "currentlySelectedCard");
            var m_get_Item = ExtensionMethods.GetMethodInfo(typeof(List<GameObject>), "get_Item", new Type[] { typeof(int) });
            var m_get_Count = ExtensionMethods.GetMethodInfo(typeof(List<GameObject>), "get_Count");
            var m_GetComponent = ExtensionMethods.GetMethodInfo(typeof(GameObject), "GetComponent", new Type[] { }).MakeGenericMethod(typeof(ApplyCardStats));
            var m_Update = ExtensionMethods.GetMethodInfo(typeof(StatsViewer.StatsViewer), "Update");
            var m_Clamp = ExtensionMethods.GetMethodInfo(typeof(Mathf), "Clamp", new Type[] { typeof(int), typeof(int), typeof(int) });
            foreach (CodeInstruction instruction in instructions)
            {
                yield return instruction;
                if (instruction.Calls(m_SetCurrentSelected))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, f_spawnedCards);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, f_currentlySelectedCard);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, f_spawnedCards);
                    yield return new CodeInstruction(OpCodes.Callvirt, m_get_Count);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                    yield return new CodeInstruction(OpCodes.Sub);
                    yield return new CodeInstruction(OpCodes.Call, m_Clamp);
                    yield return new CodeInstruction(OpCodes.Callvirt, m_get_Item);
                    yield return new CodeInstruction(OpCodes.Callvirt, m_GetComponent);
                    yield return new CodeInstruction(OpCodes.Call, m_Update);
                }
            }
        }
    }
}
