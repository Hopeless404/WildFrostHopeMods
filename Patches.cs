using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;
using Il2CppSystem.Linq;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Newtonsoft.Json.Utilities;
using System.Runtime.CompilerServices;

namespace WildFrostHopeMods;

public class Patches
{
    internal static GeneralModifier im = GeneralModifier.im;

    internal static List<Func<Entity, Entity, int, string, Action<Hit>>> eventsWhenHit = new List<Func<Entity, Entity, int, string, Action<Hit>>>();
    internal static List<Func<Entity, Entity, int, string, Action<Hit>>> eventsOnHit = new List<Func<Entity, Entity, int, string, Action<Hit>>>();
    internal static List<Func<Entity, Entity, int, string, Action<Hit>>> eventsOnHitPre = new List<Func<Entity, Entity, int, string, Action<Hit>>>();
    internal static List<Func<Entity, Action>> eventsTryDrag = new List<Func<Entity, Action>>();
    internal static List<Func<Entity, Action<Entity>>> eventsProcessUnits = new List<Func<Entity, Action<Entity>>>();
    internal static List<Func<Entity, Action<Entity>>> eventsOnPlace = new List<Func<Entity, Action<Entity>>>();
    internal static List<Func<StatusEffectApply, Action<StatusEffectApply>>> eventsOnStatusApplied = new List<Func<StatusEffectApply, Action<StatusEffectApply>>>();

    internal static BattleSetUp battleSetUp;
    internal static BattleLogSystem battleLog;
    internal static CardController cardController;
    internal static CardCharmDragHandler dragHandler;
    internal static DeckDisplaySequence deckDisplay;
    internal static GameMode gameMode;

    internal static List<ClassData> ClassDataAdditions = new List<ClassData>();

    #region HarmonyPatches

    public class HopePatches : MonoBehaviour
    {
        #region CharacterSelect patches

        [HarmonyPatch(typeof(CharacterSelectScreen), "Start")]
        class Patch
        {
            static void Postfix(CharacterSelectScreen __instance)
            {
                gameMode ??= AddressableLoader.groups["GameMode"].lookup["GameModeNormal"].Cast<GameMode>();

                foreach (var classData in ClassDataAdditions)
                {
                    if (gameMode.classes.ToList().Find(a => a.name == classData.name) == null)
                    {
                        //__instance.leaderSelection.options += 1;
                        //__instance.leaderSelection.differentTribes += 1;
                        __instance.options += 1;
                        __instance.differentTribes += 1;
                        gameMode.classes = gameMode.classes.ToArray().AddItem(classData).ToRefArray();
                    }
                }
            }
        }






        [HarmonyPatch(typeof(Town), "Start")]
        class TownStart
        {
            static void Prefix()
            {
                if (gameMode?.classes.Count > 3)
                    gameMode.classes = new Il2CppReferenceArray<ClassData>(gameMode.classes.RangeSubset(0, 3));
            }
        }
    #endregion


        #region Battle patches
        [HarmonyPatch(typeof(BattleSetUp), "Run")]
        class BattleSetUpRun
        {
            static void Postfix(BattleSetUp __instance)
            {
                
                battleSetUp = __instance;
                var battle = battleSetUp.battle;
                im.Print("battle set up has run");
                if (battle != null)
                {
                    im.Print("non null battle");
                    //im.Print(Battle.GetCards(battle.enemy).Count);
                    //im.Print(battleSetUp.enemy.reserveContainer.Count);
                }
            }
        }

        


