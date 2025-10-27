# Unity3d MCP 项目 Copilot 指令

## 项目架构与核心知识
- **分层架构**：AI客户端（Cursor/Claude/Trae）→ MCP协议层（Python Server + Unity Package）→ 通信层（TCP Socket, JSON-RPC）→ Unity编辑器层 → 工具层（32+工具/状态树）
- **主要目录**：
  - `server/`：Python MCP服务端，基于FastMCP，入口为`server.py`，配置见`pyproject.toml`和`requirements.txt`
  - `unity-package/`：Unity编辑器插件，C#实现，包含所有MCP工具与窗口
  - `unity3d/`：Unity项目源码、插件、UI、工具等
  - `docs/`：静态网站与文档

## 开发与调试流程
- **服务启动**：
  - Python服务端：`uv --directory server run server.py`（或见`README.md`/`package.json`）
  - Unity端：导入`unity-package`，通过`Window > MCP`菜单访问工具窗口
- **调试工具**：
  - `McpDebugWindow`（`unity-package/Editor/GUI/`）：用于测试和调试MCP函数调用，支持JSON输入和结果预览
  - 日志控制：`README_LogControl.md`，统一通过`McpLogger.EnableLog`开关，设置持久化到`EditorPrefs`，所有相关文件需条件检查
- **协程系统**：
  - 基于`EditorApplication.update`，核心类`MainThreadExecutor`、`CoroutineManager`、`StateTreeContext`，详见`README_Coroutines.md`
  - 支持命名/延迟/重复/条件/异步协程，所有协程在主线程执行，场景切换自动停止

## 代码/工具调用约定
- **MCP工具调用**：
  - 推荐通过`async_call`和`batch_call`（见`.cursor/rules/unity-mcp.mdc`），所有MethodTools需通过FacadeTools参数化调用
  - `async_call`支持异步调用：`type='in'`执行任务，`type='out'`获取结果，需要`id`参数标识任务
  - GM指令/调试命令统一用`gm_command`方法
- **UI开发**：
  - UI Toolkit示例见`Assets/UIToolkit/README.md`，UXML/USS/Controller分离，图片资源需按Figma节点ID命名
  - 圆角UI推荐用`ProceduralUIImage`（见`unity_package_proceduraluiimage/README.md`）

## 项目约定与注意事项
- Python代码生成到`server/`，C#生成到`Assets/Scripts/`
- 禁止在`try-catch`内写`yield`，非`MonoBehaviour`类禁止直接用`Destroy`等Unity内置方法
- 日志输出必须受`EnableLog`统一控制，新增日志需条件检查
- Unity版本推荐`2021.3.x`，跨平台支持Win/Mac/Android/iOS

## 关键文件/目录参考
- `server/server.py`、`unity-package/Editor/GUI/McpDebugWindow.cs`、`unity-package/README_LogControl.md`、`unity-package/README_Coroutines.md`、`unity3d/.cursor/rules/unity-mcp.mdc`、`Assets/UIToolkit/README.md`

---
如有不清楚或遗漏的部分，请反馈以便补充完善。
