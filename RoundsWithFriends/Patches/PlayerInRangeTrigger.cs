using HarmonyLib;
using System.Reflection;

namespace RWF.Patches
{
    [HarmonyPatch(typeof(PlayerInRangeTrigger), "Update")]
    class PlayerInRangeTrigger_Patch_Update
    {
        static readonly FieldInfo Simulated = AccessTools.Field(typeof(PlayerVelocity), "simulated");

        static bool Prefix(ref bool ___inRange, Player ___ownPlayer)
        {
            if (___ownPlayer == null || ___ownPlayer.data == null || ___ownPlayer.data.playerVel == null || RWFMod.instance == null || PlayerInRangeTrigger_Patch_Update.Simulated == null)
            {
                return true;
            }

            if (!(bool) PlayerInRangeTrigger_Patch_Update.Simulated.GetValue(___ownPlayer.data.playerVel) || RWFMod.instance.IsCeaseFire) {
                ___inRange = false;
                return false;
            }

            return true;
        }
    }
}
