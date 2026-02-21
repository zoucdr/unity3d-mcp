---
name: unity-request-http
description: 发送 HTTP 请求和下载/上传文件。
---

# Unity MCP: request_http

通过 `args` 传递参数。

## 参数 (args)

| 参数                 | 类型      | 必填 | 说明 |
|---------------------|-----------|------|------|
| `action`            | string    | 是   | HTTP 操作: `get`, `post`, `put`, `delete`, `download`, `upload`, `ping`, `batch_download` |
| `url`               | string    | 是   | 请求 URL |
| `method`            | string    | 否   | HTTP 方法: `GET`, `POST`, `PUT`, `DELETE`, `PATCH`, `HEAD`, `OPTIONS` |
| `data`              | object    | 否   | 请求数据 (POST/PUT 的 JSON 格式) |
| `headers`           | object    | 否   | 请求头字典 |
| `query_params`      | object    | 否   | 查询参数 (键值对) |
| `content_type`      | string    | 否   | 内容类型: `application/json`, `application/xml`, `text/plain`, `multipart/form-data`, `application/x-www-form-urlencoded` |
| `file_path`         | string    | 否   | 文件路径 (用于上传) |
| `save_path`         | string    | 否   | 保存路径 (用于下载，相对于 Assets 或绝对路径) |
| `auth_token`        | string    | 否   | Bearer 令牌 |
| `basic_auth`        | string    | 否   | 基本认证 (用户名:密码) |
| `user_agent`        | string    | 否   | 用户代理字符串 |
| `timeout`           | integer   | 否   | 超时时间(秒，默认: 30) |
| `retry_count`       | integer   | 否   | 重试次数 (默认: 0) |
| `retry_delay`       | integer   | 否   | 重试延迟(秒，默认: 1) |
| `follow_redirects`  | boolean   | 否   | 跟随重定向 (默认: true) |
| `accept_certificates`| boolean  | 否   | 接受所有证书 (用于测试) |
| `encoding`          | string    | 否   | 文本编码: `UTF-8`, `ASCII`, `Unicode`, `UTF-32` |
| `urls`              | array     | 否   | URL 数组 (用于 batch_download) |

## 示例参数

```json
{ "action": "get", "url": "https://api.example.com/data" }
```

```json
{ "action": "download", "url": "https://example.com/file.png", "save_path": "Assets/Textures/file.png" }
```

## 响应

成功: `{ "success": true, "data": ... }`. 失败: `{ "success": false, "message": "...", "error": "..." }`.
