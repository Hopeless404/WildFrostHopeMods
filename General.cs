using System.Collections;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Unity.IL2CPP.Utils;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;
using WildfrostModMiya;
using Il2CppSystem.Reflection;
using Il2CppSystem.Linq;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using static StatusEffectApplyX;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.Localization;
using FMOD;
using Newtonsoft.Json.Utilities;
using Rewired.Utils;

namespace WildFrostHopeMods;

[BepInPlugin("WildFrost.Hope.GeneralModifiers", "General Modifiers", "0.1.1.0")]
public class GeneralModifier : BasePlugin
{
    internal static GeneralModifier Instance;
    internal static GeneralModifier im = new GeneralModifier();

    //MiscTools.Clone(obj);
    



    // i put most stuff in here
    public class Behaviour : MonoBehaviour
    {

        PauseMenu menu = new PauseMenu();
        private void Start()
        {
            this.StartCoroutine(LoadAllGroups());
            //this.StartCoroutine(General());
            //this.StartCoroutine(CardAdditions());
        }

        void Update()
        {



            // Press ` but not CTRL
            if (Input.GetKeyDown("`") && !Input.GetKey(KeyCode.LeftAlt))
            {
                CampaignInfo();
            }


            // ALT commands
            if (Input.GetKey(KeyCode.LeftAlt))
            {
                // ALT+1: 
                if (Input.GetKeyDown("1"))
                {
                    //Campaign.Data.ToString();
                    //Campaign.instance.nodes[12].seed = 
                    MapNode node = FindObjectsOfType<MapNode>().ToList()
                        .Find(a => a.IsHovered)
                        .Cast<MapNode>();
                    node.campaignNode.seed = Dead.Random.Seed();
                    im.Print("Hover node: " + node.campaignNode.id);



                }

                // ALT+2: 
                if (Input.GetKeyDown("2"))
                {
                    var gold = ScriptableAmount.CreateInstance<ScriptableGold>();
                    gold.factor = 1;

                    if (Battle.instance != null)
                    {
                        var entityList = Battle.GetCardsOnBoard();
                        foreach (var entity in entityList)
                        {
                            im.Print(entity.data.cardType.name);
                            if (entity.data.cardType.name == "Enemy")
                            {
                                im.Print(entity.data.name + " drops " + gold.Get(entity));
                            }
                        }
                    }
                }

                if (Input.GetKeyDown("3"))
                {
                    GameObject go = FindObjectsOfType<GameObject>(includeInactive: true).ToList()
                        .Find(a => a.name == "LilBerry")
                        .Cast<GameObject>();
                    im.Print(go.name);
                }

                // ALT+R: Reloar campaign ("The R Key")
                if (Input.GetKeyDown("r"))
                    QuickRestart();

                if (Input.GetKeyDown("s"))
                {
                    //SaveImageFiles();
                    PauseMenu menu = new PauseMenu();
                    AddressableLoader.LoadGroup("CardData");
                    ExportCards exporter = new ExportCards();
                    //this.StartCoroutine(exporter.Start());
                }

                if (Input.GetKeyDown("z"))
                {
                    var source = this.GetComponents<Component>().ToList()
                        .Find(a => a.TryCast<AudioSource>() != null)
                        .Cast<AudioSource>(); im.Print("finding the listener");
                    var audio = Extensions.LoadAudioFromCardPortraits("CardPortraits\\testSound");
                    source.clip = audio;
                    source.Play();
                    //AddressableLoader.groups["GameModifierData"].list.ToArray().First().Cast<GameModifierData>().PlayRingSfx();
                }
            }
            // End of ALT commands

            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown("r"))
                menu.GoToTown();


        }

        // START OF EXTRA COMMANDS


       

        internal IEnumerator LoadAllGroups()
        {
            CoroutineManager.Start(SceneManager.Load("Console", SceneType.Persistent));
            StartCoroutine(AddressableLoader.LoadGroup("CardUpgradeData"));
            StartCoroutine(AddressableLoader.LoadGroup("KeywordData"));
            StartCoroutine(AddressableLoader.LoadGroup("TraitData"));
            StartCoroutine(AddressableLoader.LoadGroup("BattleData"));
            StartCoroutine(AddressableLoader.LoadGroup("StatusEffectData"));

            yield return new WaitUntil((Func<bool>)(() => SceneManager.IsLoaded("Town")));
            StartCoroutine(AddressableLoader.LoadGroup("CardData"));
            StartCoroutine(AddressableLoader.LoadGroup("GameModifierData"));
        }

        internal static void CampaignInfo()
        {
            im.Print("Showing Campaign node info!\n");
            var shop = ScriptableObject.CreateInstance<CampaignNodeType>();
            foreach (var node in Campaign.instance.nodes)
            {
                if (node.type.name == "CampaignNodeItem")
                {
                    im.Print($"[Item {node.id}] info: {node.seed}");
                    var cards = node.data["cards"].Cast<SaveCollection<String>>();
                    im.Print($"items: {string.Join(", ", cards.collection.ToArray().Select(name => name.ToCardData().titleKey.GetLocalizedString()))}\n");
                }

                if (node.type.name == "CampaignNodeCompanion")
                {
                    im.Print($"[Frozen Travellers {node.id}] info: {node.seed}");
                    var cards = node.data["cards"].Cast<SaveCollection<String>>();
                    im.Print($"units: {string.Join(", ", cards.collection.ToArray().Select(name => name.ToCardData().titleKey.GetLocalizedString()))}\n");
                }

                if (node.type.name == "CampaignNodeCharm")
                {
                    im.Print($"[Charm {node.id}] info:  {node.seed}");
                    im.Print($"charm: {node.data["charm"].ToString().ToCardUpgradeData().titleKey.GetLocalizedString().Replace(" Charm","")}\n");
                }

                if (node.type.name == "CampaignNodeMuncher")
                {
                    im.Print($"[Muncher {node.id}]\n");
                }

                if (node.type.name == "CampaignNodeGold")
                {
                    im.Print($"[Gold {node.id}] info: {node.seed}");
                    im.Print($"gold: {node.data["amount"].GetType()}\n");
                }

                if (node.type.name == "CampaignNodeShop")
                {
                    im.Print($"[The Woolly Snail {node.id}] info: {node.seed}");
                    var shopData = node.data["shopData"].Cast<ShopRoutine.Data>();
                    foreach (var item in shopData.items)
                    {
                        var card = Extensions.CardDataLookup(item.cardDataName);
                        im.Print($"item: {card.titleKey.GetLocalizedString()} {item.price}");
                    }
                    im.Print($"charms: {string.Join(", ", shopData.charms.ToArray().Select(name => name.ToCardUpgradeData().titleKey.GetLocalizedString().Replace(" Charm", "")))}\n");
                }

            }

            im.Print(Campaign.Data.Seed);
        }

        internal static void QuickRestart()
        {
            PauseMenu menu = new PauseMenu();
            if (SceneManager.ActiveSceneName == "CharacterSelect")
            {
                var o = FindObjectOfType<SelectLeader>();
                //ProgressSaveData data = new ProgressSaveData();
                //var seed = data.nextSeed;
                var seed = Dead.Random.Seed();
                //o.options = 0;
                o.SetSeed(seed);
                o.Reroll();
                im.Print("Current seed is " + o.seed);
            }
            else if (true) //(Campaign.instance != null && SceneManager.ActiveSceneName != "Campaign")
            {
                menu.QuickRestart();
                var o = FindObjectOfType<SelectLeader>();
                im.Print("Current seed is " + o.seed);
            }
            else
                im.Print("Please use the Reroll button instead!");
        }

