#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DistanceVisibility.Editor
{
    /// <summary>Keeps lilToon's complete inspector and adds distance controls.</summary>
    public sealed class DistanceVisibilityInspector : lilToon.lilToonInspector
    {
        private MaterialProperty distanceEnabled;
        private MaterialProperty nearEnabled;
        private MaterialProperty nearDistance;
        private MaterialProperty nearFade;
        private MaterialProperty farEnabled;
        private MaterialProperty farDistance;
        private MaterialProperty farFade;
        private MaterialProperty useMeshCenter;
        private static bool showDistanceVisibility = true;

        protected override void LoadCustomProperties(MaterialProperty[] properties, Material material)
        {
            isCustomShader = true;
            ReplaceToCustomShaders();
            isShowRenderMode = !material.shader.name.Contains("/[Optional] ");

            distanceEnabled = FindProperty("_DV_Enabled", properties);
            nearEnabled = FindProperty("_DV_NearEnabled", properties);
            nearDistance = FindProperty("_DV_NearDistance", properties);
            nearFade = FindProperty("_DV_NearFade", properties);
            farEnabled = FindProperty("_DV_FarEnabled", properties);
            farDistance = FindProperty("_DV_FarDistance", properties);
            farFade = FindProperty("_DV_FarFade", properties);
            useMeshCenter = FindProperty("_DV_UseMeshCenter", properties);
        }

        protected override void DrawCustomProperties(Material material)
        {
            showDistanceVisibility = Foldout("lilToon Distance Visibility", "距离显示", showDistanceVisibility);
            if(!showDistanceVisibility) return;

            EditorGUILayout.BeginVertical(boxOuter);
            EditorGUILayout.LabelField("距离显示 / Distance Visibility", customToggleFont);
            EditorGUILayout.BeginVertical(boxInnerHalf);

            UpgradeLegacyMaterials();
            DrawBooleanProperty(distanceEnabled, "启用距离显示");
            if(distanceEnabled.hasMixedValue || distanceEnabled.floatValue >= 0.5f)
            {
                DrawBooleanProperty(useMeshCenter, "使用网格中心（对象原点）");

                EditorGUILayout.Space(3.0f);
                DrawBooleanProperty(nearEnabled, "启用近端限制");
                if(nearEnabled.hasMixedValue || nearEnabled.floatValue >= 0.5f)
                {
                    m_MaterialEditor.ShaderProperty(nearDistance, "近端距离（米）");
                    m_MaterialEditor.ShaderProperty(nearFade, "近端过渡距离（米）");
                }

                EditorGUILayout.Space(3.0f);
                DrawBooleanProperty(farEnabled, "启用远端限制");
                if(farEnabled.hasMixedValue || farEnabled.floatValue >= 0.5f)
                {
                    m_MaterialEditor.ShaderProperty(farDistance, "远端距离（米）");
                    m_MaterialEditor.ShaderProperty(farFade, "远端过渡距离（米）");
                }

                ClampNonNegative(nearDistance);
                ClampNonNegative(nearFade);
                ClampNonNegative(farDistance);
                ClampNonNegative(farFade);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndVertical();
        }

        private static void DrawBooleanProperty(MaterialProperty property, string label)
        {
            EditorGUI.showMixedValue = property.hasMixedValue;
            EditorGUI.BeginChangeCheck();
            bool value = EditorGUILayout.ToggleLeft(label, property.floatValue >= 0.5f);
            if(EditorGUI.EndChangeCheck()) property.floatValue = value ? 1.0f : 0.0f;
            EditorGUI.showMixedValue = false;
        }

        private static void ClampNonNegative(MaterialProperty property)
        {
            if(!property.hasMixedValue && property.floatValue < 0.0f) property.floatValue = 0.0f;
        }

        private void UpgradeLegacyMaterials()
        {
            if(m_MaterialEditor == null) return;
            foreach(Object target in m_MaterialEditor.targets)
            {
                Material targetMaterial = target as Material;
                if(targetMaterial == null || !targetMaterial.HasProperty("_DV_Version"))
                {
                    continue;
                }

                float oldVersion = targetMaterial.GetFloat("_DV_Version");
                if(oldVersion >= 3.5f) continue;
                if(oldVersion < 2.5f)
                {
                    int oldMode = Mathf.Clamp(Mathf.RoundToInt(targetMaterial.GetFloat("_DV_Mode")), 0, 2);
                    targetMaterial.SetFloat("_DV_NearEnabled", oldMode == 1 ? 0.0f : 1.0f);
                    targetMaterial.SetFloat("_DV_FarEnabled", oldMode == 2 ? 0.0f : 1.0f);
                    targetMaterial.SetFloat("_DV_UseMeshCenter", 0.0f);
                }
                targetMaterial.SetFloat("_DV_Enabled", 0.0f);
                targetMaterial.SetFloat("_DV_Version", 4.0f);
                EditorUtility.SetDirty(targetMaterial);
            }
        }

        protected override void ReplaceToCustomShaders()
        {
            const string shaderName = DistanceVisibilityShaderManager.ShaderPrefix;

            lts         = DistanceVisibilityShaderManager.FindGeneratedShader(shaderName + "/lilToon");
            ltsc        = DistanceVisibilityShaderManager.FindGeneratedShader("Hidden/" + shaderName + "/Cutout");
            ltst        = DistanceVisibilityShaderManager.FindGeneratedShader("Hidden/" + shaderName + "/Transparent");
            ltsot       = DistanceVisibilityShaderManager.FindGeneratedShader("Hidden/" + shaderName + "/OnePassTransparent");
            ltstt       = DistanceVisibilityShaderManager.FindGeneratedShader("Hidden/" + shaderName + "/TwoPassTransparent");

            ltso        = DistanceVisibilityShaderManager.FindGeneratedShader("Hidden/" + shaderName + "/OpaqueOutline");
            ltsco       = DistanceVisibilityShaderManager.FindGeneratedShader("Hidden/" + shaderName + "/CutoutOutline");
            ltsto       = DistanceVisibilityShaderManager.FindGeneratedShader("Hidden/" + shaderName + "/TransparentOutline");
            ltsoto      = DistanceVisibilityShaderManager.FindGeneratedShader("Hidden/" + shaderName + "/OnePassTransparentOutline");
            ltstto      = DistanceVisibilityShaderManager.FindGeneratedShader("Hidden/" + shaderName + "/TwoPassTransparentOutline");

            ltsoo       = DistanceVisibilityShaderManager.FindGeneratedShader(shaderName + "/[Optional] OutlineOnly/Opaque");
            ltscoo      = DistanceVisibilityShaderManager.FindGeneratedShader(shaderName + "/[Optional] OutlineOnly/Cutout");
            ltstoo      = DistanceVisibilityShaderManager.FindGeneratedShader(shaderName + "/[Optional] OutlineOnly/Transparent");

            ltstess     = DistanceVisibilityShaderManager.FindGeneratedShader("Hidden/" + shaderName + "/Tessellation/Opaque");
            ltstessc    = DistanceVisibilityShaderManager.FindGeneratedShader("Hidden/" + shaderName + "/Tessellation/Cutout");
            ltstesst    = DistanceVisibilityShaderManager.FindGeneratedShader("Hidden/" + shaderName + "/Tessellation/Transparent");
            ltstessot   = DistanceVisibilityShaderManager.FindGeneratedShader("Hidden/" + shaderName + "/Tessellation/OnePassTransparent");
            ltstesstt   = DistanceVisibilityShaderManager.FindGeneratedShader("Hidden/" + shaderName + "/Tessellation/TwoPassTransparent");
            ltstesso    = DistanceVisibilityShaderManager.FindGeneratedShader("Hidden/" + shaderName + "/Tessellation/OpaqueOutline");
            ltstessco   = DistanceVisibilityShaderManager.FindGeneratedShader("Hidden/" + shaderName + "/Tessellation/CutoutOutline");
            ltstessto   = DistanceVisibilityShaderManager.FindGeneratedShader("Hidden/" + shaderName + "/Tessellation/TransparentOutline");
            ltstessoto  = DistanceVisibilityShaderManager.FindGeneratedShader("Hidden/" + shaderName + "/Tessellation/OnePassTransparentOutline");
            ltstesstto  = DistanceVisibilityShaderManager.FindGeneratedShader("Hidden/" + shaderName + "/Tessellation/TwoPassTransparentOutline");

            ltsl        = DistanceVisibilityShaderManager.FindGeneratedShader(shaderName + "/lilToonLite");
            ltslc       = DistanceVisibilityShaderManager.FindGeneratedShader("Hidden/" + shaderName + "/Lite/Cutout");
            ltslt       = DistanceVisibilityShaderManager.FindGeneratedShader("Hidden/" + shaderName + "/Lite/Transparent");
            ltslot      = DistanceVisibilityShaderManager.FindGeneratedShader("Hidden/" + shaderName + "/Lite/OnePassTransparent");
            ltsltt      = DistanceVisibilityShaderManager.FindGeneratedShader("Hidden/" + shaderName + "/Lite/TwoPassTransparent");
            ltslo       = DistanceVisibilityShaderManager.FindGeneratedShader("Hidden/" + shaderName + "/Lite/OpaqueOutline");
            ltslco      = DistanceVisibilityShaderManager.FindGeneratedShader("Hidden/" + shaderName + "/Lite/CutoutOutline");
            ltslto      = DistanceVisibilityShaderManager.FindGeneratedShader("Hidden/" + shaderName + "/Lite/TransparentOutline");
            ltsloto     = DistanceVisibilityShaderManager.FindGeneratedShader("Hidden/" + shaderName + "/Lite/OnePassTransparentOutline");
            ltsltto     = DistanceVisibilityShaderManager.FindGeneratedShader("Hidden/" + shaderName + "/Lite/TwoPassTransparentOutline");

            ltsref      = DistanceVisibilityShaderManager.FindGeneratedShader("Hidden/" + shaderName + "/Refraction");
            ltsrefb     = DistanceVisibilityShaderManager.FindGeneratedShader("Hidden/" + shaderName + "/RefractionBlur");
            ltsfur      = DistanceVisibilityShaderManager.FindGeneratedShader("Hidden/" + shaderName + "/Fur");
            ltsfurc     = DistanceVisibilityShaderManager.FindGeneratedShader("Hidden/" + shaderName + "/FurCutout");
            ltsfurtwo   = DistanceVisibilityShaderManager.FindGeneratedShader("Hidden/" + shaderName + "/FurTwoPass");
            ltsfuro     = DistanceVisibilityShaderManager.FindGeneratedShader(shaderName + "/[Optional] FurOnly/Transparent");
            ltsfuroc    = DistanceVisibilityShaderManager.FindGeneratedShader(shaderName + "/[Optional] FurOnly/Cutout");
            ltsfurotwo  = DistanceVisibilityShaderManager.FindGeneratedShader(shaderName + "/[Optional] FurOnly/TwoPass");
            ltsgem      = DistanceVisibilityShaderManager.FindGeneratedShader("Hidden/" + shaderName + "/Gem");
            ltsfs       = DistanceVisibilityShaderManager.FindGeneratedShader(shaderName + "/[Optional] FakeShadow");

            ltsover     = DistanceVisibilityShaderManager.FindGeneratedShader(shaderName + "/[Optional] Overlay");
            ltsoover    = DistanceVisibilityShaderManager.FindGeneratedShader(shaderName + "/[Optional] OverlayOnePass");
            ltslover    = DistanceVisibilityShaderManager.FindGeneratedShader(shaderName + "/[Optional] LiteOverlay");
            ltsloover   = DistanceVisibilityShaderManager.FindGeneratedShader(shaderName + "/[Optional] LiteOverlayOnePass");

            ltsm        = DistanceVisibilityShaderManager.FindGeneratedShader(shaderName + "/lilToonMulti");
            ltsmo       = DistanceVisibilityShaderManager.FindGeneratedShader("Hidden/" + shaderName + "/MultiOutline");
            ltsmref     = DistanceVisibilityShaderManager.FindGeneratedShader("Hidden/" + shaderName + "/MultiRefraction");
            ltsmfur     = DistanceVisibilityShaderManager.FindGeneratedShader("Hidden/" + shaderName + "/MultiFur");
            ltsmgem     = DistanceVisibilityShaderManager.FindGeneratedShader("Hidden/" + shaderName + "/MultiGem");
        }
    }
}
#endif
