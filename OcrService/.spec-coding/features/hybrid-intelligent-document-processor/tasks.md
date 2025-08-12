# 实施计划：混合模式智能文档处理器

## 1. 环境搭建与新模型定义

- [x] **1.1. 添加阿里云 SDK 依赖**
    - 在 `OcrService.csproj` 文件中添加对 `AlibabaCloud.SDK.Ocr-api20210707` 的包引用。

- [x] **1.2. 定义输出数据模型**
    - 创建一个新的 C# 文件 `Models/TicketExtractionResult.cs`。
    - 在该文件中定义用于序列化为最终 JSON 的相关类，至少应包括 `TicketExtractionResult`, `FieldResult`, 和 `BoundingBox`。
    - *关联需求: 2.4*

## 2. 实现阿里云增强模式

- [x] **2.1. 创建阿里云处理逻辑**
    - 在 `OcrTools.cs` 中，创建一个私有方法 `private TicketExtractionResult ProcessWithAlibabaCloud(Stream stream)`。
    - 在方法内部，初始化阿里云 OCR API 的客户端 (`AlibabaCloud.SDK.Ocr_api20210707.Client`)，需要从环境变量读取 `ALIBABA_CLOUD_ACCESS_KEY_ID` 和 `ALIBABA_CLOUD_ACCESS_KEY_SECRET`。
    - 调用阿里云的文档结构化识别接口，传入文件流。
    - *关联需求: 2.3.2*

- [x] **2.2. 实现阿里云结果解析**
    - 在 `ProcessWithAlibabaCloud` 方法中，编写逻辑来解析阿里云 API 返回的结果。
    - 将返回的字段、值、置信度和坐标映射到我们自己的 `TicketExtractionResult` 数据模型中。
    - *关联需求: 2.3.3, 2.4.2*

## 3. 实现默认本地解析模式

- [x] **3.1. 创建本地处理逻辑存根**
    - 在 `OcrTools.cs` 中，创建一个私有方法 `private TicketExtractionResult ProcessWithTesseractParsing(Stream stream)`。
    - 在方法内部，首先调用项目中已有的 `ProcessImage` 或 `ProcessPdf` 方法来获取完整的 OCR 文本。
    - *关联需求: 2.2.2*

- [x] **3.2. 实现字段解析器 (示例：总金额)**
    - 在 `ProcessWithTesseractParsing` 方法中，编写一个简单的解析器，使用正则表达式和关键词（如“总计”, “金额”, “¥”）来从 OCR 全文中查找并提取总金额。
    - 这是一个“尽力而为”的实现，我们先实现一个字段作为代表。
    - *关联需求: 2.2.2, 2.4.3*

## 4. 集成与暴露新工具

- [x] **4.1. 创建主工具方法 `ExtractTicketData`**
    - 在 `OcrTools.cs` 中，创建一个新的公开的 MCP 工具方法 `public async Task<TicketExtractionResult> ExtractTicketData(OcrInput input)`。
    - *关联需求: 2.1.1*

- [x] **4.2. 实现双模式切换逻辑**
    - 在 `ExtractTicketData` 方法中，实现检查环境变量的逻辑。
    - 如果检测到阿里云配置，则调用 `ProcessWithAlibabaCloud` 方法。
    - 否则，调用 `ProcessWithTesseractParsing` 方法。
    - *关联需求: 2.2.1, 2.3.1*

- [x] **4.3. 实现完整的错误处理**
    - 在 `ExtractTicketData` 方法外层包裹 `try-catch` 块，捕获和处理文件读取、云服务连接、解析失败等各种异常情况。
    - *关联需求: 2.1.2, 2.3.5*
