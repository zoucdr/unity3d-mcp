/*-*-* Copyright (c) Mycoria@
 * Author: AI Assistant
 * Creation Date: 2025-12-17
 * Version: 1.0.0
 * Description: 资源索引管理器的ProjectSettings面板
 * Features: 
 *   - 添加/删除/搜索资源索引
 *   - 分类管理
 *   - JSON批量导出/导入（支持追加和覆盖模式）
 *   - 快速添加选中对象
 *   - 定位资源功能
 *_*/
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace UniMcp.Tools
{
    /// <summary>
    /// 资源索引管理器的ProjectSettings面板提供器
    /// </summary>
    public class AssetIndexManagerSettingsProvider
    {
        private static Vector2 scrollPosition;
        private static string newName = "";
        private static UnityEngine.Object newAssetObject = null;
        private static string newCategory = "默认";
        private static string newNote = "";
        private static string searchKeyword = "";
        private static List<AssetIndexInfo> displayAssets;
        private static bool showAddSection = true;
        private static int editingId = 0; // 正在编辑的资源索引ID，0表示新增模式

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            var provider = new SettingsProvider("Project/MCP/资源索引管理器", SettingsScope.Project)
            {
                label = "资源索引管理器",
                guiHandler = DrawSettingsGUI,
                deactivateHandler = () => AssetIndexSetting.Save(),
                keywords = new string[] { "asset", "index", "guid", "资源", "索引", "管理" }
            };

            return provider;
        }

        private static void DrawSettingsGUI(string searchContext)
        {
            // 初始化显示列表
            if (displayAssets == null)
            {
                RefreshDisplayAssets();
            }

            EditorGUILayout.Space(5);

            // 标题和统计
            EditorGUILayout.LabelField("资源索引管理器", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"共有 {AssetIndexSetting.instance.GetCount()} 个资源索引", EditorStyles.miniLabel);

            EditorGUILayout.Space(10);

            // 添加/搜索切换按钮
            EditorGUILayout.BeginHorizontal();

            // 添加索引按钮
            GUI.backgroundColor = showAddSection ? new Color(0.7f, 0.9f, 1f) : Color.white;
            if (GUILayout.Button("添加索引", GUILayout.Height(25)))
            {
                showAddSection = true;
            }
            GUI.backgroundColor = Color.white;

            // 搜索索引按钮
            GUI.backgroundColor = !showAddSection ? new Color(0.7f, 0.9f, 1f) : Color.white;
            if (GUILayout.Button("搜索索引", GUILayout.Height(25)))
            {
                showAddSection = false;
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // 显示对应面板
            if (showAddSection)
            {
                DrawAddSection();
            }
            else
            {
                DrawSearchSection();
            }

            EditorGUILayout.Space(15);

            // 资源索引列表
            DrawAssetIndexList();

            EditorGUILayout.Space(10);

            if (!showAddSection)
            {
                // 底部操作按钮
                DrawBottomActions();
            }
        }

        private static void DrawAddSection()
        {
            // 根据编辑模式显示不同标题
            if (editingId > 0)
            {
                EditorGUILayout.LabelField($"编辑资源索引 (ID: {editingId})", EditorStyles.boldLabel);
            }
            else
            {
                EditorGUILayout.LabelField("添加新资源索引", EditorStyles.boldLabel);
            }

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginVertical("box");

            // 如果是编辑模式，显示ID（不可编辑）
            if (editingId > 0)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("索引ID", EditorStyles.miniLabel, GUILayout.Width(100));
                GUI.enabled = false;
                EditorGUILayout.TextField(editingId.ToString());
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(3);
            }

            EditorGUILayout.LabelField("资源对象 *", EditorStyles.miniLabel);
            EditorGUI.BeginChangeCheck();
            newAssetObject = EditorGUILayout.ObjectField(newAssetObject, typeof(UnityEngine.Object), false);
            if (EditorGUI.EndChangeCheck() && newAssetObject != null)
            {
                // 当对象改变时，自动填充名称
                if (editingId == 0 && string.IsNullOrEmpty(newName))
                {
                    newName = newAssetObject.name;
                }
            }

            EditorGUILayout.Space(3);

            EditorGUILayout.LabelField("资源名称 *", EditorStyles.miniLabel);
            newName = EditorGUILayout.TextField(newName);

            EditorGUILayout.Space(3);

            EditorGUILayout.LabelField("分类", EditorStyles.miniLabel);
            newCategory = EditorGUILayout.TextField(newCategory);

            EditorGUILayout.Space(3);

            EditorGUILayout.LabelField("备注", EditorStyles.miniLabel);
            newNote = EditorGUILayout.TextArea(newNote, GUILayout.Height(50));

            EditorGUILayout.Space(8);

            // 按钮区域
            EditorGUILayout.BeginHorizontal();

            GUI.enabled = !string.IsNullOrEmpty(newName) && newAssetObject != null;
            if (editingId > 0)
            {
                GUI.backgroundColor = new Color(0.7f, 0.9f, 1f);
                if (GUILayout.Button("保存修改", GUILayout.Height(30)))
                {
                    SaveAssetIndex();
                }
                GUI.backgroundColor = Color.white;
            }
            else
            {
                if (GUILayout.Button("添加索引", GUILayout.Height(30)))
                {
                    AddAssetIndex();
                }
            }
            GUI.enabled = true;

            // 如果是编辑模式，显示取消按钮
            if (editingId > 0)
            {
                if (GUILayout.Button("取消编辑", GUILayout.Height(30), GUILayout.Width(100)))
                {
                    CancelEdit();
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            // 分类快速筛选
            DrawCategoryFilter();
        }

        private static void DrawSearchSection()
        {
            EditorGUILayout.LabelField("搜索资源索引", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("关键词", GUILayout.Width(50));

            // 实时搜索：检测文本变化
            EditorGUI.BeginChangeCheck();
            searchKeyword = EditorGUILayout.TextField(searchKeyword);
            if (EditorGUI.EndChangeCheck())
            {
                SearchAssetIndices();
            }

            if (GUILayout.Button("清除", GUILayout.Width(80)))
            {
                searchKeyword = "";
                RefreshDisplayAssets();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private static void DrawCategoryFilter()
        {
            var categories = AssetIndexSetting.instance.GetAllCategories();
            if (categories.Count == 0)
                return;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("按分类筛选", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();

            // 添加"全部"按钮
            int totalCount = AssetIndexSetting.instance.GetCount();
            GUI.backgroundColor = new Color(0.8f, 0.9f, 1f);
            if (GUILayout.Button($"全部 ({totalCount})", GUILayout.Height(25), GUILayout.MinWidth(80)))
            {
                searchKeyword = "";
                RefreshDisplayAssets();
            }
            GUI.backgroundColor = Color.white;

            int count = 0;
            foreach (var category in categories)
            {
                int categoryCount = AssetIndexSetting.instance.GetAssetIndicesByCategory(category).Count;

                if (GUILayout.Button($"{category} ({categoryCount})", GUILayout.Height(25), GUILayout.MinWidth(80)))
                {
                    searchKeyword = category;
                    SearchAssetIndices();
                }

                count++;
                if (count % 4 == 0)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private static void DrawAssetIndexList()
        {
            EditorGUILayout.LabelField($"资源索引列表 ({displayAssets.Count})", EditorStyles.boldLabel);

            float windowHeight = EditorGUIUtility.currentViewWidth > 0 ? 400 : 300;

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, "box", GUILayout.Height(windowHeight));

            if (displayAssets.Count == 0)
            {
                EditorGUILayout.HelpBox("没有找到资源索引记录", MessageType.Info);
            }
            else
            {
                for (int i = 0; i < displayAssets.Count; i++)
                {
                    DrawAssetIndexItem(displayAssets[i], i);

                    if (i < displayAssets.Count - 1)
                    {
                        EditorGUILayout.Space(3);
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private static void DrawAssetIndexItem(AssetIndexInfo asset, int index)
        {
            EditorGUILayout.BeginVertical("box");

            // 标题行
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField($"[ID:{asset.id}] {asset.name}", EditorStyles.boldLabel);

            if (!string.IsNullOrEmpty(asset.category) && asset.category != "默认")
            {
                GUILayout.Label($"[{asset.category}]", EditorStyles.miniLabel, GUILayout.Width(100));
            }

            EditorGUILayout.EndHorizontal();

            // Object字段行（可拖拽，有变化时更新GUID）
            string assetPath = asset.GetAssetPath();
            var currentObj = string.IsNullOrEmpty(assetPath) ? null : AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            var newObj = EditorGUILayout.ObjectField("资源:", currentObj, typeof(UnityEngine.Object), false);
            if (EditorGUI.EndChangeCheck() && newObj != currentObj)
            {
                // 对象改变，更新GUID
                string newPath = AssetDatabase.GetAssetPath(newObj);
                if (!string.IsNullOrEmpty(newPath))
                {
                    string newGuid = AssetDatabase.AssetPathToGUID(newPath);
                    if (!string.IsNullOrEmpty(newGuid))
                    {
                        asset.guid = newGuid;
                        AssetIndexSetting.Save();
                        Debug.Log($"[资源索引管理器] 已更新资源对象: {asset.name}");
                    }
                }
            }

            GUI.backgroundColor = new Color(0.7f, 1f, 0.7f);
            if (GUILayout.Button("定位", GUILayout.Width(50)))
            {
                LocateAsset(asset);
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            // 路径行（只读显示）
            if (!string.IsNullOrEmpty(assetPath))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.SelectableLabel($"路径: {assetPath}", EditorStyles.textField, GUILayout.Height(18));

                if (GUILayout.Button("复制", GUILayout.Width(50)))
                {
                    EditorGUIUtility.systemCopyBuffer = assetPath;
                    Debug.Log($"已复制路径: {assetPath}");
                }

                EditorGUILayout.EndHorizontal();
            }

            // 操作按钮行
            EditorGUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();

            GUI.backgroundColor = new Color(0.7f, 0.9f, 1f);
            if (GUILayout.Button("编辑", GUILayout.Width(60)))
            {
                EditAssetIndex(asset);
            }
            GUI.backgroundColor = Color.white;

            GUI.backgroundColor = new Color(1f, 0.7f, 0.7f);
            if (GUILayout.Button("删除", GUILayout.Width(60)))
            {
                if (EditorUtility.DisplayDialog("确认删除", $"确定要删除资源索引 '{asset.name}' (ID: {asset.id}) 吗？", "删除", "取消"))
                {
                    AssetIndexSetting.instance.RemoveAssetIndexById(asset.id);
                    if (editingId == asset.id)
                    {
                        CancelEdit();
                    }
                    RefreshDisplayAssets();
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            // 底部信息行
            EditorGUILayout.BeginHorizontal();

            GUILayout.Label($"索引: {index}", EditorStyles.miniLabel, GUILayout.Width(60));

            if (!string.IsNullOrEmpty(asset.note))
            {
                GUILayout.Label($"备注: {asset.note}", EditorStyles.miniLabel);
            }

            GUILayout.FlexibleSpace();

            if (!string.IsNullOrEmpty(asset.addTime))
            {
                GUILayout.Label($"{asset.addTime}", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private static void DrawBottomActions()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("刷新列表", GUILayout.Height(28)))
            {
                RefreshDisplayAssets();
            }

            GUI.backgroundColor = new Color(0.7f, 1f, 0.7f);
            if (GUILayout.Button("导出为JSON", GUILayout.Height(28)))
            {
                ExportToJson();
            }
            GUI.backgroundColor = Color.white;

            GUI.backgroundColor = new Color(0.7f, 0.9f, 1f);
            if (GUILayout.Button("从JSON导入", GUILayout.Height(28)))
            {
                ImportFromJson();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // 第二行：清空按钮
            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = new Color(1f, 0.7f, 0.7f);
            if (GUILayout.Button("清空所有索引", GUILayout.Height(28)))
            {
                if (EditorUtility.DisplayDialog("确认清空",
                    $"确定要删除所有 {AssetIndexSetting.instance.GetCount()} 个资源索引吗？此操作不可恢复！",
                    "确定删除", "取消"))
                {
                    AssetIndexSetting.instance.ClearAll();
                    RefreshDisplayAssets();
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
        }

        private static void AddAssetIndex()
        {
            if (string.IsNullOrEmpty(newName) || newAssetObject == null)
            {
                EditorUtility.DisplayDialog("错误", "名称和资源对象不能为空", "确定");
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(newAssetObject);
            if (string.IsNullOrEmpty(assetPath))
            {
                EditorUtility.DisplayDialog("错误", "无法获取资源路径", "确定");
                return;
            }

            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
            {
                EditorUtility.DisplayDialog("错误", "无法获取资源GUID", "确定");
                return;
            }

            AssetIndexSetting.instance.AddAssetIndex(newName, guid, newCategory, newNote);

            ClearInputFields();
            RefreshDisplayAssets();

            Debug.Log($"[资源索引管理器] 资源索引添加成功！");
        }

        private static void SaveAssetIndex()
        {
            if (string.IsNullOrEmpty(newName) || newAssetObject == null)
            {
                EditorUtility.DisplayDialog("错误", "名称和资源对象不能为空", "确定");
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(newAssetObject);
            if (string.IsNullOrEmpty(assetPath))
            {
                EditorUtility.DisplayDialog("错误", "无法获取资源路径", "确定");
                return;
            }

            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
            {
                EditorUtility.DisplayDialog("错误", "无法获取资源GUID", "确定");
                return;
            }

            var asset = AssetIndexSetting.instance.GetAssetIndexById(editingId);
            if (asset != null)
            {
                asset.name = newName;
                asset.guid = guid;
                asset.category = newCategory;
                asset.note = newNote;
                asset.addTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                AssetIndexSetting.Save();

                Debug.Log($"[资源索引管理器] 资源索引 (ID: {editingId}) 修改成功！");

                ClearInputFields();
                RefreshDisplayAssets();

                EditorUtility.DisplayDialog("保存成功", $"资源索引 '{asset.name}' 已更新", "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("错误", $"找不到ID为 {editingId} 的资源索引", "确定");
                CancelEdit();
            }
        }

        private static void EditAssetIndex(AssetIndexInfo asset)
        {
            showAddSection = true;

            editingId = asset.id;
            newName = asset.name;

            // 从GUID加载对象
            string assetPath = asset.GetAssetPath();
            newAssetObject = string.IsNullOrEmpty(assetPath) ? null : AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);

            newCategory = asset.category;
            newNote = asset.note;

            Debug.Log($"[资源索引管理器] 开始编辑资源索引 (ID: {asset.id})");
        }

        private static void CancelEdit()
        {
            ClearInputFields();
            Debug.Log($"[资源索引管理器] 取消编辑");
        }

        private static void ClearInputFields()
        {
            editingId = 0;
            newName = "";
            newAssetObject = null;
            newCategory = "默认";
            newNote = "";
        }

        private static void SearchAssetIndices()
        {
            displayAssets = AssetIndexSetting.instance.SearchAssetIndices(searchKeyword);
            SortAssetsByTimeDescending();
        }

        private static void RefreshDisplayAssets()
        {
            displayAssets = AssetIndexSetting.instance.SearchAssetIndices("");
            SortAssetsByTimeDescending();
        }

        private static void SortAssetsByTimeDescending()
        {
            if (displayAssets == null || displayAssets.Count == 0)
                return;

            displayAssets.Sort((a, b) =>
            {
                if (string.IsNullOrEmpty(a.addTime) && string.IsNullOrEmpty(b.addTime))
                    return 0;
                if (string.IsNullOrEmpty(a.addTime))
                    return 1;
                if (string.IsNullOrEmpty(b.addTime))
                    return -1;

                return string.Compare(b.addTime, a.addTime, System.StringComparison.Ordinal);
            });
        }

        private static void LocateAsset(AssetIndexInfo asset)
        {
            string assetPath = asset.GetAssetPath();
            if (string.IsNullOrEmpty(assetPath))
            {
                EditorUtility.DisplayDialog("定位失败", $"无法找到资源路径，GUID可能已失效", "确定");
                return;
            }

            var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (obj == null)
            {
                EditorUtility.DisplayDialog("定位失败", $"无法加载资源: {assetPath}", "确定");
                return;
            }

            Selection.activeObject = obj;
            EditorGUIUtility.PingObject(obj);

            Debug.Log($"[资源索引管理器] 已定位资源: {obj.name} - {assetPath}");
        }

        private static void ExportToJson()
        {
            int count = AssetIndexSetting.instance.GetCount();
            if (count == 0)
            {
                EditorUtility.DisplayDialog("导出失败", "没有可导出的资源索引记录", "确定");
                return;
            }

            string defaultName = $"asset_indices_{System.DateTime.Now:yyyyMMdd_HHmmss}.json";
            string path = EditorUtility.SaveFilePanel("导出资源索引数据", "", defaultName, "json");

            if (string.IsNullOrEmpty(path))
                return;

            try
            {
                string jsonContent = AssetIndexSetting.instance.ExportToJson();
                System.IO.File.WriteAllText(path, jsonContent);

                Debug.Log($"[资源索引管理器] 已导出 {count} 个资源索引到: {path}");
                EditorUtility.DisplayDialog("导出成功",
                    $"已成功导出 {count} 个资源索引到:\n{path}\n\n文件大小: {new System.IO.FileInfo(path).Length / 1024.0:F2} KB",
                    "确定");

                if (EditorUtility.DisplayDialog("打开文件夹？", "是否要打开文件所在的文件夹？", "打开", "关闭"))
                {
                    EditorUtility.RevealInFinder(path);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[资源索引管理器] 导出失败: {e.Message}");
                EditorUtility.DisplayDialog("导出失败", $"导出失败:\n{e.Message}", "确定");
            }
        }

        private static void ImportFromJson()
        {
            string path = EditorUtility.OpenFilePanel("选择要导入的JSON文件", "", "json");

            if (string.IsNullOrEmpty(path))
                return;

            try
            {
                if (!System.IO.File.Exists(path))
                {
                    EditorUtility.DisplayDialog("导入失败", "文件不存在", "确定");
                    return;
                }

                string jsonContent = System.IO.File.ReadAllText(path);

                if (string.IsNullOrEmpty(jsonContent))
                {
                    EditorUtility.DisplayDialog("导入失败", "文件内容为空", "确定");
                    return;
                }

                int option = EditorUtility.DisplayDialogComplex("选择导入模式",
                    $"当前已有 {AssetIndexSetting.instance.GetCount()} 个资源索引\n\n" +
                    "请选择导入模式：\n" +
                    "• 追加模式：保留现有记录，添加新记录，更新重复GUID\n" +
                    "• 覆盖模式：清空所有现有记录，只保留导入的记录",
                    "追加导入", "取消", "覆盖导入");

                if (option == 1)
                    return;

                bool overwrite = (option == 2);

                int importedCount = AssetIndexSetting.instance.ImportFromJson(jsonContent, overwrite);

                if (importedCount > 0)
                {
                    RefreshDisplayAssets();
                    Debug.Log($"[资源索引管理器] 成功导入 {importedCount} 个资源索引，当前共有 {AssetIndexSetting.instance.GetCount()} 个");
                    EditorUtility.DisplayDialog("导入成功",
                        $"成功导入 {importedCount} 个资源索引\n" +
                        $"当前共有 {AssetIndexSetting.instance.GetCount()} 个资源索引记录\n" +
                        $"导入模式: {(overwrite ? "覆盖" : "追加")}",
                        "确定");
                }
                else
                {
                    EditorUtility.DisplayDialog("导入失败", "未能导入任何有效的资源索引记录，请检查JSON格式", "确定");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[资源索引管理器] 导入失败: {e.Message}\n{e.StackTrace}");
                EditorUtility.DisplayDialog("导入失败", $"导入失败:\n{e.Message}", "确定");
            }
        }
    }
}

