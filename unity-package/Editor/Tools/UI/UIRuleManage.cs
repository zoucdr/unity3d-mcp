using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityMcp.Models;
using System.IO;
using UnityEngine.Networking;
using System.Collections;

namespace UnityMcp.Tools
{
    /// <summary>
    /// UIRules management tool，Responsible for managementUICreate solution and modification record
    /// Corresponding method name: ui_rule_manage
    /// </summary>
    [ToolName("ui_rule_manage", "UIManage")]
    public class UIRuleManage : StateMethodBase
    {
        /// <summary>
        /// Create the list of parameter keys supported by the current method
        /// </summary>
        protected override MethodKey[] CreateKeys()
        {
            return new[]
            {
                new MethodKey("action", "Operation type: create_rule(create production plan), get_rule(get production plan), get_prototype_pic(get prototype picture as base64), add_modify(add modification record), record_names(batch record node naming info), get_names(get node naming info), record_sprites(batch record sprite info), get_sprites(get sprite info)", false),
                new MethodKey("name", "UI name, used for finding and recording", false),
                new MethodKey("modify_desc", "Modification description", true),
                new MethodKey("save_path", "Save path, used to create new FigmaUGUIRuleObject", true),
                new MethodKey("properties", "Property data, Json formatted string", true),
                new MethodKey("names_data", "Json object with node_id:{name,originName} pairs {\"node_id1\":{\"name\":\"new_name1\",\"originName\":\"orig_name1\"}} or simple node_id:node_name pairs {\"node_id1\":\"node_name1\"} - Required for record_names", true),
                new MethodKey("sprites_data", "Json object with node_id:fileName pairs {\"node_id1\":\"file_name1\",\"node_id2\":\"file_name2\"} - Required for record_sprites", true),
                new MethodKey("auto_load_sprites", "Automatically load sprites from Assets folder based on fileName (default: true)", true)
            };
        }

        /// <summary>
        /// Create state tree
        /// </summary>
        protected override StateTree CreateStateTree()
        {
            return StateTreeBuilder
                .Create()
                .Key("action")
                    .Leaf("create_rule", CreateUIRule)
                    .Leaf("get_rule", GetUIRule)
                    .Leaf("get_prototype_pic", GetPrototypePic)
                    .Leaf("add_modify", AddModifyRecord)
                    .Leaf("record_names", RecordNodeNames)
                    .Leaf("get_names", GetNodeNames)
                    .Leaf("record_sprites", RecordNodeSprites)
                    .Leaf("get_sprites", GetNodeSprites)
                .Build();
        }

        /// <summary>
        /// CreateUIMake rules
        /// </summary>
        private object CreateUIRule(StateTreeContext ctx)
        {
            string uiName = ctx["name"]?.ToString();
            string savePath = ctx["save_path"]?.ToString();
            string propertiesJson = ctx["properties"]?.ToString();

            if (string.IsNullOrEmpty(uiName))
                return Response.Error("'name' is required for create_rule.");

            if (string.IsNullOrEmpty(savePath))
            {
                // If no save path provided，Use default path
                savePath = "Assets/ScriptableObjects";
            }

            try
            {
                // Make sure save directory exists
                if (!System.IO.Directory.Exists(savePath))
                {
                    System.IO.Directory.CreateDirectory(savePath);
                    AssetDatabase.Refresh();
                }

                // Check if an asset with the same name already exists
                string assetPath = Path.Combine(savePath, $"{uiName}_Rule.asset");
                if (File.Exists(assetPath))
                {
                    return Response.Error($"FigmaUGUIRuleObject already exists at path: {assetPath}");
                }

                // Create new FigmaUGUIRuleObject Instance
                UIDefineRuleObject newRule = ScriptableObject.CreateInstance<UIDefineRuleObject>();

                // Set basic property
                newRule.name = uiName;
                newRule.modify_records = new List<string>();

                // If providedproperties，Try to parseJSONData
                if (!string.IsNullOrEmpty(propertiesJson))
                {
                    try
                    {
                        JsonClass properties = Json.Parse(propertiesJson) as JsonClass;

                        // Set various attributes
                        if (properties["link_url"] != null)
                            newRule.link_url = properties["link_url"].Value;

                        if (properties["picture_url"] != null)
                            newRule.img_save_to = properties["picture_url"].Value;

                        if (properties["prototype_pic"] != null)
                            newRule.prototype_pic = properties["prototype_pic"].Value;

                        if (properties["image_scale"] != null)
                            newRule.image_scale = properties["image_scale"].AsInt;

                        if (properties["descriptions"] != null)
                            newRule.descriptions = properties["descriptions"].Value;
                        // Note：descriptionsAndpreferred_componentsNow fromMcpSettingsGet from
                        // No longer frompropertiesParse these fields in
                    }
                    catch (Exception jsonEx)
                    {
                        LogWarning($"[FigmaMakeUGUI] Failed to parse properties Json: {jsonEx.Message}");
                    }
                }

                // Create asset file
                AssetDatabase.CreateAsset(newRule, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                LogInfo($"[FigmaMakeUGUI] Created new FigmaUGUIRuleObject for UI '{uiName}' at path: {assetPath}");

                return Response.Success($"Successfully created FigmaUGUIRuleObject for UI '{uiName}'.",
                    new
                    {
                        uiName = uiName,
                        assetPath = assetPath,
                        rule = Json.FromObject(BuildUIRule(newRule))
                    });
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return Response.Error($"Failed to create UI rule for '{uiName}': {e.Message}");
            }
        }

