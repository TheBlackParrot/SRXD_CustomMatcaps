using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Logging;
using CustomMatcaps.Classes;
using HarmonyLib;
using SpinCore.Translation;
using UnityEngine;

// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator

namespace CustomMatcaps;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("srxd.raoul1808.spincore", "1.1.2")]
public partial class Plugin : BaseUnityPlugin
{
    private const string TRANSLATION_PREFIX = $"{nameof(CustomMatcaps)}_";
    internal static ManualLogSource Log = null!;
    private static readonly Harmony HarmonyInstance = new(MyPluginInfo.PLUGIN_GUID);

    private static string DataPath => Path.Combine(Paths.ConfigPath, nameof(CustomMatcaps));

    private void Awake()
    {
        Log = Logger;
        
        if (!Directory.Exists(DataPath))
        {
            Directory.CreateDirectory(DataPath);
        }
        
        TranslationHelper.AddTranslation($"{TRANSLATION_PREFIX}ModName", nameof(CustomMatcaps));
        
        RegisterConfigEntries();
        CreateModPage();
        
        HarmonyInstance.PatchAll();
        
        Log.LogInfo("Plugin loaded");
    }

    private void OnDestroy()
    {
        HarmonyInstance.UnpatchSelf();
    }

    private void OnEnable()
    {
        Task.Run(async () =>
        {
            try
            {
                await Initialize();
            }
            catch (Exception e)
            {
                Log.LogError(e);
            }
        });
    }

    internal static Cubemap? BlankCubemap;
    
    private static GameObject? _trackStripSolid;
    private static ReplaceableMatcapObject? _trackStripSolidMatcapObject;
    private static readonly List<GameObject> WheelObjects = [];
    private static readonly List<ReplaceableMatcapObject> WheelObjectsMatcapObjects = [];
    private static readonly List<GameObject> WheelBackingObjects = [];
    private static readonly List<ReplaceableMatcapObject> WheelBackingObjectsMatcapObjects = [];

    internal static List<Material?> CharacterMaterials => CharacterMaterialMatcapObjects.Select(x => x?.MaterialObject).ToList();
    private static readonly ReplaceableMatcapObject?[] CharacterMaterialMatcapObjects =
        Enumerable.Repeat<ReplaceableMatcapObject?>(null, 7).ToArray();
    
    internal static List<Material?> VRWandMaterials => VRWandMaterialMatcapObjects.Select(x => x?.MaterialObject).ToList();

    internal static readonly ReplaceableMatcapObject?[] VRWandMaterialMatcapObjects =
        Enumerable.Repeat<ReplaceableMatcapObject?>(null, 3).ToArray();
    
    private static async Task Initialize()
    {
        await Awaitable.MainThreadAsync();
        
        if (BlankCubemap == null)
        {
            Color[] pixels = Enumerable.Repeat(Color.black, 64).ToArray();
            
            BlankCubemap = new Cubemap(8, TextureFormat.RGB24, 0)
            {
                wrapMode = TextureWrapMode.Repeat
            };

            for (CubemapFace face = CubemapFace.PositiveX; face <= CubemapFace.NegativeZ; face++)
            {
                BlankCubemap.SetPixels(pixels, face);   
            }
            BlankCubemap.Apply();
        }

        _ = InitializeTrackStrip();
        _ = InitializeCharacterMaterials();
    }

    private static async Task InitializeCharacterMaterials()
    {
        Shader matcapShader = Resources.FindObjectsOfTypeAll<Shader>().First(x => x.name == "Unlit/Matcap");
        for (int idx = 0; idx < CharacterMaterialMatcapObjects.Length; idx++)
        {
            CharacterMaterialMatcapObjects[idx] = new ReplaceableMatcapObject(matcapShader);
            
            // rider sweetie this LITERALLY cannot be null it's ^^^^^^ RIGHT THERE
            await CharacterMaterialMatcapObjects[idx]!.SetCustomMatcap(CharacterMaterialFilenames[idx].Value.ToLowerInvariant() == "default"
                ? "default"
                : $"{DataPath}/{CharacterMaterialFilenames[idx].Value}");
        }
    }

