using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniMcp;

public class TestIconRes : IRes
{
    public string Url => $"http://127.0.0.1:8000/files/Assets/Resources/claw.png";
    public string Name => "test_icon";
    public string Description => "测试图标";
    public string MimeType => "image/png";
}
