using System;
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
    
    internal static ConfigEntry<string> CharacterPinkMatcap = null!;
    internal static ConfigEntry<string> CharacterBlueMatcap = null!;
    internal static ConfigEntry<string> CharacterWhiteMatcap = null!;
    internal static ConfigEntry<string> CharacterBlackMatcap = null!;
    internal static ConfigEntry<string> CharacterBlackGreenMatcap = null!;
    internal static ConfigEntry<string> CharacterGreenMatcap = null!;
    internal static ConfigEntry<string> CharacterPurpleMatcap = null!;

    private void RegisterConfigEntries()
    {
        TrackStripMatcap = Config.Bind("Matcaps", nameof(TrackStripMatcap), "default", 
            "Filename of the matcap texture to use for the track edges");
        WheelMatcap = Config.Bind("Matcaps", nameof(WheelMatcap), "default", 
            "Filename of the matcap texture to use for the wheel");
        WheelBackingMatcap = Config.Bind("Matcaps", nameof(WheelBackingMatcap), "default", 
            "Filename of the matcap texture to use for meshes behind the note wedges on the wheel");
        
        TranslationHelper.AddTranslation($"{TRANSLATION_PREFIX}{nameof(TrackStripMatcap)}", "Track edge matcap texture");
        TranslationHelper.AddTranslation($"{TRANSLATION_PREFIX}{nameof(WheelMatcap)}", "Wheel matcap texture");
        TranslationHelper.AddTranslation($"{TRANSLATION_PREFIX}{nameof(WheelBackingMatcap)}", "Wheel wedge backing matcap texture");
        
        CharacterPinkMatcap = Config.Bind("Matcaps", nameof(CharacterPinkMatcap), "default", 
            "Filename of the matcap texture to use for the mascot/character's default pink material");
        CharacterBlueMatcap = Config.Bind("Matcaps", nameof(CharacterBlueMatcap), "default", 
            "Filename of the matcap texture to use for the mascot/character's default blue material");
        CharacterWhiteMatcap = Config.Bind("Matcaps", nameof(CharacterWhiteMatcap), "default", 
            "Filename of the matcap texture to use for the mascot/character's default white material");
        CharacterBlackMatcap = Config.Bind("Matcaps", nameof(CharacterBlackMatcap), "default", 
            "Filename of the matcap texture to use for the mascot/character's default black material");
        CharacterBlackGreenMatcap = Config.Bind("Matcaps", nameof(CharacterBlackGreenMatcap), "default", 
            "Filename of the matcap texture to use for the mascot/character's default green-tinted black material");
        CharacterGreenMatcap = Config.Bind("Matcaps", nameof(CharacterGreenMatcap), "default", 
            "Filename of the matcap texture to use for the mascot/character's default green material");
        CharacterPurpleMatcap = Config.Bind("Matcaps", nameof(CharacterPurpleMatcap), "default", 
            "Filename of the matcap texture to use for the mascot/character's default purple material");
        
        TranslationHelper.AddTranslation($"{TRANSLATION_PREFIX}{nameof(CharacterPinkMatcap)}", "Pink material matcap");
        TranslationHelper.AddTranslation($"{TRANSLATION_PREFIX}{nameof(CharacterBlueMatcap)}", "Blue material matcap");
        TranslationHelper.AddTranslation($"{TRANSLATION_PREFIX}{nameof(CharacterWhiteMatcap)}", "White material matcap");
        TranslationHelper.AddTranslation($"{TRANSLATION_PREFIX}{nameof(CharacterBlackMatcap)}", "Black material matcap");
        TranslationHelper.AddTranslation($"{TRANSLATION_PREFIX}{nameof(CharacterBlackGreenMatcap)}", "Green-tinted black material matcap");
        TranslationHelper.AddTranslation($"{TRANSLATION_PREFIX}{nameof(CharacterGreenMatcap)}", "Green material matcap");
        TranslationHelper.AddTranslation($"{TRANSLATION_PREFIX}{nameof(CharacterPurpleMatcap)}", "Purple material matcap");
        
        TranslationHelper.AddTranslation($"{TRANSLATION_PREFIX}GameplayElements", "Gameplay Elements");
        TranslationHelper.AddTranslation($"{TRANSLATION_PREFIX}CharacterMaterials", "Character Materials");
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
        
        #region CharacterPinkMatcap
        CustomGroup characterPinkMatcapGroup = UIHelper.CreateGroup(modGroup, "CharacterPinkMatcapGroup");
        characterPinkMatcapGroup.LayoutDirection = Axis.Horizontal;
        UIHelper.CreateLabel(characterPinkMatcapGroup, "CharacterPinkMatcapLabel", $"{TRANSLATION_PREFIX}{nameof(CharacterPinkMatcap)}");
        CustomInputField characterPinkMatcapInput = UIHelper.CreateInputField(characterPinkMatcapGroup, "CharacterPinkMatcapInput",
            (oldValue, newValue) =>
            {
                if (oldValue == newValue)
                {
                    return;
                }
            
                CharacterPinkMatcap.Value = newValue;

                Task.Run(async () =>
                {
                    try
                    {
                        await Awaitable.MainThreadAsync();
                    
                        await _characterPinkMaterialMatcapObject?.SetCustomMatcap(newValue.ToLowerInvariant() == "default"
                            ? "default"
                            : $"{DataPath}/{CharacterPinkMatcap.Value}")!;
                    }
                    catch (Exception e)
                    {
                        Log.LogError(e);
                    }
                });
            });
        characterPinkMatcapInput.InputField.SetText(CharacterPinkMatcap.Value);
        #endregion
        
        #region CharacterBlueMatcap
        CustomGroup characterBlueMatcapGroup = UIHelper.CreateGroup(modGroup, "CharacterBlueMatcapGroup");
        characterBlueMatcapGroup.LayoutDirection = Axis.Horizontal;
        UIHelper.CreateLabel(characterBlueMatcapGroup, "CharacterBlueMatcapLabel", $"{TRANSLATION_PREFIX}{nameof(CharacterBlueMatcap)}");
        CustomInputField characterBlueMatcapInput = UIHelper.CreateInputField(characterBlueMatcapGroup, "CharacterBlueMatcapInput",
            (oldValue, newValue) =>
            {
                if (oldValue == newValue)
                {
                    return;
                }
            
                CharacterBlueMatcap.Value = newValue;

                Task.Run(async () =>
                {
                    try
                    {
                        await Awaitable.MainThreadAsync();
                    
                        await _characterBlueMaterialMatcapObject?.SetCustomMatcap(newValue.ToLowerInvariant() == "default"
                            ? "default"
                            : $"{DataPath}/{CharacterBlueMatcap.Value}")!;
                    }
                    catch (Exception e)
                    {
                        Log.LogError(e);
                    }
                });
            });
        characterBlueMatcapInput.InputField.SetText(CharacterBlueMatcap.Value);
        #endregion
        
        #region CharacterWhiteMatcap
        CustomGroup characterWhiteMatcapGroup = UIHelper.CreateGroup(modGroup, "CharacterWhiteMatcapGroup");
        characterWhiteMatcapGroup.LayoutDirection = Axis.Horizontal;
        UIHelper.CreateLabel(characterWhiteMatcapGroup, "CharacterWhiteMatcapLabel", $"{TRANSLATION_PREFIX}{nameof(CharacterWhiteMatcap)}");
        CustomInputField characterWhiteMatcapInput = UIHelper.CreateInputField(characterWhiteMatcapGroup, "CharacterWhiteMatcapInput",
            (oldValue, newValue) =>
            {
                if (oldValue == newValue)
                {
                    return;
                }
            
                CharacterWhiteMatcap.Value = newValue;

                Task.Run(async () =>
                {
                    try
                    {
                        await Awaitable.MainThreadAsync();
                    
                        await _characterWhiteMaterialMatcapObject?.SetCustomMatcap(newValue.ToLowerInvariant() == "default"
                            ? "default"
                            : $"{DataPath}/{CharacterWhiteMatcap.Value}")!;
                    }
                    catch (Exception e)
                    {
                        Log.LogError(e);
                    }
                });
            });
        characterWhiteMatcapInput.InputField.SetText(CharacterWhiteMatcap.Value);
        #endregion
        
        #region CharacterBlackMatcap
        CustomGroup characterBlackMatcapGroup = UIHelper.CreateGroup(modGroup, "CharacterBlackMatcapGroup");
        characterBlackMatcapGroup.LayoutDirection = Axis.Horizontal;
        UIHelper.CreateLabel(characterBlackMatcapGroup, "CharacterBlackMatcapLabel", $"{TRANSLATION_PREFIX}{nameof(CharacterBlackMatcap)}");
        CustomInputField characterBlackMatcapInput = UIHelper.CreateInputField(characterBlackMatcapGroup, "CharacterBlackMatcapInput",
            (oldValue, newValue) =>
            {
                if (oldValue == newValue)
                {
                    return;
                }
            
                CharacterBlackMatcap.Value = newValue;

                Task.Run(async () =>
                {
                    try
                    {
                        await Awaitable.MainThreadAsync();
                    
                        await _characterBlackMaterialMatcapObject?.SetCustomMatcap(newValue.ToLowerInvariant() == "default"
                            ? "default"
                            : $"{DataPath}/{CharacterBlackMatcap.Value}")!;
                    }
                    catch (Exception e)
                    {
                        Log.LogError(e);
                    }
                });
            });
        characterBlackMatcapInput.InputField.SetText(CharacterBlackMatcap.Value);
        #endregion
        
        #region CharacterBlackGreenMatcap
        CustomGroup characterBlackGreenMatcapGroup = UIHelper.CreateGroup(modGroup, "CharacterBlackGreenMatcapGroup");
        characterBlackGreenMatcapGroup.LayoutDirection = Axis.Horizontal;
        UIHelper.CreateLabel(characterBlackGreenMatcapGroup, "CharacterBlackGreenMatcapLabel", $"{TRANSLATION_PREFIX}{nameof(CharacterBlackGreenMatcap)}");
        CustomInputField characterBlackGreenMatcapInput = UIHelper.CreateInputField(characterBlackGreenMatcapGroup, "CharacterBlackGreenMatcapInput",
            (oldValue, newValue) =>
            {
                if (oldValue == newValue)
                {
                    return;
                }
            
                CharacterBlackGreenMatcap.Value = newValue;

                Task.Run(async () =>
                {
                    try
                    {
                        await Awaitable.MainThreadAsync();
                    
                        await _characterBlackGreenMaterialMatcapObject?.SetCustomMatcap(newValue.ToLowerInvariant() == "default"
                            ? "default"
                            : $"{DataPath}/{CharacterBlackGreenMatcap.Value}")!;
                    }
                    catch (Exception e)
                    {
                        Log.LogError(e);
                    }
                });
            });
        characterBlackGreenMatcapInput.InputField.SetText(CharacterBlackGreenMatcap.Value);
        #endregion
        
        #region CharacterGreenMatcap
        CustomGroup characterGreenMatcapGroup = UIHelper.CreateGroup(modGroup, "CharacterGreenMatcapGroup");
        characterGreenMatcapGroup.LayoutDirection = Axis.Horizontal;
        UIHelper.CreateLabel(characterGreenMatcapGroup, "CharacterGreenMatcapLabel", $"{TRANSLATION_PREFIX}{nameof(CharacterGreenMatcap)}");
        CustomInputField characterGreenMatcapInput = UIHelper.CreateInputField(characterGreenMatcapGroup, "CharacterGreenMatcapInput",
            (oldValue, newValue) =>
            {
                if (oldValue == newValue)
                {
                    return;
                }
            
                CharacterGreenMatcap.Value = newValue;

                Task.Run(async () =>
                {
                    try
                    {
                        await Awaitable.MainThreadAsync();
                    
                        await _characterGreenMaterialMatcapObject?.SetCustomMatcap(newValue.ToLowerInvariant() == "default"
                            ? "default"
                            : $"{DataPath}/{CharacterGreenMatcap.Value}")!;
                    }
                    catch (Exception e)
                    {
                        Log.LogError(e);
                    }
                });
            });
        characterGreenMatcapInput.InputField.SetText(CharacterGreenMatcap.Value);
        #endregion
        
        #region CharacterPurpleMatcap
        CustomGroup characterPurpleMatcapGroup = UIHelper.CreateGroup(modGroup, "CharacterPurpleMatcapGroup");
        characterPurpleMatcapGroup.LayoutDirection = Axis.Horizontal;
        UIHelper.CreateLabel(characterPurpleMatcapGroup, "CharacterPurpleMatcapLabel", $"{TRANSLATION_PREFIX}{nameof(CharacterPurpleMatcap)}");
        CustomInputField characterPurpleMatcapInput = UIHelper.CreateInputField(characterPurpleMatcapGroup, "CharacterPurpleMatcapInput",
            (oldValue, newValue) =>
            {
                if (oldValue == newValue)
                {
                    return;
                }
            
                CharacterPurpleMatcap.Value = newValue;

                Task.Run(async () =>
                {
                    try
                    {
                        await Awaitable.MainThreadAsync();
                    
                        await _characterPurpleMaterialMatcapObject?.SetCustomMatcap(newValue.ToLowerInvariant() == "default"
                            ? "default"
                            : $"{DataPath}/{CharacterPurpleMatcap.Value}")!;
                    }
                    catch (Exception e)
                    {
                        Log.LogError(e);
                    }
                });
            });
        characterPurpleMatcapInput.InputField.SetText(CharacterPurpleMatcap.Value);
        #endregion
    }
}