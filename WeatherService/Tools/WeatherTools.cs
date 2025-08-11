using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

/// <summary>
/// MCP tools for real weather data queries.
/// Uses Seniverse API to fetch current weather information.
/// </summary>
[McpServerToolType]
internal class WeatherTools
{
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;

    public WeatherTools()
    {
        // 创建HttpClientHandler以启用自动解压缩
        var handler = new HttpClientHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
        };
        
        _httpClient = new HttpClient(handler);
        
        // 设置请求头
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        
        _apiKey = Environment.GetEnvironmentVariable("QW_API_KEY");
        var host = Environment.GetEnvironmentVariable("QW_HOST");
        
        Console.WriteLine($"[DEBUG] WeatherTools 初始化");
        Console.WriteLine($"[DEBUG] API密钥状态: {(string.IsNullOrEmpty(_apiKey) ? "未设置" : "已设置")}");
        Console.WriteLine($"[DEBUG] Host状态: {(string.IsNullOrEmpty(host) ? "未设置" : "已设置")}");
        
        if (string.IsNullOrEmpty(_apiKey))
        {
            Console.WriteLine("警告: QW_API_KEY 环境变量未设置，天气查询功能将不可用");
            Console.WriteLine("请注册和风天气API: https://dev.qweather.com/");
        }
        
        if (string.IsNullOrEmpty(host))
        {
            Console.WriteLine("警告: QW_HOST 环境变量未设置，天气查询功能将不可用");
        }
        
        // 设置默认请求头
        if (!string.IsNullOrEmpty(_apiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("X-QW-Api-Key", _apiKey);
        }
    }

