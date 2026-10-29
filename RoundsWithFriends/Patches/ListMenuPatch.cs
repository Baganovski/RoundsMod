using HarmonyLib;
using UnityEngine;

namespace RWF.Patches
{
    [HarmonyPatch(typeof(ListMenu), "SelectButton")]
    public class ListMenu_Patch_SelectButton
    {
        private static bool Prefix(ListMenuButton buttonToSelect)
        {
            if (ListMenu.instance == null || ListMenu.instance.bar == null)
            {
                return true;
            }

            bool intro = ListMenu.instance.selectedPage != null && ListMenu.instance.selectedPage.gameObject.name == "Intro";
            if (buttonToSelect == null || !buttonToSelect.gameObject.activeInHierarchy || intro)
            {
                ListMenu.instance.bar.transform.position = Vector3.up * 10000f;
                return false;
            }

            if (buttonToSelect.gameObject.name.Contains("(short)"))
            {
                ListMenu.instance.bar.transform.localScale = new Vector3(buttonToSelect.GetComponent<RectTransform>().sizeDelta.x, ListMenu.instance.bar.transform.localScale.y);
            }
            else
            {
                ListMenu.instance.bar.transform.localScale = new Vector3(4000, ListMenu.instance.bar.transform.localScale.y);
            }

            return true;
        }
    }
}
