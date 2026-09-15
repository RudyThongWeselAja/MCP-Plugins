package com.xenithpay.mcp

import com.xenithpay.mcp.model.PaymentRequest
import com.xenithpay.mcp.model.PaymentResponse
import com.xenithpay.mcp.model.ToolInfo
import io.ktor.client.HttpClient
import io.ktor.client.engine.android.Android
import io.ktor.client.plugins.sse.SSE
import io.modelcontextprotocol.kotlin.sdk.client.Client
import io.modelcontextprotocol.kotlin.sdk.client.StreamableHttpClientTransport
import io.modelcontextprotocol.kotlin.sdk.types.CallToolResult
import io.modelcontextprotocol.kotlin.sdk.types.Implementation
import io.modelcontextprotocol.kotlin.sdk.types.TextContent
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.booleanOrNull
import kotlinx.serialization.json.jsonObject
import kotlinx.serialization.json.jsonPrimitive

public class XenithPayMcpClient(
    private val config: XenithPayMcpConfig,
    private val httpClient: HttpClient =
        HttpClient(Android) {
            install(SSE)
        },
) {
    private var client: Client? = null
    private var transport: StreamableHttpClientTransport? = null

    public val isConnected: Boolean
        get() = client?.transport != null

    public suspend fun connect() {
        if (isConnected) {
            return
        }

        require(
            config.serverUrl.isNotBlank()
        ) {
            "serverUrl must not be blank"
        }

        val mcpClient =
            Client(
                clientInfo =
                    Implementation(
                        name =
                            config.clientName,

                        version =
                            config.clientVersion,
                    )
            )

        val mcpTransport =
            StreamableHttpClientTransport(
                client =
                    httpClient,

                url =
                    config.serverUrl,

                requestBuilder = {
                    config.headers.forEach { (name, value) ->
                        headers.append(
                            name,
                            value
                        )
                    }
                },
            )

        try {
            mcpClient.connect(
                mcpTransport
            )
        } catch (error: Throwable) {
            runCatching {
                mcpClient.close()
            }

            throw XenithPayMcpException(
                "Failed to connect to MCP server: ${error.message}",
                error,
            )
        }

        client =
            mcpClient

        transport =
            mcpTransport
    }

    public suspend fun disconnect() {
        val currentClient =
            client

        client = null
        transport = null

        if (currentClient != null) {
            currentClient.close()
        }
    }

    public suspend fun close() {
        disconnect()
        httpClient.close()
    }

    public suspend fun ping(): Boolean {
        val currentClient =
            requireClient()

        return try {
            currentClient.ping()
            true
        } catch (error: Throwable) {
            throw XenithPayMcpException(
                "MCP ping failed: ${error.message}",
                error,
            )
        }
    }

    public suspend fun listTools(): List<ToolInfo> {
        val currentClient =
            requireClient()

        return currentClient
            .listTools()
            .tools
            .map { tool ->
                ToolInfo(
                    name =
                        tool.name,

                    description =
                        tool.description,
                )
            }
    }

    public suspend fun callTool(
        name: String,
        arguments: Map<String, Any?> = emptyMap(),
    ): CallToolResult {
        val currentClient =
            requireClient()

        return try {
            currentClient.callTool(
                name =
                    name,

                arguments =
                    arguments,
            )
        } catch (error: Throwable) {
            throw XenithPayMcpException(
                "MCP tool '$name' failed: ${error.message}",
                error,
            )
        }
    }

    public suspend fun isXenithPayActive(
        implementation: String,
    ): Boolean {
        val result =
            callTool(
                name =
                    "is_xenithpay_active",

                arguments =
                    mapOf(
                        "implementation" to implementation
                    ),
            )

        val json =
            resultJson(result)
                ?: throw XenithPayMcpException(
                    "MCP active check returned no JSON content."
                )

        return json
            .jsonObject["active"]
            ?.jsonPrimitive
            ?.booleanOrNull
            ?: false
    }

    public suspend fun getPaymentMethod(
        implementation: String,
    ): String {
        val result =
            callTool(
                name =
                    "get_xenithpay_payment_method",

                arguments =
                    mapOf(
                        "implementation" to implementation
                    ),
            )

        return resultJson(result)
            ?.toString()
            ?: throw XenithPayMcpException(
                "MCP payment method call returned no JSON content."
            )
    }

    public suspend fun createPayment(
        request: PaymentRequest,
    ): PaymentResponse {
        val result =
            callTool(
                name =
                    "create_xenithpay_payment",

                arguments =
                    request.toArguments(),
            )

        val json =
            resultJson(result)
                ?: throw XenithPayMcpException(
                    "MCP payment call returned no JSON content."
                )

        return try {
            Json.decodeFromString<PaymentResponse>(
                json.toString()
            )
        } catch (error: Throwable) {
            throw XenithPayMcpException(
                "Unable to parse XenithPay payment response: ${error.message}",
                error,
            )
        }
    }

    private fun requireClient(): Client =
        client
            ?: throw XenithPayMcpException(
                "MCP client is not connected. Call connect() first."
            )

    private fun resultJson(
        result: CallToolResult,
    ) =
        result.structuredContent
            ?: result.content
                .filterIsInstance<TextContent>()
                .firstOrNull()
                ?.text
                ?.let { text ->
                    runCatching {
                        Json.parseToJsonElement(
                            text
                        )
                    }.getOrNull()
                }

    @Suppress("UNUSED_VARIABLE")
    private fun negotiatedProtocolVersion(): String? {
        val currentTransport =
            transport
                ?: return null

        return currentTransport.protocolVersion
    }
}