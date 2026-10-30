using System.Collections.Generic;
using TMPro;
using UnboundLib;
using UnboundLib.Cards;
using UnboundLib.Extensions;
using UnboundLib.GameModes;
using UnboundLib.Networking;
using UnboundLib.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace RWF.UI
{
    public class SandboxCardBrowser : MonoBehaviour
    {
        const string LockKey = "RWF_SandboxCardBrowser";
        const float RowHeight = 44f;
        const float ColumnWidth = 88f;
        const float ViewButtonWidth = 76f;

        bool open;
        string filter = "";
        string playerKey = "";
        string previewCardId = "";
        float nextHintSearch;

        GameObject root;
        GameObject hint;
        GameObject spawnHint;
        GameObject preview;
        Image dim;
        Transform vanillaHint;
        RectTransform rootRect;
        RectTransform panelRect;
        RectTransform contentRect;
        RectTransform headerRect;
        TextMeshProUGUI searchLabel;
        TextMeshProUGUI statusLabel;
        readonly List<GameObject> rows = new List<GameObject>();
        readonly List<GameObject> headers = new List<GameObject>();
        readonly Dictionary<int, List<string>> selectedCardsByPlayer = new Dictionary<int, List<string>>();

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
                if (spawnHint != null)
                {
                    spawnHint.SetActive(false);
                }
                return;
            }

            EnsureHint();
            EnsureSpawnHint();
            bool waitingForPlayer = JoinedPlayers().Count == 0;
            hint.SetActive(!open && !DevConsole.isTyping);
            if (spawnHint != null)
            {
                bool showSpawn = waitingForPlayer && !open && !DevConsole.isTyping;
                if (showSpawn && !spawnHint.activeSelf)
                {
                    spawnHint.transform.SetAsLastSibling();
                }
                spawnHint.SetActive(showSpawn);
            }
            HideVanillaHelper();

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

            string key = JoinedPlayerKey();
            if (key != playerKey)
            {
                playerKey = key;
                Rebuild();
            }

            if (ApplyTyping())
            {
                Rebuild();
            }
        }

        void LateUpdate()
        {
            if (preview == null)
            {
                return;
            }

            RevealPreviewFront(preview);
            PlacePreview();
        }

        void OnDisable()
        {
            if (open)
            {
                Close();
            }
        }

        void OnDestroy()
        {
            HidePreview();
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
            playerKey = JoinedPlayerKey();
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
            HidePreview();
        }

        static string JoinedPlayerKey()
        {
            if (PlayerManager.instance == null || PlayerManager.instance.players == null)
            {
                return "";
            }

            List<int> ids = new List<int>();
            List<Player> players = PlayerManager.instance.players;
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i] != null)
                {
                    ids.Add(players[i].playerID);
                }
            }
            ids.Sort();
            string key = "";
            for (int i = 0; i < ids.Count; i++)
            {
                if (i > 0)
                {
                    key += ",";
                }
                key += ids[i];
            }
            return key;
        }

        static List<Player> JoinedPlayers()
        {
            List<Player> joined = new List<Player>();
            if (PlayerManager.instance == null || PlayerManager.instance.players == null)
            {
                return joined;
            }

            List<Player> players = PlayerManager.instance.players;
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i] != null)
                {
                    joined.Add(players[i]);
                }
            }
            joined.Sort((a, b) => a.playerID.CompareTo(b.playerID));
            return joined;
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

            List<Player> players = JoinedPlayers();
            float columnsWidth = players.Count * ColumnWidth;
            if (panelRect != null)
            {
                Vector2 panelSize = panelRect.sizeDelta;
                panelSize.x = Mathf.Max(760f, 340f + columnsWidth);
                panelRect.sizeDelta = panelSize;
            }
            RebuildHeaders(players);

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
                rows.Add(CreateRow(matches[i], players));
                shown++;
            }

            statusLabel.text = players.Count == 0 ? "JOIN TO APPLY CARDS" : shown + " CARDS";
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i] == null)
                {
                    continue;
                }

                TextMeshProUGUI[] labels = rows[i].GetComponentsInChildren<TextMeshProUGUI>(true);
                for (int j = 0; j < labels.Length; j++)
                {
                    labels[j].ForceMeshUpdate(true);
                }
            }
            searchLabel.ForceMeshUpdate(true);
            statusLabel.ForceMeshUpdate(true);
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

        void RebuildHeaders(List<Player> players)
        {
            for (int i = 0; i < headers.Count; i++)
            {
                if (headers[i] != null)
                {
                    Destroy(headers[i]);
                }
            }
            headers.Clear();
            if (headerRect == null)
            {
                return;
            }

            for (int i = 0; i < players.Count; i++)
            {
                Player player = players[i];
                int colorId = player.colorID();
                GameObject header = new GameObject("Player");
                header.transform.SetParent(headerRect, false);
                header.transform.localScale = Vector3.one;
                TextMeshProUGUI label = header.AddComponent<TextMeshProUGUI>();
                RectTransform rect = label.rectTransform;
                rect.anchorMin = new Vector2(1f, 0.5f);
                rect.anchorMax = new Vector2(1f, 0.5f);
                rect.pivot = new Vector2(1f, 0.5f);
                rect.anchoredPosition = new Vector2(-((players.Count - 1 - i) * ColumnWidth), 0f);
                rect.sizeDelta = new Vector2(ColumnWidth - 4f, 32f);
                label.font = RoundsResources.MenuFont;
                label.fontSize = 16f;
                label.alignment = TextAlignmentOptions.Center;
                label.color = PlayerSkinBank.GetPlayerSkinColors(colorId).winText;
                label.enableWordWrapping = false;
                label.overflowMode = TextOverflowModes.Overflow;
                label.raycastTarget = false;
                label.text = "P" + (player.playerID + 1) + "\n" + ExtraPlayerSkins.GetTeamColorName(colorId).ToUpper();
                label.ForceMeshUpdate(true);
                headers.Add(header);
            }
        }

        GameObject CreateRow(CardInfo card, List<Player> players)
        {
            GameObject row = new GameObject("Card");
            row.transform.SetParent(contentRect, false);
            row.transform.localScale = Vector3.one;

            LayoutElement layout = row.AddComponent<LayoutElement>();
            layout.minHeight = RowHeight;
            layout.preferredHeight = RowHeight;
            layout.flexibleWidth = 1f;

            Image image = row.AddComponent<Image>();
            image.color = new Color(0.08f, 0.18f, 0.38f, 0.95f);

            GameObject labelGo = new GameObject("Label");
            labelGo.transform.SetParent(row.transform, false);
            labelGo.transform.localScale = Vector3.one;
            TextMeshProUGUI label = labelGo.AddComponent<TextMeshProUGUI>();
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.pivot = new Vector2(0f, 0.5f);
            labelRect.offsetMin = new Vector2(ViewButtonWidth + 16f, 4f);
            labelRect.offsetMax = new Vector2(-(players.Count * ColumnWidth + 8f), -4f);
            label.font = RoundsResources.MenuFont;
            label.fontSize = 26f;
            label.enableAutoSizing = false;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Overflow;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.color = Color.white;
            label.text = card.cardName.ToUpper();
            label.raycastTarget = false;
            label.ForceMeshUpdate(true);
            CreateViewButton(row.transform, card);

            for (int i = 0; i < players.Count; i++)
            {
                CreateTick(row.transform, card, players[i], players.Count, i);
            }

            return row;
        }

        void CreateTick(Transform row, CardInfo card, Player player, int playerCount, int column)
        {
            List<string> owned;
            bool selected = selectedCardsByPlayer.TryGetValue(player.playerID, out owned) && owned.Contains(card.gameObject.name);
            Color skin = PlayerSkinBank.GetPlayerSkinColors(player.colorID()).winText;

            GameObject box = new GameObject("Tick");
            box.transform.SetParent(row, false);
            box.transform.localScale = Vector3.one;
            RectTransform boxRect = box.AddComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(1f, 0.5f);
            boxRect.anchorMax = new Vector2(1f, 0.5f);
            boxRect.pivot = new Vector2(0.5f, 0.5f);
            boxRect.anchoredPosition = new Vector2(-((playerCount - 1 - column) * ColumnWidth + ColumnWidth * 0.5f), 0f);
            boxRect.sizeDelta = new Vector2(28f, 28f);

            Image boxImage = box.AddComponent<Image>();
            boxImage.color = selected ? skin : new Color(0.02f, 0.05f, 0.12f, 1f);

            Button button = box.AddComponent<Button>();
            button.targetGraphic = boxImage;
            ColorBlock colors = button.colors;
            colors.highlightedColor = skin;
            colors.pressedColor = skin * 0.7f;
            colors.normalColor = boxImage.color;
            button.colors = colors;

            CardInfo picked = card;
            int playerId = player.playerID;
            button.onClick.AddListener(() => ToggleCard(picked, playerId));

            GameObject markGo = new GameObject("Mark");
            markGo.transform.SetParent(box.transform, false);
            markGo.transform.localScale = Vector3.one;
            TextMeshProUGUI mark = markGo.AddComponent<TextMeshProUGUI>();
            Stretch(mark.rectTransform, Vector2.zero, Vector2.zero);
            mark.font = RoundsResources.MenuFont;
            mark.fontSize = 22f;
            mark.alignment = TextAlignmentOptions.Center;
            mark.color = new Color(0.02f, 0.08f, 0.16f, 1f);
            mark.text = selected ? "X" : "";
            mark.raycastTarget = false;
            mark.ForceMeshUpdate(true);
        }

        void CreateViewButton(Transform row, CardInfo card)
        {
            bool showing = preview != null && previewCardId == card.gameObject.name;

            GameObject box = new GameObject("View");
            box.transform.SetParent(row, false);
            box.transform.localScale = Vector3.one;
            RectTransform boxRect = box.AddComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0f, 0.5f);
            boxRect.anchorMax = new Vector2(0f, 0.5f);
            boxRect.pivot = new Vector2(0f, 0.5f);
            boxRect.anchoredPosition = new Vector2(8f, 0f);
            boxRect.sizeDelta = new Vector2(ViewButtonWidth, 30f);

            Image boxImage = box.AddComponent<Image>();
            boxImage.color = showing ? new Color(0.2f, 0.85f, 0.95f, 1f) : new Color(0.02f, 0.05f, 0.12f, 1f);

            Button button = box.AddComponent<Button>();
            button.targetGraphic = boxImage;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.25f, 0.9f, 1f, 1f);
            colors.pressedColor = new Color(0.1f, 0.55f, 0.7f, 1f);
            colors.normalColor = boxImage.color;
            button.colors = colors;

            CardInfo picked = card;
            button.onClick.AddListener(() => ShowCard(picked));

            GameObject labelGo = new GameObject("Label");
            labelGo.transform.SetParent(box.transform, false);
            labelGo.transform.localScale = Vector3.one;
            TextMeshProUGUI label = labelGo.AddComponent<TextMeshProUGUI>();
            Stretch(label.rectTransform, Vector2.zero, Vector2.zero);
            label.font = RoundsResources.MenuFont;
            label.fontSize = 16f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = showing ? new Color(0.02f, 0.08f, 0.16f, 1f) : Color.white;
            label.text = showing ? "HIDE" : "VIEW";
            label.raycastTarget = false;
            label.ForceMeshUpdate(true);
        }

        void ShowCard(CardInfo card)
        {
            if (card == null)
            {
                return;
            }

            if (preview != null && previewCardId == card.gameObject.name)
            {
                HidePreview();
                Rebuild();
                return;
            }

            HidePreview();
            if (CardChoice.instance == null || Camera.main == null)
            {
                return;
            }

            preview = CardChoice.instance.AddCardVisual(card, Vector3.zero);
            previewCardId = card.gameObject.name;
            PreparePreview(preview);
            SetDim(false);
            Rebuild();
            PlacePreview();
        }

        void HidePreview()
        {
            if (preview != null)
            {
                Destroy(preview);
                preview = null;
            }

            previewCardId = "";
            SetDim(true);
        }

        static void DisablePreviewGameplay(GameObject cardObject)
        {
            ApplyCardStats[] stats = cardObject.GetComponentsInChildren<ApplyCardStats>(true);
            for (int i = 0; i < stats.Length; i++)
            {
                stats[i].enabled = false;
            }

            Collider2D[] colliders2D = cardObject.GetComponentsInChildren<Collider2D>(true);
            for (int i = 0; i < colliders2D.Length; i++)
            {
                colliders2D[i].enabled = false;
            }

            Rigidbody2D[] bodies2D = cardObject.GetComponentsInChildren<Rigidbody2D>(true);
            for (int i = 0; i < bodies2D.Length; i++)
            {
                bodies2D[i].isKinematic = true;
            }
        }

        static readonly Vector3 PreviewInnerScale = Vector3.one * 1.15f;

        void PreparePreview(GameObject cardObject)
        {
            DisablePreviewGameplay(cardObject);
            HoldPreviewScale(cardObject);

            Canvas[] canvases = cardObject.GetComponentsInChildren<Canvas>(true);
            for (int i = 0; i < canvases.Length; i++)
            {
                canvases[i].sortingLayerName = "MostFront";
                GraphicRaycaster raycaster = canvases[i].GetComponent<GraphicRaycaster>();
                if (raycaster != null)
                {
                    raycaster.enabled = false;
                }
            }

            HideCover(cardObject, "Back");
            HideCover(cardObject, "BlockFront");
        }

        static void HoldPreviewScale(GameObject cardObject)
        {
            SetScaleToZero[] zeroScales = cardObject.GetComponentsInChildren<SetScaleToZero>(true);
            for (int i = 0; i < zeroScales.Length; i++)
            {
                zeroScales[i].enabled = false;
                zeroScales[i].transform.localScale = PreviewInnerScale;
            }

            ScaleShake[] shakes = cardObject.GetComponentsInChildren<ScaleShake>(true);
            for (int i = 0; i < shakes.Length; i++)
            {
                shakes[i].enabled = false;
                shakes[i].transform.localScale = PreviewInnerScale;
            }
        }

        static void HideCover(GameObject cardObject, string objectName)
        {
            Transform[] transforms = cardObject.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform transform = transforms[i];
                if (transform != null && transform.name == objectName && transform.gameObject.activeSelf)
                {
                    transform.gameObject.SetActive(false);
                }
            }
        }

        static void RevealPreviewFront(GameObject cardObject)
        {
            HideCover(cardObject, "Back");
            HideCover(cardObject, "BlockFront");

            CardVisuals[] visuals = cardObject.GetComponentsInChildren<CardVisuals>(true);
            for (int i = 0; i < visuals.Length; i++)
            {
                CardVisuals visual = visuals[i];
                if (visual == null)
                {
                    continue;
                }

                if (visual.objectsToToggle != null)
                {
                    for (int j = 0; j < visual.objectsToToggle.Length; j++)
                    {
                        GameObject cover = visual.objectsToToggle[j];
                        if (cover != null && cover.activeSelf)
                        {
                            cover.SetActive(false);
                        }
                    }
                }

                Transform grid = visual.transform.Find("Canvas/Front/Grid");
                if (grid != null)
                {
                    CanvasGroup group = grid.GetComponent<CanvasGroup>();
                    if (group != null)
                    {
                        group.alpha = 1f;
                    }
                }
            }
        }

        void PlacePreview()
        {
            if (preview == null || Camera.main == null)
            {
                return;
            }

            HoldPreviewScale(preview);
            Camera cam = Camera.main;
            float distance = DistanceToGameplayPlane(cam);
            float viewportX = PreviewViewportX();
            preview.transform.rotation = Quaternion.identity;
            preview.transform.position = cam.ViewportToWorldPoint(new Vector3(viewportX, 0.5f, distance));
            preview.transform.localScale = cam.orthographic ? Vector3.one * (cam.orthographicSize / 20f) : Vector3.one;
        }

        float PreviewViewportX()
        {
            Canvas canvas = Unbound.Instance != null ? Unbound.Instance.canvas : null;
            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
            float canvasWidth = canvasRect != null ? canvasRect.rect.width : 0f;
            float panelWidth = panelRect != null ? panelRect.sizeDelta.x : 760f;
            if (canvasWidth < 1f || panelWidth < 1f)
            {
                return 0.78f;
            }

            float rightEdge = (canvasWidth + panelWidth) * 0.5f;
            float mid = (rightEdge + canvasWidth) * 0.5f;
            return Mathf.Clamp(mid / canvasWidth, 0.72f, 0.88f);
        }

        void SetDim(bool visible)
        {
            if (dim != null)
            {
                dim.enabled = visible;
            }
        }

        static float DistanceToGameplayPlane(Camera cam)
        {
            Plane plane = new Plane(Vector3.forward, Vector3.zero);
            Ray ray = new Ray(cam.transform.position, cam.transform.forward);
            float distance;
            if (plane.Raycast(ray, out distance) && distance > 0.01f)
            {
                return distance;
            }

            return Mathf.Max(0.01f, Mathf.Abs(cam.transform.position.z));
        }

        void HideVanillaHelper()
        {
            if (vanillaHint != null)
            {
                if (vanillaHint.gameObject.activeSelf)
                {
                    vanillaHint.gameObject.SetActive(false);
                }
                return;
            }

            if (Time.unscaledTime < nextHintSearch)
            {
                return;
            }

            nextHintSearch = Time.unscaledTime + 0.5f;
            TMP_Text[] labels = Object.FindObjectsOfType<TMP_Text>();
            for (int i = 0; i < labels.Length; i++)
            {
                TMP_Text label = labels[i];
                if (label == null || string.IsNullOrEmpty(label.text) || label.text.IndexOf("SHOOT THE CARDS") < 0)
                {
                    continue;
                }

                vanillaHint = HighestHelperRoot(label.transform);
                vanillaHint.gameObject.SetActive(false);
                return;
            }
        }

        static Transform HighestHelperRoot(Transform label)
        {
            Transform banner = label;
            while (banner.parent != null && banner.parent.GetComponent<Canvas>() == null)
            {
                TMP_Text[] texts = banner.parent.GetComponentsInChildren<TMP_Text>(true);
                bool onlyHelper = true;
                for (int i = 0; i < texts.Length; i++)
                {
                    if (texts[i] == null || string.IsNullOrEmpty(texts[i].text))
                    {
                        continue;
                    }

                    if (texts[i].text.IndexOf("SHOOT THE CARDS") >= 0)
                    {
                        continue;
                    }

                    onlyHelper = false;
                    break;
                }

                if (!onlyHelper)
                {
                    break;
                }

                banner = banner.parent;
            }

            return banner;
        }

        void ToggleCard(CardInfo card, int playerId)
        {
            if (card == null)
            {
                return;
            }

            List<string> owned;
            if (!selectedCardsByPlayer.TryGetValue(playerId, out owned))
            {
                owned = new List<string>();
                selectedCardsByPlayer[playerId] = owned;
            }

            string id = card.gameObject.name;
            if (owned.Contains(id))
            {
                owned.Remove(id);
            }
            else
            {
                owned.Add(id);
            }

            ApplySelection(playerId);
            Rebuild();
        }

        void ApplySelection(int playerId)
        {
            List<string> owned;
            if (!selectedCardsByPlayer.TryGetValue(playerId, out owned))
            {
                owned = new List<string>();
            }

            NetworkingManager.RPC(typeof(SandboxCardBrowser), nameof(RPC_ApplyPlayerCards), playerId, owned.ToArray());
        }

        [UnboundRPC]
        public static void RPC_ApplyPlayerCards(int playerId, string[] cardIds)
        {
            if (PlayerManager.instance == null)
            {
                return;
            }

            Player player = null;
            List<Player> players = PlayerManager.instance.players;
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i] != null && players[i].playerID == playerId)
                {
                    player = players[i];
                    break;
                }
            }

            if (player == null || player.data == null)
            {
                return;
            }

            RemoveCustomCards(player);
            player.InvokeMethod("FullReset");
            RestoreBaseline(player);
            if (player.data.currentCards != null)
            {
                player.data.currentCards.Clear();
            }
            EnsureCardBars();
            ClearCardBar(player);

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
                CardInfo info = temp.GetComponent<CardInfo>();
                if (info != null)
                {
                    info.sourceCard = card;
                }
                ApplyCardStats stats = temp.GetComponent<ApplyCardStats>();
                if (stats != null)
                {
                    stats.InvokeMethod("Start");
                    stats.OFFLINE_Pick(new Player[] { player });
                }
                Object.Destroy(temp);
            }
        }

        static void RemoveCustomCards(Player player)
        {
            if (player.data.currentCards == null)
            {
                return;
            }

            Gun gun = player.GetComponent<Holding>() != null && player.GetComponent<Holding>().holdable != null
                ? player.GetComponent<Holding>().holdable.GetComponent<Gun>()
                : null;
            GunAmmo ammo = gun != null ? gun.GetComponentInChildren<GunAmmo>() : null;
            CharacterData data = player.GetComponent<CharacterData>();
            HealthHandler health = player.GetComponent<HealthHandler>();
            Gravity gravity = player.GetComponent<Gravity>();
            Block block = player.GetComponent<Block>();
            CharacterStatModifiers stats = player.GetComponent<CharacterStatModifiers>();

            for (int i = 0; i < player.data.currentCards.Count; i++)
            {
                CardInfo card = player.data.currentCards[i];
                if (card == null)
                {
                    continue;
                }

                CustomCard custom = card.GetComponent<CustomCard>();
                if (custom != null)
                {
                    custom.OnRemoveCard(player, gun, ammo, data, health, gravity, block, stats);
                }
            }
        }

        static void RestoreBaseline(Player player)
        {
            CharacterData data = player.GetComponent<CharacterData>();
            if (data != null)
            {
                data.jumps = 1;
                data.currentJumps = 1;
            }

            HealthHandler health = player.GetComponent<HealthHandler>();
            if (health != null)
            {
                health.regeneration = 0f;
            }

            PlayerAudioModifyers audio = player.GetComponent<PlayerAudioModifyers>();
            if (audio != null && audio.modifyers != null)
            {
                audio.modifyers.Clear();
            }

            GameObject prefab = PlayerAssigner.instance != null ? PlayerAssigner.instance.playerPrefab : null;
            if (prefab == null)
            {
                return;
            }

            Gravity prefabGravity = prefab.GetComponent<Gravity>();
            Gravity gravity = player.GetComponent<Gravity>();
            if (prefabGravity != null && gravity != null)
            {
                gravity.gravityForce = prefabGravity.gravityForce;
                gravity.exponent = prefabGravity.exponent;
            }

            Holding prefabHolding = prefab.GetComponent<Holding>();
            Holding holding = player.GetComponent<Holding>();
            Gun prefabGun = prefabHolding != null && prefabHolding.holdable != null ? prefabHolding.holdable.GetComponent<Gun>() : null;
            Gun gun = holding != null && holding.holdable != null ? holding.holdable.GetComponent<Gun>() : null;
            if (prefabGun == null || gun == null)
            {
                return;
            }

            gun.bodyRecoil = prefabGun.bodyRecoil;
            if (prefabGun.projectiles != null && prefabGun.projectiles.Length > 0 && gun.projectiles != null && gun.projectiles.Length > 0)
            {
                gun.projectiles[0].objectToSpawn = prefabGun.projectiles[0].objectToSpawn;
            }

            GunAmmo prefabAmmo = prefabGun.GetComponentInChildren<GunAmmo>();
            GunAmmo ammo = gun.GetComponentInChildren<GunAmmo>();
            if (prefabAmmo != null && ammo != null)
            {
                ammo.ammoReg = prefabAmmo.ammoReg;
            }
        }

        static void EnsureCardBars()
        {
            if (CardBarHandler.instance == null || PlayerManager.instance == null)
            {
                return;
            }

            CardBar[] bars = CardBarHandler.instance.GetFieldValue("cardBars") as CardBar[];
            int count = PlayerManager.instance.players.Count;
            if (bars == null || bars.Length < count)
            {
                RWF.CardBarHandlerExtensions.Rebuild(CardBarHandler.instance);
                bars = CardBarHandler.instance.GetFieldValue("cardBars") as CardBar[];
            }

            if (bars == null)
            {
                return;
            }

            for (int i = 0; i < bars.Length; i++)
            {
                if (bars[i] != null && bars[i].GetFieldValue("source") == null)
                {
                    bars[i].InvokeMethod("Start");
                }
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

        void EnsureSpawnHint()
        {
            if (spawnHint != null)
            {
                return;
            }

            Canvas canvas = Unbound.Instance != null ? Unbound.Instance.canvas : null;
            if (canvas == null)
            {
                return;
            }

            spawnHint = new GameObject("SandboxSpawnHint");
            spawnHint.transform.SetParent(canvas.transform, false);
            spawnHint.transform.localScale = Vector3.one;

            RectTransform rect = spawnHint.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(1280f, 200f);

            Image image = spawnHint.AddComponent<Image>();
            image.color = new Color(0.04f, 0.1f, 0.22f, 0.88f);
            image.raycastTarget = false;

            GameObject labelGo = new GameObject("Label");
            labelGo.transform.SetParent(spawnHint.transform, false);
            labelGo.transform.localScale = Vector3.one;
            TextMeshProUGUI label = labelGo.AddComponent<TextMeshProUGUI>();
            Stretch(label.rectTransform, new Vector2(24f, 12f), new Vector2(-24f, -12f));
            label.font = RoundsResources.MenuFont;
            label.fontSize = 52f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Overflow;
            label.text = "PRESS [SPACE] TO SPAWN\nOR PRESS A CONTROLLER BUTTON";
            label.raycastTarget = false;
            label.ForceMeshUpdate(true);
            spawnHint.SetActive(false);
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

            rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            dim = root.AddComponent<Image>();
            dim.color = new Color(0f, 0.02f, 0.08f, 0.72f);

            GameObject panel = new GameObject("Panel");
            panel.transform.SetParent(root.transform, false);
            panel.transform.localScale = Vector3.one;
            panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 1f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(760f, -48f);
            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.03f, 0.08f, 0.18f, 0.96f);

            searchLabel = CreateText(panel.transform, "Search", "TYPE TO SEARCH", 32f, TextAlignmentOptions.MidlineLeft);
            RectTransform searchRect = searchLabel.rectTransform;
            searchRect.anchorMin = new Vector2(0f, 1f);
            searchRect.anchorMax = new Vector2(0f, 1f);
            searchRect.pivot = new Vector2(0f, 1f);
            searchRect.anchoredPosition = new Vector2(28f, -18f);
            searchRect.sizeDelta = new Vector2(420f, 42f);

            statusLabel = CreateText(panel.transform, "Status", "", 22f, TextAlignmentOptions.MidlineRight);
            RectTransform statusRect = statusLabel.rectTransform;
            statusRect.anchorMin = new Vector2(1f, 1f);
            statusRect.anchorMax = new Vector2(1f, 1f);
            statusRect.pivot = new Vector2(1f, 1f);
            statusRect.anchoredPosition = new Vector2(-28f, -22f);
            statusRect.sizeDelta = new Vector2(180f, 36f);
            statusLabel.color = new Color(0.7f, 0.8f, 0.95f, 1f);

            GameObject header = new GameObject("Headers");
            header.transform.SetParent(panel.transform, false);
            header.transform.localScale = Vector3.one;
            headerRect = header.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.anchoredPosition = new Vector2(0f, -64f);
            headerRect.sizeDelta = new Vector2(-48f, 40f);

            TextMeshProUGUI help = CreateText(panel.transform, "Help", "VIEW SHOWS THE CARD  ·  TICK A PLAYER COLUMN TO GIVE THAT PLAYER THE CARD  ·  ESC CLOSES", 18f, TextAlignmentOptions.Center);
            help.enableWordWrapping = true;
            help.overflowMode = TextOverflowModes.Overflow;
            RectTransform helpRect = help.rectTransform;
            helpRect.anchorMin = new Vector2(0f, 0f);
            helpRect.anchorMax = new Vector2(1f, 0f);
            helpRect.pivot = new Vector2(0.5f, 0f);
            helpRect.offsetMin = new Vector2(20f, 10f);
            helpRect.offsetMax = new Vector2(-20f, 64f);
            help.color = new Color(0.65f, 0.75f, 0.9f, 1f);

            GameObject scroll = new GameObject("Scroll");
            scroll.transform.SetParent(panel.transform, false);
            scroll.transform.localScale = Vector3.one;
            RectTransform scrollRectTransform = scroll.AddComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0f, 0f);
            scrollRectTransform.anchorMax = new Vector2(1f, 1f);
            scrollRectTransform.offsetMin = new Vector2(24f, 80f);
            scrollRectTransform.offsetMax = new Vector2(-24f, -112f);

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
            label.overflowMode = TextOverflowModes.Overflow;
            label.enableWordWrapping = false;
            label.raycastTarget = false;
            label.ForceMeshUpdate(true);
            return label;
        }

        static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.localScale = Vector3.one;
        }
    }
}