        /// <summary>
        /// GetUICreate rules and solutions
        /// </summary>
        private object GetUIRule(StateTreeContext ctx)
        {
            string uiName = ctx["name"]?.ToString();

            if (string.IsNullOrEmpty(uiName))
                return Response.Error("'name' is required for get_rule.");

            try
            {
                // Search the related UIDefineRule
                UIDefineRuleObject figmaObj = FindUIDefineRule(uiName);

                if (figmaObj == null)
                {
                    // Even if the specific one is not foundUIRule，Can also return global build steps and environment settings
                    var mcpSettings = McpSettings.Instance;
                    return Response.Success($"No specific UI rule found for '{uiName}', but global build configuration is available.",
                        new
                        {
                            uiName = uiName,
                            foundObject = false,
                            suggestion = "Create a UIDefineRule asset to define UI creation rules",
                        });
                }

                // Usectx.AsyncReturnHandle async operation
                LogInfo($"[UIRuleManage] Start async fetchUIRule: {uiName}");
                return ctx.AsyncReturn(GetUIRuleCoroutine(figmaObj, uiName));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return Response.Error($"Failed to get UI rule for '{uiName}': {e.Message}");
            }
        }

        /// <summary>
        /// Get prototype image（Base64Format）
        /// </summary>
        private object GetPrototypePic(StateTreeContext ctx)
        {
            string uiName = ctx["name"]?.ToString();

            if (string.IsNullOrEmpty(uiName))
                return Response.Error("'name' is required for get_prototype_pic.");

            try
            {
                // Search the related UIDefineRule
                UIDefineRuleObject figmaObj = FindUIDefineRule(uiName);

                if (figmaObj == null)
                {
                    return Response.Error($"No UIDefineRule found for UI '{uiName}'. Please create one first.");
                }

                // Check whether there isprototype_picPath
                if (string.IsNullOrEmpty(figmaObj.prototype_pic))
                {
                    return Response.Success($"No prototype picture path found for UI '{uiName}'.", new
                    {
                        uiName = uiName,
                        hasPrototypePic = false,
                        prototypePicPath = "",
                        prototypePicBase64 = (string)null
                    });
                }

                // Usectx.AsyncReturnHandle async operation
                LogInfo($"[UIRuleManage] Start async fetch of prototype image: {uiName}");
                return ctx.AsyncReturn(GetPrototypePicCoroutine(figmaObj, uiName));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return Response.Error($"Failed to get prototype picture for '{uiName}': {e.Message}");
            }
        }

        /// <summary>
        /// Coroutine to get prototype image
        /// </summary>
        private IEnumerator GetPrototypePicCoroutine(UIDefineRuleObject figmaObj, string uiName)
        {
            LogInfo($"[UIRuleManage] Start coroutine to get prototype image: {uiName}");

            string prototypePicBase64 = null;

            // Start image loading coroutine
            yield return LoadImageAsBase64(figmaObj.prototype_pic, (base64Result) =>
            {
                prototypePicBase64 = base64Result;
            });

            bool hasPrototypePic = !string.IsNullOrEmpty(prototypePicBase64);

            LogInfo($"[UIRuleManage] Prototype image loaded: {uiName}, Success: {hasPrototypePic}");

            yield return Response.Success($"Retrieved prototype picture for UI '{uiName}'.", new
            {
                uiName = uiName,
                hasPrototypePic = hasPrototypePic,
                prototypePicPath = figmaObj.prototype_pic,
                prototypePicBase64 = prototypePicBase64,
                assetPath = AssetDatabase.GetAssetPath(figmaObj)
            });
        }

        /// <summary>
        /// AddUIModification record
        /// </summary>
        private object AddModifyRecord(JsonClass args)
        {
            string uiName = args["name"]?.Value;
            string modify_desc = args["modify_desc"]?.Value;
            if (string.IsNullOrEmpty(modify_desc)) modify_desc = "UI modification";

            if (string.IsNullOrEmpty(uiName))
                return Response.Error("'name' is required for add_modify.");

