using System;
// Migrated from Newtonsoft.Json to SimpleJson
using UnityEngine;
using UnityMcp.Models;
namespace UnityMcp.Tools
{
    /// <summary>
    /// UnityObject selector interface
    /// Define unified object search method
    /// </summary>
    public interface IObjectSelector
    {
        /// <summary>
        /// Create list of parameter keys supported by current method
        /// </summary>
        MethodKey[] CreateKeys();
        /// <summary>
        /// Build object search state tree
        /// </summary>
        StateTree BuildStateTree();
    }
}
