using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniMcp;

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
        public object Minimum;
        public object Maximum;

        public MethodKey(string key, string desc, bool optional = true, string type = "string")
        {
            Key = key;
            Desc = desc;
            Optional = optional;
            Type = ValidateType(type);
            Examples = new List<string>();
            EnumValues = new List<string>();
            DefaultValue = null;
            Minimum = null;
            Maximum = null;
        }

        /// <summary>
        /// Validate if type is valid
        /// </summary>
        public static string ValidateType(string type)
        {
            if (string.IsNullOrEmpty(type))
                throw new System.ArgumentException("Type cannot be empty");

            if (!ValidTypes.Contains(type))
                throw new System.ArgumentException($"Unsupported type: {type}. Supported types: {string.Join(", ", ValidTypes)}");

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
            Minimum = null;
            Maximum = null;
        }

        /// <summary>
        /// Add example value
        /// </summary>
        public MethodKey AddExample(string example)
        {
            Examples.Add(example);
            return this;
        }

        /// <summary>
        /// Add multiple example values
        /// </summary>
        public MethodKey AddExamples(params string[] examples)
        {
            Examples.AddRange(examples);
            return this;
        }

        /// <summary>
        /// Set enumeration values
        /// </summary>
        public MethodKey SetEnumValues(params string[] values)
        {
            EnumValues.Clear();
            EnumValues.AddRange(values);
            return this;
        }

        /// <summary>
        /// Set default value
        /// </summary>
        public MethodKey SetDefault(object defaultValue)
        {
            DefaultValue = defaultValue;
            return this;
        }

        /// <summary>
        /// Set parameter type
        /// </summary>
        public MethodKey SetType(string type)
        {
            Type = ValidateType(type);
            return this;
        }
    }

    /// <summary>
    /// String type parameter
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
        /// Set as enumeration string
        /// </summary>
        public new MethodStr SetEnumValues(params string[] values)
        {
            base.SetEnumValues(values);
            return this;
        }

        /// <summary>
        /// Set default string value
        /// </summary>
        public MethodStr SetDefault(string defaultValue)
        {
            DefaultValue = defaultValue;
            return this;
        }
    }

    /// <summary>
    /// Integer type parameter
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
        /// Set default integer value
        /// </summary>
        public MethodInt SetDefault(int defaultValue)
        {
            DefaultValue = defaultValue;
            return this;
        }

        /// <summary>
        /// Add integer example
        /// </summary>
        public MethodInt AddExample(int example)
        {
            Examples.Add(example.ToString());
            return this;
        }

        /// <summary>
        /// Set numeric range
        /// </summary>
        public MethodInt SetRange(int min, int max)
        {
            Minimum = min;
            Maximum = max;
            string rangeText = L.IsChinese() 
                ? $"范围: {min} - {max}" 
                : $"Range: {min} - {max}";
            Examples.Add(rangeText);
            return this;
        }
    }

    /// <summary>
    /// Float type parameter
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
        /// Set default float value
        /// </summary>
        public MethodFloat SetDefault(float defaultValue)
        {
            DefaultValue = defaultValue;
            return this;
        }

        /// <summary>
        /// Add float example
        /// </summary>
        public MethodFloat AddExample(float example)
        {
            Examples.Add(example.ToString("F2"));
            return this;
        }

        /// <summary>
        /// Set numeric range
        /// </summary>
        public MethodFloat SetRange(float min, float max)
        {
            Minimum = min;
            Maximum = max;
            string rangeText = L.IsChinese() 
                ? $"范围: {min:F2} - {max:F2}" 
                : $"Range: {min:F2} - {max:F2}";
            Examples.Add(rangeText);
            return this;
        }
    }

    /// <summary>
    /// Boolean type parameter
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
        /// Set default boolean value
        /// </summary>
        public MethodBool SetDefault(bool defaultValue)
        {
            DefaultValue = defaultValue;
            return this;
        }
    }

    /// <summary>
    /// Array type parameter
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
        /// Set array item type
        /// </summary>
        public MethodArr SetItemType(string itemType)
        {
            ItemType = ValidateType(itemType);
            return this;
        }

        /// <summary>
        /// Add array example
        /// </summary>
        public MethodArr AddExample(params string[] items)
        {
            Examples.Add($"[{string.Join(", ", items)}]");
            return this;
        }

        /// <summary>
        /// Set default array value
        /// </summary>
        public MethodArr SetDefault(params string[] defaultValues)
        {
            DefaultValue = defaultValues;
            return this;
        }
    }

    /// <summary>
    /// Object type parameter
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
        /// Add object property
        /// </summary>
        public MethodObj AddProperty(string propName, string propType)
        {
            Properties[propName] = ValidateType(propType);
            return this;
        }

        /// <summary>
        /// Add string type property
        /// </summary>
        public MethodObj AddStringProperty(string propName)
        {
            Properties[propName] = "string";
            return this;
        }

        /// <summary>
        /// Add number type property
        /// </summary>
        public MethodObj AddNumberProperty(string propName)
        {
            Properties[propName] = "number";
            return this;
        }

        /// <summary>
        /// Add array type property
        /// </summary>
        public MethodObj AddArrayProperty(string propName, string itemType = "string")
        {
            Properties[propName] = "array";
            // Store array element type information for MCP schema generation
            ArrayItemTypes[propName] = ValidateType(itemType);
            return this;
        }

        /// <summary>
        /// Add boolean type property
        /// </summary>
        public MethodObj AddBooleanProperty(string propName)
        {
            Properties[propName] = "boolean";
            return this;
        }

        /// <summary>
        /// Add object type property
        /// </summary>
        public MethodObj AddObjectProperty(string propName)
        {
            Properties[propName] = "object";
            return this;
        }
    }

    /// <summary>
    /// Vector type parameter (Unity specific) - Array form
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
        /// Set default vector value
        /// </summary>
        public MethodVector SetDefault(params float[] values)
        {
            DefaultValue = values;
            return this;
        }

        /// <summary>
        /// Add vector example
        /// </summary>
        public MethodVector AddExample(params float[] values)
        {
            string vectorString = "[" + string.Join(", ", values.Select(v => v.ToString("F1"))) + "]";
            Examples.Add(vectorString);
            return this;
        }
    }

    /// <summary>
    /// Color type parameter (Unity specific)
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
        /// Set default color value
        /// </summary>
        public MethodColor SetDefault(float r, float g, float b, float a = 1.0f)
        {
            DefaultValue = $"[{r}, {g}, {b}, {a}]";
            return this;
        }

        /// <summary>
        /// Set default color name
        /// </summary>
        public MethodColor SetDefault(string colorName)
        {
            DefaultValue = colorName;
            return this;
        }
    }
}
