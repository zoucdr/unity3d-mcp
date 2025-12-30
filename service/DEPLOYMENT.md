# 部署文档

## 环境要求

### 后端
- Go 1.21 或更高版本

### 前端
- Node.js 16 或更高版本
- npm 或 yarn

## 后端部署

### 开发环境

```bash
cd service
go run cmd/server/main.go
```

### 生产环境构建

```bash
cd service
go build -o bin/mcp-server cmd/server/main.go
./bin/mcp-server
```

### 配置

在 `cmd/server/main.go` 中修改配置：

```go
config := &mcp.McpServiceConfig{
    Port:          8080,              // 服务端口
    ServerName:    "Unity MCP Server", // 服务名称
    ServerVersion: "1.0.0",           // 服务版本
}
```

### 环境变量

可以通过环境变量配置：

```bash
export MCP_PORT=8080
export MCP_SERVER_NAME="Unity MCP Server"
export MCP_SERVER_VERSION="1.0.0"
```

### Docker 部署

创建 `Dockerfile`：

```dockerfile
FROM golang:1.21-alpine AS builder

WORKDIR /app
COPY . .
RUN go build -o mcp-server cmd/server/main.go

FROM alpine:latest
WORKDIR /root/
COPY --from=builder /app/mcp-server .
EXPOSE 8080
CMD ["./mcp-server"]
```

构建和运行：

```bash
docker build -t unity-mcp-service .
docker run -p 8080:8080 unity-mcp-service
```

## 前端部署

### 开发环境

```bash
cd service/frontend
npm install
npm run dev
```

### 生产环境构建

```bash
cd service/frontend
npm run build
```

构建产物在 `dist/` 目录。

### 静态文件服务器部署

使用 Nginx：

```nginx
server {
    listen 80;
    server_name your-domain.com;

    root /path/to/dist;
    index index.html;

    location / {
        try_files $uri $uri/ /index.html;
    }

    location /api {
        proxy_pass http://localhost:8080;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
}
```

### Docker 部署

创建 `frontend/Dockerfile`：

```dockerfile
FROM node:18-alpine AS builder

WORKDIR /app
COPY package*.json ./
RUN npm install
COPY . .
RUN npm run build

FROM nginx:alpine
COPY --from=builder /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
```

## 完整部署方案

### Docker Compose

创建 `docker-compose.yml`：

```yaml
version: '3.8'

services:
  backend:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "8080:8080"
    environment:
      - MCP_PORT=8080
      - MCP_SERVER_NAME=Unity MCP Server
      - MCP_SERVER_VERSION=1.0.0

  frontend:
    build:
      context: ./frontend
      dockerfile: Dockerfile
    ports:
      - "80:80"
    depends_on:
      - backend
```

启动：

```bash
docker-compose up -d
```

### 生产环境检查清单

- [ ] 修改默认端口
- [ ] 配置 HTTPS
- [ ] 设置 CORS 策略
- [ ] 配置日志级别
- [ ] 设置监控和告警
- [ ] 配置备份策略
- [ ] 性能优化
- [ ] 安全加固

## 监控和日志

### 后端日志

服务使用标准输出日志，可以通过以下方式查看：

```bash
# 直接运行
./bin/mcp-server

# Docker
docker logs -f unity-mcp-service

# Docker Compose
docker-compose logs -f backend
```

### 健康检查

```bash
curl http://localhost:8080/health
```

## 故障排查

### 后端无法启动

1. 检查端口是否被占用：
```bash
lsof -i :8080
```

2. 检查 Go 版本：
```bash
go version
```

3. 查看日志输出

### 前端无法连接后端

1. 检查后端是否运行：
```bash
curl http://localhost:8080/health
```

2. 检查代理配置：
```javascript
// vite.config.js
proxy: {
  '/api': {
    target: 'http://localhost:8080',
    changeOrigin: true,
    rewrite: (path) => path.replace(/^\/api/, '')
  }
}
```

3. 检查 CORS 配置

## 性能优化

### 后端优化

- 启用 Gzip 压缩
- 配置连接池
- 使用缓存
- 优化数据库查询

### 前端优化

- 代码分割
- 懒加载
- 图片优化
- CDN 加速

## 安全建议

1. 使用 HTTPS
2. 配置 CORS
3. 实现认证和授权
4. 输入验证
5. 速率限制
6. 定期更新依赖
