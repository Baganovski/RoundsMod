using HarmonyLib;
using UnityEngine;
using UnboundLib.Cards;

namespace RWF.Cards
{
    public class LeeLuck : CustomCard
    {
        public const string Category = "Bag's Mod";
        public const float Interval = 5f;

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
                    amount = "Every 5s",
                    simepleAmount = CardInfoStat.SimpleAmount.notAssigned
                }
            };
        }

        protected override GameObject GetCardArt()
        {
            return null;
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

    [HarmonyPatch(typeof(HealthHandler), "DoDamage")]
    static class LeeLuck_Patch_DoDamage
    {
        static bool Prefix(HealthHandler __instance, Vector2 damage)
        {
            LeeLuckEffect effect = __instance.GetComponent<LeeLuckEffect>();
            if (effect != null && effect.TryIgnore(damage))
            {
                return false;
            }

            return true;
        }
    }
}
