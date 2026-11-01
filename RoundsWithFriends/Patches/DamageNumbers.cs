using HarmonyLib;
using RWF.Cards;
using UnityEngine;

namespace RWF.Patches
{
    [HarmonyPatch(typeof(HealthHandler), "DoDamage")]
    [HarmonyPriority(Priority.First)]
    static class DamageNumbers_Patch_DoDamage
    {
        static readonly Color NumberColor = Color.red;

        static void Prefix(CharacterData ___data, out float __state)
        {
            __state = ___data != null ? ___data.health : float.NaN;
        }

        static void Postfix(HealthHandler __instance, CharacterData ___data, float __state)
        {
            if (!RWFMod.ShowDamageNumbers || ___data == null || float.IsNaN(__state))
            {
                return;
            }

            float dealt = __state - ___data.health;
            if (dealt < 0.05f)
            {
                return;
            }

            LeeLuckMissPopup.Spawn(__instance.transform.position, Format(dealt), NumberColor, true, 0.15f);
        }

        static string Format(float dealt)
        {
            if (dealt < 1f)
            {
                return dealt.ToString("0.0");
            }

            return Mathf.RoundToInt(dealt).ToString();
        }
    }
}