    [McpServerTool]
    [Description("获取指定城市的当前天气信息")]
    public async Task<string> GetCurrentWeather(
        [Description("城市名称 (例如: 北京, 上海, 广州)")] string city,
        [Description("温度单位 (celsius/fahrenheit, 默认: celsius)")] string units = "celsius")
    {
        Console.WriteLine($"[DEBUG] GetCurrentWeather 被调用 - 城市: {city}, 单位: {units}");
        
        if (string.IsNullOrEmpty(_apiKey))
        {
            Console.WriteLine("[DEBUG] API密钥未设置，返回错误信息");
            return "错误: 未配置 QW_API_KEY 环境变量，请设置API密钥后重试";
        }

        try
        {
            var host = Environment.GetEnvironmentVariable("QW_HOST");
            if (string.IsNullOrEmpty(host))
            {
                return "错误: 未配置 QW_HOST 环境变量，请设置API主机地址后重试";
            }
            
            // 首先获取城市ID
            var locationUrl = $"https://{host}/geo/v2/city/lookup?location={Uri.EscapeDataString(city)}";
            Console.WriteLine($"[DEBUG] 查询城市ID URL: {locationUrl}");
            
            var locationResponse = await _httpClient.GetAsync(locationUrl);
            Console.WriteLine($"[DEBUG] 城市查询响应状态码: {locationResponse.StatusCode}");
            
            if (!locationResponse.IsSuccessStatusCode)
            {
                return $"错误: 城市查询失败，状态码: {locationResponse.StatusCode}";
            }

            var locationJson = await locationResponse.Content.ReadAsStringAsync();
            Console.WriteLine($"[DEBUG] 城市查询响应JSON长度: {locationJson.Length}");
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var locationData = JsonSerializer.Deserialize<QWeatherLocationResponse>(locationJson, options);
            Console.WriteLine($"[DEBUG] 解析后的城市数据: Code={locationData?.Code}, Location数量={locationData?.Location?.Count}");
            
            if (locationData == null)
            {
                Console.WriteLine("[DEBUG] JSON反序列化失败，返回null");
                return "错误: 城市数据解析失败";
            }
            
            if (locationData?.Location == null || !locationData.Location.Any())
            {
                return $"错误: 未找到城市 '{city}'，请检查城市名称是否正确";
            }

            // 选择第一个匹配的城市（通常是主要城市）
            var location = locationData.Location.First();
            Console.WriteLine($"[DEBUG] 找到城市: {location.Name}, ID: {location.Id}");
            
            // 获取天气信息
            var weatherUrl = $"https://{host}/v7/weather/now?location={location.Id}";
            Console.WriteLine($"[DEBUG] 天气查询URL: {weatherUrl}");
            
            var weatherResponse = await _httpClient.GetAsync(weatherUrl);
            Console.WriteLine($"[DEBUG] 天气查询响应状态码: {weatherResponse.StatusCode}");
            
            if (!weatherResponse.IsSuccessStatusCode)
            {
                var errorContent = await weatherResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"[DEBUG] 错误响应内容: {errorContent}");
                return $"错误: 天气查询失败，状态码: {weatherResponse.StatusCode}";
            }

            var weatherJson = await weatherResponse.Content.ReadAsStringAsync();
            Console.WriteLine($"[DEBUG] 天气响应JSON长度: {weatherJson.Length}");
            
            var weatherData = JsonSerializer.Deserialize<QWeatherResponse>(weatherJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (weatherData?.Now == null)
            {
                Console.WriteLine("[DEBUG] 无法解析天气数据");
                return "错误: 无法解析天气数据";
            }

            var tempUnit = units switch
            {
                "celsius" => "°C",
                "fahrenheit" => "°F",
                _ => "°C"
            };

            var result = $"""
                城市信息:
                - 城市名称: {location.Name}
                - 城市ID: {location.Id}
                - 国家: {location.Country}
                - 省份: {location.Adm1}
                - 地区: {location.Adm2}
                - 纬度: {location.Lat}
                - 经度: {location.Lon}
                
                当前天气:
                - 天气状况: {weatherData.Now.Text}
                - 温度: {weatherData.Now.Temp}{tempUnit}
                - 体感温度: {weatherData.Now.FeelsLike}{tempUnit}
                - 湿度: {weatherData.Now.Humidity}%
                - 风向: {weatherData.Now.WindDir}
                - 风力等级: {weatherData.Now.WindScale}级
                - 风速: {weatherData.Now.WindSpeed} km/h
                - 能见度: {weatherData.Now.Vis} km
                - 云量: {weatherData.Now.Cloud}%
                - 观测时间: {weatherData.Now.ObsTime}
                
                详细信息链接: {weatherData.FxLink}
                """;
                
            Console.WriteLine($"[DEBUG] 成功生成天气信息");
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] 发生异常: {ex.Message}");
            Console.WriteLine($"[DEBUG] 异常堆栈: {ex.StackTrace}");
            return $"错误: 获取天气信息时发生异常 - {ex.Message}";
        }
    }



    [McpServerTool]
    [Description("获取空气质量指数")]
    public async Task<string> GetAirQuality(
        [Description("纬度 (例如: 39.92)")] string latitude,
        [Description("经度 (例如: 116.41)")] string longitude)
    {
        Console.WriteLine($"[DEBUG] GetAirQuality 被调用 - 纬度: {latitude}, 经度: {longitude}");
        
        if (string.IsNullOrEmpty(_apiKey))
        {
            return "错误: 未配置 QW_API_KEY 环境变量，请设置API密钥后重试";
        }

        try
        {
            var host = Environment.GetEnvironmentVariable("QW_HOST");
            if (string.IsNullOrEmpty(host))
            {
                return "错误: 未配置 QW_HOST 环境变量，请设置API主机地址后重试";
            }
            
            // 解析经纬度参数
            if (!double.TryParse(latitude, out var latValue) || !double.TryParse(longitude, out var lonValue))
            {
                return "错误: 经纬度参数格式不正确，请输入有效的数字";
            }
            
            // 验证经纬度参数
            if (latValue < -90 || latValue > 90)
            {
                return "错误: 纬度必须在 -90 到 90 之间";
            }
            
            if (lonValue < -180 || lonValue > 180)
            {
                return "错误: 经度必须在 -180 到 180 之间";
            }
            
            // 格式化经纬度为最多两位小数
            var lat = Math.Round(latValue, 2);
            var lon = Math.Round(lonValue, 2);
            
            // 获取空气质量数据 - 使用经纬度坐标
            var aqiUrl = $"https://{host}/airquality/v1/current/{lat}/{lon}";
            Console.WriteLine($"[DEBUG] 空气质量查询URL: {aqiUrl}");
            
            var aqiResponse = await _httpClient.GetAsync(aqiUrl);
            Console.WriteLine($"[DEBUG] 空气质量查询响应状态码: {aqiResponse.StatusCode}");
            
            if (!aqiResponse.IsSuccessStatusCode)
            {
                var errorContent = await aqiResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"[DEBUG] 错误响应内容: {errorContent}");
                return $"错误: 空气质量查询失败，状态码: {aqiResponse.StatusCode}";
            }

            var aqiJson = await aqiResponse.Content.ReadAsStringAsync();
            Console.WriteLine($"[DEBUG] 空气质量响应JSON长度: {aqiJson.Length}");
            Console.WriteLine($"[DEBUG] 空气质量响应内容: {aqiJson}");
            
            var aqiData = JsonSerializer.Deserialize<QWeatherAirQualityResponse>(aqiJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (aqiData?.Indexes == null || !aqiData.Indexes.Any())
            {
                Console.WriteLine("[DEBUG] 无法解析空气质量数据");
                return "错误: 无法解析空气质量数据";
            }

            // 获取中国标准指数（主要指数）
            var cnIndex = aqiData.Indexes.FirstOrDefault(x => x.Code == "cn-mee");
            if (cnIndex == null)
            {
                return "错误: 未找到中国标准空气质量指数";
            }

            var result = $"""
                位置信息:
                - 纬度: {lat}
                - 经度: {lon}
                
                空气质量指数:
                - AQI值: {cnIndex.AqiDisplay}
                - 等级: {cnIndex.Level}级
                - 类别: {cnIndex.Category}
                - 指数名称: {cnIndex.Name}
                
                污染物浓度详情:
                """;

            // 添加污染物信息
            if (aqiData.Pollutants != null)
            {
                foreach (var pollutant in aqiData.Pollutants)
                {
                    var concentration = pollutant.Concentration?.Value ?? 0;
                    var unit = pollutant.Concentration?.Unit ?? "";
                    result += $"- {pollutant.FullName}: {concentration} {unit}\n";
                }
            }

            result += $"""
                
                主要污染物: {cnIndex.PrimaryPollutant?.Name ?? "无"}
                健康影响: {cnIndex.Health?.Effect ?? "无"}
                普通人群建议: {cnIndex.Health?.Advice?.GeneralPopulation ?? "无"}
                敏感人群建议: {cnIndex.Health?.Advice?.SensitivePopulation ?? "无"}
                
                监测站点信息:
                """;

            // 添加监测站点信息
            if (aqiData.Stations != null)
            {
                foreach (var station in aqiData.Stations)
                {
                    result += $"- 站点ID: {station.Id}, 站点名称: {station.Name}\n";
                }
            }

            result += $"""
                
                数据来源: {aqiData.Metadata?.Tag ?? "未知"}
                """;
                
            Console.WriteLine($"[DEBUG] 成功生成空气质量信息");
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] 发生异常: {ex.Message}");
            return $"错误: 获取空气质量信息时发生异常 - {ex.Message}";
        }
    }

    [McpServerTool]
    [Description("获取天气生活指数")]
    public async Task<string> GetWeatherIndices(
        [Description("城市名称")] string city,
        [Description("指数类型 (1-洗车,2-穿衣,3-钓鱼,4-紫外线,5-运动,6-旅游,7-过敏,8-舒适度,9-感冒,10-啤酒,11-逛街,12-夜生活,13-化妆,14-晾晒,15-交通,16-防晒,17-雨伞,18-空调,19-太阳镜,20-美发)")] int type = 1)
    {
        Console.WriteLine($"[DEBUG] GetWeatherIndices 被调用 - 城市: {city}, 指数类型: {type}");
        
        if (string.IsNullOrEmpty(_apiKey))
        {
            return "错误: 未配置 QW_API_KEY 环境变量，请设置API密钥后重试";
        }

        try
        {
            var host = Environment.GetEnvironmentVariable("QW_HOST");
            if (string.IsNullOrEmpty(host))
            {
                return "错误: 未配置 QW_HOST 环境变量，请设置API主机地址后重试";
            }
            
            // 首先获取城市ID
            var locationUrl = $"https://{host}/geo/v2/city/lookup?location={Uri.EscapeDataString(city)}";
            var locationResponse = await _httpClient.GetAsync(locationUrl);
            
            if (!locationResponse.IsSuccessStatusCode)
            {
                return $"错误: 城市查询失败，状态码: {locationResponse.StatusCode}";
            }

            var locationJson = await locationResponse.Content.ReadAsStringAsync();
            Console.WriteLine($"[DEBUG] 城市查询响应内容: {locationJson}");
            var locationData = JsonSerializer.Deserialize<QWeatherLocationResponse>(locationJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (locationData?.Location == null || !locationData.Location.Any())
            {
                return $"错误: 未找到城市 '{city}'，请检查城市名称是否正确";
            }

            var location = locationData.Location.First();
            
            // 获取天气指数数据
            var indicesUrl = $"https://{host}/v7/indices/1d?type={type}&location={location.Id}";
            Console.WriteLine($"[DEBUG] 天气指数查询URL: {indicesUrl}");
            
            var indicesResponse = await _httpClient.GetAsync(indicesUrl);
            Console.WriteLine($"[DEBUG] 天气指数查询响应状态码: {indicesResponse.StatusCode}");
            
            if (!indicesResponse.IsSuccessStatusCode)
            {
                return $"错误: 天气指数查询失败，状态码: {indicesResponse.StatusCode}";
            }

            var indicesJson = await indicesResponse.Content.ReadAsStringAsync();
            Console.WriteLine($"[DEBUG] 天气指数响应JSON长度: {indicesJson.Length}");
            
            var indicesData = JsonSerializer.Deserialize<QWeatherIndicesResponse>(indicesJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (indicesData?.Daily == null || !indicesData.Daily.Any())
            {
                return "错误: 无法解析天气指数数据";
            }

            var index = indicesData.Daily.First();
            var indexName = GetIndexName(type);

            var result = $"""
                城市信息:
                - 城市名称: {location.Name}
                - 城市ID: {location.Id}
                - 国家: {location.Country}
                - 省份: {location.Adm1}
                - 地区: {location.Adm2}
                - 纬度: {location.Lat}
                - 经度: {location.Lon}
                
                天气指数详情:
                - 指数类型: {indexName}
                - 指数等级: {index.Level}级
                - 指数类别: {index.Category}
                - 指数描述: {index.Text}
                - 指数日期: {index.FxDate}
                
                当日天气信息:
                - 日出时间: {index.Sunrise}
                - 日落时间: {index.Sunset}
                - 月出时间: {index.Moonrise}
                - 月落时间: {index.Moonset}
                - 月相: {index.MoonPhase}
                - 最高温度: {index.TempMax}°C
                - 最低温度: {index.TempMin}°C
                - 白天天气: {index.TextDay}
                - 夜间天气: {index.TextNight}
                - 白天风向: {index.WindDirDay}
                - 夜间风向: {index.WindDirNight}
                - 白天风力: {index.WindScaleDay}级
                - 夜间风力: {index.WindScaleNight}级
                - 白天风速: {index.WindSpeedDay} km/h
                - 夜间风速: {index.WindSpeedNight} km/h
                - 湿度: {index.Humidity}%
                - 降水量: {index.Precip} mm
                - 气压: {index.Pressure} hPa
                - 能见度: {index.Vis} km
                - 云量: {index.Cloud}%
                - 紫外线指数: {index.UvIndex}
                
                详细信息链接: {indicesData.FxLink}
                """;
                
            Console.WriteLine($"[DEBUG] 成功生成天气指数信息");
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] 发生异常: {ex.Message}");
            return $"错误: 获取天气指数信息时发生异常 - {ex.Message}";
        }
    }

    private static string GetIndexName(int type)
    {
        return type switch
        {
            1 => "洗车指数",
            2 => "穿衣指数",
            3 => "钓鱼指数",
            4 => "紫外线指数",
            5 => "运动指数",
            6 => "旅游指数",
            7 => "过敏指数",
            8 => "舒适度指数",
            9 => "感冒指数",
            10 => "啤酒指数",
            11 => "逛街指数",
            12 => "夜生活指数",
            13 => "化妆指数",
            14 => "晾晒指数",
            15 => "交通指数",
            16 => "防晒指数",
            17 => "雨伞指数",
            18 => "空调指数",
            19 => "太阳镜指数",
            20 => "美发指数",
            _ => $"指数类型{type}"
        };
    }


}

