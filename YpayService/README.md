# YpayService

这是一个Ypay支付服务的MCP（Model-driven Co-programming）项目，它提供了与Ypay支付平台进行交互的工具，包括创建支付订单和查询订单状态。

## 功能

- **创建跳转支付订单**: 生成一个支付链接，用户可以点击该链接跳转到支付页面完成支付。
- **创建API支付订单**: 通过API直接创建支付订单，适用于扫码支付等场景，会返回包含二维码链接等信息的JSON数据。
- **查询订单**: 根据商户订单号或系统订单号查询订单的支付状态和详细信息。
- **显示二维码**: 创建支付订单后，可以自动在您的默认图片查看器中弹出二维码图片，无需手动操作。

## 配置与运行

### 1. 配置环境变量

在运行此服务之前，您需要在您的环境中设置以下环境变量。这是为了安全地管理您的敏感信息。

**必选环境变量:**

- `YPAY_PID`: 您的Ypay商户ID。
- `YPAY_API_KEY`: 您的Ypay API密钥。

**可选环境变量:**

- `YPAY_HOST`: 自定义Ypay API的域名。如果您有自己的Ypay服务器或需要连接到测试环境，请设置此变量。例如 `https://your-custom-pay-domain.com`。

在macOS或Linux系统中，您可以使用以下命令进行设置：

```bash
export YPAY_PID="您的商户ID"
export YPAY_API_KEY="您的API密钥"
export YPAY_HOST="您自定义的域名"
```

### 2. 运行服务

配置好环境变量后，请进入项目根目录，并执行以下命令来启动服务：

```bash
dotnet run
```

当服务成功启动后，您就可以在MCP客户端中调用此服务提供的工具了。

## 客户端配置示例

如果您的MCP客户端支持通过JSON文件配置和启动服务器，您可以使用类似以下的配置。请注意，您需要将 `env` 中的值替换为您的真实凭据。

```json
{
  "servers": {
    "YpayService": {
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/yourproject/Mcp/YpayService"
      ],
      "env": {
        "YPAY_PID": "请替换为您的商户ID",
        "YPAY_API_KEY": "请替换为您的API密钥",
        "YPAY_HOST": "您自定义的域名(可选)"
      }
    }
  }
}
```

```json
{
   "WeatherService": {
      "command": "dnx",
      "args": [
         "YpayService",
         "--version",
         "1.0.0-lts",
         "--yes",
         "--source",
         "https://nuget.abp.top/v3/index.json"
      ],
      "env": {
        "YPAY_PID": "请替换为您的商户ID",
        "YPAY_API_KEY": "请替换为您的API密钥",
        "YPAY_HOST": "您自定义的域名(可选)"
      },
      "timeout": 30000
   }
}
```

## 可用工具

### 1. `CreatePayment`

创建一个跳转支付订单，并返回一个可供用户点击跳转的支付URL。

**参数:**

- `type` (string, 必选): 支付方式，可选值为 `alipay`, `qqpay`, `wxpay`。
- `outTradeNo` (string, 必选): 您的商户订单号，需要保证唯一性。
- `notifyUrl` (string, 必选): 服务器异步通知地址，用于接收支付结果。
- `returnUrl` (string, 必选): 页面同步跳转地址，支付成功后用户会跳转到此链接。
- `name` (string, 必选): 商品名称。
- `money` (string, 必选): 支付金额。
- `siteName` (string, 可选): 您的网站名称。

### 2. `CreatePaymentQRCode`

创建一个支付订单，并直接将其支付链接转换成二维码返回。适用于控制台显示或图片调用。

**参数:**

- `type` (string, 必选): 支付方式，可选值为 `alipay`, `qqpay`, `wxpay`。
- `outTradeNo` (string, 必选): 您的商户订单号，需要保证唯一性。
- `notifyUrl` (string, 必选): 服务器异步通知地址。
- `returnUrl` (string, 必选): 页面同步跳转地址。
- `name` (string, 必选): 商品名称。
- `money` (string, 必选): 支付金额。
- `format` (string, 可选): 返回的二维码格式。可选值为 `ascii`, `base64`, `file`, 或 `show`。
  - `ascii`: 返回可在控制台显示的ASCII字符串（显示效果依赖终端）。
  - `base64`: 返回PNG图片的Base64编码字符串。
  - `file`: 将二维码图片保存到项目下的 `qrcodes` 文件夹，并返回该文件的绝对路径。
  - `show`: 自动在您的默认图片查看器中打开二维码图片。

**注意**: 使用 `file` 或 `show` 格式时，程序会自动清理 `qrcodes` 文件夹下超过5分钟的图片文件，无需手动管理。

- `siteName` (string, 可选): 您的网站名称。

### 3. `CreateApiPayment`

通过API创建一个支付订单，通常用于生成二维码等场景。调用成功后会返回一个包含支付信息的JSON字符串。

**参数:**

- `type` (string, 必选): 支付方式，可选值为 `alipay`, `qqpay`, `wxpay`。
- `outTradeNo` (string, 必选): 您的商户订单号，需要保证唯一性。
- `notifyUrl` (string, 必選): 服务器异步通知地址。
- `returnUrl` (string, 必选): 页面同步跳转地址。
- `name` (string, 必选): 商品名称。
- `money` (string, 必选): 支付金额。
- `siteName` (string, 可选): 您的网站名称。

### 4. `FindOrder`

查询一个已创建的订单的状态和详情。

**参数:**

- `orderNo` (string, 必选): 要查询的订单号。
- `type` (int, 必选): 订单号的类型。`1` 代表商户订单号，`2` 代表Ypay系统订单号。