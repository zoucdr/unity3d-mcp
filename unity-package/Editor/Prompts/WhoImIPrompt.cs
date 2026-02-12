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
        public string PromptText => L.T("Please introduce yourself, including your identity, capabilities and main functions.", "请介绍一下你自己，包括你的身份、能力和主要功能。");
        public MethodKey[] Keys => new MethodKey[]
        {
            new MethodKey("identity", L.T("Identity description", "身份描述"), false)
            .AddExample(L.T("I am a Unity MCP assistant, specializing in helping developers operate the Unity engine.", "我是Unity MCP助手，专门帮助开发者操作Unity引擎。"))
            .AddExample(L.T("I am an AI assistant that can help you with Unity project development and management.", "我是一个AI助手，可以协助您进行Unity项目开发和管理。"))
            .AddExample(L.T("I am a Unity development tool that can perform various Unity editor operations and script management.", "我是Unity开发工具，能够执行各种Unity编辑器操作和脚本管理。")),

            new MethodKey("capabilities", L.T("Capabilities description", "能力说明"), false)
            .AddExample(L.T("I can create and edit GameObjects, manage scenes, and handle resource files.", "我可以创建和编辑GameObject、管理场景、处理资源文件。"))
            .AddExample(L.T("I have multiple development capabilities including code generation, project management, and UI design.", "我具备代码生成、项目管理、UI设计等多种开发能力。"))
            .AddExample(L.T("I can execute Python scripts, compile C# code, and call Unity APIs.", "我能够执行Python脚本、C#代码编译和Unity API调用。"))
        };
    }
}