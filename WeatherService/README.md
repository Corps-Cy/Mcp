# 天气服务 MCP 工具

这是一个基于和风天气API的MCP（Model Context Protocol）天气查询工具。

## 🚀 快速安装

### 1. 环境准备

**安装 .NET SDK 10**
```bash
# macOS
brew install dotnet

# Windows
# 下载并安装 .NET 10 SDK: https://dotnet.microsoft.com/download

# Linux
wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y dotnet-sdk-10.0
```

**安装 MCP AI 模板**
```bash
dotnet new install Microsoft.MCP.Templates
```

### 2. 安装 MCP 服务

使用 dnx 脚本安装天气服务：

```bash
dnx WeatherService --version 1.0.0-lts --yes --source https://nuget.abp.top/v3/index.json
```

### 环境变量配置

| 变量名 | 描述 | 示例值 |
|--------|------|--------|
| `QW_API_KEY` | 和风天气API密钥 | `0a992fc245144e48ad34de975f25068e` |
| `QW_HOST` | 和风天气API主机地址 | `my6e4e4pxv.re.qweatherapi.com` |

### 获取API密钥

1. 访问 [和风天气开发者平台](https://dev.qweather.com/)
2. 注册账号并创建应用
3. 获取免费API密钥和主机地址

## 功能特性

- 🌤️ **实时天气查询**: 获取指定城市的当前天气信息
- 🌬️ **空气质量查询**: 获取指定坐标的空气质量指数（基于经纬度）
- 📊 **天气生活指数**: 获取指定城市的天气生活指数（洗车、穿衣、运动等）

## 环境变量配置

在使用天气查询功能之前，需要配置以下环境变量：

### 必需的环境变量

1. **QW_API_KEY**: 和风天气API密钥
   ```bash
   export QW_API_KEY="your_api_key_here"
   ```

2. **QW_HOST**: 和风天气API主机地址
   ```bash
   export QW_HOST="abcxyz.qweatherapi.com"
   ```

### 获取API密钥

1. 访问和风天气开发者平台: https://dev.qweather.com/
2. 注册账号并创建应用
3. 获取API密钥和主机地址

## MCP客户端安装和使用

### 1. 安装MCP客户端

#### 使用npm安装
```bash
npm install -g @modelcontextprotocol/cli
```

#### 使用yarn安装
```bash
yarn global add @modelcontextprotocol/cli
```

#### 使用pnpm安装
```bash
pnpm add -g @modelcontextprotocol/cli
```

### 2. 配置MCP服务器

创建MCP配置文件 `~/.mcp/servers.json`：

```json
{
  "mcpServers": {
    "weather-service": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/your/WeatherService"],
      "env": {
        "QW_API_KEY": "your_api_key_here",
        "QW_HOST": "your_host_here"
      }
    }
  }
}
```

### 3. 启动MCP服务器

```bash
# 进入项目目录
cd WeatherService

# 构建项目
dotnet build

# 启动MCP服务器
dotnet run
```

### 4. 测试MCP连接

```bash
# 使用MCP CLI测试连接
mcp list-servers

# 列出可用工具
mcp list-tools weather-service
```

## 使用方法

### 1. 获取城市天气信息

```csharp
// 获取北京的天气信息
var weather = await GetCurrentWeather("北京");
```

**返回信息包括：**
- 城市信息（名称、ID、国家、省份、地区、经纬度）
- 当前天气（状况、温度、体感温度、湿度、风向、风力、风速、能见度、云量）
- 观测时间
- 详细信息链接

### 2. 获取空气质量

```csharp
// 获取指定坐标的空气质量信息
var airQuality = await GetAirQuality("39.92", "116.41"); // 北京坐标
```

**返回信息包括：**
- 位置信息（经纬度）
- 空气质量指数（AQI值、等级、类别、指数名称）
- 污染物浓度详情（PM2.5、PM10、NO2、O3、SO2、CO）
- 健康影响和建议
- 监测站点信息
- 数据来源

### 3. 获取天气生活指数

```csharp
// 获取北京的洗车指数
var carWashIndex = await GetWeatherIndices("北京", 1);

// 获取北京的穿衣指数
var clothingIndex = await GetWeatherIndices("北京", 2);

// 获取北京的运动指数
var sportsIndex = await GetWeatherIndices("北京", 5);
```

**返回信息包括：**
- 城市信息（名称、ID、国家、省份、地区、经纬度）
- 天气指数详情（类型、等级、类别、描述、日期）
- 当日天气信息（日出日落、月相、温度、风向、风力、湿度、降水量、气压、能见度、云量、紫外线指数）
- 详细信息链接

## API响应示例

### 天气信息响应
```
城市信息:
- 城市名称: 北京
- 城市ID: 101010100
- 国家: 中国
- 省份: 北京市
- 地区: 北京
- 纬度: 39.90499
- 经度: 116.40529

当前天气:
- 天气状况: 多云
- 温度: 32°C
- 体感温度: 34°C
- 湿度: 55%
- 风向: 北风
- 风力等级: 2级
- 风速: 9 km/h
- 能见度: 18 km
- 云量: 91%
- 观测时间: 2025-08-07T10:41+08:00

详细信息链接: https://www.qweather.com/weather/beijing-101010100.html
```

### 空气质量响应
```
位置信息:
- 纬度: 39.92
- 经度: 116.41

空气质量指数:
- AQI值: 38
- 等级: 1级
- 类别: 优
- 指数名称: AQI (CN)

污染物浓度详情:
- 颗粒物（粒径小于等于2.5µm）: 21.14 μg/m3
- 颗粒物（粒径小于等于10µm）: 37.17 μg/m3
- 二氧化氮: 19.43 μg/m3
- 臭氧: 68.71 μg/m3
- 二氧化硫: 4.14 μg/m3
- 一氧化碳: 0.64 mg/m3

主要污染物: 无
健康影响: 空气质量令人满意，基本无空气污染。
普通人群建议: 各类人群可正常活动。
敏感人群建议: 各类人群可正常活动。

监测站点信息:
- 站点ID: P58655, 站点名称: 万寿西宫
- 站点ID: P59067, 站点名称: 天坛
- 站点ID: P58911, 站点名称: 海淀区万柳
- 站点ID: P52102, 站点名称: 东四
- 站点ID: P5257, 站点名称: 官园
- 站点ID: P57961, 站点名称: 奥体中心
- 站点ID: P56750, 站点名称: 农展馆

数据来源: 471952f5707510a0d8b420330259785f1f3d931895ea389c43b1299e2a704f7c
```

### 天气指数响应
```
城市信息:
- 城市名称: 北京
- 城市ID: 101010100
- 国家: 中国
- 省份: 北京市
- 地区: 北京
- 纬度: 39.90499
- 经度: 116.40529

天气指数详情:
- 指数类型: 洗车指数
- 指数等级: 2级
- 指数类别: 较适宜
- 指数描述: 天气较好，较适宜进行各种运动，但因天气热，请适当减少运动时间，降低运动强度。
- 指数日期: 2025-08-07

当日天气信息:
- 日出时间: 05:30
- 日落时间: 19:15
- 月出时间: 22:30
- 月落时间: 10:45
- 月相: 上弦月
- 最高温度: 35°C
- 最低温度: 25°C
- 白天天气: 晴
- 夜间天气: 多云
- 白天风向: 东北风
- 夜间风向: 北风
- 白天风力: 3级
- 夜间风力: 2级
- 白天风速: 12 km/h
- 夜间风速: 8 km/h
- 湿度: 60%
- 降水量: 0 mm
- 气压: 1013 hPa
- 能见度: 20 km
- 云量: 30%
- 紫外线指数: 8

详细信息链接: https://www.qweather.com/indices/beijing-101010100.html
```

## 错误处理

工具会处理以下常见错误：

- API密钥未配置
- 主机地址未配置
- 城市名称不存在
- 经纬度参数无效
- 网络连接问题
- API响应解析错误

## 调试信息

工具会输出详细的调试信息，包括：

- API请求URL
- 响应状态码
- 响应内容长度
- 错误详情

## API接口说明

根据[和风天气官方文档](https://dev.qweather.com/docs/api/)，本项目使用以下API接口：

1. **城市查询**: `https://{host}/geo/v2/city/lookup`
   - 根据城市名称获取城市ID和坐标信息

2. **实时天气**: `https://{host}/v7/weather/now`
   - 获取指定城市的当前天气信息

3. **空气质量**: `https://{host}/airquality/v1/current/{latitude}/{longitude}`
   - 根据经纬度坐标获取空气质量指数
   - 支持全球任意位置查询

4. **天气指数**: `https://{host}/v7/indices/1d`
   - 获取指定城市的天气生活指数

API密钥通过请求头 `X-QW-Api-Key` 传递。

### 空气质量指数说明

空气质量API返回中国标准的AQI指数，包含：

- **中国标准AQI**: 中国环境监测总站空气质量指数
- **污染物浓度**: PM2.5、PM10、NO2、O3、SO2、CO等
- **健康建议**: 针对普通人群和敏感人群的建议
- **监测站点**: 提供数据的监测站点信息

### 支持的天气指数类型

- 1: 洗车指数
- 2: 穿衣指数
- 3: 钓鱼指数
- 4: 紫外线指数
- 5: 运动指数
- 6: 旅游指数
- 7: 过敏指数
- 8: 舒适度指数
- 9: 感冒指数
- 10: 啤酒指数
- 11: 逛街指数
- 12: 夜生活指数
- 13: 化妆指数
- 14: 晾晒指数
- 15: 交通指数
- 16: 防晒指数
- 17: 雨伞指数
- 18: 空调指数
- 19: 太阳镜指数
- 20: 美发指数

## 注意事项

1. 确保网络连接正常
2. API密钥有调用次数限制，请合理使用
3. 城市名称建议使用中文，如"北京"、"上海"等
4. 空气质量查询使用经纬度坐标，支持全球任意位置
5. 经纬度参数范围：纬度(-90到90)，经度(-180到180)
6. 所有接口都返回FxLink字段，提供详细信息链接

## 技术支持

如有问题，请检查：

1. 环境变量是否正确设置
2. API密钥是否有效
3. 网络连接是否正常
4. 城市名称是否正确
5. 经纬度坐标是否在有效范围内
6. MCP客户端是否正确安装和配置

## 开发环境

- .NET 8.0
- ModelContextProtocol.Server
- System.Text.Json

## 测试

运行测试命令来验证所有接口：

```bash
dotnet run test
```

这将测试所有三个接口的功能，包括参数解析、API调用和数据解析。
