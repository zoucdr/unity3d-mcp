using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UniMcp.Models;
using UnityEngine;

namespace UniMcp
{
    /// <summary>
    /// 基于 MCP 当前端口提供 /files 路由的文件访问服务。
    /// 访问示例：
    /// - /files
    /// - /files/Assets
    /// - /files/Assets/SomeFolder/file.txt
    /// </summary>
    internal static class ProjectFilesHttpService
    {
        private const string FilesRoutePrefix = "/files";

        public static async Task<bool> TryHandleRequestAsync(System.Net.HttpListenerRequest request, System.Net.HttpListenerResponse response, string requestPath)
        {
            if (string.IsNullOrEmpty(requestPath) ||
                !requestPath.StartsWith(FilesRoutePrefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            try
            {
                // 仅支持 GET/HEAD
                if (!request.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase) &&
                    !request.HttpMethod.Equals("HEAD", StringComparison.OrdinalIgnoreCase))
                {
                    response.StatusCode = 405;
                    response.ContentType = "application/json; charset=utf-8";
                    await WriteJsonAsync(response, Response.Error("Method Not Allowed. /files only supports GET/HEAD."));
                    return true;
                }

                string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
                if (string.IsNullOrEmpty(projectRoot))
                {
                    response.StatusCode = 500;
                    response.ContentType = "application/json; charset=utf-8";
                    await WriteJsonAsync(response, Response.Error("Unable to resolve Unity project root path."));
                    return true;
                }

                // 提取 /files 后的相对路径，并解码 URL
                string rawRelative = requestPath.Length > FilesRoutePrefix.Length
                    ? requestPath.Substring(FilesRoutePrefix.Length).TrimStart('/')
                    : string.Empty;
                string decodedRelative = Uri.UnescapeDataString(rawRelative ?? string.Empty);

                // 统一分隔符，防止 Path.Combine 行为差异
                string safeRelative = decodedRelative.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
                string targetPath = Path.GetFullPath(Path.Combine(projectRoot, safeRelative));

                // 防目录穿越
                string normalizedRoot = Path.GetFullPath(projectRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                string normalizedTarget = targetPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                if (!normalizedTarget.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
                {
                    response.StatusCode = 403;
                    response.ContentType = "application/json; charset=utf-8";
                    await WriteJsonAsync(response, Response.Error("Access denied. Path traversal is not allowed."));
                    return true;
                }

                if (Directory.Exists(targetPath))
                {
                    response.StatusCode = 200;
                    response.ContentType = "application/json; charset=utf-8";

                    var entries = new JsonArray();
                    foreach (var dir in Directory.GetDirectories(targetPath))
                    {
                        var dirInfo = new DirectoryInfo(dir);
                        entries.Add(CreateEntryNode(projectRoot, dirInfo.FullName, true, 0));
                    }

                    foreach (var file in Directory.GetFiles(targetPath))
                    {
                        var fileInfo = new FileInfo(file);
                        entries.Add(CreateEntryNode(projectRoot, fileInfo.FullName, false, fileInfo.Length));
                    }

                    var result = new JsonClass();
                    result.Add("root", new JsonData(projectRoot));
                    result.Add("path", new JsonData(GetRelativePathForApi(projectRoot, targetPath)));
                    result.Add("entries", entries);

                    await WriteJsonAsync(response, result);
                    return true;
                }

                if (File.Exists(targetPath))
                {
                    response.StatusCode = 200;
                    response.ContentType = GetMimeType(targetPath);
                    response.ContentLength64 = new FileInfo(targetPath).Length;

                    // HEAD 仅返回头
                    if (request.HttpMethod.Equals("HEAD", StringComparison.OrdinalIgnoreCase))
                    {
                        response.Close();
                        return true;
                    }

                    using (var fs = new FileStream(targetPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        await fs.CopyToAsync(response.OutputStream);
                    }
                    response.Close();
                    return true;
                }

                response.StatusCode = 404;
                response.ContentType = "application/json; charset=utf-8";
                await WriteJsonAsync(response, Response.Error($"Path not found: {safeRelative}"));
                return true;
            }
            catch (Exception ex)
            {
                try
                {
                    response.StatusCode = 500;
                    response.ContentType = "application/json; charset=utf-8";
                    await WriteJsonAsync(response, Response.Error($"File route error: {ex.Message}"));
                }
                catch
                {
                    // ignore
                }
                return true;
            }
        }

        private static JsonClass CreateEntryNode(string projectRoot, string fullPath, bool isDirectory, long size)
        {
            string relative = GetRelativePathForApi(projectRoot, fullPath);
            var node = new JsonClass();
            node.Add("name", new JsonData(Path.GetFileName(fullPath)));
            node.Add("isDirectory", new JsonData(isDirectory));
            node.Add("size", new JsonData(size));
            node.Add("path", new JsonData(relative));
            node.Add("url", new JsonData($"/files/{relative.Replace('\\', '/')}"));
            return node;
        }

        private static string GetRelativePathForApi(string root, string fullPath)
        {
            string rel = fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase)
                ? fullPath.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar, '/')
                : fullPath;
            return rel.Replace(Path.DirectorySeparatorChar, '/');
        }

        private static async Task WriteJsonAsync(System.Net.HttpListenerResponse response, JsonNode json)
        {
            string content = json?.ToString() ?? "{}";
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(content);
            await response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            response.Close();
        }

        private static string GetMimeType(string filePath)
        {
            string ext = Path.GetExtension(filePath)?.ToLowerInvariant();
            switch (ext)
            {
                case ".txt": return "text/plain; charset=utf-8";
                case ".json": return "application/json; charset=utf-8";
                case ".xml": return "application/xml; charset=utf-8";
                case ".yaml":
                case ".yml": return "application/yaml; charset=utf-8";
                case ".html":
                case ".htm": return "text/html; charset=utf-8";
                case ".css": return "text/css; charset=utf-8";
                case ".js": return "application/javascript; charset=utf-8";
                case ".png": return "image/png";
                case ".jpg":
                case ".jpeg": return "image/jpeg";
                case ".gif": return "image/gif";
                case ".svg": return "image/svg+xml";
                case ".webp": return "image/webp";
                case ".mp3": return "audio/mpeg";
                case ".wav": return "audio/wav";
                case ".mp4": return "video/mp4";
                case ".pdf": return "application/pdf";
                case ".zip": return "application/zip";
                default: return "application/octet-stream";
            }
        }
    }
}
