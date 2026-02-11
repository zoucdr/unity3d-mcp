using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
// Migrated from Newtonsoft.Json to SimpleJson
using UnityEditor;
using UnityEngine;
using UniMcp.Models;
using UniMcp;

namespace UniMcp.Tools
{
    /// <summary>
    /// Handles CRUD operations for shader files within the Unity project.
    /// 对应方法名: manage_shader
    /// </summary>
    [ToolName("edit_shader", "Resource Management")]
    public class EditShader : StateMethodBase
    {
        public override string Description => L.T("Manage shader assets including create and modify", "管理着色器资源，包括创建和修改");

        /// <summary>
        /// 创建当前方法支持的参数键列表
        /// </summary>
        protected override MethodKey[] CreateKeys()
        {
            return new MethodKey[]
            {
                // 操作类型
                new MethodStr("action", L.T("Operation type", "操作类型"), false)
                    .SetEnumValues("create", "read", "update", "delete"),
                
                // Shader名称
                new MethodStr("name", L.T("Shader file name", "Shader文件名"), false),
                
                // 资产路径
                new MethodStr("path", L.T("Project relative path", "工程相对路径")),
                // Shader代码
                new MethodArr("lines", L.T("Shader code content", "Shader代码内容"))
            };
        }

        protected override StateTree CreateStateTree()
        {
            return StateTreeBuilder
                .Create()
                .Key("action")
                    .Leaf("create", HandleCreateShader)
                    .Leaf("read", HandleReadShader)
                    .Leaf("update", HandleUpdateShader)
                    .Leaf("delete", HandleDeleteShader)
                .Build();
        }

        /// <summary>
        /// 处理创建Shader操作
        /// </summary>
        private object HandleCreateShader(JsonClass args)
        {
            McpLogger.Log("[ManageShader] Creating shader");
            var validationResult = ValidateParameters(args);
            if (!validationResult.IsValid)
            {
                return Response.Error(validationResult.ErrorMessage);
            }

            var pathInfo = GetPathInfo(args);
            string contents = GetShaderContents(args);

            // Check if shader already exists
            if (File.Exists(pathInfo.FullPath))
            {
                return Response.Error(
                    $"Shader already exists at '{pathInfo.RelativePath}'. Use 'update' action to modify."
                );
            }

            // Add validation for shader name conflicts in Unity
            if (Shader.Find(validationResult.Name) != null)
            {
                return Response.Error(
                    $"A shader with name '{validationResult.Name}' already exists in the project. Choose a different name."
                );
            }

            // Generate default content if none provided
            if (string.IsNullOrEmpty(contents))
            {
                contents = GenerateDefaultShaderContent(validationResult.Name);
            }

            return CreateShaderFile(pathInfo, validationResult.Name, contents);
        }

        /// <summary>
        /// 处理读取Shader操作
        /// </summary>
        private object HandleReadShader(JsonClass args)
        {
            McpLogger.Log("[ManageShader] Reading shader");
            var validationResult = ValidateParameters(args);
            if (!validationResult.IsValid)
            {
                return Response.Error(validationResult.ErrorMessage);
            }

            var pathInfo = GetPathInfo(args);

            if (!File.Exists(pathInfo.FullPath))
            {
                return Response.Error($"Shader not found at '{pathInfo.RelativePath}'.");
            }

            try
            {
                string contents = File.ReadAllText(pathInfo.FullPath);

                // Return both normal content and lines array
                var responseData = new
                {
                    path = pathInfo.RelativePath,
                    name = validationResult.Name,
                    contents = contents,
                    lines = contents.Split('\n'),
                    fileSize = contents.Length
                };

                return Response.Success(
                    $"Shader '{Path.GetFileName(pathInfo.RelativePath)}' read successfully.",
                    responseData
                );
            }
            catch (Exception e)
            {
                LogError($"[ManageShader] Failed to read shader: {e.Message}");
                return Response.Error($"Failed to read shader '{pathInfo.RelativePath}': {e.Message}");
            }
        }

