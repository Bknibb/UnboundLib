﻿using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnboundLib.Extensions;

namespace UnboundLib.Patches
{
    [HarmonyPatch(typeof(Holding), "Start")]
    class Holding_Patch_Start
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
