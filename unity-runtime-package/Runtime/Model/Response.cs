using System;
using System.Collections.Generic;
using UniMcp.Runtime;

namespace UniMcp.Runtime
{
    [System.Serializable]
    public static class Response
    {
        public static JsonClass Success(string message, JsonNode data, JsonNode resources = null)
        {
            var response = new JsonClass();
            response.Add("success", new JsonData(true));
            response.Add("message", new JsonData(message));

            if (data != null)
            {
                response.Add("data", data);
            }

            if (resources != null)
            {
                response.Add("resources", resources);
            }

            return response;
        }

        public static JsonClass Success(string message, object data = null)
        {
            if (data != null)
            {
                return Success(message, Json.FromObject(data));
            }
            return Success(message, null);
        }

        public static JsonClass Error(string errorMessage, JsonNode data)
        {
            var response = new JsonClass();
            response.Add("success", new JsonData(false));
            response.Add("error", new JsonData(errorMessage));

            if (data != null)
            {
                response.Add("data", data);
            }
            return response;
        }

        public static JsonClass Error(string errorMessage, object data = null)
        {
            if (data != null)
            {
                return Error(errorMessage, Json.FromObject(data));
            }
            return Error(errorMessage, null);
        }
    }
}
