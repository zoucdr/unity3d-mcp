
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UniMcp
{
    public enum JsonBinaryTag
    {
        Array = 1,
        Class = 2,
        Value = 3,
        IntValue = 4,
        DoubleValue = 5,
        BoolValue = 6,
        FloatValue = 7,
    }

    public class JsonNode
    {
        #region common interface
        public virtual void Add(string aKey, JsonNode aItem) { }
        public virtual JsonNode this[int aIndex] { get { return null; } set { } }
        public virtual JsonNode this[string aKey] { get { return null; } set { } }
        public virtual string Value { get { return ""; } set { } }
        public virtual int Count { get { return 0; } }

        public virtual void Add(JsonNode aItem)
        {
            Add("", aItem);
        }
        public virtual JsonNodeType type
        {
            get
            {
                return GetJSONNodeType();
            }
        }
        public virtual JsonNode Remove(string aKey) { return null; }
        public virtual JsonNode Remove(int aIndex) { return null; }
        public virtual JsonNode Remove(JsonNode aNode) { return aNode; }

        public virtual IEnumerable<JsonNode> Childs { get { yield break; } }
        public IEnumerable<JsonNode> DeepChilds
        {
            get
            {
                foreach (var C in Childs)
                    foreach (var D in C.DeepChilds)
                        yield return D;
            }
        }

        public override string ToString()
        {
            return "JsonNode";
        }
        public virtual string ToString(string aPrefix)
        {
            return "JsonNode";
        }

        /// <summary>
        /// 格式化输出 JSON，带有自动换行和缩进，方便阅读
        /// </summary>
        /// <param name="indent">缩进字符，默认为4个空格</param>
        /// <returns>格式化后的 JSON 字符串</returns>
        public string ToPrettyString(string indent = "    ")
        {
            return ToPrettyStringInternal(0, indent);
        }

        /// <summary>
        /// 格式化输出 JSON 的内部递归方法
        /// </summary>
        /// <param name="level">当前缩进层级</param>
        /// <param name="indent">缩进字符</param>
        /// <returns>格式化后的 JSON 字符串</returns>
        public virtual string ToPrettyStringInternal(int level, string indent)
        {
            return ToString();
        }

        /// <summary>
        /// 导出为 YAML 格式字符串
        /// </summary>
        /// <param name="indent">缩进字符，默认为2个空格</param>
        /// <returns>YAML 格式字符串</returns>
        public string ToYamlString(string indent = "  ")
        {
            return ToYamlStringInternal(0, indent, false);
        }

        /// <summary>
        /// 导出为 YAML 格式字符串的内部递归方法
        /// </summary>
        /// <param name="level">当前缩进层级</param>
        /// <param name="indent">缩进字符</param>
        /// <param name="isArrayItem">是否为数组项</param>
        /// <returns>YAML 格式字符串</returns>
        public virtual string ToYamlStringInternal(int level, string indent, bool isArrayItem)
        {
            // 默认实现：返回字符串值
            return EscapeYamlString(Value);
        }

        /// <summary>
        /// 转义 YAML 字符串（如果需要引号则添加）
        /// </summary>
        protected static string EscapeYamlString(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "\"\"";

            // 检查是否需要引号
            bool needQuotes = false;

            // 包含特殊字符需要引号
            if (value.Contains(':') || value.Contains('#') || value.Contains('[') || value.Contains(']') ||
                value.Contains('{') || value.Contains('}') || value.Contains(',') || value.Contains('&') ||
                value.Contains('*') || value.Contains('!') || value.Contains('|') || value.Contains('>') ||
                value.Contains('\'') || value.Contains('\"') || value.Contains('%') || value.Contains('@') ||
                value.Contains('`') || value.StartsWith("-") || value.StartsWith("?") ||
                value.StartsWith(" ") || value.EndsWith(" ") || value.Contains('\n') || value.Contains('\r'))
            {
                needQuotes = true;
            }

            // 看起来像布尔值或null但实际是字符串
            string lower = value.ToLower();
            if (lower == "true" || lower == "false" || lower == "null" || lower == "~" ||
                lower == "yes" || lower == "no" || lower == "on" || lower == "off")
            {
                needQuotes = true;
            }

            // 看起来像数字但实际是字符串
            if (!needQuotes && (int.TryParse(value, out _) || float.TryParse(value, out _) || double.TryParse(value, out _)))
            {
                needQuotes = true;
            }

            if (needQuotes)
            {
                // 使用双引号并转义内部的双引号和反斜杠
                return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t") + "\"";
            }

            return value;
        }

        #endregion common interface

        #region typecasting properties
        public virtual int AsInt
        {
            get
            {
                int v = 0;
                if (int.TryParse(Value, out v))
                    return v;
                return 0;
            }
            set
            {
                Value = value.ToString();
            }
        }
        public virtual float AsFloat
        {
            get
            {
                float v = 0.0f;
                if (float.TryParse(Value, out v))
                    return v;
                return 0.0f;
            }
            set
            {
                Value = value.ToString();
            }
        }
        public virtual double AsDouble
        {
            get
            {
                double v = 0.0;
                if (double.TryParse(Value, out v))
                    return v;
                return 0.0;
            }
            set
            {
                Value = value.ToString();
            }
        }
        public virtual bool AsBool
        {
            get
            {
                bool v = false;
                if (bool.TryParse(Value, out v))
                    return v;
                return !string.IsNullOrEmpty(Value);
            }
            set
            {
                Value = (value) ? "true" : "false";
            }
        }

        /// <summary>
        /// 获取布尔值，如果节点为 null 则返回默认值
        /// </summary>
        public bool AsBoolDefault(bool defaultValue)
        {
            if (this == null || this.IsNull()) return defaultValue;
            return AsBool;
        }

        /// <summary>
        /// 获取整数值，如果节点为 null 则返回默认值
        /// </summary>
        public int AsIntDefault(int defaultValue)
        {
            if (this == null || this.IsNull()) return defaultValue;
            return AsInt;
        }

        /// <summary>
        /// 获取浮点值，如果节点为 null 则返回默认值
        /// </summary>
        public float AsFloatDefault(float defaultValue)
        {
            if (this == null || this.IsNull()) return defaultValue;
            return AsFloat;
        }

        /// <summary>
        /// 获取双精度浮点值，如果节点为 null 则返回默认值
        /// </summary>
        public double AsDoubleDefault(double defaultValue)
        {
            if (this == null || this.IsNull()) return defaultValue;
            return AsDouble;
        }
        public virtual JsonArray AsArray
        {
            get
            {
                return this as JsonArray;
            }
        }
        public virtual JsonClass AsObject
        {
            get
            {
                return this as JsonClass;
            }
        }

        #endregion typecasting properties

        #region operators
        public static implicit operator JsonNode(string s)
        {
            return new JsonData(s);
        }
        public static implicit operator string(JsonNode d)
        {
            return (d == null) ? null : d.Value;
        }

        // 隐式转换：支持从基本类型转换为 JsonNode
        public static implicit operator JsonNode(int aInt)
        {
            return new JsonData(aInt);
        }
        public static implicit operator JsonNode(float aFloat)
        {
            return new JsonData(aFloat);
        }
        public static implicit operator JsonNode(double aDouble)
        {
            return new JsonData(aDouble);
        }
        public static implicit operator JsonNode(bool aBool)
        {
            return new JsonData(aBool);
        }
        public static implicit operator JsonNode(long aLong)
        {
            return new JsonData(aLong);
        }
        public static implicit operator JsonNode(decimal aDecimal)
        {
            return new JsonData(aDecimal);
        }
        public static implicit operator JsonNode(uint aUInt)
        {
            return new JsonData(aUInt);
        }
        public static implicit operator JsonNode(short aShort)
        {
            return new JsonData(aShort);
        }
        public static implicit operator JsonNode(ushort aUShort)
        {
            return new JsonData(aUShort);
        }
        public static implicit operator JsonNode(byte aByte)
        {
            return new JsonData(aByte);
        }
        public static implicit operator JsonNode(sbyte aSByte)
        {
            return new JsonData(aSByte);
        }
        public static implicit operator JsonNode(char aChar)
        {
            return new JsonData(aChar);
        }
        public static bool operator ==(JsonNode a, object b)
        {
            if (b == null && a is JsonLazyCreator)
                return true;
            return System.Object.ReferenceEquals(a, b);
        }

        public static bool operator !=(JsonNode a, object b)
        {
            return !(a == b);
        }
        public override bool Equals(object obj)
        {
            return System.Object.ReferenceEquals(this, obj);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }


        #endregion operators

        #region 兼容 Newtonsoft.Json 的方法

        /// <summary>
        /// 尝试获取值（类似 JsonClass.TryGetValue）
        /// </summary>
        public virtual bool TryGetValue(string key, out JsonNode value)
        {
            value = this[key];
            return value != null && !value.IsNull();
        }

        /// <summary>
        /// 尝试获取 Json 值（别名，兼容旧代码）
        /// </summary>
        public virtual bool TryGetJsonValue(string key, out JsonNode token)
        {
            return TryGetValue(key, out token);
        }

        /// <summary>
        /// 检查是否为 null
        /// </summary>
        public virtual bool IsNull()
        {
            if (this == null)
                return true;

            string val = Value;
            return string.IsNullOrEmpty(val) || val == "null";
        }

        /// <summary>
        /// 获取 JsonNode 的类型（类似 JsonNode.type）
        /// </summary>
        public virtual JsonNodeType GetJSONNodeType()
        {
            if (this == null || this.IsNull())
                return JsonNodeType.Null;

            if (this is JsonArray)
                return JsonNodeType.Array;

            if (this is JsonClass)
                return JsonNodeType.Object;

            // JsonData - 尝试判断实际类型
            var val = Value;
            if (bool.TryParse(val, out _))
                return JsonNodeType.Boolean;

            if (int.TryParse(val, out _))
                return JsonNodeType.Integer;

            if (float.TryParse(val, out _))
                return JsonNodeType.Float;

            return JsonNodeType.String;
        }

        /// <summary>
        /// 安全地转换为 JsonClass
        /// </summary>
        public JsonClass ToObject()
        {
            return this as JsonClass;
        }

        /// <summary>
        /// 安全地转换为 JsonArray
        /// </summary>
        public JsonArray ToArray()
        {
            return this as JsonArray;
        }

#if UNITY_EDITOR || UNITY_STANDALONE
        /// <summary>
        /// 转换为 Vector2
        /// </summary>
        public UnityEngine.Vector2? ToVector2()
        {
            var array = this as JsonArray;
            if (array == null || array.Count < 2) return null;

            return new UnityEngine.Vector2(
                array[0].AsFloat,
                array[1].AsFloat
            );
        }

        /// <summary>
        /// 转换为 Vector3
        /// </summary>
        public UnityEngine.Vector3? ToVector3()
        {
            var array = this as JsonArray;
            if (array == null || array.Count < 3) return null;

            return new UnityEngine.Vector3(
                array[0].AsFloat,
                array[1].AsFloat,
                array[2].AsFloat
            );
        }

        /// <summary>
        /// 转换为 Vector4
        /// </summary>
        public UnityEngine.Vector4? ToVector4()
        {
            var array = this as JsonArray;
            if (array == null || array.Count < 4) return null;

            return new UnityEngine.Vector4(
                array[0].AsFloat,
                array[1].AsFloat,
                array[2].AsFloat,
                array[3].AsFloat
            );
        }

        /// <summary>
        /// 转换为 Color
        /// </summary>
        public UnityEngine.Color? ToColor()
        {
            var obj = this as JsonClass;
            if (obj != null && obj.ContainsKey("r"))
            {
                return new UnityEngine.Color(
                    obj["r"].AsFloat,
                    obj["g"].AsFloat,
                    obj["b"].AsFloat,
                    obj["a"].AsFloat
                );
            }

            var array = this as JsonArray;
            if (array != null && array.Count >= 3)
            {
                return new UnityEngine.Color(
                    array[0].AsFloat,
                    array[1].AsFloat,
                    array[2].AsFloat,
                    array.Count > 3 ? array[3].AsFloat : 1f
                );
            }

            return null;
        }
#endif

        /// <summary>
        /// 将 JsonNode 转换为指定类型的对象（兼容 Newtonsoft.Json 的 ToObject 方法）
        /// </summary>
        /// <param name="targetType">目标类型</param>
        /// <returns>转换后的对象，如果无法转换则返回 null</returns>
        public virtual object ToObject(Type targetType)
        {
            try
            {
                if (this == null || this.IsNull())
                    return null;

                if (targetType == typeof(string))
                    return this.Value;
                if (targetType == typeof(int) || targetType == typeof(Int32))
                    return this.AsInt;
                if (targetType == typeof(long) || targetType == typeof(Int64))
                    return (long)this.AsInt;
                if (targetType == typeof(short) || targetType == typeof(Int16))
                    return (short)this.AsInt;
                if (targetType == typeof(float) || targetType == typeof(Single))
                    return this.AsFloat;
                if (targetType == typeof(double) || targetType == typeof(Double))
                    return (double)this.AsFloat;
                if (targetType == typeof(bool) || targetType == typeof(Boolean))
                    return this.AsBool;
                if (targetType == typeof(byte))
                    return (byte)this.AsInt;

#if UNITY_EDITOR || UNITY_STANDALONE
                if (targetType == typeof(UnityEngine.Vector2))
                {
                    var vec2 = this.ToVector2();
                    return vec2.HasValue ? vec2.Value : default(UnityEngine.Vector2);
                }
                if (targetType == typeof(UnityEngine.Vector3))
                {
                    var vec3 = this.ToVector3();
                    return vec3.HasValue ? vec3.Value : default(UnityEngine.Vector3);
                }
                if (targetType == typeof(UnityEngine.Vector4))
                {
                    var vec4 = this.ToVector4();
                    return vec4.HasValue ? vec4.Value : default(UnityEngine.Vector4);
                }
                if (targetType == typeof(UnityEngine.Color))
                {
                    var color = this.ToColor();
                    return color.HasValue ? color.Value : default(UnityEngine.Color);
                }
                if (targetType == typeof(UnityEngine.Quaternion) && this is JsonArray arr && arr.Count >= 4)
                {
                    return new UnityEngine.Quaternion(
                        arr[0].AsFloat,
                        arr[1].AsFloat,
                        arr[2].AsFloat,
                        arr[3].AsFloat
                    );
                }
#endif

                if (targetType.IsEnum)
                {
                    return Enum.Parse(targetType, this.Value, true);
                }

                // 如果是字符串且目标类型是 UnityEngine.Object 的子类，尝试加载资源
#if UNITY_EDITOR || UNITY_STANDALONE
                if (this is JsonData && this.type == JsonNodeType.String &&
                    typeof(UnityEngine.Object).IsAssignableFrom(targetType))
                {
                    string assetPath = this.Value;
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        return UnityEditor.AssetDatabase.LoadAssetAtPath(assetPath, targetType);
                    }
                }
#endif
                return System.Convert.ChangeType(this.Value, targetType);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[JsonNode.ToObject] 无法将 '{this}' 转换为类型 '{targetType.Name}': {ex.Message}");
            }

            return null;
        }

        #endregion

        internal static string Escape(string aText)
        {
            if (string.IsNullOrEmpty(aText))
            {
                return string.Empty;
            }

            var sb = new System.Text.StringBuilder(aText.Length * 2);
            foreach (char c in aText)
            {
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '\"': sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    default: sb.Append(c); break;
                }
            }
            return sb.ToString();
        }


        // 若整段是 "...." 的 JSON 字符串，循环解包到 {..} 或 [..]
        private static string UnwrapToJsonIfPossible(string s, int maxRounds = 3)
        {
            if (string.IsNullOrEmpty(s)) return s;
            string cur = s.Trim();
            for (int round = 0; round < maxRounds; round++)
            {
                if (cur.Length >= 2 && cur[0] == '"' && cur[^1] == '"')
                {
                    cur = JsonStringUnescape(cur.Substring(1, cur.Length - 2)).Trim();
                    if ((cur.Length > 1 && ((cur[0] == '{' && cur[^1] == '}') || (cur[0] == '[' && cur[^1] == ']'))))
                        return cur;
                    continue;
                }
                break;
            }
            return cur;
        }

        // 处理形如 {\"k\":\"v\"} 这种“未包外引号却整体被转义”的文本
        private static string TryNormalizeEscapedJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            string t = s.Trim();
            if (t.Length == 0) return t;

            // 仅在看起来像 JSON（以 { 或 [ 开头）时尝试
            if (t[0] == '{' || t[0] == '[')
            {
                // 检测是否只有转义引号（\"），几乎没有未转义引号
                bool hasEscapedQuotes = t.Contains("\\\"");
                bool hasUnescapedQuotes = HasAnyUnescapedQuote(t);

                if (hasEscapedQuotes && !hasUnescapedQuotes)
                {
                    // 把整段当成“JSON字符串内容”按 JSON 规则反转义一次
                    string u = JsonStringUnescape(t).Trim();
                    if (u.Length > 1 && ((u[0] == '{' && u[^1] == '}') || (u[0] == '[' && u[^1] == ']')))
                        return u;
                }
            }
            return s;
        }

        private static bool HasAnyUnescapedQuote(string s)
        {
            bool escaped = false;
            for (int i = 0; i < s.Length; i++)
            {
                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                if (s[i] == '\\')
                {
                    escaped = true;
                    continue;
                }

                if (s[i] == '"')
                    return true; // 找到未转义的引号
            }
            return false;
        }

        // 标准 JSON 字符串反转义
        private static string JsonStringUnescape(string s)
        {
            var sb = new System.Text.StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '\\' && i + 1 < s.Length)
                {
                    char C = s[++i];
                    switch (C)
                    {
                        case '"': sb.Append('\"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            if (i + 4 < s.Length)
                            {
                                string hex = s.Substring(i + 1, 4);
                                sb.Append((char)int.Parse(hex, System.Globalization.NumberStyles.AllowHexSpecifier));
                                i += 4;
                            }
                            break;
                        default: sb.Append(C); break;
                    }
                }
                else sb.Append(c);
            }
            return sb.ToString();
        }

        public static JsonNode Parse(string aJSON)
        {
            if (string.IsNullOrEmpty(aJSON))
                return new JsonClass();

            aJSON = UnwrapToJsonIfPossible(aJSON);
            aJSON = TryNormalizeEscapedJson(aJSON);

            // 预分配栈容量，减少动态扩容
            Stack<JsonNode> stack = new Stack<JsonNode>(8);
            JsonNode ctx = null;
            int i = 0;
            int length = aJSON.Length;

            // 使用StringBuilder替代字符串拼接
            var tokenBuilder = new System.Text.StringBuilder(64);
            var tokenNameBuilder = new System.Text.StringBuilder(32);
            bool QuoteMode = false;
            bool TokenIsQuoted = false; // 标记当前Token是否源自引号内（即使为空也需提交）

            while (i < length)
            {
                char currentChar = aJSON[i];

                switch (currentChar)
                {
                    case '{':
                        if (QuoteMode) { tokenBuilder.Append(currentChar); break; }

                        stack.Push(new JsonClass());
                        if (ctx != null)
                        {
                            string tokenName = tokenNameBuilder.ToString().Trim();
                            if (ctx is JsonArray)
                                ctx.Add(stack.Peek());
                            else if (tokenName.Length > 0)
                                ctx.Add(tokenName, stack.Peek());
                        }
                        tokenNameBuilder.Clear();
                        tokenBuilder.Clear();
                        TokenIsQuoted = false;
                        ctx = stack.Peek();
                        break;

                    case '[':
                        if (QuoteMode) { tokenBuilder.Append(currentChar); break; }

                        stack.Push(new JsonArray());
                        if (ctx != null)
                        {
                            string tokenName = tokenNameBuilder.ToString().Trim();
                            if (ctx is JsonArray)
                                ctx.Add(stack.Peek());
                            else if (tokenName.Length > 0)
                                ctx.Add(tokenName, stack.Peek());
                        }
                        tokenNameBuilder.Clear();
                        tokenBuilder.Clear();
                        TokenIsQuoted = false;
                        ctx = stack.Peek();
                        break;

                    case '}':
                    case ']':
                        if (QuoteMode) { tokenBuilder.Append(currentChar); break; }

                        if (stack.Count == 0)
                            throw new Exception("Json Parse: Too many closing brackets");

                        // 关闭当前容器前，把最后一个尚未提交的值/成员写入
                        string token = tokenBuilder.ToString();
                        if (token.Length > 0 || TokenIsQuoted)
                        {
                            string tokenName = tokenNameBuilder.ToString().Trim();
                            if (ctx is JsonArray)
                                ctx.Add(token);
                            else if (tokenName.Length > 0)
                                ctx.Add(tokenName, token);
                        }

                        stack.Pop();
                        tokenNameBuilder.Clear();
                        tokenBuilder.Clear();
                        TokenIsQuoted = false;

                        if (stack.Count > 0)
                            ctx = stack.Peek();
                        break;

                    case ':':
                        // 只有在对象上下文且不在引号内时，冒号才是 key/value 分隔符；
                        // 其它情况（如 key 含 ':' 或字符串值内的 ':'）都当普通字符。
                        if (QuoteMode || !(ctx is JsonClass))
                        {
                            tokenBuilder.Append(currentChar);
                            break;
                        }
                        tokenNameBuilder.Append(tokenBuilder);
                        tokenBuilder.Clear();
                        TokenIsQuoted = false;
                        break;

                    case '"':
                        // 引号切换字符串模式；配合下方的反斜杠处理可正确跳过转义引号
                        QuoteMode = !QuoteMode;
                        if (QuoteMode)            // 进入引号，标记该Token来自引号
                            TokenIsQuoted = true;
                        break;

                    case ',':
                        if (QuoteMode) { tokenBuilder.Append(currentChar); break; }

                        token = tokenBuilder.ToString();
                        if (token.Length > 0 || TokenIsQuoted)
                        {
                            string tokenName = tokenNameBuilder.ToString().Trim();
                            if (ctx is JsonArray)
                                ctx.Add(token);
                            else if (tokenName.Length > 0)
                                ctx.Add(tokenName, token);
                        }
                        tokenNameBuilder.Clear();
                        tokenBuilder.Clear();
                        TokenIsQuoted = false;
                        break;

                    case '\r':
                    case '\n':
                        break;

                    case ' ':
                    case '\t':
                        if (QuoteMode)
                            tokenBuilder.Append(currentChar);
                        break;

                    case '\\':
                        ++i;
                        if (i >= length) break;

                        if (QuoteMode)
                        {
                            char C = aJSON[i];
                            switch (C)
                            {
                                case 't': tokenBuilder.Append('\t'); break;
                                case 'r': tokenBuilder.Append('\r'); break;
                                case 'n': tokenBuilder.Append('\n'); break;
                                case 'b': tokenBuilder.Append('\b'); break;
                                case 'f': tokenBuilder.Append('\f'); break;
                                case 'u':
                                    if (i + 4 < length)
                                    {
                                        string s = aJSON.Substring(i + 1, 4);
                                        tokenBuilder.Append((char)int.Parse(s, System.Globalization.NumberStyles.AllowHexSpecifier));
                                        i += 4;
                                    }
                                    break;
                                default: tokenBuilder.Append(C); break; // 包括 '\"' 在内的普通转义
                            }
                        }
                        break;

                    default:
                        tokenBuilder.Append(currentChar);
                        break;
                }
                ++i;
            }

            if (QuoteMode)
                throw new Exception("Json Parse: Quotation marks seems to be messed up.");

            return ctx;
        }

        public virtual void Serialize(System.IO.BinaryWriter aWriter) { }

        public void SaveToStream(System.IO.Stream aData)
        {
            var W = new System.IO.BinaryWriter(aData);
            Serialize(W);
        }

