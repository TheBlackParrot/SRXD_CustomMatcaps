using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using XDMenuPlay.TrackMenus;
#pragma warning disable CS0618 // Type or member is obsolete

namespace CustomMatcaps.Patches;

[HarmonyPatch]
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal class PatchOnAssetsReplaced
{
    [HarmonyPatch(typeof(WheelVisuals), nameof(WheelVisuals.OnAssetsReplaced))]
    [HarmonyPostfix]
    private static void PatchWheelAssets()
    {
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
    private static List<Material> _previousMaterials = [];
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
        
        _menuCharacterAnimationHandler = __instance;
        _previousMaterials = __instance.mainMesh.sharedMaterials.ToList();
        __instance.mainMesh.SetSharedMaterials(Plugin.CharacterMaterials);
    }

    internal static void ResetCharacterMaterials() => 
        _menuCharacterAnimationHandler?.mainMesh.SetSharedMaterials(Plugin.ApplyMatcapsToCharacters.Value ? Plugin.CharacterMaterials! : _previousMaterials);

    [HarmonyPatch(typeof(ActionBasedController), nameof(ActionBasedController.OnEnable))]
    [HarmonyPostfix]
    private static void PatchVRWands(ActionBasedController __instance)
    {
        _ = InitializeVRWand(__instance);
    }

    private static async Task InitializeVRWand(ActionBasedController controller)
    {
        Transform? modelTransform = null;
        while (modelTransform == null)
        {
            modelTransform = controller.model?.Find("XRControllerWand/VRwand");
            await Awaitable.EndOfFrameAsync();
        }

        Renderer renderer = modelTransform.GetComponent<Renderer>();
        
        if (Plugin.VRWandMaterialMatcapObjects[0] == null)
        {
            await Plugin.InitializeVRWandMaterials(renderer);
        }

        renderer.SetSharedMaterials(Plugin.VRWandMaterials);
    }
}