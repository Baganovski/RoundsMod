using BepInEx;
using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnboundLib;
using UnboundLib.Cards;
using UnboundLib.GameModes;
using UnboundLib.Networking;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Photon.Pun.UtilityScripts;
using ExitGames.Client.Photon;
using RWF.Cards;
using RWF.UI;
using On;
using UnboundLib.Utils.UI;
using System.Linq;

namespace RWF
{
    public static class NetworkEventType
    {
        public static string ClientConnected = "client_connected";
        public static string SetTeamSize = "set_team_size";
    }

    [Serializable]
    public class DebugOptions
    {
        public static byte[] Serialize(object opts)
        {
            var options = (DebugOptions) opts;
            byte[] data = new byte[9];
            Buffer.BlockCopy(BitConverter.GetBytes(options.rounds), 0, data, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(options.points), 0, data, 4, 4);
            data[8] = (byte) (options.showSpawns ? 1 : 0);
            return data;
        }

        public static DebugOptions Deserialize(byte[] data)
        {
            if (data == null || data.Length != 9)
            {
                return new DebugOptions();
            }

            return new DebugOptions
            {
                rounds = BitConverter.ToInt32(data, 0),
                points = BitConverter.ToInt32(data, 4),
                showSpawns = data[8] != 0
            };
        }

        public int rounds = 5;
        public int points = 2;

        public bool showSpawns = false;
    }

    [BepInDependency("com.willis.rounds.unbound", "2.10.2")]
    [BepInPlugin(ModId, "RoundsWithFriends", Version)]
    public class RWFMod : BaseUnityPlugin
    {
        private const string ModName = "Rounds With Friends";
        private static string CompatibilityModName => RWFMod.ModName.Replace(" ", "");
        private const string ModId = "io.olavim.rounds.rwf";
        public const string Version = "2.2.2";

#if DEBUG
        public static readonly bool DEBUG = true;
#else
        public static readonly bool DEBUG = false;
#endif

#if BETA
        public static readonly bool BETA = true;
#else
        public static readonly bool BETA = false;
#endif

        public static RWFMod instance;

        private static bool facesSet = false;

        public static string GetCustomPropertyKey(string prop)
        {
            return $"{ModId}/{prop}";
        }

        public static void DebugLog(object obj)
        {
            if (obj == null)
            {
                obj = "null";
            }
            instance.Logger.LogMessage(obj);
        }

        public static void Log(object obj)
        {
            if (obj == null)
            {
                obj = "null";
            }
            instance.Logger.LogInfo(obj);
        }

        public const int MaxPlayersHardLimit = 32;
        public static int MaxColorsHardLimit => UnboundLib.Utils.ExtraPlayerSkins.numberOfSkins;
        public const int MaxCharactersPerClientHardLimit = 2;

        public const string PlayersRequiredToStartGameKey = "playersRequiredToStartGame";
        public const string MaxPlayersKey = "maxPlayers";
        public const string MaxTeamsKey = "maxTeams";
        public const string MaxClientsKey = "maxClients";
        public const string RoundsToWinGameKey = "roundsToWinGame";

        public int RoundsToWinGame
        {
            get
            {
                int rounds = PlayerPrefs.GetInt(RWFMod.GetCustomPropertyKey(RWFMod.RoundsToWinGameKey), 5);
                return rounds == 3 ? 3 : 5;
            }
        }

        public void SetRoundsToWinGame(int rounds)
        {
            rounds = rounds == 3 ? 3 : 5;
            PlayerPrefs.SetInt(RWFMod.GetCustomPropertyKey(RWFMod.RoundsToWinGameKey), rounds);
            if (this.localRoundsLabel != null)
            {
                this.localRoundsLabel.text = $"ROUNDS: {rounds}";
            }
            foreach (IGameModeHandler handler in GameModeManager.Handlers.Values)
            {
                if (handler?.Settings != null && handler.Settings.ContainsKey(RWFMod.RoundsToWinGameKey))
                {
                    handler.ChangeSetting(RWFMod.RoundsToWinGameKey, rounds);
                }
            }

            this.SyncLobbyRoundCounter();
        }

        public int ToggleRoundsToWinGame()
        {
            int rounds = this.RoundsToWinGame == 3 ? 5 : 3;
            this.SetRoundsToWinGame(rounds);
            return rounds;
        }

        void ApplyRoundsToWinGame()
        {
            if (!(PhotonNetwork.OfflineMode || PhotonNetwork.CurrentRoom == null || PhotonNetwork.IsMasterClient))
            {
                return;
            }

            IGameModeHandler handler = GameModeManager.CurrentHandler;
            if (handler?.Settings != null && handler.Settings.ContainsKey(RWFMod.RoundsToWinGameKey))
            {
                handler.ChangeSetting(RWFMod.RoundsToWinGameKey, this.RoundsToWinGame);
            }
        }

        public void SyncLobbyRoundCounter()
        {
            if (UIHandler.instance == null)
            {
                return;
            }

            UIHandler.instance.SyncLobbyRoundCounter();
        }

        public int MaxClients
        {
            get
            {
                return 16;
            }
        }

        public int MaxPlayers
        {
            get
            {
                return 16;
            }
        }

        public int MinPlayers
        {
            get
            {
                return 2;
            }
        }

        public int MaxTeams
        {
            get
            {
                return this.MaxPlayers;
            }
        }

        public int MaxCharactersPerClient
        {
            get
            {
                return RWFMod.MaxCharactersPerClientHardLimit;
            }
        }

        public bool IsCeaseFire { get; private set; }

        public Text infoText;
        TextMeshProUGUI localRoundsLabel;
        private Dictionary<string, bool> soundEnabled;
        private Dictionary<string, bool> gmInitialized;

        public DebugOptions debugOptions = new DebugOptions();

        public void Awake()
        {
            RWFMod.instance = this;

            try
            {
                Patches.PatchUtils.ApplyPatches(ModId);
                this.Logger.LogInfo("initialized");
                this.Logger.LogInfo("Baganovski local build");
            }
            catch (Exception e)
            {
                this.Logger.LogError(e.ToString());
            }
        }

