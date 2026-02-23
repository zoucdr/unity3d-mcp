using System;
using System.Collections.Generic;

namespace UniMcp.Runtime
{
    public abstract class McpTool
    {
        public abstract string ToolName { get; }
        public abstract void HandleCommand(JsonNode ctx, System.Action<JsonNode> callback);
    }
}
