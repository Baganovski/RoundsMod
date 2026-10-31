using UnityEngine;
using UnboundLib;
using UnityEngine.UI;
using TMPro;

namespace RWF.UI
{
    public static class KeybindHints
    {
        private static GameObject _KeybindHintsHolder = null;
        public static GameObject KeybindHintHolder
        {
            get
            {
                if (KeybindHints._KeybindHintsHolder != null) { return KeybindHints._KeybindHintsHolder; }

                KeybindHints._KeybindHintsHolder = new GameObject("Keybinds");
                KeybindHints._KeybindHintsHolder.transform.SetParent(MainMenuHandler.instance.transform.Find("Canvas/"));
                KeybindHints._KeybindHintsHolder.GetOrAddComponent<RectTransform>().pivot = Vector2.zero;
                var fitter = KeybindHints._KeybindHintsHolder.GetOrAddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                var layoutgroup = KeybindHints._KeybindHintsHolder.GetOrAddComponent<VerticalLayoutGroup>();
                layoutgroup.childAlignment = TextAnchor.MiddleLeft;
                layoutgroup.spacing = 10f;
                layoutgroup.childControlWidth = false;
                layoutgroup.childControlHeight = false;
                layoutgroup.childForceExpandWidth = false;
                layoutgroup.childForceExpandHeight = false;
                KeybindHints._KeybindHintsHolder.transform.localScale = Vector3.one;
                KeybindHints._KeybindHintsHolder.transform.position = MainCam.instance.transform.GetComponent<Camera>().ScreenToWorldPoint(new Vector3(0f, 0f, 0f));
                KeybindHints._KeybindHintsHolder.transform.position += new Vector3(0f, 0f, 100f);
                KeybindHints._KeybindHintsHolder.transform.localPosition += new Vector3(10f, 10f, 0f);

                return KeybindHints._KeybindHintsHolder;
            }
        }
        private static GameObject _KeybindPrefab = null;
        public static GameObject KeybindPrefab
        {
            get
            {
                if (KeybindHints._KeybindPrefab != null) { return KeybindHints._KeybindPrefab; }

                KeybindHints._KeybindPrefab = new GameObject("Hint");
                UnityEngine.GameObject.DontDestroyOnLoad(KeybindHints._KeybindPrefab);
                KeybindHints._KeybindPrefab.transform.localScale = Vector3.one;

                KeybindHints._KeybindPrefab.GetOrAddComponent<RectTransform>();
                KeybindHints._KeybindPrefab.GetOrAddComponent<LayoutElement>();
                var sizer = KeybindHints._KeybindPrefab.GetOrAddComponent<ContentSizeFitter>();
                sizer.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                sizer.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                var text = KeybindHints._KeybindPrefab.GetOrAddComponent<TextMeshProUGUI>();
                text.fontSize = 30;
                text.font = RoundsResources.MenuFont;
                text.alignment = TextAlignmentOptions.Left;
                text.overflowMode = TextOverflowModes.Overflow;
                text.enableWordWrapping = true;
                text.color = new Color32(150, 150, 150, 255);
                text.text = "";
                text.fontStyle = FontStyles.Normal;
                text.autoSizeTextContainer = false;

                KeybindHints._KeybindPrefab.SetActive(false);

                return KeybindHints._KeybindPrefab;
            }
        }
        public static void ClearHints()
        {
            if (KeybindHints._KeybindHintsHolder == null)
            {
                return;
            }

            for (int i = KeybindHints._KeybindHintsHolder.transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = KeybindHints._KeybindHintsHolder.transform.GetChild(i).gameObject;
                child.SetActive(false);
                UnityEngine.GameObject.Destroy(child);
            }
        }

        static void RefreshHolderPosition()
        {
            if (KeybindHints._KeybindHintsHolder == null || MainCam.instance == null)
            {
                return;
            }

            Camera camera = MainCam.instance.transform.GetComponent<Camera>();
            if (camera == null)
            {
                return;
            }

            KeybindHints._KeybindHintsHolder.transform.position = camera.ScreenToWorldPoint(new Vector3(0f, 0f, 0f));
            KeybindHints._KeybindHintsHolder.transform.position += new Vector3(0f, 0f, 100f);
            KeybindHints._KeybindHintsHolder.transform.localPosition += new Vector3(10f, 10f, 0f);
        }
        public static GameObject AddHint(string action, string hint, Vector2? position = null)
        {
            GameObject hintGO = GameObject.Instantiate(KeybindHints.KeybindPrefab, KeybindHints.KeybindHintHolder.transform);
            TextMeshProUGUI hintText = hintGO.GetOrAddComponent<TextMeshProUGUI>();
            hintGO.GetOrAddComponent<DestroyIfSet>();
            hintGO.SetActive(true);
            hintGO.transform.localScale = Vector3.one;
            if (position != null) 
            { 
                hintGO.transform.position = (Vector2) position;
                hintGO.GetOrAddComponent<LayoutElement>().ignoreLayout = true;
            }
            hintText.text = $"{hint} TO {action}".ToUpper();

            return hintGO;
        }
        public static GameObject AddHint(string action, string[] hints, Vector2? position = null)
        {
            GameObject hintGO = GameObject.Instantiate(KeybindHints.KeybindPrefab, KeybindHints.KeybindHintHolder.transform);
            TextMeshProUGUI hintText = hintGO.GetOrAddComponent<TextMeshProUGUI>();
            hintGO.GetOrAddComponent<DestroyIfSet>();
            hintGO.GetOrAddComponent<CycleHints>().action = action;
            hintGO.GetOrAddComponent<CycleHints>().hints = hints;
            hintGO.SetActive(true);
            hintGO.transform.localScale = Vector3.one;
            if (position != null)
            {
                hintGO.transform.position = (Vector2) position;
                hintGO.GetOrAddComponent<LayoutElement>().ignoreLayout = true;
            }
            hintText.text = $"{hints[0]} TO {action}".ToUpper();

            return hintGO;
        }
        public static void CreateLocalHints()
        {
            KeybindHints.ClearHints();
            if (PlayerPrefs.GetInt(RWFMod.GetCustomPropertyKey("ShowKeybinds"), 1) == 0) { return; }
            _ = KeybindHints.KeybindHintHolder;
            KeybindHints.RefreshHolderPosition();
            KeybindHints.AddHint("Join/Ready", "jump");
            KeybindHints.AddHint("Select face", "Left/right");
            KeybindHints.AddHint("select team", new string[] { "[W/S]","[Dpad left/right]" });
            KeybindHints.AddRoundsChangeHint();
        }

