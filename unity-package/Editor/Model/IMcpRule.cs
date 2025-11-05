using UnityEngine;

namespace UniMcp.Models
{
    /// <summary>
    /// MCP规则接口标记
    /// 所有需要通过 MCP Rules 窗口创建的 ScriptableObject 都应该实现此接口
    /// </summary>
    public interface IMcpRule
    {
        // 标记接口，用于识别可以通过 MCP Rules 窗口创建的规则类型
    }
}