            try
            {
                // Find the corresponding UIDefineRule
                UIDefineRuleObject figmaObj = FindUIDefineRule(uiName);

                if (figmaObj == null)
                {
                    return Response.Error($"No UIDefineRule found for UI '{uiName}'. Please create one first.");
                }

                // Ensure modify_records List initialized
                if (figmaObj.modify_records == null)
                {
                    figmaObj.modify_records = new List<string>();
                }

                // Create timestamp record
                string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string recordEntry = $"[{timestamp}] {modify_desc}";

                // Add record
                figmaObj.modify_records.Add(recordEntry);

                // Mark asset as dirty and save
                EditorUtility.SetDirty(figmaObj);
                string assetPath = AssetDatabase.GetAssetPath(figmaObj);
                AssetDatabase.SaveAssets();

                LogInfo($"[FigmaMakeUGUI] Added modify record for UI '{uiName}': {modify_desc}");

                return Response.Success($"Modify record added to UIDefineRule for UI '{uiName}'.",
                    new
                    {
                        uiName = uiName,
                        modify_desc = modify_desc,
                        assetPath = assetPath,
                        timestamp = timestamp
                    });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to add modify record for '{uiName}': {e.Message}");
            }
        }



        /// <summary>
        /// Batch record node naming information
        /// </summary>
        private object RecordNodeNames(JsonClass args)
        {
            string uiName = args["name"]?.Value;
            string namesDataJson = args["names_data"]?.Value;

            if (string.IsNullOrEmpty(uiName))
                return Response.Error("'name' is required for record_names.");

            if (string.IsNullOrEmpty(namesDataJson))
                return Response.Error("'names_data' is required for record_names. Provide Json object: {\"node_id1\":{\"name\":\"new_name1\",\"originName\":\"orig_name1\"}} or simple {\"node_id1\":\"node_name1\"}");

            try
            {
                // Find the corresponding UIDefineRule
                UIDefineRuleObject figmaObj = FindUIDefineRule(uiName);

                if (figmaObj == null)
                {
                    return Response.Error($"No UIDefineRule found for UI '{uiName}'. Please create one first.");
                }

                // Ensure node_names List initialized
                if (figmaObj.node_names == null)
                {
                    figmaObj.node_names = new List<NodeRenameInfo>();
                }

                int addedCount = 0;
                int updatedCount = 0;

                // Handle batch node information - Support two formats
                try
                {
                    JsonClass namesObject = Json.Parse(namesDataJson) as JsonClass;
                    foreach (KeyValuePair<string, JsonNode> kvp in namesObject.AsEnumerable())
                    {
                        string nodeId = kvp.Key;
                        string nodeName = null;
                        string originName = null;

                        // Check the type of value：String（Simple format）Or object（Detailed format）
                        if (kvp.Value is JsonData jsonData && jsonData.GetJSONNodeType() == JsonNodeType.String)
                        {
                            // Simple format：{"node_id": "node_name"}
                            nodeName = jsonData.Value;
                        }
                        else if (kvp.Value is JsonClass jsonClass)
                        {
                            // Detailed format：{"node_id": {"name": "new_name", "originName": "orig_name"}}
                            nodeName = jsonClass["name"]?.Value;
                            originName = jsonClass["originName"]?.Value;
                        }

                        if (!string.IsNullOrEmpty(nodeId) && !string.IsNullOrEmpty(nodeName))
                        {
                            var existingNode = figmaObj.node_names.FirstOrDefault(n => n.id == nodeId);
                            if (existingNode != null)
                            {
                                existingNode.name = nodeName;
                                if (!string.IsNullOrEmpty(originName))
                                {
                                    existingNode.originName = originName;
                                }
                                updatedCount++;
                            }
                            else
                            {
                                figmaObj.node_names.Add(new NodeRenameInfo
                                {
                                    id = nodeId,
                                    name = nodeName,
                                    originName = originName ?? ""
                                });
                                addedCount++;
                            }
                        }
                    }
                }
                catch (Exception jsonEx)
                {
                    return Response.Error($"Failed to parse names_data Json: {jsonEx.Message}");
                }

                if (addedCount == 0 && updatedCount == 0)
                {
                    return Response.Error("No valid node naming data found in names_data object.");
                }

                // Mark asset as dirty and save
                EditorUtility.SetDirty(figmaObj);
                string assetPath = AssetDatabase.GetAssetPath(figmaObj);
                AssetDatabase.SaveAssets();

                LogInfo($"[UIRuleManage] Batch recorded node names for UI '{uiName}': {addedCount} added, {updatedCount} updated");

                return Response.Success($"Batch node names recorded for UI '{uiName}': {addedCount} added, {updatedCount} updated.",
                    new
                    {
                        uiName = uiName,
                        addedCount = addedCount,
                        updatedCount = updatedCount,
                        totalNodes = figmaObj.node_names.Count,
                        assetPath = assetPath
                    });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to record node names for '{uiName}': {e.Message}");
            }
        }

