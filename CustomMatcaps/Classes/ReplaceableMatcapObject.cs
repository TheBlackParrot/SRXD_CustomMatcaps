using System;
using System.Threading.Tasks;
using UnityEngine;
// ReSharper disable ConvertToPrimaryConstructor

namespace CustomMatcaps.Classes;

internal class ReplaceableMatcapObject
{
    private static readonly int MatCapTexId = Shader.PropertyToID("_MatCapTex");
    private static readonly int TintColorId = Shader.PropertyToID("_TintColor");
    private static readonly int HighlightTintColorId = Shader.PropertyToID("_HighlightTintColor");
    private static readonly int ReflectionMultiplyId = Shader.PropertyToID("_ReflectionMultiply");

    internal readonly Material? MaterialObject;

    private readonly Texture2D? _defaultMatcap;
    private readonly Color _defaultReflectionTint = Color.white;
    private Texture2D? _customMatcap = Texture2D.whiteTexture;

    public ReplaceableMatcapObject(GameObject rootObject)
    {
        if (rootObject == null)
        {
            return;
        }
        
        if (!rootObject.TryGetComponent(out Renderer renderer))
        {
            return;
        }

        try
        {
            MaterialObject = renderer.sharedMaterial;
            _defaultMatcap = MaterialObject.GetTexture(MatCapTexId) as Texture2D;
            _defaultReflectionTint = MaterialObject.GetColor(HighlightTintColorId);
        }
        catch (Exception)
        {
            // ignored
        }
    }
    
    public ReplaceableMatcapObject(Shader replacementShader)
    {
        MaterialObject = new Material(replacementShader);
        _defaultMatcap = Texture2D.whiteTexture;
        _defaultReflectionTint = Color.white;
    }
    
    public ReplaceableMatcapObject(Material sourceMaterial)
    {
        MaterialObject = new Material(sourceMaterial);
        _defaultMatcap = Texture2D.whiteTexture;
        _defaultReflectionTint = sourceMaterial.GetColor(HighlightTintColorId);
        
#if DEBUG
        Plugin.Log.LogInfo(MaterialObject.name);
        for (MaterialPropertyType type = MaterialPropertyType.Float; type <= MaterialPropertyType.ComputeBuffer; type++)
        {
            string[] types = MaterialObject.GetPropertyNames(type);
            foreach (string propertyName in types)
            {
                Plugin.Log.LogInfo($"{propertyName}");
            }
        }
#endif
    }

    public async Task SetCustomMatcap(string path)
    {
        if (MaterialObject == null)
        {
            return;
        }
        
        _customMatcap = path.ToLowerInvariant() == "default" ? _defaultMatcap : (MatcapTextureCache.Get(path) ?? await MatcapTextureCache.Add(path));
        if (_customMatcap == null)
        {
            return;
        }
        
        MaterialObject.SetColor(TintColorId, Color.white);
        //MaterialObject.SetColor(HighlightTintColorId, tintColor ?? DefaultReflectionTint);
        //MaterialObject.SetColor(ReflectionMultiplyId, (tintColor ?? DefaultReflectionTint) * Plugin.WheelReflectionIntensity.Value);
        //MaterialObject.SetTexture(MatCapHighlightTexId, Texture2D.blackTexture);
        //MaterialObject.SetTexture(ReflectionMapId, Plugin.BlankCubemap);

        MaterialObject.SetTexture(MatCapTexId, _customMatcap);
    }

    public void SetReflectionColor(Color? tintColor, float intensity)
    {
        MaterialObject?.SetColor(ReflectionMultiplyId, (tintColor ?? _defaultReflectionTint) * intensity);
        MaterialObject?.SetColor(HighlightTintColorId, (tintColor ?? _defaultReflectionTint) * intensity);
    }
}