// 和风天气API响应数据模型
public class QWeatherLocationResponse
{
    public string? Code { get; set; }
    public List<QWeatherLocation>? Location { get; set; }
    public QWeatherRefer? Refer { get; set; }
}

public class QWeatherRefer
{
    public List<string>? Sources { get; set; }
    public List<string>? License { get; set; }
}

public class QWeatherLocation
{
    public string? Name { get; set; }
    public string? Id { get; set; }
    public string? Lat { get; set; }
    public string? Lon { get; set; }
    public string? Adm1 { get; set; }
    public string? Adm2 { get; set; }
    public string? Country { get; set; }
    public string? Tz { get; set; }
    public string? UtcOffset { get; set; }
    public string? IsDst { get; set; }
    public string? Type { get; set; }
    public string? Rank { get; set; }
    public string? FxLink { get; set; }
}

public class QWeatherResponse
{
    public string? Code { get; set; }
    public QWeatherNow? Now { get; set; }
    public string? FxLink {get;set;}
}

public class QWeatherNow
{
    public string? ObsTime { get; set; }
    public string? Temp { get; set; }
    public string? FeelsLike { get; set; }
    public string? Humidity { get; set; }
    public string? Text { get; set; }
    public string? WindDir { get; set; }
    public string? WindScale { get; set; }
    public string? WindSpeed { get; set; }
    public string? Vis { get; set; }
    public string? Cloud { get; set; }
}

