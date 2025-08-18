using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;
using ModelContextProtocol.Server;
using QRCoder;
using System.Drawing;
using System.IO;
using System.Diagnostics;

[McpServerToolType]
[Description("Ypay支付工具")]
public static class YpayTools
{
    private static readonly string _pid = Environment.GetEnvironmentVariable("YPAY_PID")!;
    private static readonly string _apiKey = Environment.GetEnvironmentVariable("YPAY_API_KEY")!;
    private static readonly string SubmitUrl;
    private static readonly string ApiUrl;
    private static readonly string FindOrderUrl;
    private static readonly HttpClient HttpClient = new HttpClient();

    static YpayTools()
    {
        var customHost = Environment.GetEnvironmentVariable("YPAY_HOST");
        if (!string.IsNullOrEmpty(customHost))
        {
            SubmitUrl = $"{customHost}/submit.php";
            ApiUrl = $"{customHost}/mapi.php";
            FindOrderUrl = $"{customHost}/api/findorder";
        }
        else
        {
            SubmitUrl = "https://pay.abp.top/submit.php";
            ApiUrl = "https://pay.abp.top/mapi.php";
            FindOrderUrl = "https://pay.abp.top/api/findorder";
        }
    }

    [McpServerTool]
    [Description("创建支付订单（跳转支付），返回支付URL")]
    public static async Task<string> CreatePayment(
        [Description("支付方式：alipay、qqpay、wxpay")] string type,
        [Description("商户订单号")] string outTradeNo,
        [Description("异步通知地址")] string notifyUrl,
        [Description("同步跳转地址")] string returnUrl,
        [Description("商品名称")] string name,
        [Description("金额")] string money,
        [Description("网站名称")] string? siteName = null)
    {
        return await GeneratePaymentUrl(type, outTradeNo, notifyUrl, returnUrl, name, money, siteName);
    }

