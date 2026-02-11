using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniMcp;
using UniMcp.Tools;
using UniMcp.Models;
using UniMcp.Utils;

namespace UniMcp.Tools
{
    [ToolName("cmd_tool", "System Tools", "系统工具")]
    public class CMCmdTool : StateMethodBase
    {
        public override string Description => L.T("Execute system CMD commands", "执行系统CMD命令");
        
        /// <summary>
        /// 创建当前方法支持的参数键列表
        /// </summary>
        protected override MethodKey[] CreateKeys()
        {
            return new MethodKey[]
            {
                new MethodStr("cmd", L.T("CMD command", "CMD命令"), false),
                new MethodArr("args", L.T("CMD command arguments", "CMD命令参数"), true, "string"),
                new MethodObj("env", L.T("CMD command environment variables", "CMD命令环境变量"), true)
                    .AddStringProperty("PATH")
                    .AddStringProperty("HOME"),
                new MethodStr("working_dir", L.T("Working directory", "工作目录"), true),
                new MethodInt("timeout", L.T("Timeout (seconds)", "超时时间(秒)"), true).SetDefault(30).SetRange(1, 300),
            };
        }

        protected override StateTree CreateStateTree()
        {
            return StateTreeBuilder
                .Create()
                .DefaultLeaf(HandleExecuteCmd)
                .Build();
        }

        /// <summary>
        /// 处理执行CMD命令的操作
        /// </summary>
        private object HandleExecuteCmd(JsonClass args)
        {
            var context = new StateTreeContext(args);
            ExecuteCmdCommand(context);
            return context;
        }

        private void ExecuteCmdCommand(StateTreeContext context)
        {
            try
            {
                // 使用正确的 API 获取参数
                string cmd = context["cmd"] as string;
                string[] cmdArgs = null;
                Dictionary<string, string> env = null;
                string workingDir = context["working_dir"] as string;
                int timeout = 30;

                // 尝试获取数组参数
                if (context.TryGetValue("args", out object argsObj) && argsObj is string[] args)
                {
                    cmdArgs = args;
                }

                // 尝试获取环境变量
                if (context.TryGetValue("env", out object envObj) && envObj is Dictionary<string, string> envDict)
                {
                    env = envDict;
                }

                // 尝试获取超时时间
                if (context.TryGetValue<int>("timeout", out int timeoutValue))
                {
                    timeout = timeoutValue;
                }

                if (string.IsNullOrEmpty(cmd))
                {
                    context.Complete(Response.Error(L.T("CMD command cannot be empty", "CMD命令不能为空")));
                    return;
                }

                // 构建完整的命令字符串
                string fullCommand = cmd;
                if (cmdArgs != null && cmdArgs.Length > 0)
                {
                    fullCommand += " " + string.Join(" ", cmdArgs);
                }

                // 执行CMD命令
                var processInfo = new System.Diagnostics.ProcessStartInfo()
                {
                    FileName = "cmd.exe",
                    Arguments = "/c " + fullCommand,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                // 设置工作目录
                if (!string.IsNullOrEmpty(workingDir))
                {
                    processInfo.WorkingDirectory = workingDir;
                }

                // 设置环境变量
                if (env != null)
                {
                    foreach (var kvp in env)
                    {
                        processInfo.EnvironmentVariables[kvp.Key] = kvp.Value;
                    }
                }

                using (var process = System.Diagnostics.Process.Start(processInfo))
                {
                    bool finished = process.WaitForExit(timeout * 1000);

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    int exitCode = finished ? process.ExitCode : -1;

                    if (!finished)
                    {
                        process.Kill();
                        context.Complete(Response.Error(L.T($"Command execution timeout ({timeout} seconds)", $"命令执行超时 ({timeout}秒)")));
                        return;
                    }

                    // 构建返回结果
                    var result = new
                    {
                        output = output,
                        error = error,
                        exitCode = exitCode,
                        success = exitCode == 0,
                        command = fullCommand
                    };

                    if (exitCode != 0)
                    {
                        context.Complete(Response.Error(L.T($"Command execution failed, exit code: {exitCode}, error message: {error}", $"命令执行失败，退出码: {exitCode}，错误信息: {error}"), result));
                    }
                    else
                    {
                        context.Complete(Response.Success(L.T($"Command executed successfully: {cmd}", $"命令执行成功: {cmd}"), result));
                    }
                }
            }
            catch (System.Exception ex)
            {
                context.Complete(Response.Error(L.T($"Exception occurred while executing CMD command: {ex.Message}", $"执行CMD命令时发生异常: {ex.Message}")));
            }
        }
    }
}
