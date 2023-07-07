using System.Collections;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Unity.IL2CPP.Utils;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

using WildFrostHopeMods;
using WildfrostModMiya;

namespace WildFrostNewTribe;

//[BepInPlugin("WildFrost.Hope.NewTribe1", "New Tribe #1", "0.1.0.0")]
public class TribeAdder : BasePlugin
{

    internal static TribeAdder Instance;
    internal static TribeAdder im = new TribeAdder();
    public void Print(object text) => TribeAdder.Instance.Log.LogInfo(text);



    public class NewTribe : MonoBehaviour
    {
        private void Start()
        {
            this.StartCoroutine(AddTribeOnce());
        }


        // it's probably not any more different in 1.0.6, just need to change in Patches
        public static IEnumerator AddTribeOnce()
        {
            yield return new WaitUntil((Func<bool>)(() =>
                SceneManager.Loading.Contains("CharacterSelect")));
            im.Print("Starting to add tribes");

            var hopeLeaders = new List<CardData>();
            //Array.ForEach<UnityEngine.Object>(AddressableLoader.groups["CardData"].list.ToArray(), card => { if (card.Cast<CardData>().name.StartsWith("Hope.")) hopeLeaders.Add(card.Cast<CardData>()); });
            foreach (var card in AddressableLoader.groups["CardData"].list) if (card.Cast<CardData>().name.StartsWith("Hope.")) hopeLeaders.Add(card.Cast<CardData>());
            im.Print(hopeLeaders[0].createScripts.Count);

            var gameMode = AddressableLoader.groups["GameMode"].lookup["GameModeNormal"].Cast<GameMode>();
            var c = CreateClassData("TestTribe", leaders: gameMode.classes[1].leaders, rewardPools: gameMode.classes[2].rewardPools);
            //Extensions.AddClass(c);

            var hopeUnitPool = Extensions.CreateRewardPool("Units", hopeLeaders.ToArray());
            Extensions.AddClass(CreateClassData("HopeTribe", leaders: hopeLeaders.ToArray(), rewardPools: gameMode.classes[1].rewardPools.ToArray().AddToArray(hopeUnitPool)));

            //gameMode.classes[0].flag = CardAdder.LoadSpriteFromCardPortraits("CardPortraits\\FALLBACKBATTLESPRITE");
        }

        public static ClassData CreateClassData(string name, Inventory startingInventory = null, CardData[] leaders = null, RewardPool[] rewardPools = null, Sprite flag = null)
        {
            var basic = AddressableLoader.groups["GameMode"].lookup["GameModeNormal"].Cast<GameMode>().classes[0];

            ClassData data = ScriptableObject.CreateInstance<ClassData>();
            data.name = name;
            data.requiresUnlock = basic.requiresUnlock;
            data.characterPrefab = basic.characterPrefab;
            data.startingInventory = startingInventory ?? basic.startingInventory;
            data.leaders = basic.leaders;
            data.rewardPools = basic.rewardPools;
            data.flag = flag ?? CardAdder.LoadSpriteFromCardPortraits("CardPortraits\\FALLBACKBATTLESPRITE");

            return data;
        }

    }



    

    private NewTribe _newTribe;
    public override void Load()
    {
        ClassInjector.RegisterTypeInIl2Cpp<NewTribe>();
        Instance = this;
        //Harmony.CreateAndPatchAll(System.Reflection.Assembly.GetExecutingAssembly(), "WildFrost.Hope.NewTribe1");
        _newTribe = AddComponent<NewTribe>();
    }
}
