using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnboundLib.Patches
{
    [HarmonyPatch(typeof(ArtHandler), "SetMenuArt")]
    class ArtHandler_Patch
    {
        static bool Prefix()
        {
            return false;
        }
    }
}
