# 🌟 MCP 工具集合

这是一个包含多种实用工具的 Model Context Protocol (MCP) 服务器集合，为AI助手提供强大的功能扩展。

## 📍 项目地址

**GitHub 仓库：** https://github.com/Corps-Cy/Mcp

## 🚀 安装步骤

### 1. 环境准备

**安装 .NET SDK 10**
```bash
# macOS
brew install dotnet

# Windows
# 下载并安装 .NET 10 SDK: https://dotnet.microsoft.com/download

# Linux
wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y dotnet-sdk-10.0
```

**安装 MCP AI 模板**
```bash
dotnet new install Microsoft.MCP.Templates
```

### 2. 安装 MCP 服务

使用 dnx 脚本安装天气服务：

```bash
dnx WeatherService --version 1.0.0-lts --yes --source https://nuget.abp.top/v3/index.json
```

## 🛠️ 可用工具

| 工具名称 | 类型 | 描述 | 版本 | 安装命令 |
|---------|------|------|------|----------|
| **WeatherService** | 标准输入/输出(stdio) | MCP天气信息服务服务器。提供实时天气查询、空气质量指数查询等功能，使用和风天气API获取准确的天气数据 | 1.0.0-lts | `dnx WeatherService --version 1.0.0-lts --yes --source https://nuget.abp.top/v3/index.json` |

### 环境变量配置

| 变量名 | 描述 | 示例值 |
|--------|------|--------|
| `QW_API_KEY` | 和风天气API密钥 | `0a992fc245144e48ad34de975f25068e` |
| `QW_HOST` | 和风天气API主机地址 | `my6e4e4pxv.re.qweatherapi.com` |

### 获取API密钥

1. 访问 [和风天气开发者平台](https://dev.qweather.com/)
2. 注册账号并创建应用
3. 获取免费API密钥和主机地址

## 📄 许可证

本项目采用 MIT 许可证 - 查看 [LICENSE](LICENSE) 文件了解详情。

---

<div align="center">

**⭐ 如果这个项目对您有帮助，请给我们一个星标！**

</div>
