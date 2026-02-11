# 分组重命名功能

## 功能概述

添加了修改分组名称和描述的功能，用户可以直接在分组管理界面中编辑分组信息。

## 修改的文件

### 1. McpExecuteRecordObject.cs
**文件路径**: `Packages/unity-package/Editor/Model/McpExecuteRecordObject.cs`

#### 新增方法：`RenameGroup`

```csharp
/// <summary>
/// 重命名分组
/// </summary>
public bool RenameGroup(string groupId, string newName, string newDescription = null)
```

**功能特性**:
- ✅ 支持重命名分组名称
- ✅ 支持修改分组描述（可选）
- ✅ 保护默认分组不被重命名
- ✅ 检查新名称是否为空
- ✅ 检查新名称是否与其他分组重复
- ✅ 自动保存修改
- ✅ 输出日志记录

**参数说明**:
- `groupId`: 分组的唯一ID
- `newName`: 新的分组名称（必填）
- `newDescription`: 新的分组描述（可选，为null时不修改描述）

**返回值**:
- `true`: 重命名成功
- `false`: 重命名失败（默认分组、名称为空、名称重复、分组不存在）

**安全检查**:
1. 不允许重命名默认分组
2. 名称不能为空或空白
3. 名称不能与现有分组重复（排除自己）
4. 分组必须存在

### 2. McpDebugWindow.cs
**文件路径**: `Packages/unity-package/Editor/GUI/McpDebugWindow.cs`

#### 新增状态变量

```csharp
private string editingGroupId = null;         // 当前正在编辑的分组ID
private string editingGroupName = "";         // 编辑中的分组名称
private string editingGroupDescription = "";  // 编辑中的分组描述
```

#### UI 修改

在分组列表中为每个分组添加了两种显示模式：

**1. 显示模式**（默认）
- 显示分组名称和统计信息
- 提供操作按钮：
  - **Switch（切换）**: 蓝色按钮，切换到该分组
  - **Rename（重命名）**: 绿黄色按钮，进入编辑模式（默认分组禁用）
  - **Delete（删除）**: 红色按钮，删除分组（默认分组禁用）

**2. 编辑模式**（点击"重命名"后）
- 显示可编辑的名称和描述文本框
- 提供操作按钮：
  - **Save（保存）**: 绿色按钮，保存修改
  - **Cancel（取消）**: 灰色按钮，取消编辑

## 使用方法

### 在 UI 中重命名分组

1. 打开 Unity MCP Debug Client 窗口
2. 点击右上角的"管理"按钮打开分组管理面板
3. 在"现有分组"列表中找到要重命名的分组
4. 点击该分组的"重命名"按钮
5. 修改名称和描述
6. 点击"保存"按钮确认，或点击"取消"按钮放弃修改

### 通过代码重命名分组

```csharp
var recordObject = McpExecuteRecordObject.instance;

// 只重命名，不修改描述
bool success = recordObject.RenameGroup("my_group_id", "新的分组名称");

// 同时修改名称和描述
bool success = recordObject.RenameGroup("my_group_id", "新的分组名称", "新的描述信息");

if (success)
{
    Debug.Log("重命名成功");
}
else
{
    Debug.Log("重命名失败");
}
```

## 多语言支持

所有UI文本都支持中英文切换：

| 中文 | English |
|------|---------|
| 重命名 | Rename |
| 名称: | Name: |
| 描述: | Desc: |
| 保存 | Save |
| 取消 | Cancel |
| 重命名失败 | Rename Failed |
| 重命名失败，请检查名称是否有效且未重复。 | Failed to rename group. Please check if the name is valid and not duplicated. |

## 功能限制

1. **默认分组保护**
   - 默认分组（ID为"default"）不能被重命名
   - UI中默认分组的"重命名"按钮会被禁用（灰色）

2. **名称验证**
   - 名称不能为空或只包含空白字符
   - 名称不能与其他现有分组重复

3. **编辑状态**
   - 同一时间只能编辑一个分组
   - 切换编辑对象或关闭管理面板会取消未保存的编辑

## 用户体验优化

1. **视觉反馈**
   - 编辑模式下显示文本输入框，与显示模式有明显区分
   - 按钮颜色编码：蓝色（切换）、绿黄色（重命名）、绿色（保存）、灰色（取消）、红色（删除）
   - 默认分组的"重命名"和"删除"按钮自动禁用

2. **错误处理**
   - 重命名失败时显示友好的错误对话框
   - 控制台输出详细的日志信息

3. **数据持久化**
   - 修改成功后自动保存到磁盘
   - 刷新列表显示最新数据

## 测试建议

1. **基本功能测试**
   - 创建新分组后重命名
   - 修改分组描述
   - 仅修改名称，不修改描述

2. **边界测试**
   - 尝试重命名默认分组（应被阻止）
   - 输入空名称（应显示错误）
   - 输入重复名称（应显示错误）
   - 只输入空格（应显示错误）

3. **交互测试**
   - 编辑过程中点击"取消"
   - 编辑过程中关闭管理面板
   - 快速切换编辑不同分组

4. **语言测试**
   - 切换中英文语言，确认所有文本正确显示

## 技术细节

### 按钮宽度调整

所有操作按钮的宽度从 `60px` 调整为 `50px`，以便在有限空间内容纳三个按钮：
- Switch: 50px
- Rename: 50px
- Delete: 50px

### 状态管理

使用 `editingGroupId` 来标识当前正在编辑的分组：
- `null`: 无分组处于编辑状态
- `"group_id"`: 指定ID的分组正在编辑中

### 数据验证层级

1. **UI层验证**: 
   - 按钮禁用状态（默认分组）
   - 编辑模式切换

2. **业务逻辑层验证** (`RenameGroup` 方法):
   - 默认分组检查
   - 名称非空检查
   - 名称重复检查
   - 分组存在性检查

## 总结

通过添加 `RenameGroup` 方法和更新UI，用户现在可以方便地修改分组信息，同时保持了数据的完整性和一致性。所有修改都经过严格验证，并提供了清晰的用户反馈。
