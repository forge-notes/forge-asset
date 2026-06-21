# ForgeAsset

ForgeAsset V0.1 是一个极简、可本地部署的资产管理后台，面向小团队完成资产登记、维护和 A4 标签打印。项目采用前后端同仓库结构，默认使用 SQLite，无需额外数据库即可启动。

## 技术栈

- 后端：.NET 10 Minimal API、EF Core 10、JWT
- 数据库：SQLite（默认），预留 MySQL Provider 配置
- 前端：Vue 3、TypeScript、Vite、Element Plus、Vue Router、Axios
- 部署：Docker Compose、Nginx

## 目录结构

```text
forge-asset/
├── backend/ForgeAsset.Api/       # Minimal API
├── frontend/forge-asset-web/     # Vue 3 前端
├── docs/                         # 设计参考
├── docker-compose.yml
├── README.md
└── LICENSE
```

## 本地运行

环境要求：.NET SDK 10、Node.js 24+、npm 11+。

启动后端：

```bash
cd backend/ForgeAsset.Api
dotnet run
```

后端默认地址为 `http://localhost:5050`。首次运行会在后端目录自动创建 `forge_asset.db`。

另开终端启动前端：

```bash
cd frontend/forge-asset-web
npm install
npm run dev
```

浏览器打开 `http://localhost:5173`。Vite 会把 `/api` 请求代理到本地后端。

## Docker Compose 运行

```bash
cp .env.example .env
docker compose up -d
```

打开 `http://localhost:8080`。后端同时开放在 `http://localhost:5050`。SQLite 文件保存在命名卷 `forgeasset-data` 中，重新创建容器不会丢失数据。

查看日志或停止服务：

```bash
docker compose logs -f
docker compose down
```

如需同时删除 SQLite 数据卷，可执行 `docker compose down -v`。

## 默认管理员

```text
Username: admin
Password: admin123
```

项目不提供注册、用户表和多角色功能。管理员账号来自配置。生产部署前请通过 `.env` 修改 `ADMIN_PASSWORD` 和 `JWT_KEY`。

## 配置

默认 SQLite 配置位于 `backend/ForgeAsset.Api/appsettings.json`：

```json
{
  "Database": {
    "Provider": "Sqlite",
    "ConnectionString": "Data Source=forge_asset.db"
  }
}
```

项目已引用 MySQL EF Provider。切换 MySQL 时将配置改为：

```json
{
  "Database": {
    "Provider": "MySql",
    "ConnectionString": "Server=localhost;Port=3306;Database=forge_asset;User=root;Password=123456;"
  }
}
```

也可以用环境变量 `Database__Provider` 和 `Database__ConnectionString` 覆盖配置。MySQL 数据库本身需提前创建，应用会自动创建表。

## 当前功能

- 管理员 JWT 登录、刷新后保持登录、退出登录
- 未登录路由拦截，全部资产 API 强制认证
- 资产新增、列表、编辑、软删除
- 按日期生成唯一资产编号，例如 `FA-20260621-0001`
- 资产多选与 A4 打印预览
- A4 标签动态尺寸、边距与间距设置，支持按保护贴尺寸推荐并在浏览器本地保存
- SQLite 自动建库与 Docker 数据卷持久化

## API

```text
POST   /api/auth/login
GET    /api/assets
GET    /api/assets/{id}
POST   /api/assets
PUT    /api/assets/{id}
DELETE /api/assets/{id}
POST   /api/assets/generate-code
```

除登录接口外，资产接口都需要 `Authorization: Bearer <token>`。

## 后续规划

- 二维码与扫码盘点
- 多用户与角色权限
- 手持打印机
- AI 资产问答与分析
- App

## License

[MIT](LICENSE)
