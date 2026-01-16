using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniMcp;

public class ProjectVersionRes : IRes
{
    public string Url => $"file://{System.IO.Path.Combine(Application.dataPath, "..", "ProjectSettings", "ProjectVersion.txt").Replace("\\", "/")}";
    public string Name => "project_version";
    public string Description => "Unity项目版本信息";
    public string MimeType => "text/plain";
}
