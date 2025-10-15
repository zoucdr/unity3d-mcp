using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEditor;
using UnityMcp.Models;
// Migrated from Newtonsoft.Json to SimpleJson
using System.Text;
using System.Threading.Tasks;

namespace UnityMcp
{
    /// <summary>
    /// State method base class，Provide method call framework based on state tree。
    /// All tool method classes should inherit from this class，And implement CreateStateTree Method to define state routing logic。
    /// </summary>
    public abstract class StateMethodBase : IToolMethod
    {
        /// <summary>
        /// State tree instance，For routing and executing method calls。
        /// Lazy loading mode：Only create upon first access。
        /// </summary>
        private StateTree _stateTree;

        /// <summary>
        /// KeysCached field of，Avoid duplicate creation
        /// </summary>
        private MethodKey[] _keys;

        /// <summary>
        /// List of parameter keys supported by this method，Used forAPIDocumentation generation and parameter validation。
        /// Subclass must implement this property，Define all possible parameter keys accepted by this method。
        /// </summary>
        public MethodKey[] Keys
        {
            get
            {
                if (_keys == null)
                {
                    _keys = CreateKeys();
                }
                return _keys;
            }
        }

        /// <summary>
        /// Abstract method for creating parameter key list，Subclass must implement this method to define parameter keys。
        /// </summary>
        /// <returns>MethodKeyArray</returns>
        protected abstract MethodKey[] CreateKeys();

        /// <summary>
        /// Abstract method for creating state tree，Subclass must implement this method to define state routing logic。
        /// </summary>
        /// <returns>Configured state tree instance</returns>
        protected abstract StateTree CreateStateTree();

        /// <summary>
        /// Preview state tree structure，For debugging and visualizing state routing logic。
        /// </summary>
        /// <returns>Text representation of state tree</returns>
        public virtual string Preview()
        {
            // Ensure that the state tree is initialized
            _stateTree = _stateTree ?? CreateStateTree();
            var sb = new StringBuilder();
            _stateTree.Print(sb);
            return sb.ToString();
        }

        /// <summary>
        /// Execute tool method，Implement IToolMethod Interface（Synchronous version）。
        /// Route to the corresponding handler via the state tree。
        /// </summary>
        /// <param name="ctx">Parameter object for method call</param>
        /// <returns>Execution result，Return error response if state tree execution fails</returns>
        public virtual void ExecuteMethod(StateTreeContext ctx)
        {
            // Ensure that the state tree is initialized
            _stateTree = _stateTree ?? CreateStateTree();
            var result = _stateTree.Run(ctx);
            // If the result is empty and there is an error message，Return error response
            if (result == null && !string.IsNullOrEmpty(_stateTree.ErrorMessage))
            {
                ctx.Complete(Response.Error(_stateTree.ErrorMessage));
            }
            else if (result != null && result != ctx)
            {
                ctx.Complete(Json.FromObject(result));
            }
            else
            {
                //Asynchronous execution
                LogInfo("[StateMethodBase] Async executing!");
            }
        }

        /// <summary>
        /// Log informational message，Only at McpConnect.EnableLog As true Output when。
        /// Subclass can use this method to log information during execution。
        /// </summary>
        /// <param name="message">Log message to record</param>
        public virtual void LogInfo(string message)
        {
            if (McpConnect.EnableLog) Debug.Log(message);
        }

        public virtual void LogWarning(string message)
        {
            Debug.LogWarning(message);
        }

        public virtual void LogError(string message)
        {
            Debug.LogError(message);
        }

        public virtual void LogException(Exception exception)
        {
            Debug.LogException(exception);
        }
    }
}