using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace RWF.Patches
{
    [HarmonyPatch(typeof(ListMenu), "Update")]
    public class ListMenu_Patch_Update
    {
        static void Postfix(ListMenu __instance)
        {
            if (EscapeMenuHandler.isEscMenu)
            {
                RWFMod.instance?.MatchPauseModsHighlight();
            }

            PauseModsBar.Sync(__instance);
        }
    }

    [HarmonyPatch(typeof(ListMenu), "SelectButton")]
    public class ListMenu_Patch_SelectButton
    {
        private static bool Prefix(ListMenu __instance, ListMenuButton buttonToSelect)
        {
            if (__instance == null || __instance.bar == null)
            {
                return true;
            }

            bool intro = __instance.selectedPage != null && __instance.selectedPage.gameObject.name == "Intro";
            if (buttonToSelect == null || !buttonToSelect.gameObject.activeInHierarchy || intro)
            {
                __instance.bar.transform.position = Vector3.up * 10000f;
                PauseModsBar.RestoreStockBar(__instance.bar);
                return false;
            }

            if (PauseModsBar.IsUnderEscapeMenu(buttonToSelect.transform))
            {
                buttonToSelect.hideBar = false;
                // Non-zero setBarHeight makes ISelectButton rebuild the bar with Z = 1.
                buttonToSelect.setBarHeight = 0f;
            }

            // A 2-argument Vector3 zeroes Z. Preserve depth for the pause yellow Image.
            Vector3 barScale = __instance.bar.transform.localScale;
            float barZ = barScale.z;
            if (PauseModsBar.IsUnderEscapeMenu(buttonToSelect.transform))
            {
                if (barZ < 10f)
                {
                    barZ = 27f;
                }
            }
            else if (Mathf.Approximately(barZ, 0f))
            {
                barZ = 1f;
            }

            float barX = buttonToSelect.gameObject.name.Contains("(short)")
                ? buttonToSelect.GetComponent<RectTransform>().sizeDelta.x
                : 4000f;
            float barY = PauseModsBar.IsUnderEscapeMenu(buttonToSelect.transform) ? 92f : barScale.y;
            __instance.bar.transform.localScale = new Vector3(barX, barY, barZ);

            return true;
        }
    }

    static class PauseModsBar
    {
        // OPTIONS is a flat pure yellow. The bar sprite itself is white.
        static readonly Color Yellow = new Color(1f, 1f, 0f, 1f);

        static bool spriteTinted;

        internal static void Sync(ListMenu menu)
        {
            if (menu == null || menu.bar == null || !EscapeMenuHandler.isEscMenu)
            {
                if (menu != null)
                {
                    RestoreStockBar(menu.bar);
                }

                return;
            }

            ListMenuButton selected = menu.selectedButton;
            if (selected == null || !IsPauseMods(selected.transform))
            {
                RestoreStockBar(menu.bar);
                return;
            }

            Show(menu.bar, selected.transform);
        }

        static void Show(GameObject barObject, Transform mods)
        {
            Transform bar = barObject.transform;
            Transform resume = mods.parent != null ? mods.parent.Find("Resume") : null;
            Vector3 modsPos = mods.position;
            float x = resume != null ? resume.position.x : modsPos.x;
            float z = resume != null ? resume.position.z : bar.position.z;
            bar.position = new Vector3(x, modsPos.y, z);
            bar.localScale = new Vector3(4000f, 92f, 27f);
            barObject.SetActive(true);

            // The masked Image is what OPTIONS shows as yellow. On MODS that mask
            // does not reveal it, so draw the same thin sprite and tint it yellow.
            // Leave the Image off or the two stack into the blown-out white band.
            Image image = barObject.GetComponentInChildren<Image>(true);
            if (image != null)
            {
                image.enabled = false;
            }

            SpriteRenderer sprite = barObject.GetComponent<SpriteRenderer>();
            if (sprite != null)
            {
                sprite.enabled = true;
                sprite.color = Yellow;
                spriteTinted = true;
            }
        }

        internal static void RestoreStockBar(GameObject barObject)
        {
            if (!spriteTinted || barObject == null)
            {
                return;
            }

            SpriteRenderer sprite = barObject.GetComponent<SpriteRenderer>();
            if (sprite != null)
            {
                sprite.color = Color.white;
                sprite.enabled = false;
            }

            Image image = barObject.GetComponentInChildren<Image>(true);
            if (image != null)
            {
                image.enabled = true;
            }

            spriteTinted = false;
        }

        internal static bool IsPauseMods(Transform button)
        {
            return button != null && button.name == "MODS" && IsUnderEscapeMenu(button);
        }

        internal static bool IsUnderEscapeMenu(Transform button)
        {
            Transform cursor = button;
            while (cursor != null)
            {
                if (cursor.name == "EscapeMenu")
                {
                    return true;
                }

                cursor = cursor.parent;
            }

            return false;
        }
    }
}
