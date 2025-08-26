using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using System.Text;
using Newtonsoft.Json.Linq;

namespace UnityMcp.Tools
{
    public class StateTree
    {
        public string key;                         // ��ǰ�����
        public Dictionary<object, StateTree> select = new();
        public Func<JObject, object> func;     // Ҷ�Ӻ���
        public const string Default = "*";          // ͨ���ʶ
        public string ErrorMessage;//ִ�д�����Ϣ

        /* ��ʽת����Action �� Ҷ�ӽڵ� */
        public static implicit operator StateTree(Func<JObject, object> a) => new() { func = a };

        /* ���У�����Ψһ·����JObject �����ģ� */
        public object Run(JObject ctx)
        {
            var cur = this;
            while (cur.func == null)
            {
                object keyToLookup = Default;

                if (!string.IsNullOrEmpty(cur.key) && ctx != null && ctx.TryGetValue(cur.key, out JToken token))
                {
                    keyToLookup = ConvertTokenToKey(token);
                }

                if (!cur.select.TryGetValue(keyToLookup, out var next) &&
                    !cur.select.TryGetValue(Default, out next))
                {
                    var supportedKeys = cur.select.Keys
                        .Where(k => k?.ToString() != Default)
                        .Select(k => k?.ToString() ?? "null")
                        .ToList();

                    string supportedKeysList = supportedKeys.Count > 0
                        ? string.Join(", ", supportedKeys)
                        : "none";

                    ErrorMessage = $"Invalid value '{keyToLookup}' for key '{cur.key}'. Supported values: [{supportedKeysList}]";
                    return null;
                }
                cur = next;
            }
            return cur.func?.Invoke(ctx);
        }

        private static object ConvertTokenToKey(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
                return Default;

            if (token.Type == JTokenType.Integer)
            {
                long longVal = token.Value<long>();
                if (longVal <= int.MaxValue && longVal >= int.MinValue)
                {
                    return (int)longVal;
                }
                return longVal;
            }

            if (token.Type == JTokenType.Float)
            {
                return token.Value<double>();
            }

            if (token.Type == JTokenType.Boolean)
            {
                return token.Value<bool>();
            }

            if (token.Type == JTokenType.String)
            {
                return token.Value<string>();
            }

            if (token is JValue jv && jv.Value != null)
            {
                return jv.Value;
            }

            return token.ToString();
        }

        /* ������ӡ��Unicode ���ߣ� */
        public void Print(StringBuilder sb, string indent = "", bool last = true, string parentEdgeLabel = null)
        {
            // ���ڵ㣺��ӡ����
            if (string.IsNullOrEmpty(indent))
            {
                sb.AppendLine($"{indent}StateTree");
            }

            // ����ǰ�ڵ��� key����ӡһ�νڵ� key�������븸�߱�ǩ�ظ���
            string edgesIndent = indent;
            if (!string.IsNullOrEmpty(key) && key != parentEdgeLabel)
            {
                sb.AppendLine($"{indent}���� {key}:");
                edgesIndent = indent + "   ";
            }

            // ö�ٲ���ӡ��ǰ�ڵ�ıߣ�entry.Key Ϊ�߱�ǩ��
            var entries = select.ToList();
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                bool isLastChild = i == entries.Count - 1;
                string connector = isLastChild ? "����" : "����";
                string label = entry.Key?.ToString() == Default ? "*" : entry.Key?.ToString();

                if (entry.Value.func != null)
                {
                    string actionName = entry.Value.func.Method?.Name ?? "Anonymous";
                    sb.AppendLine($"{edgesIndent}{connector} {label} �� {actionName}");
                }
                else
                {
                    // ��ӡ�߱�ǩ
                    sb.AppendLine($"{edgesIndent}{connector} {label}");
                    // �ݹ鵽�ӽڵ㣻����ӽڵ�� key ��߱�ǩ��ͬ�������Ӳ㼶��ӡ�� key
                    string nextIndent = edgesIndent + (isLastChild ? "   " : "��  ");
                    entry.Value.Print(sb, nextIndent, isLastChild, label);
                }
            }
        }
    }
}