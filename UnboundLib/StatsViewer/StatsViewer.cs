using InControl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnboundLib.Patches;
using UnityEngine;
using UnityEngine.UI;

namespace UnboundLib.StatsViewer
{
    static class StatsViewer
    {
        public static GameObject GunStatsPanel;
        public static GameObject GunStatsPanelCollapsed;
        public static GameObject OtherStatsPanel;
        public static GameObject OtherStatsPanelCollapsed;
        public static Player player;
        public class StatsArgs
        {
            public readonly GunAmmo playerGunAmmo;
            public static readonly GunAmmo defaultGunAmmo;
            public readonly Gun playerGun;
            public static readonly Gun defaultGun;
            public readonly Gun cardGun;
            public readonly float? projectileNum;
            public readonly float? chargeProjectileNum;
            public readonly CharacterStatModifiers playerCharacterStatModifiers;
            public readonly CharacterStatModifiers cardCharacterStatModifiers;
            public static readonly CharacterStatModifiers defaultCharacterStatModifiers;
            public readonly CharacterData playerCharacterData;
            public static readonly CharacterData defaultCharacterData;
            public readonly Gravity playerGravity;
            public static readonly Gravity defaultGravity;
            public readonly Block playerBlock;
            public static readonly Block defaultBlock;
            public readonly HealthHandler playerHealthHandler;
            public static readonly HealthHandler defaultHealthHandler;
            public readonly Block cardBlock;
            static StatsArgs() {
                GameObject gunGO = new GameObject("Stats Gun");
                GameObject.DontDestroyOnLoad(gunGO);
                defaultGun = gunGO.AddComponent<Gun>();
                GameObject ammoGo = new GameObject("Stats Ammo");
                ammoGo.transform.SetParent(gunGO.transform, false);
                defaultGunAmmo = ammoGo.AddComponent<GunAmmo>();
                //Unbound.Instance.ExecuteAfterSeconds(0.1f, () =>
                //{
                //    defaultGun.InvokeMethod("ResetStats");
                //    gunGO.SetActive(false);
                //});

                defaultGun.InvokeMethod("Start");
                GunAmmo_Patch_ReDrawTotalBullets.cancel = true;
                defaultGun.InvokeMethod("ResetStats");
                defaultGunAmmo.reloadTime = 2f;
                defaultGun.defaultCooldown = 0.3f;
                gunGO.SetActive(false);

                defaultCharacterStatModifiers = new CharacterStatModifiers();
                defaultCharacterStatModifiers.objectsAddedToPlayer = new List<GameObject>();
                defaultCharacterData = new CharacterData();
                defaultCharacterStatModifiers.SetFieldValue("data", defaultCharacterData);
                CharacterData_Patch_set_MaxHealth.justSet = true;
                CharacterStatModifiers_Patch_WasUpdated.cancel = true;
                CharacterStatModifiers_Patch_ConfigureMassAndSize.cancel = true;
                defaultCharacterStatModifiers.InvokeMethod("ResetStats");

                defaultGravity = new Gravity();
                defaultBlock = new Block();
                defaultBlock.InvokeMethod("ResetStats");
                defaultHealthHandler = new HealthHandler();
            }
            public StatsArgs(GunAmmo playerGunAmmo, Gun playerGun, CharacterStatModifiers playerCharacterStatModifiers, CharacterData playerCharacterData, Gravity playerGravity, Block playerBlock, HealthHandler playerHealthHandler, Gun cardGun, CharacterStatModifiers cardCharacterStatModifiers, Block cardBlock)
            {
                this.playerGunAmmo = playerGunAmmo;
                this.playerGun = playerGun;
                this.playerCharacterStatModifiers = playerCharacterStatModifiers;
                this.playerCharacterData = playerCharacterData;
                this.playerGravity = playerGravity;
                this.playerBlock = playerBlock;
                this.playerHealthHandler = playerHealthHandler;
                this.cardGun = cardGun;
                this.cardCharacterStatModifiers = cardCharacterStatModifiers;
                this.cardBlock = cardBlock;
                float num = 1f;
                if (cardGun)
                {
                    if (cardGun.numberOfProjectiles != 0 && playerGun.numberOfProjectiles != 1)
                    {
                        num = (float) cardGun.numberOfProjectiles / ((float) cardGun.numberOfProjectiles + (float) playerGun.numberOfProjectiles);
                    }
                }
                projectileNum = cardGun ? num : (float?) null;
                float num2 = 1f;
                if (cardGun)
                {
                    if (cardGun.chargeNumberOfProjectilesTo != 0)
                    {
                        num2 = cardGun.chargeNumberOfProjectilesTo / (cardGun.chargeNumberOfProjectilesTo + playerGun.chargeNumberOfProjectilesTo);
                    }
                }
                chargeProjectileNum = cardGun ? num2 : (float?) null;
            }
        }
        public enum StatsBetter
        {
            None,
            More,
            Less,
            True,
            False,
            ZeroOrMore
        }
        public enum ChangeMode
        {
            Set,
            Add,
            Mult
        }

