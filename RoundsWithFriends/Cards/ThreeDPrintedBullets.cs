using HarmonyLib;
using UnityEngine;
using UnboundLib.Cards;

namespace RWF.Cards
{
    public class ThreeDPrintedBullets : CustomCard
    {
        public override void SetupCard(CardInfo cardInfo, Gun gun, ApplyCardStats cardStats, CharacterStatModifiers statModifiers, Block block)
        {
            gun.ammo = 3;
            gun.damage = 0.6f;
            gun.projectileSpeed = 0.9f;
            cardInfo.allowMultiple = true;
        }

        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            PrintedBulletsPlastic effect = player.gameObject.GetComponent<PrintedBulletsPlastic>();
            if (effect == null)
            {
                effect = player.gameObject.AddComponent<PrintedBulletsPlastic>();
            }

            effect.stacks++;
        }

        public override void OnRemoveCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            PrintedBulletsPlastic effect = player.gameObject.GetComponent<PrintedBulletsPlastic>();
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
            return "3D PRINTED BULLETS";
        }

        protected override string GetDescription()
        {
            return "Grey plastic bullets";
        }

        protected override CardInfoStat[] GetStats()
        {
            return new CardInfoStat[]
            {
                new CardInfoStat
                {
                    positive = true,
                    stat = "AMMO",
                    amount = "+3",
                    simepleAmount = CardInfoStat.SimpleAmount.notAssigned
                },
                new CardInfoStat
                {
                    positive = false,
                    stat = "DMG",
                    amount = "-40%",
                    simepleAmount = CardInfoStat.SimpleAmount.notAssigned
                },
                new CardInfoStat
                {
                    positive = false,
                    stat = "Bullet speed",
                    amount = "-10%",
                    simepleAmount = CardInfoStat.SimpleAmount.notAssigned
                }
            };
        }

        protected override GameObject GetCardArt()
        {
            return BagCardFace.Art();
        }

        protected override CardInfo.Rarity GetRarity()
        {
            return CardInfo.Rarity.Common;
        }

        protected override CardThemeColor.CardThemeColorType GetTheme()
        {
            return CardThemeColor.CardThemeColorType.TechWhite;
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

    public class PrintedBulletsPlastic : MonoBehaviour
    {
        public int stacks;
    }

    [HarmonyPatch(typeof(Gun), "ApplyProjectileStats")]
    static class ThreeDPrintedBullets_Patch_ApplyProjectileStats
    {
        static readonly Color Plastic = new Color(0.55f, 0.55f, 0.55f, 1f);

        static void Postfix(Gun __instance, GameObject obj)
        {
            if (obj == null || __instance.player == null)
            {
                return;
            }

            if (__instance.player.GetComponent<PrintedBulletsPlastic>() == null)
            {
                return;
            }

            SpawnedAttack attack = obj.GetComponent<SpawnedAttack>();
            if (attack != null)
            {
                attack.SetColor(Plastic);
            }

            TrailRenderer[] trails = obj.GetComponentsInChildren<TrailRenderer>();
            for (int i = 0; i < trails.Length; i++)
            {
                trails[i].startColor = Plastic;
                trails[i].endColor = Plastic;
            }

            SpriteRenderer[] sprites = obj.GetComponentsInChildren<SpriteRenderer>();
            for (int i = 0; i < sprites.Length; i++)
            {
                sprites[i].color = Plastic;
            }

            LineRenderer[] lines = obj.GetComponentsInChildren<LineRenderer>();
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i].startColor = Plastic;
                lines[i].endColor = Plastic;
            }

            ParticleSystem[] particles = obj.GetComponentsInChildren<ParticleSystem>();
            for (int i = 0; i < particles.Length; i++)
            {
                ParticleSystem.MainModule main = particles[i].main;
                main.startColor = Plastic;
            }

            MeshRenderer[] meshes = obj.GetComponentsInChildren<MeshRenderer>();
            for (int i = 0; i < meshes.Length; i++)
            {
                meshes[i].material.color = Plastic;
            }

            ProjectileHit hit = obj.GetComponent<ProjectileHit>();
            if (hit != null)
            {
                hit.projectileColor = Plastic;
            }
        }
    }
}
