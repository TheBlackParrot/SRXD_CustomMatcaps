using System;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using XDMenuPlay.Customise;

namespace CustomMatcaps.Patches;

[HarmonyPatch]
internal class PatchCustomiseOpenMenu
{
    [HarmonyPatch(typeof(XDCustomiseMenu), nameof(XDCustomiseMenu.OnCustomiseMenuBecameActive))]
    [HarmonyPostfix]
    private static void Patch(XDCustomiseMenu __instance)
    {
        Plugin.Log.LogInfo("customize menu became active");
        
        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(100); // wtf
                await Awaitable.MainThreadAsync();
                await Plugin.ReinitializeWheel();
            }
            catch (Exception e)
            {
                Plugin.Log.LogError(e);   
            }
        });
    }
}