using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
// Migrated from Newtonsoft.Json to SimpleJson
using UnityEditor;
using UnityEngine;
using UnityMcp.Models;

namespace UnityMcp.Tools
{
    /// <summary>
    /// Dedicated animation clip management tool，Provide animation clip creation、Modify、Copy、Delete etc. operations
    /// Corresponding method name: manage_anim_clip
    /// </summary>
    [ToolName("edit_anim_clip", "Asset management")]
    public class EditAnimClip : StateMethodBase
    {
        /// <summary>
        /// Create parameter key list supported by current method
        /// </summary>
        protected override MethodKey[] CreateKeys()
        {
            return new[]
            {
                new MethodKey("action", "Operation type：create, modify, duplicate, delete, get_info, search, set_curve, set_events", false),
                new MethodKey("path", "Animation clip resource path，UnityStandard format：Assets/Animations/ClipName.anim", false),
                new MethodKey("source_path", "Source animation clip path（Used on copy）", true),
                new MethodKey("destination", "Target path（Copy/Used on move）", true),
                new MethodKey("query", "Search mode，Such as*.anim", true),
                new MethodKey("recursive", "Whether to search subfolders recursively", true),
                new MethodKey("force", "Whether to force executing operation（Overwrite existing files etc.）", true),
                new MethodKey("length", "Animation length（Second）", true),
                new MethodKey("frame_rate", "Frame rate", true),
                new MethodKey("loop_time", "Whether to loop playback", true),
                new MethodKey("loop_pose", "Whether to loop pose", true),
                new MethodKey("cycle_offset", "Loop offset", true),
                new MethodKey("root_rotation_offset_y", "Root rotationYAxis offset", true),
                new MethodKey("root_height_offset_y", "Root heightYAxis offset", true),
                new MethodKey("root_height_offset_y_active", "Whether to enable root heightYAxis offset", true),
                new MethodKey("lock_root_height_y", "Whether lock root heightYAxis", true),
                new MethodKey("lock_root_rotation_y", "Whether lock root rotationYAxis", true),
                new MethodKey("lock_root_rotation_offset_y", "Whether lock root rotation offsetYAxis", true),
                new MethodKey("keep_original_orientation_y", "Whether to keep original orientationYAxis", true),
                new MethodKey("height_from_ground", "Whether to calculate height from ground", true),
                new MethodKey("mirror", "Whether mirror", true),
                new MethodKey("body_orientation", "Body orientation", true),
                new MethodKey("curves", "Animation curve data", true),
                new MethodKey("events", "Animation event data", true)
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
                    .Leaf("create", CreateAnimClip)
                    .Leaf("modify", ModifyAnimClip)
                    .Leaf("duplicate", DuplicateAnimClip)
                    .Leaf("delete", DeleteAnimClip)
                    .Leaf("get_info", GetAnimClipInfo)
                    .Leaf("search", SearchAnimClips)
                    .Leaf("set_curve", SetAnimClipCurve)
                    .Leaf("set_events", SetAnimClipEvents)
                    .Leaf("set_settings", SetAnimClipSettings)
                    .Leaf("copy_from_model", CopyAnimClipFromModel)
                .Build();
        }

        // --- State tree operation method ---

        private object CreateAnimClip(JsonClass args)
        {
            string path = args["path"]?.Value;
            float length = args["length"].AsFloatDefault(1.0f);
            float frameRate = args["frame_rate"].AsFloatDefault(30.0f);

            if (string.IsNullOrEmpty(path))
                return Response.Error("'path' is required for create.");

            string fullPath = SanitizeAssetPath(path);
            string directory = Path.GetDirectoryName(fullPath);

            // Ensure directory exists
            if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), directory)))
            {
                Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), directory));
                AssetDatabase.Refresh();
            }

            if (AssetExists(fullPath))
                return Response.Error($"Animation clip already exists at path: {fullPath}");

            try
            {
                AnimationClip clip = new AnimationClip();

                // Set basic properties
                clip.frameRate = frameRate;
                // Note：clip.length Is read-only property，Cannot set directly
                // Animation length usually determined by animation data itself

                // Apply settings
                JsonClass settings = args["settings"] as JsonClass;
                if (settings != null)
                {
                    ApplyAnimClipSettings(clip, settings);
                }

                AssetDatabase.CreateAsset(clip, fullPath);
                AssetDatabase.SaveAssets();

                LogInfo($"[ManageAnimClip] Created animation clip at '{fullPath}' with length {length}s and frame rate {frameRate}fps");
                return Response.Success($"Animation clip '{fullPath}' created successfully.", GetAnimClipData(fullPath));
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to create animation clip at '{fullPath}': {e.Message}");
            }
        }

        private object ModifyAnimClip(JsonClass args)
        {
            string path = args["path"]?.Value;
            JsonClass settings = args["settings"] as JsonClass;
            JsonClass curves = args["curves"] as JsonClass;
            JsonArray events = args["events"] as JsonArray;

            if (string.IsNullOrEmpty(path))
                return Response.Error("'path' is required for modify.");

            string fullPath = SanitizeAssetPath(path);
            if (!AssetExists(fullPath))
                return Response.Error($"Animation clip not found at path: {fullPath}");

            try
            {
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(fullPath);
                if (clip == null)
                    return Response.Error($"Failed to load animation clip at path: {fullPath}");

                Undo.RecordObject(clip, $"Modify Animation Clip '{Path.GetFileName(fullPath)}'");

                bool modified = false;

                // Apply settings
                if (settings != null && settings.Count > 0)
                {
                    modified |= ApplyAnimClipSettings(clip, settings);
                }

                // Apply curve
                if (curves != null && curves.Count > 0)
                {
                    modified |= ApplyAnimClipCurves(clip, curves);
                }

                // Apply event
                if (events != null && events.Count > 0)
                {
                    modified |= ApplyAnimClipEvents(clip, events);
                }

                if (modified)
                {
                    EditorUtility.SetDirty(clip);
                    AssetDatabase.SaveAssets();
                    LogInfo($"[ManageAnimClip] Modified animation clip at '{fullPath}'");
                    return Response.Success($"Animation clip '{fullPath}' modified successfully.", GetAnimClipData(fullPath));
                }
                else
                {
                    return Response.Success($"No applicable properties found to modify for animation clip '{fullPath}'.", GetAnimClipData(fullPath));
                }
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to modify animation clip '{fullPath}': {e.Message}");
            }
        }

        private object DuplicateAnimClip(JsonClass args)
        {
            string path = args["path"]?.Value;
            string destinationPath = args["destination"]?.Value;

            if (string.IsNullOrEmpty(path))
                return Response.Error("'path' is required for duplicate.");

            string sourcePath = SanitizeAssetPath(path);
            if (!AssetExists(sourcePath))
                return Response.Error($"Source animation clip not found at path: {sourcePath}");

            string destPath;
            if (string.IsNullOrEmpty(destinationPath))
            {
                destPath = AssetDatabase.GenerateUniqueAssetPath(sourcePath);
            }
            else
            {
                destPath = SanitizeAssetPath(destinationPath);
                if (AssetExists(destPath))
                    return Response.Error($"Animation clip already exists at destination path: {destPath}");
                EnsureDirectoryExists(Path.GetDirectoryName(destPath));
            }

            try
            {
                bool success = AssetDatabase.CopyAsset(sourcePath, destPath);
                if (success)
                {
                    LogInfo($"[ManageAnimClip] Duplicated animation clip from '{sourcePath}' to '{destPath}'");
                    return Response.Success($"Animation clip '{sourcePath}' duplicated to '{destPath}'.", GetAnimClipData(destPath));
                }
                else
                {
                    return Response.Error($"Failed to duplicate animation clip from '{sourcePath}' to '{destPath}'.");
                }
            }
            catch (Exception e)
            {
                return Response.Error($"Error duplicating animation clip '{sourcePath}': {e.Message}");
            }
        }

        private object DeleteAnimClip(JsonClass args)
        {
            string path = args["path"]?.Value;

            if (string.IsNullOrEmpty(path))
                return Response.Error("'path' is required for delete.");

            string fullPath = SanitizeAssetPath(path);
            if (!AssetExists(fullPath))
                return Response.Error($"Animation clip not found at path: {fullPath}");

            try
            {
                bool success = AssetDatabase.DeleteAsset(fullPath);
                if (success)
                {
                    LogInfo($"[ManageAnimClip] Deleted animation clip at '{fullPath}'");
                    return Response.Success($"Animation clip '{fullPath}' deleted successfully.");
                }
                else
                {
                    return Response.Error($"Failed to delete animation clip '{fullPath}'. Check logs or if the file is locked.");
                }
            }
            catch (Exception e)
            {
                return Response.Error($"Error deleting animation clip '{fullPath}': {e.Message}");
            }
        }

        private object GetAnimClipInfo(JsonClass args)
        {
            string path = args["path"]?.Value;

            if (string.IsNullOrEmpty(path))
                return Response.Error("'path' is required for get_info.");

            string fullPath = SanitizeAssetPath(path);
            if (!AssetExists(fullPath))
                return Response.Error($"Animation clip not found at path: {fullPath}");

            try
            {
                return Response.Success("Animation clip info retrieved.", GetAnimClipData(fullPath));
            }
            catch (Exception e)
            {
                return Response.Error($"Error getting info for animation clip '{fullPath}': {e.Message}");
            }
        }

        private object SearchAnimClips(JsonClass args)
        {
            string searchPattern = args["query"]?.Value;
            string pathScope = args["path"]?.Value;
            bool recursive = args["recursive"].AsBoolDefault(true);

            List<string> searchFilters = new List<string>();
            if (!string.IsNullOrEmpty(searchPattern))
                searchFilters.Add(searchPattern);
            searchFilters.Add("t:AnimationClip");

            string[] folderScope = null;
            if (!string.IsNullOrEmpty(pathScope))
            {
                folderScope = new string[] { SanitizeAssetPath(pathScope) };
                if (!AssetDatabase.IsValidFolder(folderScope[0]))
                {
                    LogWarning($"Search path '{folderScope[0]}' is not a valid folder. Searching entire project.");
                    folderScope = null;
                }
            }

            try
            {
                string[] guids = AssetDatabase.FindAssets(string.Join(" ", searchFilters), folderScope);
                List<object> results = new List<object>();

                foreach (string guid in guids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (string.IsNullOrEmpty(assetPath))
                        continue;

                    results.Add(GetAnimClipData(assetPath));
                }

                LogInfo($"[ManageAnimClip] Found {results.Count} animation clip(s)");
                return Response.Success($"Found {results.Count} animation clip(s).", results);
            }
            catch (Exception e)
            {
                return Response.Error($"Error searching animation clips: {e.Message}");
            }
        }

        private object SetAnimClipCurve(JsonClass args)
        {
            string path = args["path"]?.Value;
            JsonClass curves = args["curves"] as JsonClass;

            if (string.IsNullOrEmpty(path))
                return Response.Error("'path' is required for set_curve.");
            if (curves == null || curves.Count == 0)
                return Response.Error("'curves' are required for set_curve.");

            string fullPath = SanitizeAssetPath(path);
            if (!AssetExists(fullPath))
                return Response.Error($"Animation clip not found at path: {fullPath}");

            try
            {
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(fullPath);
                if (clip == null)
                    return Response.Error($"Failed to load animation clip at path: {fullPath}");

                Undo.RecordObject(clip, $"Set Curves on Animation Clip '{Path.GetFileName(fullPath)}'");

                bool modified = ApplyAnimClipCurves(clip, curves);

                if (modified)
                {
                    EditorUtility.SetDirty(clip);
                    AssetDatabase.SaveAssets();
                    LogInfo($"[ManageAnimClip] Set curves on animation clip '{fullPath}'");
                    return Response.Success($"Curves set on animation clip '{fullPath}'.", GetAnimClipData(fullPath));
                }
                else
                {
                    return Response.Success($"No valid curves found to set on animation clip '{fullPath}'.", GetAnimClipData(fullPath));
                }
            }
            catch (Exception e)
            {
                return Response.Error($"Error setting curves on animation clip '{fullPath}': {e.Message}");
            }
        }

        private object SetAnimClipEvents(JsonClass args)
        {
            string path = args["path"]?.Value;
            JsonArray events = args["events"] as JsonArray;

            if (string.IsNullOrEmpty(path))
                return Response.Error("'path' is required for set_events.");
            if (events == null || events.Count == 0)
                return Response.Error("'events' are required for set_events.");

            string fullPath = SanitizeAssetPath(path);
            if (!AssetExists(fullPath))
                return Response.Error($"Animation clip not found at path: {fullPath}");

            try
            {
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(fullPath);
                if (clip == null)
                    return Response.Error($"Failed to load animation clip at path: {fullPath}");

                Undo.RecordObject(clip, $"Set Events on Animation Clip '{Path.GetFileName(fullPath)}'");

                bool modified = ApplyAnimClipEvents(clip, events);

                if (modified)
                {
                    EditorUtility.SetDirty(clip);
                    AssetDatabase.SaveAssets();
                    LogInfo($"[ManageAnimClip] Set events on animation clip '{fullPath}'");
                    return Response.Success($"Events set on animation clip '{fullPath}'.", GetAnimClipData(fullPath));
                }
                else
                {
                    return Response.Success($"No valid events found to set on animation clip '{fullPath}'.", GetAnimClipData(fullPath));
                }
            }
            catch (Exception e)
            {
                return Response.Error($"Error setting events on animation clip '{fullPath}': {e.Message}");
            }
        }

        private object SetAnimClipSettings(JsonClass args)
        {
            string path = args["path"]?.Value;
            JsonClass settings = args["settings"] as JsonClass;

            if (string.IsNullOrEmpty(path))
                return Response.Error("'path' is required for set_settings.");
            if (settings == null || settings.Count == 0)
                return Response.Error("'settings' are required for set_settings.");

            string fullPath = SanitizeAssetPath(path);
            if (!AssetExists(fullPath))
                return Response.Error($"Animation clip not found at path: {fullPath}");

            try
            {
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(fullPath);
                if (clip == null)
                    return Response.Error($"Failed to load animation clip at path: {fullPath}");

                Undo.RecordObject(clip, $"Set Settings on Animation Clip '{Path.GetFileName(fullPath)}'");

                bool modified = ApplyAnimClipSettings(clip, settings);

                if (modified)
                {
                    EditorUtility.SetDirty(clip);
                    AssetDatabase.SaveAssets();
                    LogInfo($"[ManageAnimClip] Set settings on animation clip '{fullPath}'");
                    return Response.Success($"Settings set on animation clip '{fullPath}'.", GetAnimClipData(fullPath));
                }
                else
                {
                    return Response.Success($"No valid settings found to set on animation clip '{fullPath}'.", GetAnimClipData(fullPath));
                }
            }
            catch (Exception e)
            {
                return Response.Error($"Error setting settings on animation clip '{fullPath}': {e.Message}");
            }
        }

        private object CopyAnimClipFromModel(JsonClass args)
        {
            string modelPath = args["model_path"]?.Value;
            string clipName = args["clip_name"]?.Value;
            string destinationPath = args["destination"]?.Value;

            if (string.IsNullOrEmpty(modelPath))
                return Response.Error("'model_path' is required for copy_from_model.");
            if (string.IsNullOrEmpty(clipName))
                return Response.Error("'clip_name' is required for copy_from_model.");

            string modelFullPath = SanitizeAssetPath(modelPath);
            if (!AssetExists(modelFullPath))
                return Response.Error($"Model not found at path: {modelFullPath}");

            try
            {
                ModelImporter modelImporter = AssetImporter.GetAtPath(modelFullPath) as ModelImporter;
                if (modelImporter == null)
                    return Response.Error($"Failed to get ModelImporter for '{modelFullPath}'");

                // Get all animation clips in model
                ModelImporterClipAnimation[] clipAnimations = modelImporter.defaultClipAnimations;
                AnimationClip targetClip = null;

                foreach (var clipAnim in clipAnimations)
                {
                    if (clipAnim.name == clipName)
                    {
                        // Find target animation clip，Need to extract it now
                        // More complex logic needed here to extract specific animation clip from model
                        // Temporarily return error，Prompt user to use other method
                        return Response.Error($"Extracting specific animation clips from models requires more complex implementation. Please use the model import settings or create animation clips manually.");
                    }
                }

                return Response.Error($"Animation clip '{clipName}' not found in model '{modelFullPath}'");
            }
            catch (Exception e)
            {
                return Response.Error($"Error copying animation clip from model '{modelFullPath}': {e.Message}");
            }
        }

        // --- Internal helper method ---

        /// <summary>
        /// Ensure asset path starts with"Assets/"Start
        /// </summary>
        private string SanitizeAssetPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;
            path = path.Replace('\\', '/');
            if (!path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                return "Assets/" + path.TrimStart('/');
            }
            return path;
        }

        /// <summary>
        /// Check if asset exists
        /// </summary>
        private bool AssetExists(string sanitizedPath)
        {
            if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(sanitizedPath)))
            {
                return true;
            }
            if (Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), sanitizedPath)))
            {
                return AssetDatabase.IsValidFolder(sanitizedPath);
            }
            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), sanitizedPath)))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Ensure directory exists
        /// </summary>
        private void EnsureDirectoryExists(string directoryPath)
        {
            if (string.IsNullOrEmpty(directoryPath))
                return;
            string fullDirPath = Path.Combine(Directory.GetCurrentDirectory(), directoryPath);
            if (!Directory.Exists(fullDirPath))
            {
                Directory.CreateDirectory(fullDirPath);
                AssetDatabase.Refresh();
            }
        }

        /// <summary>
        /// Apply animation clip settings
        /// </summary>
        private bool ApplyAnimClipSettings(AnimationClip clip, JsonClass settings)
        {
            if (clip == null || settings == null)
                return false;
            bool modified = false;

            foreach (var setting in settings.Properties())
            {
                string settingName = setting.Key;
                JsonNode settingValue = setting.Value;

                try
                {
                    switch (settingName.ToLowerInvariant())
                    {
                        case "length":
                            if (settingValue.type == JsonNodeType.Float || settingValue.type == JsonNodeType.Integer)
                            {
                                float length = settingValue.AsFloat;
                                // Note：clip.length Is read-only property，Cannot set directly
                                // Animation length usually determined by animation data itself
                                LogWarning($"[ApplyAnimClipSettings] Cannot set length property - it is read-only. Current length: {clip.length}");
                            }
                            break;
                        case "frame_rate":
                            if (settingValue.type == JsonNodeType.Float || settingValue.type == JsonNodeType.Integer)
                            {
                                float frameRate = settingValue.AsFloat;
                                if (Math.Abs(clip.frameRate - frameRate) > 0.001f)
                                {
                                    clip.frameRate = frameRate;
                                    modified = true;
                                }
                            }
                            break;
                        case "loop_time":
                            if (settingValue.type == JsonNodeType.Boolean)
                            {
                                bool loopTime = settingValue.AsBool;
                                // Note：clip.isLooping Is read-only property，Need via AnimationClipSettings Set
                                LogWarning($"[ApplyAnimClipSettings] Cannot set isLooping property directly - it is read-only. Use AnimationClipSettings instead.");
                            }
                            break;
                        case "loop_pose":
                            if (settingValue.type == JsonNodeType.Boolean)
                            {
                                bool loopPose = settingValue.AsBool;
                                // Note：loopPose Setting needs via AnimationClipSettings For setting
                                // Simplified handling here，May actually require more complex logic
                                break;
                            }
                            break;
                        case "cycle_offset":
                            if (settingValue.type == JsonNodeType.Float || settingValue.type == JsonNodeType.Integer)
                            {
                                float cycleOffset = settingValue.AsFloat;
                                // Note：cycleOffset Setting needs via AnimationClipSettings For setting
                                break;
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    LogWarning($"[ApplyAnimClipSettings] Error setting '{settingName}': {ex.Message}");
                }
            }

            return modified;
        }

        /// <summary>
        /// Apply animation clip curve
        /// </summary>
        private bool ApplyAnimClipCurves(AnimationClip clip, JsonClass curves)
        {
            if (clip == null || curves == null)
                return false;
            bool modified = false;

            foreach (var curve in curves.Properties())
            {
                string propertyPath = curve.Key;
                JsonNode curveData = curve.Value;

                try
                {
                    if (curveData is JsonClass curveObj)
                    {
                        // Need to set according to detailed curve data format
                        // Simplified implementation，May actually need more complex curve parsing logic
                        LogWarning($"[ApplyAnimClipCurves] Curve setting for '{propertyPath}' not fully implemented yet.");
                    }
                }
                catch (Exception ex)
                {
                    LogWarning($"[ApplyAnimClipCurves] Error setting curve for '{propertyPath}': {ex.Message}");
                }
            }

            return modified;
        }

        /// <summary>
        /// Apply animation clip events
        /// </summary>
        private bool ApplyAnimClipEvents(AnimationClip clip, JsonArray events)
        {
            if (clip == null || events == null)
                return false;
            bool modified = false;

            try
            {
                // Clear existing events
                AnimationUtility.SetAnimationEvents(clip, new AnimationEvent[0]);

                List<AnimationEvent> animationEvents = new List<AnimationEvent>();

                foreach (var eventData in events)
                {
                    if (eventData is JsonClass eventObj)
                    {
                        AnimationEvent animEvent = new AnimationEvent();

                        if (eventObj["time"] != null)
                            animEvent.time = eventObj["time"].AsFloat;

                        if (eventObj["function_name"] != null)
                            animEvent.functionName = eventObj["function_name"].Value;

                        if (eventObj["string_parameter"] != null)
                            animEvent.stringParameter = eventObj["string_parameter"].Value;

                        if (eventObj["float_parameter"] != null)
                            animEvent.floatParameter = eventObj["float_parameter"].AsFloat;

                        if (eventObj["int_parameter"] != null)
                            animEvent.intParameter = eventObj["int_parameter"].AsInt;

                        if (eventObj["object_reference_parameter"] != null)
                        {
                            string objPath = eventObj["object_reference_parameter"].Value;
                            animEvent.objectReferenceParameter = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(SanitizeAssetPath(objPath));
                        }

                        animationEvents.Add(animEvent);
                    }
                }

                if (animationEvents.Count > 0)
                {
                    AnimationUtility.SetAnimationEvents(clip, animationEvents.ToArray());
                    modified = true;
                }
            }
            catch (Exception ex)
            {
                LogWarning($"[ApplyAnimClipEvents] Error setting events: {ex.Message}");
            }

            return modified;
        }

        /// <summary>
        /// Get animation clip data
        /// </summary>
        private object GetAnimClipData(string path)
        {
            if (string.IsNullOrEmpty(path) || !AssetExists(path))
                return null;

            string guid = AssetDatabase.AssetPathToGUID(path);
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);

            if (clip == null)
                return null;

            // Get animation events
            AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip);
            List<object> eventList = events.Select(e => new
            {
                time = e.time,
                function_name = e.functionName,
                string_parameter = e.stringParameter,
                float_parameter = e.floatParameter,
                int_parameter = e.intParameter,
                object_reference_parameter = e.objectReferenceParameter != null ? AssetDatabase.GetAssetPath(e.objectReferenceParameter) : null
            }).ToList<object>();

            return new
            {
                path = path,
                guid = guid,
                name = Path.GetFileNameWithoutExtension(path),
                fileName = Path.GetFileName(path),
                length = clip.length,
                frame_rate = clip.frameRate,
                is_looping = clip.isLooping,
                events = eventList,
                lastWriteTimeUtc = File.GetLastWriteTimeUtc(Path.Combine(Directory.GetCurrentDirectory(), path)).ToString("o")
            };
        }
    }
}