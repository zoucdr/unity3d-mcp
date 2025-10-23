# MateMatchUI 开发计划

本开发计划基于 mcp 工具链，实现从 Figma 到 Unity UGUI 的完整 UI 界面还原。以下是详细步骤：

## 1. Figma 数据获取
- 使用 `mcp_unitymcp_figma_manage` 下载 Figma 页面节点信息，包括页面预览图。
- 调用 `mcp_unitymcp_figma_manage` 获取 Figma 转换为 UGUI 的规则（坐标转换、图片尺寸、缩放规则等）。

## 2. UI 层级设计
- 综合 Figma 页面信息和预览图，设计合理的 UGUI 层级结构。
- 明确分配可交互控件与非交互组件（后者合并显示）。

## 3. Canvas 与根容器建立
- 利用 `mcp_unitymcp_hierarchy_create` 创建 Canvas 及根容器。
- 设置 Canvas 尺寸，确保 UGUI 坐标以屏幕中心为原点。

## 4. Game 窗口与 UI 尺寸匹配
- 通过 `mcp_unitymcp_game_view` 调整 Game 窗口的分辨率，使其与设计稿一致。

## 5. UI 组件创建与布局
- 按照设计稿，逐步创建各 UI 组件，所有文本组件必须使用 TMP 字体。
- 检查缺失控件，并补全后反复校验。
- 使用 `mcp_unitymcp_ugui_layout` 工具，对各组件进行布局调整，根据组件所在位置自动选择合适锚点（anchor preset）。
- 对于全屏根节点，确保设置全屏拉伸属性。

## 6. UI 规则记录
- 利用 `mcp_unitymcp_ui_rule_manage` 将创建的 UI 组件名称与原 Figma 节点 ID 记录到规则文件，便于后续维护。
- 同步记录组件修改方式及图片信息。

## 7. 图片资源加载
- 下载所有设计稿中需要的图片，保存路径固定于 `Assets/Pics/MateMatchUI`，图片缩放比例保持 1x。
- 使用 mcp 工具加载下载的图片至对应 UI 组件上，确保资源不重复创建临时图片。

## 8. 屏幕适配和最终校验
- 调用 `mcp_unitymcp_game_view` 对 Game 窗口进行屏幕截图，以确认 UI 整体还原效果。
- 使用 `mcp_unitymcp_figma_manage` 的预览接口获取整体预览图，分析 UI 还原度。
- 若还原效果不足，则进一步优化布局与组件属性，直至达到设计稿要求。

## 9. 总结与文档记录
- 汇总整个开发过程中所调用的 mcp 工具链各步骤，记录到项目文档中，便于后期维护和迭代更新。

---

以上即为 MateMatchUI 的开发步骤规划，可按照此计划逐步开发并验证界面还原度。