using HarmonyLib;
using UnboundLib.Utils.UI;
using UnityEngine;
using UnityEngine.UI;

namespace RWF.Patches
{
    // Unbound opens the card and level menus by assigning the main-menu camera.
    // That camera is inactive during a match, so the click throws and nothing opens.
    static class PauseMenuCamera
    {
        static Camera standIn;
        internal static bool usedStandIn;

        internal static void Ensure()
        {
            usedStandIn = GameObject.Find("MainCamera") == null;
            if (!usedStandIn)
            {
                return;
            }

            if (standIn == null)
            {
                GameObject go = new GameObject("MainCamera");
                Object.DontDestroyOnLoad(go);
                standIn = go.AddComponent<Camera>();
                standIn.clearFlags = CameraClearFlags.Depth;
                standIn.cullingMask = 0;
                standIn.depth = -100f;
            }

            standIn.gameObject.SetActive(true);
            standIn.enabled = true;
        }

        internal static void Release(Canvas canvas)
        {
            if (!usedStandIn)
            {
                return;
            }

            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.worldCamera = null;
            }

            if (standIn != null)
            {
                standIn.enabled = false;
                standIn.gameObject.SetActive(false);
            }

            usedStandIn = false;
        }
    }

    [HarmonyPatch(typeof(ToggleCardsMenuHandler), "SetActive")]
    class ToggleCardsMenuHandler_Patch_SetActive
    {
        static void Prefix()
        {
            PauseMenuCamera.Ensure();
        }

        static void Postfix()
        {
            GameObject menu = ToggleCardsMenuHandler.cardMenuCanvas;
            if (menu != null && menu.activeInHierarchy && GameManager.instance != null && GameManager.instance.isPlaying)
            {
                Button[] buttons = menu.GetComponentsInChildren<Button>(true);
                for (int i = 0; i < buttons.Length; i++)
                {
                    buttons[i].interactable = true;
                }

                Toggle[] toggles = menu.GetComponentsInChildren<Toggle>(true);
                for (int i = 0; i < toggles.Length; i++)
                {
                    toggles[i].interactable = true;
                }
            }

            PauseMenuCamera.Release(menu != null ? menu.GetComponent<Canvas>() : null);
        }
    }

    [HarmonyPatch(typeof(ToggleLevelMenuHandler), "SetActive")]
    class ToggleLevelMenuHandler_Patch_SetActive
    {
        static void Prefix()
        {
            PauseMenuCamera.Ensure();
        }

        static void Postfix(ToggleLevelMenuHandler __instance)
        {
            GameObject menu = __instance != null ? __instance.mapMenuCanvas : null;
            PauseMenuCamera.Release(menu != null ? menu.GetComponent<Canvas>() : null);
        }
    }
}
