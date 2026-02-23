using System;

namespace UniMcp.Runtime
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ToolNameAttribute : Attribute
    {
        public string ToolName { get; }
        public string GroupName { get; }

        public ToolNameAttribute(string toolName, string groupName = null)
        {
            if (string.IsNullOrWhiteSpace(toolName))
                throw new ArgumentException("Tool name cannot be null or empty", nameof(toolName));

            ToolName = toolName;
            GroupName = string.IsNullOrWhiteSpace(groupName) ? "Runtime Tools" : groupName;
        }
    }
}
