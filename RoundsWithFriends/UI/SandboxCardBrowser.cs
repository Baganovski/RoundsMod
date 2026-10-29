using System.Collections.Generic;
using TMPro;
using UnboundLib;
using UnboundLib.GameModes;
using UnboundLib.Networking;
using UnityEngine;
using UnityEngine.UI;

namespace RWF.UI
{
    public class SandboxCardBrowser : MonoBehaviour
    {
        const string LockKey = "RWF_SandboxCardBrowser";
        const float RowHeight = 44f;

        bool open;
        string filter = "";
        GameObject root;
        GameObject hint;
        RectTransform contentRect;
        TextMeshProUGUI searchLabel;
        TextMeshProUGUI statusLabel;
        readonly List<GameObject> rows = new List<GameObject>();
        readonly HashSet<string> selectedCards = new HashSet<string>();

        public static bool IsOpen { get; private set; }

        void Update()
        {
            bool sandbox = IsSandbox();
            if (!sandbox)
            {
                if (open)
                {
                    Close();
                }
                if (hint != null)
                {
                    hint.SetActive(false);
                }
                return;
            }

            EnsureHint();
            hint.SetActive(!open && !DevConsole.isTyping);

            if (DevConsole.isTyping)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                if (open)
                {
                    Close();
                }
                else
                {
                    Open();
                }
                return;
            }

            if (!open)
            {
                return;
            }

            Unbound.lockInputBools[LockKey] = true;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
                return;
            }

