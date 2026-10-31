using HarmonyLib;
using System;

namespace RWF.Patches
{
    [HarmonyPatch(typeof(MainMenuHandler), "Awake")]
    class MainMenuHandler_Patch_Awake
    {
        static void Postfix() {
            try
            {
                RWFMod.instance.InjectUIElements();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(e);
            }

            try
            {
                RWFMod.instance.SetupGameModes();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(e);
            }

            RWFMod.instance.StartCoroutine(RWFMod.instance.BrandMainMenu());
        }
    }
}
