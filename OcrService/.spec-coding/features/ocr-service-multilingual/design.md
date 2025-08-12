# 设计文档：多语言 OCR 服务

## 1. 概述

本文档为“多语言 OCR 服务”功能提供技术设计方案。该服务是一个 MCP 工具，旨在为客户端提供一个统一的接口，用于从图片和 PDF 文件中提取文本。设计将重点关注准确性、多语言支持和稳健的错误处理。

## 2. 架构

服务将遵循现有的 MCP 服务器架构。核心 OCR 逻辑将封装在 `OcrTools` 类中。为了处理依赖关系和配置，我们将利用 .NET 的依赖注入（DI）框架。

我们将采用一个简单直接的单体架构，因为服务的功能是高度集中的。主要的处理流程将在 `PerformOcr` 方法内部完成。

## 3. 组件和接口

### 3.1 `OcrTools.cs`

这是核心组件，包含所有 OCR 逻辑。

*   **`PerformOcr(OcrInput input)` 方法接口:**
    *   **输入:** `input` (OcrInput) - 一个包含文件路径或文件内容的数据对象。
    *   **输出:** `Task<string>` - 一个包含从文档中提取的所有文本的字符串。如果失败，则会抛出异常。
    *   **职责:**
        1.  验证输入源（`FileContent` 优先，然后是 `FilePath`）。
        2.  从输入源获取文件流。
        3.  判断文件类型（通过文件扩展名或内容嗅探）。
        4.  调用相应的私有方法进行处理。
        5.  返回结果或处理异常。

### 3.2 内部处理逻辑

*   **`ProcessImage(Stream stream)`:**
    *   使用 `Tesseract` 库从流中加载图片。
    *   使用配置好的多语言 Tesseract 引擎进行处理。
    *   返回识别出的文本。

*   **`ProcessPdf(Stream stream)`:**
    *   使用 `PDFtoImage` 库从流中打开 PDF 文件。
    *   遍历 PDF 的每一页。
    *   将每一页转换为 300 DPI 的内存中的图片对象。
    *   对每一张图片，调用 Tesseract 引擎进行识别。
    *   使用 `StringBuilder` 将所有页面的结果高效地拼接起来。
    *   返回合并后的文本。

### 3.3 依赖项

*   **NuGet 包:**
    *   `Tesseract`: 用于核心 OCR 功能。
    *   `PDFtoImage`: 用于将 PDF 页面转换为图片。
*   **语言数据:**
    *   一个名为 `tessdata` 的目录将存在于项目的根目录下。
    *   此目录必须包含以下文件: `eng.traineddata`, `chi_sim.traineddata`, `chi_tra.traineddata`, `jpn.traineddata`。
    *   项目构建时，需要将 `tessdata` 目录完整复制到输出目录。

## 4. 数据模型

为了支持不同的通信协议（如 `stdio` 和 `SSE`），我们将引入一个输入数据模型。

*   **`OcrInput` 类:**
    ```csharp
    public class OcrInput
    {
        // 用于 stdio 模式：文件的绝对路径
        public string? FilePath { get; set; }

        // 用于 SSE/HTTP 模式：文件的原始内容（字节数组）
        public byte[]? FileContent { get; set; }
    }
    ```

此服务的主要输出仍然是字符串（识别的文本）。所有数据都是无状态的，在单次方法调用中处理和销毁。

## 5. 错误处理

错误处理将通过标准的 C# `try-catch` 块实现，以捕获和处理可预见的异常情况。

*   **`ArgumentNullException`:** 当 `OcrInput` 及其 `FilePath` 和 `FileContent` 属性都为 null 时，将抛出此异常。
*   **`FileNotFoundException`:** 当 `FilePath` 指向的文件不存在时，将捕获此异常并返回一个内容为 "错误：文件未找到。" 的 `Exception`。
*   **`ArgumentException`:** 当无法确定文件类型或文件类型不被支持时，将抛出并返回内容为 "错误：不支持的文件类型或无法确定文件格式。" 的 `Exception`。
*   **通用 `Exception`:** 对于所有其他意外错误（例如，Tesseract 引擎初始化失败、文件损坏、PDF 渲染失败），将捕获通用异常，并返回一个内容为 "错误：处理文件时发生未知错误。" 的 `Exception`，其中会包含原始异常信息以便于调试。

## 6. 测试策略

由于这是一个独立的工具，我们将专注于单元测试。

*   **测试替身 (Mocks):** 我们不会进行真正的文件系统交互或调用 Tesseract 引擎，因为这会使测试变慢且不稳定。相反，我们会模拟这些依赖项的行为。
*   **测试场景:**
    *   **成功路径:** 测试有效的图片和 PDF 文件路径能否触发正确的处理逻辑。
    *   **错误处理:**
        *   测试当传入不存在的文件路径时，是否返回预期的 `FileNotFoundException`。
        *   测试当传入无效的文件类型时，是否返回预期的 `ArgumentException`。
        *   模拟 OCR 引擎或 PDF 转换过程中的失败，验证是否返回通用的错误信息。
    *   **PDF 处理:** 验证对于 PDF 文件，是否会尝试逐页处理。
