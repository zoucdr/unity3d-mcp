using System.Collections.Generic;
using System;
// Migrated from Newtonsoft.Json to SimpleJson
using System.Collections;
using System.Threading.Tasks;
using System.Diagnostics;


namespace UnityMcp
{
    /// <summary>
    /// StateTreeExecution context wrapper class，SupportJSONSerialized fields and non-serialized object references
    /// </summary>
    public class StateTreeContext
    {
        /// <summary>
        /// JSONSerializable parameter data（Already from JsonClass Migrate to JSONClass）
        /// </summary>
        public JsonClass JsonData { get; }

        /// <summary>
        /// Non-serialized object reference dictionary，Used for storingUnityUnserializable data such as objects
        /// </summary>
        public Dictionary<string, object> ObjectReferences { get; }

        public Action<JsonNode> CompleteAction { get; private set; }

        public JsonNode Result { get; private set; }
        /// <summary>
        /// Constructor，Based on existing JsonClass Create context
        /// </summary>
        /// <param name="jsonData">JSONData</param>
        /// <param name="objectReferences">Optional object reference dictionary</param>
        public StateTreeContext(JsonClass jsonData = null, Dictionary<string, object> objectReferences = null)
        {
            JsonData = jsonData ?? new JsonClass();
            ObjectReferences = objectReferences ?? new Dictionary<string, object>();
        }
        /// <summary>
        /// Register completion callback
        /// </summary>
        /// <param name="completeAction"></param>
        public void RegistComplete(Action<JsonNode> completeAction)
        {
            CompleteAction = completeAction;
            if (Result != null)
            {
                CompleteAction?.Invoke(Result);
            }
        }
        /// <summary>
        /// End callback
        /// </summary>
        /// <param name="result"></param>
        public void Complete(JsonNode result)
        {
            if (Result == null)
            {
                Result = result;
                CompleteAction?.Invoke(Result);
            }
        }
        /// <summary>
        /// GetJSONField value（Already from JsonNode Migrate to JSONNode）
        /// </summary>
        /// <param name="key">Field key</param>
        /// <param name="token">Output ofTokenValue</param>
        /// <returns>Whether the field is found</returns>
        public bool TryGetJsonValue(string key, out JsonNode token)
        {
            return JsonData.TryGetValue(key, out token);
        }

        /// <summary>
        /// Get object reference
        /// </summary>
        /// <param name="key">Object key</param>
        /// <param name="obj">Output object reference</param>
        /// <returns>Whether the object reference is found</returns>
        public bool TryGetObjectReference(string key, out object obj)
        {
            return ObjectReferences.TryGetValue(key, out obj);
        }

        /// <summary>
        /// Get generic version of object reference
        /// </summary>
        /// <typeparam name="T">Expected object type</typeparam>
        /// <param name="key">Object key</param>
        /// <param name="obj">Output object reference</param>
        /// <returns>Whether the object reference is found and type matches</returns>
        public bool TryGetObjectReference<T>(string key, out T obj) where T : class
        {
            if (ObjectReferences.TryGetValue(key, out object objRef) && objRef is T typedObj)
            {
                obj = typedObj;
                return true;
            }
            obj = null;
            return false;
        }

        /// <summary>
        /// SettingJSONField value（Already from JsonNode Migrate to JSONNode）
        /// </summary>
        /// <param name="key">Field key</param>
        /// <param name="value">Field value</param>
        public void SetJsonValue(string key, JsonNode value)
        {
            JsonData[key] = value;
        }

        /// <summary>
        /// Set object reference
        /// </summary>
        /// <param name="key">Object key</param>
        /// <param name="obj">Object reference</param>
        public void SetObjectReference(string key, object obj)
        {
            ObjectReferences[key] = obj;
        }

        /// <summary>
        /// Copy context，Can be used to create new context instance
        /// </summary>
        /// <returns>New context instance</returns>
        public StateTreeContext Clone()
        {
            var newJsonData = JsonData.Clone();
            var newObjectReferences = new Dictionary<string, object>(ObjectReferences);
            return new StateTreeContext(newJsonData, newObjectReferences);
        }

