# Unity3d MCP 官方网站

这是Unity3d MCP项目的官方静态网站，用于GitHub Pages展示。

## 🎨 特性

- **现代化设计**：采用玻璃态效果、渐变背景、平滑动画
- **响应式布局**：完美适配桌面端、平板和移动端
- **交互动效**：代码打字动画、滚动视差、悬停效果
- **深色主题**：科技感十足的深色配色方案
- **性能优化**：轻量级代码，快速加载

## 📁 文件结构

```
docs/
├── index.html       # 主页面
├── styles.css       # 样式文件
├── script.js        # 交互脚本
└── README.md        # 本文件
```

## 🚀 部署到GitHub Pages

### 方法1：通过仓库设置（推荐）

1. 将 `docs` 文件夹推送到GitHub仓库
2. 进入仓库的 `Settings` → `Pages`
3. 在 `Source` 中选择 `Deploy from a branch`
4. 选择 `main` 分支和 `/docs` 文件夹
5. 点击 `Save`
6. 等待几分钟，网站将发布到 `https://yourusername.github.io/unity3d-mcp/`

### 方法2：使用GitHub Actions

创建 `.github/workflows/deploy.yml`：

```yaml
name: Deploy to GitHub Pages

on:
  push:
    branches: [ main ]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Deploy to GitHub Pages
        uses: peaceiris/actions-gh-pages@v3
        with:
          github_token: ${{ secrets.GITHUB_TOKEN }}
          publish_dir: ./docs
```

## ⚙️ 自定义配置

### 更新GitHub链接

在 `index.html` 中搜索并替换所有的占位符链接：

```html
<!-- 替换这些链接 -->
https://github.com/yourusername/unity3d-mcp
```

替换为您的实际GitHub仓库地址，例如：

```html
https://github.com/yourname/unity3d-mcp
```

### 修改颜色主题

在 `styles.css` 中的 `:root` 部分修改颜色变量：

```css
:root {
    --primary-color: #6366f1;      /* 主色调 */
    --secondary-color: #8b5cf6;    /* 次要色 */
    --accent-color: #ec4899;       /* 强调色 */
    /* ... 其他颜色 */
}
```

### 添加自定义内容

1. **添加新章节**：在 `index.html` 中复制现有 section，修改内容
2. **修改导航**：更新 `.nav-links` 中的链接
3. **更新统计数据**：修改 `.hero-stats` 中的数字
4. **调整动画**：在 `styles.css` 中修改 `@keyframes` 规则

## 🎯 本地预览

### 使用Python HTTP服务器

```bash
cd docs
python -m http.server 8000
```

然后访问 `http://localhost:8000`

### 使用Node.js serve

```bash
npm install -g serve
cd docs
serve
```

### 使用VS Code Live Server

1. 安装 "Live Server" 扩展
2. 右键点击 `index.html`
3. 选择 "Open with Live Server"

## 📱 浏览器兼容性

- ✅ Chrome 90+
- ✅ Firefox 88+
- ✅ Safari 14+
- ✅ Edge 90+
- ⚠️ IE 不支持（使用了现代CSS和JS特性）

## 🎨 设计元素

### 颜色方案

- **主色**：Indigo (#6366f1) - 专业、科技感
- **次色**：Purple (#8b5cf6) - 创意、活力
- **强调色**：Pink (#ec4899) - 吸引注意力

### 字体

- **主字体**：Inter - 现代、清晰易读
- **代码字体**：JetBrains Mono - 等宽、专业

### 动画效果

- 淡入上移动画（fade-in-up）
- 渐变背景飘动
- 网格背景移动
- 视差滚动效果
- 卡片悬停效果
- 打字机效果

## 🔧 优化建议

### 性能优化

1. **图片优化**：如需添加图片，使用WebP格式并压缩
2. **字体子集**：只加载需要的字符集
3. **延迟加载**：为大型内容添加lazy loading
4. **压缩资源**：生产环境使用压缩后的CSS和JS

### SEO优化

1. 更新 `<meta>` 标签中的描述和关键词
2. 添加 Open Graph 标签（用于社交分享）
3. 创建 `sitemap.xml` 文件
4. 添加 Google Analytics（可选）

示例 Open Graph 标签：

```html
<meta property="og:title" content="Unity3d MCP - AI驱动的Unity开发工作流">
<meta property="og:description" content="通过MCP协议将AI助手与Unity编辑器无缝连接">
<meta property="og:image" content="https://yourusername.github.io/unity3d-mcp/preview.png">
<meta property="og:url" content="https://yourusername.github.io/unity3d-mcp/">
```

## 📝 维护清单

- [ ] 更新GitHub仓库链接
- [ ] 添加实际的架构图（可选）
- [ ] 更新统计数据
- [ ] 添加更多应用场景案例
- [ ] 集成Google Analytics（可选）
- [ ] 添加评论功能（可选，如Disqus）
- [ ] 创建多语言版本（可选）

## 🤝 贡献

欢迎提交问题和改进建议！如需修改网站内容：

1. Fork 本仓库
2. 创建特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 开启 Pull Request

## 📄 许可证

本网站设计遵循项目的MIT许可证。

---

**技术栈**：HTML5 + CSS3 + Vanilla JavaScript  
**设计风格**：Modern Dark Theme with Glassmorphism  
**托管平台**：GitHub Pages

如有问题或建议，欢迎提Issue！

