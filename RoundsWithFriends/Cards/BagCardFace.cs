using RWF.UI;
using UnityEngine;

namespace RWF.Cards
{
    public class BagCardFace : MonoBehaviour
    {
        const float Fit = 0.85f;
        const int Pixel = 5;
        const int FitAttempts = 5;

        static GameObject art;
        int attempts;
        bool fitted;

        public static GameObject Art()
        {
            if (BagCardFace.art != null)
            {
                return BagCardFace.art;
            }

            GameObject root = new GameObject("BagCardArt");
            Object.DontDestroyOnLoad(root);
            RectTransform rect = root.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            PixelStamp.Build(root.transform, BagCardFace.Pixel);
            root.AddComponent<BagCardFace>();
            BagCardFace.art = root;
            return root;
        }

        void OnEnable()
        {
            this.attempts = 0;
            this.fitted = false;
            this.FitTitle();
        }

        void OnTransformParentChanged()
        {
            this.attempts = 0;
            this.fitted = false;
            this.FitTitle();
        }

        void Update()
        {
            if (!this.fitted)
            {
                this.FitTitle();
            }
        }

        void FitTitle()
        {
            if (this.fitted)
            {
                return;
            }

            RectTransform self = transform as RectTransform;
            RectTransform parentRect = transform.parent as RectTransform;
            if (self == null || parentRect == null)
            {
                this.fitted = true;
                return;
            }

            self.anchorMin = Vector2.zero;
            self.anchorMax = Vector2.one;
            self.offsetMin = Vector2.zero;
            self.offsetMax = Vector2.zero;
            self.pivot = new Vector2(0.5f, 0.5f);
            self.anchoredPosition = Vector2.zero;
            self.localScale = Vector3.one;
            self.localRotation = Quaternion.identity;

            RectTransform title = transform.Find("PixelTitle") as RectTransform;
            if (title == null || title.sizeDelta.x < 1f || title.sizeDelta.y < 1f)
            {
                this.fitted = true;
                return;
            }

            float windowWidth = parentRect.rect.width;
            float windowHeight = parentRect.rect.height;
            if (windowWidth < 1f || windowHeight < 1f)
            {
                this.attempts++;
                if (this.attempts >= BagCardFace.FitAttempts)
                {
                    this.fitted = true;
                }

                return;
            }

            float scale = (windowWidth * BagCardFace.Fit) / title.sizeDelta.x;
            float heightScale = (windowHeight * BagCardFace.Fit) / title.sizeDelta.y;
            if (scale > heightScale)
            {
                scale = heightScale;
            }

            title.anchorMin = new Vector2(0.5f, 0.5f);
            title.anchorMax = new Vector2(0.5f, 0.5f);
            title.pivot = new Vector2(0.5f, 0.5f);
            title.anchoredPosition = Vector2.zero;
            title.localScale = new Vector3(scale, scale, 1f);
            this.fitted = true;
        }
    }
}
