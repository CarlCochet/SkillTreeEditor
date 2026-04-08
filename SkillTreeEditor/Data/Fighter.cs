namespace SkillTreeEditor;

public class Fighter
{
    private int _hp;
    private int _ap;
    private int _mp;
    private int _range;
    private int _init;
    
    private int _resFlatFire;
    private int _resFlatEarth;
    private int _resFlatWater;
    private int _resFlatWind;
    private int _resPercentFire;
    private int _resPercentEarth;
    private int _resPercentWater;
    private int _resPercentWind;
    private int _resPercentAll;
    private int _resArea;

    private int _dmgFlatFire;
    private int _dmgFlatEarth;
    private int _dmgFlatWater;
    private int _dmgFlatWind;
    private int _dmgPercentFire;
    private int _dmgPercentEarth;
    private int _dmgPercentWater;
    private int _dmgPercentWind;
    private int _dmgPercentAll;

    private int _cc;
    private int _ec;
    private int _heal;
    private int _resApDebuff;
    private int _resMpDebuff;
    private int _damagesReboundPercent;
    private int _tackle;
    private int _dodge;

    private int _summonNumber;
    private int _summonDmg;
    private int _summonRes;
    private int _summonCc;
    private int _summonTackle;
    private int _summonHp;
    
    private int _sphereBoardId;
    private int _totalXp;
    private int _totalSpheres;
    
    private BreedData _breed;

    public Fighter(int sphereBoardId, List<SphereData> spheres, BreedData breed)
    {
        _sphereBoardId = sphereBoardId;
        _breed = breed;
        ComputeStats(spheres);
    }

    public void ResetStats()
    {
        _hp = _breed.BaseEvolutionModeHp;
        _ap = _breed.BaseEvolutionModeAp;
        _mp = _breed.BaseEvolutionModeMp;
        _range = 0;
        _init = _breed.BaseInitiative;

        _resFlatFire = 0;
        _resFlatEarth = 0;
        _resFlatWater = 0;
        _resFlatWind = 0;
        _resPercentFire = 0;
        _resPercentEarth = 0;
        _resPercentWater = 0;
        _resPercentWind = 0;
        _resPercentAll = _breed.BaseEvolutionModeResistanceInPercent;
        _resArea = 0;

        _dmgFlatFire = 0;
        _dmgFlatEarth = 0;
        _dmgFlatWater = 0;
        _dmgFlatWind = 0;
        _dmgPercentFire = 0;
        _dmgPercentEarth = 0;
        _dmgPercentWater = 0;
        _dmgPercentWind = 0;
        _dmgPercentAll = _breed.BaseEvolutionModeDamagesInPercent;

        _cc = 0;
        _ec = 0;
        _heal = _breed.BaseEvolutionModeHeal;
        _resApDebuff = 0;
        _resMpDebuff = 0;
        _damagesReboundPercent = 0;
        _tackle = _breed.BaseEvolutionModeTackle;
        _dodge = _breed.BaseEvolutionModeDodge;

        _summonNumber = 1;
        _summonDmg = _breed.BaseEvolutionModeSummonMasteryDamages;
        _summonRes = _breed.BaseEvolutionModeSummonMasteryResistance;
        _summonCc = _breed.BaseEvolutionModeSummonMasteryCritical;
        _summonTackle = _breed.BaseEvolutionModeSummonMasteryBlock;
        _summonHp = _breed.BaseEvolutionModeSummonMasteryHp;
        
        _totalXp = 0;
        _totalSpheres = 0;
    }

