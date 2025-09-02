using Newtonsoft.Json.Linq;

namespace UnityMcp.Models
{
    /// <summary>
    /// Represents a command received from the MCP client
    /// </summary>
    public class Command
    {
        /// <summary>
        /// The type of command to execute
        /// </summary>
        public string type { get; set; }

        /// <summary>
        /// The args for the command
        /// </summary>
        public JObject cmd { get; set; }
    }
}

