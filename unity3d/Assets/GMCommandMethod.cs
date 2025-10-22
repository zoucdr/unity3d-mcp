using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mcp.Models;
using Unity.Mcp;

[ToolName("gm_command", "调试工具")]
public class GMCommandMethod : StateMethodBase
{
    /// <summary>
    /// 创建当前方法支持的参数键列表
    /// </summary>
    protected override MethodKey[] CreateKeys()
    {
        return new[]
        {
            new MethodKey("action", "操作类型，默认执行主要功能", true)
        };
    }

    protected override StateTree CreateStateTree()
    {
        return StateTreeBuilder.Create()
            .Key("action")
            .DefaultLeaf(Execute)
            .Build();
    }

    private object Execute(StateTreeContext ctx)
    {
        return Response.Success("Test");
    }
}