    public void ComputeStats(List<SphereData> spheres)
    {
        ResetStats();

        foreach (var sphere in spheres.Where(s => s.SphereBoardId == _sphereBoardId))
        {
            _totalXp += sphere.XpNumber;
            var isValid = false;
            foreach (var effect in sphere.Effects)
            {
                isValid = true;
                var value = effect.Params.Count > 0 ? (int)Math.Round(effect.Params[0]) : 0;

                switch ((Enums.ActionType)effect.ActionId)
                {
                    case Enums.ActionType.CharacteristicBoostHp:
                        _hp += value;
                        break;
                    case Enums.ActionType.CharacteristicDeboostHp:
                        _hp -= value;
                        break;

                    case Enums.ActionType.CharacteristicBoostAp:
                    case Enums.ActionType.CharacteristicBoostAp2:
                    case Enums.ActionType.CharacteristicGainAp:
                        _ap += value;
                        break;
                    case Enums.ActionType.CharacteristicDeboostAp:
                    case Enums.ActionType.CharacteristicDeboostAp2:
                    case Enums.ActionType.CharacteristicLossAp:
                        _ap -= value;
                        break;

                    case Enums.ActionType.CharacteristicBoostMp:
                    case Enums.ActionType.CharacteristicBoostMp2:
                    case Enums.ActionType.CharacteristicGainMp:
                        _mp += value;
                        break;
                    case Enums.ActionType.CharacteristicDeboostMp:
                    case Enums.ActionType.CharacteristicDeboostMp2:
                    case Enums.ActionType.CharacteristicLossMp:
                        _mp -= value;
                        break;

                    case Enums.ActionType.CharacteristicGainRange:
                        _range += value;
                        break;
                    case Enums.ActionType.CharacteristicLossRange:
                        _range -= value;
                        break;

                    case Enums.ActionType.CharacteristicGainInit:
                        _init += value;
                        break;
                    case Enums.ActionType.CharacteristicLossInit:
                        _init -= value;
                        break;

                    case Enums.ActionType.CharacteristicGainResFlatFire:
                        _resFlatFire += value;
                        break;
                    case Enums.ActionType.CharacteristicLossResFlatFire:
                        _resFlatFire -= value;
                        break;
                    case Enums.ActionType.CharacteristicGainResFlatEarth:
                        _resFlatEarth += value;
                        break;
                    case Enums.ActionType.CharacteristicLossResFlatEarth:
                        _resFlatEarth -= value;
                        break;
                    case Enums.ActionType.CharacteristicGainResFlatWater:
                        _resFlatWater += value;
                        break;
                    case Enums.ActionType.CharacteristicLossResFlatWater:
                        _resFlatWater -= value;
                        break;
                    case Enums.ActionType.CharacteristicGainResFlatWind:
                        _resFlatWind += value;
                        break;
                    case Enums.ActionType.CharacteristicLossResFlatWind:
                        _resFlatWind -= value;
                        break;

                    case Enums.ActionType.CharacteristicGainResPercentFire:
                        _resPercentFire += value;
                        break;
                    case Enums.ActionType.CharacteristicLossResPercentFire:
                        _resPercentFire -= value;
                        break;
                    case Enums.ActionType.CharacteristicGainResPercentEarth:
                        _resPercentEarth += value;
                        break;
                    case Enums.ActionType.CharacteristicLossResPercentEarth:
                        _resPercentEarth -= value;
                        break;
                    case Enums.ActionType.CharacteristicGainResPercentWater:
                        _resPercentWater += value;
                        break;
                    case Enums.ActionType.CharacteristicLossResPercentWater:
                        _resPercentWater -= value;
                        break;
                    case Enums.ActionType.CharacteristicGainResPercentWind:
                        _resPercentWind += value;
                        break;
                    case Enums.ActionType.CharacteristicLossResPercentWind:
                        _resPercentWind -= value;
                        break;
                    case Enums.ActionType.CharacteristicGainResPercentAll:
                        _resPercentAll += value;
                        break;
                    case Enums.ActionType.CharacteristicLossResPercentAll:
                        _resPercentAll -= value;
                        break;
                    case Enums.ActionType.CharacteristicGainResArea:
                        _resArea += value;
                        break;
                    case Enums.ActionType.CharacteristicLossResArea:
                        _resArea -= value;
                        break;

                    case Enums.ActionType.CharacteristicGainDmgFlatFire:
                        _dmgFlatFire += value;
                        break;
                    case Enums.ActionType.CharacteristicLossDmgFlatFire:
                        _dmgFlatFire -= value;
                        break;
                    case Enums.ActionType.CharacteristicGainDmgFlatEarth:
                        _dmgFlatEarth += value;
                        break;
                    case Enums.ActionType.CharacteristicLossDmgFlatEarth:
                        _dmgFlatEarth -= value;
                        break;
                    case Enums.ActionType.CharacteristicGainDmgFlatWater:
                        _dmgFlatWater += value;
                        break;
                    case Enums.ActionType.CharacteristicLossDmgFlatWater:
                        _dmgFlatWater -= value;
                        break;
                    case Enums.ActionType.CharacteristicGainDmgFlatWind:
                        _dmgFlatWind += value;
                        break;
                    case Enums.ActionType.CharacteristicLossDmgFlatWind:
                        _dmgFlatWind -= value;
                        break;

                    case Enums.ActionType.CharacteristicGainDmgPercentFire:
                        _dmgPercentFire += value;
                        break;
                    case Enums.ActionType.CharacteristicLossDmgPercentFire:
                        _dmgPercentFire -= value;
                        break;
                    case Enums.ActionType.CharacteristicGainDmgPercentEarth:
                        _dmgPercentEarth += value;
                        break;
                    case Enums.ActionType.CharacteristicLossDmgPercentEarth:
                        _dmgPercentEarth -= value;
                        break;
                    case Enums.ActionType.CharacteristicGainDmgPercentWater:
                        _dmgPercentWater += value;
                        break;
                    case Enums.ActionType.CharacteristicLossDmgPercentWater:
                        _dmgPercentWater -= value;
                        break;
                    case Enums.ActionType.CharacteristicGainDmgPercentWind:
                        _dmgPercentWind += value;
                        break;
                    case Enums.ActionType.CharacteristicLossDmgPercentWind:
                        _dmgPercentWind -= value;
                        break;
                    case Enums.ActionType.CharacteristicGainDmgPercentAll:
                        _dmgPercentAll += value;
                        break;
                    case Enums.ActionType.CharacteristicLossDmgPercentAll:
                        _dmgPercentAll -= value;
                        break;

                    case Enums.ActionType.CharacteristicGainCc:
                        _cc += value;
                        break;
                    case Enums.ActionType.CharacteristicLossCc:
                        _cc -= value;
                        break;
                    case Enums.ActionType.CharacteristicGainEc:
                        _ec += value;
                        break;
                    case Enums.ActionType.CharacteristicLossEc:
                        _ec -= value;
                        break;
                    case Enums.ActionType.CharacteristicGainHeal:
                        _heal += value;
                        break;
                    case Enums.ActionType.CharacteristicLossHeal:
                        _heal -= value;
                        break;
                    case Enums.ActionType.CharacteristicGainResApDebuff:
                        _resApDebuff += value;
                        break;
                    case Enums.ActionType.CharacteristicGainResMpDebuff:
                        _resMpDebuff += value;
                        break;
                    case Enums.ActionType.CharacteristicGainDamagesReboundPercent:
                        _damagesReboundPercent += value;
                        break;
                    case Enums.ActionType.CharacteristicGainTackle:
                        _tackle += value;
                        break;
                    case Enums.ActionType.CharacteristicLossTackle:
                        _tackle -= value;
                        break;
                    case Enums.ActionType.CharacteristicGainDodge:
                        _dodge += value;
                        break;
                    case Enums.ActionType.CharacteristicLossDodge:
                        _dodge -= value;
                        break;
                    
                    case Enums.ActionType.CharacteristicGainSummonNumber:
                        _summonNumber += value;
                        break;
                    case Enums.ActionType.CharacteristicGainSummonDmg:
                        _summonDmg += value;
                        break;
                    case Enums.ActionType.CharacteristicGainSummonRes:
                        _summonRes += value;
                        break;
                    case Enums.ActionType.CharacteristicGainSummonCc:
                        _summonCc += value;
                        break;
                    case Enums.ActionType.CharacteristicGainSummonTackle:
                        _summonTackle += value;
                        break;
                    case Enums.ActionType.CharacteristicGainSummonHp:
                        _summonHp += value;
                        break;
                }
            }
            if (isValid)
                _totalSpheres++;
        }
        
        PrintStats();
    }

