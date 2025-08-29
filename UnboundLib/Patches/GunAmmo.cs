using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnboundLib.Patches
{
    [HarmonyPatch(typeof(GunAmmo), "ReDrawTotalBullets")]
    class GunAmmo_Patch_ReDrawTotalBullets
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
