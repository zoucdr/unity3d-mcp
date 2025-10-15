using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
// Migrated from Newtonsoft.Json to SimpleJson
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityMcp.Models; // For Response class

namespace UnityMcp.Tools
{
    /// <summary>
    /// Handles GameObject search and find operations in the scene hierarchy.
    /// Corresponding method name: hierarchy_search
    /// </summary>
    [ToolName("hierarchy_search", "Hierarchy management")]
    public class HierarchySearch : StateMethodBase
    {
        /// <summary>
        /// Create parameter key list supported by current method
        /// </summary>
        protected override MethodKey[] CreateKeys()
        {
            return new[]
            {
                new MethodKey("search_type", "Search method: by_name, by_id, by_tag, by_layer, by_component, by_query, etc.", false),
                new MethodKey("query", "Search criteria can be ID, name or path (supports wildcard *)", false),
                new MethodKey("select_many", "Whether to find all matching items", true),
                new MethodKey("include_hierarchy", "Whether to include complete hierarchy data for all children", true),
                new MethodKey("include_inactive", "Whether to search inactive objects", true),
                new MethodKey("use_regex", "Whether to use regular expressions", true)
            };
        }

        protected override StateTree CreateStateTree()
        {
            return StateTreeBuilder
                .Create()
                .Key("search_type")
                    .Leaf("by_name", HandleSearchByName)
                    .Leaf("by_id", HandleSearchById)
                    .Leaf("by_tag", HandleSearchByTag)
                    .Leaf("by_layer", HandleSearchByLayer)
                    .Leaf("by_component", HandleSearchByComponent)
                    .Leaf("by_query", HandleSearchByquery)
                .DefaultLeaf(HandleDefaultSearch) // Add default handler
                .Build();
        }

        /// <summary>
        /// Default search handler - When not specifiedsearch_typeUse name-based search for
        /// </summary>
        private object HandleDefaultSearch(JsonClass args)
        {
            string query = args["query"]?.Value;

            // If hasqueryParameter，Default to name-based search
            if (!string.IsNullOrEmpty(query))
            {
                return HandleSearchByName(args);
            }

            return Response.Error("Either 'search_type' must be specified or 'query' must be provided for default name search.");
        }

        // --- State Tree Action Handlers ---

        /// <summary>
        /// Name-based searchGameObject
        /// </summary>
        private object HandleSearchByName(JsonClass args)
        {
            string query = args["query"]?.Value;
            if (string.IsNullOrEmpty(query))
            {
                return Response.Error("query is required for by_name search.");
            }

            bool findAll = args["select_many"].AsBoolDefault(false);
            bool includeHierarchy = args["include_hierarchy"].AsBoolDefault(false);
            bool searchInInactive = args["include_inactive"].AsBoolDefault(false);

            List<GameObject> foundObjects = new List<GameObject>();

            // Exact name search - UseUnityBuiltinAPI
            GameObject exactMatch = GameObject.Find(query);
            if (exactMatch != null && (searchInInactive || exactMatch.activeInHierarchy))
            {
                foundObjects.Add(exactMatch);
            }

            if (findAll || foundObjects.Count == 0)
            {
                // Search all from current sceneGameObject
                GameObject[] allObjects = GetAllGameObjectsInActiveScene(searchInInactive);

                // Check if wildcard present
                bool hasWildcards = query.Contains('*');
                Regex regex = null;

                if (hasWildcards)
                {
                    // Convert wildcard to regex
                    string regexPattern = ConvertWildcardToRegex(query);
                    try
                    {
                        regex = new Regex(regexPattern, RegexOptions.IgnoreCase);
                    }
                    catch (ArgumentException)
                    {
                        // If regex invalid，Fallback to normal search
                        hasWildcards = false;
                    }
                }

                foreach (GameObject go in allObjects)
                {
                    bool nameMatches;

                    if (hasWildcards && regex != null)
                    {
                        nameMatches = regex.IsMatch(go.name);
                    }
                    else
                    {
                        nameMatches = go.name.Contains(query, StringComparison.OrdinalIgnoreCase);
                    }

                    if (nameMatches)
                    {
                        if (foundObjects.Contains(go))
                            continue;
                        foundObjects.Add(go);
                    }
                }
            }

