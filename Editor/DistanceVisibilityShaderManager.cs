#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace DistanceVisibility.Editor
{
    /// <summary>
    /// Creates a private lilToon custom-shader family and switches only the
    /// materials explicitly selected by the user to that family.
    /// </summary>
    public static class DistanceVisibilityShaderManager
    {
        internal const string ShaderPrefix = "DistanceVisibility/lilToon";
        internal const string GeneratedRoot = "Packages/com.zhuozhi.liltoon-distance-visibility/Generated/lilToon";

        private const int GeneratorVersion = 4;
        private const string MarkerFileName = "distance_visibility_version.txt";
        private const string DataFileName = "lilCustomShaderDatas.lilblock";
        private const string PropertiesFileName = "lilCustomShaderProperties.lilblock";
        private const string InsertFileName = "lilCustomShaderInsert.lilblock";
        private const string CustomHlslFileName = "custom.hlsl";
        private const string CustomInsertFileName = "distance_visibility.hlsl";
        private static Dictionary<string, Shader> generatedShadersByName;

        private static readonly Dictionary<string, string> OriginalToDistance =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "lilToon", ShaderPrefix + "/lilToon" },
                { "Hidden/lilToonCutout", "Hidden/" + ShaderPrefix + "/Cutout" },
                { "Hidden/lilToonTransparent", "Hidden/" + ShaderPrefix + "/Transparent" },
                { "Hidden/lilToonOnePassTransparent", "Hidden/" + ShaderPrefix + "/OnePassTransparent" },
                { "Hidden/lilToonTwoPassTransparent", "Hidden/" + ShaderPrefix + "/TwoPassTransparent" },

                { "Hidden/lilToonOutline", "Hidden/" + ShaderPrefix + "/OpaqueOutline" },
                { "Hidden/lilToonCutoutOutline", "Hidden/" + ShaderPrefix + "/CutoutOutline" },
                { "Hidden/lilToonTransparentOutline", "Hidden/" + ShaderPrefix + "/TransparentOutline" },
                { "Hidden/lilToonOnePassTransparentOutline", "Hidden/" + ShaderPrefix + "/OnePassTransparentOutline" },
                { "Hidden/lilToonTwoPassTransparentOutline", "Hidden/" + ShaderPrefix + "/TwoPassTransparentOutline" },

                { "_lil/[Optional] lilToonOutlineOnly", ShaderPrefix + "/[Optional] OutlineOnly/Opaque" },
                { "_lil/[Optional] lilToonOutlineOnlyCutout", ShaderPrefix + "/[Optional] OutlineOnly/Cutout" },
                { "_lil/[Optional] lilToonOutlineOnlyTransparent", ShaderPrefix + "/[Optional] OutlineOnly/Transparent" },

                { "Hidden/lilToonTessellation", "Hidden/" + ShaderPrefix + "/Tessellation/Opaque" },
                { "Hidden/lilToonTessellationCutout", "Hidden/" + ShaderPrefix + "/Tessellation/Cutout" },
                { "Hidden/lilToonTessellationTransparent", "Hidden/" + ShaderPrefix + "/Tessellation/Transparent" },
                { "Hidden/lilToonTessellationOnePassTransparent", "Hidden/" + ShaderPrefix + "/Tessellation/OnePassTransparent" },
                { "Hidden/lilToonTessellationTwoPassTransparent", "Hidden/" + ShaderPrefix + "/Tessellation/TwoPassTransparent" },
                { "Hidden/lilToonTessellationOutline", "Hidden/" + ShaderPrefix + "/Tessellation/OpaqueOutline" },
                { "Hidden/lilToonTessellationCutoutOutline", "Hidden/" + ShaderPrefix + "/Tessellation/CutoutOutline" },
                { "Hidden/lilToonTessellationTransparentOutline", "Hidden/" + ShaderPrefix + "/Tessellation/TransparentOutline" },
                { "Hidden/lilToonTessellationOnePassTransparentOutline", "Hidden/" + ShaderPrefix + "/Tessellation/OnePassTransparentOutline" },
                { "Hidden/lilToonTessellationTwoPassTransparentOutline", "Hidden/" + ShaderPrefix + "/Tessellation/TwoPassTransparentOutline" },

                { "Hidden/lilToonLite", ShaderPrefix + "/lilToonLite" },
                { "Hidden/lilToonLiteCutout", "Hidden/" + ShaderPrefix + "/Lite/Cutout" },
                { "Hidden/lilToonLiteTransparent", "Hidden/" + ShaderPrefix + "/Lite/Transparent" },
                { "Hidden/lilToonLiteOnePassTransparent", "Hidden/" + ShaderPrefix + "/Lite/OnePassTransparent" },
                { "Hidden/lilToonLiteTwoPassTransparent", "Hidden/" + ShaderPrefix + "/Lite/TwoPassTransparent" },
                { "Hidden/lilToonLiteOutline", "Hidden/" + ShaderPrefix + "/Lite/OpaqueOutline" },
                { "Hidden/lilToonLiteCutoutOutline", "Hidden/" + ShaderPrefix + "/Lite/CutoutOutline" },
                { "Hidden/lilToonLiteTransparentOutline", "Hidden/" + ShaderPrefix + "/Lite/TransparentOutline" },
                { "Hidden/lilToonLiteOnePassTransparentOutline", "Hidden/" + ShaderPrefix + "/Lite/OnePassTransparentOutline" },
                { "Hidden/lilToonLiteTwoPassTransparentOutline", "Hidden/" + ShaderPrefix + "/Lite/TwoPassTransparentOutline" },

                { "Hidden/lilToonRefraction", "Hidden/" + ShaderPrefix + "/Refraction" },
                { "Hidden/lilToonRefractionBlur", "Hidden/" + ShaderPrefix + "/RefractionBlur" },
                { "Hidden/lilToonFur", "Hidden/" + ShaderPrefix + "/Fur" },
                { "Hidden/lilToonFurCutout", "Hidden/" + ShaderPrefix + "/FurCutout" },
                { "Hidden/lilToonFurTwoPass", "Hidden/" + ShaderPrefix + "/FurTwoPass" },
                { "_lil/[Optional] lilToonFurOnlyTransparent", ShaderPrefix + "/[Optional] FurOnly/Transparent" },
                { "_lil/[Optional] lilToonFurOnlyCutout", ShaderPrefix + "/[Optional] FurOnly/Cutout" },
                { "_lil/[Optional] lilToonFurOnlyTwoPass", ShaderPrefix + "/[Optional] FurOnly/TwoPass" },
                { "Hidden/lilToonGem", "Hidden/" + ShaderPrefix + "/Gem" },
                { "_lil/[Optional] lilToonFakeShadow", ShaderPrefix + "/[Optional] FakeShadow" },

                { "_lil/[Optional] lilToonOverlay", ShaderPrefix + "/[Optional] Overlay" },
                { "_lil/[Optional] lilToonOverlayOnePass", ShaderPrefix + "/[Optional] OverlayOnePass" },
                { "_lil/[Optional] lilToonLiteOverlay", ShaderPrefix + "/[Optional] LiteOverlay" },
                { "_lil/[Optional] lilToonLiteOverlayOnePass", ShaderPrefix + "/[Optional] LiteOverlayOnePass" },

                { "_lil/lilToonMulti", ShaderPrefix + "/lilToonMulti" },
                { "Hidden/lilToonMultiOutline", "Hidden/" + ShaderPrefix + "/MultiOutline" },
                { "Hidden/lilToonMultiRefraction", "Hidden/" + ShaderPrefix + "/MultiRefraction" },
                { "Hidden/lilToonMultiFur", "Hidden/" + ShaderPrefix + "/MultiFur" },
                { "Hidden/lilToonMultiGem", "Hidden/" + ShaderPrefix + "/MultiGem" }
            };

        private static readonly Dictionary<string, string> DistanceToOriginal =
            OriginalToDistance.ToDictionary(pair => pair.Value, pair => pair.Key, StringComparer.Ordinal);

        internal static bool IsSupportedOriginal(Shader shader)
        {
            if(shader == null) return false;
            if(OriginalToDistance.ContainsKey(shader.name)) return true;

            Shader original;
            return LyumaWaifu2dBridge.TryGetOfficialOriginal(
                    shader,
                    out original) &&
                original != null &&
                OriginalToDistance.ContainsKey(original.name);
        }

        internal static bool IsDistanceShader(Shader shader)
        {
            return shader != null &&
                (DistanceToOriginal.ContainsKey(shader.name) ||
                    LyumaWaifu2dBridge.IsCombinedDistanceShader(shader));
        }

        internal static Shader GetDistanceShader(Shader original)
        {
            if(original == null) return null;
            if(IsDistanceShader(original)) return original;

            Shader baseOriginal = original;
            Shader officialOriginal;
            bool preserveWaifu2d = LyumaWaifu2dBridge.
                TryGetOfficialOriginal(original, out officialOriginal);
            if(preserveWaifu2d && officialOriginal != null)
                baseOriginal = officialOriginal;
            string targetName;
            if(baseOriginal == null ||
                !OriginalToDistance.TryGetValue(
                    baseOriginal.name,
                    out targetName))
            {
                return null;
            }
            if(!EnsureGeneratedShaders()) return null;
            Shader distanceShader = FindGeneratedShader(targetName);
            if(distanceShader == null || !preserveWaifu2d)
                return distanceShader;

            Shader combinedShader;
            return LyumaWaifu2dBridge.TryComposeDistanceShader(
                    distanceShader,
                    out combinedShader)
                ? combinedShader
                : null;
        }

        internal static Shader GetOriginalShader(Shader distanceShader)
        {
            if(distanceShader == null) return null;
            string originalName;
            if(DistanceToOriginal.TryGetValue(
                distanceShader.name,
                out originalName))
            {
                return Shader.Find(originalName);
            }

            Shader directDistanceShader;
            if(!LyumaWaifu2dBridge.TryGetDirectDistanceShader(
                    distanceShader,
                    out directDistanceShader) ||
                directDistanceShader == null ||
                !DistanceToOriginal.TryGetValue(
                    directDistanceShader.name,
                    out originalName))
            {
                return null;
            }

            Shader original = Shader.Find(originalName);
            Shader waifu2dShader;
            return original != null &&
                LyumaWaifu2dBridge.TryGetOfficialWaifu2dShader(
                    original,
                    out waifu2dShader)
                ? waifu2dShader
                : null;
        }

        /// <summary>Used by automated project validation; normal users do not need to call this.</summary>
        public static void RunBatchValidation()
        {
            if(!EnsureGeneratedShaders(true))
                throw new InvalidOperationException("Distance Visibility shader generation failed.");

            var missing = new List<string>();
            var compileErrors = new List<string>();
            foreach(string shaderName in OriginalToDistance.Values)
            {
                Shader shader = FindGeneratedShader(shaderName);
                if(shader == null)
                {
                    missing.Add(shaderName);
                    continue;
                }
                if(shader.FindPropertyIndex("_DV_Enabled") < 0) missing.Add(shaderName + " (missing properties)");
                if(ShaderUtil.ShaderHasError(shader)) compileErrors.Add(shaderName);
            }

            if(missing.Count > 0 || compileErrors.Count > 0)
            {
                throw new InvalidOperationException(
                    "Distance Visibility validation failed. Missing: " + string.Join(", ", missing) +
                    "; compile errors: " + string.Join(", ", compileErrors));
            }
            Debug.Log("lilToon 距离显示：批处理验证通过，共 " + OriginalToDistance.Count + " 个材质 Shader 变体。");
        }

        internal static bool EnsureGeneratedShaders(bool force = false)
        {
            string sourceFolder = FindBaseShaderResourcesFolder();
            if(string.IsNullOrEmpty(sourceFolder))
            {
                EditorUtility.DisplayDialog(
                    "lilToon 距离显示",
                    "没有找到 lilToon 的 BaseShaderResources。请确认已正确安装 lilToon 2.x。",
                    "确定");
                return false;
            }

            string markerPath = GeneratedRoot + "/" + MarkerFileName;
            string expectedMarker = BuildMarker(sourceFolder);
            Shader existingShader = Shader.Find(ShaderPrefix + "/lilToon");
            if(!force && File.Exists(markerPath) &&
               File.ReadAllText(markerPath) == expectedMarker &&
               existingShader != null && existingShader.FindPropertyIndex("_DV_Enabled") >= 0)
            {
                return true;
            }

            try
            {
                generatedShadersByName = null;
                Directory.CreateDirectory(GeneratedRoot);
                WriteSupportFiles();

                string[] sourceFiles = Directory.GetFiles(sourceFolder, "*.lilinternal", SearchOption.TopDirectoryOnly);
                if(sourceFiles.Length == 0)
                    throw new InvalidDataException("BaseShaderResources 中没有 .lilinternal 文件。");

                foreach(string sourceFile in sourceFiles)
                {
                    string outputName = Path.GetFileNameWithoutExtension(sourceFile) + ".lilcontainer";
                    string outputPath = GeneratedRoot + "/" + outputName;
                    string content = File.ReadAllText(sourceFile);
                    File.WriteAllText(outputPath, TransformContainer(content), new UTF8Encoding(false));
                }

                File.WriteAllText(markerPath, expectedMarker, new UTF8Encoding(false));
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                AssetDatabase.ImportAsset(
                    GeneratedRoot,
                    ImportAssetOptions.ImportRecursive |
                    ImportAssetOptions.ForceSynchronousImport |
                    ImportAssetOptions.ForceUpdate);
                generatedShadersByName = null;

                Shader generated = Shader.Find(ShaderPrefix + "/lilToon");
                if(generated == null || generated.FindPropertyIndex("_DV_Enabled") < 0)
                    throw new InvalidOperationException("生成的距离显示 Shader 未能载入，请查看 Console 中的 Shader 编译错误。");
                return true;
            }
            catch(Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("lilToon 距离显示", "生成 Shader 失败：\n\n" + exception.Message, "确定");
                return false;
            }
        }

        private static string FindBaseShaderResourcesFolder()
        {
            Shader shader = Shader.Find("lilToon");
            if(shader != null)
            {
                string shaderPath = NormalizePath(AssetDatabase.GetAssetPath(shader));
                string packageRoot = NormalizePath(Path.GetDirectoryName(Path.GetDirectoryName(shaderPath)));
                string candidate = packageRoot + "/BaseShaderResources";
                if(Directory.Exists(candidate)) return candidate;
            }

            foreach(string guid in AssetDatabase.FindAssets("lilCustomShaderDatas"))
            {
                string path = NormalizePath(AssetDatabase.GUIDToAssetPath(guid));
                if(!path.EndsWith("/BaseShaderResources/lilCustomShaderDatas.lilblock", StringComparison.OrdinalIgnoreCase))
                    continue;
                string folder = NormalizePath(Path.GetDirectoryName(path));
                if(File.Exists(folder + "/lts.lilinternal")) return folder;
            }
            return null;
        }

        private static string BuildMarker(string sourceFolder)
        {
            string packageJson = NormalizePath(Path.GetDirectoryName(sourceFolder)) + "/package.json";
            string packageVersion = File.Exists(packageJson) ? File.ReadAllText(packageJson) : "unknown";
            Match versionMatch = Regex.Match(packageVersion, "\\\"version\\\"\\s*:\\s*\\\"([^\\\"]+)\\\"");
            string version = versionMatch.Success ? versionMatch.Groups[1].Value : "unknown";
            int fileCount = Directory.GetFiles(sourceFolder, "*.lilinternal", SearchOption.TopDirectoryOnly).Length;
            return "generator=" + GeneratorVersion + "\nlilToon=" + version + "\ncontainers=" + fileCount + "\n";
        }

        private static void WriteSupportFiles()
        {
            File.WriteAllText(
                GeneratedRoot + "/" + DataFileName,
                "ShaderName \"" + ShaderPrefix + "\"\n" +
                "EditorName \"" + typeof(DistanceVisibilityInspector).FullName + "\"\n",
                new UTF8Encoding(false));

            File.WriteAllText(
                GeneratedRoot + "/" + PropertiesFileName,
                "        // lilToon Distance Visibility\n" +
                "        [HideInInspector] _DV_Enabled (\"Enable Distance Visibility\", Float) = 0\n" +
                "        [HideInInspector] _DV_Mode (\"Legacy Distance Mode\", Float) = 0\n" +
                "        [HideInInspector] _DV_NearEnabled (\"Enable Near Limit\", Float) = 1\n" +
                "        [HideInInspector] _DV_NearDistance (\"Near Distance\", Float) = 2.0\n" +
                "        [HideInInspector] _DV_NearFade (\"Near Transition\", Float) = 0.2\n" +
                "        [HideInInspector] _DV_FarEnabled (\"Enable Far Limit\", Float) = 1\n" +
                "        [HideInInspector] _DV_FarDistance (\"Far Distance\", Float) = 5.0\n" +
                "        [HideInInspector] _DV_FarFade (\"Far Transition\", Float) = 0.2\n" +
                "        [HideInInspector] _DV_UseMeshCenter (\"Use Mesh Center\", Float) = 0\n" +
                "        [HideInInspector] _DV_Version (\"Distance Visibility Version\", Float) = 4\n",
                new UTF8Encoding(false));

            File.WriteAllText(
                GeneratedRoot + "/" + InsertFileName,
                "#include \"" + CustomInsertFileName + "\"\n",
                new UTF8Encoding(false));

            File.WriteAllText(
                GeneratedRoot + "/" + CustomHlslFileName,
                "// Generated bridge for lilToon Distance Visibility.\n" +
                "#define LIL_CUSTOM_PROPERTIES \\\n" +
                "    float _DV_Enabled; \\\n" +
                "    float _DV_Mode; \\\n" +
                "    float _DV_NearEnabled; \\\n" +
                "    float _DV_NearDistance; \\\n" +
                "    float _DV_NearFade; \\\n" +
                "    float _DV_FarEnabled; \\\n" +
                "    float _DV_FarDistance; \\\n" +
                "    float _DV_FarFade; \\\n" +
                "    float _DV_UseMeshCenter; \\\n" +
                "    float _DV_Version;\n\n" +
                "#define LIL_V2F_FORCE_POSITION_WS\n" +
                "#define BEFORE_UNPACK_V2F \\\n" +
                "    DistanceVisibilityClip(lilToAbsolutePositionWS(input.positionWS), lilToAbsolutePositionWS(lilTransformOStoWS(float3(0.0, 0.0, 0.0))), input.positionCS.xy);\n",
                new UTF8Encoding(false));

            File.WriteAllText(
                GeneratedRoot + "/" + CustomInsertFileName,
                "// The include is inserted after Unity and lilToon common helpers.\n" +
                "float DistanceVisibilityHash(float2 pixelPosition)\n" +
                "{\n" +
                "    // Stable screen-space interleaved gradient noise for a dithered fade.\n" +
                "    return frac(52.9829189 * frac(dot(floor(pixelPosition), float2(0.06711056, 0.00583715))));\n" +
                "}\n\n" +
                "float DistanceVisibilityFadeIn(float distanceToCamera, float boundary, float width)\n" +
                "{\n" +
                "    width = max(width, 0.0);\n" +
                "    return width < 0.00001 ? step(boundary, distanceToCamera) : saturate((distanceToCamera - boundary) / width);\n" +
                "}\n\n" +
                "float DistanceVisibilityFadeOut(float distanceToCamera, float boundary, float width)\n" +
                "{\n" +
                "    width = max(width, 0.0);\n" +
                "    return width < 0.00001 ? step(distanceToCamera, boundary) : saturate((boundary - distanceToCamera) / width);\n" +
                "}\n\n" +
                "void DistanceVisibilityClip(float3 absolutePositionWS, float3 meshCenterWS, float2 pixelPosition)\n" +
                "{\n" +
                "#if !defined(UNITY_PASS_META)\n" +
                "    float useMeshCenter = step(0.5, _DV_UseMeshCenter);\n" +
                "    float3 distancePositionWS = lerp(absolutePositionWS, meshCenterWS, useMeshCenter);\n" +
                "    float distanceToCamera = distance(distancePositionWS, _WorldSpaceCameraPos.xyz);\n" +
                "    float nearVisibility = DistanceVisibilityFadeIn(distanceToCamera, max(_DV_NearDistance, 0.0), _DV_NearFade);\n" +
                "    float farVisibility = DistanceVisibilityFadeOut(distanceToCamera, max(_DV_FarDistance, 0.0), _DV_FarFade);\n" +
                "    float isNearOnly = step(0.5, _DV_Mode) * (1.0 - step(1.5, _DV_Mode));\n" +
                "    float isFarOnly = step(1.5, _DV_Mode);\n" +
                "    float useLegacyMode = 1.0 - step(2.5, _DV_Version);\n" +
                "    float nearEnabled = lerp(step(0.5, _DV_NearEnabled), 1.0 - isNearOnly, useLegacyMode);\n" +
                "    float farEnabled = lerp(step(0.5, _DV_FarEnabled), 1.0 - isFarOnly, useLegacyMode);\n" +
                "    nearVisibility = lerp(1.0, nearVisibility, nearEnabled);\n" +
                "    farVisibility = lerp(1.0, farVisibility, farEnabled);\n" +
                "    float visibility = min(nearVisibility, farVisibility);\n" +
                "    visibility = lerp(1.0, visibility, step(0.5, _DV_Enabled));\n" +
                "    float ditherThreshold = DistanceVisibilityHash(pixelPosition) * (254.0 / 256.0) + (1.0 / 256.0);\n" +
                "    clip(visibility - ditherThreshold);\n" +
                "#endif\n" +
                "}\n",
                new UTF8Encoding(false));
        }

        private static string TransformContainer(string content)
        {
            var shaderDeclaration = new Regex("(?m)^(\\s*Shader\\s+\\\")([^\\\"]+)(\\\")");
            content = shaderDeclaration.Replace(
                content,
                match => match.Groups[1].Value + MapShaderName(match.Groups[2].Value) + match.Groups[3].Value,
                1);

            content = Regex.Replace(
                content,
                "(?m)(lilPassShaderName\\s+\\\")Hidden/ltspass_",
                "$1Hidden/" + ShaderPrefix + "/ltspass_");

            content = InsertCustomProperties(content);
            if(content.IndexOf("HLSLINCLUDE", StringComparison.Ordinal) >= 0)
            {
                content = content.Replace(
                    "ENDHLSL",
                    "        #include \"" + CustomHlslFileName + "\"\n    ENDHLSL");

                Match subShader = Regex.Match(content, "(?m)^(\\s*)lilSubShader(?:BRP|LWRP|URP|HDRP)\\s+");
                if(subShader.Success && content.IndexOf("lilSubShaderInsert", StringComparison.Ordinal) < 0)
                {
                    content = content.Insert(
                        subShader.Index,
                        subShader.Groups[1].Value + "lilSubShaderInsert \"" + InsertFileName + "\"\n");
                }
            }
            return content;
        }

        private static string MapShaderName(string originalName)
        {
            string mapped;
            if(OriginalToDistance.TryGetValue(originalName, out mapped)) return mapped;
            if(originalName.StartsWith("Hidden/ltspass_", StringComparison.Ordinal))
                return "Hidden/" + ShaderPrefix + "/" + originalName.Substring("Hidden/".Length);

            // Baker and property-only internals are kept private as well, even
            // though normal material conversion never selects them directly.
            string safeName = originalName.Replace("Hidden/", string.Empty).Replace('/', '_');
            return "Hidden/" + ShaderPrefix + "/Internal/" + safeName;
        }

        private static string InsertCustomProperties(string content)
        {
            if(content.IndexOf("lilProperties \"" + PropertiesFileName + "\"", StringComparison.Ordinal) >= 0)
                return content;

            int propertiesIndex = content.IndexOf("Properties", StringComparison.Ordinal);
            if(propertiesIndex < 0) return content;
            int openBrace = content.IndexOf('{', propertiesIndex);
            if(openBrace < 0) return content;

            int depth = 0;
            for(int index = openBrace; index < content.Length; index++)
            {
                if(content[index] == '{') depth++;
                else if(content[index] == '}')
                {
                    depth--;
                    if(depth == 0)
                    {
                        string insertion = "        lilProperties \"" + PropertiesFileName + "\"\n    ";
                        return content.Insert(index, insertion);
                    }
                }
            }
            return content;
        }

        private static string NormalizePath(string path)
        {
            return string.IsNullOrEmpty(path) ? path : path.Replace('\\', '/').TrimEnd('/');
        }

        internal static Shader FindGeneratedShader(string shaderName)
        {
            Shader shader = Shader.Find(shaderName);
            if(shader != null) return shader;

            if(!Directory.Exists(GeneratedRoot)) return null;
            if(generatedShadersByName == null)
            {
                generatedShadersByName = new Dictionary<string, Shader>(StringComparer.Ordinal);
                foreach(string filePath in Directory.GetFiles(GeneratedRoot, "*.lilcontainer", SearchOption.TopDirectoryOnly))
                {
                    string assetPath = NormalizePath(filePath);
                    shader = AssetDatabase.LoadAssetAtPath<Shader>(assetPath);
                    if(shader == null)
                        shader = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Shader>().FirstOrDefault();
                    if(shader != null) generatedShadersByName[shader.name] = shader;
                }
            }

            generatedShadersByName.TryGetValue(shaderName, out shader);
            return shader;
        }

        internal static void InitializeMaterial(Material material)
        {
            if(material == null) return;
            material.SetFloat("_DV_Enabled", 0.0f);
            material.SetFloat("_DV_Mode", 0.0f);
            material.SetFloat("_DV_NearEnabled", 1.0f);
            material.SetFloat("_DV_NearDistance", 2.0f);
            material.SetFloat("_DV_NearFade", 0.2f);
            material.SetFloat("_DV_FarEnabled", 1.0f);
            material.SetFloat("_DV_FarDistance", 5.0f);
            material.SetFloat("_DV_FarFade", 0.2f);
            material.SetFloat("_DV_UseMeshCenter", 0.0f);
            material.SetFloat("_DV_Version", GeneratorVersion);
        }

        internal static void ConvertMaterials(IEnumerable<Material> materials)
        {
            Material[] targets = materials.Where(material => material != null && IsSupportedOriginal(material.shader)).Distinct().ToArray();
            if(targets.Length == 0) return;
            if(!EnsureGeneratedShaders()) return;

            Undo.RecordObjects(targets, "添加 lilToon 距离显示");
            int converted = 0;
            foreach(Material material in targets)
            {
                Shader targetShader = GetDistanceShader(material.shader);
                if(targetShader == null)
                {
                    Debug.LogError("lilToon 距离显示：找不到与 " + material.shader.name + " 对应的 Shader。", material);
                    continue;
                }

                int renderQueue = material.renderQueue;
                material.shader = targetShader;
                material.renderQueue = renderQueue;
                InitializeMaterial(material);
                EditorUtility.SetDirty(material);
                converted++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log("lilToon 距离显示：已转换 " + converted + " 个材质。", targets[0]);
        }

        internal static void RestoreMaterials(IEnumerable<Material> materials)
        {
            Material[] targets = materials.Where(material => material != null && IsDistanceShader(material.shader)).Distinct().ToArray();
            if(targets.Length == 0) return;

            Undo.RecordObjects(targets, "移除 lilToon 距离显示");
            int restored = 0;
            foreach(Material material in targets)
            {
                Shader originalShader = GetOriginalShader(material.shader);
                if(originalShader == null)
                {
                    Debug.LogError("lilToon 距离显示：无法找到原始 Shader。", material);
                    continue;
                }

                int renderQueue = material.renderQueue;
                material.shader = originalShader;
                material.renderQueue = renderQueue;
                EditorUtility.SetDirty(material);
                restored++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log("lilToon 距离显示：已还原 " + restored + " 个材质。", targets[0]);
        }

        private static class LyumaWaifu2dBridge
        {
            private const BindingFlags StaticMethodFlags =
                BindingFlags.Static |
                BindingFlags.Public |
                BindingFlags.NonPublic;

            private static readonly Type OfficialAdapterType = FindType(
                "LyumaShader.LilToonWaifu2dAdapter"
            );
            private static readonly Type CustomAdapterType = FindType(
                "LyumaShader.GenericLilCustomWaifu2dAdapter"
            );
            private static readonly MethodInfo GetOfficialOriginalMethod =
                FindShaderMethod(OfficialAdapterType, "GetOriginalShader");
            private static readonly MethodInfo GetOfficialWaifu2dMethod =
                FindShaderMethod(OfficialAdapterType, "GetWaifu2dShader");
            private static readonly MethodInfo GetCustomOriginalMethod =
                FindShaderMethod(CustomAdapterType, "GetOriginalShader");
            private static readonly MethodInfo GetCustomWaifu2dMethod =
                FindShaderMethod(CustomAdapterType, "GetWaifu2dShader");
            private static readonly MethodInfo IsCustomWaifu2dMethod =
                FindShaderMethod(CustomAdapterType, "IsWaifu2dShader");
            private static readonly MethodInfo IsDistanceVisibilityMethod =
                FindShaderMethod(
                    CustomAdapterType,
                    "IsDistanceVisibilityShader"
                );

            internal static bool TryGetOfficialOriginal(
                Shader shader,
                out Shader original
            )
            {
                return TryInvokeShader(
                    GetOfficialOriginalMethod,
                    shader,
                    out original
                );
            }

            internal static bool TryGetOfficialWaifu2dShader(
                Shader shader,
                out Shader waifu2dShader
            )
            {
                return TryInvokeShader(
                    GetOfficialWaifu2dMethod,
                    shader,
                    out waifu2dShader
                );
            }

            internal static bool TryGetDirectDistanceShader(
                Shader shader,
                out Shader directDistanceShader
            )
            {
                return TryInvokeShader(
                    GetCustomOriginalMethod,
                    shader,
                    out directDistanceShader
                );
            }

            internal static bool TryComposeDistanceShader(
                Shader shader,
                out Shader combinedShader
            )
            {
                return TryInvokeShader(
                    GetCustomWaifu2dMethod,
                    shader,
                    out combinedShader
                );
            }

            internal static bool IsCombinedDistanceShader(Shader shader)
            {
                return InvokeBool(IsCustomWaifu2dMethod, shader) &&
                    InvokeBool(IsDistanceVisibilityMethod, shader);
            }

            private static Type FindType(string fullName)
            {
                foreach(Assembly assembly in
                    AppDomain.CurrentDomain.GetAssemblies())
                {
                    Type type = assembly.GetType(fullName, false);
                    if(type != null) return type;
                }
                return null;
            }

            private static MethodInfo FindShaderMethod(
                Type type,
                string methodName
            )
            {
                return type == null
                    ? null
                    : type.GetMethod(
                        methodName,
                        StaticMethodFlags,
                        null,
                        new[] { typeof(Shader) },
                        null
                    );
            }

            private static bool TryInvokeShader(
                MethodInfo method,
                Shader shader,
                out Shader result
            )
            {
                result = null;
                if(method == null || shader == null) return false;
                try
                {
                    result = method.Invoke(null, new object[] { shader }) as
                        Shader;
                    return result != null;
                }
                catch(Exception exception)
                {
                    Debug.LogWarning(
                        "lilToon 距离显示：调用 Lyuma Waifu2d 兼容接口失败：" +
                        exception.GetBaseException().Message
                    );
                    return false;
                }
            }

            private static bool InvokeBool(
                MethodInfo method,
                Shader shader
            )
            {
                if(method == null || shader == null) return false;
                try
                {
                    object result = method.Invoke(
                        null,
                        new object[] { shader }
                    );
                    return result is bool && (bool)result;
                }
                catch(Exception)
                {
                    return false;
                }
            }
        }
    }

    internal static class DistanceVisibilityMaterialMenus
    {
        private const string AddMenu = "CONTEXT/Material/添加距离显示";
        private const string RestoreMenu = "CONTEXT/Material/移除距离显示";
        [MenuItem(AddMenu, false, 2000)]
        private static void AddSelected(MenuCommand command)
        {
            Material material = GetContextMaterial(command);
            if(material != null)
                DistanceVisibilityShaderManager.ConvertMaterials(new[] { material });
        }

        [MenuItem(AddMenu, true)]
        private static bool CanAddSelected(MenuCommand command)
        {
            Material material = GetContextMaterial(command);
            return material != null &&
                DistanceVisibilityShaderManager.IsSupportedOriginal(material.shader);
        }

        [MenuItem(RestoreMenu, false, 2001)]
        private static void RestoreSelected(MenuCommand command)
        {
            Material material = GetContextMaterial(command);
            if(material != null)
                DistanceVisibilityShaderManager.RestoreMaterials(new[] { material });
        }

        [MenuItem(RestoreMenu, true)]
        private static bool CanRestoreSelected(MenuCommand command)
        {
            Material material = GetContextMaterial(command);
            return material != null &&
                DistanceVisibilityShaderManager.IsDistanceShader(material.shader);
        }

        private static Material GetContextMaterial(MenuCommand command)
        {
            Material material = command == null ? null : command.context as Material;
            return material != null ? material : Selection.activeObject as Material;
        }

        [MenuItem("Tools/lilToon 距离显示/重新生成 Shader", false, 2100)]
        private static void RegenerateShaders()
        {
            if(DistanceVisibilityShaderManager.EnsureGeneratedShaders(true))
                Debug.Log("lilToon 距离显示：Shader 已重新生成。");
        }
    }
}
#endif