        /// <summary>
        /// 处理更新Shader操作
        /// </summary>
        private object HandleUpdateShader(JsonClass args)
        {
            McpLogger.Log("[ManageShader] Updating shader");
            var validationResult = ValidateParameters(args);
            if (!validationResult.IsValid)
            {
                return Response.Error(validationResult.ErrorMessage);
            }

            var pathInfo = GetPathInfo(args);
            string contents = GetShaderContents(args);

            if (!File.Exists(pathInfo.FullPath))
            {
                return Response.Error(
                    $"Shader not found at '{pathInfo.RelativePath}'. Use 'create' action to add a new shader."
                );
            }

            if (string.IsNullOrEmpty(contents))
            {
                return Response.Error("Content is required for the 'update' action.");
            }

            return UpdateShaderFile(pathInfo, validationResult.Name, contents);
        }

        /// <summary>
        /// 处理删除Shader操作
        /// </summary>
        private object HandleDeleteShader(JsonClass args)
        {
            McpLogger.Log("[ManageShader] Deleting shader");
            var validationResult = ValidateParameters(args);
            if (!validationResult.IsValid)
            {
                return Response.Error(validationResult.ErrorMessage);
            }

            var pathInfo = GetPathInfo(args);

            if (!File.Exists(pathInfo.FullPath))
            {
                return Response.Error($"Shader not found at '{pathInfo.RelativePath}'.");
            }

            try
            {
                // Delete the asset through Unity's AssetDatabase first
                bool success = AssetDatabase.DeleteAsset(pathInfo.RelativePath);
                if (!success)
                {
                    return Response.Error($"Failed to delete shader through Unity's AssetDatabase: '{pathInfo.RelativePath}'");
                }

                // If the file still exists (rare case), try direct deletion
                if (File.Exists(pathInfo.FullPath))
                {
                    File.Delete(pathInfo.FullPath);
                }

                return Response.Success($"Shader '{Path.GetFileName(pathInfo.RelativePath)}' deleted successfully.");
            }
            catch (Exception e)
            {
                LogError($"[ManageShader] Failed to delete shader: {e.Message}");
                return Response.Error($"Failed to delete shader '{pathInfo.RelativePath}': {e.Message}");
            }
        }

        // --- Helper Methods ---

        /// <summary>
        /// 验证参数的结果
        /// </summary>
        private class ValidationResult
        {
            public bool IsValid { get; set; }
            public string ErrorMessage { get; set; }
            public string Name { get; set; }
        }

        /// <summary>
        /// 路径信息
        /// </summary>
        private class PathInfo
        {
            public string FullPath { get; set; }
            public string RelativePath { get; set; }
            public string FullPathDir { get; set; }
        }

        /// <summary>
        /// 验证基础参数
        /// </summary>
        private ValidationResult ValidateParameters(JsonClass args)
        {
            string name = args["name"]?.Value;

            if (string.IsNullOrEmpty(name))
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Name parameter is required."
                };
            }

            // Basic name validation (alphanumeric, underscores, cannot start with number)
            if (!Regex.IsMatch(name, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"Invalid shader name: '{name}'. Use only letters, numbers, underscores, and don't start with a number."
                };
            }