    [McpServerTool]
    [Description("创建支付订单并生成二维码")]
    public static async Task<string> CreatePaymentQRCode(
        [Description("支付方式：alipay、qqpay、wxpay")] string type,
        [Description("商户订单号")] string outTradeNo,
        [Description("异步通知地址")] string notifyUrl,
        [Description("同步跳转地址")] string returnUrl,
        [Description("商品名称")] string name,
        [Description("金额")] string money,
        [Description("二维码格式：ascii, base64, file, 或 show")] string format = "show",
        [Description("网站名称")] string? siteName = null)
    {
        var jsonResponse = await CreateApiPayment(type, outTradeNo, notifyUrl, returnUrl, name, money, siteName);

        try
        {
            using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
            {
                JsonElement root = doc.RootElement;
                if (root.TryGetProperty("qrcode", out JsonElement qrCodeElement))
                {
                    string qrCodeUrl = qrCodeElement.GetString()!;

                    if (string.IsNullOrEmpty(qrCodeUrl))
                    {
                        return "错误：API返回的qrcode为空。";
                    }

                    QRCodeGenerator qrGenerator = new QRCodeGenerator();
                    QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrCodeUrl, QRCodeGenerator.ECCLevel.Q);

                    switch (format.ToLower())
                    {
                        case "ascii":
                            AsciiQRCode asciiQrCode = new AsciiQRCode(qrCodeData);
                            return asciiQrCode.GetGraphic(2);
                        case "base64":
                            PngByteQRCode pngQrCode = new PngByteQRCode(qrCodeData);
                            byte[] qrCodeAsPng = pngQrCode.GetGraphic(20);
                            return Convert.ToBase64String(qrCodeAsPng);
                        case "file":
                        case "show":
                            PngByteQRCode pngQrCodeForFile = new PngByteQRCode(qrCodeData);
                            byte[] qrCodeAsPngForFile = pngQrCodeForFile.GetGraphic(20);
                            var dir = Path.Combine(Directory.GetCurrentDirectory(), "qrcodes");
                            Directory.CreateDirectory(dir);
                            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                            string filePath = Path.Combine(dir, $"qrcode_{timestamp}.png");
                            File.WriteAllBytes(filePath, qrCodeAsPngForFile);

                            CleanOldQrCodes(dir);

                            if (format.ToLower() == "show")
                            {
                                try
                                {
                                    Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                                    return $"二维码已在您的默认图片查看器中打开: {filePath}";
                                }
                                catch (Exception ex)
                                {
                                    return $"错误：尝试自动打开图片失败: {ex.Message}。图片已保存至: {filePath}";
                                }
                            }
                            return filePath;
                        default:
                            return "无效的格式。请选择 'ascii', 'base64', 'file', 或 'show'。";
                    }
                }
                else
                {
                    return $"错误：API响应中未找到qrcode字段。响应内容：{jsonResponse}";
                }
            }
        }
        catch (JsonException ex)
        {
            return $"错误：解析API响应失败。错误信息：{ex.Message}。响应内容：{jsonResponse}";
        }
    }

    [McpServerTool]
    [Description("创建API支付订单")]
    public static async Task<string> CreateApiPayment(
        [Description("支付方式：alipay、qqpay、wxpay")] string type,
        [Description("商户订单号")] string outTradeNo,
        [Description("异步通知地址")] string notifyUrl,
        [Description("同步跳转地址")] string returnUrl,
        [Description("商品名称")] string name,
        [Description("金额")] string money,
        [Description("网站名称")] string? siteName = null)
    {
        var parameters = new Dictionary<string, string>
        {
            { "pid", _pid },
            { "type", type },
            { "out_trade_no", outTradeNo },
            { "notify_url", notifyUrl },
            { "return_url", returnUrl },
            { "name", name },
            { "money", money },
        };

        if (!string.IsNullOrEmpty(siteName))
        {
            parameters.Add("sitename", siteName);
        }

        var sortedParams = parameters.OrderBy(p => p.Key).ToDictionary(p => p.Key, p => p.Value);
        var signString = string.Join("&", sortedParams.Select(p => $"{p.Key}={p.Value}")) + _apiKey;
        var sign = GetMd5Hash(signString);

        parameters.Add("sign", sign);
        parameters.Add("sign_type", "MD5");

        var content = new FormUrlEncodedContent(parameters);
        var response = await HttpClient.PostAsync(ApiUrl, content);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    [McpServerTool]
    [Description("查询订单")]
    public static async Task<string> FindOrder(
        [Description("订单号")] string orderNo,
        [Description("订单号类型：1-商户订单号，2-系统订单号")] int type)
    {
        var parameters = new Dictionary<string, string>
        {
            { "order_no", orderNo },
            { "type", type.ToString() },
        };

        var content = new FormUrlEncodedContent(parameters);
        var response = await HttpClient.PostAsync(FindOrderUrl, content);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    private static async Task<string> GeneratePaymentUrl(string type, string outTradeNo, string notifyUrl, string returnUrl, string name, string money, string? siteName)
    {
        var parameters = new Dictionary<string, string>
        {
            { "pid", _pid },
            { "type", type },
            { "out_trade_no", outTradeNo },
            { "notify_url", notifyUrl },
            { "return_url", returnUrl },
            { "name", name },
            { "money", money },
        };

        if (!string.IsNullOrEmpty(siteName))
        {
            parameters.Add("sitename", siteName);
        }

        var sortedParams = parameters.OrderBy(p => p.Key).ToDictionary(p => p.Key, p => p.Value);
        var signString = string.Join("&", sortedParams.Select(p => $"{p.Key}={p.Value}")) + _apiKey;
        var sign = GetMd5Hash(signString);

        parameters.Add("sign", sign);
        parameters.Add("sign_type", "MD5");

        var queryString = string.Join("&", parameters.Select(p => $"{HttpUtility.UrlEncode(p.Key)}={HttpUtility.UrlEncode(p.Value)}"));
        return $"{SubmitUrl}?{queryString}";
    }

    private static string GetMd5Hash(string input)
    {
        using var md5 = MD5.Create();
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = md5.ComputeHash(inputBytes);
        return Convert.ToHexString(hashBytes).ToLower();
    }

    private static void CleanOldQrCodes(string directoryPath)
    {
        if (!Directory.Exists(directoryPath)) return;

        var files = Directory.GetFiles(directoryPath, "*.png");
        foreach (var file in files)
        {
            var creationTime = File.GetCreationTime(file);
            if (creationTime < DateTime.Now.AddMinutes(-5))
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception)
                {
                    // Ignore errors in case the file is locked
                }
            }
        }
    }
}