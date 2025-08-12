# 设计文档：压缩包批量处理器

## 1. 概述

本文档为“压缩包批量处理器”功能提供技术设计方案。该服务将作为一个新的 MCP 工具，接收 ZIP 压缩包，并对其中包含的每个受支持文件（图片、PDF）执行 OCR，最后将所有结果聚合为一个 JSON 数组返回。

设计的核心是**代码复用**，将完全利用项目中已有的 `PerformOcr` 功能。

## 2. 架构

我们将在现有的 `OcrTools` 类中添加一个新的 MCP 工具方法。这种方式最简单直接，可以最大限度地复用现有配置和代码。

### 2.1 新增工具方法

*   **`ProcessZipArchive(OcrInput input)` 方法接口:**
    *   **输入:** `input` (OcrInput) - 一个包含 ZIP 压缩包文件路径或文件内容的数据对象。
    *   **输出:** `Task<string>` - 一个序列化后的 JSON 数组字符串。
    *   **职责:**
        1.  从 `OcrInput` 获取文件流。
        2.  使用 `System.IO.Compression.ZipArchive` 打开压缩包。
        3.  遍历包内所有条目 (`ZipArchiveEntry`)。
        4.  对每个条目进行处理，并将结果存入一个列表。
        5.  将结果列表序列化为 JSON 字符串并返回。

### 2.2 核心处理逻辑

`ProcessZipArchive` 方法将包含以下逻辑：

1.  初始化一个 `List<ZipOcrResult>` 用于存放结果，`ZipOcrResult` 是我们将定义的一个内部数据结构。
2.  使用 `using` 语句包裹 `ZipArchive` 的初始化，以确保资源被正确释放。
3.  `foreach (var entry in archive.Entries)` 遍历所有文件。
4.  在循环内部，首先判断文件后缀名是否为支持的类型（.png, .jpg, .pdf 等）。
5.  **如果是不支持的类型**，向结果列表中添加一个 `status` 为 `Skipped` 的对象。
6.  **如果是支持的类型**，则打开该条目的流 (`entry.Open()`)，创建一个新的 `OcrInput` 对象（这次是使用 `FileContent` 属性传入文件流的字节），然后**调用 `PerformOcr` 方法**。
7.  因为 `PerformOcr` 可能会抛出异常（例如，文件损坏），所以对 `PerformOcr` 的调用需要包裹在 `try-catch` 块中。如果捕获到异常，向结果列表中添加一个 `status` 为 `Failed` 并包含错误信息的对象。
8.  如果 `PerformOcr` 成功，则向结果列表中添加一个 `status` 为 `Processed` 并包含识别文本和坐标的对象。
9.  循环结束后，使用 `System.Text.Json.JsonSerializer` 将结果列表序列化为字符串。

## 3. 数据模型

我们将定义一个新的 `ZipOcrResult` 类用于序列化为最终的 JSON 数组中的对象。

*   **`ZipOcrResult` 类:**
    ```csharp
    public class ZipOcrResult
    {
        public string FileName { get; set; }
        public string Status { get; set; }
        public string? Text { get; set; }
        public object? WordCoordinates { get; set; } // 使用 object 以便在失败时设为 null
        public string? Error { get; set; }
    }
    ```

## 4. 错误处理

*   **顶层错误:** 如果传入的不是一个有效的 ZIP 文件，`ZipArchive` 的构造函数会抛出 `InvalidDataException`。`ProcessZipArchive` 方法需要捕获此异常，并返回一个格式统一的顶层错误 JSON。
*   **内部文件错误:** 如 2.2 节所述，对 `PerformOcr` 的调用将被包裹在 `try-catch` 中，确保单个文件的失败不会中断整个批量处理流程。

## 5. 测试策略

*   创建一个包含多种文件类型（有效图片、有效PDF、不支持的文件、损坏的文件）的测试用 ZIP 压缩包。
*   编写单元测试，调用 `ProcessZipArchive` 方法并传入这个测试压缩包。
*   验证返回的 JSON 数组是否包含正确数量的条目，并且每个条目的 `status` 和内容都符合预期。