            return CreateHierarchySearchResult(foundObjects, "name", includeHierarchy);
        }

        /// <summary>
        /// ByIDSearchGameObject
        /// </summary>
        private object HandleSearchById(JsonClass args)
        {
            string query = args["query"]?.Value;
            bool searchInInactive = args["include_inactive"].AsBoolDefault(false);

            if (string.IsNullOrEmpty(query))
            {
                return Response.Error("query is required for by_id search.");
            }

            List<GameObject> foundObjects = new List<GameObject>();

            // Attempt parseIDAnd lookup
            if (int.TryParse(query, out int instanceId))
            {
                GameObject found = EditorUtility.InstanceIDToObject(instanceId) as GameObject;
                if (found != null && (searchInInactive || found.activeInHierarchy))
                {
                    foundObjects.Add(found);
                }
            }

            return CreateSearchResult(foundObjects, "ID");
        }

        /// <summary>
        /// Tag-based searchGameObject
        /// </summary>
        private object HandleSearchByTag(JsonClass args)
        {
            string searchTerm = args["query"]?.Value;
            bool findAll = args["select_many"].AsBoolDefault(false);
            bool searchInInactive = args["include_inactive"].AsBoolDefault(false);

            if (string.IsNullOrEmpty(searchTerm))
            {
                return Response.Error("Search term is required for by_tag search.");
            }

            List<GameObject> foundObjects = new List<GameObject>();

            // UseUnityBuiltinFindGameObjectsWithTagMethod
            GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(searchTerm);
            foundObjects.AddRange(taggedObjects);

            if (searchInInactive)
            {
                // Search inactive objects - Get from current scene
                GameObject[] allObjects = GetAllGameObjectsInActiveScene(true);
                foreach (GameObject go in allObjects)
                {
                    if (!go.activeInHierarchy && go.CompareTag(searchTerm))
                    {
                        foundObjects.Add(go);
                    }
                }
            }

            return CreateSearchResult(foundObjects, "tag");
        }

        /// <summary>
        /// Hierarchy-based searchGameObject
        /// </summary>
        private object HandleSearchByLayer(JsonClass args)
        {
            string searchTerm = args["query"]?.Value;
            bool findAll = args["select_many"].AsBoolDefault(false);
            bool searchInInactive = args["include_inactive"].AsBoolDefault(false);

            if (string.IsNullOrEmpty(searchTerm))
            {
                return Response.Error("Search term is required for by_layer search.");
            }

            List<GameObject> foundObjects = new List<GameObject>();

            // Get hierarchy index
            int layerIndex = LayerMask.NameToLayer(searchTerm);
            if (layerIndex == -1)
            {
                return Response.Error($"Layer '{searchTerm}' not found.");
            }

            // Search current sceneGameObject
            GameObject[] allObjects = GetAllGameObjectsInActiveScene(searchInInactive);

            foreach (GameObject go in allObjects)
            {
                if (go.layer == layerIndex)
                {
                    foundObjects.Add(go);
                }
            }

            return CreateSearchResult(foundObjects, "layer");
        }

        /// <summary>
        /// Component-based searchGameObject
        /// </summary>
        private object HandleSearchByComponent(JsonClass args)
        {
            string searchTerm = args["query"]?.Value;
            bool findAll = args["select_many"].AsBoolDefault(false);
            bool searchInInactive = args["include_inactive"].AsBoolDefault(false);

            if (string.IsNullOrEmpty(searchTerm))
            {
                return Response.Error("Search term is required for by_component search.");
            }

            List<GameObject> foundObjects = new List<GameObject>();

            // Try to get component type
            Type componentType = GetComponentType(searchTerm);
            if (componentType == null)
            {
                return Response.Error($"Component type '{searchTerm}' not found.");
            }

            // Search specified component from current sceneGameObject
            GameObject[] allObjects = GetAllGameObjectsInActiveScene(searchInInactive);

