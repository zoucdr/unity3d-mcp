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
    /// Specialized audio management tool，Provide audio import、Modify、Copy、Delete etc. operations
    /// Corresponding method name: edit_audio
    /// </summary>
    [ToolName("edit_audio", "Asset management")]
    public class EditAudio : StateMethodBase
    {
        /// <summary>
        /// Create parameter key list supported by current method
        /// </summary>
        protected override MethodKey[] CreateKeys()
        {
            return new[]
            {
                new MethodKey("action", "Operation type：import, modify, duplicate, delete, get_info, search, set_import_settings, convert_format, extract_metadata", false),
                new MethodKey("path", "Audio resource path，UnityStandard format：Assets/Audio/AudioName.wav", false),
                new MethodKey("source_file", "Source file path（Use on import）", true),
                new MethodKey("destination", "Target path（Copy/Use during move）", true),
                new MethodKey("query", "Search mode，Such as*.wav, *.mp3, *.ogg", true),
                new MethodKey("recursive", "Recursively search subfolders", true),
                new MethodKey("force", "Whether to force operation（Overwrite existing files etc.）", true),
                new MethodKey("import_settings", "Import setting", true),
                new MethodKey("target_format", "Target format（Use on conversion）", true),
                // Audio import setting parameter
                new MethodKey("force_to_mono", "Whether force convert to mono", true),
                new MethodKey("load_type", "Load type：DecompressOnLoad, CompressedInMemory, Streaming", true),
                new MethodKey("compression_format", "Compression format：PCM, Vorbis, MP3, ADPCM", true),
                new MethodKey("quality", "Quality（0-1）", true),
                new MethodKey("sample_rate_setting", "Sample rate setting：PreserveSampleRate, OptimizeSampleRate, OverrideSampleRate", true),
                new MethodKey("sample_rate", "Sample rate", true),
                new MethodKey("preload_audio_data", "Whether preload audio data", true),
                new MethodKey("load_in_background", "Whether background loading", true),
                new MethodKey("ambisonic_rendering", "Whether enable surround rendering", true),
                new MethodKey("dsp_buffer_size", "DSPBuffer size：BestPerformance, GoodLatency, BestLatency", true),
                new MethodKey("virtualize_when_silent", "Virtualize when muted", true),
                new MethodKey("spatialize", "Whether spatialized", true),
                new MethodKey("spatialize_post_effects", "Spatialize after post effects", true),
                new MethodKey("user_data", "User data", true),
                new MethodKey("asset_bundle_name", "Asset bundle name", true),
                new MethodKey("asset_bundle_variant", "Asset bundle variant", true)
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
                    .Leaf("import", HandleImportAction)
                    .Leaf("modify", HandleModifyAction)
                    .Leaf("duplicate", HandleDuplicateAction)
                    .Leaf("delete", HandleDeleteAction)
                    .Leaf("get_info", HandleGetInfoAction)
                    .Leaf("search", HandleSearchAction)
                    .Leaf("set_import_settings", HandleSetImportSettingsAction)
                    .Leaf("convert_format", HandleConvertFormatAction)
                    .Leaf("extract_metadata", HandleExtractMetadataAction)
                .Build();
        }

        // --- State tree operation method ---

        /// <summary>
        /// Handle import operation
        /// </summary>
        private object HandleImportAction(JsonClass args)
        {
            string sourceFile = args["source_file"]?.Value;
            string path = args["path"]?.Value;

            if (string.IsNullOrEmpty(sourceFile))
                return Response.Error("'source_file' is required for import.");
            if (string.IsNullOrEmpty(path))
                return Response.Error("'path' is required for import.");

            JsonClass importSettings = args["import_settings"] as JsonClass;
            return ImportAudio(sourceFile, path, importSettings);
        }

        private object ImportAudio(string sourceFile, string path, JsonClass importSettings)
        {
            string fullPath = SanitizeAssetPath(path);
            string directory = Path.GetDirectoryName(fullPath);

            // Ensure directory exists
            if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), directory)))
            {
                Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), directory));
                AssetDatabase.Refresh();
            }

            if (AssetExists(fullPath))
                return Response.Error($"Audio already exists at path: {fullPath}");

            try
            {
                // Check if source file exists
                if (!File.Exists(sourceFile))
                    return Response.Error($"Source file not found: {sourceFile}");

                // Copy file to target path
                string targetFilePath = Path.Combine(Directory.GetCurrentDirectory(), fullPath);
                File.Copy(sourceFile, targetFilePath);

                // Import setting
                if (importSettings != null && importSettings.Count > 0)
                {
                    AssetDatabase.ImportAsset(fullPath);
                    AudioImporter importer = AssetImporter.GetAtPath(fullPath) as AudioImporter;
                    if (importer != null)
                    {
                        ApplyAudioImportSettings(importer, importSettings);
                        importer.SaveAndReimport();
                    }
                }
                else
                {
                    AssetDatabase.ImportAsset(fullPath);
                }

                LogInfo($"[EditAudio] Imported audio from '{sourceFile}' to '{fullPath}'");
                return Response.Success($"Audio imported successfully to '{fullPath}'.", GetAudioData(fullPath));
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to import audio to '{fullPath}': {e.Message}");
            }
        }

        /// <summary>
        /// Handle modify operation
        /// </summary>
        private object HandleModifyAction(JsonClass args)
        {
            string path = args["path"]?.Value;
            JsonClass importSettings = args["import_settings"] as JsonClass;

            if (string.IsNullOrEmpty(path))
                return Response.Error("'path' is required for modify.");
            if (importSettings == null || importSettings.Count == 0)
                return Response.Error("'import_settings' are required for modify.");

            return ModifyAudio(path, importSettings);
        }

        private object ModifyAudio(string path, JsonClass importSettings)
        {
            string fullPath = SanitizeAssetPath(path);
            if (!AssetExists(fullPath))
                return Response.Error($"Audio not found at path: {fullPath}");

            try
            {
                AudioImporter importer = AssetImporter.GetAtPath(fullPath) as AudioImporter;
                if (importer == null)
                    return Response.Error($"Failed to get AudioImporter for '{fullPath}'");

                bool modified = ApplyAudioImportSettings(importer, importSettings);

                if (modified)
                {
                    importer.SaveAndReimport();
                    LogInfo($"[EditAudio] Modified audio import settings at '{fullPath}'");
                    return Response.Success($"Audio '{fullPath}' modified successfully.", GetAudioData(fullPath));
                }
                else
                {
                    return Response.Success($"No applicable settings found to modify for audio '{fullPath}'.", GetAudioData(fullPath));
                }
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to modify audio '{fullPath}': {e.Message}");
            }
        }

        /// <summary>
        /// Handle copy operation
        /// </summary>
        private object HandleDuplicateAction(JsonClass args)
        {
            string path = args["path"]?.Value;
            string destinationPath = args["destination"]?.Value;

            if (string.IsNullOrEmpty(path))
                return Response.Error("'path' is required for duplicate.");

            return DuplicateAudio(path, destinationPath);
        }

        private object DuplicateAudio(string path, string destinationPath)
        {
            string sourcePath = SanitizeAssetPath(path);
            if (!AssetExists(sourcePath))
                return Response.Error($"Source audio not found at path: {sourcePath}");

            string destPath;
            if (string.IsNullOrEmpty(destinationPath))
            {
                destPath = AssetDatabase.GenerateUniqueAssetPath(sourcePath);
            }
            else
            {
                destPath = SanitizeAssetPath(destinationPath);
                if (AssetExists(destPath))
                    return Response.Error($"Audio already exists at destination path: {destPath}");
                EnsureDirectoryExists(Path.GetDirectoryName(destPath));
            }

            try
            {
                bool success = AssetDatabase.CopyAsset(sourcePath, destPath);
                if (success)
                {
                    LogInfo($"[EditAudio] Duplicated audio from '{sourcePath}' to '{destPath}'");
                    return Response.Success($"Audio '{sourcePath}' duplicated to '{destPath}'.", GetAudioData(destPath));
                }
                else
                {
                    return Response.Error($"Failed to duplicate audio from '{sourcePath}' to '{destPath}'.");
                }
            }
            catch (Exception e)
            {
                return Response.Error($"Error duplicating audio '{sourcePath}': {e.Message}");
            }
        }

        /// <summary>
        /// Handle delete operation
        /// </summary>
        private object HandleDeleteAction(JsonClass args)
        {
            string path = args["path"]?.Value;

            if (string.IsNullOrEmpty(path))
                return Response.Error("'path' is required for delete.");

            return DeleteAudio(path);
        }

        private object DeleteAudio(string path)
        {
            string fullPath = SanitizeAssetPath(path);
            if (!AssetExists(fullPath))
                return Response.Error($"Audio not found at path: {fullPath}");

            try
            {
                bool success = AssetDatabase.DeleteAsset(fullPath);
                if (success)
                {
                    LogInfo($"[EditAudio] Deleted audio at '{fullPath}'");
                    return Response.Success($"Audio '{fullPath}' deleted successfully.");
                }
                else
                {
                    return Response.Error($"Failed to delete audio '{fullPath}'. Check logs or if the file is locked.");
                }
            }
            catch (Exception e)
            {
                return Response.Error($"Error deleting audio '{fullPath}': {e.Message}");
            }
        }

        /// <summary>
        /// Handle info retrieval operation
        /// </summary>
        private object HandleGetInfoAction(JsonClass args)
        {
            string path = args["path"]?.Value;

            if (string.IsNullOrEmpty(path))
                return Response.Error("'path' is required for get_info.");

            return GetAudioInfo(path);
        }

        private object GetAudioInfo(string path)
        {
            string fullPath = SanitizeAssetPath(path);
            if (!AssetExists(fullPath))
                return Response.Error($"Audio not found at path: {fullPath}");

            try
            {
                return Response.Success("Audio info retrieved.", GetAudioData(fullPath));
            }
            catch (Exception e)
            {
                return Response.Error($"Error getting info for audio '{fullPath}': {e.Message}");
            }
        }

        /// <summary>
        /// Handle search operation
        /// </summary>
        private object HandleSearchAction(JsonClass args)
        {
            string searchPattern = args["query"]?.Value;
            string pathScope = args["path"]?.Value;

            // Search operation requires at leastqueryOrpathOne of
            if (string.IsNullOrEmpty(searchPattern) && string.IsNullOrEmpty(pathScope))
            {
                return Response.Error("Either 'query' or 'path' parameter is required for search.");
            }

            return SearchAudios(args);
        }

        private object SearchAudios(JsonClass args)
        {
            string searchPattern = args["query"]?.Value;
            string pathScope = args["path"]?.Value;
            bool recursive = true;
            if (args["recursive"] != null)
            {
                if (bool.TryParse(args["recursive"].Value, out bool recursiveValue))
                {
                    recursive = recursiveValue;
                }
            }

            List<string> searchFilters = new List<string>();
            if (!string.IsNullOrEmpty(searchPattern))
                searchFilters.Add(searchPattern);
            searchFilters.Add("t:AudioClip");

            string[] folderScope = null;
            if (!string.IsNullOrEmpty(pathScope))
            {
                folderScope = new string[] { SanitizeAssetPath(pathScope) };
                if (!AssetDatabase.IsValidFolder(folderScope[0]))
                {
                    LogInfo($"Search path '{folderScope[0]}' is not a valid folder. Searching entire project.");
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

                    results.Add(GetAudioData(assetPath));
                }

                LogInfo($"[EditAudio] Found {results.Count} audio(s)");
                return Response.Success($"Found {results.Count} audio(s).", results);
            }
            catch (Exception e)
            {
                return Response.Error($"Error searching audios: {e.Message}");
            }
        }

        /// <summary>
        /// Handle set import settings operation
        /// </summary>
        private object HandleSetImportSettingsAction(JsonClass args)
        {
            string path = args["path"]?.Value;
            JsonClass importSettings = args["import_settings"] as JsonClass;

            if (string.IsNullOrEmpty(path))
                return Response.Error("'path' is required for set_import_settings.");
            if (importSettings == null || importSettings.Count == 0)
                return Response.Error("'import_settings' are required for set_import_settings.");

            return SetAudioImportSettings(path, importSettings);
        }

        private object SetAudioImportSettings(string path, JsonClass importSettings)
        {
            string fullPath = SanitizeAssetPath(path);
            if (!AssetExists(fullPath))
                return Response.Error($"Audio not found at path: {fullPath}");

            try
            {
                AudioImporter importer = AssetImporter.GetAtPath(fullPath) as AudioImporter;
                if (importer == null)
                    return Response.Error($"Failed to get AudioImporter for '{fullPath}'");

                bool modified = ApplyAudioImportSettings(importer, importSettings);

                if (modified)
                {
                    importer.SaveAndReimport();
                    LogInfo($"[EditAudio] Set import settings on audio '{fullPath}'");
                    return Response.Success($"Import settings set on audio '{fullPath}'.", GetAudioData(fullPath));
                }
                else
                {
                    return Response.Success($"No valid import settings found to set on audio '{fullPath}'.", GetAudioData(fullPath));
                }
            }
            catch (Exception e)
            {
                return Response.Error($"Error setting import settings on audio '{fullPath}': {e.Message}");
            }
        }

        /// <summary>
        /// Handle format conversion operation
        /// </summary>
        private object HandleConvertFormatAction(JsonClass args)
        {
            string path = args["path"]?.Value;
            string targetFormat = args["target_format"]?.Value;

            if (string.IsNullOrEmpty(path))
                return Response.Error("'path' is required for convert_format.");
            if (string.IsNullOrEmpty(targetFormat))
                return Response.Error("'target_format' is required for convert_format.");

            // Verify if target format is supported
            string[] supportedFormats = { "pcm", "vorbis", "mp3", "adpcm" };
            if (!supportedFormats.Contains(targetFormat.ToLowerInvariant()))
            {
                return Response.Error($"Unsupported target format: {targetFormat}. Supported formats: {string.Join(", ", supportedFormats)}");
            }

            return ConvertAudioFormat(path, targetFormat);
        }

        private object ConvertAudioFormat(string path, string targetFormat)
        {
            string fullPath = SanitizeAssetPath(path);
            if (!AssetExists(fullPath))
                return Response.Error($"Audio not found at path: {fullPath}");

            try
            {
                AudioImporter importer = AssetImporter.GetAtPath(fullPath) as AudioImporter;
                if (importer == null)
                    return Response.Error($"Failed to get AudioImporter for '{fullPath}'");

                AudioImporterSampleSettings settings = importer.defaultSampleSettings;

                // Set target format
                switch (targetFormat.ToLowerInvariant())
                {
                    case "pcm":
                        settings.loadType = AudioClipLoadType.DecompressOnLoad;
                        settings.compressionFormat = AudioCompressionFormat.PCM;
                        break;
                    case "vorbis":
                        settings.loadType = AudioClipLoadType.CompressedInMemory;
                        settings.compressionFormat = AudioCompressionFormat.Vorbis;
                        break;
                    case "mp3":
                        settings.loadType = AudioClipLoadType.CompressedInMemory;
                        settings.compressionFormat = AudioCompressionFormat.MP3;
                        break;
                    case "adpcm":
                        settings.loadType = AudioClipLoadType.CompressedInMemory;
                        settings.compressionFormat = AudioCompressionFormat.ADPCM;
                        break;
                    default:
                        return Response.Error($"Unsupported target format: {targetFormat}");
                }

                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();

                LogInfo($"[EditAudio] Converted audio '{fullPath}' to format '{targetFormat}'");
                return Response.Success($"Audio '{fullPath}' converted to format '{targetFormat}'.", GetAudioData(fullPath));
            }
            catch (Exception e)
            {
                return Response.Error($"Error converting audio format for '{fullPath}': {e.Message}");
            }
        }

        /// <summary>
        /// Handle extract metadata operation
        /// </summary>
        private object HandleExtractMetadataAction(JsonClass args)
        {
            string path = args["path"]?.Value;

            if (string.IsNullOrEmpty(path))
                return Response.Error("'path' is required for extract_metadata.");

            return ExtractAudioMetadata(path);
        }

        private object ExtractAudioMetadata(string path)
        {
            string fullPath = SanitizeAssetPath(path);
            if (!AssetExists(fullPath))
                return Response.Error($"Audio not found at path: {fullPath}");

            AudioClip audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(fullPath);
            if (audioClip == null)
                return Response.Error($"Failed to load audio clip at path: {fullPath}");

            try
            {
                var metadata = new
                {
                    name = audioClip.name,
                    length = audioClip.length,
                    frequency = audioClip.frequency,
                    channels = audioClip.channels,
                    samples = audioClip.samples,
                    load_type = audioClip.loadType.ToString(),
                    // preload_audio_data = targetAudioClip.preloadAudioData, // In someUnityMay be unavailable in version
                    // load_in_background = targetAudioClip.loadInBackground, // In someUnityMay be unavailable in version
                    // ambisonic_rendering = targetAudioClip.ambisonic, // In someUnityMay be unavailable in version
                    // Note：spatialize And spatializePostEffects Property in someUnityMay be unavailable in version
                    // spatialize = targetAudioClip.spatialize,
                    // spatialize_post_effects = targetAudioClip.spatializePostEffects
                };

                LogInfo($"[EditAudio] Extracted metadata from audio '{fullPath}'");
                return Response.Success($"Audio metadata extracted from '{fullPath}'.", metadata);
            }
            catch (Exception e)
            {
                return Response.Error($"Error extracting audio metadata from '{fullPath}': {e.Message}");
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
        /// Apply audio import settings
        /// </summary>
        private bool ApplyAudioImportSettings(AudioImporter importer, JsonClass settings)
        {
            if (importer == null || settings == null)
                return false;
            bool modified = false;

            AudioImporterSampleSettings sampleSettings = importer.defaultSampleSettings;

            foreach (var setting in settings.Properties())
            {
                string settingName = setting.Key;
                JsonNode settingValue = setting.Value;

                try
                {
                    switch (settingName.ToLowerInvariant())
                    {
                        case "force_to_mono":
                            if (settingValue.type == JsonNodeType.Boolean)
                            {
                                bool forceToMono = settingValue.AsBool;
                                // Note：forceToMono Property in someUnityMay be unavailable in version
                                // Here use commented-out method，Avoid compile error
                                LogInfo($"[ApplyAudioImportSettings] forceToMono setting not supported in current Unity version");
                            }
                            break;
                        case "load_type":
                            if (settingValue.type == JsonNodeType.String)
                            {
                                string loadType = settingValue.Value;
                                AudioClipLoadType clipLoadType = AudioClipLoadType.DecompressOnLoad;

                                switch (loadType.ToLowerInvariant())
                                {
                                    case "compressedinmemory":
                                        clipLoadType = AudioClipLoadType.CompressedInMemory;
                                        break;
                                    case "streaming":
                                        clipLoadType = AudioClipLoadType.Streaming;
                                        break;
                                    case "decompressonload":
                                    default:
                                        clipLoadType = AudioClipLoadType.DecompressOnLoad;
                                        break;
                                }

                                if (sampleSettings.loadType != clipLoadType)
                                {
                                    sampleSettings.loadType = clipLoadType;
                                    modified = true;
                                }
                            }
                            break;
                        case "compression_format":
                            if (settingValue.type == JsonNodeType.String)
                            {
                                string compressionFormat = settingValue.Value;
                                AudioCompressionFormat format = AudioCompressionFormat.PCM;

                                switch (compressionFormat.ToLowerInvariant())
                                {
                                    case "vorbis":
                                        format = AudioCompressionFormat.Vorbis;
                                        break;
                                    case "mp3":
                                        format = AudioCompressionFormat.MP3;
                                        break;
                                    case "adpcm":
                                        format = AudioCompressionFormat.ADPCM;
                                        break;
                                    case "pcm":
                                    default:
                                        format = AudioCompressionFormat.PCM;
                                        break;
                                }

                                if (sampleSettings.compressionFormat != format)
                                {
                                    sampleSettings.compressionFormat = format;
                                    modified = true;
                                }
                            }
                            break;
                        case "quality":
                            if (settingValue.type == JsonNodeType.Float || settingValue.type == JsonNodeType.Integer)
                            {
                                float quality = settingValue.AsFloat;
                                quality = Mathf.Clamp01(quality); // Ensure in0-1Within range
                                if (Math.Abs(sampleSettings.quality - quality) > 0.001f)
                                {
                                    sampleSettings.quality = quality;
                                    modified = true;
                                }
                            }
                            break;
                        case "sample_rate_setting":
                            if (settingValue.GetJSONNodeType() == JsonNodeType.String)
                            {
                                string sampleRateSetting = settingValue.Value;
                                AudioSampleRateSetting rateSetting = AudioSampleRateSetting.PreserveSampleRate;

                                switch (sampleRateSetting.ToLowerInvariant())
                                {
                                    case "optimizesamplerate":
                                        rateSetting = AudioSampleRateSetting.OptimizeSampleRate;
                                        break;
                                    case "overridesamplerate":
                                        rateSetting = AudioSampleRateSetting.OverrideSampleRate;
                                        break;
                                    case "preservesamplerate":
                                    default:
                                        rateSetting = AudioSampleRateSetting.PreserveSampleRate;
                                        break;
                                }

                                if (sampleSettings.sampleRateSetting != rateSetting)
                                {
                                    sampleSettings.sampleRateSetting = rateSetting;
                                    modified = true;
                                }
                            }
                            break;
                        case "sample_rate":
                            if (settingValue.type == JsonNodeType.Integer)
                            {
                                int sampleRate = settingValue.AsInt;
                                if (sampleSettings.sampleRateOverride != (uint)sampleRate)
                                {
                                    sampleSettings.sampleRateOverride = (uint)sampleRate;
                                    modified = true;
                                }
                            }
                            break;
                        case "preload_audio_data":
                            if (settingValue.type == JsonNodeType.Boolean)
                            {
                                bool preloadAudioData = settingValue.AsBool;
                                // Note：preloadAudioData Property in someUnityMay be unavailable in version
                                LogInfo($"[ApplyAudioImportSettings] preloadAudioData setting not supported in current Unity version");
                            }
                            break;
                        case "load_in_background":
                            if (settingValue.type == JsonNodeType.Boolean)
                            {
                                bool loadInBackground = settingValue.AsBool;
                                // Note：loadInBackground Property in someUnityMay be unavailable in version
                                LogInfo($"[ApplyAudioImportSettings] loadInBackground setting not supported in current Unity version");
                            }
                            break;
                        case "ambisonic_rendering":
                            if (settingValue.type == JsonNodeType.Boolean)
                            {
                                bool ambisonicRendering = settingValue.AsBool;
                                // Note：ambisonicRendering Property in someUnityMay be unavailable in version
                                // if (sampleSettings.ambisonicRendering != ambisonicRendering)
                                // {
                                //     sampleSettings.ambisonicRendering = ambisonicRendering;
                                //     modified = true;
                                // }
                                LogInfo($"[ApplyAudioImportSettings] ambisonicRendering setting not supported in current Unity version");
                            }
                            break;
                        case "dsp_buffer_size":
                            if (settingValue.type == JsonNodeType.String)
                            {
                                string dspBufferSize = settingValue.Value;
                                // Note：DSPBufferSize Enum and dspBufferSize Property in someUnityMay be unavailable in version
                                // DSPBufferSize bufferSize = DSPBufferSize.BestPerformance;
                                // 
                                // switch (dspBufferSize.ToLowerInvariant())
                                // {
                                //     case "goodlatency":
                                //         bufferSize = DSPBufferSize.GoodLatency;
                                //         break;
                                //     case "bestlatency":
                                //         bufferSize = DSPBufferSize.BestLatency;
                                //         break;
                                //     case "bestperformance":
                                //     default:
                                //         bufferSize = DSPBufferSize.BestPerformance;
                                //         break;
                                // }

                                LogInfo($"[ApplyAudioImportSettings] DSPBufferSize enum and dspBufferSize setting not supported in current Unity version");
                            }
                            break;
                        case "virtualize_when_silent":
                            if (settingValue.type == JsonNodeType.Boolean)
                            {
                                bool virtualizeWhenSilent = settingValue.AsBool;
                                // Note：virtualizeWhenSilent Property in someUnityMay be unavailable in version
                                // if (sampleSettings.virtualizeWhenSilent != virtualizeWhenSilent)
                                // {
                                //     sampleSettings.virtualizeWhenSilent = virtualizeWhenSilent;
                                //     modified = true;
                                // }
                                LogInfo($"[ApplyAudioImportSettings] virtualizeWhenSilent setting not supported in current Unity version");
                            }
                            break;
                        case "spatialize":
                            if (settingValue.type == JsonNodeType.Boolean)
                            {
                                bool spatialize = settingValue.AsBool;
                                // Note：spatialize Property in someUnityMay be unavailable in version
                                LogInfo($"[ApplyAudioImportSettings] spatialize setting not supported in current Unity version");
                            }
                            break;
                        case "spatialize_post_effects":
                            if (settingValue.type == JsonNodeType.Boolean)
                            {
                                bool spatializePostEffects = settingValue.AsBool;
                                // Note：spatializePostEffects Property in someUnityMay be unavailable in version
                                LogInfo($"[ApplyAudioImportSettings] spatializePostEffects setting not supported in current Unity version");
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    LogInfo($"[ApplyAudioImportSettings] Error setting '{settingName}': {ex.Message}");
                }
            }

            if (modified)
            {
                importer.defaultSampleSettings = sampleSettings;
            }

            return modified;
        }

        /// <summary>
        /// Get audio data
        /// </summary>
        private object GetAudioData(string path)
        {
            if (string.IsNullOrEmpty(path) || !AssetExists(path))
                return null;

            string guid = AssetDatabase.AssetPathToGUID(path);
            AudioClip audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);

            if (audioClip == null)
                return null;

            // Get audio importer info
            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
            object importSettings = null;

            if (importer != null)
            {
                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                importSettings = new
                {
                    // force_to_mono = settings.forceToMono, // In someUnityMay be unavailable in version
                    load_type = settings.loadType.ToString(),
                    compression_format = settings.compressionFormat.ToString(),
                    quality = settings.quality,
                    sample_rate_setting = settings.sampleRateSetting.ToString(),
                    sample_rate = (int)settings.sampleRateOverride,
                    // preload_audio_data = settings.preloadAudioData, // In someUnityMay be unavailable in version
                    // load_in_background = settings.loadInBackground, // In someUnityMay be unavailable in version
                    // ambisonic_rendering = settings.ambisonicRendering, // In someUnityMay be unavailable in version
                    // dsp_buffer_size = settings.dspBufferSize.ToString(), // In someUnityMay be unavailable in version
                    // virtualize_when_silent = settings.virtualizeWhenSilent, // In someUnityMay be unavailable in version
                    // spatialize = settings.spatialize, // In someUnityMay be unavailable in version
                    // spatialize_post_effects = settings.spatializePostEffects // In someUnityMay be unavailable in version
                };
            }

            return new
            {
                path = path,
                guid = guid,
                name = Path.GetFileNameWithoutExtension(path),
                fileName = Path.GetFileName(path),
                file_extension = Path.GetExtension(path),
                length = audioClip.length,
                frequency = audioClip.frequency,
                channels = audioClip.channels,
                samples = audioClip.samples,
                load_type = audioClip.loadType.ToString(),
                // preload_audio_data = audioClip.preloadAudioData, // In someUnityMay be unavailable in version
                // load_in_background = audioClip.loadInBackground, // In someUnityMay be unavailable in version
                // ambisonic_rendering = audioClip.ambisonic, // In someUnityMay be unavailable in version
                // spatialize = audioClip.spatialize, // In someUnityMay be unavailable in version
                // spatialize_post_effects = audioClip.spatializePostEffects, // In someUnityMay be unavailable in version
                import_settings = importSettings,
                lastWriteTimeUtc = File.GetLastWriteTimeUtc(Path.Combine(Directory.GetCurrentDirectory(), path)).ToString("o")
            };
        }
    }
}