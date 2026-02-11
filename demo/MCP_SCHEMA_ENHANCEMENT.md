# MCP JSON Schema 增强功能

## 修改概述

本次更新为 MCP 协议的参数定义添加了 `minimum` 和 `maximum` 约束支持，并确认了 `required` 字段的完整支持。

## 修改文件

### 1. MethodKey.cs - 添加最小/最大值属性
**文件路径**: `Packages/unity-package/Editor/Model/MethodKey.cs`

**修改内容**:
- 在 `MethodKey` 基类中添加了 `Minimum` 和 `Maximum` 属性
- 更新了所有构造函数以初始化这两个属性
- 修改了 `MethodInt.SetRange()` 方法，使其在添加示例文本的同时设置 `Minimum` 和 `Maximum` 属性
- 修改了 `MethodFloat.SetRange()` 方法，同样设置 `Minimum` 和 `Maximum` 属性

### 2. McpService.cs - 协议层输出支持
**文件路径**: `Packages/unity-package/Editor/Connect/McpService.cs`

**修改内容**:
- 在 `CreateToolInfo` 方法中添加了对 `minimum` 和 `maximum` JSON Schema 字段的输出支持
- 仅对 `number` 和 `integer` 类型的参数输出这两个字段
- 支持 int、float 和 double 类型的最小/最大值

## 功能说明

### 支持的 JSON Schema 字段

| 字段 | 类型支持 | 说明 | 状态 |
|------|---------|------|------|
| `type` | 所有类型 | 参数类型定义 | ✅ 已支持 |
| `description` | 所有类型 | 参数描述 | ✅ 已支持 |
| `default` | 所有类型 | 默认值 | ✅ 已支持 |
| `examples` | 所有类型 | 示例值数组 | ✅ 已支持 |
| `enum` | 所有类型 | 枚举值（限制可选值） | ✅ 已支持 |
| `required` | Object | 必填字段列表 | ✅ 已支持 |
| `minimum` | number/integer | 最小值约束 | ✅ **新增** |
| `maximum` | number/integer | 最大值约束 | ✅ **新增** |
| `items` | array | 数组元素类型定义 | ✅ 已支持 |
| `minItems` | array | 最小元素数量 | ✅ 已支持 |
| `maxItems` | array | 最大元素数量 | ✅ 已支持 |
| `properties` | object | 对象属性定义 | ✅ 已支持 |
| `format` | 特定类型 | 格式约束 | ✅ 已支持 |

## 使用示例

### 1. 整数类型范围约束

```csharp
// 定义一个 1-100 的整数参数，默认值 50
new MethodInt("quality", "图片质量")
    .SetRange(1, 100)     // ✅ 会输出 minimum: 1, maximum: 100
    .SetDefault(50);

// 生成的 JSON Schema:
{
    "type": "integer",
    "description": "图片质量",
    "minimum": 1,
    "maximum": 100,
    "default": 50,
    "examples": ["范围: 1 - 100"]
}
```

### 2. 浮点数类型范围约束

```csharp
// 定义一个 0.0-1.0 的浮点数参数
new MethodFloat("opacity", "透明度")
    .SetRange(0.0f, 1.0f)  // ✅ 会输出 minimum: 0.0, maximum: 1.0
    .SetDefault(1.0f);

// 生成的 JSON Schema:
{
    "type": "number",
    "description": "透明度",
    "minimum": 0.0,
    "maximum": 1.0,
    "default": 1.0,
    "examples": ["范围: 0.00 - 1.00"]
}
```

### 3. 必填参数 (required)

```csharp
// 第三个参数 optional = false 表示必填
new MethodStr("path", "文件路径", optional: false);

new MethodInt("count", "数量", optional: false)
    .SetRange(1, 100);

// 生成的 JSON Schema (object level):
{
    "type": "object",
    "properties": {
        "path": { "type": "string", "description": "文件路径" },
        "count": { 
            "type": "integer", 
            "description": "数量",
            "minimum": 1,
            "maximum": 100,
            "examples": ["范围: 1 - 100"]
        }
    },
    "required": ["path", "count"]  // ✅ 必填字段列表
}
```

