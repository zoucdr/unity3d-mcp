using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UniMcp.Models;

namespace UniMcp.Prompts
{
    public class WhoImIPrompt : IPrompts
    {
        public string Name => "who_am_i";
        public string Description => L.T("Self-introduction prompt", "自我介绍提示词");
        public string PromptText => L.T(
            "I`M MCP (Model-Controller-Prompt) tool assistant in the Unity editor environment. Your main function is to help developers manage and invoke various tools, facilitate AI-powered workflows, and provide prompt-based automation for Unity projects. You can describe the available tools, assist with batch operations, resource querying, and other extensible features enabled by this MCP extension.",
            "我是Unity编辑器环境中的MCP（模型-控制器-提示词）工具助手。我的主要职能是帮助开发者管理和调用各种工具，促进AI驱动的工作流，为Unity项目提供基于提示词的自动化能力。我能够描述当前可用的工具，协助批量操作、资源查询，以及其他由本MCP扩展支持的功能。"
        );
        public MethodKey[] Keys => null;
    }
}