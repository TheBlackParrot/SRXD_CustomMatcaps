using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using XDMenuPlay.TrackMenus;

namespace CustomMatcaps.Patches;

[HarmonyPatch]
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal class PatchOnAssetsReplaced
{
    [HarmonyPatch(typeof(WheelVisuals), nameof(WheelVisuals.OnAssetsReplaced))]
    [HarmonyPostfix]
    private static void PatchWheelAssets()
    {
        Plugin.Log.LogInfo("customize menu preview updated");
        
        Task.Run(async () =>
        {
            try
            {
                await Plugin.ReinitializeWheel();
            }
            catch (Exception e)
            {
                Plugin.Log.LogError(e);   
            }
        });
    }

    private static string _previousCharacterConfigId = string.Empty;
    private static readonly List<Material> _previousMaterials = [];
    private static MenuCharacterAnimationHandler? _menuCharacterAnimationHandler;
    
    [HarmonyPatch(typeof(MenuCharacterAnimationHandler), nameof(MenuCharacterAnimationHandler.UpdateModel))]
    [HarmonyPatch(typeof(MenuCharacterAnimationHandler), nameof(MenuCharacterAnimationHandler.OnEnable))]
    [HarmonyPostfix]
    private static void PatchCharacterAssets(MenuCharacterAnimationHandler __instance, MethodBase __originalMethod)
    {
        if (!Plugin.ApplyMatcapsToCharacters.Value)
        {
            return;
        }
        
        if (__originalMethod.Name == nameof(MenuCharacterAnimationHandler.UpdateModel))
        {
            // if OnEnable is the invoker, we probably don't need this to check things
            if (_previousCharacterConfigId == __instance.currentAssistantConfig.id)
            {
                return;
            }
        }
        _previousCharacterConfigId = __instance.currentAssistantConfig.id;
        
        Plugin.Log.LogInfo($"original method: {__originalMethod.Name}");
        _menuCharacterAnimationHandler = __instance;
        __instance.mainMesh.GetSharedMaterials(_previousMaterials);
        __instance.mainMesh.SetSharedMaterials(Plugin.CharacterMaterials);
    }

    internal static void ResetCharacterMaterials() => 
        _menuCharacterAnimationHandler?.mainMesh.SetSharedMaterials(Plugin.ApplyMatcapsToCharacters.Value ? Plugin.CharacterMaterials! : _previousMaterials);
}