        public void Start()
        {
            // register credits with unbound
            Unbound.RegisterCredits(RWFMod.ModName, new string[] { "Tilastokeskus (Project creation, 4 player support, Deathmatch, Team Deathmatch, UI, Fair pick orders)", "Pykess (> 4 player support, multiple players per client, additional player colors, disconnect handling, UI)", "BossSloth (Gamemode selection UI)" }, new string[] { "github", "Support Tilastokeskus", "Support Pykess", "Support BossSloth" }, new string[] { "https://github.com/Baganovski/RoundsMod", "https://www.buymeacoffee.com/tilastokeskus", "https://ko-fi.com/pykess", "https://www.buymeacoffee.com/BossSloth" });
            Unbound.RegisterCredits(
                RWFMod.BagsCreditsName,
                new string[]
                {
                    "Baganovski (Local version of Rounds With Friends)",
                    "Online lobbies, Steam invites, and the Online menu are removed. Local matches stay.",
                    "The main menu shows a green pixel BAG'S MOD under the ROUNDS logo, and hides it on other pages.",
                    "The corner Discord and website links are hidden.",
                    "Menu layouts are changed.",
                    "Matches can be set to 3 or 5.",
                    "Sandbox powerups are changed.",
                    "Lee Luck ignores one hit every 10 seconds and shows MISS when it blocks."
                },
                "GitHub",
                "https://github.com/Baganovski/RoundsMod");

            // add GUI to modoptions menu
            Unbound.RegisterMenu(RWFMod.ModName, () => { }, this.GUI, null, false);
            CustomCard.BuildCard<LeeLuck>();

            this.soundEnabled = new Dictionary<string, bool>();
            this.gmInitialized = new Dictionary<string, bool>();

            SceneManager.sceneLoaded += this.OnSceneLoaded;
            this.ExecuteAfterFrames(1, ArtHandler.instance.NextArt);

            Unbound.RegisterHandshake(ModId, () =>
            {
                PhotonNetwork.LocalPlayer.SetModded();
            });

            this.StartCoroutine(this.RegisterGameModesWhenReady());

            GameModeManager.OnGameModeChanged += (gm) =>
            {
                this.ApplyRoundsToWinGame();
                this.SyncLobbyRoundCounter();
                this.RedrawCharacterSelections();
                this.RedrawCharacterCreators();

                if (RWFMod.DEBUG && this.gameObject.GetComponent<DebugWindow>().enabled)
                {
                    RWFMod.SetDebugOptions(this.debugOptions);
                }
            };

            GameModeManager.AddHook(GameModeHooks.HookPointStart, gm => this.ToggleCeaseFire(true));
            GameModeManager.AddHook(GameModeHooks.HookBattleStart, gm => this.ToggleCeaseFire(false));
            GameModeManager.AddHook(GameModeHooks.HookInitEnd, this.OnGameModeInitialized);
            GameModeManager.AddHook(GameModeHooks.HookGameStart, this.UnsetFaces);
            GameModeManager.AddHook(GameModeHooks.HookPickStart, this.SetPlayerFaces);
            GameModeManager.AddHook(GameModeHooks.HookGameStart, gm => PlayerSpotlight.CancelFadeHook(true));
            GameModeManager.AddHook(GameModeHooks.HookBattleStart, gm => PlayerSpotlight.BattleStartFailsafe());

            this.gameObject.AddComponent<RoundEndHandler>();
            this.gameObject.AddComponent<SandboxCardBrowser>();
            this.gameObject.AddComponent<MenuLayerGuard>();

            if (RWFMod.DEBUG)
            {
                var debugWindow = this.gameObject.AddComponent<DebugWindow>();
                debugWindow.enabled = false;

                var sim = this.gameObject.AddComponent<PhotonLagSimulationGui>();
                sim.enabled = false;

                PhotonPeer.RegisterType(typeof(DebugOptions), 77, DebugOptions.Serialize, DebugOptions.Deserialize);
            }
            // add beta text
            if (BETA) { BetaTextHandler.AddBetaText(true); }

            On.MainMenuHandler.Awake += (orig, self) =>
            {
                orig(self);

                if (BETA) { BetaTextHandler.AddBetaText(true); }
            };
        }

        IEnumerator RegisterGameModesWhenReady()
        {
            const float timeout = 10f;
            float elapsed = 0f;
            while (MainMenuHandler.instance == null && elapsed < timeout)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            this.RegisterLocalGameMode("Deathmatch", new GameModes.DeathmatchHandler());
            this.RegisterLocalGameMode("Team Deathmatch", new GameModes.TeamDeathmatchHandler());
        }

        void RegisterLocalGameMode(string id, UnboundLib.GameModes.IGameModeHandler handler)
        {
            try
            {
                if (GameModeManager.Handlers.ContainsKey(id))
                {
                    return;
                }

                if (id == "Deathmatch")
                {
                    GameModeManager.AddHandler<GameModes.GM_Deathmatch>(id, handler);
                }
                else
                {
                    GameModeManager.AddHandler<GameModes.GM_TeamDeathmatch>(id, handler);
                }
            }
            catch (Exception e)
            {
                this.Logger.LogError(e);
            }
        }

        internal IEnumerator BrandMainMenu()
        {
            const float seconds = 15f;
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                try
                {
                    this.ApplyMainMenuBrand();
                }
                catch (Exception e)
                {
                    this.Logger.LogError(e);
                }

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            try
            {
                this.ApplyMainMenuBrand();
            }
            catch (Exception e)
            {
                this.Logger.LogError(e);
            }
        }

        internal void ApplyMainMenuBrand()
        {
            this.HideCornerLinks();

            if (MainMenuHandler.instance == null)
            {
                return;
            }

            Transform canvas = MainMenuHandler.instance.transform.Find("Canvas");
            if (canvas != null)
            {
                this.HideOnlineMenu(canvas.gameObject);
            }

            Transform title = canvas != null ? canvas.Find("Unbound Text Object") : null;
            TextMeshProUGUI unboundLabel = title != null
                ? title.GetComponent<TextMeshProUGUI>()
                : GameObject.Find("Unbound Text Object")?.GetComponent<TextMeshProUGUI>();
            this.StyleUnboundTitle(unboundLabel);
            this.SyncStampVisibility();
            this.StyleBagsCreditsButton();

            Transform group = MainMenuHandler.instance.transform.Find("Canvas/ListSelector/Main/Group");
            if (group != null)
            {
                for (int i = 0; i < group.childCount; i++)
                {
                    this.StyleUnboundTitle(group.GetChild(i).GetComponent<TextMeshProUGUI>());
                }
            }
        }

