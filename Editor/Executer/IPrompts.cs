// Migrated from Newtonsoft.Json to SimpleJson
using System.Collections;
using System.Threading.Tasks;
using UniMcp.Models;


namespace UniMcp
{
    /// <summary>
    /// Prompts接口，所有具体Prompts类都应实现此接口
    /// </summary>
    public interface IPrompts
    {
        string Name { get; }
        string Description { get; }
        MethodKey[] Keys { get; }
        string PromptText { get; }
    }
}