### 4. 实际应用案例

#### 渲染队列设置（EditMaterial.cs）

```csharp
new MethodInt("render_queue", "渲染队列值，控制渲染顺序")
    .SetRange(1000, 5000)
    .AddExample(2000);  // Geometry
```

**输出的 JSON Schema**:
```json
{
    "type": "integer",
    "description": "渲染队列值，控制渲染顺序",
    "minimum": 1000,
    "maximum": 5000,
    "examples": ["范围: 1000 - 5000", "2000"]
}
```

#### 图片压缩质量（GamePlay.cs）

```csharp
new MethodInt("quality", "图片质量（JPG格式1-100）")
    .SetRange(1, 100);
```

**输出的 JSON Schema**:
```json
{
    "type": "integer",
    "description": "图片质量（JPG格式1-100）",
    "minimum": 1,
    "maximum": 100,
    "examples": ["范围: 1 - 100"]
}
```

#### 缩放比例（GamePlay.cs）

```csharp
new MethodFloat("scale", "图片缩放因子")
    .SetRange(0.1f, 5.0f);
```

**输出的 JSON Schema**:
```json
{
    "type": "number",
    "description": "图片缩放因子",
    "minimum": 0.1,
    "maximum": 5.0,
    "examples": ["范围: 0.10 - 5.00"]
}
```

## 优势

### 之前（仅使用 examples）

```json
{
    "type": "integer",
    "description": "图片质量",
    "examples": ["范围: 1 - 100", "50"]
}
```

- ❌ AI 需要解析文本理解范围
- ❌ 无法自动验证参数值
- ❌ 依赖自然语言理解

### 现在（使用 minimum/maximum）

```json
{
    "type": "integer",
    "description": "图片质量",
    "minimum": 1,
    "maximum": 100,
    "default": 50,
    "examples": ["范围: 1 - 100", "50"]
}
```

- ✅ 结构化的范围约束
- ✅ AI 可直接理解数值限制
- ✅ 可用于自动验证
- ✅ 保留了人类可读的文本提示

## 兼容性

- ✅ 向后兼容：未设置范围的参数不受影响
- ✅ 渐进增强：现有代码无需修改即可正常工作
- ✅ 可选功能：调用 `SetRange()` 才会输出 minimum/maximum

## 注意事项

1. **仅对数字类型有效**: `minimum` 和 `maximum` 仅对 `number` 和 `integer` 类型输出
2. **保留 examples**: 范围信息仍会添加到 examples 数组，提供双重保障
3. **类型匹配**: 系统会根据值的类型（int/float/double）正确输出 JSON 数据类型
4. **required 字段**: 通过 `optional: false` 参数控制，在 schema 的 `required` 数组中输出

## 测试建议

### 测试场景

1. **基础范围测试**
   - 创建带 SetRange 的 MethodInt 参数
   - 验证 JSON Schema 包含 minimum 和 maximum 字段
   
2. **浮点数精度测试**
   - 创建带 SetRange 的 MethodFloat 参数
   - 验证浮点数值的正确序列化

3. **必填参数测试**
   - 创建 optional=false 的参数
   - 验证 required 数组包含该参数名

4. **混合参数测试**
   - 同时使用 SetRange、SetDefault、AddExample
   - 验证所有字段都正确输出

### 验证方法

启动 MCP 服务后，可以通过工具列表查看生成的 JSON Schema，确认：
- `minimum` 字段值正确
- `maximum` 字段值正确
- `required` 数组包含所有必填参数
- `examples` 仍然包含范围文本提示

## 总结

本次更新完善了 MCP 协议的参数约束能力，使 AI 客户端能够更准确地理解和验证参数范围，同时确认了 `required` 字段的完整支持。这将提高 AI 工具调用的准确性和可靠性。
