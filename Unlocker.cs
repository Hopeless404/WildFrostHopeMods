using System.Collections;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using BepInEx.Unity.IL2CPP.Utils;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace WildFrostUnlockSelector;

public static class Extensions
{
    public static T? Find<T>(this Il2CppSystem.Collections.Generic.List<T> l, Predicate<T> p)
    {
        for (int i = 0; i < l.Count; i++)
        {
            T item = l.ToArray()[i];
            if (p(item))
            {
                return item;
            }
        }

        return default;
    }
}


[BepInPlugin("WildFrost.Hope.UnlockSelector", "Unlock Selector", "0.1.0.0")]
public class Unlocker : BasePlugin
{
    internal static Unlocker Instance;
    internal static Dictionary<string, string> nameToCharm = new Dictionary<string, string>()
        {
            { "", "" },
            { "frog", "CardUpgradeFury" },
            { "greed", "CardUpgradeGreed" },
            { "chuckle", "CardUpgradeRemoveCharmLimit" },
            { "moko", "CardUpgradeFrenzyReduceAttack" },
            { "strawberry", "CardUpgradeConsumeAddHealth" },
            { "raspberry", "CardUpgradeAttackAndHealth" },
            { "critical", "CardUpgradeCritical" },
            { "spark", "CardUpgradeSpark" },
            { "pengu", "CardUpgradeSnowImmune" },
            { "moose", "CardUpgradeAttackIncreaseCounter" },
            { "durian", "CardUpgradeAttackRemoveEffects" },
            { "shellnut", "CardUpgradeShellBecomesSpice" },
            { "lamb", "CardUpgradeEffigy" },
            { "truffle", "CardUpgradeShroomReduceHealth" },
            { "molten", "CardUpgradeAttackConsume" },
            { "block", "CardUpgradeBlock" }
        };
    internal static List<string> numToUnit = new List<string>()
    {
        "Snobble",
        "Klutz",
        "TinyTyko",
        "Bombom",
        "BoBo",
        "Turmeep"
    };
    internal static List<string> numToItem = new List<string>()
    {
        "Slapcrackers",
        "Snowcracker",
        "Hooker",
        "ScrapPile",
        "MegaMimik",
        "Krono"
    };
    internal static List<string> CharmsToRemove = new List<string>();
    internal static List<string> CharmsToAdd = new List<string>();
    internal static List<string> UnitsToRemove = new List<string>();
    internal static List<string> ItemsToRemove = new List<string>();


    public class Behaviour : MonoBehaviour
    {
        private void Start()
        {
            //this.StartCoroutine(Adder());
            this.StartCoroutine(Remover());
            this.StartCoroutine(Beller());
        }
    }
    
    
    public static IEnumerator Remover()
    {
        while (true)
        {
            yield return new WaitUntil((Func<bool>)(() => SceneManager.IsLoaded("Campaign")));
            yield return new WaitUntil((Func<bool>)(() => UnityEngine.Object.FindObjectsOfType<CharacterRewards>().Count > 0));
            Instance.Log.LogInfo("Starting the Remover!");
            CharacterRewards rewards = UnityEngine.Object.FindObjectOfType<CharacterRewards>();

            var allCharms = rewards.poolLookup["Charms"].list; // contains keys "Charms", "Units", "Items"
            Instance.Log.LogInfo("There are " + allCharms.Count + " Charms!");
            foreach (var charmName in CharmsToRemove)
            {
                if (allCharms.Find(a => a.name == charmName) != null)
                {
                    DataFile charm = allCharms.Find(a => a.name == charmName).Cast<DataFile>();
                    rewards.poolLookup["Charms"].Remove(charmName);
                    Instance.Log.LogInfo("Successfully removed " + charm.name);
                }
            }
            Instance.Log.LogInfo("There are " + allCharms.Count + " Charms!");

            var allUnits = rewards.poolLookup["Units"].list; // contains keys "Charms", "Units", "Items"
            Instance.Log.LogInfo("There are " + allUnits.Count + " Units!");
            foreach (var unitName in UnitsToRemove)
            {
                if (allUnits.Find(a => a.name == unitName) != null)
                {
                    DataFile unit = allUnits.Find(a => a.name == unitName).Cast<DataFile>();
                    rewards.poolLookup["Units"].Remove(unitName);
                    Instance.Log.LogInfo("Successfully removed " + unit.name);
                }
            }
            Instance.Log.LogInfo("There are " + allUnits.Count + " Units!");

            var allItems = rewards.poolLookup["Items"].list; // contains keys "Charms", "Units", "Items"
            Instance.Log.LogInfo("There are " + allItems.Count + " Items!");
            foreach (var itemName in ItemsToRemove)
            {
                if (allItems.Find(a => a.name == itemName) != null)
                {
                    DataFile item = allItems.Find(a => a.name == itemName).Cast<DataFile>();
                    rewards.poolLookup["Items"].Remove(itemName);
                    Instance.Log.LogInfo("Successfully removed " + item.name);
                }
            }
            Instance.Log.LogInfo("There are " + allItems.Count + " Items!");
            yield return SceneManager.WaitUntilUnloaded("Campaign");
        }
        
    }
    public static IEnumerator Beller()
    {
        int moddedCount = 3;

        while (true)
        {
            yield return new WaitUntil((Func<bool>)(() => SceneManager.IsLoaded("CharacterSelect")));
            Instance.Log.LogInfo("Starting the Beller!");

            var vanillaCount = SaveSystem.LoadProgressData("hardModeModifiersUnlocked", 3);
            //SaveSystem.SaveProgressData("hardModeModifiersUnlocked", value: moddedCount);
            var c = UnityEngine.Object.FindObjectOfType<CharacterSelectScreen>();

            var allModifiers = UnityEngine.Object.FindObjectsOfTypeAll(Il2CppType.Of<GameModifierData>()).ToList();
            Instance.Log.LogInfo("There are " + allModifiers.Count + " Modifiers!");

            var visibleModifiers = new List<GameModifierData>();
            foreach (var modifier in allModifiers)
            {
                if (modifier.Cast<GameModifierData>().visible == true)
                {
                    visibleModifiers.Add(modifier.Cast<GameModifierData>());
                }
            }
            c.modifierDisplay.modifiers = visibleModifiers.ToArray();
            //Instance.Log.LogInfo(c.modifierDisplay.modifierCount);

            yield return SceneManager.WaitUntilUnloaded("Town");
            SaveSystem.SaveProgressData("hardModeModifiersUnlocked", value: vanillaCount);
            yield return SceneManager.WaitUntilUnloaded("CharacterSelect");
        }
    }

    


