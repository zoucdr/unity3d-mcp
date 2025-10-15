using System.Collections.Generic;
using System.Linq;
using System.Reflection;
// Migrated from Newtonsoft.Json to SimpleJson
using UnityEditor;
using UnityEngine;
using UnityMcp.Models;

namespace UnityMcp
{
    public class MenuUtils
    {
        /// <summary>
        /// UGUIWidget menu path mapping
        /// </summary>
        private static readonly Dictionary<string, string[]> UIMenuPaths = new Dictionary<string, string[]>
        {
            // NeedLegacyWidget of path
            { "gameobject/ui/text", new[] { "GameObject/UI/Text", "GameObject/UI/Legacy/Text" } },
            { "gameobject/ui/input_field", new[] { "GameObject/UI/Input Field", "GameObject/UI/Legacy/Input Field" } },
            { "gameobject/ui/inputfield", new[] { "GameObject/UI/Input Field", "GameObject/UI/Legacy/Input Field" } },
            { "gameobject/ui/dropdown", new[] { "GameObject/UI/Dropdown", "GameObject/UI/Legacy/Dropdown" } },
            { "gameobject/ui/button", new[] { "GameObject/UI/Button", "GameObject/UI/Legacy/Button" } },
            
            // Unchanged widget
            { "gameobject/ui/toggle", new[] { "GameObject/UI/Toggle" } },
            { "gameobject/ui/slider", new[] { "GameObject/UI/Slider" } },
            { "gameobject/ui/scrollbar", new[] { "GameObject/UI/Scrollbar" } },
            { "gameobject/ui/scroll_view", new[] { "GameObject/UI/Scroll View" } },
            { "gameobject/ui/panel", new[] { "GameObject/UI/Panel" } },
            { "gameobject/ui/image", new[] { "GameObject/UI/Image" } },
            { "gameobject/ui/raw_image", new[] { "GameObject/UI/Raw Image" } },
            { "gameobject/ui/canvas", new[] { "GameObject/UI/Canvas" } },
            { "gameobject/ui/event_system", new[] { "GameObject/UI/Event System" } }
        };

        /// <summary>
        /// Check whether isUnity 2021.2+（NeedLegacyPath）
        /// </summary>
        private static bool IsLegacyVersion()
        {
            string version = Application.unityVersion;

            // Parse major version
            var parts = version.Split('.');
            if (parts.Length < 2) return true; // For safety，Assume new version

            if (int.TryParse(parts[0], out int major))
            {
                // 2022Year and later versions definitely needLegacy
                if (major >= 2022) return true;

                // 2021Year version needs to check subversion number
                if (major == 2021 && int.TryParse(parts[1], out int minor))
                {
                    return minor >= 2; // 2021.2And later requiredLegacy
                }
            }

            return false; // 2020And earlier versions not neededLegacy
        }

