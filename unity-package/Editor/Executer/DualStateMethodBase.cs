using System;
using UnityEngine;
using UnityMcp.Models;
using System.Text;

namespace UnityMcp
{
    /// <summary>
    /// Dual state tree method base class，Provides a method invocation framework based on two state trees。
    /// The first tree is for target location，The second tree is for operation execution。
    /// All tool method classes requiring dual-phase processing should inherit from this class。
    /// </summary>
    public abstract class DualStateMethodBase : IToolMethod
    {
        /// <summary>
        /// Target locating state tree instance，For locating operation target。
        /// Lazy loading mode：Create only on first access。
        /// </summary>
        private StateTree _targetTree;

        /// <summary>
        /// Operation execution state tree instance，For performing specific operations。
        /// Lazy loading mode：Create only on first access。
        /// </summary>
        private StateTree _actionTree;

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
        /// Abstract method to create parameter key list，Subclass must implement this method to define parameter keys。
        /// </summary>
        /// <returns>MethodKeyArray</returns>
        protected abstract MethodKey[] CreateKeys();

        /// <summary>
        /// Abstract method for creating the target locating state tree，Subclass must implement this method for target location logic。
        /// </summary>
        /// <returns>Configured target locating state tree instance</returns>
        protected abstract StateTree CreateTargetTree();

        /// <summary>
        /// Abstract method for creating the operation execution state tree，Subclass must implement this method for operation execution logic。
        /// </summary>
        /// <returns>Configured operation execution state tree instance</returns>
        protected abstract StateTree CreateActionTree();

        /// <summary>
        /// Preview dual state tree structure，For debugging and visualizing the state routing logic。
        /// </summary>
        /// <returns>Text representation of dual state trees</returns>
        public virtual string Preview()
        {
            // Ensure that state trees are initialized
            _targetTree = _targetTree ?? CreateTargetTree();
            _actionTree = _actionTree ?? CreateActionTree();

            var sb = new StringBuilder();
            sb.AppendLine("=== Dual State Method Preview ===");
            sb.AppendLine();
            sb.AppendLine(">>> Target Location Tree <<<");
            _targetTree.Print(sb);
            sb.AppendLine();
            sb.AppendLine(">>> Action Execution Tree <<<");
            _actionTree.Print(sb);
            sb.AppendLine();
            sb.AppendLine("=== End Preview ===");

            return sb.ToString();
        }

        /// <summary>
        /// Execute tool method，Implement IToolMethod Interface。
        /// In two phases：First locate the target via the target state tree，Then perform operations through the execution state tree。
        /// </summary>
        /// <param name="args">Parameter object for method invocation</param>
        public virtual void ExecuteMethod(StateTreeContext args)
        {
            try
            {
                // Ensure that state trees are initialized
                _targetTree = _targetTree ?? CreateTargetTree();
                _actionTree = _actionTree ?? CreateActionTree();
                ExecuteTargetTree(args);
            }
            catch (Exception e)
            {
                Debug.LogException(new Exception("[DualStateMethodBase] Unexpected error during dual-tree execution:", e));
                args.Complete(Response.Error($"Unexpected error during execution: {e.Message}"));
            }
        }
        /// <summary>
        /// Execute target tree
        /// </summary>
        /// <param name="args"></param>
        protected virtual void ExecuteTargetTree(StateTreeContext args)
        {
            var copyContext = new StateTreeContext(args.JsonData, args.ObjectReferences);
            // First phase：Use the target state tree to find target
            LogInfo("[DualStateMethodBase] Phase 1: Target Location");
            var targetResult = _targetTree.Run(copyContext);

            // Check errors in target locating phase
            if (targetResult == null && !string.IsNullOrEmpty(_targetTree.ErrorMessage))
            {
                Debug.LogError($"[DualStateMethodBase] Target location failed: {_targetTree.ErrorMessage}");
                args.Complete(Response.Error($"Target location failed: {_targetTree.ErrorMessage}"));
            }
            else if (targetResult != null && targetResult != copyContext)
            {
                ExecuteActiontTree(targetResult, args);
            }
            else
            {
                copyContext.RegistComplete((x) => ExecuteActiontTree(x, args));
            }
        }
        /// <summary>
        /// Execute operation tree
        /// </summary>
        /// <param name="targetResult"></param>
        /// <param name="args"></param>
        protected virtual void ExecuteActiontTree(object targetResult, StateTreeContext args)
        {
            // Handle results of target location
            var processedTarget = ProcessTargetResult(targetResult);
            if (processedTarget == null)
            {
                Debug.LogError("[DualStateMethodBase] Target processing failed or returned null:，path:" + args["path"] + " ,instance_id:" + args["instance_id"]);
                args.Complete(Json.FromObject(targetResult));
                return;
            }

            LogInfo($"[DualStateMethodBase] Target located successfully: {processedTarget?.GetType()?.Name ?? "Unknown"}");
            // Second phase：Create execution context and perform operation
            LogInfo("[DualStateMethodBase] Phase 2: Action Execution");
            args.SetObjectReference("_resolved_targets", processedTarget);

            var actionResult = _actionTree.Run(args);

            // Check errors in operation execution phase
            if (actionResult == null && !string.IsNullOrEmpty(_actionTree.ErrorMessage))
            {
                Debug.LogError($"[DualStateMethodBase] Action execution failed: {_actionTree.ErrorMessage}");
                args.Complete(Response.Error($"Action execution failed: {_actionTree.ErrorMessage}"));
                return;
            }

            LogInfo("[DualStateMethodBase] Action executed successfully");
            if (actionResult == null && !string.IsNullOrEmpty(_actionTree.ErrorMessage))
            {
                Debug.LogError($"[DualStateMethodBase] Action execution failed: {_actionTree.ErrorMessage}");
                args.Complete(Response.Error($"Action execution failed: {_actionTree.ErrorMessage}"));
            }
            // Execution finished
            else if (actionResult != null && actionResult != args)
            {
                args.Complete(Json.FromObject(actionResult));
            }
            else
            {
                // Asynchronous execution completed
                LogInfo("[DualStateMethodBase] Execution completed!");
            }
        }


        /// <summary>
        /// Handle results of target location。If target result isResponseType（That includessuccessField），Then return directly，Indicates final response。
        /// Otherwise return original target result，For subsequent operation tree processing。
        /// </summary>
        protected virtual object ProcessTargetResult(object targetResult)
        {
            // Determine if isResponseType（That includessuccessField's anonymous object）
            if (targetResult != null)
            {
                var type = targetResult.GetType();
                var successProp = type.GetProperty("success");
                if (successProp != null)
                {
                    return null;
                }
            }
            // Otherwise return original target result
            return targetResult;
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
    }
}
