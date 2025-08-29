using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnboundLib.Patches
{
    [HarmonyPatch(typeof(CharacterStatModifiers), "WasUpdated")]
    class CharacterStatModifiers_Patch_WasUpdated
    {
        public static bool cancel = false;
        static bool Prefix()
        {
            bool doCancel = cancel;
            cancel = false;
            return !doCancel;
        }
    }
    [HarmonyPatch(typeof(CharacterStatModifiers), "ConfigureMassAndSize")]
    class CharacterStatModifiers_Patch_ConfigureMassAndSize
    {
        public static bool cancel = false;
        static bool Prefix()
        {
            bool doCancel = cancel;
            cancel = false;
            return !doCancel;
        }
    }
}
