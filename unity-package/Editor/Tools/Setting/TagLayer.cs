using System;
using System.Collections.Generic;
using System.Linq;
// Migrated from Newtonsoft.Json to SimpleJson
using UnityEditor;
using UnityEditorInternal; // Required for tag management
using UnityEngine;
using UniMcp.Models; // For Response class
using UniMcp;
namespace UniMcp.Tools
{
    /// <summary>
    /// Handles Unity Tag and Layer management.
    /// 对应方法名: tag_layer
    /// </summary>
    [ToolName("tag_layer", "System Management")]
    public class TagLayer : StateMethodBase
    {
        public override string Description => L.T("Manage Unity tags and layers", "管理Unity标签和层级");

        /// <summary>
        /// Create the list of parameter keys supported by this method
        /// </summary>
        protected override MethodKey[] CreateKeys()
        {
            return new MethodKey[]
            {
                // Action type
                new MethodStr("action", L.T("Action type", "操作类型"), false)
                    .SetEnumValues("add_tag", "remove_tag", "get_tags", "add_layer", "remove_layer", "get_layers")
                    .SetDefault("get_tags")
                    .AddExamples("add_tag", "get_layers"),
                
                // Tag name
                new MethodStr("tag_name", L.T("Tag name", "标签名称"))
                    .AddExamples("Player", "Enemy"),
                
                // Layer name
                new MethodStr("layer_name", L.T("Layer name", "层名称"))
                    .AddExamples("UI", "Ground")
            };
        }

        // Constant for starting user layer index
        private const int FirstUserLayerIndex = 8;

        // Constant for total layer count
        private const int TotalLayerCount = 32;

        protected override StateTree CreateStateTree()
        {
            return StateTreeBuilder
                .Create()
                .Key("action")
                    // Tag Management
                    .Leaf("add_tag", HandleAddTagAction)
                    .Leaf("remove_tag", HandleRemoveTagAction)
                    .Leaf("get_tags", HandleGetTagsAction)

                    // Layer Management
                    .Leaf("add_layer", HandleAddLayerAction)
                    .Leaf("remove_layer", HandleRemoveLayerAction)
                    .Leaf("get_layers", HandleGetLayersAction)
                .Build();
        }

        // --- State Tree Action Handlers ---

        /// <summary>
        /// 处理添加标签的操作
        /// </summary>
        private object HandleAddTagAction(JsonClass args)
        {
            string tagName = args["tagName"]?.Value;
            if (string.IsNullOrEmpty(tagName))
            {
                return Response.Error("'tagName' parameter required for add_tag.");
            }

            McpLogger.Log($"[TagLayer] Adding tag: {tagName}");
            return AddTag(tagName);
        }

        /// <summary>
        /// 处理移除标签的操作
        /// </summary>
        private object HandleRemoveTagAction(JsonClass args)
        {
            string tagName = args["tagName"]?.Value;
            if (string.IsNullOrEmpty(tagName))
            {
                return Response.Error("'tagName' parameter required for remove_tag.");
            }

            McpLogger.Log($"[TagLayer] Removing tag: {tagName}");
            return RemoveTag(tagName);
        }

        /// <summary>
        /// 处理获取标签列表的操作
        /// </summary>
        private object HandleGetTagsAction(JsonClass args)
        {
            McpLogger.Log("[TagLayer] Getting tags");
            return GetTags();
        }

        /// <summary>
        /// 处理添加层的操作
        /// </summary>
        private object HandleAddLayerAction(JsonClass args)
        {
            string layerName = args["layerName"]?.Value;
            if (string.IsNullOrEmpty(layerName))
            {
                return Response.Error("'layerName' parameter required for add_layer.");
            }

            McpLogger.Log($"[TagLayer] Adding layer: {layerName}");
            return AddLayer(layerName);
        }

