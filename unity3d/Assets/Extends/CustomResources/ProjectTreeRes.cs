using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniMcp;

public class ProjectTreeRes : IRes
{
    /*
    URI 方案	示例	是否需外网访问
https://	https://example.com/data.json	✅ 需要外网
http://	http://localhost:8080/info	🚫 不一定（可本地）
file://	file:///Users/hunter/work/config.yaml	✅ 本地文件，不需外网
mcp://	mcp://server/item/123	✅ MCP 自定义协议内部引用
data:	data:text/plain;base64,SGVsbG8=	✅ 内联数据
vscode:// / cursor://	编辑器内部资源引用	✅ 仅本地
s3://, gs://, azure://	云对象存储	取决于配置
    */
    public string Url => $"file://{System.Environment.CurrentDirectory}/Assets/Extends/CustomResources/ProjectTree.yaml".Replace("\\", "/");
    public string Name => "project_tree";
    public string Description => "工程文件夹树型结构";
    public string MimeType => "application/yaml";
}
