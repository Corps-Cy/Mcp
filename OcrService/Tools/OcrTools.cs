using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using OcrService.Models;
using PDFtoImage;
using Tesseract;

[McpServerToolType]
internal class OcrTools
{
    [McpServerTool]
    [Description("Performs OCR on an image or PDF and returns the extracted text.")]
    public Task<string> PerformOcr(OcrInput input)
    {
        try
        {
            if (input.FileContent != null && input.FileContent.Length > 0)
            {
                using var stream = new MemoryStream(input.FileContent);
                try
                {
                    return Task.FromResult(ProcessPdf(stream));
                }
                catch
                {
                    stream.Position = 0;
                    return Task.FromResult(ProcessImage(stream));
                }
            }

            if (!string.IsNullOrEmpty(input.FilePath))
            {
                if (!File.Exists(input.FilePath))
                {
                    throw new FileNotFoundException("File not found.", input.FilePath);
                }

                var extension = Path.GetExtension(input.FilePath).ToLowerInvariant();
                using var stream = File.OpenRead(input.FilePath);

                return extension switch
                {
                    ".pdf" => Task.FromResult(ProcessPdf(stream)),
                    ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" => Task.FromResult(ProcessImage(stream)),
                    _ => throw new ArgumentException($"Unsupported file type: {extension}")
                };
            }

            throw new ArgumentNullException(nameof(input), "Either FilePath or FileContent must be provided.");
        }
        catch (Exception e)
        {
            throw new Exception("An unexpected error occurred during OCR processing.", e);
        }
    }

    private string ProcessImage(Stream stream)
    {
        try
        {
            using var engine = new TesseractEngine(@"./tessdata", "eng+chi_sim+chi_tra+jpn", EngineMode.Default);
            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            memoryStream.Position = 0;
            using var img = Pix.LoadFromMemory(memoryStream.ToArray());
            using var page = engine.Process(img);
            return page.GetText();
        }
        catch (Exception e)
        {
            throw new Exception("Tesseract OCR processing failed.", e);
        }
    }

    private string ProcessPdf(Stream stream)
    {
        try
        {
            var results = new StringBuilder();
            IEnumerable<System.Drawing.Image> images = Conversion.ToImages(stream);

            foreach (var image in images)
            {
                using var imageStream = new MemoryStream();
                image.Save(imageStream, System.Drawing.Imaging.ImageFormat.Png);
                imageStream.Position = 0;
                results.Append(ProcessImage(imageStream));
                results.AppendLine("\n---\n");
            }

            return results.ToString();
        }
        catch (Exception e)
        {
            throw new Exception("PDF processing failed.", e);
        }
    }
}