public class QWeatherAirQualityResponse
{
    public QWeatherAirQualityMetadata? Metadata { get; set; }
    public List<QWeatherAirQualityIndex>? Indexes { get; set; }
    public List<QWeatherAirQualityPollutant>? Pollutants { get; set; }
    public List<QWeatherAirQualityStation>? Stations { get; set; }
}

public class QWeatherAirQualityMetadata
{
    public string Tag { get; set; } = "";
}

public class QWeatherAirQualityIndex
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public double Aqi { get; set; }
    public string AqiDisplay { get; set; } = "";
    public string Level { get; set; } = "";
    public string Category { get; set; } = "";
    public QWeatherAirQualityColor? Color { get; set; }
    public QWeatherAirQualityPrimaryPollutant? PrimaryPollutant { get; set; }
    public QWeatherAirQualityHealth? Health { get; set; }
}

public class QWeatherAirQualityColor
{
    public int Red { get; set; }
    public int Green { get; set; }
    public int Blue { get; set; }
    public int Alpha { get; set; }
}

public class QWeatherAirQualityPrimaryPollutant
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string FullName { get; set; } = "";
}

public class QWeatherAirQualityHealth
{
    public string Effect { get; set; } = "";
    public QWeatherAirQualityAdvice? Advice { get; set; }
}

