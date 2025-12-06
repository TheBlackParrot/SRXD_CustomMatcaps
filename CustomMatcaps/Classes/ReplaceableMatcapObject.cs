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
        _material = rootObject.GetComponent<Renderer>().sharedMaterial;
        _defaultMatcap = _material.GetTexture(MatCapTexId) as Texture2D;
    }

    public async Task SetCustomMatcap(string path)
    {
        _customMatcap = path.ToLowerInvariant() == "default" ? _defaultMatcap : (MatcapTextureCache.Get(path) ?? await MatcapTextureCache.Add(path));
        
        if (_material != null)
        {
            /*Plugin.Log.LogInfo("_material is not null");
            foreach (string propertyName in _material.GetPropertyNames(MaterialPropertyType.Texture))
            {
                Plugin.Log.LogInfo($"Property name: {propertyName} (Texture)");
            }
            foreach (string propertyName in _material.GetPropertyNames(MaterialPropertyType.Float))
            {
                Plugin.Log.LogInfo($"Property name: {propertyName} (Float)");
            }
            foreach (string propertyName in _material.GetPropertyNames(MaterialPropertyType.Vector))
            {
                Plugin.Log.LogInfo($"Property name: {propertyName} (Vector)");
            }*/
            
            /*Plugin.Log.LogInfo($"_TintColor: {_material.GetColor("_TintColor")}");
            Plugin.Log.LogInfo($"_HighlightTintColor: {_material.GetColor("_HighlightTintColor")}");
            Plugin.Log.LogInfo($"_ReflectionMultiply: {_material.GetColor("_ReflectionMultiply")}");*/
            
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
        else
        {
            Plugin.Log.LogWarning("_material is null");
        }
    }
}