            foreach (GameObject go in allObjects)
            {
                if (go.GetComponent(componentType) != null)
                {
                    foundObjects.Add(go);
                }
            }

            return CreateSearchResult(foundObjects, "component");
        }

        /// <summary>
        /// Search by general termGameObject
        /// </summary>
        private object HandleSearchByquery(JsonClass args)
        {
            string searchTerm = args["query"]?.Value;
            bool findAll = args["select_many"].AsBoolDefault(false);
            bool includeHierarchy = args["include_hierarchy"].AsBoolDefault(false);
            bool searchInInactive = args["include_inactive"].AsBoolDefault(false);
            bool useRegex = args["use_regex"].AsBoolDefault(false);

            if (string.IsNullOrEmpty(searchTerm))
            {
                return Response.Error("Search term is required for by_query search.");
            }

            // Check if type-based search（t:TypeName Format）
            bool isTypeSearch = false;
            string typeName = null;
            if (searchTerm.StartsWith("t:", StringComparison.OrdinalIgnoreCase))
            {
                isTypeSearch = true;
                typeName = searchTerm.Substring(2).Trim();
                if (string.IsNullOrEmpty(typeName))
                {
                    return Response.Error("Type name is required after 't:' prefix.");
                }
            }

            // Handle search mode：Wildcard、Regex or normal text
            Regex regex = null;
            bool isPatternMatch = false;

            if (!isTypeSearch)
            {
                // Check if wildcard present
                bool hasWildcards = searchTerm.Contains('*');

                if (useRegex)
                {
                    // Directly use regex
                    try
                    {
                        regex = new Regex(searchTerm, RegexOptions.IgnoreCase);
                        isPatternMatch = true;
                    }
                    catch (ArgumentException ex)
                    {
                        return Response.Error($"Invalid regular expression: {ex.Message}");
                    }
                }
                else if (hasWildcards)
                {
                    // Convert wildcard to regex
                    string regexPattern = ConvertWildcardToRegex(searchTerm);
                    try
                    {
                        regex = new Regex(regexPattern, RegexOptions.IgnoreCase);
                        isPatternMatch = true;
                    }
                    catch (ArgumentException ex)
                    {
                        return Response.Error($"Invalid wildcard pattern: {ex.Message}");
                    }
                }
            }

            List<GameObject> foundObjects = new List<GameObject>();
            HashSet<GameObject> uniqueObjects = new HashSet<GameObject>(); // Avoid duplication

            // If type-based search，Directly useFindObjectsOfType
            if (isTypeSearch)
            {
                Type queryType = GetComponentType(typeName);
                if (queryType == null)
                {
                    return Response.Error($"Component type '{typeName}' not found.");
                }

                GameObject[] sceneObjects = GetAllGameObjectsInActiveScene(searchInInactive);

                foreach (GameObject go in sceneObjects)
                {
                    if (go.GetComponent(queryType) != null)
                    {
                        if (uniqueObjects.Add(go))
                        {
                            foundObjects.Add(go);
                        }
                    }
                }

                return CreateSearchResult(foundObjects, "type");
            }

            // Search all from current sceneGameObject
            GameObject[] allObjects = GetAllGameObjectsInActiveScene(searchInInactive);

