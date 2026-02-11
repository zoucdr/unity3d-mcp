// Migrated from Newtonsoft.Json to SimpleJson

namespace UniMcp.Models
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
        /// The args for the command (migrated from JsonClass to JsonClass)
        /// </summary>
        public JsonNode cmd { get; set; }
    }
}