        /// <summary>
        /// Get node naming information
        /// </summary>
        private object GetNodeNames(JsonClass args)
        {
            string uiName = args["name"]?.Value;

            if (string.IsNullOrEmpty(uiName))
                return Response.Error("'name' is required for get_names.");

            try
            {
                // Find the corresponding UIDefineRule
                UIDefineRuleObject figmaObj = FindUIDefineRule(uiName);

                if (figmaObj == null)
                {
                    return Response.Success($"No UIDefineRule found for UI '{uiName}'.",
                        new
                        {
                            uiName = uiName,
                            nodeCount = 0,
                            nodes = new object[0]
                        });
                }

                var nodeNames = figmaObj.node_names ?? new List<NodeRenameInfo>();

                return Response.Success($"Retrieved {nodeNames.Count} node name(s) for UI '{uiName}'.",
                    new
                    {
                        uiName = uiName,
                        nodeCount = nodeNames.Count,
                        assetPath = AssetDatabase.GetAssetPath(figmaObj),
                        nodes = nodeNames.Select(n => new { id = n.id, name = n.name, originName = n.originName }).ToArray()
                    });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to get node names for '{uiName}': {e.Message}");
            }
        }

        /// <summary>
        /// Batch record nodesSpriteInformation
        /// </summary>
        private object RecordNodeSprites(JsonClass args)
        {
            string uiName = args["name"]?.Value;
            string spritesDataJson = args["sprites_data"]?.Value;
            bool autoLoadSprites = args["auto_load_sprites"].AsBoolDefault(true);

            if (string.IsNullOrEmpty(uiName))
                return Response.Error("'name' is required for record_sprites.");

            if (string.IsNullOrEmpty(spritesDataJson))
                return Response.Error("'sprites_data' is required for record_sprites. Provide Json object: {\"node_id1\":\"file_name1\",\"node_id2\":\"file_name2\"}");

            try
            {
                // Find the corresponding UIDefineRule
                UIDefineRuleObject figmaObj = FindUIDefineRule(uiName);

                if (figmaObj == null)
                {
                    return Response.Error($"No UIDefineRule found for UI '{uiName}'. Please create one first.");
                }

                // Ensure node_sprites List initialized
                if (figmaObj.node_sprites == null)
                {
                    figmaObj.node_sprites = new List<NodeSpriteInfo>();
                }

                int addedCount = 0;
                int updatedCount = 0;
                int loadedSpritesCount = 0;

                // Handle batchSpriteInformation - Key-value format
                try
                {
                    JsonClass spritesObject = Json.Parse(spritesDataJson) as JsonClass;
                    foreach (KeyValuePair<string, JsonNode> kvp in spritesObject.AsEnumerable())
                    {
                        string nodeId = kvp.Key;
                        string fileName = kvp.Value?.Value;

                        if (!string.IsNullOrEmpty(nodeId) && !string.IsNullOrEmpty(fileName))
                        {
                            var existingSprite = figmaObj.node_sprites.FirstOrDefault(s => s.id == nodeId);
                            if (existingSprite != null)
                            {
                                existingSprite.fileName = fileName;

                                // Auto loadSprite
                                if (autoLoadSprites)
                                {
                                    var loadedSprite = LoadSpriteFromPath(figmaObj.img_save_to, fileName);
                                    if (loadedSprite != null)
                                    {
                                        existingSprite.sprite = loadedSprite;
                                        loadedSpritesCount++;
                                    }
                                }

                                updatedCount++;
                            }
                            else
                            {
                                var newSpriteInfo = new NodeSpriteInfo { id = nodeId, fileName = fileName };

                                // Auto loadSprite
                                if (autoLoadSprites)
                                {
                                    var loadedSprite = LoadSpriteFromPath(figmaObj.img_save_to, fileName);
                                    if (loadedSprite != null)
                                    {
                                        newSpriteInfo.sprite = loadedSprite;
                                        loadedSpritesCount++;
                                    }
                                }

                                figmaObj.node_sprites.Add(newSpriteInfo);
                                addedCount++;
                            }
                        }
                    }
                }
                catch (Exception jsonEx)
                {
                    return Response.Error($"Failed to parse sprites_data Json: {jsonEx.Message}");
                }

                if (addedCount == 0 && updatedCount == 0)
                {
                    return Response.Error("No valid sprite data found in sprites_data object.");
                }

                // Mark asset as dirty and save
                EditorUtility.SetDirty(figmaObj);
                string assetPath = AssetDatabase.GetAssetPath(figmaObj);
                AssetDatabase.SaveAssets();

                LogInfo($"[UIRuleManage] Batch recorded node sprites for UI '{uiName}': {addedCount} added, {updatedCount} updated, {loadedSpritesCount} sprites loaded");

                return Response.Success($"Batch node sprites recorded for UI '{uiName}': {addedCount} added, {updatedCount} updated" +
                    (autoLoadSprites ? $", {loadedSpritesCount} sprites loaded" : ""),
                    new
                    {
                        uiName = uiName,
                        addedCount = addedCount,
                        updatedCount = updatedCount,
                        loadedSpritesCount = loadedSpritesCount,
                        totalSprites = figmaObj.node_sprites.Count,
                        assetPath = assetPath,
                        autoLoadSprites = autoLoadSprites
                    });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to record node sprites for '{uiName}': {e.Message}");
            }
        }

