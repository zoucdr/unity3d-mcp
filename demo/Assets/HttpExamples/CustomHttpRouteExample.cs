using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

namespace UniMcp.Examples
{
    /// <summary>
    /// 自定义 HTTP 路由示例
    /// 演示如何使用 McpService.RegistHttpRequest 注册自定义路由
    /// </summary>
    public static class CustomHttpRouteExample
    {
        /// <summary>
        /// 注册示例路由
        /// 可以在 Unity 编辑器启动时或通过菜单项调用
        /// </summary>
        [MenuItem("Tools/UniMcp/Examples/Register Custom Routes")]
        public static void RegisterExampleRoutes()
        {
            var service = McpService.Instance;

            // 示例 1: 简单的 GET 请求
            service.RegistHttpRequest("/api/hello", "GET", async (ctx) =>
            {
                Debug.Log($"[CustomRoute] 收到 GET /api/hello 请求");
                return "{\"message\": \"Hello from Unity!\", \"timestamp\": \"" + DateTime.Now.ToString("o") + "\"}";
            });

            // 示例 2: POST 请求处理
            service.RegistHttpRequest("/api/data", "POST", async (ctx) =>
            {
                Debug.Log($"[CustomRoute] 收到 POST /api/data 请求，Body: {ctx.Body}");
                
                // 解析请求体（假设是 JSON）
                try
                {
                    var response = new
                    {
                        success = true,
                        received = ctx.Body,
                        timestamp = DateTime.Now.ToString("o")
                    };
                    return UnityEngine.JsonUtility.ToJson(response);
                }
                catch (Exception ex)
                {
                    return "{\"success\": false, \"error\": \"" + ex.Message + "\"}";
                }
            });

            // 示例 3: 带路径参数的路由
            service.RegistHttpRequest("/api/user/{id}", "GET", async (ctx) =>
            {
                string userId = ctx.PathParams.ContainsKey("id") ? ctx.PathParams["id"] : "unknown";
                Debug.Log($"[CustomRoute] 获取用户信息，ID: {userId}");
                
                return "{\"userId\": \"" + userId + "\", \"name\": \"User " + userId + "\"}";
            });

            // 示例 4: 通配符路由（匹配所有 /files/ 下的路径）
            service.RegistHttpRequest("/files/*", "*", async (ctx) =>
            {
                Debug.Log($"[CustomRoute] 文件请求: {ctx.Method} {ctx.Path}");
                
                return "{\"path\": \"" + ctx.Path + "\", \"method\": \"" + ctx.Method + "\"}";
            });

            // 示例 5: 带查询参数的路由
            service.RegistHttpRequest("/api/search", "GET", async (ctx) =>
            {
                string query = ctx.QueryParams.ContainsKey("q") ? ctx.QueryParams["q"] : "";
                Debug.Log($"[CustomRoute] 搜索请求，关键词: {query}");
                
                return "{\"query\": \"" + query + "\", \"results\": []}";
            });

            // 示例 6: 高优先级路由（优先于其他路由匹配）
            service.RegistHttpRequest("/api/priority", "GET", async (ctx) =>
            {
                return "{\"message\": \"High priority route\"}";
            }, priority: 100);

            // 示例 7: 支持所有 HTTP 方法的路由
            service.RegistHttpRequest("/api/universal", "*", async (ctx) =>
            {
                Debug.Log($"[CustomRoute] 通用路由: {ctx.Method} /api/universal");
                
                return "{\"method\": \"" + ctx.Method + "\", \"message\": \"Supports all HTTP methods\"}";
            });

            // 示例 8: 返回 HTML 内容
            service.RegistHttpRequest("/test/page", "GET", async (ctx) =>
            {
                // 修改响应的 Content-Type
                ctx.Response.ContentType = "text/html; charset=utf-8";
                
                return @"
<!DOCTYPE html>
<html>
<head>
    <title>Unity HTTP Server</title>
    <style>
        body { font-family: Arial, sans-serif; max-width: 800px; margin: 50px auto; padding: 20px; }
        h1 { color: #333; }
        .info { background: #f0f0f0; padding: 15px; border-radius: 5px; }
    </style>
</head>
<body>
    <h1>Unity HTTP Server Test Page</h1>
    <div class='info'>
        <p><strong>Time:</strong> " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @"</p>
        <p><strong>Path:</strong> " + ctx.Path + @"</p>
        <p><strong>Method:</strong> " + ctx.Method + @"</p>
    </div>
</body>
</html>";
            });

            Debug.Log("[CustomRoute] 已注册 8 个示例路由");
            
            // 打印所有已注册的路由
            var routes = service.GetRegisteredRoutes();
            Debug.Log($"[CustomRoute] 当前注册的路由数量: {routes.Count}");
            foreach (var route in routes)
            {
                Debug.Log($"[CustomRoute] - {route}");
            }
        }

        /// <summary>
        /// 注销所有示例路由
        /// </summary>
        [MenuItem("Tools/UniMcp/Examples/Unregister Custom Routes")]
        public static void UnregisterExampleRoutes()
        {
            var service = McpService.Instance;
            
            // 可以按路径注销
            service.UnregistHttpRequest("/api/hello");
            service.UnregistHttpRequest("/api/data");
            service.UnregistHttpRequest("/api/user/{id}");
            
            // 或者清空所有自定义路由
            // service.ClearCustomRoutes();
            
            Debug.Log("[CustomRoute] 已注销示例路由");
        }

        /// <summary>
        /// 清空所有自定义路由
        /// </summary>
        [MenuItem("Tools/UniMcp/Examples/Clear All Custom Routes")]
        public static void ClearAllRoutes()
        {
            var service = McpService.Instance;
            service.ClearCustomRoutes();
            Debug.Log("[CustomRoute] 已清空所有自定义路由");
        }

        /// <summary>
        /// 列出所有已注册的路由
        /// </summary>
        [MenuItem("Tools/UniMcp/Examples/List Custom Routes")]
        public static void ListRoutes()
        {
            var service = McpService.Instance;
            var routes = service.GetRegisteredRoutes();
            
            Debug.Log($"[CustomRoute] 当前注册的路由数量: {routes.Count}");
            foreach (var route in routes)
            {
                Debug.Log($"[CustomRoute] - {route}");
            }
        }
    }
}