    public void PrintStats()
    {
        void PrintIfNotZero(string label, int value)
        {
            if (value != 0)
            {
                Console.WriteLine($"{label}: {value}");
            }
        }

        Console.WriteLine("-----------------------------------------------------------");
        Console.WriteLine($"Sphere Board ID: {_sphereBoardId}");
        Console.WriteLine("-----------------------------------------------------------\n");
        
        Console.WriteLine($"Total XP: {_totalXp}");
        Console.WriteLine($"Total Spheres: {_totalSpheres}\n");
        
        PrintIfNotZero("HP", _hp);
        PrintIfNotZero("AP", _ap);
        PrintIfNotZero("MP", _mp);
        PrintIfNotZero("Range", _range);
        PrintIfNotZero("Init", _init);

        PrintIfNotZero("Res Flat Fire", _resFlatFire);
        PrintIfNotZero("Res Flat Earth", _resFlatEarth);
        PrintIfNotZero("Res Flat Water", _resFlatWater);
        PrintIfNotZero("Res Flat Wind", _resFlatWind);
        PrintIfNotZero("Res Percent Fire", _resPercentFire);
        PrintIfNotZero("Res Percent Earth", _resPercentEarth);
        PrintIfNotZero("Res Percent Water", _resPercentWater);
        PrintIfNotZero("Res Percent Wind", _resPercentWind);
        PrintIfNotZero("Res Percent All", _resPercentAll);
        PrintIfNotZero("Res Area", _resArea);

        PrintIfNotZero("Dmg Flat Fire", _dmgFlatFire);
        PrintIfNotZero("Dmg Flat Earth", _dmgFlatEarth);
        PrintIfNotZero("Dmg Flat Water", _dmgFlatWater);
        PrintIfNotZero("Dmg Flat Wind", _dmgFlatWind);
        PrintIfNotZero("Dmg Percent Fire", _dmgPercentFire);
        PrintIfNotZero("Dmg Percent Earth", _dmgPercentEarth);
        PrintIfNotZero("Dmg Percent Water", _dmgPercentWater);
        PrintIfNotZero("Dmg Percent Wind", _dmgPercentWind);
        PrintIfNotZero("Dmg Percent All", _dmgPercentAll);

        PrintIfNotZero("CC", _cc);
        PrintIfNotZero("EC", _ec);
        PrintIfNotZero("Heal", _heal);
        PrintIfNotZero("Res AP Debuff", _resApDebuff);
        PrintIfNotZero("Res MP Debuff", _resMpDebuff);
        PrintIfNotZero("Damages Rebound Percent", _damagesReboundPercent);
        PrintIfNotZero("Tackle", _tackle);
        PrintIfNotZero("Dodge", _dodge);

        PrintIfNotZero("Summon Number", _summonNumber);
        PrintIfNotZero("Summon Dmg", _summonDmg);
        PrintIfNotZero("Summon Res", _summonRes);
        PrintIfNotZero("Summon CC", _summonCc);
        PrintIfNotZero("Summon Tackle", _summonTackle);
        PrintIfNotZero("Summon HP", _summonHp);
        
        Console.WriteLine("\n");
    }
}