        /// <summary>
        /// Load from specified pathSprite
        /// </summary>
        private Sprite LoadSpriteFromPath(string imgSaveTo, string fileName)
        {
            if (string.IsNullOrEmpty(imgSaveTo) || string.IsNullOrEmpty(fileName))
                return null;

            // Build the full file path
            string fullPath = System.IO.Path.Combine(imgSaveTo, fileName);

            // Try to loadSprite
            Sprite loadedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(fullPath);
            if (loadedSprite != null)
            {
                return loadedSprite;
            }

            // If direct load fails，Attempt to find file
            string fileNameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(fileName);
            string[] foundAssets = AssetDatabase.FindAssets(fileNameWithoutExt + " t:Sprite");

            if (foundAssets.Length > 0)
            {
                // Prefer files in the specified path
                foreach (string guid in foundAssets)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (assetPath.StartsWith(imgSaveTo))
                    {
                        Sprite foundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                        if (foundSprite != null)
                        {
                            return foundSprite;
                        }
                    }
                }

                // If not found in the specified path，Use the first one found
                string firstAssetPath = AssetDatabase.GUIDToAssetPath(foundAssets[0]);
                Sprite firstSprite = AssetDatabase.LoadAssetAtPath<Sprite>(firstAssetPath);
                if (firstSprite != null)
                {
                    return firstSprite;
                }
            }

            return null;
        }

        /// <summary>
        /// Get nodeSpriteInformation
        /// </summary>
        private object GetNodeSprites(JsonClass args)
        {
            string uiName = args["name"]?.Value;

            if (string.IsNullOrEmpty(uiName))
                return Response.Error("'name' is required for get_sprites.");

            try
            {
                // Find the corresponding UIDefineRule
                UIDefineRuleObject figmaObj = FindUIDefineRule(uiName);

                if (figmaObj == null)
                {
                    return Response.Success($"No UIDefineRule found for UI '{uiName}'.",
                        new
                        {
                            uiName = uiName,
                            spriteCount = 0,
                            sprites = new object[0]
                        });
                }

                var nodeSprites = figmaObj.node_sprites ?? new List<NodeSpriteInfo>();

                return Response.Success($"Retrieved {nodeSprites.Count} sprite(s) for UI '{uiName}'.",
                    new
                    {
                        uiName = uiName,
                        spriteCount = nodeSprites.Count,
                        assetPath = AssetDatabase.GetAssetPath(figmaObj),
                        sprites = nodeSprites.Select(s => new { id = s.id, fileName = s.fileName }).ToArray()
                    });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to get node sprites for '{uiName}': {e.Message}");
            }
        }

        // --- Helper Methods ---