        internal IEnumerator CardAdditions()
        {
            yield return new WaitUntil((Func<bool>)(() =>
            AddressableLoader.IsGroupLoaded("CardData")));

            Modfight();             // Custom battle + fallback enemy leader
            LilBerrySummoner();     // Customised summon effect on trigger
            Stinkbug();             // While Active effects (not working with FrontEnemy flag)
            CorpseEater();          // "Moving" a card from hand to board (changed slightly)
            Tarblade(); // wip      // Custom charm
            Witch();                // Using Actions to change effect stacks (current X)
            Bellist();              // A lot of things
            TaintedSpike();         // Lose stacks of specific effects
            Sniper();               // Clicky thingy

            //HopeTribe();            // Implementing a new tribe. bugs to fix: can't continue or give up

            // WIPs
            //Hellbender();           // Endure: Survive a fatal attack
            //Hellfire();             // need to figure out how to apply when it has injured trait
            //KnightSummoner();       // NextPhase effects (not working/clean yet)
            //Ouroboros();            // moving status effects to summon
            //Fleet();                // Fleeting: When all stacks are lost, die
            //TreadmillFight();
        }

        #region WIP stuff



        // to test carrying over things to summon
        internal void Ouroboros()
        {
            var seWhenSacdApplyTrait = Extensions.CreateStatusEffectData<StatusEffectApplyToSummon>("Hope", "When Sacrificed Apply Sacrificial + 1 To Summon",
                type: "sacrificial", stackable:false).SetText("sacrifice effect {a}");
            //this shouldn't be stackable for some reason

            var keywordSacrificial = Extensions.CreateKeywordData("", "Sacrificial"
            , title: "Sacrificial", desc: "Can be sacrificed this number of times")
            .Set("showName", true).Set("canStack", true)
            .RegisterInGroup();

            var traitSacrificial = Extensions.CreateTraitData("Hope", "Sacrificial",
                keyword: keywordSacrificial, // works with barrage
                effects: new StatusEffectData[] { seWhenSacdApplyTrait });

            var seApplyTrait = Extensions.CreateStatusEffectData<StatusEffectTemporaryTrait>("Hope", "Temporary Sacrificial",
                stackable:false)
                .Set("isKeyword", true).Set("trait", traitSacrificial);

            var seInstantApplyTrait = Extensions.CreateStatusEffectData<StatusEffectApplyXInstant>("Hope", "Instant Temporary Sacrificial",
                effectToApply: seApplyTrait, applyToFlags: ApplyToFlags.Self)
                .Set("applyEqualAmount", true).Set("separateActions", true)
                .Set("waitForAnimationEnd", false).Set("waitForApplyToAnimationEnd", false)
                .AddContextEqualAmount(Extensions.CreateScriptableAmount<ScriptableCurrentStatus>(statusType:"sacrificial", offset: 0));

            seWhenSacdApplyTrait.effectToApply = seInstantApplyTrait;


            var summonCorpseEater = Extensions.CreateStatusEffectData<StatusEffectSummon>("Hope", "Summon Corpse Eater");

            var TestInstantSummon = Extensions.CreateStatusEffectData<StatusEffectInstantSummon>("Hope", "Instant Summon Corpse Eater",
                position: StatusEffectInstantSummon.Position.InFrontOf, effectToApply: summonCorpseEater);

            var TestSummonWhen = Extensions.CreateStatusEffectData<StatusEffectApplyXWhenDestroyed>("Hope", "Summon Corpse Eater When Destroyed",
                effectToApply: TestInstantSummon, applyToFlags: ApplyToFlags.Self)
                .Set("eventPriority", 99999);

            var c = CardAdder.CreateCardData("Hope", "lily")
                .SetTitle("Lily")
                .SetIsUnit()
                .SetCanPlay(CardAdder.CanPlay.CanPlayOnEnemy | CardAdder.CanPlay.CanPlayOnBoard)
                .SetSprites("CardPortraits\\SunPortrait", "CardPortraits\\testBackground")
                .SetStats(10, 0, 1)
                .SetBloodProfile(CardAdder.VanillaBloodProfiles.BloodProfilePinkWisp)
                .SetIdleAnimationProfile(CardAdder.VanillaCardAnimationProfiles.GoopAnimationProfile)
                .SetTraits(traitSacrificial.TraitStack(1))
                .SetStartWithEffects(TestSummonWhen.StatusEffectStack(1));

            summonCorpseEater.summonCard = c;
            c.RegisterInGroup();

        }

        internal void Copyboros()
        {
            var seWhenSacdSummonCopy = Extensions.CreateStatusEffectData<StatusEffectApplyToSummon>("Hope", "When Sacrificed Apply Sacrificial + 1 To Summon",
                type: "sacrificial", stackable:false).SetText("sacrifice effect {a}");
            //this shouldn't be stackable for some reason

            var keywordSacrificial = Extensions.CreateKeywordData("", "Sacrificial"
            , title: "Sacrificial", desc: "Can be sacrificed this number of times")
            .Set("showName", true).Set("canStack", true)
            .RegisterInGroup();

            var traitSacrificial = Extensions.CreateTraitData("Hope", "Sacrificial",
                keyword: keywordSacrificial, // works with barrage
                effects: new StatusEffectData[] { seWhenSacdSummonCopy });

            var seApplyTrait = Extensions.CreateStatusEffectData<StatusEffectTemporaryTrait>("Hope", "Temporary Sacrificial",
                stackable:false)
                .Set("isKeyword", true).Set("trait", traitSacrificial);

            var seInstantApplyTrait = Extensions.CreateStatusEffectData<StatusEffectApplyXInstant>("Hope", "Instant Temporary Sacrificial",
                effectToApply: seApplyTrait, applyToFlags: ApplyToFlags.Self)
                .Set("applyEqualAmount", true)
                .Set("stackable", false).Set("separateActions", true)
                .Set("waitForAnimationEnd", false).Set("waitForApplyToAnimationEnd", false)
                .AddContextEqualAmount(Extensions.CreateScriptableAmount<ScriptableCurrentStatus>(statusType: "sacrificial", offset: 0));



            var summonCorpseEater = Extensions.CreateStatusEffectData<StatusEffectSummon>("Hope", "Summon Corpse Eater");

            var TestInstantSummon = Extensions.CreateStatusEffectData<StatusEffectInstantSummon>("Hope", "Instant Summon Corpse Eater",
                position: StatusEffectInstantSummon.Position.InFrontOf, effectToApply: summonCorpseEater);

            var TestSummonWhen = Extensions.CreateStatusEffectData<StatusEffectApplyXWhenDestroyed>("Hope", "Summon Corpse Eater When Destroyed",
                effectToApply: TestInstantSummon, applyToFlags: ApplyToFlags.Self)
                .Set("eventPriority", 99999);

            var c = CardAdder.CreateCardData("Hope", "lily")
                .SetTitle("Lily")
                .SetIsUnit()
                .SetCanPlay(CardAdder.CanPlay.CanPlayOnEnemy | CardAdder.CanPlay.CanPlayOnBoard)
                .SetSprites("CardPortraits\\SunPortrait", "CardPortraits\\testBackground")
                .SetStats(10, 0, 1)
                .SetBloodProfile(CardAdder.VanillaBloodProfiles.BloodProfilePinkWisp)
                .SetIdleAnimationProfile(CardAdder.VanillaCardAnimationProfiles.GoopAnimationProfile)
                .SetTraits(traitSacrificial.TraitStack(1))
                .SetStartWithEffects(TestSummonWhen.StatusEffectStack(1));

            summonCorpseEater.summonCard = c;
            c.RegisterInGroup();

        }

