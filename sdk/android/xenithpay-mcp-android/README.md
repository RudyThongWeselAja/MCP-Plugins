# XenithPay MCP Android SDK

Android client SDK for connecting an Android application to the XenithPay MCP server.

## Current scope

- MCP client using the official Kotlin MCP SDK
- Streamable HTTP transport
- Connect / disconnect
- Ping
- List MCP tools
- Generic tool calls
- Convenience methods for XenithPay active check, payment method, and payment creation
- No direct XenithPay credentials in the Android SDK
- No middleware credentials in the Android SDK

## Dependency

The project uses the official Kotlin MCP SDK 0.15.0 and Ktor 3.5.1.

## Basic usage

```kotlin
val client = XenithPayMcpClient(
    XenithPayMcpConfig(
        serverUrl = "https://your-mcp-server.example/mcp"
    )
)

client.connect()

val tools = client.listTools()

val payment = client.createPayment(
    PaymentRequest(
        implementation = "Python",
        amount = 10000,
        currency = "IDR",
        paymentMethod = "VIRTUAL_ACCOUNT",
        paymentChannel = "BRI.VA",
        referenceCode = "TEST-ANDROID-001",
        customerReference = "CUSTOMER-ANDROID-001",
        customerName = "Daris FZ",
        callbackUrl = "https://example.com/callback",
        redirectUrl = "https://example.com/success",
        customerPhoneNumber = "081234567890",
        description = "Android MCP SDK sandbox test",
    )
)

client.disconnect()
```