        /// <summary>
        /// Use coroutine method to getUIRule（Does not contain prototype image）
        /// </summary>
        private IEnumerator GetUIRuleCoroutine(UIDefineRuleObject figmaObj, string uiName)
        {
            LogInfo($"[UIRuleManage] Start coroutine fetchUIRule: {uiName}");
            // GetMcpSettingsConfig in
            var mcpSettings = McpSettings.Instance;

            // Readoptimize_rule_pathThe text content of
            string optimizeRuleContent = "";
            string optimizeRuleMessage = "";

            if (!string.IsNullOrEmpty(figmaObj.optimize_rule_path))
            {
                try
                {
                    string fullPath = GetFullRulePath(figmaObj.optimize_rule_path);
                    if (File.Exists(fullPath))
                    {
                        optimizeRuleContent = File.ReadAllText(fullPath, System.Text.Encoding.UTF8);
                        optimizeRuleMessage = "UILayout optimization rules loaded";
                        LogInfo($"[UIRuleManage] Successfully read optimization rule file: {fullPath}");
                    }
                    else
                    {
                        optimizeRuleMessage = "UILayout info needs downloading - File does not exist";
                        LogWarning($"[UIRuleManage] Optimization rule file does not exist: {fullPath}");
                    }
                }
                catch (Exception e)
                {
                    optimizeRuleMessage = $"UILayout info needs downloading - Read failed: {e.Message}";
                    LogError($"[UIRuleManage] Fail to read optimization rule file: {e.Message}");
                }
            }
            else
            {
                optimizeRuleMessage = "UILayout info needs downloading - Optimization rule path not set";
            }

            // BuildUIRule info（Does not containdesignPic）
            var rule = new
            {
                name = figmaObj.name,
                figmaUrl = figmaObj.link_url,
                pictureUrl = figmaObj.img_save_to,
                prototypePic = figmaObj.prototype_pic,
                optimizeRulePath = figmaObj.optimize_rule_path,
                optimizeRuleContent = optimizeRuleContent,
                optimizeRuleMessage = optimizeRuleMessage,
                imageScale = figmaObj.image_scale,
                descriptions = GenerateMarkdownDescription(mcpSettings.uiSettings?.ui_build_steps ?? McpUISettings.GetDefaultBuildSteps(), mcpSettings.uiSettings?.ui_build_enviroments ?? McpUISettings.GetDefaultBuildEnvironments(), figmaObj.descriptions),
                assetPath = AssetDatabase.GetAssetPath(figmaObj),
                rename_count = figmaObj.node_names.Count,
                sprite_count = figmaObj.node_sprites.Count > 0 ? figmaObj.node_sprites.Count : 0
            };

            LogInfo($"[UIRuleManage] UIRules construction complete: {uiName}");

            yield return Response.Success($"Found UI rule for '{uiName}'.", new
            {
                uiName = uiName,
                foundObject = true,
                rule = rule
            });
        }

        /// <summary>
        /// Load image and convert toBase64（Coroutine version）
        /// </summary>
        private IEnumerator LoadImageAsBase64(string imagePath, Action<string> callback)
        {
            LogInfo($"[UIRuleManage] Begin loading image: {imagePath}");

            // Determine if it is a local or network path
            if (IsNetworkPath(imagePath))
            {
                // Network path：UseUnityWebRequestDownload
                yield return LoadNetworkImageAsBase64(imagePath, callback);
            }
            else
            {
                // Local path：Read file directly
                yield return LoadLocalImageAsBase64(imagePath, callback);
            }
        }

