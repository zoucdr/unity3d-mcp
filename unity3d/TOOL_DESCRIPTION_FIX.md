# Unity3D-MCP 工具描述修复指南

## 问题描述
在创建工具信息时，未获取到有效的unity工具description，需要使用IToolMethod中的Description属性，并将所有继承于IToolMethod的非抽象子类实现Description。

## 需要修改的文件

### 1. McpService.cs
**位置**：`/Users/zht/WorkSpace/unity3d-mcp/unity-package/Editor/Connect/McpService.cs`

**修改内容**：修改`CreateToolInfo`方法，使用IToolMethod中的Description属性

**修改前**：
```csharp
private ToolInfo CreateToolInfo(string toolName, IToolMethod toolInstance)
{
    var toolInfo = new ToolInfo
    {
        name = toolName,
        description = $"Unity工具: {toolName}"
    };
    // ...
}
```

**修改后**：
```csharp
private ToolInfo CreateToolInfo(string toolName, IToolMethod toolInstance)
{
    var toolInfo = new ToolInfo
    {
        name = toolName,
        description = !string.IsNullOrEmpty(toolInstance.Description) ? toolInstance.Description : $"Unity工具: {toolName}"
    };
    // ...
}
```

### 2. StateMethodBase.cs
**位置**：`/Users/zht/WorkSpace/unity3d-mcp/unity-package/Editor/Executer/StateMethodBase.cs`

**修改内容**：添加Description属性的实现

**修改前**：
```csharp
/// <summary>
/// 当前方法支持的参数键列表，用于API文档生成和参数验证。
/// 子类必须实现此属性，定义该方法接受的所有可能参数键。
/// </summary>
public MethodKey[] Keys
{
    get
    {
        if (_keys == null)
        {
            _keys = CreateKeys();
        }
        return _keys;
    }
}
```

**修改后**：
```csharp
/// <summary>
/// 当前方法支持的参数键列表，用于API文档生成和参数验证。
/// 子类必须实现此属性，定义该方法接受的所有可能参数键。
/// </summary>
public MethodKey[] Keys
{
    get
    {
        if (_keys == null)
        {
            _keys = CreateKeys();
        }
        return _keys;
    }
}

/// <summary>
/// 工具方法的描述，用于在状态树中引用
/// </summary>
public virtual string Description { get; } = "未设置工具描述";
```

### 3. DualStateMethodBase.cs
**位置**：`/Users/zht/WorkSpace/unity3d-mcp/unity-package/Editor/Executer/DualStateMethodBase.cs`

**修改内容**：添加Description属性的实现

**修改前**：
```csharp
/// <summary>
/// 当前方法支持的参数键列表，用于API文档生成和参数验证。
/// 子类必须实现此属性，定义该方法接受的所有可能参数键。
/// </summary>
public MethodKey[] Keys
{
    get
    {
        if (_keys == null)
        {
            _keys = CreateKeys();
        }
        return _keys;
    }
}
```

**修改后**：
```csharp
/// <summary>
/// 当前方法支持的参数键列表，用于API文档生成和参数验证。
/// 子类必须实现此属性，定义该方法接受的所有可能参数键。
/// </summary>
public MethodKey[] Keys
{
    get
    {
        if (_keys == null)
        {
            _keys = CreateKeys();
        }
        return _keys;
    }
}

/// <summary>
/// 工具方法的描述，用于在状态树中引用
/// </summary>
public virtual string Description { get; } = "未设置工具描述";
```

## 子类实现

以下是继承自IToolMethod的非抽象子类列表，它们都需要实现Description属性：

1. **DualStateMethodBase子类**：
   - ObjectDelete.cs

2. **StateMethodBase子类**：
   - SourceLocation.cs
   - ProjectSearch.cs
   - ProjectOperate.cs
   - ProjectCreate.cs
   - GameView.cs
   - SceneView.cs
   - CodeRunner.cs
   - PythonRunner.cs
   - RequestHttp.cs
   - GamePlay.cs
   - TagLayer.cs
   - Package.cs
   - ConsoleRead.cs
   - ConsoleWrite.cs

### 子类实现示例

以ProjectSearch.cs为例：

**修改前**：
```csharp
public class ProjectSearch : StateMethodBase
{
    // ...
}
```

**修改后**：
```csharp
public class ProjectSearch : StateMethodBase
{
    /// <summary>
    /// 项目搜索工具，用于搜索项目中的文件和资源
    /// </summary>
    public override string Description { get; } = "项目搜索工具，用于搜索项目中的文件和资源";
    
    // ...
}
```

## 总结
1. 修改McpService.cs，使用IToolMethod.Description
2. 在StateMethodBase和DualStateMethodBase中添加Description属性
3. 为所有继承自IToolMethod的非抽象子类实现Description属性

完成这些修改后，工具描述将能够正确显示，提高工具的可用性和可理解性。