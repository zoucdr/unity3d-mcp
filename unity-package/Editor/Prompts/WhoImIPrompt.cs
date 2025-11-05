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
        public string Description => "自我介绍提示词";
        public string PromptText => "请介绍一下你自己，包括你的身份、能力和主要功能。";
        public MethodKey[] Keys => new MethodKey[]
        {
            new MethodKey("identity", "身份描述", false)
            .AddExample("我是Unity MCP助手，专门帮助开发者操作Unity引擎。")
            .AddExample("我是一个AI助手，可以协助您进行Unity项目开发和管理。")
            .AddExample("我是Unity开发工具，能够执行各种Unity编辑器操作和脚本管理。"),

            new MethodKey("capabilities", "能力说明", false)
            .AddExample("我可以创建和编辑GameObject、管理场景、处理资源文件。")
            .AddExample("我具备代码生成、项目管理、UI设计等多种开发能力。")
            .AddExample("我能够执行Python脚本、C#代码编译和Unity API调用。")
        };
    }
}