        internal void Fleet()
        {
            var seFleetCount = Extensions.CreateStatusEffectData<StatusEffectApplyXWhenDamageTaken>("Hope", "Fleeting Count").Set("type", "fleeting");

            var seReducer = Extensions.CreateStatusEffectData<StatusEffectInstantLoseX>("", "Instant Lose Fleeting", type:"lose fleeting")
                .Set("statusToLose", seFleetCount)
                .SetText("Lose <{a}> <Fleeting>");

            var seFleeter = Extensions.CreateStatusEffectData<StatusEffectApplyXWhenDamageTaken>("Hope", "Fleeting",
                effectToApply: seReducer, applyToFlags: ApplyToFlags.Self)
                .Set("stackable", false)
                .SetText("When damaged, lose fleeting");

            var kFleeting = Extensions.CreateKeywordData("", "Fleeting"
            , title: "Fleeting", desc: "This card can play this number of times")
            .Set("showName", true).Set("canStack", true)
            .RegisterInGroup();

            var tFleeting = Extensions.CreateTraitData("Hope", "Fleeting",
                keyword: kFleeting,
                effects: new StatusEffectData[] { seFleetCount, seFleeter })
                .RegisterInGroup();

            var c = CardAdder.CreateCardData("Hope", "Fleet")
                .SetTitle("Fleet")
                .SetIsUnit()
                .SetCanPlay(CardAdder.CanPlay.CanPlayOnEnemy | CardAdder.CanPlay.CanPlayOnBoard)
                .SetSprites("CardPortraits\\SunPortrait", "CardPortraits\\testBackground")
                .SetStats(10, 10, 10)
                .SetBloodProfile(CardAdder.VanillaBloodProfiles.BloodProfilePinkWisp)
                .SetIdleAnimationProfile(CardAdder.VanillaCardAnimationProfiles.GoopAnimationProfile)
                .SetTraits(tFleeting.TraitStack(3))
                .SetStartWithEffects()//seFleeter.StatusEffectStack(1))
                .RegisterInGroup();
        }

        //wip
        internal void Hellbender()
        {
            var Health = Extensions.CreateStatusEffectData<StatusEffectInstantSetHealth>("Hope", "Health", equalAmount: true).SetText("Retain 1 Health").Set("eventPriority", -10);
            var DeployHealth = Extensions.CreateStatusEffectData<StatusEffectApplyXWhenDeployed>("Hope", "DeployHealth", effectToApply: Health, applyToFlags: StatusEffectApplyX.ApplyToFlags.Self);
            
            var weak = CardAdder.CreateCardData("Hope", "Weak")
                .SetTitle("Weak")
                .SetIsUnit()
                //.AddToPool(CardAdder.VanillaRewardPools.BasicItemPool) debug cards shouldn't be in pools
                .SetCanPlay(CardAdder.CanPlay.CanPlayOnEnemy | CardAdder.CanPlay.CanPlayOnBoard)
                .SetSprites("CardPortraits\\SunPortrait", "CardPortraits\\testBackground")
                .SetStats(10, 0, 1)
                .SetBloodProfile(CardAdder.VanillaBloodProfiles.BloodProfilePinkWisp)
                .SetIdleAnimationProfile(CardAdder.VanillaCardAnimationProfiles.GoopAnimationProfile)
                .SetStartWithEffects(DeployHealth.StatusEffectStack(1))
                .RegisterInGroup();





            var RemoveEffects = Extensions.CreateStatusEffectData<StatusEffectRemoveEffects>("Hope", "Remove Effects");

            var WhenHitRemoveEffect = Extensions.CreateStatusEffectData<StatusEffectApplyXWhenDamageTaken>("Hope", "When Damage Taken Remove",
                effectToApply: RemoveEffects, applyToFlags: ApplyToFlags.Self).SetText("Remove all effects")
                .Set("eventPriority", 99999).Set("stackable", false);
            

            var WhenHitHealthEffect = Extensions.CreateStatusEffectData<StatusEffectApplyXWhenDamageTaken>("Hope", "When Damage Taken DoSomething",
                effectToApply: Health, applyToFlags: ApplyToFlags.Self).SetText("When damaged, check health")
                .Set("preventDeath", true)
                .AddApplyConstraint(Extensions.CreateTargetConstraint<TargetConstraintHealthMoreThan>(not: true, value: 0));

            
            
            
            var keywordEndure = Extensions.CreateKeywordData("", "Endure"
            ,title: "Endure", desc: "While above 1 <keyword=health>, taking damage does not destroy this card")
            .Set("showName", true).Set("canStack", false)
            .RegisterInGroup(); // this is required

            var traitEndure = Extensions.CreateTraitData("Hope", "Endure",
                keyword: keywordEndure, // works with barrage
                effects: new StatusEffectData[] { WhenHitHealthEffect, WhenHitRemoveEffect }
                ).RegisterInGroup();

            var c = CardAdder.CreateCardData("Hope", "Hellbender")
                .SetTitle("Hellbender")
                .SetIsUnit()
                .SetCanPlay(CardAdder.CanPlay.CanPlayOnEnemy | CardAdder.CanPlay.CanPlayOnBoard)
                .SetSprites("CardPortraits\\SunPortrait", "CardPortraits\\testBackground")
                .SetStats(10, 0, 1)
                .SetBloodProfile(CardAdder.VanillaBloodProfiles.BloodProfilePinkWisp)
                .SetIdleAnimationProfile(CardAdder.VanillaCardAnimationProfiles.GoopAnimationProfile)
                .SetTraits(traitEndure.TraitStack(1))
                .RegisterInGroup();

            im.Print("Hellbender has been spawned");

        }


        // wip, make keyword and trait
        internal void Hellfire()
        {
            var c = CardUpgradeAdder.CreateCardUpgradeData("Hope", "Hellfire")
               .SetText("While <color=#FF002D>injured</color>, start battles with 75% <keyword=health> and <keyword=attack>")
               .SetTitle("Hellfire charm")
               .SetUpgradeType(CardUpgradeData.Type.Charm)
               .SetImage("CardPortraits\\TarCharm")
               .AddTargetConstraint(Extensions.CreateTargetConstraint<TargetConstraintHasHealth>())
               .AddScript(Extensions.CreateCardScript<CardScriptMultiplyHealth>(
                   multiply: 1.5f)
               ).RegisterInGroup();
        }


        #endregion

        #region Mostly functional

