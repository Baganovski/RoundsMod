using HarmonyLib;
using Photon.Pun;
using UnityEngine;
using UnboundLib.Cards;

namespace RWF.Cards
{
    public class OutOfDateBiscuits : CustomCard
    {
        public const float MineDamage = 10f;
        public const float MineLife = 10f;

        public override void SetupCard(CardInfo cardInfo, Gun gun, ApplyCardStats cardStats, CharacterStatModifiers statModifiers, Block block)
        {
            cardInfo.allowMultiple = false;
        }

        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            OutOfDateBiscuitsEffect effect = player.gameObject.GetComponent<OutOfDateBiscuitsEffect>();
            if (effect == null)
            {
                effect = player.gameObject.AddComponent<OutOfDateBiscuitsEffect>();
            }

            effect.stacks++;
        }

        public override void OnRemoveCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            OutOfDateBiscuitsEffect effect = player.gameObject.GetComponent<OutOfDateBiscuitsEffect>();
            if (effect == null)
            {
                return;
            }

            effect.stacks = Mathf.Max(0, effect.stacks - 1);
        }

        protected override string GetTitle()
        {
            return "OUT OF DATE BISCUITS";
        }

        protected override string GetDescription()
        {
            return "10 HP mines, last 10s";
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
            return CardThemeColor.CardThemeColorType.NatureBrown;
        }

        public override string GetModName()
        {
            return LeeLuck.Category;
        }

        public override bool GetEnabled()
        {
            return true;
        }
    }

    public class OutOfDateBiscuitsEffect : MonoBehaviour
    {
        public int stacks;
    }

    static class OutOfDateBiscuitsDrop
    {
        public static void TryDrop(HitInfo hit, Player owner)
        {
            OutOfDateBiscuitsEffect effect = owner != null ? owner.GetComponent<OutOfDateBiscuitsEffect>() : null;
            if (effect == null || effect.stacks <= 0)
            {
                return;
            }

            if (hit.transform == null && hit.collider == null)
            {
                return;
            }

            Transform target = hit.transform != null ? hit.transform : hit.collider.transform;
            if (target.GetComponentInParent<HealthHandler>() != null)
            {
                return;
            }

            OutOfDateBiscuitMine.Spawn(hit, owner);
        }
    }

    public class OutOfDateBiscuitMine : MonoBehaviour
    {
        const float VisualWidth = 0.78f;
        const float VisualHeight = 0.3f;
        const float NormalOffset = 0.06f;
        const float TriggerRadius = 0.27f;
        static readonly Color BiscuitColor = new Color(0.45f, 0.28f, 0.12f, 1f);
        static Sprite biscuitSprite;

        float age;
        bool spent;
        Player owner;
        Vector2 damagePoint;
        Collider2D anchorCollider;
        Transform surface;
        Vector3 localPosition;
        Quaternion localRotation;
        Rigidbody2D body;

        public static void Spawn(HitInfo hit, Player damagingPlayer)
        {
            Vector2 normal = hit.normal;
            if (normal.sqrMagnitude < 0.0001f)
            {
                normal = Vector2.up;
            }
            else
            {
                normal.Normalize();
            }

            Collider2D collider = hit.collider;
            Transform anchor = collider != null ? collider.transform : hit.transform;
            Vector2 worldPoint = hit.point + normal * NormalOffset;
            float angle = Mathf.Atan2(normal.y, normal.x) * Mathf.Rad2Deg - 90f;
            Quaternion worldRotation = Quaternion.Euler(0f, 0f, angle);

            GameObject mine = new GameObject("OutOfDateBiscuit");
            mine.transform.position = worldPoint;
            mine.transform.rotation = worldRotation;

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(mine.transform, false);
            SpriteRenderer sprite = visual.AddComponent<SpriteRenderer>();
            sprite.sprite = GetBiscuitSprite();
            sprite.color = BiscuitColor;
            sprite.sortingOrder = 8;
            visual.transform.localScale = new Vector3(VisualWidth, VisualHeight, 1f);

            CircleCollider2D trigger = mine.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = TriggerRadius;

            Rigidbody2D rigidbody = mine.AddComponent<Rigidbody2D>();
            rigidbody.isKinematic = true;
            rigidbody.simulated = true;

            OutOfDateBiscuitMine behaviour = mine.AddComponent<OutOfDateBiscuitMine>();
            behaviour.owner = damagingPlayer;
            behaviour.damagePoint = worldPoint;
            behaviour.anchorCollider = collider;
            behaviour.surface = anchor;
            behaviour.body = rigidbody;
            if (anchor != null)
            {
                behaviour.localPosition = anchor.InverseTransformPoint(worldPoint);
                behaviour.localRotation = Quaternion.Inverse(anchor.rotation) * worldRotation;
            }
        }

        void FixedUpdate()
        {
            if (!SurfaceIsAlive())
            {
                Destroy(gameObject);
                return;
            }

            Vector3 worldPosition = surface.TransformPoint(localPosition);
            Quaternion worldRotation = surface.rotation * localRotation;
            body.MovePosition(worldPosition);
            body.MoveRotation(worldRotation.eulerAngles.z);
            damagePoint = worldPosition;
        }

        bool SurfaceIsAlive()
        {
            if (anchorCollider != null)
            {
                if (!anchorCollider.enabled || !anchorCollider.gameObject.activeInHierarchy)
                {
                    return false;
                }

                surface = anchorCollider.transform;
            }

            return surface != null;
        }

        void Update()
        {
            age += Time.deltaTime;
            if (age >= OutOfDateBiscuits.MineLife)
            {
                Destroy(gameObject);
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            TryHarm(other);
        }

        void OnTriggerStay2D(Collider2D other)
        {
            TryHarm(other);
        }

        void TryHarm(Collider2D other)
        {
            if (spent || other == null)
            {
                return;
            }

            HealthHandler health = other.GetComponentInParent<HealthHandler>();
            if (health == null)
            {
                return;
            }

            CharacterData data = health.GetComponent<CharacterData>();
            if (data == null || data.dead || !data.isPlaying)
            {
                return;
            }

            if (RWFMod.instance != null && RWFMod.instance.IsCeaseFire)
            {
                return;
            }

            if (data.block != null && data.block.IsBlocking())
            {
                return;
            }

            PhotonView view = health.GetComponent<PhotonView>();
            if (view != null && !view.IsMine)
            {
                return;
            }

            spent = true;
            health.DoDamage(
                Vector2.up * OutOfDateBiscuits.MineDamage,
                damagePoint,
                BiscuitColor,
                null,
                owner,
                true,
                true,
                false);
            Destroy(gameObject);
        }

        static Sprite GetBiscuitSprite()
        {
            if (biscuitSprite != null)
            {
                return biscuitSprite;
            }

            const int width = 64;
            const int height = 24;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            float centerX = (width - 1) * 0.5f;
            float centerY = (height - 1) * 0.5f;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float nx = (x - centerX) / centerX;
                    float ny = (y - centerY) / centerY;
                    float ellipse = nx * nx + ny * ny * 1.15f;
                    float alpha = ellipse <= 1f ? 1f : 0f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            biscuitSprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 64f);
            return biscuitSprite;
        }
    }

    [HarmonyPatch(typeof(Gun), "ApplyProjectileStats")]
    static class OutOfDateBiscuits_Patch_ApplyProjectileStats
    {
        static void Postfix(Gun __instance, GameObject obj)
        {
            if (obj == null || __instance.player == null)
            {
                return;
            }

            OutOfDateBiscuitsEffect effect = __instance.player.GetComponent<OutOfDateBiscuitsEffect>();
            if (effect == null || effect.stacks <= 0)
            {
                return;
            }

            ProjectileHit projectile = obj.GetComponent<ProjectileHit>();
            if (projectile == null)
            {
                return;
            }

            Player owner = __instance.player;
            projectile.AddHitActionWithData(hit => OutOfDateBiscuitsDrop.TryDrop(hit, owner));
        }
    }
}