        public static Dictionary<string, Func<StatsArgs, string>> GunStats = new Dictionary<string, Func<StatsArgs, string>>()
        {
            { "Ammo Regen", (args) => ProcessStat(args.cardGun?.ammoReg, args.playerGunAmmo.ammoReg, StatsArgs.defaultGunAmmo.ammoReg, StatsBetter.More, ChangeMode.Add) },
            { "Max Ammo", (args) => ProcessStat(args.cardGun ? (Mathf.Clamp(args.playerGunAmmo.maxAmmo + args.cardGun.ammo, 1, 90)) : (int?)null, args.playerGunAmmo.maxAmmo, StatsArgs.defaultGunAmmo.maxAmmo, StatsBetter.More) },
            { "Reload Time Mult", (args) => ProcessStat(args.cardGun?.reloadTime, args.playerGunAmmo.reloadTimeMultiplier, StatsArgs.defaultGunAmmo.reloadTimeMultiplier, StatsBetter.Less, ChangeMode.Mult) },
            { "Reload Time Add", (args) => ProcessStat(args.cardGun?.reloadTimeAdd, args.playerGunAmmo.reloadTimeAdd, StatsArgs.defaultGunAmmo.reloadTimeAdd, StatsBetter.Less, ChangeMode.Add) },
            { "Reload Time", (args) => ProcessStat(null, args.playerGunAmmo.reloadTime, StatsArgs.defaultGunAmmo.reloadTime, StatsBetter.Less, ChangeMode.Add) },
            { "Calculated Reload Time", (args) => ProcessStat(args.cardGun ? ((args.playerGunAmmo.reloadTime + (args.playerGunAmmo.reloadTimeAdd + args.cardGun.reloadTimeAdd)) * (args.playerGunAmmo.reloadTimeMultiplier * args.cardGun.reloadTime)) : (float?)null, (args.playerGunAmmo.reloadTime + args.playerGunAmmo.reloadTimeAdd) * args.playerGunAmmo.reloadTimeMultiplier, (StatsArgs.defaultGunAmmo.reloadTime + StatsArgs.defaultGunAmmo.reloadTimeAdd) * StatsArgs.defaultGunAmmo.reloadTimeMultiplier, StatsBetter.Less, ChangeMode.Set) },
            { "Default Cooldown", (args) => ProcessStat(args.cardGun && args.cardGun.lockGunToDefault ? args.cardGun.forceSpecificAttackSpeed : (float?)null, args.playerGun.defaultCooldown, StatsArgs.defaultGun.defaultCooldown, StatsBetter.Less, ChangeMode.Set) },
            { "Lock Gun To Default", (args) => ProcessStat(args.cardGun?.lockGunToDefault, args.playerGun.lockGunToDefault, StatsArgs.defaultGun.lockGunToDefault, StatsBetter.False) },
            { "Unblockable", (args) => ProcessStat(args.cardGun?.unblockable, args.playerGun.unblockable, StatsArgs.defaultGun.unblockable, StatsBetter.True) },
            { "Ignore Walls", (args) => ProcessStat(args.cardGun?.ignoreWalls, args.playerGun.ignoreWalls, StatsArgs.defaultGun.ignoreWalls, StatsBetter.True) },
            { "Number Of Projectiles", (args) => ProcessStat(args.cardGun?.numberOfProjectiles, args.playerGun.numberOfProjectiles, StatsArgs.defaultGun.numberOfProjectiles, StatsBetter.More, ChangeMode.Add) },
            { "Damage", (args) => ProcessStat(args.projectileNum.HasValue ? Mathf.Max(args.playerGun.damage * (1f - args.projectileNum.Value * (1f - args.cardGun.damage)), 0.25f) : (float?)null, args.playerGun.damage, StatsArgs.defaultGun.damage, StatsBetter.More, ChangeMode.Set) },
            { "Bullet Damage Mult", (args) => ProcessStat(args.cardGun?.bulletDamageMultiplier, args.playerGun.bulletDamageMultiplier, StatsArgs.defaultGun.bulletDamageMultiplier, StatsBetter.More, ChangeMode.Mult) },
            { "Calculated Damage", (args) => ProcessStat(args.projectileNum.HasValue ? Mathf.Max(args.playerGun.damage * (1f - args.projectileNum.Value * (1f - args.cardGun.damage)), 0.25f) * args.cardGun.bulletDamageMultiplier : (float?)null, args.playerGun.damage * args.playerGun.bulletDamageMultiplier, args.playerGun.damage * args.playerGun.bulletDamageMultiplier, StatsBetter.More, ChangeMode.Set) },
            { "Percentage Damage (Of Target Health)", (args) => ProcessStat(args.cardGun?.percentageDamage, args.playerGun.percentageDamage, StatsArgs.defaultGun.percentageDamage, StatsBetter.More, ChangeMode.Add) },
            { "Size", (args) => ProcessStat(args.cardGun?.size, args.playerGun.size, StatsArgs.defaultGun.size, StatsBetter.More, ChangeMode.Add) },
            { "Knockback", (args) => ProcessStat(args.projectileNum.HasValue ? 1f - args.projectileNum.Value * (1f - args.cardGun.knockback) : (float?)null, args.playerGun.knockback, StatsArgs.defaultGun.knockback, StatsBetter.More, ChangeMode.Mult) },
            { "Projectile Speed", (args) => ProcessStat(args.cardGun?.projectileSize, args.playerGun.projectileSize, StatsArgs.defaultGun.projectileSize, StatsBetter.More, ChangeMode.Mult) },
            { "Projectile Sim Speed", (args) => ProcessStat(args.cardGun?.projectielSimulatonSpeed, args.playerGun.projectielSimulatonSpeed, StatsArgs.defaultGun.projectielSimulatonSpeed, StatsBetter.More, ChangeMode.Mult) },
            { "Gravity", (args) => ProcessStat(args.cardGun?.gravity, args.playerGun.gravity, StatsArgs.defaultGun.gravity, StatsBetter.None, ChangeMode.Mult) },
            { "Attack Speed", (args) => ProcessStat(args.cardGun?.attackSpeed, args.playerGun.attackSpeed, StatsArgs.defaultGun.attackSpeed, StatsBetter.Less, ChangeMode.Mult) },
            //{ "Body Recoil (Unused)", (args) => ProcessStat(args.cardGun?.recoilMuiltiplier, args.playerGun.bodyRecoil, StatsArgs.defaultGun.bodyRecoil, StatsBetter.Less, ChangeMode.Mult) },
            { "Speed Mult On Bounce", (args) => ProcessStat(args.cardGun?.speedMOnBounce, args.playerGun.speedMOnBounce, StatsArgs.defaultGun.speedMOnBounce, StatsBetter.More, ChangeMode.Mult) },
            { "Damage Mult On Bounce", (args) => ProcessStat(args.cardGun?.dmgMOnBounce, args.playerGun.dmgMOnBounce, StatsArgs.defaultGun.dmgMOnBounce, StatsBetter.More, ChangeMode.Mult) },
            { "Spread", (args) => ProcessStat(args.cardGun?.spread, args.playerGun.spread, StatsArgs.defaultGun.spread, StatsBetter.None, ChangeMode.Add) },
            { "Spread Mult", (args) => ProcessStat(args.cardGun?.multiplySpread, args.playerGun.multiplySpread, StatsArgs.defaultGun.multiplySpread, StatsBetter.None, ChangeMode.Mult) },
            { "Calculated Spread", (args) => ProcessStat(args.cardGun ? (args.playerGun.spread + args.cardGun.spread) * (args.playerGun.multiplySpread * args.cardGun.multiplySpread) : (float?)null, args.playerGun.spread * args.playerGun.multiplySpread, StatsArgs.defaultGun.spread * StatsArgs.defaultGun.multiplySpread, StatsBetter.None, ChangeMode.Set) },
            //{ "Even Spread (Unused)", (args) => ProcessStat(args.cardGun?.evenSpread, args.playerGun.evenSpread, StatsArgs.defaultGun.evenSpread, StatsBetter.None, ChangeMode.Add) },
            { "Drag", (args) => ProcessStat(args.cardGun?.drag, args.playerGun.drag, StatsArgs.defaultGun.drag, StatsBetter.Less, ChangeMode.Add) },
            { "Drag Min Speed", (args) => ProcessStat(args.cardGun?.dragMinSpeed, args.playerGun.dragMinSpeed, StatsArgs.defaultGun.dragMinSpeed, StatsBetter.More, ChangeMode.Add) },
            { "Time Between Bullets", (args) => ProcessStat(args.cardGun?.timeBetweenBullets, args.playerGun.timeBetweenBullets, StatsArgs.defaultGun.timeBetweenBullets, StatsBetter.Less, ChangeMode.Add) },
            { "Reflects", (args) => ProcessStat(args.cardGun?.reflects, args.playerGun.reflects, StatsArgs.defaultGun.reflects, StatsBetter.More, ChangeMode.Add) },
            //{ "Smart Bounce (Unused)", (args) => ProcessStat(args.cardGun?.smartBounce, args.playerGun.smartBounce, StatsArgs.defaultGun.smartBounce, StatsBetter.More, ChangeMode.Add) },
            //{ "Bullet Portal (Unused)", (args) => ProcessStat(args.cardGun?.bulletPortal, args.playerGun.bulletPortal, StatsArgs.defaultGun.bulletPortal, StatsBetter.None, ChangeMode.Add) },
            //{ "Random Bounces (Unused)", (args) => ProcessStat(args.cardGun?.randomBounces, args.playerGun.randomBounces, StatsArgs.defaultGun.randomBounces, StatsBetter.None, ChangeMode.Add) },
            { "Bursts", (args) => ProcessStat(args.cardGun?.bursts, args.playerGun.bursts, StatsArgs.defaultGun.bursts, StatsBetter.More, ChangeMode.Add) },
            { "Slow", (args) => ProcessStat(args.cardGun?.slow, args.playerGun.slow, StatsArgs.defaultGun.slow, StatsBetter.Less, ChangeMode.Add) },
            //{ "Overheat Mult (Unused)", (args) => ProcessStat(args.cardGun?.overheatMultiplier, args.playerGun.overheatMultiplier, StatsArgs.defaultGun.overheatMultiplier, StatsBetter.None, ChangeMode.Add) },
            //{ "Projectile Size (Unused)", (args) => ProcessStat(args.cardGun?.projectileSize, args.playerGun.projectileSize, StatsArgs.defaultGun.projectileSize, StatsBetter.None, ChangeMode.Add) },
            { "Damage After Distance Mult", (args) => ProcessStat(args.cardGun?.damageAfterDistanceMultiplier, args.playerGun.damageAfterDistanceMultiplier, StatsArgs.defaultGun.damageAfterDistanceMultiplier, StatsBetter.More, ChangeMode.Mult) },
            //{ "Time To Reach Full Movement Mult (Unused)", (args) => ProcessStat(args.cardGun?.timeToReachFullMovementMultiplier, args.playerGun.timeToReachFullMovementMultiplier, StatsArgs.defaultGun.timeToReachFullMovementMultiplier, StatsBetter.None, ChangeMode.Mult) },
            { "cos", (args) => ProcessStat(args.cardGun?.cos, args.playerGun.cos, StatsArgs.defaultGun.cos, StatsBetter.None, ChangeMode.Add) },
            { "Don't Allow Auto Fire", (args) => ProcessStat(args.cardGun?.dontAllowAutoFire, args.playerGun.dontAllowAutoFire, StatsArgs.defaultGun.dontAllowAutoFire, StatsBetter.False) },
            { "Destroy Bullet After", (args) => ProcessStat((args.cardGun && args.cardGun.destroyBulletAfter != 0f) ? args.cardGun.destroyBulletAfter : (float?) null, args.playerGun.destroyBulletAfter, StatsArgs.defaultGun.destroyBulletAfter, StatsBetter.ZeroOrMore, ChangeMode.Set) },
            { "Use Charge", (args) => ProcessStat((args.cardGun && args.cardGun.useCharge) ? args.cardGun.useCharge : (bool?)null, args.playerGun.useCharge, StatsArgs.defaultGun.useCharge, StatsBetter.True) },
            { "Charge Number Of Projectiles To", (args) => ProcessStat(args.cardGun?.chargeNumberOfProjectilesTo, args.playerGun.chargeNumberOfProjectilesTo, StatsArgs.defaultGun.chargeNumberOfProjectilesTo, StatsBetter.More, ChangeMode.Add) },
            { "Charge Damage Mult", (args) => ProcessStat(args.chargeProjectileNum.HasValue ? 1f - args.chargeProjectileNum.Value * (1f - args.playerGun.chargeDamageMultiplier) : (float?)null, args.playerGun.chargeDamageMultiplier, StatsArgs.defaultGun.chargeDamageMultiplier, StatsBetter.More, ChangeMode.Mult) },
            { "Charge Spread To", (args) => ProcessStat(args.cardGun?.chargeSpreadTo, args.playerGun.chargeSpreadTo, StatsArgs.defaultGun.chargeSpreadTo, StatsBetter.None, ChangeMode.Add) },
            //{ "Charge Even Spread To (Unused)", (args) => ProcessStat(args.cardGun?.chargeEvenSpreadTo, args.playerGun.chargeEvenSpreadTo, StatsArgs.defaultGun.chargeEvenSpreadTo, StatsBetter.None, ChangeMode.Add) },
            //{ "Charge Speed To (Unused)", (args) => ProcessStat(args.cardGun?.chargeSpeedTo, args.playerGun.chargeSpeedTo, StatsArgs.defaultGun.chargeSpeedTo, StatsBetter.More, ChangeMode.Add) },
            { "Charge Recoil To", (args) => ProcessStat(args.cardGun?.chargeRecoilTo, args.playerGun.chargeRecoilTo, StatsArgs.defaultGun.chargeRecoilTo, StatsBetter.Less, ChangeMode.Add) },
            { "Projectile Color", (args) => ProcessStat(args.cardGun ? projectileColorMath(args.cardGun.projectileColor, args.playerGun.projectileColor) : null, args.playerGun.projectileColor) },
        };
        public static Dictionary<string, Func<StatsArgs, string>> CharacterStats = new Dictionary<string, Func<StatsArgs, string>>()
        {
            { "Size Mult", (args) => ProcessStat(args.cardCharacterStatModifiers?.sizeMultiplier, args.playerCharacterStatModifiers.sizeMultiplier, StatsArgs.defaultCharacterStatModifiers.sizeMultiplier, StatsBetter.Less, ChangeMode.Mult) },
            { "Max Health", (args) => ProcessStat(args.cardCharacterStatModifiers?.health, args.playerCharacterData.MaxHealth, StatsArgs.defaultCharacterData.MaxHealth, StatsBetter.More, ChangeMode.Mult) },
            { "Calculated Scale", (args) => ProcessStat(args.cardCharacterStatModifiers ? 1.2f * Mathf.Pow((args.playerCharacterData.MaxHealth * args.cardCharacterStatModifiers.health) / 100f * 1.2f, 0.2f) * (args.playerCharacterStatModifiers.sizeMultiplier * args.cardCharacterStatModifiers.sizeMultiplier) : (float?)null, 1.2f * Mathf.Pow(args.playerCharacterData.MaxHealth / 100f * 1.2f, 0.2f) * args.playerCharacterStatModifiers.sizeMultiplier, 1.2f * Mathf.Pow(StatsArgs.defaultCharacterData.MaxHealth / 100f * 1.2f, 0.2f) * StatsArgs.defaultCharacterStatModifiers.sizeMultiplier, StatsBetter.Less, ChangeMode.Set) },
            { "Calculated Mass", (args) => ProcessStat(args.cardCharacterStatModifiers ? 100f * Mathf.Pow((args.playerCharacterData.MaxHealth * args.cardCharacterStatModifiers.health) / 100f * 1.2f, 0.8f) * (args.playerCharacterStatModifiers.sizeMultiplier * args.cardCharacterStatModifiers.sizeMultiplier) : (float?)null, 100f * Mathf.Pow(args.playerCharacterData.MaxHealth / 100f * 1.2f, 0.8f) * args.playerCharacterStatModifiers.sizeMultiplier, 100f * Mathf.Pow(StatsArgs.defaultCharacterData.MaxHealth / 100f * 1.2f, 0.8f) * StatsArgs.defaultCharacterStatModifiers.sizeMultiplier, StatsBetter.Less, ChangeMode.Set) },
            { "Movement Speed", (args) => ProcessStat(args.cardCharacterStatModifiers?.movementSpeed, args.playerCharacterStatModifiers.movementSpeed, StatsArgs.defaultCharacterStatModifiers.movementSpeed, StatsBetter.More, ChangeMode.Mult) },
            { "Jump", (args) => ProcessStat(args.cardCharacterStatModifiers?.jump, args.playerCharacterStatModifiers.jump, StatsArgs.defaultCharacterStatModifiers.jump, StatsBetter.More, ChangeMode.Mult) },
            { "Jumps", (args) => ProcessStat(args.cardCharacterStatModifiers?.numberOfJumps, args.playerCharacterStatModifiers.numberOfJumps, StatsArgs.defaultCharacterStatModifiers.numberOfJumps, StatsBetter.More, ChangeMode.Add) },
            { "Gravity Force", (args) => ProcessStat(args.cardCharacterStatModifiers?.gravity, args.playerGravity.gravityForce, StatsArgs.defaultGravity.gravityForce, StatsBetter.None, ChangeMode.Mult) },
            { "Regeneration", (args) => ProcessStat(args.cardCharacterStatModifiers?.regen, args.playerHealthHandler.regeneration, StatsArgs.defaultHealthHandler.regeneration, StatsBetter.More, ChangeMode.Add) },
            { "Life Steal", (args) => ProcessStat(args.cardCharacterStatModifiers?.lifeSteal, args.playerCharacterStatModifiers.lifeSteal, StatsArgs.defaultCharacterStatModifiers.lifeSteal, StatsBetter.More, ChangeMode.Add) },
            { "Respawns", (args) => ProcessStat(args.cardCharacterStatModifiers?.respawns, args.playerCharacterStatModifiers.respawns, StatsArgs.defaultCharacterStatModifiers.respawns, StatsBetter.More, ChangeMode.Add) },
            { "Seconds To Take Damage Over", (args) => ProcessStat(args.cardCharacterStatModifiers?.secondsToTakeDamageOver, args.playerCharacterStatModifiers.secondsToTakeDamageOver, StatsArgs.defaultCharacterStatModifiers.secondsToTakeDamageOver, StatsBetter.More, ChangeMode.Add) },
            { "Refresh On Damage", (args) => ProcessStat(args.cardCharacterStatModifiers && args.cardCharacterStatModifiers.refreshOnDamage ? true : (bool?)null, args.playerCharacterStatModifiers.refreshOnDamage, StatsArgs.defaultCharacterStatModifiers.refreshOnDamage, StatsBetter.True) },
            { "Automatic Reload", (args) => ProcessStat(args.cardCharacterStatModifiers && !args.cardCharacterStatModifiers.automaticReload ? false : (bool?)null, args.playerCharacterStatModifiers.automaticReload, StatsArgs.defaultCharacterStatModifiers.automaticReload, StatsBetter.True) },
        };
        public static Dictionary<string, Func<StatsArgs, string>> BlockStats = new Dictionary<string, Func<StatsArgs, string>>()
        {
            { "Cooldown Mult", (args) => ProcessStat(args.cardBlock?.cdMultiplier, args.playerBlock.cdMultiplier, StatsArgs.defaultBlock.cdMultiplier, StatsBetter.Less, ChangeMode.Mult) },
            { "Cooldown Add", (args) => ProcessStat(args.cardBlock?.cdAdd, args.playerBlock.cdAdd, StatsArgs.defaultBlock.cdAdd, StatsBetter.Less, ChangeMode.Add) },
            { "Cooldown", (args) => ProcessStat(null, args.playerBlock.cooldown, StatsArgs.defaultBlock.cooldown, StatsBetter.Less, ChangeMode.Add) },
            { "Calculated Cooldown", (args) => ProcessStat(args.cardBlock ? (args.playerBlock.cooldown + (args.playerBlock.cdAdd + args.cardBlock.cdAdd)) * (args.playerBlock.cdMultiplier * args.cardBlock.cdMultiplier) : (float?)null, (args.playerBlock.cooldown + args.playerBlock.cdAdd) * args.playerBlock.cdMultiplier, (StatsArgs.defaultBlock.cooldown + StatsArgs.defaultBlock.cdAdd) * StatsArgs.defaultBlock.cdMultiplier, StatsBetter.Less, ChangeMode.Set) },
            { "Force To Add", (args) => ProcessStat(args.cardBlock?.forceToAdd, args.playerBlock.forceToAdd, StatsArgs.defaultBlock.forceToAdd, StatsBetter.Less, ChangeMode.Add) },
            { "Force To Add Up", (args) => ProcessStat(args.cardBlock?.forceToAddUp, args.playerBlock.forceToAddUp, StatsArgs.defaultBlock.forceToAddUp, StatsBetter.Less, ChangeMode.Add) },
            { "Additional Blocks", (args) => ProcessStat(args.cardBlock?.additionalBlocks, args.playerBlock.additionalBlocks, StatsArgs.defaultBlock.additionalBlocks, StatsBetter.More, ChangeMode.Add) },
            { "Healing", (args) => ProcessStat(args.cardBlock?.healing, args.playerBlock.healing, StatsArgs.defaultBlock.healing, StatsBetter.More, ChangeMode.Add) },
            { "Auto Block", (args) => ProcessStat(args.cardBlock && args.cardBlock.autoBlock ? args.cardBlock.autoBlock : (bool?)null, args.playerBlock.autoBlock, StatsArgs.defaultBlock.autoBlock, StatsBetter.True) },
        };
        public static string Colour(string text, bool greenCondition, bool redCondition)
        {
            if (greenCondition)
            {
                return "<color=green>" + text + "</color>";
            }
            else if (redCondition)
            {
                return "<color=red>" + text + "</color>";
            }
            return text;
        }
        public static string Colour(string text, float newValue, float originalValue, StatsBetter better)
        {
            if (better != StatsBetter.Less && better != StatsBetter.More && better != StatsBetter.ZeroOrMore) return text;
            bool greenCondition = false;
            bool redCondition = false;
            if (better == StatsBetter.More)
            {
                greenCondition = newValue > originalValue;
                redCondition = newValue < originalValue;
            } else if (better == StatsBetter.Less)
            {
                greenCondition = newValue < originalValue;
                redCondition = newValue > originalValue;
            } else if (better == StatsBetter.ZeroOrMore)
            {
                if ((originalValue == 0f || newValue == 0f) && originalValue != newValue)
                {
                    greenCondition = newValue == 0f;
                    redCondition = newValue != 0f;
                } else
                {
                    greenCondition = newValue > originalValue;
                    redCondition = newValue < originalValue;
                }
            }
            if (greenCondition)
            {
                return "<color=green>" + text + "</color>";
            }
            else if (redCondition)
            {
                return "<color=red>" + text + "</color>";
            }
            return text;
        }
        public static string Colour(string text, int newValue, int originalValue, StatsBetter better)
        {
            if (better != StatsBetter.Less && better != StatsBetter.More && better != StatsBetter.ZeroOrMore) return text;
            bool greenCondition = false;
            bool redCondition = false;
            if (better == StatsBetter.More)
            {
                greenCondition = newValue > originalValue;
                redCondition = newValue < originalValue;
            }
            else if (better == StatsBetter.Less)
            {
                greenCondition = newValue < originalValue;
                redCondition = newValue > originalValue;
            }
            else if (better == StatsBetter.ZeroOrMore)
            {
                if ((originalValue == 0 || newValue == 0) && originalValue != newValue)
                {
                    greenCondition = newValue == 0;
                    redCondition = newValue != 0;
                }
                else
                {
                    greenCondition = newValue > originalValue;
                    redCondition = newValue < originalValue;
                }
            }
            if (greenCondition)
            {
                return "<color=green>" + text + "</color>";
            }
            else if (redCondition)
            {
                return "<color=red>" + text + "</color>";
            }
            return text;
        }
        public static string Colour(string text, bool newValue, bool originalValue, StatsBetter better)
        {
            if (better != StatsBetter.True && better != StatsBetter.False) return text;
            bool greenCondition = false;
            bool redCondition = false;
            if (better == StatsBetter.True)
            {
                greenCondition = !originalValue && newValue;
                redCondition = originalValue && !newValue;
            }
            else if (better == StatsBetter.False)
            {
                greenCondition = originalValue && !newValue;
                redCondition = !originalValue && newValue;
            }
            if (greenCondition)
            {
                return "<color=green>" + text + "</color>";
            }
            else if (redCondition)
            {
                return "<color=red>" + text + "</color>";
            }
            return text;
        }
        public static string Colour(Color color, bool richColor = true)
        {
            string ret = $"RGBA: {Mathf.Round(color.r * 255)},{Mathf.Round(color.g * 255)},{Mathf.Round(color.b * 255)},{Mathf.Round(color.a * 255)}";
            if (richColor)
            {
                ret = $"<color=#{ColorUtility.ToHtmlStringRGBA(new Color(color.r, color.g, color.b, Mathf.Max(color.a, 0.2f)))}>" + ret + "</color>";
            }
            return ret;
        }
        public static string ProcessStat(float? changeValue, float value, float defaultValue, StatsBetter better, ChangeMode change)
        {
            string ret = Colour(Math.Round(value, 3).ToString(), value, defaultValue, better);
            if (changeValue.HasValue)
            {
                float newValue = value;
                if (change == ChangeMode.Set)
                {
                    newValue = changeValue.Value;
                }
                else if (change == ChangeMode.Add)
                {
                    newValue += changeValue.Value;
                }
                else if (change == ChangeMode.Mult)
                {
                    newValue *= changeValue.Value;
                }
                if (newValue != value)
                {
                    ret = Colour(Math.Round(newValue, 3).ToString(), newValue, value, better) + " < " + ret;
                }
            }
            return ret;
        }
        public static string ProcessStat(int? changeValue, int value, int defaultValue, StatsBetter better, ChangeMode change)
        {
            string ret = Colour(value.ToString(), value, defaultValue, better);
            if (changeValue.HasValue)
            {
                int newValue = value;
                if (change == ChangeMode.Set)
                {
                    newValue = changeValue.Value;
                }
                else if (change == ChangeMode.Add)
                {
                    newValue += changeValue.Value;
                }
                else if (change == ChangeMode.Mult)
                {
                    newValue *= changeValue.Value;
                }
                if (newValue != value)
                {
                    ret = Colour(newValue.ToString(), newValue, value, better) + " < " + ret;
                }
            }
            return ret;
        }
        public static string ProcessStat(float? newValue, float value, float defaultValue, StatsBetter better)
        {
            string ret = Colour(Math.Round(value, 3).ToString(), value, defaultValue, better);
            if (newValue.HasValue && newValue != value)
            {
                ret = Colour(Math.Round(newValue.Value, 3).ToString(), newValue.Value, value, better) + " < " + ret;
            }
            return ret;
        }
        public static string ProcessStat(int? newValue, int value, int defaultValue, StatsBetter better)
        {
            string ret = Colour(value.ToString(), value, defaultValue, better);
            if (newValue.HasValue && newValue != value)
            {
                ret = Colour(newValue.Value.ToString(), newValue.Value, value, better) + " < " + ret;
            }
            return ret;
        }
        public static string ProcessStat(bool? newValue, bool value, bool defaultValue, StatsBetter better)
        {
            string ret = Colour(value ? "Yes" : "No", value, defaultValue, better);
            if (newValue.HasValue && newValue != value)
            {
                ret = Colour(newValue.Value ? "Yes" : "No", newValue.Value, value, better) + " < " + ret;
            }
            return ret;
        }
        public static string ProcessStat(Color? newValue, Color value)
        {
            string ret = Colour(value);
            if (newValue.HasValue && newValue != value)
            {
                ret = Colour(newValue.Value) + " < " + ret;
            }
            return ret;
        }

