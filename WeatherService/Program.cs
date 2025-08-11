using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// Configure all logs to go to stderr (stdout is used for the MCP protocol messages).
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

// Add the MCP services: the transport to use (stdio) and the tools to register.
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<WeatherTools>();

// 添加测试代码
if (args.Length > 0 && args[0] == "test")
{
    await TestWeatherTools();
    return;
}

await builder.Build().RunAsync();

// 测试函数
static async Task TestWeatherTools()
{
    Console.WriteLine("=== 开始测试天气服务接口 ===");
    
    // 设置测试环境变量
    Environment.SetEnvironmentVariable("QW_API_KEY", "0a992fc245144e48ad34de975f25068e");
    Environment.SetEnvironmentVariable("QW_HOST", "my6e4e4pxv.re.qweatherapi.com");
    
    var weatherTools = new WeatherTools();
    
    try
    {
        // 测试1: GetCurrentWeather
        Console.WriteLine("\n--- 测试1: GetCurrentWeather ---");
        var weatherResult = await weatherTools.GetCurrentWeather("北京");
        Console.WriteLine($"天气查询结果: {weatherResult}");
        
        // 测试2: GetAirQuality
        Console.WriteLine("\n--- 测试2: GetAirQuality ---");
        var airQualityResult = await weatherTools.GetAirQuality("39.92", "116.41");
        Console.WriteLine($"空气质量查询结果: {airQualityResult}");
        
        // 测试3: GetWeatherIndices
        Console.WriteLine("\n--- 测试3: GetWeatherIndices ---");
        var indicesResult = await weatherTools.GetWeatherIndices("北京", 1);
        Console.WriteLine($"天气指数查询结果: {indicesResult}");
        
        Console.WriteLine("\n=== 所有测试完成 ===");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"测试过程中发生错误: {ex.Message}");
        Console.WriteLine($"错误详情: {ex}");
    }
}
