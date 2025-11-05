using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UniMcp.Res
{
    public class ProjectVersionRes : IRes
    {
        public string Url => $"file://{System.Environment.CurrentDirectory}/ProjectSettings/ProjectVersion.txt".Replace("\\", "/");
        public string Name => "project_version";
        public string Description => "工程版本信息";
        public string MimeType => "application/text";
    }
}
