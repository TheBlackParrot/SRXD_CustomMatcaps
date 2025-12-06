using System;
using System.Threading.Tasks;
using HarmonyLib;

namespace CustomMatcaps.Patches;

[HarmonyPatch]
internal class PatchOnAssetsReplaced
{
    [HarmonyPatch(typeof(WheelVisuals), nameof(WheelVisuals.OnAssetsReplaced))]
    [HarmonyPostfix]
    private static void Patch()
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
}