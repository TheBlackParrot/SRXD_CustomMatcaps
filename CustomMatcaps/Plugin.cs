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

    internal static string DataPath => Path.Combine(Paths.ConfigPath, nameof(CustomMatcaps));
    
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int LightColor = Shader.PropertyToID("_LightColor");

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
        
        MainCamera.OnCurrentCameraChanged += InitializeAfterCamera;
    }

    private static void InitializeAfterCamera(Camera obj)
    {
        MainCamera.OnCurrentCameraChanged -= InitializeAfterCamera;
        SetMenuLogoColors();
    }

    //private static Cubemap? _blankCubemap;
    
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
    private static readonly ReplaceableMatcapObject?[] VRWandMaterialMatcapObjects =
        Enumerable.Repeat<ReplaceableMatcapObject?>(null, 3).ToArray();
    
    private static async Task Initialize()
    {
        await Awaitable.MainThreadAsync();
        
        /*if (_blankCubemap == null)
        {
            Color[] pixels = Enumerable.Repeat(Color.black, 64).ToArray();
            
            _blankCubemap = new Cubemap(8, TextureFormat.RGB24, 0)
            {
                wrapMode = TextureWrapMode.Repeat
            };

            for (CubemapFace face = CubemapFace.PositiveX; face <= CubemapFace.NegativeZ; face++)
            {
                _blankCubemap.SetPixels(pixels, face);   
            }
            _blankCubemap.Apply();
        }*/

        _ = InitializeTrackStrip();
        _ = InitializeCharacterMaterials();
    }

    private static async Task InitializeCharacterMaterials(Material? overrideMaterial = null)
    {
        Shader matcapShader = Resources.FindObjectsOfTypeAll<Shader>().First(x => x.name == "Unlit/Matcap");
        
#if DEBUG
        Log.LogInfo("SHADER LIST:");
        foreach (Shader shader in Resources.FindObjectsOfTypeAll<Shader>())
        {
            Log.LogInfo($" -- {shader.name}");
        }
#endif
        
        for (int idx = 0; idx < CharacterMaterialMatcapObjects.Length; idx++)
        {
            if (overrideMaterial != null)
            {
                CharacterMaterialMatcapObjects[idx] = new ReplaceableMatcapObject(overrideMaterial);
            }
            else
            {
                CharacterMaterialMatcapObjects[idx] = new ReplaceableMatcapObject(matcapShader);
            }

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

    private static WheelVisuals[] wheelVisuals =>
        FindObjectsByType<WheelVisuals>(FindObjectsInactive.Include, FindObjectsSortMode.None);
    private static async Task InitializeWheel()
    {
        if (WheelObjects.Count > 0)
        {
            return;
        }
        
        await Awaitable.MainThreadAsync();
        
        while (wheelVisuals.Length == 0)
        {
            await Awaitable.EndOfFrameAsync();
        }

        List<GameObject> foundWheelObjects = [];
        List<GameObject> foundWheelBackingObjects = [];
        foreach (WheelVisuals visual in wheelVisuals)
        {
            for (int idx = 0; idx < visual.transform.childCount; idx++)
            {
                GameObject childObject = visual.transform.GetChild(idx).gameObject;
                if (!childObject.name.Contains("WheelMesh"))
                {
                    continue;
                }
                
                foundWheelObjects.Add(visual.transform.GetChild(idx).gameObject);
            }

            for (int idx = 0; idx < visual.wheelSpinning.childCount; idx++)
            {
                GameObject childObject = visual.wheelSpinning.GetChild(idx).gameObject;
                if (!childObject.name.Contains("WheelWedge Prefab"))
                {
                    continue;
                }
                
                foundWheelBackingObjects.Add(childObject.transform.Find("WedgeBacking").gameObject);
            }
        }
        WheelObjects.AddRange(foundWheelObjects);
        WheelBackingObjects.AddRange(foundWheelBackingObjects);
        
        Color? wheelReflectionTintColor = ColorUtility.TryParseHtmlString($"#{WheelReflectionTint.Value}", out Color parsedA) ? parsedA : null;
        foreach (GameObject wheelObject in WheelObjects)
        {
            ReplaceableMatcapObject matcapObject = new(wheelObject);
            WheelObjectsMatcapObjects.Add(matcapObject);
            
            await matcapObject.SetCustomMatcap(WheelMatcap.Value.ToLowerInvariant() == "default"
                ? "default"
                : $"{DataPath}/{WheelMatcap.Value}");

            matcapObject.SetReflectionColor(wheelReflectionTintColor, WheelReflectionIntensity.Value);
        }
        
        Color? wheelBackingReflectionTintColor = ColorUtility.TryParseHtmlString($"#{WheelBackingReflectionTint.Value}", out Color parsedB) ? parsedB : null;
        foreach (GameObject wheelBackingObject in WheelBackingObjects)
        {
            ReplaceableMatcapObject matcapObject = new(wheelBackingObject);
            WheelBackingObjectsMatcapObjects.Add(matcapObject);
            
            await matcapObject.SetCustomMatcap(WheelBackingMatcap.Value.ToLowerInvariant() == "default"
                ? "default"
                : $"{DataPath}/{WheelBackingMatcap.Value}");
            
            matcapObject.SetReflectionColor(wheelBackingReflectionTintColor, WheelBackingReflectionIntensity.Value);
        }
    }

    private static bool _initializingWheel; 
    internal static async Task ReinitializeWheel()
    {
        WheelObjects.Clear();
        WheelObjectsMatcapObjects.Clear();
        WheelBackingObjects.Clear();
        WheelBackingObjectsMatcapObjects.Clear();
        
        while (_initializingWheel)
        {
            await Awaitable.EndOfFrameAsync();
        }
        await Awaitable.MainThreadAsync();

        _initializingWheel = true;
        await InitializeWheel();
        _initializingWheel = false;
    }

    private static bool _hasInitializedVRWandMaterials;

    internal static async Task InitializeVRWandMaterials(Renderer renderer)
    {
        if (_hasInitializedVRWandMaterials)
        {
            return;
        }
        
        _hasInitializedVRWandMaterials = true;

        Material[] sharedMaterials = [];
        try
        {
            sharedMaterials = renderer.GetSharedMaterialArray();
            for (int idx = 0; idx < VRWandMaterialMatcapObjects.Length; idx++)
            {
                await Awaitable.MainThreadAsync();
                
                // the 0th index is on purpose, that's the main body material
                VRWandMaterialMatcapObjects[idx] = new ReplaceableMatcapObject(sharedMaterials[0]);

                await VRWandMaterialMatcapObjects[idx]!.SetCustomMatcap(
                    VRWandMaterialFilenames[idx].Value.ToLowerInvariant() == "default"
                        ? "default"
                        : $"{DataPath}/{VRWandMaterialFilenames[idx].Value}");
                
                VRWandMaterialMatcapObjects[idx]!.SetReflectionColor(Color.white, 0);
            }
        }
        catch (Exception e)
        {
            Log.LogError(e);
        }

        try
        {
            // Unlit/Matcap is not VR-compatible, InitializeVRWandMaterials can only be triggered when VR controller objects exist
            // so now's a good time to re-init character materials with a compatible shader/material
            
            await InitializeCharacterMaterials(sharedMaterials[0]);
            
            while (CharacterMaterials.Any(x => x == null))
            {
                await Awaitable.EndOfFrameAsync();
            }
            Patches.PatchOnAssetsReplaced.ResetCharacterMaterials();
        }
        catch (Exception e)
        {
            Log.LogError(e);
        }
    }

    private static readonly Color _defaultLogoXCharacterColor = new(0.114f, 0.46f, 0.726f);
    private static readonly Color _defaultLogoDCharacterColor = new(0.679f, 0.158f, 0.313f);
    private static readonly Color _defaultLogoBackingColor = new(0.082f, 0.088f, 0.123f); // _BaseColor
    private static readonly Color _defaultLogoBackingReflectionColor = new(0.454f, 0.374f, 1f); // _LightColor
    private static readonly Color _defaultLogoOutlineColor = Color.white;
    private static readonly Color _defaultLogoOutlineReflectionColor = Color.white;
    private static Material? _replacementLogoGlowMaterial;
    private static void SetMenuLogoColors()
    {
        Material xLogoMaterial = Resources.FindObjectsOfTypeAll<Material>().First(x => x.name == "BlueLOGOMenu");
        Material dLogoMaterial = Resources.FindObjectsOfTypeAll<Material>().First(x => x.name == "RedLOGOMenu");
        Material backgroundLogoMaterial = Resources.FindObjectsOfTypeAll<Material>().First(x => x.name == "BlackLOGOMenu");
        
        Transform logoMesh = GameObject.Find("XD_title_mesh_2023/CenterPoint").transform;
        Renderer backingMeshRenderer = GameObject.Find("XD_title_mesh_2023/CenterPoint/XD_logo_Back_2021").GetComponent<Renderer>();
        
        if (_replacementLogoGlowMaterial == null)
        {
            _replacementLogoGlowMaterial = new Material(backgroundLogoMaterial);

            // yes this is backwards lol
            Transform sparkleLight = logoMesh.Find("AnimatorLayerDark (1)");
            
            logoMesh.Find("D_outline").GetComponent<Renderer>().sharedMaterial = _replacementLogoGlowMaterial;
            logoMesh.Find("Rhythm").GetComponent<Renderer>().sharedMaterial = _replacementLogoGlowMaterial;
            logoMesh.Find("Spin").GetComponent<Renderer>().sharedMaterial = _replacementLogoGlowMaterial;
            logoMesh.Find("X_outline").GetComponent<Renderer>().sharedMaterial = _replacementLogoGlowMaterial;
            logoMesh.Find("XD_logo_Back_White").GetComponent<Renderer>().sharedMaterial = _replacementLogoGlowMaterial;
            
            for (int idx = 0; idx < sparkleLight.childCount; idx++)
            {
                sparkleLight.GetChild(idx).GetComponent<Renderer>().sharedMaterial = _replacementLogoGlowMaterial;
            }
        }
        
        Color? xColor = ColorUtility.TryParseHtmlString($"#{MenuLogoXColor.Value}", out Color xParsed) ? xParsed : null;
        Color? dColor = ColorUtility.TryParseHtmlString($"#{MenuLogoDColor.Value}", out Color dParsed) ? dParsed : null;
        Color? bgColor = ColorUtility.TryParseHtmlString($"#{MenuLogoBackingColor.Value}", out Color bgParsed) ? bgParsed : null;
        Color? bgReflectColor = ColorUtility.TryParseHtmlString($"#{MenuLogoBackingReflectionColor.Value}", out Color bgReflectParsed) ? bgReflectParsed : null;
        Color? outlineColor = ColorUtility.TryParseHtmlString($"#{MenuLogoOutlineColor.Value}", out Color oParsed) ? oParsed : null;
        Color? outlineReflectColor = ColorUtility.TryParseHtmlString($"#{MenuLogoOutlineReflectionColor.Value}", out Color oReflectParsed) ? oReflectParsed : null;
        
        xLogoMaterial.SetColor(BaseColor, xColor ?? _defaultLogoXCharacterColor);
        dLogoMaterial.SetColor(BaseColor, dColor ?? _defaultLogoDCharacterColor);
        backgroundLogoMaterial.SetColor(BaseColor, bgColor ?? _defaultLogoBackingColor);
        backgroundLogoMaterial.SetColor(LightColor, bgReflectColor ?? _defaultLogoBackingReflectionColor);
        _replacementLogoGlowMaterial.SetColor(BaseColor, outlineColor ?? _defaultLogoOutlineColor);
        _replacementLogoGlowMaterial.SetColor(LightColor, outlineReflectColor ?? _defaultLogoOutlineReflectionColor);
        
        // for some reason updating the black material doesn't apply to these??? i'm blaming unity lol
        backingMeshRenderer.material.SetColor(BaseColor, bgColor ?? _defaultLogoBackingColor);
        backingMeshRenderer.material.SetColor(LightColor, bgReflectColor ?? _defaultLogoBackingReflectionColor);
        Transform sparkleDark = logoMesh.Find("AnimatorLayerLight (1)");
        for (int idx = 0; idx < sparkleDark.childCount; idx++)
        {
            Renderer sparkleRenderer = sparkleDark.GetChild(idx).GetComponent<Renderer>();
            sparkleRenderer.material.SetColor(BaseColor, bgColor ?? _defaultLogoBackingColor);
            sparkleRenderer.material.SetColor(LightColor, bgReflectColor ?? _defaultLogoBackingReflectionColor);
        }
    }
}