        /// <summary>
        /// Load online image and convert toBase64
        /// </summary>
        private IEnumerator LoadNetworkImageAsBase64(string url, Action<string> callback)
        {
            LogInfo($"[UIRuleManage] Load image from network: {url}");

            using (var request = UnityWebRequest.Get(url))
            {
                request.timeout = 30; // 30Seconds timeout
                request.SetRequestHeader("User-Agent", "Unity-MCP-UIRuleManager/1.0");

                var operation = request.SendWebRequest();
                float startTime = Time.realtimeSinceStartup;

                // Wait for download complete
                while (!operation.isDone)
                {
                    // Check timeout
                    if (Time.realtimeSinceStartup - startTime > 30f)
                    {
                        request.Abort();
                        LogError($"[UIRuleManage] Network image download timeout: {url}");
                        callback?.Invoke(null);
                        yield break;
                    }
                    yield return null;
                }

                // Check download result
                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        byte[] imageData = request.downloadHandler.data;
                        string base64String = Convert.ToBase64String(imageData);

                        // According toContent-TypeAdd dataURIPrefix
                        string contentType = request.GetResponseHeader("Content-Type") ?? "image/png";
                        string dataUri = $"data:{contentType};base64,{base64String}";

                        LogInfo($"[UIRuleManage] Network image converted toBase64Success，Size: {imageData.Length} bytes");
                        callback?.Invoke(dataUri);
                    }
                    catch (Exception e)
                    {
                        LogError($"[UIRuleManage] Online imageBase64Conversion failed: {e.Message}");
                        callback?.Invoke(null);
                    }
                }
                else
                {
                    LogError($"[UIRuleManage] Network image download failed: {request.error}");
                    callback?.Invoke(null);
                }
            }
        }

        /// <summary>
        /// Load local image and convert toBase64
        /// </summary>
        private IEnumerator LoadLocalImageAsBase64(string filePath, Action<string> callback)
        {
            LogInfo($"[UIRuleManage] Load image from local: {filePath}");

            // Normalize path
            string fullPath = GetFullImagePath(filePath);

            if (!File.Exists(fullPath))
            {
                LogError($"[UIRuleManage] Local image file does not exist: {fullPath}");
                callback?.Invoke(null);
                yield break;
            }

            // Handle file reading in coroutine，Avoid blocking
            byte[] imageData = null;
            string errorMessage = null;

            // Use coroutines to read large files in frames
            yield return ReadFileInChunks(fullPath, (data, error) =>
            {
                imageData = data;
                errorMessage = error;
            });

            if (!string.IsNullOrEmpty(errorMessage))
            {
                LogError($"[UIRuleManage] Failed to read local image: {errorMessage}");
                callback?.Invoke(null);
                yield break;
            }

            if (imageData == null || imageData.Length == 0)
            {
                LogError($"[UIRuleManage] Local image data is empty: {fullPath}");
                callback?.Invoke(null);
                yield break;
            }

            // Determine by file extensionMIMEType
            string extension = Path.GetExtension(fullPath).ToLower();
            string mimeType = GetMimeTypeFromExtension(extension);

            // Convert toBase64
            string base64String = Convert.ToBase64String(imageData);
            string dataUri = $"data:{mimeType};base64,{base64String}";

            LogInfo($"[UIRuleManage] Local image converted toBase64Success，Size: {imageData.Length} bytes");
            callback?.Invoke(dataUri);
        }

        /// <summary>
        /// Read file in chunks to avoid blocking（Coroutine version）
        /// </summary>
        private IEnumerator ReadFileInChunks(string filePath, Action<byte[], string> callback)
        {
            const int chunkSize = 1024 * 1024; // 1MB chunks
            var chunks = new List<byte[]>();
            string errorMessage = null;

            FileStream fileStream = null;

            // Handle exceptions outside the coroutine，Avoid intry-catchUse inyield return
            bool initSuccess = false;
            long totalSize = 0;

            try
            {
                fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                totalSize = fileStream.Length;
                initSuccess = true;
            }
            catch (Exception e)
            {
                errorMessage = e.Message;
            }

            if (!initSuccess || !string.IsNullOrEmpty(errorMessage))
            {
                fileStream?.Dispose();
                callback?.Invoke(null, errorMessage ?? "Failed to open file");
                yield break;
            }

            // Read file data
            long bytesRead = 0;
            while (bytesRead < totalSize)
            {
                try
                {
                    int currentChunkSize = (int)Math.Min(chunkSize, totalSize - bytesRead);
                    byte[] chunk = new byte[currentChunkSize];

                    int actualRead = fileStream.Read(chunk, 0, currentChunkSize);
                    if (actualRead > 0)
                    {
                        if (actualRead < currentChunkSize)
                        {
                            // Adjust array size
                            Array.Resize(ref chunk, actualRead);
                        }
                        chunks.Add(chunk);
                        bytesRead += actualRead;
                    }
                    else
                    {
                        // No more data to read
                        break;
                    }
                }
                catch (Exception e)
                {
                    errorMessage = e.Message;
                    break;
                }

                // For each chunk readyieldOnce，Avoid blocking
                yield return null;
            }

            // Clear resources
            fileStream?.Close();
            fileStream?.Dispose();

            if (!string.IsNullOrEmpty(errorMessage))
            {
                callback?.Invoke(null, errorMessage);
                yield break;
            }

            // Merge all chunks
            int totalLength = chunks.Sum(c => c.Length);
            byte[] result = new byte[totalLength];
            int offset = 0;

            foreach (var chunk in chunks)
            {
                Array.Copy(chunk, 0, result, offset, chunk.Length);
                offset += chunk.Length;
            }

            callback?.Invoke(result, null);
        }

        /// <summary>
        /// Determine whether it's a network path
        /// </summary>
        private bool IsNetworkPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            return path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                   path.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                   path.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Get complete local path of image
        /// </summary>
        /// <summary>
        /// Get complete local path of image，CompatiblefilePathA full path or relative path
        /// </summary>
        private string GetFullImagePath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return filePath;

            // If already an absolute path，Directly return
            if (Path.IsPathRooted(filePath) && File.Exists(filePath))
            {
                return filePath;
            }

            // If path starts withAssetsStart，Append to project root directory
            if (filePath.StartsWith("Assets"))
            {
                string absPath = Path.Combine(Directory.GetCurrentDirectory(), filePath);
                if (File.Exists(absPath))
                    return absPath;
            }

            // Try to append toAssetsIn the directory
            string assetsPath = Path.Combine(Application.dataPath, filePath);
            if (File.Exists(assetsPath))
                return assetsPath;

            // If none can be found，Return original path（May be wrong path）
            return filePath;
        }

        /// <summary>
        /// Get the complete local path of the optimization rule file，CompatiblefilePathA full path or relative path
        /// </summary>
        private string GetFullRulePath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return filePath;

            // If already an absolute path，Directly return
            if (Path.IsPathRooted(filePath))
            {
                return filePath;
            }

            // If path starts withAssetsStart，Append to project root directory
            if (filePath.StartsWith("Assets"))
            {
                string absPath = Path.Combine(Directory.GetCurrentDirectory(), filePath);
                return absPath;
            }

            // Try to append toAssetsIn the directory
            string assetsPath = Path.Combine(Application.dataPath, filePath);
            return assetsPath;
        }

        /// <summary>
        /// Get by file extensionMIMEType
        /// </summary>
        private string GetMimeTypeFromExtension(string extension)
        {
            switch (extension)
            {
                case ".jpg":
                case ".jpeg":
                    return "image/jpeg";
                case ".png":
                    return "image/png";
                case ".gif":
                    return "image/gif";
                case ".bmp":
                    return "image/bmp";
                case ".webp":
                    return "image/webp";
                case ".svg":
                    return "image/svg+xml";
                default:
                    return "image/png"; // Default isPNG
            }
        }

        /// <summary>
        /// Find relatedUIDefineRule
        /// </summary>
        private UIDefineRuleObject FindUIDefineRule(string uiName)
        {
            // Find all throughout the project UIDefineRule
            string[] guids = AssetDatabase.FindAssets($"t:" + typeof(UIDefineRuleObject).Name);

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                UIDefineRuleObject figmaObj = AssetDatabase.LoadAssetAtPath<UIDefineRuleObject>(assetPath);

                if (figmaObj != null)
                {
                    var objName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
                    if (objName.ToLower() == uiName.ToLower())
                    {
                        return figmaObj;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// BuildUIMake rules
        /// </summary>
        private object BuildUIRule(UIDefineRuleObject figmaObj)
        {
            // GetMcpSettingsConfig in
            var mcpSettings = McpSettings.Instance;

            // Readoptimize_rule_pathThe text content of
            string optimizeRuleContent = "";
            string optimizeRuleMessage = "";

            if (!string.IsNullOrEmpty(figmaObj.optimize_rule_path))
            {
                try
                {
                    string fullPath = GetFullRulePath(figmaObj.optimize_rule_path);
                    if (File.Exists(fullPath))
                    {
                        optimizeRuleContent = File.ReadAllText(fullPath, System.Text.Encoding.UTF8);
                        optimizeRuleMessage = "UILayout optimization rules loaded";
                    }
                    else
                    {
                        optimizeRuleMessage = "UILayout info needs downloading - File does not exist";
                    }
                }
                catch (Exception e)
                {
                    optimizeRuleMessage = $"UILayout info needs downloading - Read failed: {e.Message}";
                }
            }
            else
            {
                optimizeRuleMessage = "UILayout info needs downloading - Optimization rule path not set";
            }

            return new
            {
                name = figmaObj.name,
                figmaUrl = figmaObj.link_url,
                pictureUrl = figmaObj.img_save_to,
                optimizeRulePath = figmaObj.optimize_rule_path,
                optimizeRuleContent = optimizeRuleContent,
                optimizeRuleMessage = optimizeRuleMessage,
                imageScale = figmaObj.image_scale,
                descriptions = figmaObj.descriptions,
                // UseMcpUISettingsProviderConfig in replaces the originaldescriptionsAndpreferred_components
                buildSteps = Json.FromObject(mcpSettings.uiSettings?.ui_build_steps ?? McpUISettings.GetDefaultBuildSteps()),
                buildEnvironments = Json.FromObject(mcpSettings.uiSettings?.ui_build_enviroments ?? McpUISettings.GetDefaultBuildEnvironments()),
                assetPath = AssetDatabase.GetAssetPath(figmaObj)
            };
        }

        /// <summary>
        /// Generate including build steps、Of build environment and extra conditionsMarkdownDescription text
        /// </summary>
        private string GenerateMarkdownDescription(List<string> buildSteps, List<string> buildEnvironments, string additionalConditions)
        {
            var markdown = new System.Text.StringBuilder();

            // Add title
            markdown.AppendLine("# UIBuild rule description");
            markdown.AppendLine();

            // Add build step
            if (buildSteps != null && buildSteps.Count > 0)
            {
                markdown.AppendLine("## 🔨 Build step");
                markdown.AppendLine();
                for (int i = 0; i < buildSteps.Count; i++)
                {
                    markdown.AppendLine($"{i + 1}. {buildSteps[i]}");
                }
                markdown.AppendLine();
            }

            // Add build environment
            if (buildEnvironments != null && buildEnvironments.Count > 0)
            {
                markdown.AppendLine("## 🌐 Build environment");
                markdown.AppendLine();
                foreach (var env in buildEnvironments)
                {
                    markdown.AppendLine($"- {env}");
                }
                markdown.AppendLine();
            }

            // Add additional condition
            if (!string.IsNullOrEmpty(additionalConditions))
            {
                markdown.AppendLine("## 📋 Additional condition");
                markdown.AppendLine();
                markdown.AppendLine(additionalConditions);
                markdown.AppendLine();
            }

            return markdown.ToString().TrimEnd();
        }

    }
}
