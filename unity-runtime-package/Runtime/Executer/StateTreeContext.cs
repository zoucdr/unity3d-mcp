using System.Collections.Generic;
using System;
using System.Collections;

namespace UniMcp.Runtime
{
    public class StateTreeContext
    {
        public JsonClass JsonData { get; }
        public Dictionary<string, object> ObjectReferences { get; }
        public Action<JsonNode> CompleteAction { get; private set; }
        public JsonNode Result { get; set; }
        public bool IsComplete { get; private set; }

        public StateTreeContext(JsonClass jsonData = null, Dictionary<string, object> objectReferences = null)
        {
            JsonData = jsonData ?? new JsonClass();
            ObjectReferences = objectReferences ?? new Dictionary<string, object>();
        }

        public void RegistComplete(Action<JsonNode> completeAction)
        {
            CompleteAction = completeAction;
            if (Result != null)
            {
                CompleteAction?.Invoke(Result);
            }
        }

        public void Complete(JsonNode result)
        {
            if (Result == null)
            {
                Result = result;
                IsComplete = true;
                CompleteAction?.Invoke(Result);
            }
        }

        public bool TryGetJsonValue(string key, out JsonNode token)
        {
            return JsonData.TryGetValue(key, out token);
        }

        public bool TryGetObjectReference(string key, out object obj)
        {
            return ObjectReferences.TryGetValue(key, out obj);
        }

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

        public void SetJsonValue(string key, JsonNode value)
        {
            JsonData[key] = value;
        }

        public void SetObjectReference(string key, object obj)
        {
            ObjectReferences[key] = obj;
        }

        public StateTreeContext Clone()
        {
            var newJsonData = JsonData.Clone();
            var newObjectReferences = new Dictionary<string, object>(ObjectReferences);
            return new StateTreeContext(newJsonData, newObjectReferences);
        }

        public object this[string key]
        {
            get
            {
                if (JsonData.TryGetValue(key, out JsonNode token))
                {
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
                            return token;
                    }
                }

                if (ObjectReferences.TryGetValue(key, out object obj))
                {
                    return obj;
                }

                return null;
            }
            set
            {
                if (value == null)
                {
                    JsonData[key] = new JsonData("null");
                    ObjectReferences.Remove(key);
                }
                else if (IsSerializableType(value))
                {
                    JsonData[key] = Json.FromObject(value);
                    ObjectReferences.Remove(key);
                }
                else
                {
                    ObjectReferences[key] = value;
                    JsonData.Remove(key);
                }
            }
        }

        private bool IsSerializableType(object value)
        {
            if (value == null) return true;
            var type = value.GetType();
            return type.IsPrimitive || type == typeof(string) || type == typeof(decimal) ||
                   type.IsEnum || type.IsArray && type.GetElementType().IsPrimitive;
        }
    }
}