            foreach (GameObject go in allObjects)
            {
                bool matches = false;

                // 1. Check name match
                if (isPatternMatch && regex != null)
                {
                    if (regex.IsMatch(go.name))
                    {
                        matches = true;
                    }
                }
                else
                {
                    if (go.name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    {
                        matches = true;
                    }
                }

                // 2. Check tag match
                if (!matches)
                {
                    if (isPatternMatch && regex != null)
                    {
                        if (regex.IsMatch(go.tag))
                        {
                            matches = true;
                        }
                    }
                    else
                    {
                        // Safely check tag，Avoid undefined tag errors
                        try
                        {
                            if (go.CompareTag(searchTerm) || go.tag.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                            {
                                matches = true;
                            }
                        }
                        catch (UnityException)
                        {
                            // Tag not defined，Skip tag match
                        }
                    }
                }

                // 3. Check hierarchy match
                if (!matches)
                {
                    string layerName = LayerMask.LayerToName(go.layer);
                    if (!string.IsNullOrEmpty(layerName))
                    {
                        if (isPatternMatch && regex != null)
                        {
                            if (regex.IsMatch(layerName))
                            {
                                matches = true;
                            }
                        }
                        else
                        {
                            if (layerName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                            {
                                matches = true;
                            }
                        }
                    }
                }

                // 4. Check component match
                if (!matches)
                {
                    Component[] components = go.GetComponents<Component>();
                    foreach (Component component in components)
                    {
                        if (component != null)
                        {
                            string componentTypeName = component.GetType().Name;
                            if (isPatternMatch && regex != null)
                            {
                                if (regex.IsMatch(componentTypeName))
                                {
                                    matches = true;
                                    break;
                                }
                            }
                            else
                            {
                                if (componentTypeName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                                {
                                    matches = true;
                                    break;
                                }
                            }
                        }
                    }
                }

                // 5. Check child object name match（Default enabled）
                if (!matches)
                {
                    Transform[] children = go.GetComponentsInChildren<Transform>();
                    foreach (Transform child in children)
                    {
                        if (child != go.transform)
                        {
                            if (isPatternMatch && regex != null)
                            {
                                if (regex.IsMatch(child.name))
                                {
                                    matches = true;
                                    break;
                                }
                            }
                            else
                            {
                                if (child.name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                                {
                                    matches = true;
                                    break;
                                }
                            }
                        }
                    }
                }

                if (matches && uniqueObjects.Add(go))
                {
                    foundObjects.Add(go);
                }
            }

            return CreateHierarchySearchResult(foundObjects, "term", includeHierarchy);
        }

        // --- Helper Methods ---

        /// <summary>
        /// Get all in active sceneGameObject
        /// </summary>
        private GameObject[] GetAllGameObjectsInActiveScene(bool includeInactive)
        {
            List<GameObject> allObjects = new List<GameObject>();

            // Get root objects in active scene
            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid())
            {
                return allObjects.ToArray();
            }

            GameObject[] rootObjects = activeScene.GetRootGameObjects();

            foreach (GameObject rootObj in rootObjects)
            {
                if (includeInactive)
                {
                    // Include inactive objects，Get all child objects（Including inactives）
                    Transform[] allTransforms = rootObj.GetComponentsInChildren<Transform>(true);
                    foreach (Transform t in allTransforms)
                    {
                        allObjects.Add(t.gameObject);
                    }
                }
                else
                {
                    // Active objects only
                    if (rootObj.activeInHierarchy)
                    {
                        Transform[] activeTransforms = rootObj.GetComponentsInChildren<Transform>(false);
                        foreach (Transform t in activeTransforms)
                        {
                            allObjects.Add(t.gameObject);
                        }
                    }
                }
            }

            return allObjects.ToArray();
        }

        /// <summary>
        /// Create search result
        /// </summary>
        private object CreateSearchResult(List<GameObject> foundObjects, string searchType)
        {
            // Build result data
            var results = foundObjects.Select(go => Json.FromObject(GameObjectUtils.GetGameObjectData(go))).ToList();

            // Build Chinese message
            string message;
            if (results.Count == 0)
            {
                message = $"No GameObjects found using search method: {searchType}.";
            }
            else
            {
                message = $"Found {results.Count} GameObjects using {searchType}.";
            }

            // Build result object，Include execution time、Success flag、Message and data
            var response = new JsonClass
            {
                ["success"] = true,
                ["message"] = message,
                ["data"] = Json.FromObject(results),
                ["exec_time_ms"] = 1.00,
                ["mode"] = "Async mode"
            };

            return response;
        }

        /// <summary>
        /// Create search result containing full hierarchy
        /// </summary>
        private object CreateHierarchySearchResult(List<GameObject> foundObjects, string searchType, bool includeHierarchy)
        {
            // Build result data - UseJObjectEnsure correct serialization
            var results = new List<JsonClass>();

            if (includeHierarchy)
            {
                // Get full hierarchy data
                foreach (var go in foundObjects)
                {
                    var hierarchyData = GetCompleteHierarchyData(go);
                    results.Add(hierarchyData);
                }
            }
            else
            {
                // Use standardGameObjectData
                foreach (var go in foundObjects)
                {
                    results.Add(GameObjectUtils.GetGameObjectData(go));
                }
            }

            // Build message
            string message;
            if (results.Count == 0)
            {
                message = $"No GameObjects found using search method: {searchType}.";
            }
            else
            {
                string hierarchyInfo = includeHierarchy ? " with complete hierarchy" : "";
                message = $"Found {results.Count} GameObjects using {searchType}{hierarchyInfo}.";
            }

            // Build result object，Ensure correct serialization
            var response = new JsonClass
            {
                ["success"] = true,
                ["message"] = message,
                ["data"] = Json.FromObject(results),
                ["exec_time_ms"] = 1.00,
                ["mode"] = "Async mode"
            };

            return response;
        }

        /// <summary>
        /// GetGameObjectFull hierarchy data of（Full info of all child objects）
        /// </summary>
        private JsonClass GetCompleteHierarchyData(GameObject go)
        {
            if (go == null)
                return null;

            // Get current object's basicYAMLData
            var baseYaml = GameObjectUtils.GetGameObjectDataYaml(go);

            // Create resultJSONClass
            JsonClass result = new JsonClass();
            result["yaml"] = baseYaml;

            // Recursively obtain complete data for all child objects
            if (go.transform.childCount > 0)
            {
                JsonArray childrenArray = new JsonArray();
                foreach (Transform child in go.transform)
                {
                    if (child != null && child.gameObject != null)
                    {
                        JsonClass childData = GetCompleteHierarchyData(child.gameObject);
                        if (childData != null)
                        {
                            childrenArray.Add(childData);
                        }
                    }
                }

                if (childrenArray.Count > 0)
                {
                    result["children"] = childrenArray;
                }
            }

            return result;
        }

        /// <summary>
        /// Get component type
        /// </summary>
        private Type GetComponentType(string typeName)
        {
            // Try from commonUnityGet by namespace
            string[] commonNamespaces = {
                "UnityEngine",
                "UnityEngine.UI",
                "UnityEngine.EventSystems",
                "UnityEditor"
            };

            foreach (string ns in commonNamespaces)
            {
                Type type = Type.GetType($"{ns}.{typeName}");
                if (type != null && typeof(UnityEngine.Object).IsAssignableFrom(type))
                    return type;
            }

            // Try get type directly
            Type directType = Type.GetType(typeName);
            if (directType != null && typeof(UnityEngine.Object).IsAssignableFrom(directType))
                return directType;

            // Try searching from all loaded assemblies
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    // Try full name match first
                    Type type = assembly.GetType(typeName);
                    if (type != null && typeof(UnityEngine.Object).IsAssignableFrom(type))
                        return type;

                    // Then iterate all types，Try short name match
                    foreach (var t in assembly.GetTypes())
                    {
                        if ((t.Name == typeName || t.FullName == typeName) &&
                            typeof(UnityEngine.Object).IsAssignableFrom(t))
                            return t;
                    }
                }
                catch
                {
                    // Ignore inaccessible assemblies
                    continue;
                }
            }

            return null;
        }

        /// <summary>
        /// Convert wildcard pattern to regex
        /// </summary>
        /// <param name="wildcardPattern">Contains wildcard*Pattern of</param>
        /// <returns>Regex string</returns>
        private string ConvertWildcardToRegex(string wildcardPattern)
        {
            if (string.IsNullOrEmpty(wildcardPattern))
                return string.Empty;

            // Escape special chars in regex，But keep wildcard*
            string escaped = Regex.Escape(wildcardPattern);

            // Escaped\*Replace as.*（Match any char）
            string regexPattern = escaped.Replace("\\*", ".*");

            // Add anchor to ensure full match
            return $"^{regexPattern}$";
        }
    }
}
