using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniMcp;

public class TestTalkStepPrompts : IPrompts
{
    public string Name => "test_talk_step";
    public string Description => L.T("Test dialogue steps", "测试对话步骤");
    public string PromptText => L.T("Please describe the content of a dialogue step, including the character's lines and emotions.", "请描述一个对话步骤的内容，包括角色的台词和情感。");
    public MethodKey[] Keys => new MethodKey[]
    {
        new MethodKey("step", L.T("Dialogue step", "对话步骤"), false)
        .AddExample(L.T("1. Character A: Hello, I'm A. Character B: Hello, I'm B.", "1. 角色A：你好，我是A。角色B：你好，我是B。"))
        .AddExample(L.T("1. Character A: Hello, I'm A. Character B: Hello, I'm B. Character C: Hello, I'm C.", "1. 角色A：你好，我是A。角色B：你好，我是B。角色C：你好，我是C。"))
        .AddExample(L.T("1. Character A: Hello, I'm A. Character B: Hello, I'm B. Character C: Hello, I'm C. Character D: Hello, I'm D.", "1. 角色A：你好，我是A。角色B：你好，我是B。角色C：你好，我是C。角色D：你好，我是D。"))
    };
}