        /// <summary>
        /// Indexer，Support by[]Syntax accessJsonDataOrObjectReferencesValue in
        /// Priority for search：JsonData -> ObjectReferences
        /// Set rule：Base types and serializable objects -> JsonData，UnityObjects -> ObjectReferences
        /// </summary>
        /// <param name="key">Key to access</param>
        /// <returns>Found value，If not found then returnnull</returns>
        public object this[string key]
        {
            get
            {
                // Preferably searchJsonData
                if (JsonData.TryGetValue(key, out JsonNode token))
                {
                    // If it is a base type，Return value directly
                    var nodeType = token.type;
                    switch (nodeType)
                    {
                        case JsonNodeType.String:
                            return token.Value;
                        case JsonNodeType.Integer:
                            return token.AsInt;
                        case JsonNodeType.Float:
                            return token.AsFloat;
                        case JsonNodeType.Boolean:
                            return token.AsBool;
                        case JsonNodeType.Null:
                            return null;
                        default:
                            return token; // Return JsonNode Itself
                    }
                }

                // IfJsonDataNot in，SearchObjectReferences
                if (ObjectReferences.TryGetValue(key, out object obj))
                {
                    return obj;
                }

                return null; // None found
            }
            set
            {
                if (value == null)
                {
                    // nullStore value preferentially toJsonDataIn
                    JsonData[key] = new JsonData("null");
                    // Also fromObjectReferencesRemove from
                    ObjectReferences.Remove(key);
                }
                else if (IsSerializableType(value))
                {
                    // Serializable types are stored toJsonData
                    JsonData[key] = Json.FromObject(value);
                    // FromObjectReferencesRemove from
                    ObjectReferences.Remove(key);
                }
                else
                {
                    // UnityObject and other complex objects are stored toObjectReferences
                    ObjectReferences[key] = value;
                    // FromJsonDataRemove from
                    JsonData.Remove(key);
                }
            }
        }

        /// <summary>
        /// Check if the specified key exists inJsonDataOrObjectReferencesIn
        /// </summary>
        /// <param name="key">Key to check</param>
        /// <returns>Whether the key exists</returns>
        public bool ContainsKey(string key)
        {
            return JsonData.ContainsKey(key) || ObjectReferences.ContainsKey(key);
        }

        /// <summary>
        /// Try to get value（Unified method to get）
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Output value</param>
        /// <returns>Whether found the key</returns>
        public bool TryGetValue(string key, out object value)
        {
            // Prefer fromJsonDataGet
            if (JsonData.TryGetValue(key, out JsonNode token))
            {
                var nodeType = token.type;
                switch (nodeType)
                {
                    case JsonNodeType.String:
                        value = token.Value;
                        return true;
                    case JsonNodeType.Integer:
                        value = token.AsInt;
                        return true;
                    case JsonNodeType.Float:
                        value = token.AsFloat;
                        return true;
                    case JsonNodeType.Boolean:
                        value = token.AsBool;
                        return true;
                    case JsonNodeType.Null:
                        value = null;
                        return true;
                    default:
                        value = token;
                        return true;
                }
            }

            // FromObjectReferencesGet
            return ObjectReferences.TryGetValue(key, out value);
        }

        /// <summary>
        /// Try getting the value of the specified type
        /// </summary>
        /// <typeparam name="T">Expected value type</typeparam>
        /// <param name="key">Key</param>
        /// <param name="value">Output value</param>
        /// <returns>Whether the key is found and type matches</returns>
        public bool TryGetValue<T>(string key, out T value)
        {
            if (TryGetValue(key, out object obj))
            {
                if (obj is T typedValue)
                {
                    value = typedValue;
                    return true;
                }

                // Try type conversion
                try
                {
                    value = (T)Convert.ChangeType(obj, typeof(T));
                    return true;
                }
                catch
                {
                    // Conversion failed
                }
            }

            value = default(T);
            return false;
        }

        /// <summary>
        /// Remove the value for the specified key（FromJsonDataAndObjectReferencesRemove all from）
        /// </summary>
        /// <param name="key">Key to be removed</param>
        /// <returns>Whether any value was removed</returns>
        public bool Remove(string key)
        {
            bool removedFromJson = JsonData.Remove(key) != null;
            bool removedFromObjects = ObjectReferences.Remove(key);
            return removedFromJson || removedFromObjects;
        }

        /// <summary>
        /// Determine whether object is a serializable base type
        /// </summary>
        /// <param name="value">Object to check</param>
        /// <returns>Whether it is a serializable type</returns>
        private static bool IsSerializableType(object value)
        {
            if (value == null) return true;

            var type = value.GetType();

            // UnityObject type is not serializable
            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
                return false;

            // Base type is serializable
            if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal))
                return true;

            // Enum is serializable
            if (type.IsEnum)
                return true;

            // DateTimeSerializable
            if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
                return true;

            // GuidSerializable
            if (type == typeof(Guid))
                return true;

            // Array and collection（If element is serializable）
            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                return elementType != null && IsSerializableType(Activator.CreateInstance(elementType));
            }

            // Try serialization test（Use with caution，May affect performance）
            try
            {
                Json.FromObject(value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Start asynchronous coroutine
        /// </summary>
        /// <param name="coroutine">Coroutine</param>
        /// <returns>Current context instance</returns>
        public StateTreeContext AsyncReturn(IEnumerator coroutine)
        {
            if (coroutine == null)
                return this;

            // UseMainThreadExecutorTo start coroutine
            CoroutineRunner.StartCoroutine(coroutine, (result) =>
            {
                // Invoke completion callback
                UnityEngine.Debug.Log($"AsyncReturn: {result}");
                CompleteAction?.Invoke(Json.FromObject(result));
            });
            return this;
        }
    }
}