        internal void Modfight()
        {
            Extensions.CreateBattleData("Hope", "Battle", "Modfight!",
                pools: Extensions.CreateBattleWavePoolData("", "Battle", "Wave Pool 1",
                    waves: new BattleWavePoolData.Wave[0], //template.pools[0].waves,
                    unitList: new List<string>() { "Waddlegoons", "Waddlegoons", "Waddlegoons", "Waddlegoons", "Waddlegoons", "Waddlegoons" },
                    pullCount: 1
                    ).ToArray())
                .RegisterInGroup()
                .AddToTier(0);
                
        }

        internal void HopeTribe()
        {
            var hopeLeaders = new List<CardData>();
            foreach (var card in AddressableLoader.groups["CardData"].list) if (card.Cast<CardData>().name.StartsWith("Hope.")) hopeLeaders.Add(card.Cast<CardData>());

            var gameMode = AddressableLoader.groups["GameMode"].lookup["GameModeNormal"].Cast<GameMode>();
            
            var hopeUnitPool = Extensions.CreateRewardPool("Units", hopeLeaders.ToArray());
            Extensions.AddClass(Extensions.CreateClassData("HopeTribe", leaders: hopeLeaders.ToArray(), rewardPools: gameMode.classes[1].rewardPools.ToArray().AddToArray(hopeUnitPool)));
        }

        // example using predefined overload
        internal void LilBerrySummoner()
        {
            var TestSummon = Extensions.CreateStatusEffectData<StatusEffectSummon>("Hope", "Summon LilBerry",
                newCard: "LilBerry");
            var TestInstantSummon = Extensions.CreateStatusEffectData<StatusEffectInstantSummon>("Hope", "TestInstantSummon",
                position: StatusEffectInstantSummon.Position.InFrontOf, effectToApply: TestSummon).Set("visible", true);
            var TestSummonWhen = Extensions.CreateStatusEffectData<StatusEffectApplyXWhenDeployed>("Hope", "TestSummonWhenDeployed",
                effectToApply: TestInstantSummon, applyToFlags: StatusEffectApplyX.ApplyToFlags.Self);
            im.Print("TestSummonWhen successful");

            var c = CardAdder.CreateCardData("Hope", "Summoner")
                .SetTitle("Summoner")
                .SetIsUnit()
                //.AddToPool(CardAdder.VanillaRewardPools.BasicItemPool) debug cards shouldn't be in pools
                .SetCanPlay(CardAdder.CanPlay.CanPlayOnEnemy | CardAdder.CanPlay.CanPlayOnBoard)
                .SetSprites("CardPortraits\\SunPortrait", "CardPortraits\\testBackground")
                .SetStats(5, null, 1)
                .SetBloodProfile(CardAdder.VanillaBloodProfiles.BloodProfilePinkWisp)
                .SetIdleAnimationProfile(CardAdder.VanillaCardAnimationProfiles.GoopAnimationProfile)
                .SetStartWithEffects(TestSummonWhen.StatusEffectStack(1))
                .RegisterInGroup();

        }

        // changed applyToFlag
        internal void Stinkbug()
        {
            var se = Extensions.CreateStatusEffectData<StatusEffectWhileActiveX>("Hope", "While Active Reduce Attack To EnemiesInRow",
                effectToApply: CardAdder.VanillaStatusEffects.OngoingReduceAttack.StatusEffectData(),
                applyToFlags: StatusEffectApplyX.ApplyToFlags.EnemiesInRow,
                stackable:true, canBeBoosted:true)
                .SetText("While active, reduce {0} of enemies in row by <{a}>")
                .Set("hiddenKeywords", AddressableLoader.groups["KeywordData"].lookup["active"].Cast<KeywordData>().ToRefArray())
                .Set("targetMustBeAlive", true)
                .Set("affectOthersWithSameEffect", true)
                .RegisterInGroup();

            var c = CardAdder.CreateCardData("Hope", "Stinkbug")
                .SetTitle("Stinkbug")
                .SetIsUnit()
                .SetCanPlay(CardAdder.CanPlay.CanPlayOnEnemy | CardAdder.CanPlay.CanPlayOnBoard)
                .SetSprites("CardPortraits\\SunPortrait", "CardPortraits\\testBackground")
                .SetStats(2, 1, 4)
                .SetBloodProfile(CardAdder.VanillaBloodProfiles.BloodProfilePinkWisp)
                .SetIdleAnimationProfile(CardAdder.VanillaCardAnimationProfiles.GoopAnimationProfile)
                .SetStartWithEffects(se.StatusEffectStack(1))
                .RegisterInGroup();
        }

        //wip, balancing
        internal void CorpseEater()
        {
            var summonCorpseEater = Extensions.CreateStatusEffectData<StatusEffectSummon>("Hope", "Summon Corpse Eater");

            var TestInstantSummon = Extensions.CreateStatusEffectData<StatusEffectInstantSummon>("Hope", "Instant Summon Corpse Eater",
                position: StatusEffectInstantSummon.Position.InFrontOf, effectToApply: summonCorpseEater);

            var TestSummonWhen = Extensions.CreateStatusEffectData<StatusEffectApplyXWhenDestroyed>("Hope", "Summon Corpse Eater When Destroyed",
                effectToApply: TestInstantSummon, applyToFlags: ApplyToFlags.Self)
                .Set("eventPriority", 99999);

            var se = Extensions.CreateStatusEffectData<StatusEffectWhileInHandX>("Hope", "While in hand effect",
                effectToApply: TestSummonWhen, applyToFlags: ApplyToFlags.Allies);

            var c = CardAdder.CreateCardData("Hope", "Corpse Eater")
                .SetTitle("Corpse Eater")
                .SetIsUnit()
                .SetCanPlay(CardAdder.CanPlay.CanPlayOnEnemy | CardAdder.CanPlay.CanPlayOnBoard)
                .SetSprites("CardPortraits\\SunPortrait", "CardPortraits\\testBackground")
                .SetStats(2, 1, 4)
                .SetBloodProfile(CardAdder.VanillaBloodProfiles.BloodProfilePinkWisp)
                .SetIdleAnimationProfile(CardAdder.VanillaCardAnimationProfiles.GoopAnimationProfile)
                .SetStartWithEffects(se.StatusEffectStack(1))
                .SetText("While in hand, summon <card=Hope.Corpse Eater> when an alive ally dies");

            summonCorpseEater.summonCard = c;
            se = se.AddApplyConstraint(Extensions.CreateTargetConstraint<TargetConstraintIsSpecificCard>(
                    not: true, allowedCards: new CardData[] { c }))
                .AddApplyConstraint(Extensions.CreateTargetConstraint<TargetConstraintIsCardType>(
                    not: true, allowedTypes: new CardType[] { Extensions.CardTypeLookup("Clunker") }));
            c.RegisterInGroup();
        }

        internal void Tarblade()
        {
            var keyTar = Extensions.CreateKeywordData("", "Tar", "Tar", "Deal additional damage for each <Tar> card in hand. Counts as <Tar Blade>");
            var trTar = Extensions.CreateTraitData("Hope", "Tar", keyTar);

            var c = CardUpgradeAdder.CreateCardUpgradeData("Hope", "Tar")
                .SetText("Lose all <keyword=attack> and deal additional damage equal to <card=Dart> in hand")
                .SetTitle("Tar charm")
                .SetUpgradeType(CardUpgradeData.Type.Charm)
                .SetImage("CardPortraits\\TarCharm")
                .AddTargetConstraint(Extensions.CreateTargetConstraint<TargetConstraintDoesAttack>())
                .AddScript(Extensions.CreateCardScript<CardScriptReplaceAttackWithApply>(
                    effect: AddressableLoader.groups["StatusEffectData"].lookup["Bonus Damage Equal To Darts In Hand"].Cast<StatusEffectData>())
                ).RegisterInGroup();
        }

