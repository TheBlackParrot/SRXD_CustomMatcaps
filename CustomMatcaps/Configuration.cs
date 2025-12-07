using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BepInEx.Configuration;
using CustomMatcaps.Classes;
using SpinCore.Translation;
using SpinCore.UI;
using UnityEngine;

namespace CustomMatcaps;

public partial class Plugin
{
    internal static ConfigEntry<string> TrackStripMatcap = null!;
    internal static ConfigEntry<string> WheelMatcap = null!;
    internal static ConfigEntry<string> WheelBackingMatcap = null!;

    internal static ConfigEntry<bool> ApplyMatcapsToCharacters = null!;
    private static readonly List<ConfigEntry<string>> CharacterMaterialFilenames = [];
    private static readonly List<ConfigEntry<string>> VRWandMaterialFilenames = [];

    private void RegisterConfigEntries()
    {
        TrackStripMatcap = Config.Bind("Matcaps", nameof(TrackStripMatcap), "default", 
            "Filename of the matcap texture to use for the track edges");
        WheelMatcap = Config.Bind("Matcaps", nameof(WheelMatcap), "default", 
            "Filename of the matcap texture to use for the wheel");
        WheelBackingMatcap = Config.Bind("Matcaps", nameof(WheelBackingMatcap), "default", 
            "Filename of the matcap texture to use for meshes behind the note wedges on the wheel");

        ApplyMatcapsToCharacters = Config.Bind("Characters", nameof(ApplyMatcapsToCharacters), true,
            "Apply matcap overrides to character models");
        
        TranslationHelper.AddTranslation($"{TRANSLATION_PREFIX}{nameof(TrackStripMatcap)}", "Track edge matcap");
        TranslationHelper.AddTranslation($"{TRANSLATION_PREFIX}{nameof(WheelMatcap)}", "Wheel matcap");
        TranslationHelper.AddTranslation($"{TRANSLATION_PREFIX}{nameof(WheelBackingMatcap)}", "Wheel wedge backing matcap");
        TranslationHelper.AddTranslation($"{TRANSLATION_PREFIX}{nameof(ApplyMatcapsToCharacters)}", "Override character matcaps");
        
        TranslationHelper.AddTranslation($"{TRANSLATION_PREFIX}GameplayElements", "Gameplay Elements");
        TranslationHelper.AddTranslation($"{TRANSLATION_PREFIX}CharacterMaterials", "Character Materials");
        TranslationHelper.AddTranslation($"{TRANSLATION_PREFIX}VRWandMaterials", "VR Wand Materials");
        
        for (int idx = 0; idx < 7; idx++)
        {
            TranslationHelper.AddTranslation($"{TRANSLATION_PREFIX}CharacterMaterial{idx + 1}", $"Matcap for material slot #{idx + 1}");
            CharacterMaterialFilenames.Add(Config.Bind("Matcaps", $"CharacterMaterial{idx + 1}", "default",
                $"Filename of the matcap texture to use for the mascot/character's material slot #{idx + 1}"));
        }
        
        for (int idx = 0; idx < 3; idx++)
        {
            TranslationHelper.AddTranslation($"{TRANSLATION_PREFIX}VRWandMaterial{idx + 1}", $"Matcap for material slot #{idx + 1}");
            VRWandMaterialFilenames.Add(Config.Bind("Matcaps", $"VRWandMaterial{idx + 1}", "default",
                $"Filename of the matcap texture to use for the VR wand's material slot #{idx + 1}"));
        }
    }

    private static void CreateModPage()
    {
        CustomPage rootModPage = UIHelper.CreateCustomPage("ModSettings");
        rootModPage.OnPageLoad += RootModPageOnPageLoad;

        UIHelper.RegisterMenuInModSettingsRoot($"{TRANSLATION_PREFIX}ModName", rootModPage);
    }

