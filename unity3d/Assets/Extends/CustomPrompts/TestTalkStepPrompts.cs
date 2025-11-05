using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniMcp;

public class TestTalkStepPrompts : IPrompts
{
    public string Name => "test_talk_step";
    public string Description => "测试对话步骤";
    public string PromptText => "请描述一个对话步骤的内容，包括角色的台词和情感。";
    public MethodKey[] Keys => new MethodKey[]
    {
        new     MethodKey("step", "对话步骤", false)
        .AddExample("1. 角色A：你好，我是A。角色B：你好，我是B。")
        .AddExample("1. 角色A：你好，我是A。角色B：你好，我是B。角色C：你好，我是C。")
        .AddExample("1. 角色A：你好，我是A。角色B：你好，我是B。角色C：你好，我是C。角色D：你好，我是D。")
    };
}