        internal void Witch()
        {
            var witchEffect = Extensions.CreateStatusEffectData<StatusEffectApplyXInstant>("", "Instant Apply Current Overload To Self",
                effectToApply: CardAdder.VanillaStatusEffects.Overload.StatusEffectData(),
                applyToFlags:ApplyToFlags.Self
                )
                .AddTargetConstraint(Extensions.CreateTargetConstraint<TargetConstraintCanBeHit>())
                //.AddContextEqualAmount(Extensions.CreateScriptableAmount<ScriptableCurrentStatus>(statusType: "overload"))
                //.AddScriptableAmount(Extensions.CreateScriptableAmount<ScriptableCurrentStatus>(statusType: "overload"))
                // context is the target's overload, scriptable is the applier's overload.
                // since it's applying onto itself, they're both the same in this case
                .SetText("Apply current <sprite name=overload>")
                .RegisterInGroup();

            var deployOverload = Extensions.CreateStatusEffectData<StatusEffectApplyXWhenDeployed>("", "When Deployed Overload To Self",
                effectToApply: CardAdder.VanillaStatusEffects.Overload.StatusEffectData(),
                applyToFlags:ApplyToFlags.Self,
                stackable:true, canBeBoosted:true)
                .SetText("When deployed, gain <{a}> <sprite name=overload>")
                .RegisterInGroup();

            var c = CardAdder.CreateCardData("Hope", "Witch")
                .SetTitle("Witch")
                .SetIsUnit()
                .SetCanPlay(CardAdder.CanPlay.CanPlayOnEnemy | CardAdder.CanPlay.CanPlayOnBoard)
                .SetSprites("CardPortraits\\SunPortrait", "CardPortraits\\testBackground")
                .SetStats(5, 1, 3)
                .SetBloodProfile(CardAdder.VanillaBloodProfiles.BloodProfilePinkWisp)
                .SetIdleAnimationProfile(CardAdder.VanillaCardAnimationProfiles.GoopAnimationProfile)
                .SetAttackEffects(witchEffect.StatusEffectStack(1))
                .SetStartWithEffects(deployOverload.StatusEffectStack(1))
                .SetTraits(CardAdder.VanillaTraits.Barrage.TraitStack(1))
                .RegisterInGroup()
                .AddPreTurnAction(unit =>
                    unit.attackEffects.Find(se => se.data == witchEffect).count = Extensions.CreateScriptableAmount<ScriptableCurrentStatus>(statusType: "overload").Get(unit)
                    );
        }

        internal void Bellist()
        {
            #region chime
            var chime = CardAdder.CreateCardData("", "Chime")
                .SetTitle("Chime")
                .SetIsUnit()
                .SetCanPlay(CardAdder.CanPlay.CanPlayOnEnemy | CardAdder.CanPlay.CanPlayOnBoard)
                .SetSprites("CardPortraits\\SunPortrait", "CardPortraits\\testBackground")
                .SetStats(3, null, 0)
                .SetBloodProfile(CardAdder.VanillaBloodProfiles.BloodProfileHusk)
                .SetIdleAnimationProfile(CardAdder.VanillaCardAnimationProfiles.SwayAnimationProfile);

            var SummonChime = Extensions.CreateStatusEffectData<StatusEffectSummon>("Hope", "Summon Chime").Set("summonCard", chime);
            var InstantSummonChime = Extensions.CreateStatusEffectData<StatusEffectInstantSummon>("Hope", "Instant Summon Chime",
                position: StatusEffectInstantSummon.Position.InFrontOf,
                effectToApply: SummonChime);
            var WhenDeployedChime = Extensions.CreateStatusEffectData<StatusEffectApplyXWhenDeployed>("Hope", "Summon Chime When Deployed",
                effectToApply: InstantSummonChime, applyToFlags: ApplyToFlags.Self, stackable:true);
            #endregion


            // target constraints acts on itself to be able to receive this effect
            // apply constraint acts on the applicant (here it is the chimes) with the effectToApply

            #region bellist

            var keyBellist = Extensions.CreateKeywordData("", "Bellist", 
                title: "Bellist", desc: "When deployed, spawn <card=Chime>\n\nTriggers when a <card=Chime> is struck")
            .Set("showName", true).Set("canStack", false).RegisterInGroup();
            var traitBellist = Extensions.CreateTraitData("Hope", "Bellist", 
                keyword: keyBellist, effects: new StatusEffectData[] { WhenDeployedChime }, isReaction: true).RegisterInGroup();

            var keycrush = AddressableLoader.groups["KeywordData"].lookup["crush"].Cast<KeywordData>();
            var crush = Extensions.CreateTraitData("", "Crush", keyword:keycrush, 
                effects:CardAdder.VanillaStatusEffects.Crush.StatusEffectData().Cast<StatusEffectCrush>().ToRefArray());

            var WhenChimeTrigger = Extensions.CreateStatusEffectData<StatusEffectApplyXWhenHit>("Hope", "When Hit Trigger Bellists",
                effectToApply: CardAdder.VanillaStatusEffects.Trigger.StatusEffectData().Cast<StatusEffectInstantTrigger>(),
                applyToFlags: ApplyToFlags.Allies).Set("eventPriority", 999999).Set("doPing", true).Set("queue", true)
                .AddApplyConstraint(Extensions.CreateTargetConstraint<TargetConstraintHasTrait>(trait: traitBellist));

            #endregion

            #region daus
            var conditionalSummon = Extensions.CreateStatusEffectData<StatusEffectApplyXWhenHit>("Hope", "When Hit Summon Chime",
                effectToApply: InstantSummonChime, applyToFlags: ApplyToFlags.Self, stackable:true, equalAmount:true)
                //.Set("mode", StatusEffectApplyXEveryTurn.Mode.TurnStart)
                .SetText("When hit, summon <card=Chime> if none are on the board")
                .RegisterInGroup();

            var bellist = CardAdder.CreateCardData("Hope", "Daus")
                .SetTitle("Daus")
                .SetIsUnit()
                .SetCanPlay(CardAdder.CanPlay.CanPlayOnEnemy | CardAdder.CanPlay.CanPlayOnBoard)
                .SetSprites("CardPortraits\\SunPortrait", "CardPortraits\\testBackground")
                .SetStats(4, 4, 0)
                .SetBloodProfile(CardAdder.VanillaBloodProfiles.BloodProfileHusk)
                .SetIdleAnimationProfile(CardAdder.VanillaCardAnimationProfiles.ShakeAnimationProfile)
                .SetTraits(traitBellist.TraitStack(1))
                .SetStartWithEffects(WhenDeployedChime.StatusEffectStack(1), conditionalSummon.StatusEffectStack(1))
                .RegisterInGroup();

            #endregion

            chime = chime.SetStartWithEffects(WhenChimeTrigger.StatusEffectStack(1))
                .RegisterInGroup().AddWhenHitPlayRingSFX();

            bellist.AddOnHitPreAction(entity => 
                entity.statusEffects.Find(se => se.data.name == conditionalSummon.name).count = (Battle.GetCardsOnBoard(Battle.instance.player).Find(entity => entity.name == chime.name) != null) ? 0 : 1
            );

            #region beller charm

            var c = CardUpgradeAdder.CreateCardUpgradeData("Hope", "Bellist")
                .SetText("Gain <keyword=bellist>")
                .SetTitle("Bell charm")
                .SetUpgradeType(CardUpgradeData.Type.Charm)
                .SetImage("CardPortraits\\TarCharm")
                .AddTargetConstraint(Extensions.CreateTargetConstraint<TargetConstraintDoesAttack>())
                .AddScript(Extensions.CreateCardScript<CardScriptAddTrait>(
                    trait: traitBellist, range: new Vector2Int(1,1)
                ))
                .RegisterInGroup();

            #endregion

            

            //eventHitsounds.Add(new Func<Entity, Entity, int, string, Action>((attacker, target, damage, damageType) =>
            //    {
            //        if (target.name == "Chime")
            //            return () => AddressableLoader.groups["GameModifierData"].list.ToArray().First().Cast<GameModifierData>().PlayRingSfx();
            //        return () => { };
            //    })
            //);
        }

