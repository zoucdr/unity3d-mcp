using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniMcp;

public class TestIconRes : IRes
{
    public string Url => $"https://chord-dev.wekoi.cc/api/images/download/1.10.png";
    public string Name => "test_icon";
    public string Description => "测试图标";
    public string MimeType => "image/png";
}