    private static void RootModPageOnPageLoad(Transform rootModPageTransform)
    {
        CustomGroup modGroup = UIHelper.CreateGroup(rootModPageTransform, nameof(CustomMatcaps));
        UIHelper.CreateSectionHeader(modGroup, "ModGroupHeader", $"{TRANSLATION_PREFIX}ModName", false);

        UIHelper.CreateSectionHeader(modGroup, "GameplayElementsHeader", $"{TRANSLATION_PREFIX}GameplayElements", false);
        
        #region TrackStripMatcap
        CustomGroup trackStripMatcapGroup = UIHelper.CreateGroup(modGroup, "TrackStripMatcapGroup");
        trackStripMatcapGroup.LayoutDirection = Axis.Horizontal;
        UIHelper.CreateLabel(trackStripMatcapGroup, "TrackStripMatcapLabel", $"{TRANSLATION_PREFIX}{nameof(TrackStripMatcap)}");
        CustomInputField trackStripMatcapInput = UIHelper.CreateInputField(trackStripMatcapGroup, "TrackStripMatcapInput",
            (oldValue, newValue) =>
        {
            if (oldValue == newValue)
            {
                return;
            }
            
            TrackStripMatcap.Value = newValue;

            Task.Run(async () =>
            {
                try
                {
                    await Awaitable.MainThreadAsync();
                    
                    await _trackStripSolidMatcapObject?.SetCustomMatcap(newValue.ToLowerInvariant() == "default"
                        ? "default"
                        : $"{DataPath}/{TrackStripMatcap.Value}")!;
                }
                catch (Exception e)
                {
                    Log.LogError(e);
                }
            });
        });
        trackStripMatcapInput.InputField.SetText(TrackStripMatcap.Value);
        #endregion
        
        #region WheelMatcap
        CustomGroup wheelMatcapGroup = UIHelper.CreateGroup(modGroup, "WheelMatcapGroup");
        wheelMatcapGroup.LayoutDirection = Axis.Horizontal;
        UIHelper.CreateLabel(wheelMatcapGroup, "WheelMatcapLabel", $"{TRANSLATION_PREFIX}{nameof(WheelMatcap)}");
        CustomInputField wheelMatcapInput = UIHelper.CreateInputField(wheelMatcapGroup, "WheelMatcapInput",
            (oldValue, newValue) =>
            {
                if (oldValue == newValue)
                {
                    return;
                }
            
                WheelMatcap.Value = newValue;

                Task.Run(async () =>
                {
                    try
                    {
                        await InitializeWheel();
                        
                        await Awaitable.MainThreadAsync();

                        foreach (ReplaceableMatcapObject matcapObject in WheelObjectsMatcapObjects)
                        {
                            await matcapObject.SetCustomMatcap(newValue.ToLowerInvariant() == "default"
                                ? "default"
                                : $"{DataPath}/{WheelMatcap.Value}");
                        }
                    }
                    catch (Exception e)
                    {
                        Log.LogError(e);
                    }
                });
            });
        wheelMatcapInput.InputField.SetText(WheelMatcap.Value);
        #endregion
        
        #region WheelBackingMatcap
        CustomGroup wheelBackingMatcapGroup = UIHelper.CreateGroup(modGroup, "WheelBackingMatcapGroup");
        wheelBackingMatcapGroup.LayoutDirection = Axis.Horizontal;
        UIHelper.CreateLabel(wheelBackingMatcapGroup, "WheelBackingMatcapLabel", $"{TRANSLATION_PREFIX}{nameof(WheelBackingMatcap)}");
        CustomInputField wheelBackingMatcapInput = UIHelper.CreateInputField(wheelBackingMatcapGroup, "WheelBackingMatcapInput",
            (oldValue, newValue) =>
            {
                if (oldValue == newValue)
                {
                    return;
                }
            
                WheelBackingMatcap.Value = newValue;

                Task.Run(async () =>
                {
                    try
                    {
                        await InitializeWheel();
                        
                        await Awaitable.MainThreadAsync();

                        foreach (ReplaceableMatcapObject matcapObject in WheelBackingObjectsMatcapObjects)
                        {
                            await matcapObject.SetCustomMatcap(newValue.ToLowerInvariant() == "default"
                                ? "default"
                                : $"{DataPath}/{WheelBackingMatcap.Value}");
                        }
                    }
                    catch (Exception e)
                    {
                        Log.LogError(e);
                    }
                });
            });
        wheelBackingMatcapInput.InputField.SetText(WheelBackingMatcap.Value);
        #endregion
        
        UIHelper.CreateSectionHeader(modGroup, "CharacterMaterialsHeader", $"{TRANSLATION_PREFIX}CharacterMaterials", false);
        
        #region ApplyMatcapsToCharacters
        CustomGroup applyMatcapsToCharactersGroup = UIHelper.CreateGroup(modGroup, "ApplyMatcapsToCharactersGroup");
        applyMatcapsToCharactersGroup.LayoutDirection = Axis.Horizontal;
        UIHelper.CreateSmallToggle(applyMatcapsToCharactersGroup, nameof(ApplyMatcapsToCharacters),
            $"{TRANSLATION_PREFIX}{nameof(ApplyMatcapsToCharacters)}", ApplyMatcapsToCharacters.Value, value =>
            {
                ApplyMatcapsToCharacters.Value = value;
                Patches.PatchOnAssetsReplaced.ResetCharacterMaterials();
            });
        #endregion
        
        #region CharacterMaterials
        for (int idx = 0; idx < 7; idx++)
        {
            int humanIdx = idx + 1;
            
            CustomGroup characterMaterialGroup = UIHelper.CreateGroup(modGroup, $"CharacterMaterial{humanIdx}Group");
            characterMaterialGroup.LayoutDirection = Axis.Horizontal;
            UIHelper.CreateLabel(characterMaterialGroup, $"CharacterMaterial{humanIdx}Label", $"{TRANSLATION_PREFIX}CharacterMaterial{humanIdx}");

            int capturedIdx = idx;
            CustomInputField characterMaterialInput = UIHelper.CreateInputField(characterMaterialGroup, $"CharacterMaterial{humanIdx}Input",
                (oldValue, newValue) =>
                {
                    if (oldValue == newValue)
                    {
                        return;
                    }
            
                    CharacterMaterialFilenames[capturedIdx].Value = newValue;

                    Task.Run(async () =>
                    {
                        try
                        {
                            await Awaitable.MainThreadAsync();

                            await CharacterMaterialMatcapObjects[capturedIdx]?.SetCustomMatcap(newValue.ToLowerInvariant() == "default"
                                ? "default"
                                : $"{DataPath}/{CharacterMaterialFilenames[capturedIdx].Value}")!;
                        }
                        catch (Exception e)
                        {
                            Log.LogError(e);
                        }
                    });
                });
            
            characterMaterialInput.InputField.SetText(CharacterMaterialFilenames[idx].Value);
        }
        #endregion
        
        UIHelper.CreateSectionHeader(modGroup, "CharacterMaterialsHeader", $"{TRANSLATION_PREFIX}VRWandMaterials", false);
        
        #region VRWandMaterials
        for (int idx = 0; idx < 3; idx++)
        {
            int humanIdx = idx + 1;
            
            CustomGroup vrWandMaterialGroup = UIHelper.CreateGroup(modGroup, $"VRWandMaterial{humanIdx}Group");
            vrWandMaterialGroup.LayoutDirection = Axis.Horizontal;
            UIHelper.CreateLabel(vrWandMaterialGroup, $"VRWandMaterial{humanIdx}Label", $"{TRANSLATION_PREFIX}VRWandMaterial{humanIdx}");

            int capturedIdx = idx;
            CustomInputField vrWandMaterialInput = UIHelper.CreateInputField(vrWandMaterialGroup, $"VRWandMaterial{humanIdx}Input",
                (oldValue, newValue) =>
                {
                    if (oldValue == newValue)
                    {
                        return;
                    }
            
                    VRWandMaterialFilenames[capturedIdx].Value = newValue;

                    Task.Run(async () =>
                    {
                        try
                        {
                            await Awaitable.MainThreadAsync();

                            await VRWandMaterialMatcapObjects[capturedIdx]?.SetCustomMatcap(newValue.ToLowerInvariant() == "default"
                                ? "default"
                                : $"{DataPath}/{VRWandMaterialFilenames[capturedIdx].Value}")!;
                        }
                        catch (Exception e)
                        {
                            Log.LogError(e);
                        }
                    });
                });
            
            vrWandMaterialInput.InputField.SetText(VRWandMaterialFilenames[idx].Value);
        }
        #endregion
    }
}