        // the dumb realisation there is literally an effect to lose stacks of other specific effects
        internal void TaintedSpike()
        {
            var gainTeeth = Extensions.CreateStatusEffectData<StatusEffectApplyXOnCardPlayed>("", "On Card Played Apply Teeth To Self",
                effectToApply: CardAdder.VanillaStatusEffects.Teeth.StatusEffectData(),
                applyToFlags: ApplyToFlags.Self)
                .SetText("Gain <{a}> <sprite name=teeth>");

            var fakeReduce = Extensions.CreateStatusEffectData<StatusEffectInstantLoseX>("", "Instant Lose Teeth", type: "lose teeth")
                .Set("statusToLose", CardAdder.VanillaStatusEffects.Teeth.StatusEffectData())
                //.Set("doPing", true) // debugging
                .SetText("Lose <{a}> <sprite name=teeth>");

            var hitEffect = Extensions.CreateStatusEffectData<StatusEffectApplyXWhenHit>("", "When Hit Lose Teeth", type: "reducer",
                effectToApply : fakeReduce,
                applyToFlags:ApplyToFlags.Self)
                .SetText("When hit, reduce <sprite name=teeth> by <{a}>");

            var c = Extensions.CardDataLookup("Jagzag")
                .SetStats(8, null, 2)
                .SetStartWithEffects(CardAdder.VanillaStatusEffects.Teeth.StatusEffectStack(1),
                    gainTeeth.StatusEffectStack(1),
                    hitEffect.StatusEffectStack(1))
                .SetTraits()
                .RegisterInGroup();
                // Originally I wrote this without knowing there was literally a LoseEffect effect lmao I feel dumb

                //.AddWhenHitAction(hit =>
                //    {
                //        if (hit.countsAsHit)
                //        {
                //            var se = hit.target.statusEffects.Find(se => se.type == "teeth");
                //            var reduceBy = hit.target.statusEffects.Find(se => se.type == "reducer").GetAmount();
                //            if (se.count > reduceBy) 
                //                se.count = Extensions.CreateScriptableAmount<ScriptableCurrentStatus>(
                //                    statusType: se.type, offset: -reduceBy
                //                    ).Get(hit.target);
                //            else CoroutineManager.Start(se.Remove());
                //        }
                //    });
        }

        internal void Sniper()
        {
            var statusSniper = Extensions.CreateStatusEffectData<StatusEffectBombard>("Hope", "Sniper", type: "sniper")
                .Set("targetCountRange", new Vector2Int(1, 1))
                .Set("hitFriendlyChance", 0)
                .Set("delayBetweenTargets", 0)
                .Set("delayAfter", 0.1f)
                .Set("maxFrontTargets", 2)
                .SetText("Click to cycle this card's <sprite=target>")
                .RegisterInGroup();

            var onBoardSniper = Extensions.CreateStatusEffectData<StatusEffectApplyXWhenDeployed>("Hope", "When Deployed Sniper",
                effectToApply: statusSniper, applyToFlags: ApplyToFlags.Self);

            var c = CardAdder.CreateCardData("Hope", "Sniper")
                .SetTitle("Sniper")
                .SetIsUnit()
                .SetCanPlay(CardAdder.CanPlay.CanPlayOnEnemy | CardAdder.CanPlay.CanPlayOnBoard)
                .SetSprites("CardPortraits\\SunPortrait", "CardPortraits\\testBackground")
                .SetStats(10, 10, 10)
                .SetBloodProfile(CardAdder.VanillaBloodProfiles.BloodProfilePinkWisp)
                .SetIdleAnimationProfile(CardAdder.VanillaCardAnimationProfiles.GoopAnimationProfile)
                .SetStartWithEffects(onBoardSniper.StatusEffectStack(1))
                .SetTraits(CardAdder.VanillaTraits.Unmovable.TraitStack(1));
            //.RegisterInGroup()
            //.AddOnPlaceAction((entity) => {
            //        CoroutineManager.Start(EnableRoutineAfterDeploy(entity, "sniper").WrapToIl2Cpp());
            //});



            var c2 = CardAdder.CreateCardData("Hope", "Sniper")
                .SetTitle("Sniper")
                .SetIsUnit()
                .SetCanPlay(CardAdder.CanPlay.CanPlayOnEnemy | CardAdder.CanPlay.CanPlayOnBoard)
                .SetSprites("CardPortraits\\SunPortrait", "CardPortraits\\testBackground")
                .SetStats(4, 8, 6)
                .SetBloodProfile(CardAdder.VanillaBloodProfiles.BloodProfilePinkWisp)
                .SetIdleAnimationProfile(CardAdder.VanillaCardAnimationProfiles.GoopAnimationProfile)
                .SetStartWithEffects(statusSniper.StatusEffectStack(1))
                .SetTraits(CardAdder.VanillaTraits.Unmovable.TraitStack(1))
                .RegisterInGroup();
                //.AddPreTurnAction(unit => CoroutineManager.Start(Battle.CardCountDown(unit)));
        }


        #endregion

        #region dropped ideas

