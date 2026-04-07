using SkillTreeEditor.Enums;

namespace SkillTreeEditor;

public class Helper
{
    public record EnumItem(int Id, string Name);
    public record LongEnumItem(long Id, string Name);
    
    public static int ParseIntOrDefault(string? text)
    {
        return int.TryParse(text, out var value) ? value : 0;
    }
    
    public static List<EnumItem> CreateEnumItems<TEnum>(IEnumerable<int> values)
        where TEnum : struct, Enum
    {
        return values
            .Select(value => new EnumItem(value, Enum.IsDefined(typeof(TEnum), value)
                ? ((TEnum)(object)value).ToString()
                : $"Unknown ({value})"))
            .ToList();
    }
    
    public static List<LongEnumItem> CreateEnumItems<TEnum>(IEnumerable<long> values)
        where TEnum : struct, Enum
    {
        return values
            .Select(value => new LongEnumItem(value, Enum.IsDefined(typeof(TEnum), value)
                ? ((TEnum)(object)value).ToString()
                : $"Unknown ({value})"))
            .ToList();
    }

    public static int GetIconIdFromSphere(SphereData sphere)
    {
        var id = sphere switch
        {
            { Impassable: true } => 34,
            { FighterCardListId: not 0 } => 31,
            { SpellId: not 0 } => 30,
            { TeleportXPosition: not 0 } or { TeleportYPosition: not 0 } => 32,
            _ => 0
        };
        if (id != 0 || sphere.Effects.Count == 0)
            return id;

        return (ActionType)sphere.Effects[0].ActionId switch
        {
            ActionType.BreedFireDamage => 5,
            ActionType.BreedEarthDamage => 6,
            ActionType.BreedWaterDamage => 7,
            ActionType.BreedWindDamage => 4,
            ActionType.DefaultHpLoss => 8,
            ActionType.WeaponFireDamage => 5,
            ActionType.WeaponEarthDamage => 6,
            ActionType.WeaponWaterDamage => 7,
            ActionType.WeaponWindDamage => 4,
            ActionType.SpellPhysicalDamage => 8,
            ActionType.SpellFireDamage => 5,
            ActionType.SpellEarthDamage => 6,
            ActionType.SpellWaterDamage => 7,
            ActionType.SpellWindDamage => 4,
            ActionType.SpellFireDamagePerApRemaining => 5,
            ActionType.SpellFireDamagePerMpRemaining => 5,
            ActionType.SpellWindDamagePerApRemaining => 4,
            ActionType.SpellWindDamagePerMpRemaining => 4,
            ActionType.SpellWaterDamagePerApRemaining => 7,
            ActionType.SpellWaterDamagePerMpRemaining => 7,
            ActionType.SpellEarthDamagePerApRemaining => 6,
            ActionType.SpellEarthDamagePerMpRemaining => 6,
            ActionType.SpellFireDamageAreaTrigger => 5,
            ActionType.SpellWaterDamageAreaTrigger => 7,
            ActionType.SpellWindDamageAreaTrigger => 4,
            ActionType.SpellEarthDamageAreaTrigger => 6,
            ActionType.ApLossAreaTrigger => 15,
            ActionType.MpLossAreaTrigger => 16,
            ActionType.HpLeech => 8,
            ActionType.HpLeechFire => 5,
            ActionType.HpLeechEarth => 6,
            ActionType.HpLeechWater => 7,
            ActionType.HpLeechWind => 4,
            ActionType.DamagePercent => 8,
            ActionType.Poison => 8,
            ActionType.Heal => 21,
            ActionType.UseAp => 15,
            ActionType.UseMp => 16,
            ActionType.CharacteristicBoostHp => 18,
            ActionType.CharacteristicDeboostHp => 18,
            ActionType.CharacteristicBoostAp => 15,
            ActionType.CharacteristicBoostAp2 => 15,
            ActionType.CharacteristicDeboostAp => 15,
            ActionType.CharacteristicDeboostAp2 => 15,
            ActionType.CharacteristicBoostMp => 16,
            ActionType.CharacteristicBoostMp2 => 16,
            ActionType.CharacteristicDeboostMp => 16,
            ActionType.CharacteristicDeboostMp2 => 16,
            ActionType.CharacteristicGainAp => 15,
            ActionType.CharacteristicGainMp => 16,
            ActionType.CharacteristicGainResFlatFire => 5,
            ActionType.CharacteristicGainResFlatEarth => 6,
            ActionType.CharacteristicGainResFlatWater => 7,
            ActionType.CharacteristicGainResFlatWind => 4,
            ActionType.CharacteristicGainResPercentFire => 10,
            ActionType.CharacteristicGainResPercentEarth => 11,
            ActionType.CharacteristicGainResPercentWater => 12,
            ActionType.CharacteristicGainResPercentWind => 9,
            ActionType.CharacteristicGainResPercentAll => 13,
            ActionType.CharacteristicGainResArea => 13,
            ActionType.CharacteristicGainDmgFlatFire => 5,
            ActionType.CharacteristicGainDmgFlatEarth => 6,
            ActionType.CharacteristicGainDmgFlatWater => 7,
            ActionType.CharacteristicGainDmgFlatWind => 4,
            ActionType.CharacteristicGainDmgPercentFire => 5,
            ActionType.CharacteristicGainDmgPercentEarth => 6,
            ActionType.CharacteristicGainDmgPercentWater => 7,
            ActionType.CharacteristicGainDmgPercentWind => 4,
            ActionType.CharacteristicGainDmgPercentAll => 8,
            ActionType.CharacteristicGainCc => 3,
            ActionType.CharacteristicGainEc => 3,
            ActionType.CharacteristicGainRange => 19,
            ActionType.CharacteristicGainInit => 17,
            ActionType.CharacteristicGainHeal => 21,
            ActionType.CharacteristicGainResApDebuff => 15,
            ActionType.CharacteristicGainResMpDebuff => 16,
            ActionType.CharacteristicGainDamagesReboundPercent => 20,
            ActionType.CharacteristicGainTackle => 1,
            ActionType.CharacteristicGainDodge => 2,
            ActionType.CharacteristicGainSummonNumber => 14,
            ActionType.CharacteristicGainSummonDmg => 24,
            ActionType.CharacteristicGainSummonRes => 26,
            ActionType.CharacteristicGainSummonCc => 23,
            ActionType.CharacteristicGainSummonTackle => 22,
            ActionType.CharacteristicGainSummonHp => 25,
            ActionType.CharacteristicLossAp => 15,
            ActionType.CharacteristicLossMp => 16,
            ActionType.CharacteristicLossResFlatFire => 10,
            ActionType.CharacteristicLossResFlatEarth => 11,
            ActionType.CharacteristicLossResFlatWater => 12,
            ActionType.CharacteristicLossResFlatWind => 9,
            ActionType.CharacteristicLossResPercentFire => 10,
            ActionType.CharacteristicLossResPercentEarth => 11,
            ActionType.CharacteristicLossResPercentWater => 12,
            ActionType.CharacteristicLossResPercentWind => 9,
            ActionType.CharacteristicLossResPercentAll => 13,
            ActionType.CharacteristicLossResArea => 13,
            ActionType.CharacteristicLossDmgFlatFire => 5,
            ActionType.CharacteristicLossDmgFlatEarth => 6,
            ActionType.CharacteristicLossDmgFlatWater => 7,
            ActionType.CharacteristicLossDmgFlatWind => 4,
            ActionType.CharacteristicLossDmgPercentFire => 5,
            ActionType.CharacteristicLossDmgPercentEarth => 6,
            ActionType.CharacteristicLossDmgPercentWater => 7,
            ActionType.CharacteristicLossDmgPercentWind => 4,
            ActionType.CharacteristicLossDmgPercentAll => 8,
            ActionType.CharacteristicLossCc => 3,
            ActionType.CharacteristicLossEc => 3,
            ActionType.CharacteristicLossRange => 19,
            ActionType.CharacteristicLossInit => 17,
            ActionType.CharacteristicLossHeal => 21,
            ActionType.CharacteristicLossTackle => 1,
            ActionType.CharacteristicLossDodge => 2,
            ActionType.CharacteristicLeechAp => 15,
            ActionType.CharacteristicLeechMp => 16,
            ActionType.CharacteristicLeechDmgPercentAll => 8,
            ActionType.CharacteristicGainOnHitFire => 5,
            ActionType.CharacteristicGainOnHitEarth => 6,
            ActionType.CharacteristicGainOnHitWater => 7,
            ActionType.CharacteristicGainOnHitWind => 4,
            ActionType.Teleport => 50,
            ActionType.Pull => 50,
            ActionType.Push => 50,
            ActionType.MoveTowardsTarget => 50,
            ActionType.PushedBackFromTarget => 50,
            ActionType.Carry => 50,
            ActionType.Throw => 50,
            ActionType.ExchangePosition => 50,
            ActionType.PropertyStable => 50,
            ActionType.PropertyDrunk => 5,
            ActionType.PropertyImmune => 13,
            ActionType.PropertyImmuneToSpell => 13,
            ActionType.PropertyNonTransposable => 50,
            ActionType.PropertyRooted => 50,
            ActionType.PropertyImmobilized => 16,
            ActionType.PropertyPetrified => 50,
            ActionType.PropertySpellRebound => 13,
            ActionType.PropertyInvisible => 19,
            ActionType.PropertyEvanescent => 13,
            ActionType.PropertyInvisibleForParent => 19,
            ActionType.AdaptLook => 19,
            ActionType.ChangeLook => 19,
            ActionType.Summon => 14,
            ActionType.SummonDouble => 14,
            ActionType.SummonMirror => 14,
            ActionType.SpellCooldownReset => 50,
            ActionType.InvertBonusCell => 50,
            ActionType.SetEffectArea => 50,
            ActionType.RemoveEffect => 50,
            ActionType.RevealInvisible => 19,
            ActionType.Death => 8,
            ActionType.Debuff => 50,
            ActionType.AttractSight => 19,
            ActionType.Bluff => 8,
            ActionType.MapDestruction => 50,
            ActionType.NoEffect => 50,
            _ => 0
        };
    }
}