    private static async Task InitializeTrackStrip()
    {
        while (_trackStripSolid == null)
        {
            _trackStripSolid = GameObject.Find("TrackStripSolid");
            await Awaitable.EndOfFrameAsync();
        }
        
        _trackStripSolidMatcapObject = new ReplaceableMatcapObject(_trackStripSolid);
        await _trackStripSolidMatcapObject.SetCustomMatcap(TrackStripMatcap.Value.ToLowerInvariant() == "default"
            ? "default"
            : $"{DataPath}/{TrackStripMatcap.Value}");
    }

    private static async Task InitializeWheel()
    {
        WheelVisuals[] visuals = [];

        if (WheelObjects.Count > 0)
        {
            return;
        }
        
        await Awaitable.MainThreadAsync();
        
        while (visuals.Length == 0)
        {
            visuals = FindObjectsByType<WheelVisuals>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            await Awaitable.EndOfFrameAsync();
        }

        foreach (WheelVisuals visual in visuals)
        {
            for (int idx = 0; idx < visual.transform.childCount; idx++)
            {
                GameObject childObject = visual.transform.GetChild(idx).gameObject;
                if (!childObject.name.Contains("WheelMesh"))
                {
                    continue;
                }
                
                WheelObjects.Add(visual.transform.GetChild(idx).gameObject);
            }

            for (int idx = 0; idx < visual.wheelSpinning.childCount; idx++)
            {
                GameObject childObject = visual.wheelSpinning.GetChild(idx).gameObject;
                if (!childObject.name.Contains("WheelWedge Prefab"))
                {
                    continue;
                }
                
                WheelBackingObjects.Add(childObject.transform.Find("WedgeBacking").gameObject);
            }
        }
        
        foreach (GameObject wheelObject in WheelObjects)
        {
            ReplaceableMatcapObject matcapObject = new(wheelObject);
            WheelObjectsMatcapObjects.Add(matcapObject);
            
            await matcapObject.SetCustomMatcap(WheelMatcap.Value.ToLowerInvariant() == "default"
                ? "default"
                : $"{DataPath}/{WheelMatcap.Value}");
        }
        
        foreach (GameObject wheelBackingObject in WheelBackingObjects)
        {
            ReplaceableMatcapObject matcapObject = new(wheelBackingObject);
            WheelBackingObjectsMatcapObjects.Add(matcapObject);
            
            await matcapObject.SetCustomMatcap(WheelBackingMatcap.Value.ToLowerInvariant() == "default"
                ? "default"
                : $"{DataPath}/{WheelBackingMatcap.Value}");
        }
    }

    internal static async Task ReinitializeWheel()
    {
        WheelObjects.Clear();
        WheelObjectsMatcapObjects.Clear();
        WheelBackingObjects.Clear();
        WheelBackingObjectsMatcapObjects.Clear();
        await InitializeWheel();
    }
    
    internal static async Task InitializeVRWandMaterials(Renderer renderer)
    {
        if (VRWandMaterialMatcapObjects[0] != null)
        {
            return;
        }
        
        try
        {
            Material[] sharedMaterials = renderer.GetSharedMaterialArray();
            for (int idx = 0; idx < VRWandMaterialMatcapObjects.Length; idx++)
            {
                await Awaitable.MainThreadAsync();
                // the 0th index is on purpose, that's the main body material
                VRWandMaterialMatcapObjects[idx] = new ReplaceableMatcapObject(sharedMaterials[0]);

                await VRWandMaterialMatcapObjects[idx]!.SetCustomMatcap(
                    VRWandMaterialFilenames[idx].Value.ToLowerInvariant() == "default"
                        ? "default"
                        : $"{DataPath}/{VRWandMaterialFilenames[idx].Value}");
            }
        }
        catch (Exception e)
        {
            Log.LogError(e);
        }

        try
        {
            if (ColorUtility.TryParseHtmlString($"#{VRWandGlowColor.Value}", out Color parsedColor))
            {
                VRWandMaterialMatcapObjects[1]?.MaterialObject?.SetColor(TintColorId, parsedColor);   
            }
        }
        catch (Exception e)
        {
            Log.LogError(e);
        }
    }
}