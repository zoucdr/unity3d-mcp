using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using System.Text;

namespace UnityMcp.Tools
{
    public class StateTree
    {
        public string key;                         // ��ǰ�����
        public Dictionary<object, StateTree> select = new();
        public Action Act;                         // Ҷ�Ӻ���
        public const string Default = "*";          // ͨ���ʶ

        /* ��ʽת����Action �� Ҷ�ӽڵ� */
        public static implicit operator StateTree(Action a) => new() { Act = a };

        /* ���У�����Ψһ·�� */
        public void Run(IReadOnlyDictionary<string, object> ctx)
        {
            var cur = this;
            while (cur.Act == null)
            {
                if (!cur.select.TryGetValue(ctx.TryGetValue(cur.key!, out var v) ? v! : Default, out var next) &&
                    !cur.select.TryGetValue(Default, out next))
                    break;
                cur = next;
            }
            cur.Act?.Invoke();
        }

        /* ������ӡ��Unicode ���ߣ� */
        public void Print(StringBuilder sb, string indent = "", bool last = true)
        {
            // ��ӡ��ǰ�ڵ�
            if (!string.IsNullOrEmpty(key))
            {
                sb.AppendLine($"{indent}���� {key}");
            }
            else
            {
                sb.AppendLine($"{indent}StateTree");
            }

            // ��ȡ�����ӽڵ�
            var entries = select.ToList();

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                bool isLastChild = i == entries.Count - 1;

                // �����µ�����
                string newIndent = indent + (last ? "   " : "��  ");

                // ��ӡ�ӽڵ�
                if (entry.Value.Act != null)
                {
                    // Ҷ�ӽڵ㣨��Action��
                    string actionName = entry.Value.Act.Method?.Name ?? "Anonymous";
                    string connector = isLastChild ? "����" : "����";

                    if (entry.Key.ToString() == Default)
                    {
                        sb.AppendLine($"{newIndent}{connector} * �� {actionName}");
                    }
                    else
                    {
                        sb.AppendLine($"{newIndent}{connector} {entry.Key} �� {actionName}");
                    }
                }
                else
                {
                    // ��Ҷ�ӽڵ㣨���ӽڵ㣩
                    string connector = isLastChild ? "����" : "����";

                    if (entry.Key.ToString() == Default)
                    {
                        sb.AppendLine($"{newIndent}{connector} *");
                    }
                    else
                    {
                        sb.AppendLine($"{newIndent}{connector} {entry.Key}");
                    }

                    // �ݹ��ӡ�ӽڵ�
                    entry.Value.Print(sb, newIndent, isLastChild);
                }
            }
        }
    }
}