        [HarmonyPatch(typeof(Battle), nameof(Battle.EntityCreated))]
        class BattleEntityCreated
        {
            static void Postfix(Entity entity, Battle __instance)
            {
                var battle = __instance;
                if (battle != null)
                    if (entity._data != null)
                    {
                        // Change name based on charms
                        if (entity.owner == battle.enemy && entity._data.upgrades.Count > 0)
                        {
                            var charmNames = string.Join(" ", entity._data.upgrades.ToArray().Select(charm => charm.titleKey.GetLocalizedString().Replace(" Charm", "").Adjectivise())).Replace("Crown".Adjectivise()+" ", "");
                            if (!entity._data.titleKey.GetLocalizedString().Contains(charmNames)) entity._data = entity._data.SetTitle($"{charmNames} {entity.data.titleKey.GetLocalizedString()}");
                        }

                        // Strengthen pulse animation when counter at 1
                        entity.imminentAnimation.strength = 0.5f;
                    }
            }
        }


        // these run during campaign generation
        //[HarmonyPatch(typeof(ScriptUpgradeEnemies), "Run")]
        class ScriptUpgradeEnemiesRun
        {
            static void Postfix(ScriptUpgradeEnemies __instance)
            {
                im.Print("upgrading enemies");
            }
        }

        //[HarmonyPatch(typeof(ScriptUpgradeEnemies), nameof(ScriptUpgradeEnemies.TryAddUpgrade), new Type[] {typeof(BattleWaveManager.WaveData), typeof(int)})]
        class ScriptUpgradeEnemiesTryAddUpgrade
        {
            static void Postfix(bool __result, ref BattleWaveManager.WaveData wave, int cardIndex)
            {
                im.Print("trying to upgrade enemies. success?" + __result);
            }
        }



        //[HarmonyPatch(typeof(BattleLogSystem), nameof(BattleLogSystem.LogHit))]
        class eventHit
        {
            static void Postfix(Entity attacker, Entity target, int damage, string damageType)
            {
                foreach (var eventOnHit in eventsOnHit)
                {
                    var action = eventOnHit(attacker, target, damage, damageType);
                    //action(attacker);
                    //action(target);
                }
                foreach (var eventWhenHit in eventsWhenHit)
                {
                    var action = eventWhenHit(attacker, target, damage, damageType);
                    //action(attacker);
                    //action(target);
                }
            }
        }

        // this seems to trigger during processunits, but not for things that normally don't have counter?
        // it actually doesn't work with tainted spike, maybe that's it
        //[HarmonyPatch(typeof(Events), nameof(Events.InvokeEntityHit))]
        [HarmonyPatch(typeof(BattleLogSystem), nameof(BattleLogSystem.Hit))]
        class eventHit2
        {
            static void Prefix(Hit hit)
            {
                if (hit.countsAsHit && hit.attacker != null)
                {
                    Entity attacker = hit.attacker;
                    Entity target = hit.target;
                    int damage = hit.damage;
                    string damageType = hit.damageType;
                    foreach (var eventOnHit in eventsOnHitPre)
                    {
                        var action = eventOnHit(attacker, target, damage, damageType);
                        action(hit);
                    }
                }
            }
            static void Postfix(Hit hit)
            {
                if (hit.countsAsHit && hit.attacker != null)
                {
                    Entity attacker = hit.attacker;
                    Entity target = hit.target;
                    int damage = hit.damage;
                    string damageType = hit.damageType;
                    foreach (var eventOnHit in eventsOnHit)
                    {
                        var action = eventOnHit(attacker, target, damage, damageType);
                        action(hit);
                    }
                    foreach (var eventWhenHit in eventsWhenHit)
                    {
                        var action = eventWhenHit(attacker, target, damage, damageType);
                        action(hit);
                    }
                }

            }
        }



        // simple patch methods break savecollection
        // by automatically converting SaveCollection<BattleWaveManager.WaveList> to SaveCollection<>
        //[HarmonyPatch(typeof(BattleGenerationScriptWaves), nameof(BattleGenerationScriptWaves.Run))]
        class TestPrefixingBattlesOnCreation
        {
            static void Postfix(ref BattleData battleData, int points)//, ref SaveCollection<T> __result)
            {
                im.Print("postfix");
            }
        }


