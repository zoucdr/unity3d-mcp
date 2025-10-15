using System;
// Migrated from Newtonsoft.Json to SimpleJson
using UnityEngine;
using UnityMcp.Models;

namespace UnityMcp.Tools
{
    /// <summary>
    /// Simplified object selector，ByIDOrpathFind and return uniqueTType object
    /// </summary>
    /// <typeparam name="T">To searchUnityObject type，Must inherit fromUnityEngine.Object</typeparam>
    public class ObjectSelector<T> : IObjectSelector where T : UnityEngine.Object
    {
        /// <summary>
        /// Create list of parameter keys supported by current method
        /// </summary>
        /// <returns>IncludeidAndpathParameter'sMethodKeyArray</returns>
        public MethodKey[] CreateKeys()
        {
            return new MethodKey[]
            {
                new MethodKey("instance_id", "Object'sInstanceID", true),
                new MethodKey("path", "Path of the object（AssetsPath orHierarchyPath）", true)
            };
        }

        /// <summary>
        /// Build object search state tree，Support byIDOrpathSearch
        /// </summary>
        /// <returns>Built state tree</returns>
        public StateTree BuildStateTree()
        {
            return StateTreeBuilder.Create()
                .OptionalLeaf("instance_id", (Func<StateTreeContext, object>)HandleByIdSearch)
                .OptionalLeaf("path", (Func<StateTreeContext, object>)HandleByPathSearch)
                .DefaultLeaf((Func<StateTreeContext, object>)HandleDefaultSearch)
                .Build();
        }

        /// <summary>
        /// ByIDSearch processing，Return uniqueTType object
        /// </summary>
        /// <param name="context">State tree context</param>
        /// <returns>Found object or error information</returns>
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

        /// <summary>
        /// Search and handle by path，Return uniqueTType object
        /// </summary>
        /// <param name="context">State tree context</param>
        /// <returns>Found object or error information</returns>
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
                // Try asAssetsPath loading
                T asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                {
                    return asset;
                }

                // IfAssetsPath failed，Try asHierarchyPath search
                T hierarchyObject = FindByHierarchyPath(path);
                if (hierarchyObject != null)
                {
                    return hierarchyObject;
                }

                return Response.Error($"Object of type {typeof(T).Name} not found at path '{path}'.");
            }
            catch (Exception ex)
            {
                return Response.Error($"Error occurred while searching path '{path}': {ex.Message}");
            }
        }

        /// <summary>
        /// Default search handling，Ensure that at least providedidOrpathOne of the parameters
        /// </summary>
        /// <param name="context">State tree context</param>
        /// <returns>Found object or error information</returns>
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

            return Response.Error("No matching object found.");
        }

        /// <summary>
        /// ThroughHierarchyFind object by path
        /// </summary>
        /// <param name="path">HierarchyPath，Such as"Parent/Child/Target"</param>
        /// <returns>Found object，If not found then returnnull</returns>
        private T FindByHierarchyPath(string path)
        {
            // UseGameObject.FindSearchGameObject
            GameObject foundGameObject = GameObject.Find(path);

            if (foundGameObject == null)
            {
                return null;
            }

            // IfTIsGameObjectType，Return directly
            if (foundGameObject is T directMatch)
            {
                return directMatch;
            }

            // IfTIsComponentType，Try to get component
            if (typeof(UnityEngine.Component).IsAssignableFrom(typeof(T)))
            {
                return foundGameObject.GetComponent<T>();
            }

            return null;
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

            return $"Type: {type}, Name: {name}, InstanceID: {instanceId}";
        }

        /// <summary>
        /// General byIDSearch method，Can be called directly
        /// </summary>
        /// <param name="instanceId">Object'sInstanceID</param>
        /// <returns>Found object，If not found or type mismatchnull</returns>
        public T FindById(int instanceId)
        {
            try
            {
                var foundObject = UnityEditor.EditorUtility.InstanceIDToObject(instanceId);
                return foundObject as T;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Generic search-by-path method，Can be called directly
        /// </summary>
        /// <param name="path">Path of the object（AssetsPath orHierarchyPath）</param>
        /// <returns>Found object，If not found or type mismatchnull</returns>
        public T FindByPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            try
            {
                // Try asAssetsPath loading
                T asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                {
                    return asset;
                }

                // Try asHierarchyPath search
                return FindByHierarchyPath(path);
            }
            catch
            {
                return null;
            }
        }
    }
}