        void HideCornerLinks()
        {
            if (MainMenuHandler.instance == null)
            {
                return;
            }

            Transform canvas = MainMenuHandler.instance.transform.Find("Canvas");
            if (canvas == null)
            {
                return;
            }

            Transform[] descendants = canvas.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < descendants.Length; i++)
            {
                Transform child = descendants[i];
                if (child.name.StartsWith("Links"))
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        void StyleUnboundTitle(TextMeshProUGUI label)
        {
            if (label == null)
            {
                return;
            }

            string text = label.text;
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            text = text.Trim();
            if (!text.Equals("UNBOUND", StringComparison.OrdinalIgnoreCase)
                && !text.Equals("Baganovski's Mod", StringComparison.OrdinalIgnoreCase)
                && !text.Equals(RWFMod.StampText, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            label.text = RWFMod.StampText;
            label.color = new Color(0f, 0f, 0f, 0f);
            label.raycastTarget = false;

            if (label.gameObject.name == "Unbound Text Object")
            {
                this.PlaceStamp(label);
            }
        }

        const string BagsCreditsName = "Bag's Mod";
        const string StampText = "BAG'S MOD";
        const string StampFrameName = "BagStampFrame";
        static readonly Color StampGreen = new Color(0.25f, 0.95f, 0.35f, 1f);
        static Sprite stampSprite;

        void PlaceStamp(TextMeshProUGUI label)
        {
            Transform gap = this.StampGap();
            if (gap == null)
            {
                return;
            }

            if (label.transform.parent != gap)
            {
                label.transform.SetParent(gap, false);
            }

            RectTransform rect = label.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 22f);
            rect.sizeDelta = new Vector2(420f, 56f);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;

            label.fontSize = 26f;
            label.enableAutoSizing = false;
            label.alignment = TextAlignmentOptions.Center;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Overflow;

            this.EnsurePixelTitle(label);
            Transform frame = gap.Find(RWFMod.StampFrameName);
            if (frame != null)
            {
                frame.gameObject.SetActive(false);
            }
        }

        Transform StampGap()
        {
            if (MainMenuHandler.instance == null)
            {
                return null;
            }

            Transform group = MainMenuHandler.instance.transform.Find("Canvas/ListSelector/Main/Group");
            if (group == null)
            {
                return null;
            }

            this.FitMainMenuLogo(group);
            RectTransform groupRect = group.GetComponent<RectTransform>();
            if (groupRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(groupRect);
            }

            return group.Find("TitleGap");
        }

        void EnsurePixelTitle(TextMeshProUGUI label)
        {
            const int pixel = 5;
            const int letterGap = 3;
            const int glyphWidth = 5;
            const int glyphHeight = 7;
            Transform existing = label.transform.Find("PixelTitle");
            if (existing != null)
            {
                RectTransform existingRect = existing.GetComponent<RectTransform>();
                if (existingRect != null && Mathf.Approximately(existingRect.sizeDelta.y, glyphHeight * pixel))
                {
                    return;
                }

                UnityEngine.Object.Destroy(existing.gameObject);
            }

            GameObject root = new GameObject("PixelTitle");
            root.transform.SetParent(label.transform, false);
            RectTransform rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = Vector2.zero;

            string text = RWFMod.StampText;
            int cursor = 0;
            int width = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == ' ')
                {
                    width += pixel * 3;
                    continue;
                }

                width += glyphWidth * pixel;
                if (i < text.Length - 1 && text[i + 1] != ' ')
                {
                    width += letterGap;
                }
            }

            rootRect.sizeDelta = new Vector2(width, glyphHeight * pixel);
            for (int i = 0; i < text.Length; i++)
            {
                char character = text[i];
                if (character == ' ')
                {
                    cursor += pixel * 3;
                    continue;
                }

                int[] rows;
                if (!RWFMod.PixelGlyphs.TryGetValue(character, out rows))
                {
                    cursor += glyphWidth * pixel + letterGap;
                    continue;
                }

                for (int y = 0; y < rows.Length; y++)
                {
                    int bits = rows[y];
                    for (int x = 0; x < glyphWidth; x++)
                    {
                        if ((bits & (1 << (glyphWidth - 1 - x))) == 0)
                        {
                            continue;
                        }

                        GameObject dot = new GameObject("Px");
                        dot.transform.SetParent(root.transform, false);
                        RectTransform dotRect = dot.AddComponent<RectTransform>();
                        dotRect.anchorMin = new Vector2(0f, 1f);
                        dotRect.anchorMax = new Vector2(0f, 1f);
                        dotRect.pivot = new Vector2(0f, 1f);
                        dotRect.sizeDelta = new Vector2(pixel, pixel);
                        dotRect.anchoredPosition = new Vector2(cursor + x * pixel, -(y * pixel));
                        Image image = dot.AddComponent<Image>();
                        image.sprite = RWFMod.StampSprite;
                        image.color = RWFMod.StampGreen;
                        image.raycastTarget = false;
                    }
                }

                cursor += glyphWidth * pixel + letterGap;
            }
        }

        static readonly Dictionary<char, int[]> PixelGlyphs = new Dictionary<char, int[]>
        {
            { 'A', new[] { 0b01110, 0b10001, 0b10001, 0b11111, 0b10001, 0b10001, 0b10001 } },
            { 'B', new[] { 0b11110, 0b10001, 0b10001, 0b11110, 0b10001, 0b10001, 0b11110 } },
            { 'D', new[] { 0b11110, 0b10001, 0b10001, 0b10001, 0b10001, 0b10001, 0b11110 } },
            { 'G', new[] { 0b01111, 0b10000, 0b10000, 0b10111, 0b10001, 0b10001, 0b01111 } },
            { 'M', new[] { 0b10001, 0b11011, 0b10101, 0b10101, 0b10001, 0b10001, 0b10001 } },
            { 'O', new[] { 0b01110, 0b10001, 0b10001, 0b10001, 0b10001, 0b10001, 0b01110 } },
            { 'S', new[] { 0b01111, 0b10000, 0b10000, 0b01110, 0b00001, 0b00001, 0b11110 } },
            { '\'', new[] { 0b00110, 0b00110, 0b01100, 0b00000, 0b00000, 0b00000, 0b00000 } },
        };

