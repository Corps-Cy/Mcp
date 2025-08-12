# 多语言 OCR 服务 (OcrService MCP Tool)

这是一个实现了模型上下文协议 (MCP) 的服务端工具，提供强大的多语言光学字符识别（OCR）功能。

## 功能特性

- **多种文件格式支持:** 可识别多种图片格式 (PNG, JPEG, BMP, GIF) 和 PDF 文档。
- **多语言识别:** 支持英文 (`eng`), 简体中文 (`chi_sim`), 繁体中文 (`chi_tra`), 和日文 (`jpn`) 的混合识别。
- **灵活的输入:** 支持通过文件绝对路径或直接传递文件字节内容两种方式进行调用。

## 如何使用

### 1. 环境准备

- **安装 .NET 8.0 SDK.**
- **下载 Tesseract 语言包:**
    1.  在项目根目录（与 `OcrService.csproj` 文件同级）下，创建一个名为 `tessdata` 的文件夹。
    2.  从 [Tesseract 官方数据仓库](https://github.com/tesseract-ocr/tessdata_fast) 下载以下四个文件，并将它们放入 `tessdata` 文件夹中：
        - `eng.traineddata`
        - `chi_sim.traineddata`
        - `chi_tra.traineddata`
        - `jpn.traineddata`

### 2. 运行服务

在项目根目录下打开终端，执行以下命令：

```bash
dotnet run
```

服务将以 `stdio` 模式启动。

### 3. MCP 客户端调用示例

您可以在您的 MCP 客户端中通过以下方式调用本服务。请注意，工具的输入参数是一个 `OcrInput` 对象。

#### 示例 1: 通过文件路径调用

```csharp
// 假设 client 是一个已配置好的 McpClient 实例

var input = new 
{
    FilePath = "/path/to/your/document.pdf" // 使用文件的绝对路径
};

var ocrResult = await client.InvokeToolAsync<string>("OcrService", "PerformOcr", new { input });

Console.WriteLine(ocrResult);
```

#### 示例 2: 通过文件内容调用

当服务以网络模式（如 SSE）运行时，您可以通过传递文件字节数组来调用。

```csharp
// 假设 client 是一个已配置好的 McpClient 实例

byte[] fileBytes = await File.ReadAllBytesAsync("/path/to/your/image.png");

var input = new 
{
    FileContent = fileBytes
};

var ocrResult = await client.InvokeToolAsync<string>("OcrService", "PerformOcr", new { input });

Console.WriteLine(ocrResult);
```

## MCP 宿主配置示例

如果您正在构建一个 MCP 宿主应用（如一个开发工具或 AI Agent），您可以通过类似下面的配置来让宿主启动并管理本服务。

```json
{
  "servers": {
    "OcrService": {
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/yourproject/Mcp/OcrService/OcrService.csproj"
      ],
      "env": {}
    }
  }
}
```

**配置说明:**
- **`type`**: `stdio` 表明宿主将通过标准输入/输出与服务进行通信。
- **`command`**: `dotnet` 是用来启动服务的可执行程序。
- **`args`**: `run --project ...` 参数精确地告诉 `dotnet` 工具需要运行的项目及其 `.csproj` 文件的绝对路径，这是一种健壮的启动方式。