        public static void Init()
        {
            // Panels Creation:

            // Gun Stats Panel
            GunStatsPanel = new GameObject("Gun Stats Panel");
            GunStatsPanel.transform.SetParent(AttachedCardChoiceUI.instance.transform);
            RectTransform GunStatsPanelRectTransform = GunStatsPanel.AddComponent<RectTransform>();
            GunStatsPanelRectTransform.anchorMin = new Vector2(1, 0);
            GunStatsPanelRectTransform.anchorMax = new Vector2(1, 0);
            GunStatsPanelRectTransform.pivot = new Vector2(1, 0);
            GunStatsPanelRectTransform.anchoredPosition = new Vector2(-10, 10);
            GunStatsPanelRectTransform.sizeDelta = new Vector2(450, 885);
            GunStatsPanelRectTransform.localScale = Vector3.one;
            GunStatsPanel.AddComponent<Image>().color = new Color(0, 0, 0, 0.5f);
            VerticalLayoutGroup GunStatsPanelRectTransformLayoutGroup = GunStatsPanel.AddComponent<VerticalLayoutGroup>();
            GunStatsPanelRectTransformLayoutGroup.spacing = 2;
            GunStatsPanelRectTransformLayoutGroup.childControlWidth = true;
            GunStatsPanelRectTransformLayoutGroup.childScaleHeight = true;
            GunStatsPanelRectTransformLayoutGroup.childForceExpandHeight = false;
            GunStatsPanelRectTransformLayoutGroup.childControlHeight = false;
            GunStatsPanelRectTransformLayoutGroup.childAlignment = TextAnchor.UpperCenter;
            GunStatsPanel.SetActive(false);

            // Gun Stats Panel Collapsed
            GunStatsPanelCollapsed = new GameObject("Gun Stats Panel Collapsed");
            GunStatsPanelCollapsed.transform.SetParent(AttachedCardChoiceUI.instance.transform);
            RectTransform GunStatsPanelCollapsedRectTransform = GunStatsPanelCollapsed.AddComponent<RectTransform>();
            GunStatsPanelCollapsedRectTransform.anchorMin = new Vector2(1, 0);
            GunStatsPanelCollapsedRectTransform.anchorMax = new Vector2(1, 0);
            GunStatsPanelCollapsedRectTransform.pivot = new Vector2(1, 0);
            GunStatsPanelCollapsedRectTransform.anchoredPosition = new Vector2(-10, 10);
            GunStatsPanelCollapsedRectTransform.sizeDelta = new Vector2(450, 40);
            GunStatsPanelCollapsedRectTransform.localScale = Vector3.one;
            GunStatsPanelCollapsed.AddComponent<Image>().color = new Color(0, 0, 0, 0.5f);

            // Other Stats Panel
            OtherStatsPanel = new GameObject("Other Stats Panel");
            OtherStatsPanel.transform.SetParent(AttachedCardChoiceUI.instance.transform);
            RectTransform OtherStatsPanelRectTransform = OtherStatsPanel.AddComponent<RectTransform>();
            OtherStatsPanelRectTransform.anchorMin = new Vector2(1, 0);
            OtherStatsPanelRectTransform.anchorMax = new Vector2(1, 0);
            OtherStatsPanelRectTransform.pivot = new Vector2(1, 0);
            OtherStatsPanelRectTransform.anchoredPosition = new Vector2(-470, 10);
            OtherStatsPanelRectTransform.sizeDelta = new Vector2(450, 548);
            OtherStatsPanelRectTransform.localScale = Vector3.one;
            OtherStatsPanel.AddComponent<Image>().color = new Color(0, 0, 0, 0.5f);
            VerticalLayoutGroup OtherStatsPanelRectTransformLayoutGroup = OtherStatsPanel.AddComponent<VerticalLayoutGroup>();
            OtherStatsPanelRectTransformLayoutGroup.spacing = 2;
            OtherStatsPanelRectTransformLayoutGroup.childControlWidth = true;
            OtherStatsPanelRectTransformLayoutGroup.childScaleHeight = true;
            OtherStatsPanelRectTransformLayoutGroup.childForceExpandHeight = false;
            OtherStatsPanelRectTransformLayoutGroup.childControlHeight = false;
            OtherStatsPanelRectTransformLayoutGroup.childAlignment = TextAnchor.UpperCenter;
            OtherStatsPanel.SetActive(false);

            // Other Stats Panel Collapsed
            OtherStatsPanelCollapsed = new GameObject("Other Stats Panel Collapsed");
            OtherStatsPanelCollapsed.transform.SetParent(AttachedCardChoiceUI.instance.transform);
            RectTransform OtherStatsPanelCollapsedRectTransform = OtherStatsPanelCollapsed.AddComponent<RectTransform>();
            OtherStatsPanelCollapsedRectTransform.anchorMin = new Vector2(1, 0);
            OtherStatsPanelCollapsedRectTransform.anchorMax = new Vector2(1, 0);
            OtherStatsPanelCollapsedRectTransform.pivot = new Vector2(1, 0);
            OtherStatsPanelCollapsedRectTransform.anchoredPosition = new Vector2(-470, 10);
            OtherStatsPanelCollapsedRectTransform.sizeDelta = new Vector2(450, 40);
            OtherStatsPanelCollapsedRectTransform.localScale = Vector3.one;
            OtherStatsPanelCollapsed.AddComponent<Image>().color = new Color(0, 0, 0, 0.5f);


            // Only need to create elements on collapsed once, so it is done here instead of FullRefresh
            AddTitle("Gun Stats", GunStatsPanelCollapsed, true, true, GunStatsPanel);
            AddTitle("Other Stats", OtherStatsPanelCollapsed, true, true, OtherStatsPanel);
        }
        public static void AddTitle(string text, GameObject parent, bool hasCollapse, bool collapsed, GameObject otherCollapse)
        {
            GameObject title = new GameObject(text);
            title.transform.SetParent(parent.transform, false);
            RectTransform titleRectTransform = title.AddComponent<RectTransform>();
            if (collapsed)
            {
                titleRectTransform.anchorMin = Vector2.zero;
                titleRectTransform.anchorMax = Vector2.one;
                titleRectTransform.offsetMin = Vector2.zero;
                titleRectTransform.offsetMax = Vector2.zero;
            } else
            {
                titleRectTransform.sizeDelta = new Vector2(0, 40);
            }
            titleRectTransform.localScale = Vector3.one;
            TextMeshProUGUI titleText = title.AddComponent<TextMeshProUGUI>();
            titleText.text = text;
            titleText.horizontalAlignment = HorizontalAlignmentOptions.Center;
            if (hasCollapse)
            {
                GameObject collapse = new GameObject("Collapse");
                collapse.transform.SetParent(title.transform, false);
                RectTransform collapseRectTransform = collapse.AddComponent<RectTransform>();
                collapseRectTransform.anchorMin = new Vector2(1, 0.5f);
                collapseRectTransform.anchorMax = new Vector2(1, 0.5f);
                collapseRectTransform.pivot = new Vector2(1, 0.5f);
                collapseRectTransform.anchoredPosition = new Vector2(-10, 0);
                collapseRectTransform.sizeDelta = new Vector2(30, 30);
                collapseRectTransform.localScale = Vector3.one;
                Button collapseButton = collapse.AddComponent<Button>();
                collapseButton.onClick.AddListener(() =>
                {
                    parent.SetActive(false);
                    otherCollapse.SetActive(true);
                });
                GameObject collapseText = new GameObject("Text");
                collapseText.transform.SetParent(collapse.transform, false);
                RectTransform collapseTextRectTransform = collapseText.AddComponent<RectTransform>();
                collapseTextRectTransform.anchorMin = Vector2.zero;
                collapseTextRectTransform.anchorMax = Vector2.one;
                collapseTextRectTransform.offsetMin = Vector2.zero;
                collapseTextRectTransform.offsetMax = Vector2.zero;
                collapseTextRectTransform.localScale = Vector3.one;
                TextMeshProUGUI collapseTextTMP = collapseText.AddComponent<TextMeshProUGUI>();
                collapseTextTMP.text = collapsed ? "/\\" : "\\/";
                collapseTextTMP.fontStyle = FontStyles.Bold;
                collapseTextTMP.characterSpacing = -10;
                collapseTextTMP.horizontalAlignment = HorizontalAlignmentOptions.Center;
                collapseTextTMP.verticalAlignment = VerticalAlignmentOptions.Middle;
                collapseButton.targetGraphic = collapseTextTMP;
            }
        }
        public static void AddStat(string name, string value, GameObject parent)
        {
            GameObject stat = new GameObject(name);
            stat.transform.SetParent(parent.transform, false);
            RectTransform statRectTransform = stat.AddComponent<RectTransform>();
            statRectTransform.sizeDelta = new Vector2(0, 18);
            statRectTransform.localScale = Vector3.one;
            GameObject nameGO = new GameObject("Name");
            nameGO.transform.SetParent(stat.transform, false);
            RectTransform nameRectTransform = nameGO.AddComponent<RectTransform>();
            nameRectTransform.anchorMin = Vector2.zero;
            nameRectTransform.anchorMax = Vector2.one;
            nameRectTransform.offsetMin = new Vector2(10, 0);
            nameRectTransform.offsetMax = Vector2.zero;
            TextMeshProUGUI nameText = nameGO.AddComponent<TextMeshProUGUI>();
            nameText.text = name;
            nameText.enableAutoSizing = true;
            nameText.verticalAlignment = VerticalAlignmentOptions.Middle;
            GameObject valueGO = new GameObject("Value");
            valueGO.transform.SetParent(stat.transform, false);
            RectTransform valueRectTransform = valueGO.AddComponent<RectTransform>();
            valueRectTransform.anchorMin = Vector2.zero;
            valueRectTransform.anchorMax = Vector2.one;
            valueRectTransform.offsetMin = Vector2.zero;
            valueRectTransform.offsetMax = new Vector2(-10, 0);
            TextMeshProUGUI valueText = valueGO.AddComponent<TextMeshProUGUI>();
            valueText.text = value;
            valueText.enableAutoSizing = true;
            valueText.verticalAlignment = VerticalAlignmentOptions.Middle;
            valueText.horizontalAlignment = HorizontalAlignmentOptions.Right;
        }
        public static void FullRefresh(ApplyCardStats applyCardStats = null)
        {
            for (int i = 0; i < GunStatsPanel.transform.childCount; i++) GameObject.Destroy(GunStatsPanel.transform.GetChild(i).gameObject);
            for (int i = 0; i < OtherStatsPanel.transform.childCount; i++) GameObject.Destroy(OtherStatsPanel.transform.GetChild(i).gameObject);

            Gun gun = player.GetComponent<Holding>().holdable.GetComponent<Gun>();
            GunAmmo gunAmmo = gun.GetComponentInChildren<GunAmmo>();
            CharacterStatModifiers characterStatModifiers = player.GetComponent<CharacterStatModifiers>();
            CharacterData characterData = player.GetComponent<CharacterData>();
            Gravity gravity = player.GetComponent<Gravity>();
            Block block = player.GetComponent<Block>();
            HealthHandler healthHandler = player.GetComponent<HealthHandler>();
            StatsArgs statsArgs = new StatsArgs(gunAmmo, gun, characterStatModifiers, characterData, gravity, block, healthHandler, (Gun)applyCardStats?.GetFieldValue("myGunStats"), (CharacterStatModifiers)applyCardStats?.GetFieldValue("myPlayerStats"), (Block)applyCardStats?.GetFieldValue("myBlock"));

            // Gun Stats Panel
            AddTitle("Gun Stats", GunStatsPanel, true, false, GunStatsPanelCollapsed);

            foreach (KeyValuePair<string, Func<StatsArgs, string>> stat in GunStats)
            {
                AddStat(stat.Key, stat.Value(statsArgs), GunStatsPanel);
            }

            // Other Stats Panel
            AddTitle("Character Stats", OtherStatsPanel, true, false, OtherStatsPanelCollapsed);

            foreach (KeyValuePair<string, Func<StatsArgs, string>> stat in CharacterStats)
            {
                AddStat(stat.Key, stat.Value(statsArgs), OtherStatsPanel);
            }

            AddTitle("Block Stats", OtherStatsPanel, false, false, OtherStatsPanelCollapsed);

            foreach (KeyValuePair<string, Func<StatsArgs, string>> stat in BlockStats)
            {
                AddStat(stat.Key, stat.Value(statsArgs), OtherStatsPanel);
            }
        }
        public static void Update(ApplyCardStats applyCardStats = null)
        {
            Gun gun = player.GetComponent<Holding>().holdable.GetComponent<Gun>();
            GunAmmo gunAmmo = gun.GetComponentInChildren<GunAmmo>();
            CharacterStatModifiers characterStatModifiers = player.GetComponent<CharacterStatModifiers>();
            CharacterData characterData = player.GetComponent<CharacterData>();
            Gravity gravity = player.GetComponent<Gravity>();
            Block block = player.GetComponent<Block>();
            HealthHandler healthHandler = player.GetComponent<HealthHandler>();
            StatsArgs statsArgs = new StatsArgs(gunAmmo, gun, characterStatModifiers, characterData, gravity, block, healthHandler, (Gun)applyCardStats?.GetFieldValue("myGunStats"), (CharacterStatModifiers)applyCardStats?.GetFieldValue("myPlayerStats"), (Block)applyCardStats?.GetFieldValue("myBlock"));

            foreach (KeyValuePair<string, Func<StatsArgs, string>> stat in GunStats)
            {
                Transform statT = GunStatsPanel.transform.Find(stat.Key);
                if (statT == null)
                {
                    FullRefresh(applyCardStats);
                    return;
                }
                TextMeshProUGUI statValueText = statT.Find("Value").GetComponent<TextMeshProUGUI>();
                statValueText.text = stat.Value(statsArgs);
            }

            foreach (KeyValuePair<string, Func<StatsArgs, string>> stat in CharacterStats)
            {
                Transform statT = OtherStatsPanel.transform.Find(stat.Key);
                if (statT == null)
                {
                    FullRefresh(applyCardStats);
                    return;
                }
                TextMeshProUGUI statValueText = statT.Find("Value").GetComponent<TextMeshProUGUI>();
                statValueText.text = stat.Value(statsArgs);
            }
            foreach (KeyValuePair<string, Func<StatsArgs, string>> stat in BlockStats)
            {
                Transform statT = OtherStatsPanel.transform.Find(stat.Key);
                if (statT == null)
                {
                    FullRefresh(applyCardStats);
                    return;
                }
                TextMeshProUGUI statValueText = statT.Find("Value").GetComponent<TextMeshProUGUI>();
                statValueText.text = stat.Value(statsArgs);
            }
        }
        public static void ChangePlayer(Player newPlayer, ApplyCardStats applyCardStats = null)
        {
            player = newPlayer;
            Update(applyCardStats);
        }
        public static void UpdateControls()
        {
            foreach (InputDevice inputDevice in InputManager.ActiveDevices)
            {
                if (inputDevice.Action3.WasPressed)
                {
                    bool newState = !GunStatsPanel.activeSelf;
                    GunStatsPanel.SetActive(newState);
                    GunStatsPanelCollapsed.SetActive(!newState);
                    OtherStatsPanel.SetActive(newState);
                    OtherStatsPanelCollapsed.SetActive(!newState);
                }
            }
        }
        public static Color? projectileColorMath(Color card, Color player)
        {
            if (card != Color.black)
            {
                if (player == Color.black)
                {
                    player = card;
                }
                float num3 = Mathf.Pow((player.r * player.r + card.r * card.r) / 2f, 0.5f);
                float num4 = Mathf.Pow((player.g * player.g + card.g * card.g) / 2f, 0.5f);
                float num5 = Mathf.Pow((player.b * player.b + card.b * card.b) / 2f, 0.5f);
                Color color = new Color(num3, num4, num5, 1f);
                float num6 = 0f;
                float num7 = 0f;
                float num8 = 0f;
                Color.RGBToHSV(color, out num6, out num7, out num8);
                num7 = 1f;
                num8 = 1f;
                return Color.HSVToRGB(num6, num7, num8);
            }
            return null;
        }
    }
}
