using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UniMcp
{
    public class MethodKey
    {
        // 支持的基础类型常量
        public static readonly HashSet<string> ValidTypes = new HashSet<string>
        {
            "string", "number", "boolean", "array", "object", "integer"
        };

        public string Key;
        public string Desc;
        public bool Optional;
        public string Type;
        public List<string> Examples;
        public List<string> EnumValues;
        public object DefaultValue;

        public MethodKey(string key, string desc, bool optional = true, string type = "string")
        {
            Key = key;
            Desc = desc;
            Optional = optional;
            Type = ValidateType(type);
            Examples = new List<string>();
            EnumValues = new List<string>();
            DefaultValue = null;
        }

        /// <summary>
        /// 验证类型是否有效
        /// </summary>
        public static string ValidateType(string type)
        {
            if (string.IsNullOrEmpty(type))
                throw new System.ArgumentException("类型不能为空");

            if (!ValidTypes.Contains(type))
                throw new System.ArgumentException($"不支持的类型: {type}。支持的类型: {string.Join(", ", ValidTypes)}");

            return type;
        }

        public MethodKey(string key, string desc, bool optional, string type, params string[] examples)
        {
            Key = key;
            Desc = desc;
            Optional = optional;
            Type = ValidateType(type);
            Examples = new List<string>(examples);
            EnumValues = new List<string>();
            DefaultValue = null;
        }

        /// <summary>
        /// 添加示例值
        /// </summary>
        public MethodKey AddExample(string example)
        {
            Examples.Add(example);
            return this;
        }

        /// <summary>
        /// 添加多个示例值
        /// </summary>
        public MethodKey AddExamples(params string[] examples)
        {
            Examples.AddRange(examples);
            return this;
        }

        /// <summary>
        /// 设置枚举值
        /// </summary>
        public MethodKey SetEnumValues(params string[] values)
        {
            EnumValues.Clear();
            EnumValues.AddRange(values);
            return this;
        }

        /// <summary>
        /// 设置默认值
        /// </summary>
        public MethodKey SetDefault(object defaultValue)
        {
            DefaultValue = defaultValue;
            return this;
        }

        /// <summary>
        /// 设置参数类型
        /// </summary>
        public MethodKey SetType(string type)
        {
            Type = ValidateType(type);
            return this;
        }
    }

    /// <summary>
    /// 字符串类型参数
    /// </summary>
    public class MethodStr : MethodKey
    {
        public MethodStr(string key, string desc, bool optional = true)
            : base(key, desc, optional, "string")
        {
        }

        public MethodStr(string key, string desc, bool optional, params string[] examples)
            : base(key, desc, optional, "string", examples)
        {
        }

        /// <summary>
        /// 设置为枚举字符串
        /// </summary>
        public new MethodStr SetEnumValues(params string[] values)
        {
            base.SetEnumValues(values);
            return this;
        }

        /// <summary>
        /// 设置默认字符串值
        /// </summary>
        public MethodStr SetDefault(string defaultValue)
        {
            DefaultValue = defaultValue;
            return this;
        }
    }

    /// <summary>
    /// 整数类型参数
    /// </summary>
    public class MethodInt : MethodKey
    {
        public MethodInt(string key, string desc, bool optional = true)
            : base(key, desc, optional, "integer")
        {
        }

        public MethodInt(string key, string desc, bool optional, params int[] examples)
            : base(key, desc, optional, "integer")
        {
            foreach (var example in examples)
            {
                Examples.Add(example.ToString());
            }
        }

        /// <summary>
        /// 设置默认整数值
        /// </summary>
        public MethodInt SetDefault(int defaultValue)
        {
            DefaultValue = defaultValue;
            return this;
        }

        /// <summary>
        /// 添加整数示例
        /// </summary>
        public MethodInt AddExample(int example)
        {
            Examples.Add(example.ToString());
            return this;
        }

        /// <summary>
        /// 设置数值范围
        /// </summary>
        public MethodInt SetRange(int min, int max)
        {
            Examples.Add($"范围: {min} - {max}");
            return this;
        }
    }

    /// <summary>
    /// 浮点数类型参数
    /// </summary>
    public class MethodFloat : MethodKey
    {
        public MethodFloat(string key, string desc, bool optional = true)
            : base(key, desc, optional, "number")
        {
        }

        public MethodFloat(string key, string desc, bool optional, params float[] examples)
            : base(key, desc, optional, "number")
        {
            foreach (var example in examples)
            {
                Examples.Add(example.ToString("F2"));
            }
        }

        /// <summary>
        /// 设置默认浮点值
        /// </summary>
        public MethodFloat SetDefault(float defaultValue)
        {
            DefaultValue = defaultValue;
            return this;
        }

        /// <summary>
        /// 添加浮点示例
        /// </summary>
        public MethodFloat AddExample(float example)
        {
            Examples.Add(example.ToString("F2"));
            return this;
        }

        /// <summary>
        /// 设置数值范围
        /// </summary>
        public MethodFloat SetRange(float min, float max)
        {
            Examples.Add($"范围: {min:F2} - {max:F2}");
            return this;
        }
    }

    /// <summary>
    /// 布尔类型参数
    /// </summary>
    public class MethodBool : MethodKey
    {
        public MethodBool(string key, string desc, bool optional = true)
            : base(key, desc, optional, "boolean")
        {
            Examples.Add("true");
            Examples.Add("false");
        }

        /// <summary>
        /// 设置默认布尔值
        /// </summary>
        public MethodBool SetDefault(bool defaultValue)
        {
            DefaultValue = defaultValue;
            return this;
        }
    }

    /// <summary>
    /// 数组类型参数
    /// </summary>
    public class MethodArr : MethodKey
    {
        public string ItemType { get; set; }

        public MethodArr(string key, string desc, bool optional = true, string itemType = "string")
            : base(key, desc, optional, "array")
        {
            ItemType = ValidateType(itemType);
        }

        /// <summary>
        /// 设置数组项类型
        /// </summary>
        public MethodArr SetItemType(string itemType)
        {
            ItemType = ValidateType(itemType);
            return this;
        }

        /// <summary>
        /// 添加数组示例
        /// </summary>
        public MethodArr AddExample(params string[] items)
        {
            Examples.Add($"[{string.Join(", ", items)}]");
            return this;
        }

        /// <summary>
        /// 设置默认数组值
        /// </summary>
        public MethodArr SetDefault(params string[] defaultValues)
        {
            DefaultValue = defaultValues;
            return this;
        }
    }

    /// <summary>
    /// 对象类型参数
    /// </summary>
    public class MethodObj : MethodKey
    {
        public Dictionary<string, string> Properties { get; set; }
        public Dictionary<string, string> ArrayItemTypes { get; set; }

        public MethodObj(string key, string desc, bool optional = true)
            : base(key, desc, optional, "object")
        {
            Properties = new Dictionary<string, string>();
            ArrayItemTypes = new Dictionary<string, string>();
        }

        /// <summary>
        /// 添加对象属性
        /// </summary>
        public MethodObj AddProperty(string propName, string propType)
        {
            Properties[propName] = ValidateType(propType);
            return this;
        }

        /// <summary>
        /// 添加字符串类型属性
        /// </summary>
        public MethodObj AddStringProperty(string propName)
        {
            Properties[propName] = "string";
            return this;
        }

        /// <summary>
        /// 添加数字类型属性
        /// </summary>
        public MethodObj AddNumberProperty(string propName)
        {
            Properties[propName] = "number";
            return this;
        }

        /// <summary>
        /// 添加数组类型属性
        /// </summary>
        public MethodObj AddArrayProperty(string propName, string itemType = "string")
        {
            Properties[propName] = "array";
            // 存储数组元素类型信息，用于MCP schema生成
            ArrayItemTypes[propName] = ValidateType(itemType);
            return this;
        }

        /// <summary>
        /// 添加布尔类型属性
        /// </summary>
        public MethodObj AddBooleanProperty(string propName)
        {
            Properties[propName] = "boolean";
            return this;
        }

        /// <summary>
        /// 添加对象类型属性
        /// </summary>
        public MethodObj AddObjectProperty(string propName)
        {
            Properties[propName] = "object";
            return this;
        }
    }

    /// <summary>
    /// 向量类型参数（Unity专用）- 数组形式
    /// </summary>
    public class MethodVector : MethodKey
    {
        public int Dimension { get; set; }

        public MethodVector(string key, string desc, bool optional = true, int dimension = 3)
            : base(key, desc, optional, "array")
        {
            Dimension = dimension;
            switch (dimension)
            {
                case 2:
                    Examples.Add("[1.0, 2.0]");
                    break;
                case 3:
                    Examples.Add("[1.0, 2.0, 3.0]");
                    break;
                case 4:
                    Examples.Add("[1.0, 2.0, 3.0, 4.0]");
                    break;
            }
        }

        /// <summary>
        /// 设置默认向量值
        /// </summary>
        public MethodVector SetDefault(params float[] values)
        {
            DefaultValue = values;
            return this;
        }

        /// <summary>
        /// 添加向量示例
        /// </summary>
        public MethodVector AddExample(params float[] values)
        {
            string vectorString = "[" + string.Join(", ", values.Select(v => v.ToString("F1"))) + "]";
            Examples.Add(vectorString);
            return this;
        }
    }

    /// <summary>
    /// 颜色类型参数（Unity专用）
    /// </summary>
    public class MethodColor : MethodKey
    {
        public MethodColor(string key, string desc, bool optional = true)
            : base(key, desc, optional, "array")
        {
            Examples.Add("[1.0, 0.0, 0.0, 1.0]");
            Examples.Add("[r, g, b, a]");
            Examples.Add("red");
            Examples.Add("blue");
            Examples.Add("#FF0000");
        }

        /// <summary>
        /// 设置默认颜色值
        /// </summary>
        public MethodColor SetDefault(float r, float g, float b, float a = 1.0f)
        {
            DefaultValue = $"[{r}, {g}, {b}, {a}]";
            return this;
        }

        /// <summary>
        /// 设置默认颜色名称
        /// </summary>
        public MethodColor SetDefault(string colorName)
        {
            DefaultValue = colorName;
            return this;
        }
    }
}
