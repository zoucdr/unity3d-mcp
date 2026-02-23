using System;
using System.Collections;
using System.Collections.Generic;
using UniMcp.Runtime;

namespace UniMcp.Runtime.Examples
{
    [ToolName("hello", "Demo")]
    public class HelloTool : IToolMethod
    {
        public string Description => "A simple hello world tool for testing";

        public MethodKey[] Keys => new MethodKey[]
        {
            new MethodKey("name", "Name to greet", true, "string", "World")
        };

        public void ExecuteMethod(StateTreeContext args)
        {
            string name = args.JsonData["name"]?.Value ?? "World";
            string message = $"Hello, {name}! Welcome to Unity Runtime MCP.";

            RuntimeLogger.Log($"[HelloTool] Greeting: {name}");

            args.Complete(Response.Success(message, new JsonClass
            {
                { "greeting", message },
                { "timestamp", DateTime.Now.ToString("o") }
            }));
        }

        public string Preview()
        {
            return "hello(name: string) - A simple hello world tool";
        }
    }

    [ToolName("get_system_info", "System")]
    public class GetSystemInfoTool : IToolMethod
    {
        public string Description => "Get system information from the Unity runtime";

        public MethodKey[] Keys => new MethodKey[]
        {
            new MethodKey("include_memory", "Include memory info", true, "boolean", "true"),
            new MethodKey("include_platform", "Include platform info", true, "boolean", "true")
        };

        public void ExecuteMethod(StateTreeContext args)
        {
            bool includeMemory = args.JsonData["include_memory"]?.AsBool ?? true;
            bool includePlatform = args.JsonData["include_platform"]?.AsBool ?? true;

            var info = new JsonClass();

            if (includePlatform)
            {
                info.Add("platform", UnityEngine.Application.platform.ToString());
                info.Add("version", UnityEngine.Application.version);
                info.Add("company", UnityEngine.Application.companyName);
                info.Add("product", UnityEngine.Application.productName);
            }

            if (includeMemory)
            {
                info.Add("total_memory_mb", UnityEngine.SystemInfo.systemMemorySize);
                info.Add("graphics_device", UnityEngine.SystemInfo.graphicsDeviceName);
                info.Add("graphics_memory_mb", UnityEngine.SystemInfo.graphicsMemorySize);
            }

            info.Add("unity_version", UnityEngine.Application.unityVersion);
            info.Add("runtime_version", Environment.Version.ToString());

            RuntimeLogger.Log($"[GetSystemInfoTool] Returning system info");

            args.Complete(Response.Success("System information retrieved", info));
        }

        public string Preview()
        {
            return "get_system_info(include_memory: boolean, include_platform: boolean) - Get system information";
        }
    }

    [ToolName("log_message", "Debug")]
    public class LogMessageTool : IToolMethod
    {
        public string Description => "Log a message to the Unity console from MCP";

        public MethodKey[] Keys => new MethodKey[]
        {
            new MethodKey("message", "Message to log", false, "string"),
            new MethodKey("level", "Log level (log/warning/error)", true, "string", "log")
        };

        public void ExecuteMethod(StateTreeContext args)
        {
            string message = args.JsonData["message"]?.Value ?? "";
            string level = args.JsonData["level"]?.Value ?? "log";

            switch (level.ToLower())
            {
                case "warning":
                    RuntimeLogger.LogWarning($"[MCP] {message}");
                    break;
                case "error":
                    RuntimeLogger.LogError($"[MCP] {message}");
                    break;
                default:
                    RuntimeLogger.Log($"[MCP] {message}");
                    break;
            }

            args.Complete(Response.Success($"Message logged at {level} level", new JsonClass
            {
                { "logged_message", message },
                { "level", level }
            }));
        }

        public string Preview()
        {
            return "log_message(message: string, level: string) - Log a message to console";
        }
    }

    [ToolName("get_time", "Utility")]
    public class GetTimeTool : IToolMethod
    {
        public string Description => "Get current time from the Unity runtime";

        public MethodKey[] Keys => new MethodKey[]
        {
            new MethodKey("format", "Time format (iso/unix/ticks)", true, "string", "iso")
        };

        public void ExecuteMethod(StateTreeContext args)
        {
            string format = args.JsonData["format"]?.Value ?? "iso";
            var now = DateTime.Now;

            var result = new JsonClass();

            switch (format.ToLower())
            {
                case "unix":
                    result.Add("unix_timestamp", new JsonData((long)(now - new DateTime(1970, 1, 1)).TotalSeconds));
                    break;
                case "ticks":
                    result.Add("ticks", new JsonData(now.Ticks));
                    break;
                default:
                    result.Add("iso8601", new JsonData(now.ToString("o")));
                    result.Add("local", new JsonData(now.ToString("yyyy-MM-dd HH:mm:ss")));
                    break;
            }

            result.Add("utc", new JsonData(now.ToUniversalTime().ToString("o")));

            args.Complete(Response.Success("Current time retrieved", result));
        }

        public string Preview()
        {
            return "get_time(format: string) - Get current time";
        }
    }
}
