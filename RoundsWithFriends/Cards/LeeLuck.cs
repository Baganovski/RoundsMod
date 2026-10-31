using HarmonyLib;
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
            return "Lee Luck";
        }

        protected override string GetDescription()
        {
            return "Ignore one instance of damage";
        }

        protected override CardInfoStat[] GetStats()
        {
            return new CardInfoStat[]
            {
                new CardInfoStat
                {
                    positive = true,
                    stat = "Ignore a hit",
                    amount = "Every 10s",
                    simepleAmount = CardInfoStat.SimpleAmount.notAssigned
                }
            };
        }

        protected override GameObject GetCardArt()
        {
            return new GameObject("LeeLuckArt");
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
        public int stacks;
        float readyAt;

        public bool TryIgnore(Vector2 damage)
        {
            if (stacks <= 0 || damage.sqrMagnitude <= 0.0001f || Time.time < readyAt)
            {
                return false;
            }

            readyAt = Time.time + (LeeLuck.Interval / stacks);
            return true;
        }
    }

    public class LeeLuckMissPopup : MonoBehaviour
    {
        const float Life = 0.7f;
        float age;
        Vector3 origin;
        TextMesh text;

        public static void Spawn(Vector3 position)
        {
            GameObject popup = new GameObject("MISS");
            popup.transform.position = position;
            TextMesh label = popup.AddComponent<TextMesh>();
            label.text = "MISS";
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = 48;
            label.characterSize = 0.12f;
            label.fontStyle = FontStyle.Bold;
            label.color = new Color(0.85f, 0.95f, 1f, 1f);
            popup.GetComponent<MeshRenderer>().sortingOrder = 500;
            popup.AddComponent<LeeLuckMissPopup>();
        }

        void Start()
        {
            origin = transform.position;
            text = GetComponent<TextMesh>();
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
            transform.position = origin + Vector3.up * (t * 1.4f);
            Color color = text.color;
            color.a = 1f - Mathf.Clamp01((t - 0.45f) / 0.55f);
            text.color = color;
        }
    }

    [HarmonyPatch(typeof(HealthHandler), "DoDamage")]
    static class LeeLuck_Patch_DoDamage
    {
        static bool Prefix(HealthHandler __instance, Vector2 damage)
        {
            LeeLuckEffect effect = __instance.GetComponent<LeeLuckEffect>();
            if (effect != null && effect.TryIgnore(damage))
            {
                LeeLuckMissPopup.Spawn(__instance.transform.position);
                return false;
            }

            return true;
        }
    }
}
