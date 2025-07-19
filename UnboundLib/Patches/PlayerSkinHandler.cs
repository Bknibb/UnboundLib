using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnboundLib.Extensions;
using UnityEngine;

namespace UnboundLib.Patches
{
    [HarmonyPatch(typeof(PlayerSkinHandler), "Init")]
    class PlayerSkinHandler_Patch_Init
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var f_playerID = ExtensionMethods.GetPropertyInfo(typeof(Player), "PlayerID").GetMethod;
            var m_colorID = ExtensionMethods.GetMethodInfo(typeof(PlayerExtensions), nameof(PlayerExtensions.colorID));

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
}
