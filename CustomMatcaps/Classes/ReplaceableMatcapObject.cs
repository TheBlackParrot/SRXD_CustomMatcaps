using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
// ReSharper disable ConvertToPrimaryConstructor

namespace CustomMatcaps.Classes;

internal class ReplaceableMatcapObject
{
    private static readonly int MatCapTexId = Shader.PropertyToID("_MatCapTex");
    private static readonly int MatCapHighlightTexId = Shader.PropertyToID("_MatCapHightlightTex"); // hightlight
    private static readonly int TintColorId = Shader.PropertyToID("_TintColor");
    private static readonly int HighlightTintColorId = Shader.PropertyToID("_HighlightTintColor");
    private static readonly int ReflectionMultiplyId = Shader.PropertyToID("_ReflectionMultiply");
    private static readonly int ReflectionMapId = Shader.PropertyToID("_ReflectionMap");
    private static readonly int TexturePropertyId = Shader.PropertyToID("_Texture");

    private readonly Material? _material;

    private readonly Texture2D? _defaultMatcap;
    private Texture2D? _customMatcap = Texture2D.blackTexture;

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
            _material = renderer.sharedMaterial;
            _defaultMatcap = _material.GetTexture(MatCapTexId) as Texture2D;
        }
        catch (Exception)
        {
            // ignored
        }
    }
    
    public ReplaceableMatcapObject(Material material, Shader replacementShader)
    {
        material.shader = replacementShader;
        material.SetTexture(MatCapTexId, Texture2D.whiteTexture);
        
        _material = material;
        _defaultMatcap = Texture2D.whiteTexture;
        /*
        Plugin.Log.LogInfo($"Material {material.name}:");
        for (MaterialPropertyType type = MaterialPropertyType.Float; type <= MaterialPropertyType.ComputeBuffer; type++)
        {
            foreach (string propertyName in material.GetPropertyNames(type))
            {
                Plugin.Log.LogInfo($"{propertyName} ({type})");
            }
        }
        */
    }

    public async Task SetCustomMatcap(string path)
    {
        if (_material == null)
        {
            return;
        }
        
        _customMatcap = path.ToLowerInvariant() == "default" ? _defaultMatcap : (MatcapTextureCache.Get(path) ?? await MatcapTextureCache.Add(path));
        if (_customMatcap == null)
        {
            return;
        }
            
        _material.SetColor(TintColorId, Color.white);
        _material.SetColor(HighlightTintColorId, Color.black);
        _material.SetColor(ReflectionMultiplyId, Color.black); // reflection strength, give this a setting
        _material.SetTexture(MatCapHighlightTexId, Texture2D.blackTexture);
        _material.SetTexture(ReflectionMapId, Plugin.BlankCubemap);

        _material.SetTexture(MatCapTexId, _customMatcap);
        if (_material.HasProperty(TexturePropertyId))
        {
            _material.SetTexture(TexturePropertyId, _customMatcap);
        }
    }
}