﻿using System.Collections.Generic;
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
        /// UGUI控件菜单路径映射
        /// </summary>
        private static readonly Dictionary<string, string[]> UIMenuPaths = new Dictionary<string, string[]>
        {
            // 需要Legacy路径的控件
            { "gameobject/ui/text", new[] { "GameObject/UI/Text", "GameObject/UI/Legacy/Text" } },
            { "gameobject/ui/input_field", new[] { "GameObject/UI/Input Field", "GameObject/UI/Legacy/Input Field" } },
            { "gameobject/ui/inputfield", new[] { "GameObject/UI/Input Field", "GameObject/UI/Legacy/Input Field" } },
            { "gameobject/ui/dropdown", new[] { "GameObject/UI/Dropdown", "GameObject/UI/Legacy/Dropdown" } },
            { "gameobject/ui/button", new[] { "GameObject/UI/Button", "GameObject/UI/Legacy/Button" } },
            
            // 没有变化的控件
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
        /// 检查是否为Unity 2021.2+（需要Legacy路径）
        /// </summary>
        private static bool IsLegacyVersion()
        {
            string version = Application.unityVersion;

            // 解析主版本号
            var parts = version.Split('.');
            if (parts.Length < 2) return true; // 安全起见，假设是新版本

            if (int.TryParse(parts[0], out int major))
            {
                // 2022年及以后的版本肯定需要Legacy
                if (major >= 2022) return true;

                // 2021年版本需要检查次版本号
                if (major == 2021 && int.TryParse(parts[1], out int minor))
                {
                    return minor >= 2; // 2021.2及以后需要Legacy
                }
            }

            return false; // 2020及更早版本不需要Legacy
        }

        /// <summary>
        /// 尝试执行菜单项，自动处理兼容性
        /// </summary>
        public static object TryExecuteMenuItem(string menuPath)
        {
            if (string.IsNullOrWhiteSpace(menuPath))
            {
                return Response.Error("Menu path is missing or empty.");
            }

            // 标准化菜单键名
            string normalizedKey = menuPath.ToLower().Replace(" ", "_").Replace("-", "_");
            bool isLegacy = IsLegacyVersion();

            // 获取可能的路径
            var tryPaths = new List<string>();

            if (UIMenuPaths.TryGetValue(normalizedKey, out string[] knownPaths))
            {
                // 如果是已知控件，按版本选择路径
                if (knownPaths.Length > 1)
                {
                    if (isLegacy)
                    {
                        tryPaths.Add(knownPaths[1]); // Legacy路径优先
                        tryPaths.Add(knownPaths[0]); // 备用路径
                    }
                    else
                    {
                        tryPaths.Add(knownPaths[0]); // 原始路径优先
                        tryPaths.Add(knownPaths[1]); // Legacy备用路径
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

            // 依次尝试执行
            foreach (string path in tryPaths)
            {
                try
                {
                    if (EditorApplication.ExecuteMenuItem(path))
                    {
                        return Response.Success($"Successfully executed menu item: '{path}' (Unity {Application.unityVersion})");
                    }
                }
                catch
                {

                }
            }

            return Response.Error($"Failed to execute menu item. Unity {Application.unityVersion}. Tried: [{string.Join(", ", tryPaths)}]");
        }

        /// <summary>
        /// 处理菜单执行命令
        /// </summary>
        public static object HandleExecuteMenu(JsonClass cmd)
        {
            string menuPath = cmd["menu_path"]?.Value;

            if (string.IsNullOrWhiteSpace(menuPath))
            {
                return Response.Error("Required parameter 'menu_path' is missing or empty.");
            }

            return TryExecuteMenuItem(menuPath);
        }

        /// <summary>
        /// 获取指定根路径下的所有菜单项（使用反射）
        /// </summary>
        /// <param name="rootPath">根路径，如 "GameObject/UI"</param>
        /// <param name="includeSubmenus">是否包含子菜单</param>
        /// <returns>菜单列表</returns>
        public static List<string> GetMenuItems(string rootPath, bool includeSubmenus = true)
        {
            var menuItems = new List<string>();

            try
            {
                // 使用反射访问 Unity 内部的菜单系统
                var menuType = typeof(Menu);

                // 尝试获取 GetMenuItems 方法（不同版本可能有不同签名）
                MethodInfo getMenuItemsMethod = null;

                // 尝试签名1: GetMenuItems(string, bool, bool)
                getMenuItemsMethod = menuType.GetMethod("GetMenuItems",
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
                    null,
                    new[] { typeof(string), typeof(bool), typeof(bool) },
                    null);

                // 如果没找到，尝试签名2: GetMenuItems(string)
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

                    // 根据参数数量调用
                    if (parameters.Length == 3)
                    {
                        result = getMenuItemsMethod.Invoke(null, new object[] { rootPath, false, false });
                    }
                    else if (parameters.Length == 1)
                    {
                        result = getMenuItemsMethod.Invoke(null, new object[] { rootPath });
                    }

                    // 处理返回结果
                    if (result != null)
                    {
                        if (result is System.Collections.IEnumerable enumerable)
                        {
                            foreach (var item in enumerable)
                            {
                                if (item != null)
                                {
                                    // 尝试多种方式获取路径
                                    string path = null;

                                    // 方式1: 通过 path 属性
                                    var pathProperty = item.GetType().GetProperty("path", BindingFlags.Public | BindingFlags.Instance);
                                    if (pathProperty != null)
                                    {
                                        path = pathProperty.GetValue(item) as string;
                                    }

                                    // 方式2: 通过 menuPath 字段
                                    if (string.IsNullOrEmpty(path))
                                    {
                                        var menuPathField = item.GetType().GetField("menuPath", BindingFlags.Public | BindingFlags.Instance);
                                        if (menuPathField != null)
                                        {
                                            path = menuPathField.GetValue(item) as string;
                                        }
                                    }

                                    // 方式3: 直接转换为字符串
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

            // 过滤子菜单
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
        /// 验证菜单项是否存在
        /// </summary>
        /// <param name="menuPath">菜单路径</param>
        /// <returns>菜单是否存在</returns>
        public static bool MenuItemExists(string menuPath)
        {
            try
            {
                // 尝试执行菜单（不实际创建对象）
                return EditorApplication.ExecuteMenuItem(menuPath);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 处理获取菜单列表命令
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
