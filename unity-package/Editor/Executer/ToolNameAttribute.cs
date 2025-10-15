using System;

namespace UnityMcp
{
    /// <summary>
    /// Used to mark the name and group attribute of a tool method class
    /// Priority is higher than automatic conversion by class namesnake_caseNaming
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ToolNameAttribute : Attribute
    {
        /// <summary>
        /// Tool method name
        /// </summary>
        public string ToolName { get; }

        /// <summary>
        /// Group name for tool method
        /// </summary>
        public string GroupName { get; }

        /// <summary>
        /// Initialization ToolNameAttribute
        /// </summary>
        /// <param name="toolName">Tool method name，Normally usedsnake_caseFormat</param>
        /// <param name="groupName">Group name for tool method，Such as"Hierarchy management"、"Resource management"Etc.，Use default group if empty</param>
        public ToolNameAttribute(string toolName, string groupName = null)
        {
            if (string.IsNullOrWhiteSpace(toolName))
                throw new ArgumentException("Tool name cannot be null or empty", nameof(toolName));

            ToolName = toolName;
            GroupName = string.IsNullOrWhiteSpace(groupName) ? "Not grouped" : groupName;
        }
    }
}
