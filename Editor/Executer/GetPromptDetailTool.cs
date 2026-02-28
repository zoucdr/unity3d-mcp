using System;
using UniMcp;
using UniMcp.Executer;
using UniMcp.Models;

namespace UniMcp.Prompts
{
    /// <summary>
    /// 系统自带工具：获取提示词详情，供 MCP 客户端查询
    /// </summary>
    public sealed class GetPromptDetailTool : McpTool
    {
        public override string ToolName => "get_prompt_detail";

        public override void HandleCommand(JsonNode ctx, Action<JsonNode> callback)
        {
            try
            {
                string name = ctx?["name"]?.Value;
                if (string.IsNullOrEmpty(name))
                {
                    callback(Response.Error(L.T("Parameter 'name' is required", "参数 'name' 必填")));
                    return;
                }
                var detail = McpService.Instance.GetPromptDetailByName(name);
                if (detail == null)
                {
                    callback(Response.Error(string.Format(L.T("Prompt not found: {0}", "提示词不存在: {0}"), name)));
                    return;
                }
                callback(Response.Success(L.T("Prompt detail retrieved.", "已获取提示词详情。"), detail));
            }
            catch (Exception ex)
            {
                McpLogger.LogError($"[UniMcp] get_prompt failed: {ex.Message}");
                callback(Response.Error(ex.Message));
            }
        }
    }
}
