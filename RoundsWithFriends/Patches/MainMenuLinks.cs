using HarmonyLib;
using UnboundLib.Utils.UI;

namespace RWF.Patches
{
    [HarmonyPatch(typeof(MainMenuLinks), nameof(MainMenuLinks.AddLinks))]
    class MainMenuLinks_Patch_AddLinks
    {
        static bool Prefix()
        {
            return false;
        }
    }
}
