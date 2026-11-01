using System.Collections.Generic;
using UnboundLib.Utils.UI;
using UnityEngine;
using UnityEngine.UI;

namespace RWF.UI
{
    public class MenuLayerGuard : MonoBehaviour
    {
        const int OverlaySort = short.MaxValue;

        bool groupsHidden;
        bool healthHidden;
        int lastHealthPlayerCount = -1;
        readonly List<GameObject> hiddenGroups = new List<GameObject>();
        readonly Dictionary<Behaviour, bool> hiddenGraphics = new Dictionary<Behaviour, bool>();
        readonly Dictionary<Renderer, bool> hiddenRenderers = new Dictionary<Renderer, bool>();
        readonly List<CurveAnimation> unscaledCardAnims = new List<CurveAnimation>();

        void LateUpdate()
        {
            bool togglesOpen = RaiseToggleCanvases();
            if (togglesOpen)
            {
                if (!groupsHidden)
                {
                    HideEscapeGroups();
                }
            }
            else if (groupsHidden)
            {
                RestoreEscapeGroups();
            }

            if (togglesOpen || SandboxCardBrowser.IsOpen)
            {
                int playerCount = PlayerManager.instance != null && PlayerManager.instance.players != null
                    ? PlayerManager.instance.players.Count
                    : 0;
                if (!healthHidden || playerCount != lastHealthPlayerCount)
                {
                    if (healthHidden)
                    {
                        RestoreHealthBars();
                    }

                    HideHealthBars();
                    lastHealthPlayerCount = playerCount;
                }
            }
            else if (healthHidden)
            {
                RestoreHealthBars();
            }
        }

        void OnDisable()
        {
            RestoreEscapeGroups();
            RestoreHealthBars();
            RestoreCardAnimationTime();
        }

        bool RaiseToggleCanvases()
        {
            bool open = false;
            GameObject cardMenu = ToggleCardsMenuHandler.cardMenuCanvas;
            if (cardMenu != null && cardMenu.activeInHierarchy)
            {
                UseUnscaledCardAnimations(cardMenu);
                Raise(cardMenu);
                open = true;
            }
            else if (unscaledCardAnims.Count > 0)
            {
                RestoreCardAnimationTime();
            }

            ToggleLevelMenuHandler levels = ToggleLevelMenuHandler.instance;
            if (levels != null && levels.mapMenuCanvas != null && levels.mapMenuCanvas.activeInHierarchy)
            {
                Raise(levels.mapMenuCanvas);
                open = true;
            }

            return open;
        }

        void UseUnscaledCardAnimations(GameObject menu)
        {
            // Opening this menu from the in-game pause screen sets TimeHandler.deltaTime to 0.
            // The card faces play a CurveAnimation on that clock, so they stay on the reversed first frame.
            CurveAnimation[] anims = menu.GetComponentsInChildren<CurveAnimation>(true);
            for (int i = 0; i < anims.Length; i++)
            {
                CurveAnimation anim = anims[i];
                if (anim == null || !anim.useTimeScale)
                {
                    continue;
                }

                anim.useTimeScale = false;
                if (!unscaledCardAnims.Contains(anim))
                {
                    unscaledCardAnims.Add(anim);
                }
            }
        }

        void RestoreCardAnimationTime()
        {
            for (int i = 0; i < unscaledCardAnims.Count; i++)
            {
                if (unscaledCardAnims[i] != null)
                {
                    unscaledCardAnims[i].useTimeScale = true;
                }
            }

            unscaledCardAnims.Clear();
        }

        static void Raise(GameObject menu)
        {
            Canvas canvas = menu.GetComponent<Canvas>();
            if (canvas == null)
            {
                return;
            }

            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            canvas.overrideSorting = true;
            canvas.sortingOrder = OverlaySort;
        }

        void HideEscapeGroups()
        {
            groupsHidden = true;
            if (UIHandler.instance == null)
            {
                return;
            }

            Transform escape = UIHandler.instance.transform.Find("Canvas/EscapeMenu");
            if (escape == null)
            {
                return;
            }

            for (int i = 0; i < escape.childCount; i++)
            {
                Transform group = escape.GetChild(i).Find("Group");
                if (group != null && group.gameObject.activeSelf)
                {
                    hiddenGroups.Add(group.gameObject);
                    group.gameObject.SetActive(false);
                }
            }
        }

        void RestoreEscapeGroups()
        {
            for (int i = 0; i < hiddenGroups.Count; i++)
            {
                if (hiddenGroups[i] != null)
                {
                    hiddenGroups[i].SetActive(true);
                }
            }

            hiddenGroups.Clear();
            groupsHidden = false;
        }

        void HideHealthBars()
        {
            healthHidden = true;
            HealthBar[] bars = Object.FindObjectsOfType<HealthBar>();
            for (int i = 0; i < bars.Length; i++)
            {
                if (bars[i] == null)
                {
                    continue;
                }

                Graphic[] graphics = bars[i].GetComponentsInChildren<Graphic>(true);
                for (int j = 0; j < graphics.Length; j++)
                {
                    RememberAndDisable(graphics[j]);
                }

                Renderer[] renderers = bars[i].GetComponentsInChildren<Renderer>(true);
                for (int j = 0; j < renderers.Length; j++)
                {
                    RememberAndDisable(renderers[j]);
                }
            }
        }

        void RememberAndDisable(Behaviour graphic)
        {
            if (graphic == null || hiddenGraphics.ContainsKey(graphic))
            {
                return;
            }

            hiddenGraphics[graphic] = graphic.enabled;
            graphic.enabled = false;
        }

        void RememberAndDisable(Renderer renderer)
        {
            if (renderer == null || hiddenRenderers.ContainsKey(renderer))
            {
                return;
            }

            hiddenRenderers[renderer] = renderer.enabled;
            renderer.enabled = false;
        }

        void RestoreHealthBars()
        {
            foreach (KeyValuePair<Behaviour, bool> entry in hiddenGraphics)
            {
                if (entry.Key != null)
                {
                    entry.Key.enabled = entry.Value;
                }
            }

            foreach (KeyValuePair<Renderer, bool> entry in hiddenRenderers)
            {
                if (entry.Key != null)
                {
                    entry.Key.enabled = entry.Value;
                }
            }

            hiddenGraphics.Clear();
            hiddenRenderers.Clear();
            healthHidden = false;
        }
    }
}
