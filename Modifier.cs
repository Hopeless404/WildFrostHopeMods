using System.Collections;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using BepInEx.Unity.IL2CPP.Utils;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;
using WildfrostModMiya;
using static WildfrostModMiya.CardAdder;

using Il2CppSystem.Diagnostics;
using Il2CppSystem.Reflection;

namespace WildFrostImageModifier;

[BepInPlugin("WildFrost.Hope.ImageModifiers", "Image Modifiers", "0.1.0.0")]
public class ImageModifier : BasePlugin
{

    internal static ImageModifier Instance;
    internal static ImageModifier im = new ImageModifier();

    public class Behaviour : MonoBehaviour
    {
        private void Start()
        {
            this.StartCoroutine(ModifyLilBerry());
            this.StartCoroutine(ModifyYuki());
            //this.StartCoroutine(General());
            //this.StartCoroutine(Daily());
        }
    }

    public void Print(object text)
    {
        Instance.Log.LogInfo(text);
    }

    public static IEnumerator ModifyLilBerry()
    {
        yield return new WaitUntil((Func<bool>)(() =>
            AddressableLoader.IsGroupLoaded("CardData")));
        Instance.Log.LogInfo("CardData is Loaded!");

        CardData card = AddressableLoader.groups["CardData"]
            .lookup["LilBerry"]
            .Cast<CardData>();

        UnityEngine.UI.Image image = card.scriptableImagePrefab.GetComponents<Component>().ToList()
            .Find(a => a.TryCast<UnityEngine.UI.Image>() != null)
            .Cast<UnityEngine.UI.Image>();

        image.m_Sprite = LoadSpriteFromCardPortraits("CardPortraits\\testPortrait");
        Instance.Log.LogInfo("Image is injected!");
        //LilBerry prefab = card.scriptableImagePrefab.Cast<LilBerry>();
    }


    public static IEnumerator ModifyYuki()
    {

        yield return new WaitUntil((Func<bool>)(() =>
            AddressableLoader.IsGroupLoaded("CardData")));
        Instance.Log.LogInfo("CardData is Loaded!");

        CardData card = AddressableLoader.groups["CardData"]
            .lookup["Yuki"]
            .Cast<CardData>();

        SnowBear prefab = UnityEngine.Object.FindObjectsOfTypeIncludingAssets(Il2CppType.Of<SnowBear>())
            .ToList()[0].Cast<SnowBear>();
        im.Print(prefab.name);

        Keyframe lastKeyframe = prefab.ballScaleCurve[prefab.ballScaleCurve.length - 1];
        lastKeyframe.time = float.MaxValue;
        lastKeyframe.inTangent = 1;
        lastKeyframe.outTangent = 1;




        var allSprites = UnityEngine.Object.FindObjectsOfTypeIncludingAssets(Il2CppType.Of<Sprite>());
        Sprite ball = allSprites.ToList()
            .Find(a => a.name.Equals("Yuki Ball"))
            .Cast<Sprite>();

        Instance.Log.LogInfo("We ballin!" + ball.name);

        var allImages = UnityEngine.Object.FindObjectsOfTypeIncludingAssets(Il2CppType.Of<UnityEngine.UI.Image>());
        UnityEngine.UI.Image image = allImages.ToList()
            .Find(a => a.Cast<UnityEngine.UI.Image>().m_Sprite == ball)
            .Cast<UnityEngine.UI.Image>();

        image.m_Sprite = CardAdder.LoadSpriteFromCardPortraits("CardPortraits\\testPortrait");
        var trans = image.GetComponents<Component>().ToList()[0].Cast<RectTransform>();
        trans.localScale = new Vector3(1f, 1f, 1f);
        trans.sizeDelta = new Vector2(3.8f, 5.7f);
        Instance.Log.LogInfo("Yuki is injected with methamphetamines!");
    }
    

    public override void Load()
    {

        ClassInjector.RegisterTypeInIl2Cpp<Behaviour>();
        Instance = this;
        Harmony.CreateAndPatchAll(System.Reflection.Assembly.GetExecutingAssembly(), "WildFrost.Hope.ImageModifiers");
        AddComponent<Behaviour>();

    }
}
