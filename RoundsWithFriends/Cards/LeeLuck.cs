using System.Collections.Generic;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;
using UnboundLib.Cards;

namespace RWF.Cards
{
    public class LeeLuck : CustomCard
    {
        public const string Category = "Bag's Mod";
        public const float Interval = 10f;

        public override void SetupCard(CardInfo cardInfo, Gun gun, ApplyCardStats cardStats, CharacterStatModifiers statModifiers, Block block)
        {
            cardInfo.allowMultiple = false;
        }

        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            LeeLuckEffect effect = player.gameObject.GetComponent<LeeLuckEffect>();
            if (effect == null)
            {
                effect = player.gameObject.AddComponent<LeeLuckEffect>();
            }

            effect.stacks++;
        }

        public override void OnRemoveCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            LeeLuckEffect effect = player.gameObject.GetComponent<LeeLuckEffect>();
            if (effect == null)
            {
                return;
            }

            effect.stacks--;
            if (effect.stacks <= 0)
            {
                Object.Destroy(effect);
            }
        }

        protected override string GetTitle()
        {
            return "LEE LUCK";
        }

        protected override string GetDescription()
        {
            return "Ignore a hit per 10s";
        }

        protected override CardInfoStat[] GetStats()
        {
            return new CardInfoStat[0];
        }

        protected override GameObject GetCardArt()
        {
            return BagCardFace.Art();
        }

        protected override CardInfo.Rarity GetRarity()
        {
            return CardInfo.Rarity.Uncommon;
        }

        protected override CardThemeColor.CardThemeColorType GetTheme()
        {
            return CardThemeColor.CardThemeColorType.DefensiveBlue;
        }

        public override string GetModName()
        {
            return Category;
        }

        public override bool GetEnabled()
        {
            return true;
        }
    }

    public class LeeLuckEffect : MonoBehaviour
    {
        const float GlowSize = 2.3f;
        public int stacks;
        float readyAt;
        SpriteRenderer glow;
        CharacterData character;
        bool sorted;
        static Sprite glowSprite;
        static Material glowMaterial;

        public bool TryIgnore(Vector2 damage)
        {
            if (stacks <= 0 || damage.sqrMagnitude <= 0.0001f || Time.time < readyAt)
            {
                return false;
            }

            readyAt = Time.time + (LeeLuck.Interval / stacks);
            sorted = false;
            return true;
        }

        void Update()
        {
            bool show = stacks > 0 && Time.time >= readyAt;
            if (character == null)
            {
                character = GetComponent<CharacterData>();
            }

            if (character != null && character.dead)
            {
                show = false;
            }

            if (!show)
            {
                if (glow != null)
                {
                    glow.enabled = false;
                }

                return;
            }

            EnsureGlow();
            glow.enabled = true;
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 3.2f);
            Color color = new Color(0.25f, 1f, 0.35f, Mathf.Lerp(0.28f, 0.55f, pulse));
            glow.color = color;
            float scale = Mathf.Lerp(0.88f, 1.06f, pulse);
            glow.transform.localScale = new Vector3(scale, scale, 1f);
            if (!sorted)
            {
                MatchSorting();
                sorted = true;
            }
        }

        void OnDestroy()
        {
            if (glow != null)
            {
                Destroy(glow.gameObject);
            }
        }

        void EnsureGlow()
        {
            if (glow != null)
            {
                return;
            }

            GameObject child = new GameObject("LeeLuckGlow");
            child.transform.SetParent(transform, false);
            child.layer = gameObject.layer;
            glow = child.AddComponent<SpriteRenderer>();
            glow.sprite = GetGlowSprite();
            glow.material = GetGlowMaterial();
            glow.color = new Color(0.25f, 1f, 0.35f, 0.4f);
            glow.transform.localScale = Vector3.one;
            MatchSorting();
        }

        void MatchSorting()
        {
            if (glow == null)
            {
                return;
            }

            glow.gameObject.layer = gameObject.layer;
            SpriteRenderer[] sprites = GetComponentsInChildren<SpriteRenderer>();
            int lowest = int.MaxValue;
            bool found = false;
            for (int i = 0; i < sprites.Length; i++)
            {
                SpriteRenderer sprite = sprites[i];
                if (sprite == null || sprite == glow)
                {
                    continue;
                }

                if (!found || sprite.sortingOrder < lowest)
                {
                    glow.sortingLayerID = sprite.sortingLayerID;
                    lowest = sprite.sortingOrder;
                    found = true;
                }
            }

            if (found)
            {
                glow.sortingOrder = lowest - 1;
            }
        }

        static Sprite GetGlowSprite()
        {
            if (glowSprite != null)
            {
                return glowSprite;
            }

            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            float center = (size - 1) * 0.5f;
            float radius = center;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - center) / radius;
                    float dy = (y - center) / radius;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(1f - dist);
                    alpha = alpha * alpha;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            glowSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size / GlowSize);
            return glowSprite;
        }

        static Material GetGlowMaterial()
        {
            if (glowMaterial != null)
            {
                return glowMaterial;
            }

            Shader shader = Shader.Find("Particles/Additive");
            if (shader == null)
            {
                shader = Shader.Find("Legacy Shaders/Particles/Additive");
            }

            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            glowMaterial = new Material(shader);
            return glowMaterial;
        }
    }

    public class LeeLuckMissPopup : MonoBehaviour
    {
        const float Life = 1.4f;
        const float OutlineOffset = 0.05625f;
        static readonly Vector2[] RiseDirections = new Vector2[]
        {
            Vector2.up,
            new Vector2(-0.35f, 1f).normalized,
            new Vector2(0.35f, 1f).normalized,
            new Vector2(-0.85f, 1f).normalized,
            new Vector2(0.85f, 1f).normalized
        };
        static readonly Vector2[] OutlineSteps = new Vector2[]
        {
            Vector2.right,
            Vector2.left,
            Vector2.up,
            Vector2.down,
            new Vector2(1f, 1f).normalized,
            new Vector2(1f, -1f).normalized,
            new Vector2(-1f, 1f).normalized,
            new Vector2(-1f, -1f).normalized
        };
        float age;
        Vector3 origin;
        Vector3 drift = Vector3.up;
        TextMesh[] labels;

        public static void Spawn(Vector3 position)
        {
            Spawn(position, "MISS", new Color(0.85f, 0.95f, 1f, 1f), false, 0.12f);
        }

        public static void Spawn(Vector3 position, string message, Color color, bool outline, float characterSize)
        {
            Vector2 direction = RiseDirections[Random.Range(0, RiseDirections.Length)];
            GameObject popup = new GameObject(message);
            popup.transform.position = position;
            AddLabel(popup, message, color, 501, characterSize);
            if (outline)
            {
                for (int i = 0; i < OutlineSteps.Length; i++)
                {
                    GameObject edge = new GameObject("Outline");
                    edge.transform.SetParent(popup.transform, false);
                    edge.transform.localPosition = (Vector3)(OutlineSteps[i] * OutlineOffset);
                    AddLabel(edge, message, Color.white, 500, characterSize);
                }
            }

            LeeLuckMissPopup motion = popup.AddComponent<LeeLuckMissPopup>();
            motion.drift = new Vector3(direction.x, direction.y, 0f);
        }

        static void AddLabel(GameObject host, string message, Color color, int sortingOrder, float characterSize)
        {
            TextMesh label = host.AddComponent<TextMesh>();
            label.text = message;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = Mathf.RoundToInt(48f * characterSize / 0.12f);
            label.characterSize = characterSize;
            label.fontStyle = FontStyle.Bold;
            label.color = color;
            host.GetComponent<MeshRenderer>().sortingOrder = sortingOrder;
        }

        void Start()
        {
            origin = transform.position;
            labels = GetComponentsInChildren<TextMesh>(true);
            transform.localScale = Vector3.one * 0.25f;
        }

        void Update()
        {
            age += Time.deltaTime;
            float t = age / Life;
            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            float pop = t < 0.18f ? t / 0.18f : 1f;
            transform.localScale = Vector3.one * Mathf.Lerp(0.25f, 1f, pop);
            transform.position = origin + drift * (t * 1.4f);
            float alpha = 1f - Mathf.Clamp01((t - 0.45f) / 0.55f);
            for (int i = 0; i < labels.Length; i++)
            {
                Color color = labels[i].color;
                color.a = alpha;
                labels[i].color = color;
            }
        }
    }

    [HarmonyPatch(typeof(ProjectileHit), "RPCA_DoHit")]
    static class LeeLuck_Patch_DoHit
    {
        static bool Prefix(ProjectileHit __instance, int viewID, bool wasBlocked)
        {
            if (wasBlocked && !__instance.unblockable)
            {
                return true;
            }

            HealthHandler health = FindHealth(viewID);
            if (health == null || !CanSpend(health, __instance.unblockable) || !CanHurt(__instance))
            {
                return true;
            }

            Player owner = __instance.ownPlayer;
            if (owner == null)
            {
                return true;
            }

            LeeLuckEffect effect = health.GetComponent<LeeLuckEffect>();
            if (effect == null || !effect.TryIgnore(IgnoreDamage(__instance)))
            {
                return true;
            }

            Hold(health, __instance);
            LeeLuckMissPopup.Spawn(health.transform.position);
            return false;
        }

        static bool CanSpend(HealthHandler health, bool unblockable)
        {
            CharacterData data = health.GetComponent<CharacterData>();
            if (data == null || data.dead || !data.isPlaying)
            {
                return false;
            }

            if (RWFMod.instance != null && RWFMod.instance.IsCeaseFire)
            {
                return false;
            }

            if (!unblockable && data.block != null && data.block.IsBlocking())
            {
                return false;
            }

            return true;
        }

        static bool CanHurt(ProjectileHit hit)
        {
            if (hit.damage > 0.0001f || hit.percentageDamage > 0.0001f)
            {
                return true;
            }

            return hit.GetComponentInChildren<RayHitPoison>() != null;
        }

        static Vector2 IgnoreDamage(ProjectileHit hit)
        {
            float amount = hit.damage;
            if (amount <= 0.0001f)
            {
                amount = 1f;
            }

            return Vector2.up * amount;
        }

        static HealthHandler FindHealth(int viewID)
        {
            if (viewID == -1)
            {
                return null;
            }

            PhotonView view = PhotonNetwork.GetPhotonView(viewID);
            if (view == null)
            {
                return null;
            }

            return view.GetComponent<HealthHandler>();
        }

        static void Hold(HealthHandler health, ProjectileHit hit)
        {
            List<HealthHandler> held = AccessTools.Field(typeof(ProjectileHit), "playersHit").GetValue(hit) as List<HealthHandler>;
            if (held != null && held.Contains(health))
            {
                return;
            }

            hit.AddPlayerToHeld(health);
        }
    }
}
