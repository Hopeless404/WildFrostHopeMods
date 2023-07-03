using System.Collections;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Unity.IL2CPP.Utils;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Data;
using Il2CppSystem.Dynamic.Utils;
using UnityEngine;

using WildFrostHopeMods;
using WildfrostModMiya;

namespace WildFrostNewTribe;

[BepInPlugin("WildFrost.Hope.NewTribe1", "New Tribe #1", "0.1.0.0")]
public class TribeAdder : BasePlugin
{

    internal static TribeAdder Instance;
    internal static GeneralModifier im = new GeneralModifier();


    public class Behaviour : MonoBehaviour
    {
        private void Start()
        {
            //this.StartCoroutine(AddTribeOnce());
        }


        // adding tribes in 1.0.5 need more work and i'm not doing that
        // it's probably not any more different in 1.0.6, just need to change in Patches
        public static IEnumerator AddTribeOnce()
        {
            yield return new WaitUntil((Func<bool>)(() =>
                AddressableLoader.IsGroupLoaded("GameMode")));
            im.Print("Starting to add tribes");

            var gameMode = AddressableLoader.groups["GameMode"].lookup["GameModeNormal"].Cast<GameMode>();
            var c = Extensions.CreateClassData("TestTribe", leaders: gameMode.classes[1].leaders, rewardPools: gameMode.classes[2].rewardPools);
            //Extensions.AddClass(c);
            //gameMode.classes[0].flag = CardAdder.LoadSpriteFromCardPortraits("CardPortraits\\FALLBACKBATTLESPRITE");
        }
    }

    private Behaviour _behaviour;
    public override void Load()
    {
        Instance = this;
        Harmony.CreateAndPatchAll(System.Reflection.Assembly.GetExecutingAssembly(), "WildFrost.Hope.NewTribe1");
        _behaviour = AddComponent<Behaviour>();
    }
}
