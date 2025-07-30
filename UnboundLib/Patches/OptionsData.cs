using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace UnboundLib.Patches
{
    [HarmonyPatch(typeof(OptionsData), "ApplyScreen")]
    [HarmonyPatch(typeof(OptionsData), "InitializeResolution")]
    class OptionsData_Patch
    {
        static void Postfix()
        {
            Cursor.lockState = Unbound.lockMouse.Value ? CursorLockMode.Confined : CursorLockMode.None;
        }
    }
}