            return new ValidationResult
            {
                IsValid = true,
                Name = name
            };
        }

        /// <summary>
        /// 获取路径信息
        /// </summary>
        private PathInfo GetPathInfo(JsonClass args)
        {
            string name = args["name"]?.Value;
            string path = args["path"]?.Value;

            // Ensure path is relative to Assets/, removing any leading "Assets/"
            // Set default directory to "Shaders" if path is not provided
            string relativeDir = path ?? "Shaders"; // Default to "Shaders" if path is null
            if (!string.IsNullOrEmpty(relativeDir))
            {
                relativeDir = relativeDir.Replace('\\', '/').Trim('/');
                if (relativeDir.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                {
                    relativeDir = relativeDir.Substring("Assets/".Length).TrimStart('/');
                }
            }
            // Handle empty string case explicitly after processing
            if (string.IsNullOrEmpty(relativeDir))
            {
                relativeDir = "Shaders"; // Ensure default if path was provided as "" or only "/" or "Assets/"
            }

            // Construct paths
            string shaderFileName = $"{name}.shader";
            string fullPathDir = Path.Combine(Application.dataPath, relativeDir);
            string fullPath = Path.Combine(fullPathDir, shaderFileName);
            string relativePath = Path.Combine("Assets", relativeDir, shaderFileName).Replace('\\', '/');

            return new PathInfo
            {
                FullPath = fullPath,
                RelativePath = relativePath,
                FullPathDir = fullPathDir
            };
        }

        /// <summary>
        /// 获取Shader内容
        /// </summary>
        private string GetShaderContents(JsonClass args)
        {
            // Handle lines parameter (array of strings)
            if (args["lines"] != null)
            {
                try
                {
                    var linesArray = args["lines"] as JsonArray;
                    if (linesArray != null)
                    {
                        return string.Join("\n", linesArray.ToStringList().Select(line => line.ToString()));
                    }
                    else
                    {
                        LogError("[ManageShader] Lines parameter must be an array of strings.");
                        return null;
                    }
                }
                catch (Exception e)
                {
                    LogError($"[ManageShader] Failed to process lines parameter: {e.Message}");
                    return null;
                }
            }

            return null;
        }

        /// <summary>
        /// 创建Shader文件
        /// </summary>
        private object CreateShaderFile(PathInfo pathInfo, string name, string contents)
        {
            try
            {
                // Ensure the target directory exists
                if (!Directory.Exists(pathInfo.FullPathDir))
                {
                    Directory.CreateDirectory(pathInfo.FullPathDir);
                    // Refresh AssetDatabase to recognize new folders
                    AssetDatabase.Refresh();
                }

                File.WriteAllText(pathInfo.FullPath, contents, new System.Text.UTF8Encoding(false));
                AssetDatabase.ImportAsset(pathInfo.RelativePath);
                AssetDatabase.Refresh(); // Ensure Unity recognizes the new shader

                return Response.Success(
                    $"Shader '{name}.shader' created successfully at '{pathInfo.RelativePath}'.",
                    new
                    {
                        path = pathInfo.RelativePath,
                        name = name,
                        fileSize = contents.Length
                    }
                );
            }
            catch (Exception e)
            {
                LogError($"[ManageShader] Failed to create shader: {e.Message}");
                return Response.Error($"Failed to create shader '{pathInfo.RelativePath}': {e.Message}");
            }
        }

        /// <summary>
        /// 更新Shader文件
        /// </summary>
        private object UpdateShaderFile(PathInfo pathInfo, string name, string contents)
        {
            try
            {
                File.WriteAllText(pathInfo.FullPath, contents, new System.Text.UTF8Encoding(false));
                AssetDatabase.ImportAsset(pathInfo.RelativePath);
                AssetDatabase.Refresh();

                return Response.Success(
                    $"Shader '{Path.GetFileName(pathInfo.RelativePath)}' updated successfully.",
                    new
                    {
                        path = pathInfo.RelativePath,
                        name = name,
                        fileSize = contents.Length
                    }
                );
            }
            catch (Exception e)
            {
                LogError($"[ManageShader] Failed to update shader: {e.Message}");
                return Response.Error($"Failed to update shader '{pathInfo.RelativePath}': {e.Message}");
            }
        }



        /// <summary>
        /// Generate default shader content
        /// </summary>
        private static string GenerateDefaultShaderContent(string name)
        {
            return @"Shader """ + name + @"""
{
    Properties
    {
        _MainTex (""Texture"", 2D) = ""white"" {}
    }
    SubShader
    {
        Tags { ""RenderType""=""Opaque"" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include ""UnityCG.cginc""

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                return col;
            }
            ENDCG
        }
    }
}";
        }
    }
}
