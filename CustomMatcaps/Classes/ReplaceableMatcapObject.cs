using System;
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

    internal readonly Material? MaterialObject;

    private readonly Texture2D? _defaultMatcap;
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
    }
    
    public ReplaceableMatcapObject(Material sourceMaterial)
    {
        MaterialObject = new Material(sourceMaterial);
        _defaultMatcap = Texture2D.whiteTexture;
        
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
        MaterialObject.SetColor(HighlightTintColorId, Color.black);
        MaterialObject.SetColor(ReflectionMultiplyId, Color.black);
        MaterialObject.SetTexture(MatCapHighlightTexId, Texture2D.blackTexture);
        MaterialObject.SetTexture(ReflectionMapId, Plugin.BlankCubemap);

        MaterialObject.SetTexture(MatCapTexId, _customMatcap);
    }
}