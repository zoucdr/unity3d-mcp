/*-*-* Copyright (c) Mycoria@
 * Author: zouhangte
 * Creation Date: 2025-12-12
 * Version: 1.2.0
 * Description: 网页管理器的ProjectSettings面板
 * Features: 
 *   - 添加/删除/搜索网页
 *   - 分类管理
 *   - JSON批量导出/导入（支持追加和覆盖模式）
 *   - 自动处理URL重复
 *_*/
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace UniMcp.Tools
{
    /// <summary>
    /// 网页管理器的ProjectSettings面板提供器
    /// </summary>
    public class WebPageManagerSettingsProvider
    {
        private static Vector2 scrollPosition;
        private static string newDescription = "";
        private static string newUrl = "";
        private static string newCategory = "默认";
        private static string newNote = "";
        private static string searchKeyword = "";
        private static List<WebPageInfo> displayPages;
        private static bool showAddSection = true;
        private static int editingId = 0; // 正在编辑的网页ID，0表示新增模式

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            var provider = new SettingsProvider("Project/MCP/网页管理器", SettingsScope.Project)
            {
                label = "网页管理器",
                guiHandler = DrawSettingsGUI,
                deactivateHandler = () => WebPageSetting.Save(),
                keywords = new string[] { "webpage", "web", "url", "网页", "管理", "链接" }
            };

            return provider;
        }

        private static void DrawSettingsGUI(string searchContext)
        {
            // 初始化显示列表
            if (displayPages == null)
            {
                RefreshDisplayPages();
            }

            EditorGUILayout.Space(5);

            // 标题和统计
            EditorGUILayout.LabelField("网页管理器", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"共有 {WebPageSetting.instance.GetCount()} 个网页", EditorStyles.miniLabel);

            EditorGUILayout.Space(10);

            // 添加/搜索切换按钮
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Toggle(showAddSection, "添加网页", "Button", GUILayout.Height(25)))
            {
                showAddSection = true;
            }
            if (GUILayout.Toggle(!showAddSection, "搜索网页", "Button", GUILayout.Height(25)))
            {
                showAddSection = false;
            }
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

            // 网页列表
            DrawWebPageList();

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
                EditorGUILayout.LabelField($"编辑网页 (ID: {editingId})", EditorStyles.boldLabel);
            }
            else
            {
                EditorGUILayout.LabelField("添加新网页", EditorStyles.boldLabel);
            }

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginVertical("box");

            // 如果是编辑模式，显示ID（不可编辑）
            if (editingId > 0)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("网页ID", EditorStyles.miniLabel, GUILayout.Width(100));
                GUI.enabled = false;
                EditorGUILayout.TextField(editingId.ToString());
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(3);
            }

            EditorGUILayout.LabelField("网站描述 *", EditorStyles.miniLabel);
            newDescription = EditorGUILayout.TextField(newDescription);

            EditorGUILayout.Space(3);

            EditorGUILayout.LabelField("网站URL *", EditorStyles.miniLabel);
            newUrl = EditorGUILayout.TextField(newUrl);

            EditorGUILayout.Space(3);

            EditorGUILayout.LabelField("分类", EditorStyles.miniLabel);
            newCategory = EditorGUILayout.TextField(newCategory);

            EditorGUILayout.Space(3);

            EditorGUILayout.LabelField("备注", EditorStyles.miniLabel);
            newNote = EditorGUILayout.TextArea(newNote, GUILayout.Height(50));

            EditorGUILayout.Space(8);

            // 按钮区域
            EditorGUILayout.BeginHorizontal();

            GUI.enabled = !string.IsNullOrEmpty(newDescription) && !string.IsNullOrEmpty(newUrl);
            if (editingId > 0)
            {
                GUI.backgroundColor = new Color(0.7f, 0.9f, 1f);
                if (GUILayout.Button("保存修改", GUILayout.Height(30)))
                {
                    SaveWebPage();
                }
                GUI.backgroundColor = Color.white;
            }
            else
            {
                if (GUILayout.Button("添加网页", GUILayout.Height(30)))
                {
                    AddWebPage();
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

            // 分类快速筛选（移到添加页面）
            DrawCategoryFilter();
        }

        private static void DrawSearchSection()
        {
            EditorGUILayout.LabelField("搜索网页", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("关键词", GUILayout.Width(50));

            // 实时搜索：检测文本变化
            EditorGUI.BeginChangeCheck();
            searchKeyword = EditorGUILayout.TextField(searchKeyword);
            if (EditorGUI.EndChangeCheck())
            {
                // 文本变化时立即搜索
                SearchWebPages();
            }

            if (GUILayout.Button("清除", GUILayout.Width(80)))
            {
                searchKeyword = "";
                RefreshDisplayPages();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private static void DrawCategoryFilter()
        {
            var categories = WebPageSetting.instance.GetAllCategories();
            if (categories.Count == 0)
                return;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("按分类筛选", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();

            // 添加"全部"按钮，显示总数量
            int totalCount = WebPageSetting.instance.GetCount();
            GUI.backgroundColor = new Color(0.8f, 0.9f, 1f);
            if (GUILayout.Button($"全部 ({totalCount})", GUILayout.Height(25), GUILayout.MinWidth(80)))
            {
                searchKeyword = "";
                RefreshDisplayPages();
            }
            GUI.backgroundColor = Color.white;

            int count = 0;
            foreach (var category in categories)
            {
                // 获取该分类的数量
                int categoryCount = WebPageSetting.instance.GetWebPagesByCategory(category).Count;

                if (GUILayout.Button($"{category} ({categoryCount})", GUILayout.Height(25), GUILayout.MinWidth(80)))
                {
                    searchKeyword = category;
                    SearchWebPages();
                }

                count++;
                // 每行显示5个分类按钮（包含"全部"按钮，所以实际是4个分类）
                if (count % 4 == 0)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private static void DrawWebPageList()
        {
            EditorGUILayout.LabelField($"网页列表 ({displayPages.Count})", EditorStyles.boldLabel);

            // 计算ScrollView高度
            float windowHeight = EditorGUIUtility.currentViewWidth > 0 ? 400 : 300;

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, "box", GUILayout.Height(windowHeight));

            if (displayPages.Count == 0)
            {
                EditorGUILayout.HelpBox("没有找到网页记录", MessageType.Info);
            }
            else
            {
                for (int i = 0; i < displayPages.Count; i++)
                {
                    DrawWebPageItem(displayPages[i], i);

                    if (i < displayPages.Count - 1)
                    {
                        EditorGUILayout.Space(3);
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private static void DrawWebPageItem(WebPageInfo page, int index)
        {
            EditorGUILayout.BeginVertical("box");

            // 标题行
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField($"[ID:{page.id}] {page.description}", EditorStyles.boldLabel);

            if (!string.IsNullOrEmpty(page.category) && page.category != "默认")
            {
                GUILayout.Label($"[{page.category}]", EditorStyles.miniLabel, GUILayout.Width(100));
            }

            EditorGUILayout.EndHorizontal();

            // URL行
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.SelectableLabel(page.url, EditorStyles.textField, GUILayout.Height(18));

            if (GUILayout.Button("打开", GUILayout.Width(50)))
            {
                Application.OpenURL(page.url);
            }

            if (GUILayout.Button("复制", GUILayout.Width(50)))
            {
                EditorGUIUtility.systemCopyBuffer = page.url;
                Debug.Log($"已复制URL: {page.url}");
            }

            GUI.backgroundColor = new Color(0.7f, 0.9f, 1f);
            if (GUILayout.Button("编辑", GUILayout.Width(50)))
            {
                EditWebPage(page);
            }
            GUI.backgroundColor = Color.white;

            GUI.backgroundColor = new Color(1f, 0.7f, 0.7f);
            if (GUILayout.Button("删除", GUILayout.Width(50)))
            {
                if (EditorUtility.DisplayDialog("确认删除", $"确定要删除网页 '{page.description}' (ID: {page.id}) 吗？", "删除", "取消"))
                {
                    WebPageSetting.instance.RemoveWebPageById(page.id);
                    // 如果正在编辑这个网页，清除编辑状态
                    if (editingId == page.id)
                    {
                        CancelEdit();
                    }
                    RefreshDisplayPages();
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            // 底部信息行：索引、备注、时间
            EditorGUILayout.BeginHorizontal();

            // 显示索引信息（灰色小字）
            GUILayout.Label($"索引: {index}", EditorStyles.miniLabel, GUILayout.Width(60));

            // 备注
            if (!string.IsNullOrEmpty(page.note))
            {
                GUILayout.Label($"备注: {page.note}", EditorStyles.miniLabel);
            }

            GUILayout.FlexibleSpace();

            // 添加时间
            if (!string.IsNullOrEmpty(page.addTime))
            {
                GUILayout.Label($"{page.addTime}", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private static void DrawBottomActions()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("刷新列表", GUILayout.Height(28)))
            {
                RefreshDisplayPages();
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
            if (GUILayout.Button("清空所有网页", GUILayout.Height(28)))
            {
                if (EditorUtility.DisplayDialog("确认清空",
                    $"确定要删除所有 {WebPageSetting.instance.GetCount()} 个网页吗？此操作不可恢复！",
                    "确定删除", "取消"))
                {
                    WebPageSetting.instance.ClearAll();
                    RefreshDisplayPages();
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
        }

        private static void AddWebPage()
        {
            if (string.IsNullOrEmpty(newDescription) || string.IsNullOrEmpty(newUrl))
            {
                EditorUtility.DisplayDialog("错误", "描述和URL不能为空", "确定");
                return;
            }

            if (!newUrl.StartsWith("http://") && !newUrl.StartsWith("https://") && !newUrl.StartsWith("file://"))
            {
                EditorUtility.DisplayDialog("错误", "URL必须以 http://, https:// 或 file:// 开头", "确定");
                return;
            }

            WebPageSetting.instance.AddWebPage(newDescription, newUrl, newCategory, newNote);

            // 清空输入
            ClearInputFields();

            RefreshDisplayPages();

            Debug.Log($"[网页管理器] 网页添加成功！");
        }

        private static void SaveWebPage()
        {
            if (string.IsNullOrEmpty(newDescription) || string.IsNullOrEmpty(newUrl))
            {
                EditorUtility.DisplayDialog("错误", "描述和URL不能为空", "确定");
                return;
            }

            if (!newUrl.StartsWith("http://") && !newUrl.StartsWith("https://") && !newUrl.StartsWith("file://"))
            {
                EditorUtility.DisplayDialog("错误", "URL必须以 http://, https:// 或 file:// 开头", "确定");
                return;
            }

            // 查找要编辑的网页
            var page = WebPageSetting.instance.GetWebPageById(editingId);
            if (page != null)
            {
                // 更新信息
                page.description = newDescription;
                page.url = newUrl;
                page.category = newCategory;
                page.note = newNote;
                page.addTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                WebPageSetting.Save();

                Debug.Log($"[网页管理器] 网页 (ID: {editingId}) 修改成功！");

                // 清空输入并退出编辑模式
                ClearInputFields();
                RefreshDisplayPages();

                EditorUtility.DisplayDialog("保存成功", $"网页 '{page.description}' 已更新", "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("错误", $"找不到ID为 {editingId} 的网页", "确定");
                CancelEdit();
            }
        }

        private static void EditWebPage(WebPageInfo page)
        {
            // 切换到添加页面
            showAddSection = true;

            // 填充数据
            editingId = page.id;
            newDescription = page.description;
            newUrl = page.url;
            newCategory = page.category;
            newNote = page.note;

            Debug.Log($"[网页管理器] 开始编辑网页 (ID: {page.id})");
        }

        private static void CancelEdit()
        {
            ClearInputFields();
            Debug.Log($"[网页管理器] 取消编辑");
        }

        private static void ClearInputFields()
        {
            editingId = 0;
            newDescription = "";
            newUrl = "";
            newCategory = "默认";
            newNote = "";
        }

        private static void SearchWebPages()
        {
            displayPages = WebPageSetting.instance.SearchWebPages(searchKeyword);
            SortPagesByTimeDescending();
        }

        private static void RefreshDisplayPages()
        {
            displayPages = WebPageSetting.instance.SearchWebPages("");
            SortPagesByTimeDescending();
        }

        private static void SortPagesByTimeDescending()
        {
            if (displayPages == null || displayPages.Count == 0)
                return;

            // 按添加时间倒序排列（最新的在前面）
            displayPages.Sort((a, b) =>
            {
                // 处理空时间的情况
                if (string.IsNullOrEmpty(a.addTime) && string.IsNullOrEmpty(b.addTime))
                    return 0;
                if (string.IsNullOrEmpty(a.addTime))
                    return 1;
                if (string.IsNullOrEmpty(b.addTime))
                    return -1;

                // 比较时间字符串（倒序）
                return string.Compare(b.addTime, a.addTime, System.StringComparison.Ordinal);
            });
        }

        private static void ExportToJson()
        {
            int count = WebPageSetting.instance.GetCount();
            if (count == 0)
            {
                EditorUtility.DisplayDialog("导出失败", "没有可导出的网页记录", "确定");
                return;
            }

            string defaultName = $"webpages_{System.DateTime.Now:yyyyMMdd_HHmmss}.json";
            string path = EditorUtility.SaveFilePanel("导出网页数据", "", defaultName, "json");

            if (string.IsNullOrEmpty(path))
                return;

            try
            {
                string jsonContent = WebPageSetting.instance.ExportToJson();
                System.IO.File.WriteAllText(path, jsonContent);

                Debug.Log($"[网页管理器] 已导出 {count} 个网页到: {path}");
                EditorUtility.DisplayDialog("导出成功",
                    $"已成功导出 {count} 个网页到:\n{path}\n\n文件大小: {new System.IO.FileInfo(path).Length / 1024.0:F2} KB",
                    "确定");

                // 询问是否打开文件所在文件夹
                if (EditorUtility.DisplayDialog("打开文件夹？", "是否要打开文件所在的文件夹？", "打开", "关闭"))
                {
                    EditorUtility.RevealInFinder(path);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[网页管理器] 导出失败: {e.Message}");
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

                // 询问导入模式
                int option = EditorUtility.DisplayDialogComplex("选择导入模式",
                    $"当前已有 {WebPageSetting.instance.GetCount()} 个网页\n\n" +
                    "请选择导入模式：\n" +
                    "• 追加模式：保留现有记录，添加新记录，更新重复URL\n" +
                    "• 覆盖模式：清空所有现有记录，只保留导入的记录",
                    "追加导入", "取消", "覆盖导入");

                if (option == 1) // 取消
                    return;

                bool overwrite = (option == 2); // 0=追加, 2=覆盖

                // 执行导入
                int importedCount = WebPageSetting.instance.ImportFromJson(jsonContent, overwrite);

                if (importedCount > 0)
                {
                    RefreshDisplayPages();
                    Debug.Log($"[网页管理器] 成功导入 {importedCount} 个网页，当前共有 {WebPageSetting.instance.GetCount()} 个网页");
                    EditorUtility.DisplayDialog("导入成功",
                        $"成功导入 {importedCount} 个网页\n" +
                        $"当前共有 {WebPageSetting.instance.GetCount()} 个网页记录\n" +
                        $"导入模式: {(overwrite ? "覆盖" : "追加")}",
                        "确定");
                }
                else
                {
                    EditorUtility.DisplayDialog("导入失败", "未能导入任何有效的网页记录，请检查JSON格式", "确定");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[网页管理器] 导入失败: {e.Message}\n{e.StackTrace}");
                EditorUtility.DisplayDialog("导入失败", $"导入失败:\n{e.Message}", "确定");
            }
        }
    }
}

