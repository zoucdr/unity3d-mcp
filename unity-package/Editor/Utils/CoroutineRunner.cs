using System;
using System.Collections.Generic;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityMcp;
using UnityEngine.Networking;

namespace UnityMcp
{
    /// <summary>
    /// Main thread executor，Used to ensure code is inUnityExecute on main thread
    /// </summary>
    public static class CoroutineRunner
    {
        private static readonly Queue<System.Action> _actions = new Queue<System.Action>();
        private static readonly List<CoroutineInfo> _coroutines = new List<CoroutineInfo>();
        private static readonly object _lock = new object();
        private static bool _initialized = false;

        /// <summary>
        /// Coroutine info structure
        /// </summary>
        private class CoroutineInfo
        {
            public IEnumerator Coroutine { get; set; }
            public bool IsRunning { get; set; }
            public Action<object> CompleteCallback { get; set; }
            public object Result { get; set; }
            public bool HasResult { get; set; } // Mark if a valid result is present
            public Exception Error { get; set; } // Store exception info
            public CoroutineInfo SubCoroutine { get; set; } // Sub-coroutine
            public bool WaitingForSubCoroutine { get; set; } // Whether waiting for sub-coroutine
            public object SubCoroutineResult { get; set; } // Sub-coroutine result，Used for passing to main coroutine
            public bool HasSubCoroutineResult { get; set; } // Whether sub-coroutine result is present

            // WaitForSecondsSupport
            public bool IsWaitingForTime { get; set; } // Whether waiting for time
            public double WaitEndTime { get; set; } // Wait for end time（UseEditorApplication.timeSinceStartup）

            // UnityWebRequestAsyncOperationSupport
            public bool IsWaitingForWebRequest { get; set; } // Whether waiting for network request
            public UnityWebRequestAsyncOperation WebRequestOperation { get; set; } // Network request operation
        }

        /// <summary>
        /// Initialize main thread executor
        /// </summary>
        static CoroutineRunner()
        {
            Initialize();
        }

        private static void Initialize()
        {
            if (_initialized) return;

            _initialized = true;

            // UseEditorApplication.updateEnsure that tasks in the queue are processed every frame
            EditorApplication.update += ProcessQueue;
        }

        /// <summary>
        /// Handle tasks in queue
        /// </summary>
        private static void ProcessQueue()
        {
            lock (_lock)
            {
                // Handle normal task queue
                while (_actions.Count > 0)
                {
                    var action = _actions.Dequeue();
                    try
                    {
                        action?.Invoke();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[MainThreadExecutor] Error executing action: {e}");
                    }
                }

                // Process coroutine queue
                ProcessCoroutines();
            }
        }

