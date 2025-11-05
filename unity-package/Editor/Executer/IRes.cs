// Migrated from Newtonsoft.Json to SimpleJson
using System.Collections;
using System.Threading.Tasks;
using UniMcp.Models;

namespace UniMcp
{
    /// <summary>
    /// Resources接口，所有具体Resources类都应实现此接口 
    /// </summary>
    public interface IRes
    {
        string Url { get; }
        string Name { get; }
        string Description { get; }
        string MimeType { get; }
    }
}