        static Sprite StampSprite
        {
            get
            {
                if (RWFMod.stampSprite == null)
                {
                    Texture2D texture = Texture2D.whiteTexture;
                    RWFMod.stampSprite = Sprite.Create(
                        texture,
                        new Rect(0f, 0f, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f),
                        100f);
                }

                return RWFMod.stampSprite;
            }
        }

        void SyncStampVisibility()
        {
            if (MainMenuHandler.instance == null)
            {
                return;
            }

            Transform gap = MainMenuHandler.instance.transform.Find("Canvas/ListSelector/Main/Group/TitleGap");
            if (gap == null)
            {
                return;
            }

            bool show = ListMenu.instance != null
                && ListMenu.instance.selectedPage != null
                && ListMenu.instance.selectedPage.name == "Main";

            Transform label = gap.Find("Unbound Text Object");
            if (label != null && label.gameObject.activeSelf != show)
            {
                label.gameObject.SetActive(show);
            }

            Transform frame = gap.Find(RWFMod.StampFrameName);
            if (frame != null && frame.gameObject.activeSelf != show)
            {
                frame.gameObject.SetActive(show);
            }
        }

        private void GUI(GameObject menu)
        {
            MenuHandler.CreateText($"{RWFMod.ModName} Options", menu, out TextMeshProUGUI _, 45);
            MenuHandler.CreateText(" ", menu, out TextMeshProUGUI _, 15);
            void ShowKeybindsChanged(bool val)
            {
                PlayerPrefs.SetInt(RWFMod.GetCustomPropertyKey("ShowKeybinds"), val ? 1 : 0);
            }
            MenuHandler.CreateToggle(PlayerPrefs.GetInt(RWFMod.GetCustomPropertyKey("ShowKeybinds"), 1) == 1, "Show keybind hints in menus", menu, ShowKeybindsChanged, 30);
        }

        void StyleBagsCreditsButton()
        {
            if (MainMenuHandler.instance == null)
            {
                return;
            }

            Transform group = MainMenuHandler.instance.transform.Find("Canvas/ListSelector/CREDITS/Group");
            if (group == null)
            {
                return;
            }

            Transform bags = group.Find(RWFMod.BagsCreditsName);
            if (bags == null)
            {
                return;
            }

            Transform rounds = group.Find(RWFMod.ModName);
            if (rounds != null && bags.GetSiblingIndex() != rounds.GetSiblingIndex() + 1)
            {
                bags.SetSiblingIndex(rounds.GetSiblingIndex() + 1);
            }

            TextMeshProUGUI label = bags.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label == null)
            {
                return;
            }

            label.color = new Color(0f, 0f, 0f, 0f);
            label.raycastTarget = false;
            this.EnsurePixelTitle(label);
        }

        public void Update()
        {
            this.SyncStampVisibility();
            this.StyleBagsCreditsButton();

            if (RWFMod.DEBUG && Input.GetKeyDown(KeyCode.F8))
            {
                var debugWindow = this.gameObject.GetComponent<DebugWindow>();
                debugWindow.enabled = !debugWindow.enabled;
            }
        }