        //wip 0 progress
        internal void KnightSummoner()
        {
            var modName = "Hope";
            var effectName = "Change to LilBerry";
            var param_CardName = "LilBerry";
            var se = StatusEffectAdder.CreateStatusEffectData<StatusEffectNextPhase>(modName, effectName).ModifyFields(
                        delegate (StatusEffectNextPhase data)
                        {
                            var templateSummon = CardAdder.VanillaStatusEffects.SummonBeepop.StatusEffectData().Cast<StatusEffectSummon>();
                            data.name = $"Summon {param_CardName}";
                            data = data.SetText("{0}");
                            data.textInsert = $"When killed, transform into <card={param_CardName}>";
                            data.eventPriority = 99999;
                            data.nextPhase = AddressableLoader.groups["CardData"]
                                .lookup[param_CardName]
                                .Cast<CardData>();
                            data.preventDeath = true;
                            data.type = "nextphase";
                            data.count = 0;
                            data.animation = ScriptableObject.CreateInstance<CardAnimation>();

                            return data;
                        });
            im.Print($"{modName}.{effectName} has been injected");

            var c = CardAdder.CreateCardData("Hope", "Transformer")
                .SetTitle("Transformer")
                .SetIsUnit()
                //.AddToPool(CardAdder.VanillaRewardPools.BasicItemPool) debug cards shouldn't be in pools
                .SetCanPlay(CardAdder.CanPlay.CanPlayOnEnemy | CardAdder.CanPlay.CanPlayOnBoard)
                .SetSprites("CardPortraits\\SunPortrait", "CardPortraits\\testBackground")
                .SetStats(5, null, 1)
                .SetBloodProfile(CardAdder.VanillaBloodProfiles.BloodProfilePinkWisp)
                .SetIdleAnimationProfile(CardAdder.VanillaCardAnimationProfiles.GoopAnimationProfile)
                .SetStartWithEffects(new CardData.StatusEffectStacks()
                {
                    data = se,
                    count = 1
                });
            if (!AddressableLoader.groups["CardData"].lookup.ContainsKey(c.name))
            {
                AddressableLoader.groups["CardData"].list.Add(c);
                AddressableLoader.groups["CardData"].lookup.Add(c.name, c);
            }

            Instance.Log.LogInfo($"Card {c.name} is injected by api!");
        }
        #endregion