        /// <summary>
        /// Process coroutine queue
        /// </summary>
        private static void ProcessCoroutines()
        {
            var completedCoroutines = new List<CoroutineInfo>();

            foreach (var coroutineInfo in _coroutines.ToArray())
            {
                if (!coroutineInfo.IsRunning) continue;

                try
                {
                    // Check waiting status

                    // 1. If waiting for network request，Check if request finished
                    if (coroutineInfo.IsWaitingForWebRequest)
                    {
                        if (coroutineInfo.WebRequestOperation != null && !coroutineInfo.WebRequestOperation.isDone)
                        {
                            // Network request not yet completed，Continue waiting
                            continue;
                        }
                        else
                        {
                            // Network request completed，Continue executing coroutine
                            coroutineInfo.IsWaitingForWebRequest = false;
                            coroutineInfo.WebRequestOperation = null;
                        }
                    }

                    // 2. If waiting for time，Check if time is up
                    if (coroutineInfo.IsWaitingForTime)
                    {
                        if (EditorApplication.timeSinceStartup < coroutineInfo.WaitEndTime)
                        {
                            // Still waiting for time，Continue waiting
                            continue;
                        }
                        else
                        {
                            // Wait time finished，Continue executing coroutine
                            coroutineInfo.IsWaitingForTime = false;
                            coroutineInfo.WaitEndTime = 0;
                        }
                    }

                    // 2. If waiting for sub-coroutine，Check sub-coroutine status first
                    if (coroutineInfo.WaitingForSubCoroutine && coroutineInfo.SubCoroutine != null)
                    {
                        if (coroutineInfo.SubCoroutine.IsRunning)
                        {
                            // Sub-coroutine still running，Continue waiting
                            continue;
                        }
                        else
                        {
                            // Sub-coroutine completed，Get sub-coroutine result
                            if (coroutineInfo.SubCoroutine.Error != null)
                            {
                                // Sub-coroutine has exception，Propagate exception
                                throw coroutineInfo.SubCoroutine.Error;
                            }

                            // Sub-coroutine completed normally，Set the sub-coroutine’s result as the main coroutine result
                            if (coroutineInfo.SubCoroutine.HasResult)
                            {
                                // Directly set sub-coroutine result as main coroutine result
                                coroutineInfo.Result = coroutineInfo.SubCoroutine.Result;
                                coroutineInfo.HasResult = true;

                                // Also save toSubCoroutineResult（Used for debugging）
                                coroutineInfo.SubCoroutineResult = coroutineInfo.SubCoroutine.Result;
                                coroutineInfo.HasSubCoroutineResult = true;
                            }

                            // Continue main coroutine
                            coroutineInfo.WaitingForSubCoroutine = false;
                            coroutineInfo.SubCoroutine = null;
                        }
                    }

                    // 3. If not waiting for sub-coroutine、Time and network request，Execute next step of coroutine
                    if (!coroutineInfo.WaitingForSubCoroutine && !coroutineInfo.IsWaitingForTime && !coroutineInfo.IsWaitingForWebRequest)
                    {
                        if (coroutineInfo.Coroutine.MoveNext())
                        {
                            // Coroutine still running，Check return value
                            var current = coroutineInfo.Coroutine.Current;

                            // Check if a sub-coroutine is returned（IEnumerator）
                            if (current is IEnumerator subCoroutine)
                            {
                                // Start sub-coroutine
                                var subCoroutineInfo = new CoroutineInfo
                                {
                                    Coroutine = subCoroutine,
                                    IsRunning = true,
                                    CompleteCallback = null,
                                    Result = null,
                                    HasResult = false,
                                    Error = null,
                                    SubCoroutine = null,
                                    WaitingForSubCoroutine = false,
                                    SubCoroutineResult = null,
                                    HasSubCoroutineResult = false,
                                    IsWaitingForTime = false,
                                    WaitEndTime = 0,
                                    IsWaitingForWebRequest = false,
                                    WebRequestOperation = null
                                };

                                // Add sub-coroutine to coroutine list
                                _coroutines.Add(subCoroutineInfo);

                                // Set main coroutine to wait for sub-coroutine
                                coroutineInfo.SubCoroutine = subCoroutineInfo;
                                coroutineInfo.WaitingForSubCoroutine = true;
                            }
                            // Check if returnedUnityWebRequestAsyncOperation
                            else if (current is UnityWebRequestAsyncOperation webRequestOp)
                            {
                                // Set waiting for network request state
                                coroutineInfo.IsWaitingForWebRequest = true;
                                coroutineInfo.WebRequestOperation = webRequestOp;

                                // Save result
                                coroutineInfo.Result = current;
                                coroutineInfo.HasResult = true;
                            }
                            // Check if returnedWaitForSeconds
                            else if (current is WaitForSeconds waitForSeconds)
                            {
                                // Obtain using reflectionWaitForSecondsWait time of
                                var waitTime = GetWaitTimeFromWaitForSeconds(waitForSeconds);
                                if (waitTime > 0)
                                {
                                    // Set waiting status
                                    coroutineInfo.IsWaitingForTime = true;
                                    coroutineInfo.WaitEndTime = EditorApplication.timeSinceStartup + waitTime;
                                    //Debug.Log($"[CoroutineRunner] Start waiting {waitTime} Seconds，End time: {coroutineInfo.WaitEndTime}");
                                }
                                else
                                {
                                    // Unable to get wait time，Use default value
                                    Debug.LogWarning($"[CoroutineRunner] Unable to getWaitForSecondsWait time of，Use default0.1Seconds");
                                    coroutineInfo.IsWaitingForTime = true;
                                    coroutineInfo.WaitEndTime = EditorApplication.timeSinceStartup + 0.1;
                                }

                                // Save result
                                coroutineInfo.Result = current;
                                coroutineInfo.HasResult = true;
                            }
                            else if (current != null)
                            {
                                // Save coroutine result（Other type）
                                coroutineInfo.Result = current;
                                coroutineInfo.HasResult = true;

                                // For other types（Such asnull），Go to next frame directly
                            }
                        }
                        else
                        {
                            // Coroutine completed
                            coroutineInfo.IsRunning = false;
                            completedCoroutines.Add(coroutineInfo);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(new Exception($"[CoroutineRunner] Error executing coroutine: ", e));
                    coroutineInfo.IsRunning = false;
                    coroutineInfo.Error = e;
                    coroutineInfo.HasResult = false;
                    coroutineInfo.WaitingForSubCoroutine = false;
                    coroutineInfo.SubCoroutine = null;
                    coroutineInfo.SubCoroutineResult = null;
                    coroutineInfo.HasSubCoroutineResult = false;
                    coroutineInfo.IsWaitingForTime = false;
                    coroutineInfo.WaitEndTime = 0;
                    coroutineInfo.IsWaitingForWebRequest = false;
                    coroutineInfo.WebRequestOperation = null;
                    completedCoroutines.Add(coroutineInfo);
                }
            }

            // Remove finished coroutines and call completion callbacks
            foreach (var completed in completedCoroutines)
            {
                _coroutines.Remove(completed);
                try
                {
                    // Decide the result passed to the callback
                    object resultToPass;
                    if (completed.Error != null)
                    {
                        // If has exception，Pass exception
                        resultToPass = completed.Error;
                    }
                    else if (completed.HasResult)
                    {
                        // If has result，Pass result
                        resultToPass = completed.Result;
                    }
                    else
                    {
                        // Neither exception nor result，Passnull
                        resultToPass = null;
                    }
                    //Debug.Log($"[CoroutineRunner] Complete callback: {resultToPass}");
                    completed.CompleteCallback?.Invoke(resultToPass);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[CoroutineRunner] Error in coroutine complete callback: {e}");
                }
            }
        }

        /// <summary>
        /// Start coroutine
        /// </summary>
        /// <param name="coroutine">Coroutine enumerator</param>
        /// <param name="completeCallback">Complete callback</param>
        public static void StartCoroutine(IEnumerator coroutine, Action<object> completeCallback = null)
        {
            if (coroutine == null) return;

            lock (_lock)
            {
                var coroutineInfo = new CoroutineInfo
                {
                    Coroutine = coroutine,
                    IsRunning = true,
                    CompleteCallback = completeCallback,
                    Result = null,
                    HasResult = false,
                    Error = null,
                    SubCoroutine = null,
                    WaitingForSubCoroutine = false,
                    SubCoroutineResult = null,
                    HasSubCoroutineResult = false,
                    IsWaitingForTime = false,
                    WaitEndTime = 0,
                    IsWaitingForWebRequest = false,
                    WebRequestOperation = null
                };

                _coroutines.Add(coroutineInfo);
            }
        }

        /// <summary>
        /// Stop all coroutines
        /// </summary>
        public static void StopAllCoroutines()
        {
            lock (_lock)
            {
                _coroutines.Clear();
            }
        }

        /// <summary>
        /// Use reflection fromWaitForSecondsGet wait time from object
        /// </summary>
        /// <param name="waitForSeconds">WaitForSecondsInstance</param>
        /// <returns>Wait time（Seconds），Return if retrieval fails-1</returns>
        private static float GetWaitTimeFromWaitForSeconds(WaitForSeconds waitForSeconds)
        {
            try
            {
                // Obtain using reflectionWaitForSecondsPrivate field of m_Seconds
                var field = typeof(WaitForSeconds).GetField("m_Seconds",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (field != null)
                {
                    var value = field.GetValue(waitForSeconds);
                    if (value is float seconds)
                    {
                        return seconds;
                    }
                }

                // If the above field name is incorrect，Try other possible field names
                var fields = typeof(WaitForSeconds).GetFields(
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                foreach (var f in fields)
                {
                    if (f.FieldType == typeof(float))
                    {
                        var value = f.GetValue(waitForSeconds);
                        if (value is float seconds && seconds > 0)
                        {
                            Debug.Log($"[CoroutineRunner] Find wait time field: {f.Name} = {seconds}");
                            return seconds;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[CoroutineRunner] GetWaitForSecondsWait time failed: {e.Message}");
            }

            return -1; // Failed to obtain
        }
    }
}
