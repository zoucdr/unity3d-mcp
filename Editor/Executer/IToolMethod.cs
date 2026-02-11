// Migrated from Newtonsoft.Json to SimpleJson
using System.Collections;
using System.Threading.Tasks;
using UniMcp.Models;

namespace UniMcp
{
    /// <summary>
    /// Tool method interface, all specific tool classes should implement this interface
    /// </summary>
    public interface IToolMethod
    {
        /// <summary>
        /// Tool method description, used for reference in state tree
        /// </summary>
        string Description { get; }
        /// <summary>
        /// Tool method keys, used for reference in state tree
        /// </summary>
        MethodKey[] Keys { get; }
        /// <summary>
        /// Execute tool method (synchronous version, maintains backward compatibility)
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