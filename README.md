# 🌟 MCP 工具集合

这是一个包含多种实用工具的 Model Context Protocol (MCP) 服务器集合，为AI助手提供强大的功能扩展。

## 📍 项目地址

**GitHub 仓库：** https://github.com/Corps-Cy/Mcp

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## 🛠️ 可用工具

| 工具名称 | 类型 | 描述 | 版本 |
|---------|------|------|------|
| **[WeatherService](./WeatherService/)** | 标准输入/输出(stdio) | MCP天气信息服务服务器。提供实时天气查询、空气质量指数查询等功能，使用和风天气API获取准确的天气数据 | 1.0.0-lts |
| **[OcrService](./OcrService/)** | 标准输入/输出(stdio) | MCP多语言光学字符识别（OCR）服务。支持多种图片和PDF格式，提供多语言识别功能。 | 1.0.0-lts |
| **[YpayService](./YpayService/)** | 标准输入/输出(stdio) | MCP Ypay支付服务，提供创建支付订单、查询订单状态等功能。 | 1.0.0-lts |

## 🚀 如何使用

1.  **克隆仓库**

    ```bash
    git clone https://github.com/Corps-Cy/Mcp.git
    cd Mcp
    ```

2.  **配置服务**

    每个服务可能需要单独的配置（例如，API密钥）。请参考每个服务目录下的 `README.md` 文件获取详细信息：

    - [WeatherService/README.md](./WeatherService/README.md)
    - [OcrService/README.md](./OcrService/README.md)
    - [YpayService/README.md](./YpayService/README.md)

3.  **运行服务**

    使用 `dotnet run` 命令来启动你想要运行的服务。例如，要启动 `WeatherService`：

    ```bash
    cd WeatherService
    dotnet run
    ```

## 项目结构

```
.
├── OcrService/         # OCR服务
├── WeatherService/     # 天气服务
├── YpayService/        # Ypay支付服务
├── McpSolution.sln     # Visual Studio 解决方案
└── README.md           # 本文档
```

## 📄 许可证

本项目采用 MIT 许可证 - 查看 [LICENSE](LICENSE) 文件了解详情。
