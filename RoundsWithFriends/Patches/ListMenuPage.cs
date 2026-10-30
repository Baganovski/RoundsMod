using HarmonyLib;
using UnityEngine;
using RWF.UI;

namespace RWF.Patches
{
    [HarmonyPatch(typeof(ListMenuPage),"Close")]
    class ListMenuPage_Patch_Close
    {
        // patch to clear any keybind hints on screen when exiting a menu
        static void Postfix()
        {
            KeybindHints.ClearHints();
        }
    }

    [HarmonyPatch(typeof(ListMenuPage), "Open")]
    class ListMenuPage_Patch_Open
    {
        static bool hasCompletedInitialIntro;

        static bool Prefix(ListMenuPage __instance)
        {
            if (__instance == null)
            {
                return true;
            }

            if (__instance.gameObject.name == "Intro")
            {
                // SkipIntro lives on this page and opens Main when the intro grid activates.
                // Skipping Open() leaves the title screen blank on return from a match.
                __instance.firstSelected = null;
                return true;
            }

            if (__instance.gameObject.name != "Main")
            {
                return true;
            }

            Transform group = __instance.transform.Find("Group");
            ListMenuButton local = group != null ? group.Find("LOCAL")?.GetComponent<ListMenuButton>() : null;
            if (local != null)
            {
                __instance.firstSelected = local;
            }

            return true;
        }

        static void Postfix(ListMenuPage __instance)
        {
            if (__instance == null || RWFMod.instance == null)
            {
                return;
            }

            if (__instance.gameObject.name == "Main")
            {
                hasCompletedInitialIntro = true;
                CloseIntroGroup(__instance);
                RWFMod.instance.NormalizeMenuPageLayout(__instance);
                Transform local = __instance.transform.Find("Group/LOCAL");
                if (local != null)
                {
                    ListMenuButton button = local.GetComponent<ListMenuButton>();
                    if (button != null)
                    {
                        button.OnPointerEnter(null);
                    }
                }
                return;
            }

            if (__instance.gameObject.name == "CharacterSelect")
            {
                KeybindHints.CreateLocalHints();
                RWFMod.instance?.SyncLobbyRoundCounter();
                return;
            }

            if (__instance.gameObject.name == "Intro")
            {
                if (!hasCompletedInitialIntro)
                {
                    return;
                }

                CloseIntroGroup(__instance);
                if (ListMenu.instance != null && ListMenu.instance.selectedPage == __instance)
                {
                    Transform main = __instance.transform.parent != null ? __instance.transform.parent.Find("Main") : null;
                    ListMenuPage mainPage = main != null ? main.GetComponent<ListMenuPage>() : null;
                    if (mainPage != null)
                    {
                        mainPage.Open();
                    }
                }
                return;
            }

            RWFMod.instance.NormalizeMenuPageLayout(__instance);
        }

        static void CloseIntroGroup(ListMenuPage page)
        {
            Transform introGroup = page.transform.parent != null ? page.transform.parent.Find("Intro/Group") : null;
            if (introGroup != null)
            {
                introGroup.gameObject.SetActive(false);
            }
        }
    }
}