            if (ApplyTyping())
            {
                Rebuild();
            }
        }

        void OnDisable()
        {
            if (open)
            {
                Close();
            }
        }

        static bool IsSandbox()
        {
            return GameModeManager.CurrentHandlerID == GameModeManager.SandBoxID
                && GM_Test.instance != null
                && GM_Test.instance.gameObject.activeInHierarchy;
        }

        void Open()
        {
            EnsureUi();
            filter = "";
            open = true;
            IsOpen = true;
            root.SetActive(true);
            root.transform.SetAsLastSibling();
            Unbound.lockInputBools[LockKey] = true;
            Rebuild();
        }

        void Close()
        {
            open = false;
            IsOpen = false;
            filter = "";
            if (root != null)
            {
                root.SetActive(false);
            }
            Unbound.lockInputBools[LockKey] = false;
        }

        bool ApplyTyping()
        {
            string typed = Input.inputString;
            if (string.IsNullOrEmpty(typed))
            {
                return false;
            }

            bool changed = false;
            for (int i = 0; i < typed.Length; i++)
            {
                char c = typed[i];
                if (c == '\b')
                {
                    if (filter.Length > 0)
                    {
                        filter = filter.Substring(0, filter.Length - 1);
                        changed = true;
                    }
                    continue;
                }

                if (char.IsControl(c))
                {
                    continue;
                }

                filter += c;
                changed = true;
            }

            return changed;
        }

        void Rebuild()
        {
            ClearRows();
            if (searchLabel != null)
            {
                searchLabel.text = filter.Length == 0 ? "TYPE TO SEARCH" : filter.ToUpper();
            }

            CardInfo[] cards = CardChoice.instance != null ? CardChoice.instance.cards : null;
            if (cards == null || cards.Length == 0)
            {
                statusLabel.text = "NO CARDS AVAILABLE";
                return;
            }

            string needle = filter.ToUpper().Replace(" ", "");
            int shown = 0;
            List<CardInfo> matches = new List<CardInfo>();
            for (int i = 0; i < cards.Length; i++)
            {
                CardInfo card = cards[i];
                if (card == null || string.IsNullOrEmpty(card.cardName))
                {
                    continue;
                }

                if (needle.Length > 0)
                {
                    string name = card.cardName.ToUpper().Replace(" ", "");
                    if (name.IndexOf(needle) < 0)
                    {
                        continue;
                    }
                }

                matches.Add(card);
            }

            matches.Sort((a, b) => string.CompareOrdinal(a.cardName, b.cardName));

            for (int i = 0; i < matches.Count; i++)
            {
                rows.Add(CreateRow(matches[i]));
                shown++;
            }

            statusLabel.text = shown + " CARDS";
            searchLabel.ForceMeshUpdate(true);
            statusLabel.ForceMeshUpdate(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        }

        void ClearRows()
        {
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i] != null)
                {
                    Destroy(rows[i]);
                }
            }
            rows.Clear();
        }

        GameObject CreateRow(CardInfo card)
        {
            bool selected = selectedCards.Contains(card.gameObject.name);

            GameObject row = new GameObject("Card");
            row.transform.SetParent(contentRect, false);
            row.transform.localScale = Vector3.one;

            LayoutElement layout = row.AddComponent<LayoutElement>();
            layout.minHeight = RowHeight;
            layout.preferredHeight = RowHeight;
            layout.flexibleWidth = 1f;

            Image image = row.AddComponent<Image>();
            image.color = selected
                ? new Color(0.12f, 0.32f, 0.62f, 0.98f)
                : new Color(0.08f, 0.18f, 0.38f, 0.95f);

            Button button = row.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.25f, 0.55f, 1f, 1f);
            colors.pressedColor = new Color(0.15f, 0.4f, 0.85f, 1f);
            colors.normalColor = image.color;
            button.colors = colors;

            CardInfo picked = card;
            button.onClick.AddListener(() => ToggleCard(picked));

            GameObject box = new GameObject("Tick");
            box.transform.SetParent(row.transform, false);
            box.transform.localScale = Vector3.one;
            LayoutElement boxLayout = box.AddComponent<LayoutElement>();
            boxLayout.minWidth = 28f;
            boxLayout.preferredWidth = 28f;
            boxLayout.minHeight = 28f;
            boxLayout.preferredHeight = 28f;
            Image boxImage = box.AddComponent<Image>();
            boxImage.color = selected ? new Color(0.45f, 0.85f, 1f, 1f) : new Color(0.02f, 0.05f, 0.12f, 1f);
            boxImage.raycastTarget = false;

            GameObject markGo = new GameObject("Mark");
            markGo.transform.SetParent(box.transform, false);
            markGo.transform.localScale = Vector3.one;
            RectTransform markRect = markGo.AddComponent<RectTransform>();
            markRect.anchorMin = Vector2.zero;
            markRect.anchorMax = Vector2.one;
            markRect.offsetMin = Vector2.zero;
            markRect.offsetMax = Vector2.zero;
            TextMeshProUGUI mark = markGo.AddComponent<TextMeshProUGUI>();
            mark.font = RoundsResources.MenuFont;
            mark.fontSize = 24f;
            mark.alignment = TextAlignmentOptions.Center;
            mark.color = new Color(0.02f, 0.08f, 0.16f, 1f);
            mark.text = selected ? "X" : "";
            mark.raycastTarget = false;

            GameObject labelGo = new GameObject("Label");
            labelGo.transform.SetParent(row.transform, false);
            labelGo.transform.localScale = Vector3.one;
            LayoutElement labelLayout = labelGo.AddComponent<LayoutElement>();
            labelLayout.flexibleWidth = 1f;
            labelLayout.minWidth = 200f;
            labelLayout.preferredHeight = RowHeight;

            TextMeshProUGUI label = labelGo.AddComponent<TextMeshProUGUI>();
            label.font = RoundsResources.MenuFont;
            label.fontSize = 28f;
            label.enableAutoSizing = false;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.color = Color.white;
            label.margin = new Vector4(4f, 0f, 8f, 0f);
            label.text = card.cardName.ToUpper();
            label.raycastTarget = false;

            return row;
        }

        void ToggleCard(CardInfo card)
        {
            if (card == null)
            {
                return;
            }

            string id = card.gameObject.name;
            if (selectedCards.Contains(id))
            {
                selectedCards.Remove(id);
            }
            else
            {
                selectedCards.Add(id);
            }

            ApplySelection();
            Rebuild();
        }

        void ApplySelection()
        {
            if (PlayerManager.instance == null)
            {
                return;
            }

            string[] ids = new string[selectedCards.Count];
            selectedCards.CopyTo(ids);

            List<Player> players = PlayerManager.instance.players;
            HashSet<int> sentActors = new HashSet<int>();
            bool applied = false;
            for (int i = 0; i < players.Count; i++)
            {
                Player player = players[i];
                if (player == null || player.data == null || player.data.view == null || !player.data.view.IsMine)
                {
                    continue;
                }

                applied = true;
                int actor = player.data.view.OwnerActorNr;
                if (sentActors.Add(actor))
                {
                    NetworkingManager.RPC(typeof(SandboxCardBrowser), nameof(RPC_ApplyCards), actor, ids);
                }
            }

            if (!applied && statusLabel != null)
            {
                statusLabel.text = "JOIN TO APPLY CARDS";
            }
        }

        [UnboundRPC]
        public static void RPC_ApplyCards(int actorNumber, string[] cardIds)
        {
            if (PlayerManager.instance == null)
            {
                return;
            }

            List<Player> owned = new List<Player>();
            List<Player> players = PlayerManager.instance.players;
            for (int i = 0; i < players.Count; i++)
            {
                Player player = players[i];
                if (player != null && player.data != null && player.data.view != null && player.data.view.OwnerActorNr == actorNumber)
                {
                    owned.Add(player);
                }
            }

            if (owned.Count == 0)
            {
                return;
            }

            for (int i = 0; i < owned.Count; i++)
            {
                Player player = owned[i];
                player.InvokeMethod("FullReset");
                if (player.data.currentCards != null)
                {
                    player.data.currentCards.Clear();
                }
                ClearCardBar(player);
            }

            if (cardIds == null)
            {
                return;
            }

            for (int i = 0; i < cardIds.Length; i++)
            {
                CardInfo card = FindCard(cardIds[i]);
                if (card == null)
                {
                    continue;
                }

                GameObject temp = Object.Instantiate(card.gameObject, new Vector3(0f, -1000f, 0f), Quaternion.identity);
                ApplyCardStats stats = temp.GetComponent<ApplyCardStats>();
                if (stats != null)
                {
                    stats.OFFLINE_Pick(owned.ToArray());
                }
                Object.Destroy(temp);
            }
        }

        static void ClearCardBar(Player player)
        {
            if (CardBarHandler.instance == null)
            {
                return;
            }

            CardBar[] bars = CardBarHandler.instance.GetFieldValue("cardBars") as CardBar[];
            if (bars == null || player.playerID < 0 || player.playerID >= bars.Length || bars[player.playerID] == null)
            {
                return;
            }

            bars[player.playerID].ClearBar();
        }

        static CardInfo FindCard(string objectName)
        {
            if (CardChoice.instance == null || CardChoice.instance.cards == null || string.IsNullOrEmpty(objectName))
            {
                return null;
            }

            CardInfo[] cards = CardChoice.instance.cards;
            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i] != null && cards[i].gameObject.name == objectName)
                {
                    return cards[i];
                }
            }

            return null;
        }

        void EnsureHint()
        {
            if (hint != null)
            {
                return;
            }

            Canvas canvas = Unbound.Instance != null ? Unbound.Instance.canvas : null;
            if (canvas == null)
            {
                return;
            }

            hint = new GameObject("SandboxCardHint");
            hint.transform.SetParent(canvas.transform, false);
            hint.transform.localScale = Vector3.one;

            RectTransform rect = hint.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(24f, 24f);
            rect.sizeDelta = new Vector2(280f, 48f);

            Image image = hint.AddComponent<Image>();
            image.color = new Color(0.04f, 0.1f, 0.22f, 0.85f);

            Button button = hint.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() =>
            {
                if (!open)
                {
                    Open();
                }
            });

            GameObject labelGo = new GameObject("Label");
            labelGo.transform.SetParent(hint.transform, false);
            labelGo.transform.localScale = Vector3.one;
            RectTransform labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI label = labelGo.AddComponent<TextMeshProUGUI>();
            label.font = RoundsResources.MenuFont;
            label.fontSize = 24f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.text = "TAB  CARDS";
            label.raycastTarget = false;
        }

        void EnsureUi()
        {
            if (root != null)
            {
                return;
            }

            Canvas canvas = Unbound.Instance.canvas;

            root = new GameObject("SandboxCardBrowser");
            root.transform.SetParent(canvas.transform, false);
            root.transform.localScale = Vector3.one;

            RectTransform rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Image dim = root.AddComponent<Image>();
            dim.color = new Color(0f, 0.02f, 0.08f, 0.72f);

            GameObject panel = new GameObject("Panel");
            panel.transform.SetParent(root.transform, false);
            panel.transform.localScale = Vector3.one;
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(760f, 860f);
            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.03f, 0.08f, 0.18f, 0.96f);

            searchLabel = CreateText(panel.transform, "Search", "TYPE TO SEARCH", 32f, TextAlignmentOptions.MidlineLeft);
            RectTransform searchRect = searchLabel.rectTransform;
            searchRect.anchorMin = new Vector2(0f, 1f);
            searchRect.anchorMax = new Vector2(1f, 1f);
            searchRect.pivot = new Vector2(0.5f, 1f);
            searchRect.anchoredPosition = new Vector2(0f, -16f);
            searchRect.sizeDelta = new Vector2(-48f, 48f);

            statusLabel = CreateText(panel.transform, "Status", "", 22f, TextAlignmentOptions.MidlineRight);
            RectTransform statusRect = statusLabel.rectTransform;
            statusRect.anchorMin = new Vector2(0f, 1f);
            statusRect.anchorMax = new Vector2(1f, 1f);
            statusRect.pivot = new Vector2(0.5f, 1f);
            statusRect.anchoredPosition = new Vector2(0f, -64f);
            statusRect.sizeDelta = new Vector2(-48f, 32f);
            statusLabel.color = new Color(0.7f, 0.8f, 0.95f, 1f);

            TextMeshProUGUI help = CreateText(panel.transform, "Help", "TICK A CARD TO GIVE IT    UNTICK TO REMOVE IT    ESC CLOSES", 20f, TextAlignmentOptions.Center);
            RectTransform helpRect = help.rectTransform;
            helpRect.anchorMin = new Vector2(0f, 0f);
            helpRect.anchorMax = new Vector2(1f, 0f);
            helpRect.pivot = new Vector2(0.5f, 0f);
            helpRect.anchoredPosition = new Vector2(0f, 16f);
            helpRect.sizeDelta = new Vector2(-32f, 32f);
            help.color = new Color(0.65f, 0.75f, 0.9f, 1f);

            GameObject scroll = new GameObject("Scroll");
            scroll.transform.SetParent(panel.transform, false);
            scroll.transform.localScale = Vector3.one;
            RectTransform scrollRectTransform = scroll.AddComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0f, 0f);
            scrollRectTransform.anchorMax = new Vector2(1f, 1f);
            scrollRectTransform.offsetMin = new Vector2(24f, 60f);
            scrollRectTransform.offsetMax = new Vector2(-24f, -104f);

            ScrollRect scrollRect = scroll.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 40f;

            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scroll.transform, false);
            viewport.transform.localScale = Vector3.one;
            RectTransform viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewport.AddComponent<RectMask2D>();
            Image viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.01f);

            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            content.transform.localScale = Vector3.one;
            contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;

            root.SetActive(false);
        }

        static TextMeshProUGUI CreateText(Transform parent, string name, string text, float size, TextAlignmentOptions alignment)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one;
            TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
            label.font = RoundsResources.MenuFont;
            label.fontSize = size;
            label.alignment = alignment;
            label.color = Color.white;
            label.text = text;
            label.raycastTarget = false;
            return label;
        }
    }
}
