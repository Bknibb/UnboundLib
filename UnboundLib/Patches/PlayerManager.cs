using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnboundLib.Extensions;
using UnityEngine;


namespace UnboundLib.Patches
{
    [HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.GetColorFromPlayer))]
    class PlayerManager_Patch_GetColorFromPlayer
    {
        static void Prefix(ref int playerID)
        {
            playerID = PlayerManager.instance.players[playerID].colorID();
        }
    }
    [HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.GetColorFromTeam))]
    class PlayerManager_Patch_GetColorFromTeam
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var f_playerID = ExtensionMethods.GetPropertyInfo(typeof(Player), "PlayerID").GetMethod;
            var m_colorID = UnboundLib.ExtensionMethods.GetMethodInfo(typeof(PlayerExtensions), nameof(PlayerExtensions.colorID));

            foreach (var ins in instructions)
            {
                if (ins.Calls(f_playerID))
                {
                    // we want colorID instead of teamID
                    yield return new CodeInstruction(OpCodes.Call, m_colorID); // call the colorID method, which pops the player instance off the stack and leaves the result [colorID, ...]
                }
                else
                {
                    yield return ins;
                }
            }
        }
    }
    // Added a patch to fix a vanilla bug where pausing while a bot is spawned in breaks the game.
    [HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.SetInputActive))]
    class PlayerManager_Patch_SetInputActive
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var m_getIsLocal = ExtensionMethods.GetMethodInfo(typeof(Player), "get_IsLocal");
            var f_data = ExtensionMethods.GetFieldInfo(typeof(Player), "data");
            var f_playerActions = ExtensionMethods.GetFieldInfo(typeof(CharacterData), "playerActions");
            CodeInstruction lastIns = null;
            foreach (var ins in instructions)
            {
                yield return ins;
                if (ins.opcode == OpCodes.Brfalse && lastIns.Calls(m_getIsLocal))
                {
                    yield return new CodeInstruction(OpCodes.Ldloc_1);
                    yield return new CodeInstruction(OpCodes.Ldfld, f_data);
                    yield return new CodeInstruction(OpCodes.Ldfld, f_playerActions);
                    yield return ins;
                }
                lastIns = ins;
            }
        }
    }
}