        static void AddRoundsChangeHint()
        {
            GameObject hintGO = GameObject.Instantiate(KeybindHints.KeybindPrefab, KeybindHints.KeybindHintHolder.transform);
            hintGO.GetOrAddComponent<TextMeshProUGUI>();
            hintGO.GetOrAddComponent<DestroyIfSet>();
            hintGO.GetOrAddComponent<RoundsChangeHint>();
            hintGO.SetActive(true);
            hintGO.transform.localScale = Vector3.one;
        }
        class RoundsChangeHint : MonoBehaviour
        {
            int lastRounds = -1;

            void Update()
            {
                if (RWFMod.instance == null)
                {
                    return;
                }

                int rounds = RWFMod.instance.RoundsToWinGame;
                if (rounds == this.lastRounds)
                {
                    return;
                }

                this.lastRounds = rounds;
                TextMeshProUGUI label = this.GetComponent<TextMeshProUGUI>();
                label.text = $"[R] TO CHANGE ROUNDS (CURRENTLY {rounds} ROUNDS)".ToUpper();
                if (this.transform.parent?.GetComponent<LayoutGroup>() != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(this.transform.GetComponent<RectTransform>());
                }
            }
        }

        class DestroyIfSet : MonoBehaviour
        {
            void Start()
            {
                if (PlayerPrefs.GetInt(RWFMod.GetCustomPropertyKey("ShowKeybinds"), 1) == 0 && this.gameObject != null) { UnityEngine.GameObject.Destroy(this.gameObject); }
            }
            void Update()
            {
                if (PlayerPrefs.GetInt(RWFMod.GetCustomPropertyKey("ShowKeybinds"), 1) == 0 && this.gameObject != null) { UnityEngine.GameObject.Destroy(this.gameObject); }
            }
        }
        internal class DisableIfSet : MonoBehaviour
        {
            void Start()
            {
                if (PlayerPrefs.GetInt(RWFMod.GetCustomPropertyKey("ShowKeybinds"), 1) == 0 && this.gameObject != null) { this.gameObject.SetActive(false); }
            }
            void Update()
            {
                if (PlayerPrefs.GetInt(RWFMod.GetCustomPropertyKey("ShowKeybinds"), 1) == 0 && this.gameObject != null) { this.gameObject.SetActive(false); }
            }
        }
        internal class ControllerBasedHints : MonoBehaviour
        {
            internal string[] hints;
            internal string action;
            private const float timeToWait = 1f;
            private float time = 0f;
            void Start()
            {
                if (this.hints == null || this.action == null || this.hints.Length == 1)
                {
                    UnityEngine.GameObject.Destroy(this);
                }
            }
            void Update()
            {
                this.time -= Time.deltaTime;
                if (this.time < 0f)
                {
                    this.time = ControllerBasedHints.timeToWait;
                    int i = MenuControllerHandler.menuControl == MenuControllerHandler.MenuControl.Controller ? 1 : 0;
                    this.gameObject.GetOrAddComponent<TextMeshProUGUI>().text = $"{this.hints[i]} TO {this.action}".ToUpper();
                    if (this.gameObject?.transform?.parent?.GetComponent<LayoutGroup>() != null)
                    {
                        LayoutRebuilder.ForceRebuildLayoutImmediate(this.gameObject.GetComponent<RectTransform>());
                    }
                }
            }
        }
        class CycleHints : MonoBehaviour
        {
            internal string[] hints;
            internal string action;
            private const float timeToWait = 2f;
            private float time = timeToWait;
            private int i = 0;
            void Start()
            {
                if (this.hints == null || this.action == null || this.hints.Length == 1)
                {
                    UnityEngine.GameObject.Destroy(this);
                }
            }
            void Update()
            {
                this.time -= Time.deltaTime;
                if (this.time < 0f)
                {
                    this.time = CycleHints.timeToWait;
                    this.i = Math.mod(this.i + 1, this.hints.Length);
                    this.gameObject.GetOrAddComponent<TextMeshProUGUI>().text = $"{this.hints[this.i]} TO {this.action}".ToUpper();
                    if (this.gameObject?.transform?.parent?.GetComponent<LayoutGroup>() != null)
                    {
                        LayoutRebuilder.ForceRebuildLayoutImmediate(this.gameObject.GetComponent<RectTransform>());
                    }
                }
            }
        }
    }
}
