using System.Collections.Generic;

namespace UniMcp.Runtime
{
    public class MethodKey
    {
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
        }

        public MethodKey AddExample(string example)
        {
            Examples.Add(example);
            return this;
        }

        public MethodKey AddExamples(params string[] examples)
        {
            Examples.AddRange(examples);
            return this;
        }

        public MethodKey SetEnumValues(params string[] values)
        {
            EnumValues.Clear();
            EnumValues.AddRange(values);
            return this;
        }

        public MethodKey SetDefault(object defaultValue)
        {
            DefaultValue = defaultValue;
            return this;
        }
    }

    public class MethodVector : MethodKey
    {
        public int Dimension { get; private set; }

        public MethodVector(string key, string desc, int dimension, bool optional = true)
            : base(key, desc, optional, "array")
        {
            Dimension = dimension;
        }
    }

    public class MethodArr : MethodKey
    {
        public string ItemType { get; private set; }

        public MethodArr(string key, string desc, string itemType, bool optional = true)
            : base(key, desc, optional, "array")
        {
            ItemType = itemType;
        }
    }

    public class MethodObj : MethodKey
    {
        public Dictionary<string, string> Properties { get; private set; }
        public Dictionary<string, string> ArrayItemTypes { get; private set; }

        public MethodObj(string key, string desc, bool optional = true)
            : base(key, desc, optional, "object")
        {
            Properties = new Dictionary<string, string>();
            ArrayItemTypes = new Dictionary<string, string>();
        }

        public MethodObj AddProperty(string propName, string propType)
        {
            Properties[propName] = propType;
            return this;
        }

        public MethodObj AddArrayProperty(string propName, string itemType)
        {
            Properties[propName] = "array";
            ArrayItemTypes[propName] = itemType;
            return this;
        }
    }
}