        /// <summary>
        /// 处理移除层的操作
        /// </summary>
        private object HandleRemoveLayerAction(JsonClass args)
        {
            string layerName = args["layerName"]?.Value;
            if (string.IsNullOrEmpty(layerName))
            {
                return Response.Error("'layerName' parameter required for remove_layer.");
            }

            McpLogger.Log($"[TagLayer] Removing layer: {layerName}");
            return RemoveLayer(layerName);
        }

        /// <summary>
        /// 处理获取层列表的操作
        /// </summary>
        private object HandleGetLayersAction(JsonClass args)
        {
            McpLogger.Log("[TagLayer] Getting layers");
            return GetLayers();
        }

        // --- Tag Management Methods ---

        private object AddTag(string tagName)
        {
            if (string.IsNullOrWhiteSpace(tagName))
                return Response.Error("Tag name cannot be empty or whitespace.");

            // Check if tag already exists
            if (InternalEditorUtility.tags.Contains(tagName))
            {
                return Response.Error($"Tag '{tagName}' already exists.");
            }

            try
            {
                // Add the tag using the internal utility
                InternalEditorUtility.AddTag(tagName);
                // Force save assets to ensure the change persists in the TagManager asset
                AssetDatabase.SaveAssets();
                return Response.Success($"Tag '{tagName}' added successfully.");
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to add tag '{tagName}': {e.Message}");
            }
        }

        private object RemoveTag(string tagName)
        {
            if (string.IsNullOrWhiteSpace(tagName))
                return Response.Error("Tag name cannot be empty or whitespace.");
            if (tagName.Equals("Untagged", StringComparison.OrdinalIgnoreCase))
                return Response.Error("Cannot remove the built-in 'Untagged' tag.");

            // Check if tag exists before attempting removal
            if (!InternalEditorUtility.tags.Contains(tagName))
            {
                return Response.Error($"Tag '{tagName}' does not exist.");
            }

            try
            {
                // Remove the tag using the internal utility
                InternalEditorUtility.RemoveTag(tagName);
                // Force save assets
                AssetDatabase.SaveAssets();
                return Response.Success($"Tag '{tagName}' removed successfully.");
            }
            catch (Exception e)
            {
                // Catch potential issues if the tag is somehow in use or removal fails
                return Response.Error($"Failed to remove tag '{tagName}': {e.Message}");
            }
        }

        private object GetTags()
        {
            try
            {
                string[] tags = InternalEditorUtility.tags;
                return Response.Success("Retrieved current tags.", tags);
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to retrieve tags: {e.Message}");
            }
        }

        // --- Layer Management Methods ---

        private object AddLayer(string layerName)
        {
            if (string.IsNullOrWhiteSpace(layerName))
                return Response.Error("Layer name cannot be empty or whitespace.");

            // Access the TagManager asset
            SerializedObject tagManager = GetTagManager();
            if (tagManager == null)
                return Response.Error("Could not access TagManager asset.");

            SerializedProperty layersProp = tagManager.FindProperty("layers");
            if (layersProp == null || !layersProp.isArray)
                return Response.Error("Could not find 'layers' property in TagManager.");

            // Check if layer name already exists (case-insensitive check recommended)
            for (int i = 0; i < TotalLayerCount; i++)
            {
                SerializedProperty layerSP = layersProp.GetArrayElementAtIndex(i);
                if (
                    layerSP != null
                    && layerName.Equals(layerSP.stringValue, StringComparison.OrdinalIgnoreCase)
                )
                {
                    return Response.Error($"Layer '{layerName}' already exists at index {i}.");
                }
            }

            // Find the first empty user layer slot (indices 8 to 31)
            int firstEmptyUserLayer = -1;
            for (int i = FirstUserLayerIndex; i < TotalLayerCount; i++)
            {
                SerializedProperty layerSP = layersProp.GetArrayElementAtIndex(i);
                if (layerSP != null && string.IsNullOrEmpty(layerSP.stringValue))
                {
                    firstEmptyUserLayer = i;
                    break;
                }
            }

