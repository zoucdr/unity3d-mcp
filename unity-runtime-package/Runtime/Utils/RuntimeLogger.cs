using System;
using UnityEngine;

namespace UniMcp.Runtime
{
    public static class RuntimeLogger
    {
        private static bool _enableLog = true;
        private static bool _enableWarning = true;
        private static bool _enableError = true;

        public static bool EnableLog
        {
            get => _enableLog;
            set => _enableLog = value;
        }

        public static bool EnableWarning
        {
            get => _enableWarning;
            set => _enableWarning = value;
        }

        public static bool EnableError
        {
            get => _enableError;
            set => _enableError = value;
        }

        public static void Log(string message)
        {
            if (_enableLog)
            {
                Debug.Log($"[UniMcp.Runtime] {message}");
            }
        }

        public static void LogWarning(string message)
        {
            if (_enableWarning)
            {
                Debug.LogWarning($"[UniMcp.Runtime] {message}");
            }
        }

        public static void LogError(string message)
        {
            if (_enableError)
            {
                Debug.LogError($"[UniMcp.Runtime] {message}");
            }
        }

        public static void LogFormat(string format, params object[] args)
        {
            if (_enableLog)
            {
                Debug.LogFormat($"[UniMcp.Runtime] {format}", args);
            }
        }
    }
}
