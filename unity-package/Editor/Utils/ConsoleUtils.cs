using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace UnityMcp
{
    /// <summary>
    /// Responsible forUnityActual operation of editor console，Include reading and clearing log entries。
    /// Use reflection to access internalLogEntryMethod/Attribute。
    /// </summary>
    public static class ConsoleUtils
    {
        // Used to access internalLogEntryReflection member for data
        private static MethodInfo _startGettingEntriesMethod;
        private static MethodInfo _endGettingEntriesMethod;
        private static MethodInfo _clearMethod;
        private static MethodInfo _getCountMethod;
        private static MethodInfo _getEntryMethod;
        private static FieldInfo _modeField;
        private static FieldInfo _messageField;
        private static FieldInfo _fileField;
        private static FieldInfo _lineField;
        private static FieldInfo _instanceIdField;

        // Static constructor used for reflection setup
        static ConsoleUtils()
        {
            try
            {
                Type logEntriesType = typeof(EditorApplication).Assembly.GetType(
                    "UnityEditor.LogEntries"
                );
                if (logEntriesType == null)
                    throw new Exception("Could not find internal type UnityEditor.LogEntries");

                // ContainsNonPublicBinding flag，Because internalAPIMay affect accessibility
                BindingFlags staticFlags =
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
                BindingFlags instanceFlags =
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

                _startGettingEntriesMethod = logEntriesType.GetMethod(
                    "StartGettingEntries",
                    staticFlags
                );
                if (_startGettingEntriesMethod == null)
                    throw new Exception("Failed to reflect LogEntries.StartGettingEntries");

                _endGettingEntriesMethod = logEntriesType.GetMethod(
                    "EndGettingEntries",
                    staticFlags
                );
                if (_endGettingEntriesMethod == null)
                    throw new Exception("Failed to reflect LogEntries.EndGettingEntries");

                _clearMethod = logEntriesType.GetMethod("Clear", staticFlags);
                if (_clearMethod == null)
                    throw new Exception("Failed to reflect LogEntries.Clear");

                _getCountMethod = logEntriesType.GetMethod("GetCount", staticFlags);
                if (_getCountMethod == null)
                    throw new Exception("Failed to reflect LogEntries.GetCount");

                _getEntryMethod = logEntriesType.GetMethod("GetEntryInternal", staticFlags);
                if (_getEntryMethod == null)
                    throw new Exception("Failed to reflect LogEntries.GetEntryInternal");

                Type logEntryType = typeof(EditorApplication).Assembly.GetType(
                    "UnityEditor.LogEntry"
                );
                if (logEntryType == null)
                    throw new Exception("Could not find internal type UnityEditor.LogEntry");

                _modeField = logEntryType.GetField("mode", instanceFlags);
                if (_modeField == null)
                    throw new Exception("Failed to reflect LogEntry.mode");

                _messageField = logEntryType.GetField("message", instanceFlags);
                if (_messageField == null)
                    throw new Exception("Failed to reflect LogEntry.message");

                _fileField = logEntryType.GetField("file", instanceFlags);
                if (_fileField == null)
                    throw new Exception("Failed to reflect LogEntry.file");

                _lineField = logEntryType.GetField("line", instanceFlags);
                if (_lineField == null)
                    throw new Exception("Failed to reflect LogEntry.line");

                _instanceIdField = logEntryType.GetField("instanceID", instanceFlags);
                if (_instanceIdField == null)
                    throw new Exception("Failed to reflect LogEntry.instanceID");
            }
            catch (Exception e)
            {
                if (McpConnect.EnableLog) Debug.LogError(
                    $"[ConsoleController] Static Initialization Failed: Could not setup reflection for LogEntries/LogEntry. Console reading/clearing will likely fail. Specific Error: {e.Message}"
                );
                // Set member asnullTo avoid subsequentNullReferenceExceptions
                _startGettingEntriesMethod =
                    _endGettingEntriesMethod =
                    _clearMethod =
                    _getCountMethod =
                    _getEntryMethod =
                        null;
                _modeField = _messageField = _fileField = _lineField = _instanceIdField = null;
            }
        }

        /// <summary>
        /// Check if reflection members have been correctly initialized
        /// </summary>
        public static bool AreReflectionMembersInitialized()
        {
            return _startGettingEntriesMethod != null
                && _endGettingEntriesMethod != null
                && _clearMethod != null
                && _getCountMethod != null
                && _getEntryMethod != null
                && _modeField != null
                && _messageField != null
                && _fileField != null
                && _lineField != null
                && _instanceIdField != null;
        }

        /// <summary>
        /// Clear console logs
        /// </summary>
        public static void ClearConsole()
        {
            if (!AreReflectionMembersInitialized())
            {
                throw new InvalidOperationException("ConsoleController reflection members are not initialized. Cannot clear console logs.");
            }

            try
            {
                _clearMethod.Invoke(null, null); // Static method，No instance，No parameter
            }
            catch (Exception e)
            {
                if (McpConnect.EnableLog) Debug.LogError($"[ConsoleController] Failed to clear console: {e}");
                throw new InvalidOperationException($"Failed to clear console: {e.Message}", e);
            }
        }

        /// <summary>
        /// Get console log entry
        /// </summary>
        /// <param name="types">List of log types to get</param>
        /// <param name="count">Limit the number retrieved，nullIndicates get all</param>
        /// <param name="filterText">Text filter</param>
        /// <param name="format">Return format</param>
        /// <param name="includeStacktrace">Whether contains stack trace</param>
        /// <returns>Formatted log entry list</returns>
        public static List<object> GetConsoleEntries(
            List<string> types,
            int? count,
            string filterText,
            string format,
            bool includeStacktrace
        )
        {
            if (!AreReflectionMembersInitialized())
            {
                throw new InvalidOperationException("ConsoleController reflection members are not initialized. Cannot access console logs.");
            }

            List<object> formattedEntries = new List<object>();
            int retrievedCount = 0;

            try
            {
                // LogEntries Need to be atGetEntries/GetEntryInternalCalling aroundStart/Stop
                _startGettingEntriesMethod.Invoke(null, null);

                int totalEntries = (int)_getCountMethod.Invoke(null, null);
                // Create instance and pass toGetEntryInternal - Ensure type is correct
                Type logEntryType = typeof(EditorApplication).Assembly.GetType(
                    "UnityEditor.LogEntry"
                );
                if (logEntryType == null)
                    throw new Exception(
                        "Could not find internal type UnityEditor.LogEntry during GetConsoleEntries."
                    );
                object logEntryInstance = Activator.CreateInstance(logEntryType);

                for (int i = 0; i < totalEntries; i++)
                {
                    // Use reflection to get entry data into our instance
                    _getEntryMethod.Invoke(null, new object[] { i, logEntryInstance });

                    // Use reflection to extract data
                    int mode = (int)_modeField.GetValue(logEntryInstance);
                    string message = (string)_messageField.GetValue(logEntryInstance);
                    string file = (string)_fileField.GetValue(logEntryInstance);
                    int line = (int)_lineField.GetValue(logEntryInstance);

                    if (string.IsNullOrEmpty(message))
                        continue; // Skip blank message



                    // --- Formatting and type inference ---
                    string stackTrace = includeStacktrace ? ExtractStackTrace(message) : null;
                    // According toincludeStacktraceParameter determines whether stack info is included
                    string messageOnly;
                    if (includeStacktrace)
                    {
                        // When stack trace is needed，Use full message
                        messageOnly = message;
                    }
                    else
                    {
                        // When no stack trace needed，Only extract the first line as plain message
                        messageOnly = message.Split(
                            new[] { '\n', '\r' },
                            StringSplitOptions.RemoveEmptyEntries
                        )[0];
                    }

                    // Identify type more accurately using stack trace info
                    LogType currentType = GetLogTypeFromModeAndStackTrace(mode, message, stackTrace);

                    // --- Filter ---  
                    // Filter by type
                    if (!types.Contains(currentType.ToString().ToLowerInvariant()))
                    {
                        continue;
                    }

                    // Filter by text（Case insensitive）
                    if (
                        !string.IsNullOrEmpty(filterText)
                        && message.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) < 0
                    )
                    {
                        continue;
                    }

                    object formattedEntry = null;
                    switch (format)
                    {
                        case "plain":
                            formattedEntry = messageOnly;
                            break;
                        case "json":
                        case "detailed": // WilldetailedRegard asjsonTo return structured data
                        default:
                            formattedEntry = new
                            {
                                type = currentType.ToString(),
                                message = messageOnly,
                                file = file,
                                line = line,
                                stackTrace = stackTrace, // IfincludeStacktraceForfalseOr stack not found，Will benull
                            };
                            break;
                    }

                    formattedEntries.Add(formattedEntry);
                    retrievedCount++;

                    // Apply count limit（After filtering）
                    if (count.HasValue && retrievedCount >= count.Value)
                    {
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                if (McpConnect.EnableLog) Debug.LogError($"[ConsoleController] Error while retrieving log entries: {e}");
                // Even if an error occurs during iteration，Also make sure callEndGettingEntries
                try
                {
                    _endGettingEntriesMethod.Invoke(null, null);
                }
                catch
                { /* Ignore nested exception */ }
                throw new InvalidOperationException($"Error retrieving log entries: {e.Message}", e);
            }
            finally
            {
                // Ensure we always callEndGettingEntries
                try
                {
                    _endGettingEntriesMethod.Invoke(null, null);
                }
                catch (Exception e)
                {
                    if (McpConnect.EnableLog) Debug.LogError($"[ConsoleController] Failed to call EndGettingEntries: {e}");
                    // No error returned here，Because we may have valid data，But record it。
                }
            }

            return formattedEntries;
        }

        // --- Internal helper method ---

        // LogEntry.modeBit toLogTypeEnum mapping
        // Based on decompilationUnityEditorCode or common pattern。Exact bit may be inUnityChange between versions。
        private const int ModeBitError = 1 << 0;
        private const int ModeBitAssert = 1 << 1;
        private const int ModeBitWarning = 1 << 2;
        private const int ModeBitLog = 1 << 3;
        private const int ModeBitException = 1 << 4; // Often withErrorBit combination
        private const int ModeBitScriptingError = 1 << 9;
        private const int ModeBitScriptingWarning = 1 << 10;
        private const int ModeBitScriptingLog = 1 << 11;
        private const int ModeBitScriptingException = 1 << 18;
        private const int ModeBitScriptingAssertion = 1 << 22;

        private static LogType GetLogTypeFromMode(int mode)
        {
            // Based onUnity 2021.3The actualbitPattern
            // Discovered by observationUnityInternalmodeBit definition differs from our expectation

            // Simplified mapping based on bit pattern
            // Based on observation，Redefine bit mapping：

            if ((mode & 0x8) != 0) // Bit3 - Actual corresponds toError
            {
                return LogType.Error;
            }
            else if ((mode & 0x4) != 0) // Bit2 - Actual corresponds toWarning  
            {
                return LogType.Warning;
            }
            else if ((mode & 0x2) != 0) // Bit1 - Actual corresponds toAssert
            {
                return LogType.Assert;
            }
            else if ((mode & 0x1) != 0) // Bit0 - Actual corresponds toException
            {
                return LogType.Exception;
            }
            else if ((mode & 0x10) != 0) // Bit4 - May correspond to otherExceptionType
            {
                return LogType.Exception;
            }
            else
            {
                return LogType.Log; // Default is normal log
            }
        }

        /// <summary>
        /// Based onmode、Infer correct log type from message and stack trace
        /// </summary>
        private static LogType GetLogTypeFromModeAndStackTrace(int mode, string fullMessage, string stackTrace)
        {
            // Check compile warnings first - UnityMark compile warning asError，But we should recognize it asWarning
            if (!string.IsNullOrEmpty(fullMessage) &&
                (fullMessage.Contains("warning CS") || fullMessage.Contains(": warning ")))
            {
                return LogType.Warning;
            }

            // Prefer stack trace for type judgement，This is the most reliable method
            string textToSearch = stackTrace ?? fullMessage;

            if (!string.IsNullOrEmpty(textToSearch))
            {
                // Exact matchDebugMethod call
                if (textToSearch.Contains("UnityEngine.Debug:LogError (object)"))
                {
                    return LogType.Error;
                }
                else if (textToSearch.Contains("UnityEngine.Debug:LogWarning (object)"))
                {
                    return LogType.Warning;
                }
                else if (textToSearch.Contains("UnityEngine.Debug:LogAssertion (object)"))
                {
                    return LogType.Assert;
                }
                else if (textToSearch.Contains("UnityEngine.Debug:LogException"))
                {
                    return LogType.Exception;
                }
                else if (textToSearch.Contains("UnityEngine.Debug:Log (object)"))
                {
                    return LogType.Log;
                }

                // Alternative pattern match（Not so precise）
                else if (textToSearch.Contains("UnityEngine.Debug:LogError"))
                {
                    return LogType.Error;
                }
                else if (textToSearch.Contains("UnityEngine.Debug:LogWarning"))
                {
                    return LogType.Warning;
                }
                else if (textToSearch.Contains("UnityEngine.Debug:Log"))
                {
                    return LogType.Log;
                }
            }

            // For special cases like compilation error，UsemodeBit analysis
            // Compile errors usually don't containDebugCalled stack trace
            if (string.IsNullOrEmpty(stackTrace) || !stackTrace.Contains("UnityEngine.Debug:"))
            {
                // For real compile errors，modeBit is reliable
                if ((mode & ModeBitError) != 0 || (mode & ModeBitException) != 0 ||
                    (mode & ModeBitScriptingError) != 0 || (mode & ModeBitScriptingException) != 0)
                {
                    return LogType.Error;
                }
                else if ((mode & ModeBitWarning) != 0 || (mode & ModeBitScriptingWarning) != 0)
                {
                    return LogType.Warning;
                }
                else if ((mode & ModeBitAssert) != 0 || (mode & ModeBitScriptingAssertion) != 0)
                {
                    return LogType.Assert;
                }
            }

            // Default fallback
            return LogType.Log;
        }

        /// <summary>
        /// Try to extract stack trace section from log message。
        /// UnityLog message usually appends stack trace after main message，
        /// Start from new line，Usually indented or with"at "Start。
        /// </summary>
        /// <param name="fullMessage">Full log message containing possible stack trace。</param>
        /// <returns>Extracted stack trace string，Return if not foundnull。</returns>
        private static string ExtractStackTrace(string fullMessage)
        {
            if (string.IsNullOrEmpty(fullMessage))
                return null;

            // Split into lines，Remove blank lines to gracefully handle line endings。
            string[] lines = fullMessage.Split(
                new[] { '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries
            );

            // If only one line or less，No separate stack trace。
            if (lines.Length <= 1)
                return null;

            int stackStartIndex = -1;

            // Start check from line 2。
            for (int i = 1; i < lines.Length; ++i)
            {
                string trimmedLine = lines[i].TrimStart();

                // Check common stack trace patterns。
                if (
                    trimmedLine.StartsWith("at ")
                    || trimmedLine.StartsWith("UnityEngine.")
                    || trimmedLine.StartsWith("UnityEditor.")
                    || trimmedLine.Contains("(at ")
                    || // Cover"(at Assets/..."Pattern
                       // Heuristic：Check if line starts with a possible namespace/Class pattern start（Uppercase.Something）
                    (
                        trimmedLine.Length > 0
                        && char.IsUpper(trimmedLine[0])
                        && trimmedLine.Contains('.')
                    )
                )
                {
                    stackStartIndex = i;
                    break; // Find possible start of stack trace
                }
            }

            // If potential start index found...
            if (stackStartIndex > 0)
            {
                // Join lines from stack start index with standard newline。
                // This reconstructs the stack trace portion of the message。
                return string.Join("\n", lines.Skip(stackStartIndex));
            }

            // No explicit stack trace found based on pattern。
            return null;
        }
    }
}