        //[HarmonyPatch(typeof(CardController), nameof(CardController.TryDrag))] //I would prefer to use this with CardControllerBattle but it doesn't recognise the method
        [HarmonyPatch(typeof(Events), nameof(Events.InvokeEntitySelect))]
        class TouchPress
        {
            static void Postfix(Entity entity)
            {
                if (Battle.instance != null)
                {
                    //cardController = Battle.instance.playerCardController;
                    var se = entity.statusEffects.Find(se => se.type == "sniper")?.Cast<StatusEffectBombard>();
                    if (se != null && entity.owner == Battle.instance.player)
                    {
                        var index = Battle.instance.allSlots.IndexOf((Func<CardSlot, bool>)(slot => slot == se.targetList[0]));
                        while (se.targetList[0] != Battle.instance.allSlots.ToList()[6 + ((index + 1) % 6)])
                            se.SetTargets().MoveNext(); // this causes the sound to fluctuate a bit
                                                        // if it's really a problem then figure out the eventref for targeting sfx

                        //se.targetList[0] = Battle.instance.allSlots.ToList()[(index+1) % 12];
                        // this is another method, but the visual indicator doesn't update
                        // if I can figure out how to update that, this is much cleaner
                    }
                }
            }
        } // fun note: Battle.instance.rows[Battle.instance.enemy][0].Count gives the number of enemies in the first row



        [HarmonyPatch(typeof(Events), nameof(Events.InvokeEntityPlace))]
        class EntityPlace
        {
            static void Postfix(Entity entity, Il2CppReferenceArray<CardContainer> containers, bool freeMove)
            {
                im.Print("Entity placed");
                if (Battle.instance != null)
                {
                    foreach (var eventOnPlace in eventsOnPlace)
                    {
                        var action = eventOnPlace(entity);
                        action(entity);
                    }
                }
            }
        }


        [HarmonyPatch(typeof(BattleLogSystem), nameof(BattleLogSystem.StatusApplied))]
        class StatusApplied
        {
            static void Postfix(StatusEffectApply apply)
            {
                im.Print("effect applied");
                if (Battle.instance != null)
                {
                    foreach (var eventOnStatusApplied in eventsOnStatusApplied)
                    {
                        var action = eventOnStatusApplied(apply);
                        action(apply);
                    }
                }
            }
        }

        




        [HarmonyPatch(typeof(Battle), nameof(Battle.ProcessUnit))]
        class processUnit
        {
            static void Prefix(ref Entity unit)
            {
                //im.Print(unit.name);
                foreach (var eventProcessUnit in eventsProcessUnits)
                {
                    var action = eventProcessUnit(unit);
                    action(unit);
                }
            }
        }
        #endregion


        #region DeckPack patches
        [HarmonyPatch(typeof(DeckDisplaySequence), "Run")]
        class DeckDisplaySequenceRun
        {
            static void Postfix(DeckDisplaySequence __instance)
            {
                deckDisplay = __instance;
            }
        }


        // Charm preview
        [HarmonyPatch(typeof(CardCharmDragHandler), nameof(CardCharmDragHandler.EntityHover))]
        class UpgradeAssign
        {
            static void Postfix(Entity entity)
            {
                dragHandler = deckDisplay.charmDragHandler;
                //dragHandler.instantAssign = true;
                if (dragHandler.dragging != null) im.Print("hovering over " + $"{entity.name}" + dragHandler.dragging.data.name);
                //CoroutineManager.Start(dragHandler.Assign(dragHandler.dragging, entity));//.MoveNext();

            }
        }

        #endregion


        #region CardScript patches

        [HarmonyPatch(typeof(CardScriptAddRandomHealth), nameof(CardScriptAddRandomHealth.Run))]
        class CardScriptAddRandomHealthRun
        {
            static void Prefix(out int __state, CardData target)
            {
                __state = target.hp;
            }
            static void Postfix(int __state, CardData target)
            {
                im.Print($"Added {target.hp - __state} to {target.name}");
            }

        }


        #endregion



        #endregion
    }

}