        // END OF EXTRA COMMANDS
    }

    // END OF BEHAVIOUR

    class Test
    // Intercept a process by activating when the process in nameof, under typeof, is run
    //[HarmonyPatch(typeof(CharacterSelectScreen), nameof(CharacterSelectScreen.Start))]
    {
        // set the intercepting functions to run
        static System.Collections.IEnumerator IETest(Il2CppSystem.Collections.IEnumerator a)
        {
            //yield return UpdateIE();
            yield return a;
        }

        // replace the process with a new one
        //[HarmonyPostfix]
        static Il2CppSystem.Collections.IEnumerator Postfix(Il2CppSystem.Collections.IEnumerator __result, CharacterSelectScreen __instance)
        {
            //Instance.Log.LogInfo("CharacterSelectScreen start run! " + CardDataAdditions.Count);
            //WildFrostAPIMod.APIGameObject.instance.StartCoroutine(CardAdder.FixPetsAmountQWQ());
            return IETest(__result).WrapToIl2Cpp();
        }
    }

    

    public void Print(object text) { Instance.Log.LogInfo(text) ;}

    public static IEnumerator General()
    {
       yield return new WaitUntil((Func<bool>)(() =>
            AddressableLoader.IsGroupLoaded("GameModifierData")    
        ));
        string[] layers = System.Linq.Enumerable.Range(0, 31).Select(index => LayerMask.LayerToName(index)).Where(l => !string.IsNullOrEmpty(l)).ToArray();
        //foreach (var i in layers) im.Print(i);

        var se = Extensions.CreateStatusEffectData<StatusEffectInstantMultiple>("Hope", "multiple")
            .Set("effects", new StatusEffectInstant[]
            {
                CardAdder.VanillaStatusEffects.ReduceCounter.StatusEffectData().Cast<StatusEffectInstant>(),
                CardAdder.VanillaStatusEffects.IncreaseAttack.StatusEffectData().Cast<StatusEffectInstant>()
            }.ToRefArray());


        var s = Extensions.CreateStatusEffectData<StatusEffectApplyXOnCardPlayed>("", "on play", effectToApply: se, applyToFlags: ApplyToFlags.Self);

        var c = CardAdder.CreateCardData("Hope", "Summoner")
                .SetTitle("Summoner")
                .SetIsUnit()
                .SetCanPlay(CardAdder.CanPlay.CanPlayOnEnemy | CardAdder.CanPlay.CanPlayOnBoard)
                .SetSprites("CardPortraits\\SunPortrait", "CardPortraits\\testBackground")
                .SetStats(5, 0, 1)
                .SetBloodProfile(CardAdder.VanillaBloodProfiles.BloodProfilePinkWisp)
                .SetIdleAnimationProfile(CardAdder.VanillaCardAnimationProfiles.GoopAnimationProfile)
                .SetStartWithEffects(s.StatusEffectStack(1));
                //.RegisterInGroup();

        var gameMode = AddressableLoader.groups["GameMode"].lookup["GameModeNormal"].Cast<GameMode>();
        var unit = gameMode.classes[2].rewardPools.ToList().Find(a => a.name == "GeneralUnitPool");
        var charm = gameMode.classes[2].rewardPools.ToList().Find(a => a.name == "GeneralCharmPool");
        var item = gameMode.classes[2].rewardPools.ToList().Find(a => a.name == "GeneralItemPool");
        gameMode.classes[2].rewardPools = new RewardPool[] { unit, charm, item }.ToRefArray();
        im.Print(gameMode.classes[2].rewardPools.Count + "pools");
        //foreach (var item in gameMode.classes[0].rewardPools.ToList().Find(a => a.name == "GeneralUnitPool").list)
        //    im.Print(item.name);

        var o = new MyCommand();
        //Console.commands.Add(o);
    }
    public Console.Command command = new Console.CommandToggleFps();
    public class MyCommand : Console.Command
    {
        public new string id = "test";
        private Console.Command command = new Console.CommandKillAll();
        public override void Run(string args)
        {
            command.Run(args);
        }

    }





    private Behaviour _behaviour;


    public override void Load()
    {
        //Extensions.LaunchEvent();
        ClassInjector.RegisterTypeInIl2Cpp<Behaviour>();
        Instance = this;
        Harmony.CreateAndPatchAll(System.Reflection.Assembly.GetExecutingAssembly(), "WildFrost.Hope.GeneralModifiers");
        _behaviour = AddComponent<Behaviour>();
    }











    #region Old stuff



    public static IEnumerator GetStrings()
    {
        yield return new WaitUntil((Func<bool>)(() =>
            //UnityEngine.Object.FindObjectOfType<BattleLogSystem>() != null));
            //yield return new WaitUntil((Func<bool>)(() =>
            //UnityEngine.Object.FindObjectOfType<GameModifierData>() != null));
            SceneManager.IsLoaded("Town")));
        im.Print("b81e88471bd1a4444a9d08e056ffc16c");
        StringTable table = LocalizationSettings.StringDatabase.GetTable(TableReference.TableReferenceFromString("GUID:b81e88471bd1a4444a9d08e056ffc16c"));
        foreach (var i in table.TableData)
        {
            im.Print(i.m_Localized);
        }
        im.Print("679b5249a8ed5b140a44902895a0b37a");
        StringTable table2 = LocalizationSettings.StringDatabase.GetTable(TableReference.TableReferenceFromString("GUID:679b5249a8ed5b140a44902895a0b37a"));
        foreach (var i in table2.TableData)
        {
            im.Print(i.m_Localized);
        }
        im.Print("775758c0ba0d2a84984c7ae6ac6e5feb");
        StringTable tabl3e = LocalizationSettings.StringDatabase.GetTable(TableReference.TableReferenceFromString("GUID:775758c0ba0d2a84984c7ae6ac6e5feb"));
        foreach (var i in tabl3e.TableData)
        {
            im.Print(i.m_Localized);
        }
        // GUID:679b5249a8ed5b140a44902895a0b37a
        // 775758c0ba0d2a84984c7ae6ac6e5feb
    }
    public static Texture2D textureFromSprite(Sprite sprite)
    {
        try
        {
            if (sprite.rect.width != sprite.texture.width)
            {
                Texture2D newText = new Texture2D((int)sprite.rect.width, (int)sprite.rect.height);
                Color[] newColors = sprite.texture.GetPixels((int)sprite.textureRect.x,
                                                             (int)sprite.textureRect.y,
                                                             (int)sprite.textureRect.width,
                                                             (int)sprite.textureRect.height);
                newText.SetPixels(newColors);
                newText.Apply();
                return newText;
            }
            else
                return sprite.texture;
        }
        catch
        {
            return null;
        }
    }
    public void SaveSprite(Sprite? sprite, string fileName)
    {
        if (sprite != null)
        {
            var texture = textureFromSprite(sprite).MakeReadable();
            var bytes = texture.EncodeToPNG();
            var file = new FileStream(WildFrostAPIMod.ModsFolder + $"/{fileName}.png", FileMode.Create, FileAccess.Write);
            file.Write(bytes, 0, bytes.Length);
            file.Close();
        }
    }


    private static void SaveImageFiles()
    {
        im.Print("Saving Image Files!");
        var allImages = UnityEngine.Object.FindObjectsOfTypeIncludingAssets(Il2CppType.Of<UnityEngine.UI.Image>());
        if (allImages != null)
        {
            var savedTextures = new List<string>();
            foreach (var imageinList in allImages.ToList())
            {
                var image = imageinList.MemberwiseClone().TryCast<UnityEngine.UI.Image>();

                if (image != null)
                {
                    // textureFromImage
                    Sprite sprite = image.sprite;
                    if (sprite == null)
                    {
                        im.Print("Illegal sprite at " + image.name);
                    }
                    else
                    {
                        Texture2D texture = image.sprite.texture;
                        if (texture == null)
                        {
                            im.Print(image.name + " is illegal!");
                        }
                        var illegalNames = new String[]
                        {
                            "sactx-0-4096x4096-BC7-CardAtlas-6ff9eaf3",
                            "sactx-0-4096x2048-Crunch-HandOverlayAtlas-6c958f34",
                            "sactx-0-8192x8192-Crunch-CardCompanionAtlas-6b2514c3",
                            "sactx-0-4096x8192-Uncompressed-CardLeaderAtlas-45f265f1",
                            "sactx-0-8192x8192-Crunch-CardItemAtlas-25f6c0b7",
                            "sactx-0-8192x4096-Crunch-CardClunkerAtlas-6eb4d30e",
                            "sactx-0-8192x8192-Crunch-CardBossAtlas-c56e219d",
                            "sactx-0-8192x8192-Crunch-CardEnemyAtlas-98224c39",
                            "Death screen sheet",
                            "Controller ButtonSheet",
                            "Charm sheet",
                            "VFX sheet",
                            "Journal UI sheet",
                            "Journal map spritesheet",
                            "Town_progress_task_bar_sheet",
                            // each sun freind sprite has the same name as its texture


                        };
                        // duplicateTexture
                        if (texture != null
                            && !illegalNames.Contains(texture.name)
                            && !savedTextures.Contains(texture.name)
                            )
                        {
                            im.Print("Attempting to save " + texture.name + " from " + sprite.name);
                            Texture2D readableText = texture.MakeReadable();
                            var bytes = readableText.EncodeToPNG();
                            var file = new FileStream(WildFrostAPIMod.ModsFolder + $"/{texture.name}.png", FileMode.Create, FileAccess.Write);
                            file.Write(bytes, 0, bytes.Length);
                            file.Close();

                            savedTextures.Add(texture.name);
                        }
                    }
                }
            }
        }
    }
    public class ObjectA
    {
        private ObjectB _objectB;

        public ObjectA()
        {
        }

        public void Initialize(ObjectB objectB)
        {
            _objectB = objectB;
        }
    }

    public class ObjectB
    {
        private ObjectA _objectA;

        public ObjectB()
        {
        }

        public void Initialize(ObjectA objectA)
        {
            _objectA = objectA;
        }
    }

    private static void SaveTextureFiles()
    {
        im.Print("Saving Texture Files!");
        var allSprites = UnityEngine.Object.FindObjectsOfTypeIncludingAssets(Il2CppType.Of<Sprite>());
        if (allSprites != null)
        {
            var savedTextures = new List<string>();
            foreach (var spriteinList in allSprites.ToList())
            {
                var sprite = spriteinList.MemberwiseClone().TryCast<Sprite>();

                if (sprite != null)
                {
                    // textureFromSprite
                    Texture2D texture = sprite.texture;
                    if (texture == null)
                    {
                        im.Print(sprite.name + " is illegal!");
                    }
                    var illegalNames = new String[]
                    {
                            "sactx-0-4096x4096-BC7-CardAtlas-6ff9eaf3",
                            "sactx-0-4096x2048-Crunch-HandOverlayAtlas-6c958f34",
                            "sactx-0-8192x8192-Crunch-CardCompanionAtlas-6b2514c3",
                            "sactx-0-4096x8192-Uncompressed-CardLeaderAtlas-45f265f1",
                            "sactx-0-8192x8192-Crunch-CardItemAtlas-25f6c0b7",
                            "sactx-0-8192x4096-Crunch-CardClunkerAtlas-6eb4d30e",
                            "sactx-0-8192x8192-Crunch-CardBossAtlas-c56e219d",
                            "sactx-0-8192x8192-Crunch-CardEnemyAtlas-98224c39",
                            "Death screen sheet",
                            "Controller ButtonSheet",
                            "Charm sheet",
                            "VFX sheet",
                            "Journal UI sheet",
                            "Journal map spritesheet",
                            "Town_progress_task_bar_sheet",
                        // each sun freind sprite has the same name as its texture


                    };
                    // duplicateTexture
                    if (texture != null
                        && !illegalNames.Contains(texture.name)
                        && !savedTextures.Contains(texture.name)
                        )
                    {
                        im.Print("Attempting to save " + texture.name + " from " + sprite.name);
                        Texture2D readableText = texture.MakeReadable();
                        var bytes = readableText.EncodeToPNG();
                        var file = new FileStream(WildFrostAPIMod.ModsFolder + $"/{texture.name}.png", FileMode.Create, FileAccess.Write);
                        file.Write(bytes, 0, bytes.Length);
                        file.Close();

                        savedTextures.Add(texture.name);
                    }
                }
            }
        }
        
    }
    #endregion
}




// testing auto-adding coroutines because lazy
public class CoroutineAutoAdder : MonoBehaviour
{
    private void Start()
    {
        //this.StartCoroutine(AutoAddCoroutines());
    }

    private IEnumerator AutoAddCoroutines()
    {
        Type type = GetType();
        System.Reflection.MethodInfo[] methods = type.GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

        foreach (System.Reflection.MethodInfo method in methods)
            if (method.ReturnType == typeof(IEnumerator) && method.Name.StartsWith("AutoAddCoroutines"))
            {
            yield return StartCoroutine((Il2CppSystem.Collections.IEnumerator)method.Invoke(this, null));
        }
    }

    private IEnumerator MyCoroutine()
    {
        // Coroutine logic here
        yield return null;
    }

        // Other coroutine methods...
}