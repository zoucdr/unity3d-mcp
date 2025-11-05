using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UniMcp
{
    public class McpLogger
    {
        public enum LogLevel
        {
            None,
            Error,
            Warning,
            Info
        }
        private static LogLevel logLevel = LogLevel.Warning;
        private const string LogLevelPrefKey = "mcp_log_level";

        static McpLogger()
        {
            logLevel = (LogLevel)EditorPrefs.GetInt(LogLevelPrefKey, (int)LogLevel.Warning);
        }

        public static LogLevel GetLogLevel()
        {
            return logLevel;
        }

        public static void SetLogLevel(LogLevel level)
        {
            logLevel = level;
            EditorPrefs.SetInt(LogLevelPrefKey, (int)level);
        }

        // 统一的日志输出方法
        public static void Log(string message)
        {
            if (logLevel >= LogLevel.Info) Debug.Log(message);
        }

        public static void LogWarning(string message)
        {
            if (logLevel >= LogLevel.Warning) Debug.LogWarning(message);
        }

        public static void LogError(string message)
        {
            if (logLevel >= LogLevel.Error) Debug.LogError(message);
        }
    }
}