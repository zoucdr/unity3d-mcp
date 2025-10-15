using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
// Migrated from Newtonsoft.Json to SimpleJson
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityMcp.Models;

namespace UnityMcp.Tools
{
    /// <summary>
    /// Scene hierarchy object selector
    /// ByIDOrpathFind and return uniqueTType object，Only inHierarchySearch in
    /// </summary>
    /// <typeparam name="T">To searchUnityObject type</typeparam>
    public class HierarchySelector<T> : IObjectSelector where T : UnityEngine.Object
    {

        public MethodKey[] CreateKeys()
        {
            return new MethodKey[]
            {
                new MethodKey("instance_id", "Object InstanceID", true),
                new MethodKey("path", "Object Hierarchy path", true)
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

                // Check if object is in current sceneHierarchyIn
                if (!IsInCurrentSceneHierarchy(foundObject))
                {
                    return Response.Error($"Object with ID '{instanceId}' is not in the current scene's Hierarchy.");
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
                // Only in the current sceneHierarchySearch in
                T hierarchyObject = GameObjectUtils.FindByHierarchyPath<T>(path);
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
        /// Check if object is in current sceneHierarchyIn
        /// </summary>
        /// <param name="obj">Object to check</param>
        /// <returns>If the object is in the current sceneHierarchyReturn intrue，Otherwise returnfalse</returns>
        private bool IsInCurrentSceneHierarchy(UnityEngine.Object obj)
        {
            if (obj == null) return false;

            // If isGameObject，Check its scene
            if (obj is GameObject gameObject)
            {
                return gameObject.scene == SceneManager.GetActiveScene();
            }

            // If isComponent，Check itsGameObjectScene of
            if (obj is Component component)
            {
                return component.gameObject.scene == SceneManager.GetActiveScene();
            }

            // Other types of objects are not inHierarchyIn
            return false;
        }

        /// <summary>
        /// General byIDSearch method，Only in the current sceneHierarchySearch in
        /// </summary>
        /// <param name="instanceId">Object'sInstanceID</param>
        /// <returns>Found object，If not found or type mismatchnull</returns>
        public T FindById(int instanceId)
        {
            try
            {
                var foundObject = UnityEditor.EditorUtility.InstanceIDToObject(instanceId);
                if (foundObject != null && IsInCurrentSceneHierarchy(foundObject) && foundObject is T typedObject)
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
        /// Generic search-by-path method，Only in the current sceneHierarchySearch in
        /// </summary>
        /// <param name="path">HierarchyPath</param>
        /// <returns>Found object，If not found or type mismatchnull</returns>
        public T FindByPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            try
            {
                return GameObjectUtils.FindByHierarchyPath<T>(path);
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

            return $"Type: {type}, Name: {name}, InstanceID: {instanceId}";
        }

    }
}