        /// <summary>
        /// Try execute menu item，Automatic compatibility handling
        /// 
        /// Function enhancement：
        /// 1. Automatic handlingUnityMenu path difference of different versions（LegacySupport）
        /// 2. Automatically return all available parent menu list on execution failure
        /// 3. Provide detailed error info and suggestions，Help user find correct menu path
        /// 
        /// Return data structure（When failed）：
        /// - failed_menu_path: Failed menu path
        /// - tried_paths: All tried paths
        /// - unity_version: UnityVersion
        /// - parent_path: Parent menu path
        /// - available_menus_count: Available menu count
        /// - available_menus: Available menu list
        /// </summary>
        public static JsonClass TryExecuteMenuItem(string menuPath)
        {
            if (string.IsNullOrWhiteSpace(menuPath))
            {
                return Response.Error("Menu path is missing or empty.");
            }

            // Normalize menu key name
            string normalizedKey = menuPath.ToLower().Replace(" ", "_").Replace("-", "_");
            bool isLegacy = IsLegacyVersion();

            // Get possible path
            var tryPaths = new List<string>();

            if (UIMenuPaths.TryGetValue(normalizedKey, out string[] knownPaths))
            {
                // If is known widget，Choose path by version
                if (knownPaths.Length > 1)
                {
                    if (isLegacy)
                    {
                        tryPaths.Add(knownPaths[1]); // LegacyPath first
                        tryPaths.Add(knownPaths[0]); // Backup path
                    }
                    else
                    {
                        tryPaths.Add(knownPaths[0]); // Original path first
                        tryPaths.Add(knownPaths[1]); // LegacyBackup path
                    }
                }
                else
                {
                    tryPaths.AddRange(knownPaths);
                }
            }
            else
            {
                tryPaths.Add(menuPath);
            }

            // Try execute in sequence
            foreach (string path in tryPaths)
            {
                bool success = false;
                try
                {
                    // Capture execution result
                    success = EditorApplication.ExecuteMenuItem(path);
                    if (success)
                    {
                        Debug.Log($"[MenuUtils] Successfully executed menu item: '{path}'");
                        return Response.Success($"Successfully executed menu item: '{path}' (Unity {Application.unityVersion})");
                    }
                    else
                    {
                        Debug.LogWarning($"[MenuUtils] Menu item not found or execution returned false: '{path}'");
                    }
                }
                catch (System.Exception ex)
                {
                    // Log exception details
                    Debug.LogError($"[MenuUtils] Exception when executing menu item '{path}': {ex.Message}\nStackTrace: {ex.StackTrace}");
                }
            }

            // Execution failed，Get sibling menu list as suggestion
            string parentPath = GetParentMenuPath(menuPath);
            List<string> availableMenus = new List<string>();

            if (!string.IsNullOrEmpty(parentPath))
            {
                availableMenus = GetMenuItems(parentPath, true);
            }

            // Build error message and data
            var errorData = new JsonClass();
            errorData["failed_menu_path"] = menuPath;
            errorData["tried_paths"] = new JsonArray();
            foreach (var path in tryPaths)
            {
                ((JsonArray)errorData["tried_paths"]).Add(path);
            }
            errorData["unity_version"] = Application.unityVersion;
            errorData["parent_path"] = parentPath ?? "";
            errorData["available_menus_count"] = availableMenus.Count;

            var menusArray = new JsonArray();
            foreach (var menu in availableMenus)
            {
                menusArray.Add(menu);
            }
            errorData["available_menus"] = menusArray;

            string errorMsg = $"ExecuteMenuItem failed because there is no menu named '{menuPath}'.\n" +
                            $"Unity {Application.unityVersion}. Tried: [{string.Join(", ", tryPaths)}]\n";

            if (availableMenus.Count > 0)
            {
                errorMsg += $"\nAvailable menus under '{parentPath}' ({availableMenus.Count} items):\n" +
                           string.Join("\n", availableMenus.Take(20));

                if (availableMenus.Count > 20)
                {
                    errorMsg += $"\n... and {availableMenus.Count - 20} more";
                }
            }
            return Response.Error(errorMsg, errorData);
        }

        /// <summary>
        /// Get parent menu path
        /// </summary>
        private static string GetParentMenuPath(string menuPath)
        {
            if (string.IsNullOrEmpty(menuPath))
                return null;

            int lastSlashIndex = menuPath.LastIndexOf('/');
            if (lastSlashIndex > 0)
            {
                return menuPath.Substring(0, lastSlashIndex);
            }

            return null;
        }

        /// <summary>
        /// Handle menu execute command
        /// </summary>
        public static JsonClass HandleExecuteMenu(JsonClass cmd)
        {
            string menuPath = cmd["menu_path"]?.Value;

            if (string.IsNullOrWhiteSpace(menuPath))
            {
                return Response.Error("Required parameter 'menu_path' is missing or empty.");
            }

            return TryExecuteMenuItem(menuPath);
        }

