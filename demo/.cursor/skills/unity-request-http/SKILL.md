---
name: unity-request-http
description: Send HTTP requests and download/upload files.
---

# Unity MCP: request_http

Pass parameters in `args`.

## Parameters (args)

| Param               | Type      | Required | Description |
|--------------------|-----------|----------|-------------|
| `action`           | string    | yes      | HTTP operation: `get`, `post`, `put`, `delete`, `download`, `upload`, `ping`, `batch_download` |
| `url`              | string    | yes      | Request URL |
| `method`           | string    | no       | HTTP method: `GET`, `POST`, `PUT`, `DELETE`, `PATCH`, `HEAD`, `OPTIONS` |
| `data`             | object    | no       | Request data (JSON format for POST/PUT) |
| `headers`          | object    | no       | Request headers dictionary |
| `query_params`     | object    | no       | Query parameters (key-value) |
| `content_type`     | string    | no       | Content type: `application/json`, `application/xml`, `text/plain`, `multipart/form-data`, `application/x-www-form-urlencoded` |
| `file_path`        | string    | no       | File path (for upload) |
| `save_path`        | string    | no       | Save path (for download, relative to Assets or absolute) |
| `auth_token`       | string    | no       | Bearer token |
| `basic_auth`       | string    | no       | Basic auth (username:password) |
| `user_agent`       | string    | no       | User agent string |
| `timeout`          | integer   | no       | Timeout in seconds (default: 30) |
| `retry_count`      | integer   | no       | Retry count (default: 0) |
| `retry_delay`      | integer   | no       | Retry delay in seconds (default: 1) |
| `follow_redirects` | boolean   | no       | Follow redirects (default: true) |
| `accept_certificates`| boolean | no       | Accept all certificates (for testing) |
| `encoding`         | string    | no       | Text encoding: `UTF-8`, `ASCII`, `Unicode`, `UTF-32` |
| `urls`             | array     | no       | URL array (for batch_download) |

## Example args

```json
{ "action": "get", "url": "https://api.example.com/data" }
```

```json
{ "action": "download", "url": "https://example.com/file.png", "save_path": "Assets/Textures/file.png" }
```

## Response

Success: `{ "success": true, "data": ... }`. Failure: `{ "success": false, "message": "...", "error": "..." }`.
