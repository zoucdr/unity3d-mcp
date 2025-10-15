using System;
using System.Collections.Generic;
using System.Linq;
// Migrated from Newtonsoft.Json to SimpleJson
using UnityEngine;
using UnityEditor;
using UnityMcp.Models;

namespace UnityMcp.Tools
{
    /// <summary>
    /// Project asset object selector
    /// Specifically handle object search in project assets
    /// Support variousUnityFinding of asset type
    /// </summary>
    /// <typeparam name="T">To searchUnityObject type</typeparam>
    public class ProjectSelector<T> : IObjectSelector where T : UnityEngine.Object
    {
        public MethodKey[] CreateKeys()
        {
            return new MethodKey[]
            {
                new MethodKey("instance_id", "Object InstanceID", true),
                new MethodKey("path", "Object Project path", true)
            };
        }

        public StateTree BuildStateTree()
        {
            return StateTreeBuilder.Create()
                .OptionalLeaf("instance_id", (Func<StateTreeContext, object>)HandleByIdSearch)
                .OptionalLeaf("path", (Func<StateTreeContext, object>)HandleByPathSearch)
                .DefaultLeaf((Func<StateTreeContext, object>)HandleDefaultSearch)
                .Build();
        }

        private object HandleByIdSearch(StateTreeContext context)
        {
            // GetIDParameter
            if (!context.TryGetValue("instance_id", out object idObj) || idObj == null)
            {
                return Response.Error("Parameter 'id' is required.");
            }

            // ParseID
            if (!int.TryParse(idObj.ToString(), out int instanceId))
            {
                return Response.Error($"Invalid ID format: '{idObj}'. ID must be an integer.");
            }

            try
            {
                // UseEditorUtility.InstanceIDToObjectSearch object
                var foundObject = UnityEditor.EditorUtility.InstanceIDToObject(instanceId);

                if (foundObject == null)
                {
                    return Response.Error($"Object with ID '{instanceId}' not found.");
                }

                // Check if object is a project asset
                if (!IsProjectAsset(foundObject))
                {
                    return Response.Error($"Object with ID '{instanceId}' is not a project asset.");
                }

                // Check if object type matches
                if (foundObject is T typedObject)
                {
                    return typedObject;
                }
                else
                {
                    return Response.Error($"Found object with ID '{instanceId}', but type mismatch. Expected type: '{typeof(T).Name}', actual type: '{foundObject.GetType().Name}'.");
                }
            }
            catch (Exception ex)
            {
                return Response.Error($"Error occurred while searching object: {ex.Message}");
            }
        }

        private object HandleByPathSearch(StateTreeContext context)
        {
            // GetpathParameter
            if (!context.TryGetValue("path", out object pathObj) || pathObj == null)
            {
                return Response.Error("Parameter 'path' is required.");
            }

            string path = pathObj.ToString();

            try
            {
                // Only search in project assets
                T asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                {
                    return asset;
                }

                return Response.Error($"Asset of type {typeof(T).Name} not found at path '{path}'.");
            }
            catch (Exception ex)
            {
                return Response.Error($"Error occurred while searching path '{path}': {ex.Message}");
            }
        }

        private object HandleDefaultSearch(StateTreeContext context)
        {
            // Check if at least providedidOrpathOne of the parameters
            bool hasId = context.TryGetValue("instance_id", out object idObj) && idObj != null;
            bool hasPath = context.TryGetValue("path", out object pathObj) && pathObj != null;

            if (!hasId && !hasPath)
            {
                Debug.LogError("Either 'instance_id' or 'path' parameter must be provided.");
                return Response.Error("Either 'instance_id' or 'path' parameter must be provided.");
            }

            // Prefer to useidSearch
            if (hasId)
            {
                return HandleByIdSearch(context);
            }

            // UsepathSearch
            if (hasPath)
            {
                return HandleByPathSearch(context);
            }

            return Response.Error("No matching asset found.");
        }

        /// <summary>
        /// Check if object is a project asset
        /// </summary>
        /// <param name="obj">Object to check</param>
        /// <returns>Return if object is project assettrue，Otherwise returnfalse</returns>
        private bool IsProjectAsset(UnityEngine.Object obj)
        {
            if (obj == null) return false;

            // UseAssetDatabase.ContainsCheck if object is asset
            return UnityEditor.AssetDatabase.Contains(obj);
        }

        /// <summary>
        /// General byIDSearch method，Only search in project assets
        /// </summary>
        /// <param name="instanceId">Object'sInstanceID</param>
        /// <returns>Found object，If not found or type mismatchnull</returns>
        public T FindById(int instanceId)
        {
            try
            {
                var foundObject = UnityEditor.EditorUtility.InstanceIDToObject(instanceId);
                if (foundObject != null && IsProjectAsset(foundObject) && foundObject is T typedObject)
                {
                    return typedObject;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Generic search-by-path method，Only search in project assets
        /// </summary>
        /// <param name="path">Asset path</param>
        /// <returns>Found object，If not found or type mismatchnull</returns>
        public T FindByPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            try
            {
                return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get detailed object information
        /// </summary>
        /// <param name="obj">Object to get information of</param>
        /// <returns>Object information string</returns>
        public string GetObjectInfo(T obj)
        {
            if (obj == null) return "null";

            string type = typeof(T).Name;
            string name = obj.name;
            string instanceId = obj.GetInstanceID().ToString();
            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(obj);

            return $"Type: {type}, Name: {name}, InstanceID: {instanceId}, Asset Path: {assetPath}";
        }

        /// <summary>
        /// Get all project assets for the specified type
        /// </summary>
        /// <returns>All foundTType asset list</returns>
        public List<T> FindAllAssets()
        {
            var assets = new List<T>();
            try
            {
                // UseAssetDatabase.FindAssetsFind allTAsset of type
                string[] guids = UnityEditor.AssetDatabase.FindAssets($"t:{typeof(T).Name}");

                foreach (string guid in guids)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    T asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
                    if (asset != null)
                    {
                        assets.Add(asset);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error occurred while finding all {typeof(T).Name} assets: {ex.Message}");
            }

            return assets;
        }

        /// <summary>
        /// Find asset in specified folder
        /// </summary>
        /// <param name="folderPath">Folder path, e.g. "Assets/Scripts"</param>
        /// <returns>FoundTType asset list</returns>
        public List<T> FindAssetsInFolder(string folderPath)
        {
            var assets = new List<T>();
            if (string.IsNullOrEmpty(folderPath))
            {
                return assets;
            }

            try
            {
                // Find asset in specified folder
                string[] guids = UnityEditor.AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folderPath });

                foreach (string guid in guids)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    T asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
                    if (asset != null)
                    {
                        assets.Add(asset);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error occurred while finding {typeof(T).Name} assets in folder '{folderPath}': {ex.Message}");
            }

            return assets;
        }

    }
}