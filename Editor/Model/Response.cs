using System;
using System.Collections.Generic;
using UniMcp;

namespace UniMcp.Models
{
    /// <summary>
    /// Provides static methods for creating standardized success and error response objects.
    /// Ensures consistent Json structure for communication back to the Python server.
    /// </summary>
    public static class Response
    {
        /// <summary>
        /// Creates a standardized success response object.
        /// </summary>
        /// <param name="message">A message describing the successful operation.</param>
        /// <param name="data">Optional additional data to include in the response.</param>
        /// <returns>A JsonClass representing the success response.</returns>
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

        /// <summary>
        /// Creates a standardized success response object with any data type.
        /// </summary>
        /// <param name="message">A message describing the successful operation.</param>
        /// <param name="data">Optional additional data to include in the response.</param>
        /// <returns>A JsonClass representing the success response.</returns>
        public static JsonClass Success(string message, object data = null)
        {
            if (data != null)
            {
                return Success(message, Json.FromObject(data));
            }
            return Success(message, null);
        }

        /// <summary>
        /// Creates a standardized error response object.
        /// </summary>
        /// <param name="errorMessage">A message describing the error.</param>
        /// <param name="data">Optional additional data (e.g., error details) to include.</param>
        /// <returns>A JsonClass representing the error response.</returns>
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

        /// <summary>
        /// Creates a standardized error response object with any data type.
        /// </summary>
        /// <param name="errorMessage">A message describing the error.</param>
        /// <param name="data">Optional additional data (e.g., error details) to include.</param>
        /// <returns>A JsonClass representing the error response.</returns>
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