#if USE_SharpZipLib
            public void SaveToCompressedStream(System.IO.Stream aData)
{
            using (var gzipOut = new ICSharpCode.SharpZipLib.BZip2.BZip2OutputStream(aData))
{
                gzipOut.IsStreamOwner = false;
SaveToStream(gzipOut);
gzipOut.Close();
}
}
 
public void SaveToCompressedFile(string aFileName)
{
#if USE_FileIO
            System.IO.Directory.CreateDirectory((new System.IO.FileInfo(aFileName)).Directory.FullName);
using(var F = System.IO.File.OpenWrite(aFileName))
{
    SaveToCompressedStream(F);
}
#else
            throw new Exception("Can't use File IO stuff in webplayer");
#endif
}
public string SaveToCompressedBase64()
{
    using (var stream = new System.IO.MemoryStream())
    {
        SaveToCompressedStream(stream);
        stream.Position = 0;
        return System.Convert.ToBase64String(stream.ToArray());
    }
}
 
#else
        public void SaveToCompressedStream(System.IO.Stream aData)
        {
            throw new Exception("Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
        }
        public void SaveToCompressedFile(string aFileName)
        {
            throw new Exception("Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
        }
        public string SaveToCompressedBase64()
        {
            throw new Exception("Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
        }
#endif

        public void SaveToFile(string aFileName)
        {
#if USE_FileIO
            System.IO.Directory.CreateDirectory((new System.IO.FileInfo(aFileName)).Directory.FullName);
using(var F = System.IO.File.OpenWrite(aFileName))
{
    SaveToStream(F);
}
#else
            throw new Exception("Can't use File IO stuff in webplayer");
#endif
        }
        public string SaveToBase64()
        {
            using (var stream = new System.IO.MemoryStream())
            {
                SaveToStream(stream);
                stream.Position = 0;
                return System.Convert.ToBase64String(stream.ToArray());
            }
        }
        public static JsonNode Deserialize(System.IO.BinaryReader aReader)
        {
            JsonBinaryTag type = (JsonBinaryTag)aReader.ReadByte();
            switch (type)
            {
                case JsonBinaryTag.Array:
                    {
                        int count = aReader.ReadInt32();
                        JsonArray tmp = new JsonArray();
                        for (int i = 0; i < count; i++)
                            tmp.Add(Deserialize(aReader));
                        return tmp;
                    }
                case JsonBinaryTag.Class:
                    {
                        int count = aReader.ReadInt32();
                        JsonClass tmp = new JsonClass();
                        for (int i = 0; i < count; i++)
                        {
                            string key = aReader.ReadString();
                            var val = Deserialize(aReader);
                            tmp.Add(key, val);
                        }
                        return tmp;
                    }
                case JsonBinaryTag.Value:
                    {
                        return new JsonData(aReader.ReadString());
                    }
                case JsonBinaryTag.IntValue:
                    {
                        return new JsonData(aReader.ReadInt32());
                    }
                case JsonBinaryTag.DoubleValue:
                    {
                        return new JsonData(aReader.ReadDouble());
                    }
                case JsonBinaryTag.BoolValue:
                    {
                        return new JsonData(aReader.ReadBoolean());
                    }
                case JsonBinaryTag.FloatValue:
                    {
                        return new JsonData(aReader.ReadSingle());
                    }

                default:
                    {
                        throw new Exception("Error deserializing Json. Unknown tag: " + type);
                    }
            }
        }

#if USE_SharpZipLib
            public static JsonNode LoadFromCompressedStream(System.IO.Stream aData)
{
            var zin = new ICSharpCode.SharpZipLib.BZip2.BZip2InputStream(aData);
return LoadFromStream(zin);
}
public static JsonNode LoadFromCompressedFile(string aFileName)
{
#if USE_FileIO
            using(var F = System.IO.File.OpenRead(aFileName))
{
                return LoadFromCompressedStream(F);
}
#else
throw new Exception("Can't use File IO stuff in webplayer");
#endif
}
public static JsonNode LoadFromCompressedBase64(string aBase64)
{
            var tmp = System.Convert.FromBase64String(aBase64);
var stream = new System.IO.MemoryStream(tmp);
stream.Position = 0;
return LoadFromCompressedStream(stream);
}
#else
        public static JsonNode LoadFromCompressedFile(string aFileName)
        {
            throw new Exception("Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
        }
        public static JsonNode LoadFromCompressedStream(System.IO.Stream aData)
        {
            throw new Exception("Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
        }
        public static JsonNode LoadFromCompressedBase64(string aBase64)
        {
            throw new Exception("Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
        }
#endif

        public static JsonNode LoadFromStream(System.IO.Stream aData)
        {
            using (var R = new System.IO.BinaryReader(aData))
            {
                return Deserialize(R);
            }
        }
        public static JsonNode LoadFromFile(string aFileName)
        {
#if USE_FileIO
            using(var F = System.IO.File.OpenRead(aFileName))
{
                return LoadFromStream(F);
}
#else
            throw new Exception("Can't use File IO stuff in webplayer");
#endif
        }
        public static JsonNode LoadFromBase64(string aBase64)
        {
            var tmp = System.Convert.FromBase64String(aBase64);
            var stream = new System.IO.MemoryStream(tmp);
            stream.Position = 0;
            return LoadFromStream(stream);
        }
    } // End of JsonNode

    public class JsonArray : JsonNode, IEnumerable
    {
        private List<JsonNode> m_List = new List<JsonNode>();

        public override string Value
        {
            get { return ToString(); }
            set { /* JsonArray 不支持直接设置 Value */ }
        }

        public override JsonNode this[int aIndex]
        {
            get
            {
                if (aIndex < 0 || aIndex >= m_List.Count)
                    return new JsonLazyCreator(this);
                return m_List[aIndex];
            }
            set
            {
                if (aIndex < 0 || aIndex >= m_List.Count)
                    m_List.Add(value);
                else
                    m_List[aIndex] = value;
            }
        }
        public override JsonNode this[string aKey]
        {
            get { return new JsonLazyCreator(this); }
            set { m_List.Add(value); }
        }
        public override int Count
        {
            get { return m_List.Count; }
        }
        public override void Add(string aKey, JsonNode aItem)
        {
            m_List.Add(aItem);
        }
        public override JsonNode Remove(int aIndex)
        {
            if (aIndex < 0 || aIndex >= m_List.Count)
                return null;
            JsonNode tmp = m_List[aIndex];
            m_List.RemoveAt(aIndex);
            return tmp;
        }
        public override JsonNode Remove(JsonNode aNode)
        {
            m_List.Remove(aNode);
            return aNode;
        }
        public override IEnumerable<JsonNode> Childs
        {
            get
            {
                foreach (JsonNode N in m_List)
                    yield return N;
            }
        }
        public IEnumerator GetEnumerator()
        {
            foreach (JsonNode N in m_List)
                yield return N;
        }
        public override string ToString()
        {
            var sb = new System.Text.StringBuilder("[ ");
            bool isFirst = true;
            foreach (JsonNode N in m_List)
            {
                if (!isFirst)
                    sb.Append(", ");
                isFirst = false;
                sb.Append(N.ToString());
            }
            sb.Append(" ]");
            return sb.ToString();
        }
        public override string ToString(string aPrefix)
        {
            var sb = new System.Text.StringBuilder("[ ");
            bool isFirst = true;
            foreach (JsonNode N in m_List)
            {
                if (!isFirst)
                    sb.Append(", ");
                isFirst = false;
                sb.Append("\n").Append(aPrefix).Append("   ");
                sb.Append(N.ToString(aPrefix + "   "));
            }
            sb.Append("\n").Append(aPrefix).Append("]");
            return sb.ToString();
        }

        public override string ToPrettyStringInternal(int level, string indent)
        {
            if (m_List.Count == 0)
                return "[]";

            var currentIndent = new string(' ', level * indent.Length);
            var nextIndent = new string(' ', (level + 1) * indent.Length);
            var result = new System.Text.StringBuilder("[\n");

            bool first = true;
            foreach (JsonNode node in m_List)
            {
                if (!first)
                    result.Append(",\n");
                first = false;

                result.Append(nextIndent);
                result.Append(node.ToPrettyStringInternal(level + 1, indent));
            }

            result.Append("\n");
            result.Append(currentIndent);
            result.Append("]");
            return result.ToString();
        }

        public override string ToYamlStringInternal(int level, string indent, bool isArrayItem)
        {
            if (m_List.Count == 0)
                return "[]";

            var result = new System.Text.StringBuilder();
            var currentIndent = new string(' ', level * indent.Length);

            foreach (JsonNode node in m_List)
            {
                if (result.Length > 0)
                    result.Append("\n");

                result.Append(currentIndent);
                result.Append("- ");

                // 如果元素是对象或数组，需要特殊处理
                if (node is JsonClass jsonClass)
                {
                    // 对象：第一个键值对跟在 "- " 后面，后续键值对需要对齐缩进
                    bool firstProperty = true;
                    var itemIndent = currentIndent + indent;

                    foreach (var kvp in jsonClass.AsEnumerable())
                    {
                        if (!firstProperty)
                        {
                            result.Append("\n");
                            result.Append(itemIndent);
                        }

                        // YAML key
                        string yamlKey = kvp.Key;
                        if (yamlKey.Contains(':') || yamlKey.Contains('#') || yamlKey.Contains(' ') ||
                            yamlKey.Contains('[') || yamlKey.Contains(']') || yamlKey.Contains('{') || yamlKey.Contains('}'))
                        {
                            yamlKey = "\"" + yamlKey.Replace("\"", "\\\"") + "\"";
                        }

                        result.Append(yamlKey);
                        result.Append(": ");

                        // YAML value
                        if (kvp.Value is JsonClass || kvp.Value is JsonArray)
                        {
                            // 复杂类型：换行并缩进
                            result.Append("\n");
                            result.Append(kvp.Value.ToYamlStringInternal(level + 2, indent, false));
                        }
                        else
                        {
                            // 简单类型：直接跟在冒号后面
                            result.Append(kvp.Value.ToYamlStringInternal(level + 2, indent, false));
                        }

                        firstProperty = false;
                    }
                }
                else if (node is JsonArray)
                {
                    // 嵌套数组
                    result.Append("\n");
                    result.Append(node.ToYamlStringInternal(level + 1, indent, false));
                }
                else
                {
                    // 简单类型
                    result.Append(node.ToYamlStringInternal(level + 1, indent, true));
                }
            }

            return result.ToString();
        }

        public override void Serialize(System.IO.BinaryWriter aWriter)
        {
            aWriter.Write((byte)JsonBinaryTag.Array);
            aWriter.Write(m_List.Count);
            for (int i = 0; i < m_List.Count; i++)
            {
                m_List[i].Serialize(aWriter);
            }
        }

        /// <summary>
        /// 转换为 List&lt;JsonNode&gt;
        /// </summary>
        public List<JsonNode> ToList()
        {
            return new List<JsonNode>(m_List);
        }

        /// <summary>
        /// 转换为字符串列表
        /// </summary>
        public List<string> ToStringList()
        {
            var count = m_List.Count;
            var list = new List<string>(count);
            for (int i = 0; i < count; i++)
            {
                list.Add(m_List[i].Value);
            }
            return list;
        }

    } // End of JsonArray

    public class JsonClass : JsonNode, IEnumerable
    {
        private Dictionary<string, JsonNode> m_Dict = new Dictionary<string, JsonNode>();

        public override string Value
        {
            get { return ToString(); }
            set { /* JsonClass 不支持直接设置 Value */ }
        }

        public override JsonNode this[string aKey]
        {
            get
            {
                if (m_Dict.ContainsKey(aKey))
                    return m_Dict[aKey];
                else
                    return new JsonLazyCreator(this, aKey);
            }
            set
            {
                if (m_Dict.ContainsKey(aKey))
                    m_Dict[aKey] = value;
                else
                    m_Dict.Add(aKey, value);
            }
        }
        public override JsonNode this[int aIndex]
        {
            get
            {
                if (aIndex < 0 || aIndex >= m_Dict.Count)
                    return null;
                return m_Dict.ElementAt(aIndex).Value;
            }
            set
            {
                if (aIndex < 0 || aIndex >= m_Dict.Count)
                    return;
                string key = m_Dict.ElementAt(aIndex).Key;
                m_Dict[key] = value;
            }
        }
        public override int Count
        {
            get { return m_Dict.Count; }
        }


        public override void Add(string aKey, JsonNode aItem)
        {
            if (!string.IsNullOrEmpty(aKey))
            {
                if (m_Dict.ContainsKey(aKey))
                    m_Dict[aKey] = aItem;
                else
                    m_Dict.Add(aKey, aItem);
            }
            else
                m_Dict.Add(Guid.NewGuid().ToString(), aItem);
        }

        public override JsonNode Remove(string aKey)
        {
            if (!m_Dict.ContainsKey(aKey))
                return null;
            JsonNode tmp = m_Dict[aKey];
            m_Dict.Remove(aKey);
            return tmp;
        }
        public override JsonNode Remove(int aIndex)
        {
            if (aIndex < 0 || aIndex >= m_Dict.Count)
                return null;
            var item = m_Dict.ElementAt(aIndex);
            m_Dict.Remove(item.Key);
            return item.Value;
        }
        public override JsonNode Remove(JsonNode aNode)
        {
            if (aNode == null)
                return null;

            foreach (var kvp in m_Dict)
            {
                if (kvp.Value == aNode)
                {
                    m_Dict.Remove(kvp.Key);
                    return aNode;
                }
            }

            return null;
        }

        public override IEnumerable<JsonNode> Childs
        {
            get
            {
                foreach (KeyValuePair<string, JsonNode> N in m_Dict)
                    yield return N.Value;
            }
        }

        public IEnumerator GetEnumerator()
        {
            foreach (KeyValuePair<string, JsonNode> N in m_Dict)
                yield return N;
        }
        public override string ToString()
        {
            var sb = new System.Text.StringBuilder("{");
            bool isFirst = true;
            foreach (KeyValuePair<string, JsonNode> N in m_Dict)
            {
                if (!isFirst)
                    sb.Append(", ");
                isFirst = false;
                sb.Append("\"").Append(Escape(N.Key)).Append("\":").Append(N.Value.ToString());
            }
            sb.Append("}");
            return sb.ToString();
        }
        public override string ToString(string aPrefix)
        {
            var sb = new System.Text.StringBuilder("{ ");
            bool isFirst = true;
            foreach (KeyValuePair<string, JsonNode> N in m_Dict)
            {
                if (!isFirst)
                    sb.Append(", ");
                isFirst = false;
                sb.Append("\n").Append(aPrefix).Append("   ");
                sb.Append("\"").Append(Escape(N.Key)).Append("\" : ").Append(N.Value.ToString(aPrefix + "   "));
            }
            sb.Append("\n").Append(aPrefix).Append("}");
            return sb.ToString();
        }

        public override string ToPrettyStringInternal(int level, string indent)
        {
            if (m_Dict.Count == 0)
                return "{}";

            var currentIndent = new string(' ', level * indent.Length);
            var nextIndent = new string(' ', (level + 1) * indent.Length);
            var result = new System.Text.StringBuilder("{\n");

            bool first = true;
            foreach (KeyValuePair<string, JsonNode> kvp in m_Dict)
            {
                if (!first)
                    result.Append(",\n");
                first = false;

                result.Append(nextIndent);
                result.Append("\"");
                result.Append(Escape(kvp.Key));
                result.Append("\": ");
                result.Append(kvp.Value.ToPrettyStringInternal(level + 1, indent));
            }

            result.Append("\n");
            result.Append(currentIndent);
            result.Append("}");
            return result.ToString();
        }

        public override string ToYamlStringInternal(int level, string indent, bool isArrayItem)
        {
            if (m_Dict.Count == 0)
                return "{}";

            var result = new System.Text.StringBuilder();
            var currentIndent = new string(' ', level * indent.Length);
            var nextIndent = new string(' ', (level + 1) * indent.Length);

            bool first = true;
            foreach (KeyValuePair<string, JsonNode> kvp in m_Dict)
            {
                if (!first)
                    result.Append("\n");
                first = false;

                result.Append(currentIndent);

                // YAML key (需要检查是否需要引号)
                string yamlKey = kvp.Key;
                if (yamlKey.Contains(':') || yamlKey.Contains('#') || yamlKey.Contains(' ') ||
                    yamlKey.Contains('[') || yamlKey.Contains(']') || yamlKey.Contains('{') || yamlKey.Contains('}'))
                {
                    yamlKey = "\"" + yamlKey.Replace("\"", "\\\"") + "\"";
                }

                result.Append(yamlKey);
                result.Append(": ");

                // YAML value
                if (kvp.Value is JsonClass || kvp.Value is JsonArray)
                {
                    // 复杂类型：换行并缩进
                    result.Append("\n");
                    result.Append(kvp.Value.ToYamlStringInternal(level + 1, indent, false));
                }
                else
                {
                    // 简单类型：直接跟在冒号后面
                    result.Append(kvp.Value.ToYamlStringInternal(level + 1, indent, false));
                }
            }

            return result.ToString();
        }

        public override void Serialize(System.IO.BinaryWriter aWriter)
        {
            aWriter.Write((byte)JsonBinaryTag.Class);
            aWriter.Write(m_Dict.Count);
            foreach (string K in m_Dict.Keys)
            {
                aWriter.Write(K);
                m_Dict[K].Serialize(aWriter);
            }
        }

        /// <summary>
        /// 创建 JsonClass 的深度副本
        /// </summary>
        public JsonClass Clone()
        {
            var clone = new JsonClass();
            foreach (var kvp in m_Dict)
            {
                if (kvp.Value is JsonClass jsonClass)
                    clone.Add(kvp.Key, jsonClass.Clone());
                else if (kvp.Value is JsonArray jsonArray)
                {
                    var newArray = new JsonArray();
                    foreach (var item in jsonArray.Childs)
                    {
                        if (item is JsonClass itemClass)
                            newArray.Add(itemClass.Clone());
                        else if (item is JsonArray itemArray)
                            newArray.Add(Json.Parse(itemArray.ToString()) as JsonArray); // 嵌套数组暂时使用序列化方式
                        else
                            newArray.Add(new JsonData(item.Value));
                    }
                    clone.Add(kvp.Key, newArray);
                }
                else
                    clone.Add(kvp.Key, new JsonData(kvp.Value.Value));
            }
            return clone;
        }

        /// <summary>
        /// 获取所有键（兼容 JSONClass）
        /// </summary>
        public IEnumerable<string> GetKeys()
        {
            foreach (string key in m_Dict.Keys)
            {
                yield return key;
            }
        }

        /// <summary>
        /// 获取所有键（属性，便于访问）
        /// </summary>
        public IEnumerable<string> Keys
        {
            get { return m_Dict.Keys; }
        }

        /// <summary>
        /// 检查是否包含指定的键
        /// </summary>
        public bool ContainsKey(string key)
        {
            if (m_Dict.ContainsKey(key))
            {
                var value = m_Dict[key];
                return value != null && !value.IsNull();
            }
            return false;
        }

        /// <summary>
        /// 返回可枚举的键值对集合，用于foreach循环
        /// </summary>
        public IEnumerable<KeyValuePair<string, JsonNode>> AsEnumerable()
        {
            foreach (var kvp in m_Dict)
            {
                yield return new KeyValuePair<string, JsonNode>(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// 兼容 Newtonsoft.Json 的 Properties() 方法，返回可枚举的键值对集合
        /// </summary>
        public IEnumerable<KeyValuePair<string, JsonNode>> Properties()
        {
            return AsEnumerable();
        }

    } // End of JsonClass

    public class JsonData : JsonNode
    {
        private string m_Data;
        private JsonNodeType? m_CachedType = null;

        public override string Value
        {
            get { return m_Data; }
            set
            {
                m_Data = value;
                m_CachedType = null; // 值变化时清除缓存的类型
            }
        }
        public JsonData(string aData)
        {
            m_Data = aData;
        }
        public JsonData(float aData)
        {
            AsFloat = aData;
        }
        public JsonData(double aData)
        {
            AsDouble = aData;
        }
        public JsonData(bool aData)
        {
            AsBool = aData;
        }
        public JsonData(int aData)
        {
            AsInt = aData;
        }
        public JsonData(long aData)
        {
            m_Data = aData.ToString();
        }
        public JsonData(decimal aData)
        {
            m_Data = aData.ToString();
        }
        public JsonData(uint aData)
        {
            m_Data = aData.ToString();
        }
        public JsonData(short aData)
        {
            AsInt = aData;
        }
        public JsonData(ushort aData)
        {
            m_Data = aData.ToString();
        }
        public JsonData(byte aData)
        {
            AsInt = aData;
        }
        public JsonData(sbyte aData)
        {
            AsInt = aData;
        }
        public JsonData(char aData)
        {
            m_Data = aData.ToString();
        }

        public override string ToString()
        {
            JsonNodeType nodeType = GetCachedNodeType();
            if (nodeType == JsonNodeType.Boolean)
            {
                return m_Data.ToLower(); // 布尔值不需要引号
            }
            else if (nodeType == JsonNodeType.Integer || nodeType == JsonNodeType.Float)
            {
                return m_Data; // 数字不需要引号
            }
            return "\"" + Escape(m_Data) + "\"";
        }

        public override string ToString(string aPrefix)
        {
            JsonNodeType nodeType = GetCachedNodeType();
            if (nodeType == JsonNodeType.Boolean)
            {
                return m_Data.ToLower(); // 布尔值不需要引号
            }
            else if (nodeType == JsonNodeType.Integer || nodeType == JsonNodeType.Float)
            {
                return m_Data; // 数字不需要引号
            }
            return "\"" + Escape(m_Data) + "\"";
        }

        public override string ToPrettyStringInternal(int level, string indent)
        {
            JsonNodeType nodeType = GetCachedNodeType();
            if (nodeType == JsonNodeType.Boolean)
            {
                return m_Data.ToLower(); // 布尔值不需要引号
            }
            else if (nodeType == JsonNodeType.Integer || nodeType == JsonNodeType.Float)
            {
                return m_Data; // 数字不需要引号
            }
            return "\"" + Escape(m_Data) + "\"";
        }

        public override string ToYamlStringInternal(int level, string indent, bool isArrayItem)
        {
            JsonNodeType nodeType = GetCachedNodeType();

            // null 值
            if (string.IsNullOrEmpty(m_Data) || m_Data == "null")
                return "null";

            // 布尔值
            if (nodeType == JsonNodeType.Boolean)
                return m_Data.ToLower();

            // 数字（整数或浮点数）
            if (nodeType == JsonNodeType.Integer || nodeType == JsonNodeType.Float)
                return m_Data;

            // 字符串：使用 EscapeYamlString 处理
            return EscapeYamlString(m_Data);
        }

        // 隐式转换操作符，支持从基本类型转换为 JsonData
        public static implicit operator JsonData(int aInt)
        {
            return new JsonData(aInt);
        }
        public static implicit operator JsonData(float aFloat)
        {
            return new JsonData(aFloat);
        }
        public static implicit operator JsonData(double aDouble)
        {
            return new JsonData(aDouble);
        }
        public static implicit operator JsonData(bool aBool)
        {
            return new JsonData(aBool);
        }
        public static implicit operator JsonData(long aLong)
        {
            return new JsonData(aLong);
        }
        public static implicit operator JsonData(decimal aDecimal)
        {
            return new JsonData(aDecimal);
        }
        public static implicit operator JsonData(uint aUInt)
        {
            return new JsonData(aUInt);
        }
        public static implicit operator JsonData(short aShort)
        {
            return new JsonData(aShort);
        }
        public static implicit operator JsonData(ushort aUShort)
        {
            return new JsonData(aUShort);
        }
        public static implicit operator JsonData(byte aByte)
        {
            return new JsonData(aByte);
        }
        public static implicit operator JsonData(sbyte aSByte)
        {
            return new JsonData(aSByte);
        }
        public static implicit operator JsonData(char aChar)
        {
            return new JsonData(aChar);
        }

        private JsonNodeType GetCachedNodeType()
        {
            if (m_CachedType.HasValue)
                return m_CachedType.Value;

            if (string.IsNullOrEmpty(m_Data) || m_Data == "null")
                m_CachedType = JsonNodeType.Null;
            else if (bool.TryParse(m_Data, out _))
                m_CachedType = JsonNodeType.Boolean;
            else if (int.TryParse(m_Data, out _))
                m_CachedType = JsonNodeType.Integer;
            else if (float.TryParse(m_Data, out _))
                m_CachedType = JsonNodeType.Float;
            else
                m_CachedType = JsonNodeType.String;

            return m_CachedType.Value;
        }

        public override void Serialize(System.IO.BinaryWriter aWriter)
        {
            var tmp = new JsonData("");

            tmp.AsInt = AsInt;
            if (tmp.m_Data == this.m_Data)
            {
                aWriter.Write((byte)JsonBinaryTag.IntValue);
                aWriter.Write(AsInt);
                return;
            }
            tmp.AsFloat = AsFloat;
            if (tmp.m_Data == this.m_Data)
            {
                aWriter.Write((byte)JsonBinaryTag.FloatValue);
                aWriter.Write(AsFloat);
                return;
            }
            tmp.AsDouble = AsDouble;
            if (tmp.m_Data == this.m_Data)
            {
                aWriter.Write((byte)JsonBinaryTag.DoubleValue);
                aWriter.Write(AsDouble);
                return;
            }

            tmp.AsBool = AsBool;
            if (tmp.m_Data == this.m_Data)
            {
                aWriter.Write((byte)JsonBinaryTag.BoolValue);
                aWriter.Write(AsBool);
                return;
            }
            aWriter.Write((byte)JsonBinaryTag.Value);
            aWriter.Write(m_Data);
        }
    } // End of JsonData

    internal class JsonLazyCreator : JsonNode
    {
        private JsonNode m_Node = null;
        private string m_Key = null;

        public JsonLazyCreator(JsonNode aNode)
        {
            m_Node = aNode;
            m_Key = null;
        }
        public JsonLazyCreator(JsonNode aNode, string aKey)
        {
            m_Node = aNode;
            m_Key = aKey;
        }

        private void Set(JsonNode aVal)
        {
            if (m_Key == null)
            {
                m_Node.Add(aVal);
            }
            else
            {
                m_Node.Add(m_Key, aVal);
            }
            m_Node = null; // Be GC friendly.
        }

        public override JsonNode this[int aIndex]
        {
            get
            {
                return new JsonLazyCreator(this);
            }
            set
            {
                var tmp = new JsonArray();
                tmp.Add(value);
                Set(tmp);
            }
        }

        public override JsonNode this[string aKey]
        {
            get
            {
                return new JsonLazyCreator(this, aKey);
            }
            set
            {
                var tmp = new JsonClass();
                tmp.Add(aKey, value);
                Set(tmp);
            }
        }
        public override void Add(JsonNode aItem)
        {
            var tmp = new JsonArray();
            tmp.Add(aItem);
            Set(tmp);
        }
        public override void Add(string aKey, JsonNode aItem)
        {
            var tmp = new JsonClass();
            tmp.Add(aKey, aItem);
            Set(tmp);
        }
        public static bool operator ==(JsonLazyCreator a, object b)
        {
            if (b == null)
                return true;
            return System.Object.ReferenceEquals(a, b);
        }

        public static bool operator !=(JsonLazyCreator a, object b)
        {
            return !(a == b);
        }
        public override bool Equals(object obj)
        {
            if (obj == null)
                return true;
            return System.Object.ReferenceEquals(this, obj);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return "";
        }
        public override string ToString(string aPrefix)
        {
            return "";
        }

        public override int AsInt
        {
            get
            {
                JsonData tmp = new JsonData(0);
                Set(tmp);
                return 0;
            }
            set
            {
                JsonData tmp = new JsonData(value);
                Set(tmp);
            }
        }
        public override float AsFloat
        {
            get
            {
                JsonData tmp = new JsonData(0.0f);
                Set(tmp);
                return 0.0f;
            }
            set
            {
                JsonData tmp = new JsonData(value);
                Set(tmp);
            }
        }
        public override double AsDouble
        {
            get
            {
                JsonData tmp = new JsonData(0.0);
                Set(tmp);
                return 0.0;
            }
            set
            {
                JsonData tmp = new JsonData(value);
                Set(tmp);
            }
        }
        public override bool AsBool
        {
            get
            {
                JsonData tmp = new JsonData(false);
                Set(tmp);
                return false;
            }
            set
            {
                JsonData tmp = new JsonData(value);
                Set(tmp);
            }
        }
        public override JsonArray AsArray
        {
            get
            {
                JsonArray tmp = new JsonArray();
                Set(tmp);
                return tmp;
            }
        }
        public override JsonClass AsObject
        {
            get
            {
                JsonClass tmp = new JsonClass();
                Set(tmp);
                return tmp;
            }
        }
    } // End of JsonLazyCreator

    /// <summary>
    /// JsonNode 类型枚举（类似 Newtonsoft.Json 的 JSONNodeType）
    /// </summary>
    public enum JsonNodeType
    {
        Null,
        Object,
        Array,
        String,
        Integer,
        Float,
        Boolean
    }

    public static class Json
    {
        public static JsonNode Parse(string aJSON)
        {
            return JsonNode.Parse(aJSON);
        }

        /// <summary>
        /// 从对象创建 JSONNode（类似 Json.FromObject）
        /// </summary>
        public static JsonNode FromObject(object obj)
        {
            if (obj == null)
                return new JsonData("null");

            if (obj is JsonNode jsonNode)
                return jsonNode;

            if (obj is string str)
                return new JsonData(str);

            if (obj is int intVal)
                return new JsonData(intVal);
            if (obj is long longVal)
                return new JsonData(longVal);
            if (obj is short shortVal)
                return new JsonData(shortVal);
            if (obj is byte byteVal)
                return new JsonData(byteVal);
            if (obj is uint uintVal)
                return new JsonData(uintVal);
            if (obj is ushort ushortVal)
                return new JsonData(ushortVal);
            if (obj is sbyte sbyteVal)
                return new JsonData(sbyteVal);
            if (obj is char charVal)
                return new JsonData(charVal);

            if (obj is float floatVal)
                return new JsonData(floatVal);
            if (obj is double doubleVal)
                return new JsonData(doubleVal);
            if (obj is decimal decimalVal)
                return new JsonData(decimalVal);

            if (obj is bool b)
                return new JsonData(b ? "true" : "false");

#if UNITY_EDITOR || UNITY_STANDALONE
            if (obj is UnityEngine.Vector2 v2)
            {
                var arr = new JsonArray();
                arr.Add(new JsonData(v2.x.ToString()));
                arr.Add(new JsonData(v2.y.ToString()));
                return arr;
            }

            if (obj is UnityEngine.Vector3 v3)
            {
                var arr = new JsonArray();
                arr.Add(new JsonData(v3.x.ToString()));
                arr.Add(new JsonData(v3.y.ToString()));
                arr.Add(new JsonData(v3.z.ToString()));
                return arr;
            }

            if (obj is UnityEngine.Vector4 v4)
            {
                var arr = new JsonArray();
                arr.Add(new JsonData(v4.x.ToString()));
                arr.Add(new JsonData(v4.y.ToString()));
                arr.Add(new JsonData(v4.z.ToString()));
                arr.Add(new JsonData(v4.w.ToString()));
                return arr;
            }

            if (obj is UnityEngine.Quaternion q)
            {
                var arr = new JsonArray();
                arr.Add(new JsonData(q.x.ToString()));
                arr.Add(new JsonData(q.y.ToString()));
                arr.Add(new JsonData(q.z.ToString()));
                arr.Add(new JsonData(q.w.ToString()));
                return arr;
            }

            if (obj is UnityEngine.Color c)
            {
                var colorObj = new JsonClass();
                colorObj.Add("r", new JsonData(c.r.ToString()));
                colorObj.Add("g", new JsonData(c.g.ToString()));
                colorObj.Add("b", new JsonData(c.b.ToString()));
                colorObj.Add("a", new JsonData(c.a.ToString()));
                return colorObj;
            }
#endif

            // 先检查 IDictionary，因为 Dictionary 同时实现了 IDictionary 和 IEnumerable
            if (obj is System.Collections.IDictionary dict)
            {
                var jsonObj = new JsonClass();
                foreach (System.Collections.DictionaryEntry entry in dict)
                {
                    jsonObj.Add(entry.Key.ToString(), FromObject(entry.Value));
                }
                return jsonObj;
            }

            if (obj is System.Collections.IEnumerable enumerable && !(obj is string))
            {
                var arr = new JsonArray();
                foreach (var item in enumerable)
                {
                    arr.Add(FromObject(item));
                }
                return arr;
            }

            // 特殊处理匿名类型和具有属性的对象
            if (obj.GetType().IsClass && !(obj is string))
            {
                var type = obj.GetType();

                var jsonObj = new JsonClass();

                // 处理字段
                var fields = type.GetFields();
                foreach (var field in fields)
                {
                    try
                    {
                        var value = field.GetValue(obj);
                        jsonObj.Add(field.Name, FromObject(value));
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogWarning($"Json.FromObject: 无法访问字段 {field.Name}: {ex.Message}");
                        jsonObj.Add(field.Name, new JsonData("null"));
                    }
                }

                // 处理属性
                var properties = type.GetProperties();
                foreach (var prop in properties)
                {
                    // 只处理可读且不是索引器的属性
                    if (!prop.CanRead || prop.GetIndexParameters().Length > 0) continue;
                    try
                    {
                        var value = prop.GetValue(obj, null);
                        jsonObj.Add(prop.Name, FromObject(value));
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogWarning($"Json.FromObject: 无法访问属性 {prop.Name}: {ex.Message}");
                        jsonObj.Add(prop.Name, new JsonData("null"));
                    }
                }

                if (jsonObj.Count > 0)
                {
                    return jsonObj;
                }
            }

            // 默认转换为字符串
            return new JsonData(obj.ToString());
        }
    }
}