        /// <summary>
        /// Get all menu items under specified root path（Use reflection）
        /// </summary>
        /// <param name="rootPath">Root path，Such as "GameObject/UI"</param>
        /// <param name="includeSubmenus">Whether contains submenu</param>
        /// <returns>Menu list</returns>
        public static List<string> GetMenuItems(string rootPath, bool includeSubmenus = true)
        {
            var menuItems = new List<string>();

            try
            {
                // Access via reflection Unity Internal menu system
                var menuType = typeof(Menu);

                // Try to get GetMenuItems Function（Different versions may have different signatures）
                MethodInfo getMenuItemsMethod = null;

                // Try signatures1: GetMenuItems(string, bool, bool)
                getMenuItemsMethod = menuType.GetMethod("GetMenuItems",
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
                    null,
                    new[] { typeof(string), typeof(bool), typeof(bool) },
                    null);

                // If not found，Try signatures2: GetMenuItems(string)
                if (getMenuItemsMethod == null)
                {
                    getMenuItemsMethod = menuType.GetMethod("GetMenuItems",
                        BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
                        null,
                        new[] { typeof(string) },
                        null);
                }

                if (getMenuItemsMethod != null)
                {
                    object result = null;
                    var parameters = getMenuItemsMethod.GetParameters();

                    // Call according to parameter count
                    if (parameters.Length == 3)
                    {
                        result = getMenuItemsMethod.Invoke(null, new object[] { rootPath, false, false });
                    }
                    else if (parameters.Length == 1)
                    {
                        result = getMenuItemsMethod.Invoke(null, new object[] { rootPath });
                    }

                    // Handle return result
                    if (result != null)
                    {
                        if (result is System.Collections.IEnumerable enumerable)
                        {
                            foreach (var item in enumerable)
                            {
                                if (item != null)
                                {
                                    // Try multiple ways to get path
                                    string path = null;

                                    // Method1: Through path Property
                                    var pathProperty = item.GetType().GetProperty("path", BindingFlags.Public | BindingFlags.Instance);
                                    if (pathProperty != null)
                                    {
                                        path = pathProperty.GetValue(item) as string;
                                    }

                                    // Method2: Through menuPath Field
                                    if (string.IsNullOrEmpty(path))
                                    {
                                        var menuPathField = item.GetType().GetField("menuPath", BindingFlags.Public | BindingFlags.Instance);
                                        if (menuPathField != null)
                                        {
                                            path = menuPathField.GetValue(item) as string;
                                        }
                                    }

                                    // Method3: Convert directly to string
                                    if (string.IsNullOrEmpty(path))
                                    {
                                        path = item.ToString();
                                    }

                                    if (!string.IsNullOrEmpty(path) && path.StartsWith(rootPath))
                                    {
                                        menuItems.Add(path);
                                    }
                                }
                            }
                        }
                        else if (result is string[] stringArray)
                        {
                            menuItems.AddRange(stringArray.Where(s => !string.IsNullOrEmpty(s) && s.StartsWith(rootPath)));
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"GetMenuItems method not found in Menu type. Available methods: {string.Join(", ", menuType.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public).Select(m => m.Name))}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to get menu items via reflection: {e.Message}\nStackTrace: {e.StackTrace}");
            }

            // Filter submenu
            if (!includeSubmenus && !string.IsNullOrEmpty(rootPath))
            {
                string prefix = rootPath.EndsWith("/") ? rootPath : rootPath + "/";
                menuItems = menuItems.Where(m =>
                {
                    if (m.StartsWith(prefix))
                    {
                        string subPath = m.Substring(prefix.Length);
                        return !subPath.Contains("/");
                    }
                    return false;
                }).ToList();
            }

            return menuItems;
        }

        /// <summary>
        /// Validate if menu item exists
        /// </summary>
        /// <param name="menuPath">Menu path</param>
        /// <returns>Whether menu exists</returns>
        public static bool MenuItemExists(string menuPath)
        {
            try
            {
                // Try execute menu（Do not actually create object）
                bool result = EditorApplication.ExecuteMenuItem(menuPath);

                // Log details to help debugging
                if (result)
                {
                    Debug.Log($"[MenuUtils] Menu item exists: '{menuPath}'");
                }
                else
                {
                    Debug.Log($"[MenuUtils] Menu item does not exist: '{menuPath}'");
                }

                return result;
            }
            catch (System.Exception ex)
            {
                // Log exception details
                Debug.LogError($"[MenuUtils] Exception when checking menu item '{menuPath}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Handle get menu list command
        /// </summary>
        public static object HandleGetMenuItems(JsonClass cmd)
        {
            string rootPath = cmd["root_path"]?.Value ?? "GameObject/UI";
            bool includeSubmenus = cmd["include_submenus"]?.AsBool ?? true;

            var menuItems = GetMenuItems(rootPath, includeSubmenus);

            var data = new JsonClass();
            data["root_path"] = rootPath;
            data["unity_version"] = Application.unityVersion;
            data["is_legacy_version"] = IsLegacyVersion().ToString().ToLower();
            data["total_count"] = menuItems.Count;

            var menusArray = new JsonArray();
            foreach (var menu in menuItems)
            {
                menusArray.Add(menu);
            }
            data["menus"] = menusArray;

            return Response.Success($"Retrieved {menuItems.Count} menu items under '{rootPath}'.", data);
        }
    }
}
