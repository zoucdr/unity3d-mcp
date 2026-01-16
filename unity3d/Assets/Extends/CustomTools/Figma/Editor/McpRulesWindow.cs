using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UniMcp.Models;

namespace UniMcp.Gui
{
    /// <summary>
    /// MCP规则管理窗口 - 用于创建实现 IMcpRule 接口的 ScriptableObject
    /// </summary>
    public class McpRulesWindow : EditorWindow
    {
        [MenuItem("Window/MCP/Rules")]
        public static void ShowWindow()
        {
            McpRulesWindow window = GetWindow<McpRulesWindow>("MCP Rules");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        // UI状态变量
        private Vector2 scrollPosition;
        private List<Type> ruleTypes = new List<Type>();
        private Dictionary<Type, string> ruleSavePaths = new Dictionary<Type, string>();
        private const string DEFAULT_SAVE_PATH = "Assets/ScriptableObjects";
        private const string PREFS_KEY_PREFIX = "McpRulesWindow_SavePath_";

        private void OnEnable()
        {
            // 查找所有实现 IMcpRule 接口的 ScriptableObject 类型
            FindAllRuleTypes();
            // 加载每个规则类型的保存路径
            LoadSavePaths();
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // 标题
            EditorGUILayout.LabelField("MCP 规则创建", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox("列出所有可创建的规则类型，每个规则类型可设置独立的保存路径", MessageType.Info);
            EditorGUILayout.Space(10);

            // 显示可创建的规则类型列表
            DrawRuleTypesList();

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 查找所有实现 IMcpRule 接口的 ScriptableObject 类型
        /// </summary>
        private void FindAllRuleTypes()
        {
            ruleTypes.Clear();

            // 获取所有程序集
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                try
                {
                    // 获取程序集中的所有类型
                    Type[] types = assembly.GetTypes();
                    foreach (Type type in types)
                    {
                        // 检查是否是 ScriptableObject 的子类，且实现了 IMcpRule 接口，且不是抽象类
                        if (typeof(ScriptableObject).IsAssignableFrom(type) &&
                            typeof(IMcpRule).IsAssignableFrom(type) &&
                            !type.IsAbstract)
                        {
                            ruleTypes.Add(type);
                        }
                    }
                }
                catch
                {
                    // 跳过无法访问的程序集
                }
            }

            // 按类型名称排序
            ruleTypes = ruleTypes.OrderBy(t => t.Name).ToList();
        }

        /// <summary>
        /// 绘制规则类型列表
        /// </summary>
        private void DrawRuleTypesList()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"可创建的规则类型 ({ruleTypes.Count})", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            if (ruleTypes.Count == 0)
            {
                EditorGUILayout.HelpBox("未找到实现 IMcpRule 接口的 ScriptableObject 类型", MessageType.Warning);
            }
            else
            {
                foreach (Type ruleType in ruleTypes)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    // 第一行：类型名称和创建按钮
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(ruleType.Name, EditorStyles.boldLabel, GUILayout.ExpandWidth(true));
                    if (GUILayout.Button("创建", GUILayout.Width(80)))
                    {
                        CreateRule(ruleType);
                    }
                    EditorGUILayout.EndHorizontal();

                    // 第二行：保存路径设置
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("保存路径:", GUILayout.Width(70));
                    string currentPath = GetSavePath(ruleType);
                    string newPath = EditorGUILayout.TextField(currentPath);
                    if (newPath != currentPath)
                    {
                        SetSavePath(ruleType, newPath);
                    }
                    if (GUILayout.Button("浏览", GUILayout.Width(60)))
                    {
                        string selectedPath = EditorUtility.OpenFolderPanel($"选择 {ruleType.Name} 保存路径", "Assets", "");
                        if (!string.IsNullOrEmpty(selectedPath) && selectedPath.StartsWith(Application.dataPath))
                        {
                            string relativePath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                            SetSavePath(ruleType, relativePath);
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(3);
                }
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 创建规则
        /// </summary>
        private void CreateRule(Type ruleType)
        {
            // 获取该规则类型的保存路径
            string savePath = GetSavePath(ruleType);

            // 弹出对话框让用户输入名称
            string ruleName = EditorUtility.SaveFilePanelInProject(
                $"创建 {ruleType.Name}",
                $"New{ruleType.Name}",
                "asset",
                $"请输入 {ruleType.Name} 的名称",
                savePath
            );

            if (string.IsNullOrEmpty(ruleName))
            {
                // 用户取消了操作
                return;
            }

            try
            {
                // 创建 ScriptableObject 实例
                ScriptableObject newRule = ScriptableObject.CreateInstance(ruleType);

                // 从完整路径中提取文件名（不含扩展名）作为 asset 的名称
                string fileName = Path.GetFileNameWithoutExtension(ruleName);
                newRule.name = fileName;

                // 创建资产文件
                AssetDatabase.CreateAsset(newRule, ruleName);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"[McpRulesWindow] 成功创建 {ruleType.Name} '{fileName}' 于路径: {ruleName}");

                // 选中新创建的资产
                Selection.activeObject = newRule;
                EditorGUIUtility.PingObject(newRule);

                // 在 Inspector 中显示
                EditorGUIUtility.PingObject(newRule);
            }
            catch (Exception e)
            {
                Debug.LogError($"[McpRulesWindow] 创建规则失败: {e.Message}\n{e.StackTrace}");
                EditorUtility.DisplayDialog("错误", $"创建规则失败: {e.Message}", "确定");
            }
        }

        /// <summary>
        /// 加载所有规则类型的保存路径
        /// </summary>
        private void LoadSavePaths()
        {
            ruleSavePaths.Clear();
            foreach (Type ruleType in ruleTypes)
            {
                string key = GetPrefsKey(ruleType);
                string path = EditorPrefs.GetString(key, DEFAULT_SAVE_PATH);
                ruleSavePaths[ruleType] = path;
            }
        }

        /// <summary>
        /// 获取规则类型的保存路径
        /// </summary>
        private string GetSavePath(Type ruleType)
        {
            if (ruleSavePaths.TryGetValue(ruleType, out string path))
            {
                return path;
            }
            return DEFAULT_SAVE_PATH;
        }

        /// <summary>
        /// 设置规则类型的保存路径
        /// </summary>
        private void SetSavePath(Type ruleType, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                path = DEFAULT_SAVE_PATH;
            }

            ruleSavePaths[ruleType] = path;
            string key = GetPrefsKey(ruleType);
            EditorPrefs.SetString(key, path);
        }

        /// <summary>
        /// 获取规则类型在 EditorPrefs 中的键名
        /// </summary>
        private string GetPrefsKey(Type ruleType)
        {
            return PREFS_KEY_PREFIX + ruleType.FullName;
        }
    }
}

