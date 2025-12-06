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

        Track.OnStartedPlayingTrack += async (_, _) =>
        {
            await Awaitable.MainThreadAsync();
            
            await _trackStripSolidMatcapObject?.SetCustomMatcap(TrackStripMatcap.Value.ToLowerInvariant() == "default"
                ? "default"
                : $"{DataPath}/{TrackStripMatcap.Value}")!;

            await ReinitializeWheel();
        };
    }

    internal static Cubemap? BlankCubemap;
    
    private static GameObject? _trackStripSolid;
    private static ReplaceableMatcapObject? _trackStripSolidMatcapObject;
    private static readonly List<GameObject> WheelObjects = [];
    private static readonly List<ReplaceableMatcapObject> WheelObjectsMatcapObjects = [];
    private static readonly List<GameObject> WheelBackingObjects = [];
    private static readonly List<ReplaceableMatcapObject> WheelBackingObjectsMatcapObjects = [];
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
        //_ = InitializeWheel();
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
            
                Log.LogInfo($"Found wheel mesh part {childObject.name}");
                WheelObjects.Add(visual.transform.GetChild(idx).gameObject);
            }

            for (int idx = 0; idx < visual.wheelSpinning.childCount; idx++)
            {
                GameObject childObject = visual.wheelSpinning.GetChild(idx).gameObject;
                if (!childObject.name.Contains("WheelWedge Prefab"))
                {
                    continue;
                }
                
                Log.LogInfo($"Found wheel wedge backing part {childObject.transform.Find("WedgeBacking")}");
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
}