    private ConfigEntry<string>? CharmNamesToRemove;
    //private ConfigEntry<string>? CharmNamesToAdd;
    private ConfigEntry<int>? UnitNumsToRemove;
    private ConfigEntry<int>? ItemNumsToRemove;
    public override void Load()
    {
        CharmNamesToRemove = Config.Bind("Removers",
            "CharmsToRemove",
            "Frog,Greed,Chuckle,Moko,Strawberry,Raspberry,Critical,Spark,Pengu,Moose,Durian,Shellnut,Lamb,Truffle,Molten,Block",
            "Type the first names of the charms to remove from campaign (case-insensitive), e.g. 'Chuckle', 'durian' or 'moLTen'");

        foreach (var name in CharmNamesToRemove.Value.Split(',').ToList())
        {
            if (name != "" && nameToCharm.ContainsKey(name.ToLower()))
            {
                CharmsToRemove.Add(nameToCharm[name.ToLower()]);
            }
        }

        UnitNumsToRemove = Config.Bind("Removers",
            "UnitsRetained",
            -1,
            "Type the number of already unlocked Hot Spring companions to keep, e.g. 2 for Snobble and Jumbo, then remove the rest. Type -1 to ignore this modifier.");

        if (UnitNumsToRemove.Value >= 0 && UnitNumsToRemove.Value <= 6)
        {
            for (int i = 5; i >= UnitNumsToRemove.Value; i--)
            {
                UnitsToRemove.Add(numToUnit[i]);
            }
        }

        ItemNumsToRemove = Config.Bind("Removers",
            "ItemsRetained",
            -1,
            "Type the number of already unlocked Inventor's Hut items to keep, e.g. 2 for Slapcrackers and Kobonker, then remove the rest. Type -1 to ignore this modifier.");

        if (ItemNumsToRemove.Value >= 0 && ItemNumsToRemove.Value <= 6)
        {
            for (int i = 5; i >= ItemNumsToRemove.Value; i--)
            {
                ItemsToRemove.Add(numToItem[i]);
            }
        }

        //var p = UnityEngine.Object.FindObjectOfType<MetaprogressionSystem>();
        //foreach (var key in MetaprogressionSystem.data.Keys)
        //    Instance.Log.LogInfo(key.ToString());

        //CharmNamesToAdd = Config.Bind("Add Charms (priority over Remove Charms)",
        //    "CharmsToAdd",
        //    "Frog,Greed,Chuckle,Moko,Strawberry,Raspberry,Critical,Spark,Pengu,Moose,Durian,Shellnut,Lamb,Truffle,Molten,Block",
        //    "Type the first names of the charms to add to campaign (case-insensitive), e.g. 'Chuckle', 'durian' or 'moLTen'");
        //
        //foreach (var name in CharmNamesToAdd.Value.Split(',').ToList())
        //{
        //   if (nameToCharm.ContainsKey(name.ToLower()))
        //    {
        //        CharmsToAdd.Add(nameToCharm[name.ToLower()]);
        //    }
        //}

        // Logic
        // Single hit damage threshold:
        if (CharmsToRemove.Contains("CardUpgradeAttackIncreaseCounter") && !CharmsToRemove.Contains("CardUpgradeAttackRemoveEffects")) {
            CharmsToRemove.Add("CardUpgradeAttackIncreaseCounter");
        }
            
        if (!CharmsToAdd.Contains("CardUpgradeAttackIncreaseCounter") && CharmsToAdd.Contains("CardUpgradeAttackRemoveEffects"))
            CharmsToAdd.Remove("CardUpgradeAttackRemoveEffects");

        // Kill combo threshold:
        // lilgazi 6, tinytyko 4, slapcrackers 3
        if (ItemsToRemove.Contains("Slapcrackers") && !UnitsToRemove.Contains("TinyTyko"))
        {
            for (int i = 5; i > 3; i--)
            {
                UnitsToRemove.Add(numToUnit[i]);
            }
        }

        ClassInjector.RegisterTypeInIl2Cpp<Behaviour>();
        Instance = this;
        Harmony.CreateAndPatchAll(System.Reflection.Assembly.GetExecutingAssembly(), "WildFrost.Hope.UnlockSelector");
        AddComponent<Behaviour>();

        }

}
