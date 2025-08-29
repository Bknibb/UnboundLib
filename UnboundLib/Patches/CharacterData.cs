using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnboundLib.Patches
{
    [HarmonyPatch(typeof(CharacterData), "set_MaxHealth")]
    class CharacterData_Patch_set_MaxHealth
    {
        public static bool justSet = false;
        static bool Prefix(float value, ref float ___m_maxHealth)
        {
            bool doJustSet = justSet;
            if (doJustSet)
            {
                ___m_maxHealth = value;
            }
            justSet = false;
            return !doJustSet;
        }
    }
}