        internal void SyncDebugOptions()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                NetworkingManager.RPC(typeof(RWFMod), nameof(RWFMod.SetDebugOptions), this.debugOptions);
            }
        }

        [UnboundRPC]
        public static void SetDebugOptions(DebugOptions opts)
        {
            RWFMod.instance.debugOptions = opts;
            GameModeManager.CurrentHandler?.ChangeSetting("roundsToWinGame", opts.rounds);
            GameModeManager.CurrentHandler?.ChangeSetting("pointsToWinRound", opts.points);
        }

        private IEnumerator UnsetFaces(IGameModeHandler gm)
        {
            RWFMod.facesSet = false;
            yield break;
        }
        private IEnumerator SetPlayerFaces(IGameModeHandler gm)
        {
            if (RWFMod.facesSet || PhotonNetwork.OfflineMode)
            {
                yield break;
            }
            foreach (Player player in PlayerManager.instance.players)
            {
                if (player.data.view.IsMine)
                {
                    PlayerFace playerFace = CharacterCreatorHandler.instance.selectedPlayerFaces[player.GetAdditionalData().localID];
                    player.data.view.RPC("RPCA_SetFace", RpcTarget.All, new object[]
                    {
                        playerFace.eyeID,
                        playerFace.eyeOffset,
                        playerFace.mouthID,
                        playerFace.mouthOffset,
                        playerFace.detailID,
                        playerFace.detailOffset,
                        playerFace.detail2ID,
                        playerFace.detail2Offset
                    });
                }
            }
            RWFMod.facesSet = true;
            yield break;
        }

        static string GetHandlerID(IGameModeHandler gm)
        {
            return GameModeManager.Handlers.Where(kv => kv.Value == gm).Select(kv => kv.Key).FirstOrDefault();
        }

        private IEnumerator OnGameModeInitialized(IGameModeHandler gm)
        {
            string ID = GetHandlerID(gm);
            if (!this.gmInitialized.ContainsKey(ID))
            {
                this.gmInitialized.Add(ID, true);
            }
            else
            {
                this.gmInitialized[ID] = true;
            }

            this.ApplyRoundsToWinGame();

            yield break;
        }

        public bool IsGameModeInitialized(string handlerID)
        {
            return this.gmInitialized.ContainsKey(handlerID) && this.gmInitialized[handlerID];
        }

        private IEnumerator ToggleCeaseFire(bool isCeaseFire)
        {
            this.IsCeaseFire = isCeaseFire;
            yield break;
        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "Main")
            {
                this.gmInitialized.Clear();

                this.ExecuteAfterFrames(1, () =>
                {
                    ArtHandler.instance.NextArt();
                });

                this.ExecuteAfterSeconds(2.5f, this.ApplyMainMenuBrand);
                this.ExecuteAfterSeconds(5f, this.ApplyMainMenuBrand);
            }
        }

        public void RedrawCharacterSelections()
        {
            var uiGo = GameObject.Find("/Game/UI").gameObject;
            var mainMenuGo = uiGo.transform.Find("UI_MainMenu").Find("Canvas").gameObject;
            var charSelectionGroupGo = mainMenuGo.transform.Find("ListSelector").Find("CharacterSelect").GetChild(0).gameObject;

            for (int i = 0; i < charSelectionGroupGo.transform.childCount; i++)
            {
                var charSelGo = charSelectionGroupGo.transform.GetChild(i).gameObject;
                if (charSelGo.GetComponent<CharacterSelectionInstance>() == null || charSelGo.transform.childCount < 3)
                {
                    continue;
                }
                var faceGo = charSelGo.transform.GetChild(0).gameObject;
                var joinGo = charSelGo.transform.GetChild(1).gameObject;
                var readyGo = charSelGo.transform.GetChild(2).gameObject;

                var textColor = PlayerSkinBank.GetPlayerSkinColors(i % this.MaxTeams).winText;
                var faceColor = PlayerSkinBank.GetPlayerSkinColors(i % this.MaxTeams).color;

                joinGo.GetComponentInChildren<GeneralParticleSystem>(true).particleSettings.color = textColor;
                readyGo.GetComponentInChildren<GeneralParticleSystem>(true).particleSettings.color = textColor;

                foreach (Transform faceSelector in faceGo.transform.GetChild(0))
                {
                    faceSelector.Find("PlayerScaler_Small").Find("Face").GetComponent<SpriteRenderer>().color = faceColor;
                }
            }
        }

        public void RedrawCharacterCreators()
        {
            var charGo = GameObject.Find("/CharacterCustom");

            for (int i = 1; i < charGo.transform.childCount; i++)
            {
                var creatorGo = charGo.transform.GetChild(i);
                int playerID = i - 1;
                int teamID = playerID % this.MaxTeams;
                var faceColor = PlayerSkinBank.GetPlayerSkinColors(teamID).color;

                var buttonSource = creatorGo.transform.Find("Canvas").Find("Items").GetChild(0);
                buttonSource.Find("Face").gameObject.GetComponent<Image>().color = faceColor;

                foreach (Transform scaler in creatorGo.transform.Find("Faces"))
                {
                    scaler.Find("Face").GetComponent<SpriteRenderer>().color = faceColor;
                }
            }
        }

        public void SetSoundEnabled(string key, bool enabled)
        {
            if (!this.soundEnabled.ContainsKey(key))
            {
                this.soundEnabled.Add(key, enabled);
            }
            else
            {
                this.soundEnabled[key] = enabled;
            }
        }

        public bool GetSoundEnabled(string key)
        {
            return this.soundEnabled.ContainsKey(key) ? this.soundEnabled[key] : true;
        }

        public void SetupGameModes()
        {
            try
            {
                GameModeManager.RemoveHandler(GameModeManager.ArmsRaceID);
            }
            catch (Exception e)
            {
                this.Logger.LogError(e);
            }
        }

        public void InjectUIElements()
        {
            var uiGo = GameObject.Find("/Game/UI");
            var charGo = GameObject.Find("/CharacterCustom");
            var gameGo = uiGo.transform.Find("UI_Game").Find("Canvas").gameObject;
            var mainMenuGo = uiGo.transform.Find("UI_MainMenu").Find("Canvas").gameObject;
            var charSelectionGroupGo = mainMenuGo.transform.Find("ListSelector").Find("CharacterSelect").GetChild(0).gameObject;

            StartCoroutine(InjectKeybindsWhenReady());
            this.NormalizeMainMenuLayout(mainMenuGo);
            StartCoroutine(CatchLateMenuButtons(mainMenuGo));

            if (!charSelectionGroupGo.transform.Find("CharacterSelect 3"))
            {
                var charSelectInstanceGo1 = charSelectionGroupGo.transform.GetChild(0).gameObject;
                var charSelectInstanceGo2 = charSelectionGroupGo.transform.GetChild(1).gameObject;

                charSelectInstanceGo1.transform.position += new Vector3(0, 6, 0);
                charSelectInstanceGo2.transform.position += new Vector3(0, 6, 0);

                for (int playerNum = 3; playerNum <= this.MaxPlayers; playerNum++)
                {
                    var newCharSelectInstanceGo = GameObject.Instantiate(playerNum % 2 == 1 ? charSelectInstanceGo1 : charSelectInstanceGo2, charSelectionGroupGo.transform);
                    newCharSelectInstanceGo.name = "CharacterSelect " + playerNum.ToString();
                    newCharSelectInstanceGo.transform.localScale = Vector3.one;

                    newCharSelectInstanceGo.transform.position = (playerNum % 2 == 1 ? charSelectInstanceGo1 : charSelectInstanceGo2).transform.position - new Vector3(0, 12 * (UnityEngine.Mathf.Ceil(((float) playerNum - 2f) / 2f)), 0);

                    foreach (var portrait in newCharSelectInstanceGo.transform.GetChild(0).GetChild(0).GetComponentsInChildren<CharacterCreatorPortrait>())
                    {
                        portrait.playerId = playerNum - 1;
                    }
                    charSelectionGroupGo.GetComponent<GoBack>().goBackEvent.AddListener(newCharSelectInstanceGo.GetComponent<CharacterSelectionInstance>().ResetMenu);
                }
            }

            this.HideOnlineMenu(mainMenuGo);

            if (!charGo.transform.Find("Creator_Local" + this.MaxPlayers))
            {
                var creatorGo1 = charGo.transform.GetChild(1).gameObject;
                var creatorGo2 = charGo.transform.GetChild(2).gameObject;

                creatorGo1.transform.localPosition = new Vector3(-15, 8, 0);

                // Looks nicer when the right-side CharacterCreator is a bit further to the right
                creatorGo2.transform.localPosition = new Vector3(18, 8, 0);

                for (int playerNum = 3; playerNum <= this.MaxPlayers; playerNum++)
                {
                    string creatorName = "Creator_Local" + playerNum.ToString();
                    if (charGo.transform.Find(creatorName))
                    {
                        continue;
                    }

                    bool leftSide = playerNum % 2 == 1;
                    var template = leftSide ? creatorGo1 : creatorGo2;
                    var creatorGo = GameObject.Instantiate(template, charGo.transform);
                    creatorGo.name = creatorName;
                    creatorGo.transform.localScale = Vector3.one;
                    creatorGo.GetComponent<CharacterCreator>().playerID = playerNum - 1;
                    int row = (playerNum - 1) / 2;
                    creatorGo.transform.localPosition = new Vector3(leftSide ? -15f : 18f, 8f - (16f * row), 0f);
                    creatorGo.SetActive(false);
                }
            }

            if (!gameGo.transform.Find("RoundStartText"))
            {
                var newPos = gameGo.transform.position + new Vector3(0, 2, 0);
                var baseGo = GameObject.Instantiate(gameGo.transform.Find("PopUpHandler").Find("Yes").gameObject, newPos, Quaternion.identity, gameGo.transform);
                baseGo.name = "RoundStartText";
                baseGo.AddComponent<UI.ScalePulse>();
                baseGo.GetComponent<TextMeshProUGUI>().fontSize = 140f;
                baseGo.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
            }

            if (!gameGo.transform.Find("PopUpMenu"))
            {
                var popupGo = new GameObject("PopUpMenu");
                popupGo.transform.SetParent(gameGo.transform);
                popupGo.transform.localScale = Vector3.one;
                popupGo.AddComponent<UI.PopUpMenu>();
            }

        }
        IEnumerator CatchLateMenuButtons(GameObject mainMenuGo)
        {
            const int frames = 8;
            int lastCount = -1;
            for (int frame = 0; frame < frames; frame++)
            {
                yield return new WaitForEndOfFrame();
                Transform group = mainMenuGo != null ? mainMenuGo.transform.Find("ListSelector/Main/Group") : null;
                int count = group != null ? group.childCount : -1;
                bool lateButtons = group != null && group.Find("MODS") != null && group.Find("CREDITS") != null;
                if (count != lastCount || lateButtons)
                {
                    this.NormalizeMainMenuLayout(mainMenuGo);
                    this.ApplyMainMenuBrand();
                    lastCount = group != null ? group.childCount : count;
                }

                this.MatchPauseModsHighlight();
            }
        }

        internal void MatchPauseModsHighlight()
        {
            if (UIHandler.instance == null)
            {
                return;
            }

            Transform group = UIHandler.instance.transform.Find("Canvas/EscapeMenu/Main/Group");
            Transform mods = group != null ? group.Find("MODS") : null;
            Transform resume = group != null ? group.Find("Resume") : null;
            if (mods == null || resume == null)
            {
                return;
            }

            RectTransform resumeRect = resume.GetComponent<RectTransform>();
            RectTransform modsRect = mods.GetComponent<RectTransform>();
            if (resumeRect == null || modsRect == null)
            {
                return;
            }

            float height = resumeRect.sizeDelta.y;
            mods.localScale = Vector3.one;
            modsRect.anchorMin = resumeRect.anchorMin;
            modsRect.anchorMax = resumeRect.anchorMax;
            modsRect.pivot = resumeRect.pivot;
            // Unbound adds 400px of width, which shifts this row's center off the shared bar.
            modsRect.sizeDelta = resumeRect.sizeDelta;

            LayoutElement resumeLayout = resume.GetComponent<LayoutElement>();
            LayoutElement modsLayout = mods.gameObject.GetOrAddComponent<LayoutElement>();
            if (resumeLayout != null)
            {
                modsLayout.minHeight = resumeLayout.minHeight;
                modsLayout.preferredHeight = resumeLayout.preferredHeight;
                modsLayout.flexibleHeight = resumeLayout.flexibleHeight;
                modsLayout.ignoreLayout = resumeLayout.ignoreLayout;
            }
            else
            {
                modsLayout.minHeight = height;
                modsLayout.preferredHeight = height;
                modsLayout.flexibleHeight = 0f;
                modsLayout.ignoreLayout = false;
            }

            ContentSizeFitter modsFitter = mods.GetComponent<ContentSizeFitter>();
            if (modsFitter != null)
            {
                modsFitter.enabled = false;
            }

            TextMeshProUGUI resumeLabel = resume.GetComponentInChildren<TextMeshProUGUI>(true);
            TextMeshProUGUI modsLabel = mods.GetComponentInChildren<TextMeshProUGUI>(true);
            if (resumeLabel != null && modsLabel != null)
            {
                // Unbound's button prefab auto-sizes, so the short "MODS" label grows.
                // Stock pause labels are a fixed size.
                modsLabel.enableAutoSizing = false;
                modsLabel.fontSize = resumeLabel.fontSize;
                modsLabel.fontSizeMin = resumeLabel.fontSizeMin;
                modsLabel.fontSizeMax = resumeLabel.fontSizeMax;
                if (resumeLabel.font != null)
                {
                    modsLabel.font = resumeLabel.font;
                    modsLabel.fontSharedMaterial = resumeLabel.fontSharedMaterial;
                }

                RectTransform resumeText = resumeLabel.rectTransform;
                RectTransform modsText = modsLabel.rectTransform;
                modsText.anchorMin = resumeText.anchorMin;
                modsText.anchorMax = resumeText.anchorMax;
                modsText.pivot = resumeText.pivot;
                modsText.sizeDelta = resumeText.sizeDelta;
                modsText.anchoredPosition = resumeText.anchoredPosition;
                modsText.localScale = Vector3.one;
            }

            ListMenuButton modsButton = mods.GetComponent<ListMenuButton>();
            if (modsButton == null)
            {
                return;
            }

            // Unbound's menu-button prefab hides the shared selection bar. Pause-menu rows show it.
            modsButton.hideBar = false;

            modsButton.setBarHeight = 0f;
        }

        void HideOnlineMenu(GameObject mainMenuGo)
        {
            if (mainMenuGo == null)
            {
                return;
            }

            Transform listSelector = mainMenuGo.transform.Find("ListSelector");
            if (listSelector == null)
            {
                listSelector = mainMenuGo.name == "ListSelector" ? mainMenuGo.transform : null;
            }
            if (listSelector == null)
            {
                return;
            }

            Transform group = listSelector.Find("Main/Group");
            if (group == null)
            {
                return;
            }

            for (int i = 0; i < group.childCount; i++)
            {
                Transform child = group.GetChild(i);
                if (!this.IsOnlineEntry(child))
                {
                    continue;
                }

                this.SelectAwayFrom(group, child);
                child.gameObject.SetActive(false);
            }

            Transform onlinePage = listSelector.Find("Online");
            if (onlinePage != null)
            {
                onlinePage.gameObject.SetActive(false);
            }
        }

        bool IsOnlineEntry(Transform child)
        {
            if (child == null)
            {
                return false;
            }

            if (child.name.IndexOf("online", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            TextMeshProUGUI label = child.GetComponent<TextMeshProUGUI>();
            if (label == null)
            {
                label = child.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            return label != null && label.text.IndexOf("online", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        void SelectAwayFrom(Transform group, Transform avoid)
        {
            ListMenu menu = group.GetComponentInParent<ListMenu>();
            if (menu == null || menu.selectedButton == null)
            {
                return;
            }

            Transform selected = menu.selectedButton.transform;
            if (selected != avoid && !selected.IsChildOf(avoid))
            {
                return;
            }

            for (int i = 0; i < group.childCount; i++)
            {
                Transform child = group.GetChild(i);
                if (child == avoid || !child.gameObject.activeInHierarchy || this.IsOnlineEntry(child))
                {
                    continue;
                }

                ListMenuButton button = child.GetComponent<ListMenuButton>();
                if (button == null)
                {
                    continue;
                }

                button.OnPointerEnter(null);
                return;
            }
        }

        internal void NormalizeMainMenuLayout(GameObject mainMenuGo = null)
        {
            if (mainMenuGo == null && MainMenuHandler.instance != null)
            {
                mainMenuGo = MainMenuHandler.instance.transform.Find("Canvas")?.gameObject;
            }
            if (mainMenuGo == null)
            {
                return;
            }

            var main = mainMenuGo.transform.Find("ListSelector/Main");
            var group = main?.Find("Group");
            if (main == null || group == null)
            {
                return;
            }

            var layout = group.GetComponent<VerticalLayoutGroup>();
            if (layout != null)
            {
                layout.spacing = 4f;
                layout.childControlHeight = false;
                layout.childForceExpandHeight = false;
            }

            main.position = new Vector3(0f, 1.7f, main.position.z);
            this.HideOnlineMenu(mainMenuGo);

            try
            {
                this.FitMainMenuLogo(group);
            }
            catch (NullReferenceException)
            {
            }

            const float optionHeight = 56f;
            for (int i = 0; i < group.childCount; i++)
            {
                try
                {
                    this.FitMainMenuOption(group.GetChild(i), optionHeight);
                }
                catch (NullReferenceException)
                {
                }
            }

            RectTransform groupRect = group.GetComponent<RectTransform>();
            if (groupRect != null)
            {
                try
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(groupRect);
                }
                catch (NullReferenceException)
                {
                }
            }

            for (int i = 0; i < group.childCount; i++)
            {
                try
                {
                    this.StyleMainMenuLabel(group.GetChild(i), optionHeight);
                }
                catch (NullReferenceException)
                {
                }
            }
        }

        void FitMainMenuLogo(Transform group)
        {
            var logo = group.Find("Rounds_Logo2_White");
            if (logo == null)
            {
                return;
            }

            var logoRect = logo.GetComponent<RectTransform>();
            if (logoRect != null)
            {
                logoRect.sizeDelta = new Vector2(logoRect.sizeDelta.x, 80f);
            }
            var logoLayout = logo.gameObject.GetOrAddComponent<LayoutElement>();
            logoLayout.minHeight = 80f;
            logoLayout.preferredHeight = 80f;

            const float titleGap = 112f;
            Transform spacer = group.Find("TitleGap");
            if (spacer == null)
            {
                GameObject spacerGo = new GameObject("TitleGap", typeof(RectTransform));
                spacerGo.transform.SetParent(group, false);
                spacer = spacerGo.transform;
            }

            spacer.SetSiblingIndex(logo.GetSiblingIndex() + 1);
            LayoutElement gapLayout = spacer.gameObject.GetOrAddComponent<LayoutElement>();
            gapLayout.minHeight = titleGap;
            gapLayout.preferredHeight = titleGap;
            gapLayout.flexibleHeight = 0f;
            gapLayout.ignoreLayout = false;
        }

        void FitMainMenuOption(Transform child, float optionHeight)
        {
            if (child == null || !child.gameObject.activeSelf)
            {
                return;
            }
            if (child.GetComponent<ListMenuButton>() == null && child.GetComponent<Button>() == null)
            {
                return;
            }

            RectTransform rect = child.GetComponent<RectTransform>();
            if (rect == null)
            {
                return;
            }

            child.localScale = Vector3.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, optionHeight);

            LayoutElement element = child.gameObject.GetOrAddComponent<LayoutElement>();
            element.minHeight = optionHeight;
            element.preferredHeight = optionHeight;
            element.flexibleHeight = 0f;
            element.ignoreLayout = false;

            ContentSizeFitter buttonFitter = child.GetComponent<ContentSizeFitter>();
            if (buttonFitter != null)
            {
                buttonFitter.enabled = false;
            }

            ListMenuButton menuButton = child.GetComponent<ListMenuButton>();
            if (menuButton != null)
            {
                menuButton.setBarHeight = optionHeight;
            }
        }

        void StyleMainMenuLabel(Transform child, float optionHeight)
        {
            if (child == null || !child.gameObject.activeSelf)
            {
                return;
            }
            if (child.GetComponent<ListMenuButton>() == null && child.GetComponent<Button>() == null)
            {
                return;
            }

            TextMeshProUGUI label = child.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label == null || label.font == null || !label.isActiveAndEnabled)
            {
                return;
            }

            RectTransform rect = child.GetComponent<RectTransform>();
            RectTransform textRect = label.rectTransform;
            if (rect == null || textRect == null)
            {
                return;
            }

            label.enableAutoSizing = false;
            label.fontSize = 50f;
            label.alignment = TextAlignmentOptions.Center;
            label.margin = Vector4.zero;
            ContentSizeFitter textFitter = label.GetComponent<ContentSizeFitter>();
            if (textFitter != null)
            {
                textFitter.enabled = false;
            }

            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.localPosition = new Vector3(0f, 0f, textRect.localPosition.z);
            textRect.sizeDelta = new Vector2(rect.sizeDelta.x, optionHeight);
        }

        internal void NormalizeMenuPageLayout(ListMenuPage page)
        {
            if (page == null)
            {
                return;
            }

            string name = page.gameObject.name;
            if (name == "Intro" || name == "CharacterSelect")
            {
                return;
            }

            const float optionHeight = 56f;
            page.barHeight = optionHeight;
            if (name == "Main")
            {
                this.NormalizeMainMenuLayout();
                return;
            }

            Transform group = page.transform.Find("Group");
            if (group == null)
            {
                return;
            }

            Transform content = page.transform.Find("Group/Grid/Scroll View/Viewport/Content");
            Transform layoutRoot = content != null ? content : group;
            VerticalLayoutGroup layout = layoutRoot.GetComponent<VerticalLayoutGroup>();
            if (layout != null)
            {
                layout.spacing = 4f;
                layout.childControlHeight = false;
                layout.childForceExpandHeight = false;
            }

            bool hasButtons = false;
            for (int i = 0; i < layoutRoot.childCount; i++)
            {
                if (layoutRoot.GetChild(i).GetComponent<Button>() != null)
                {
                    hasButtons = true;
                    break;
                }
            }

            if (hasButtons)
            {
                for (int i = 0; i < layoutRoot.childCount; i++)
                {
                    Transform child = layoutRoot.GetChild(i);
                    if (!child.gameObject.activeSelf || child.GetComponent<Button>() != null)
                    {
                        continue;
                    }

                    TextMeshProUGUI spacer = child.GetComponent<TextMeshProUGUI>();
                    if (spacer != null && string.IsNullOrWhiteSpace(spacer.text))
                    {
                        child.gameObject.SetActive(false);
                    }
                }
            }

            for (int i = 0; i < layoutRoot.childCount; i++)
            {
                this.NormalizeMenuButton(layoutRoot.GetChild(i), optionHeight);
            }

            Transform back = group.Find("Back");
            if (back != null && back.parent != layoutRoot)
            {
                this.NormalizeMenuButton(back, optionHeight);
            }

            RectTransform layoutRect = layoutRoot.GetComponent<RectTransform>();
            if (layoutRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRect);
            }
        }

        void NormalizeMenuButton(Transform child, float optionHeight)
        {
            if (child == null || !child.gameObject.activeSelf || child.GetComponent<Button>() == null)
            {
                return;
            }

            if (child.GetComponent<Toggle>() != null || child.GetComponent<Slider>() != null || child.GetComponentInChildren<TMP_InputField>(true) != null)
            {
                return;
            }

            RectTransform rect = child.GetComponent<RectTransform>();
            if (rect == null)
            {
                return;
            }

            child.localScale = Vector3.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, optionHeight);

            LayoutElement element = child.gameObject.GetOrAddComponent<LayoutElement>();
            element.minHeight = optionHeight;
            element.preferredHeight = optionHeight;
            element.flexibleHeight = 0f;
            element.ignoreLayout = false;

            ContentSizeFitter buttonFitter = child.GetComponent<ContentSizeFitter>();
            if (buttonFitter != null)
            {
                buttonFitter.enabled = false;
            }

            TextMeshProUGUI label = child.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.fontSize = 50f;
                label.enableAutoSizing = false;
                label.alignment = TextAlignmentOptions.Center;
                label.margin = Vector4.zero;
                ContentSizeFitter textFitter = label.GetComponent<ContentSizeFitter>();
                if (textFitter != null)
                {
                    textFitter.enabled = false;
                }

                RectTransform textRect = label.GetComponent<RectTransform>();
                if (textRect != null)
                {
                    textRect.anchorMin = new Vector2(0.5f, 0.5f);
                    textRect.anchorMax = new Vector2(0.5f, 0.5f);
                    textRect.pivot = new Vector2(0.5f, 0.5f);
                    textRect.anchoredPosition = Vector2.zero;
                    textRect.localPosition = new Vector3(0f, 0f, textRect.localPosition.z);
                    textRect.sizeDelta = new Vector2(rect.sizeDelta.x, optionHeight);
                }
            }

            ListMenuButton menuButton = child.GetComponent<ListMenuButton>();
            if (menuButton != null)
            {
                menuButton.setBarHeight = optionHeight;
            }
        }

        public IEnumerator InjectKeybindsWhenReady()
        {
            var uiGo = GameObject.Find("/Game/UI");
            var mainMenuGo = uiGo.transform.Find("UI_MainMenu").Find("Canvas").gameObject;

            var localGameModeGroupGo = mainMenuGo.transform.Find("ListSelector/LOCAL/Group/Grid/Scroll View/Viewport/Content")?.gameObject;
            while (localGameModeGroupGo is null)
            {
                yield return new WaitForEndOfFrame();
                localGameModeGroupGo = mainMenuGo.transform.Find("ListSelector/LOCAL/Group/Grid/Scroll View/Viewport/Content")?.gameObject;
            }
            foreach (Transform gameModeButtonGo in localGameModeGroupGo.transform)
            {
                if (gameModeButtonGo.name == "Test") { continue; } // we don't want to add keybind hints to Sandbox
                var gameModeButton = gameModeButtonGo.GetComponent<Button>();
                if (gameModeButton is null) { continue; }
                gameModeButton.onClick.AddListener(() =>
                {
                    KeybindHints.CreateLocalHints();
                });
            }

            this.CreateLocalRoundsButton(localGameModeGroupGo);
            yield break;
        }

        void CreateLocalRoundsButton(GameObject parent)
        {
            if (parent.transform.Find("RoundsToWin") != null)
            {
                return;
            }

            GameObject button = MenuHandler.CreateButton($"ROUNDS: {this.RoundsToWinGame}", parent, () =>
            {
                int rounds = this.ToggleRoundsToWinGame();
                if (this.localRoundsLabel != null)
                {
                    this.localRoundsLabel.text = $"ROUNDS: {rounds}";
                }
            });
            button.name = "RoundsToWin";
            this.localRoundsLabel = button.GetComponentInChildren<TextMeshProUGUI>();

            Transform back = parent.transform.Find("Back");
            if (back != null)
            {
                button.transform.SetSiblingIndex(back.GetSiblingIndex());
            }
        }
    }
}