public class QWeatherAirQualityAdvice
{
    public string GeneralPopulation { get; set; } = "";
    public string SensitivePopulation { get; set; } = "";
}

public class QWeatherAirQualityPollutant
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string FullName { get; set; } = "";
    public QWeatherAirQualityConcentration? Concentration { get; set; }
    public List<QWeatherAirQualitySubIndex>? SubIndexes { get; set; }
}

public class QWeatherAirQualityConcentration
{
    public double Value { get; set; }
    public string Unit { get; set; } = "";
}

public class QWeatherAirQualitySubIndex
{
    public string Code { get; set; } = "";
    public double Aqi { get; set; }
    public string AqiDisplay { get; set; } = "";
}

public class QWeatherAirQualityStation
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
}

// 天气指数响应数据模型
public class QWeatherIndicesResponse
{
    public string? Code { get; set; }
    public List<QWeatherIndicesDaily>? Daily { get; set; }
    public string? FxLink { get; set; }
}

public class QWeatherIndicesDaily
{
    public string? FxDate { get; set; }
    public string? Sunrise { get; set; }
    public string? Sunset { get; set; }
    public string? Moonrise { get; set; }
    public string? Moonset { get; set; }
    public string? MoonPhase { get; set; }
    public string? MoonPhaseIcon { get; set; }
    public string? TempMax { get; set; }
    public string? TempMin { get; set; }
    public string? IconDay { get; set; }
    public string? TextDay { get; set; }
    public string? IconNight { get; set; }
    public string? TextNight { get; set; }
    public string? Wind360Day { get; set; }
    public string? WindDirDay { get; set; }
    public string? WindScaleDay { get; set; }
    public string? WindSpeedDay { get; set; }
    public string? Wind360Night { get; set; }
    public string? WindDirNight { get; set; }
    public string? WindScaleNight { get; set; }
    public string? WindSpeedNight { get; set; }
    public string? Humidity { get; set; }
    public string? Precip { get; set; }
    public string? Pressure { get; set; }
    public string? Vis { get; set; }
    public string? Cloud { get; set; }
    public string? UvIndex { get; set; }
    public string? Type { get; set; }
    public string? Name { get; set; }
    public string? Level { get; set; }
    public string? Category { get; set; }
    public string? Text { get; set; }
} 