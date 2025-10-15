// Migrated from Newtonsoft.Json to SimpleJson
using System.Collections;
using System.Threading.Tasks;
using UnityMcp.Models;

namespace UnityMcp
{
    /// <summary>
    /// Tool method interface，All concrete tool classes must implement this interface
    /// </summary>
    public interface IToolMethod
    {
        MethodKey[] Keys { get; }
        /// <summary>
        /// Execute tool method（Synchronous version，Remain backward compatible）
        /// </summary>
        /// <param name="args">Parameter object</param>
        /// <returns>Execution result</returns>
        void ExecuteMethod(StateTreeContext args);
        /// <summary>
        /// Preview method
        /// </summary>
        /// <returns>Preview result</returns>
        string Preview();
    }
}