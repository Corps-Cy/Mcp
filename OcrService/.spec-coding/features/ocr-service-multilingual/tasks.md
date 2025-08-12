# 实施计划：多语言 OCR 服务

## 1. 环境搭建与依赖集成

- [x] **1.1. 添加 NuGet 包**
    - 在 `OcrService.csproj` 文件中添加对 `Tesseract` 和 `PDFtoImage` 的包引用。

- [x] **1.2. 创建 `tessdata` 目录并配置**
    - 在项目根目录手动创建一个名为 `tessdata` 的文件夹。
    - **(手动任务)** 下载 `eng.traineddata`, `chi_sim.traineddata`, `chi_tra.traineddata`, `jpn.traineddata` 文件并放入 `tessdata` 目录。
    - 编辑 `OcrService.csproj` 文件，确保 `tessdata` 目录及其内容在构建时能被复制到输出目录。

## 2. 核心类型定义与重构

- [x] **2.1. 定义 `OcrInput` 数据模型**
    - 创建一个新的 C# 文件 `Models/OcrInput.cs`。
    - 在该文件中定义 `OcrInput` 类，包含 `FilePath` (string?) 和 `FileContent` (byte[]?) 两个公共属性。
    - *关联需求: 3.1 (隐含)*

- [x] **2.2. 重构 `OcrTools` 接口**
    - 修改 `Tools/OcrTools.cs` 中的 `PerformOcr` 方法签名，使其接受 `OcrInput` 对象作为参数，并移除现有的占位符实现。
    - `public async Task<string> PerformOcr(OcrInput input)`
    - *关联需求: 2.1, 2.2, 3.1, 4.1, 4.2, 4.3*

## 3. 实现核心 OCR 处理逻辑

- [x] **3.1. 实现输入处理和文件类型分发**
    - 在 `PerformOcr` 方法中，编写逻辑来检查 `OcrInput`。优先使用 `FileContent` 创建一个 `MemoryStream`。如果 `FileContent` 为空，则使用 `FilePath` 打开一个 `FileStream`。
    - 如果两个输入都为空，则抛出 `ArgumentNullException`。
    - 添加逻辑以根据文件扩展名（从 `FilePath` 获取或假设一个默认值）来决定是调用图片处理还是 PDF 处理的私有方法。
    - *关联需求: 4.1, 4.2*

- [x] **3.2. 实现图片 OCR 功能**
    - 创建私有方法 `private string ProcessImage(Stream stream)`。
    - 在方法内，初始化 Tesseract 引擎，并指定语言为 `eng+chi_sim+chi_tra+jpn`。
    - 从输入流中加载图像并执行 OCR。
    - *关联需求: 2.1, 3.1, 3.2, 3.3, 3.4, 3.5*

- [x] **3.3. 实现 PDF OCR 功能**
    - 创建私有方法 `private string ProcessPdf(Stream stream)`。
    - 在方法内，使用 `PDFtoImage` 从流中逐页读取 PDF 并转换为 300 DPI 的图片。
    - 对转换后的每一张图片，调用 `ProcessImage` 方法进行 OCR。
    - 将所有页面的结果拼接成一个字符串并返回。
    - *关联需求: 2.2, 3.1, 3.2, 3.3, 3.4, 3.5*

## 4. 错误处理和最终集成

- [x] **4.1. 实现完整的错误处理**
    - 在 `PerformOcr` 方法外层包裹 `try-catch` 块。
    - 捕获 `FileNotFoundException`, `ArgumentException` 以及通用 `Exception`，并根据设计文档返回格式化的错误信息。
    - *关联需求: 2.1.2, 2.2.2, 4.1, 4.2, 4.3*

- [ ] **4.2. (可选) 编写单元测试**
    - 创建一个新的单元测试项目。
    - 编写针对 `OcrTools` 的单元测试，使用模拟（Mocking）来隔离文件系统和 Tesseract 引擎的依赖。
    - 测试各种输入场景，包括有效输入、文件未找到、不支持的类型以及引擎处理失败等情况。
    - *关联需求: (所有)*