            if (firstEmptyUserLayer == -1)
            {
                return Response.Error("No empty User Layer slots available (8-31 are full).");
            }

            // Assign the name to the found slot
            try
            {
                SerializedProperty targetLayerSP = layersProp.GetArrayElementAtIndex(
                    firstEmptyUserLayer
                );
                targetLayerSP.stringValue = layerName;
                // Apply the changes to the TagManager asset
                tagManager.ApplyModifiedProperties();
                // Save assets to make sure it's written to disk
                AssetDatabase.SaveAssets();
                return Response.Success(
                    $"Layer '{layerName}' added successfully to slot {firstEmptyUserLayer}."
                );
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to add layer '{layerName}': {e.Message}");
            }
        }

        private object RemoveLayer(string layerName)
        {
            if (string.IsNullOrWhiteSpace(layerName))
                return Response.Error("Layer name cannot be empty or whitespace.");

            // Access the TagManager asset
            SerializedObject tagManager = GetTagManager();
            if (tagManager == null)
                return Response.Error("Could not access TagManager asset.");

            SerializedProperty layersProp = tagManager.FindProperty("layers");
            if (layersProp == null || !layersProp.isArray)
                return Response.Error("Could not find 'layers' property in TagManager.");

            // Find the layer by name (must be user layer)
            int layerIndexToRemove = -1;
            for (int i = FirstUserLayerIndex; i < TotalLayerCount; i++) // Start from user layers
            {
                SerializedProperty layerSP = layersProp.GetArrayElementAtIndex(i);
                // Case-insensitive comparison is safer
                if (
                    layerSP != null
                    && layerName.Equals(layerSP.stringValue, StringComparison.OrdinalIgnoreCase)
                )
                {
                    layerIndexToRemove = i;
                    break;
                }
            }

            if (layerIndexToRemove == -1)
            {
                return Response.Error($"User layer '{layerName}' not found.");
            }

            // Clear the name for that index
            try
            {
                SerializedProperty targetLayerSP = layersProp.GetArrayElementAtIndex(
                    layerIndexToRemove
                );
                targetLayerSP.stringValue = string.Empty; // Set to empty string to remove
                // Apply the changes
                tagManager.ApplyModifiedProperties();
                // Save assets
                AssetDatabase.SaveAssets();
                return Response.Success(
                    $"Layer '{layerName}' (slot {layerIndexToRemove}) removed successfully."
                );
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to remove layer '{layerName}': {e.Message}");
            }
        }

        private object GetLayers()
        {
            try
            {
                var layers = new Dictionary<int, string>();
                for (int i = 0; i < TotalLayerCount; i++)
                {
                    string layerName = LayerMask.LayerToName(i);
                    if (!string.IsNullOrEmpty(layerName)) // Only include layers that have names
                    {
                        layers.Add(i, layerName);
                    }
                }
                return Response.Success("Retrieved current named layers.", layers);
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to retrieve layers: {e.Message}");
            }
        }

        // --- Helper Methods ---

        /// <summary>
        /// Gets the SerializedObject for the TagManager asset.
        /// </summary>
        private SerializedObject GetTagManager()
        {
            try
            {
                // Load the TagManager asset from the ProjectSettings folder
                UnityEngine.Object[] tagManagerAssets = AssetDatabase.LoadAllAssetsAtPath(
                    "ProjectSettings/TagManager.asset"
                );
                if (tagManagerAssets == null || tagManagerAssets.Length == 0)
                {
                    McpLogger.LogError("[TagLayer] TagManager.asset not found in ProjectSettings.");
                    return null;
                }
                // The first object in the asset file should be the TagManager
                return new SerializedObject(tagManagerAssets[0]);
            }
            catch (Exception e)
            {
                McpLogger.LogError($"[TagLayer] Error accessing TagManager.asset: {e.Message}");
                return null;
            }
        }
    }
}

