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
    }
}