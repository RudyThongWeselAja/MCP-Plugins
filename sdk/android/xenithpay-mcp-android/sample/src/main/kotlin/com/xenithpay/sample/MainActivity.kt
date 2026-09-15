package com.xenithpay.sample

import android.app.Activity
import android.os.Bundle
import android.widget.Button
import android.widget.LinearLayout
import android.widget.ScrollView
import android.widget.TextView
import com.xenithpay.mcp.XenithPayMcpClient
import com.xenithpay.mcp.XenithPayMcpConfig
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.cancel
import kotlinx.coroutines.launch
import kotlinx.coroutines.runBlocking

class MainActivity : Activity() {

    private val scope =
        CoroutineScope(
            SupervisorJob() +
                Dispatchers.Main
        )

    private lateinit var output: TextView
    private lateinit var testButton: Button

    private val client =
        XenithPayMcpClient(
            XenithPayMcpConfig(
                serverUrl =
                    "http://127.0.0.1:3001/mcp"
            )
        )

    override fun onCreate(
        savedInstanceState: Bundle?
    ) {
        super.onCreate(
            savedInstanceState
        )

        val root =
            LinearLayout(this).apply {
                orientation =
                    LinearLayout.VERTICAL

                setPadding(
                    32,
                    32,
                    32,
                    32
                )
            }

        val title =
            TextView(this).apply {
                text =
                    "XenithPay MCP Android Test"

                textSize = 22f
            }

        testButton =
            Button(this).apply {
                text =
                    "Test All MCP Tools"
            }

        output =
            TextView(this).apply {
                text =
                    "Ready."

                textSize = 16f
            }

        val scroll =
            ScrollView(this).apply {
                addView(output)
            }

        root.addView(title)
        root.addView(testButton)

        root.addView(
            scroll,
            LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.MATCH_PARENT,
                0,
                1f
            )
        )

        setContentView(root)

        testButton.setOnClickListener {
            testAllTools()
        }
    }

    private fun testAllTools() {
        if (client.isConnected) {
            output.text =
                "Already connected.\n"

            return
        }

        testButton.isEnabled = false
        output.text =
            "Starting MCP test...\n"

        scope.launch {
            try {

                output.append(
                    "\n[1] Connecting...\n"
                )

                client.connect()

                output.append(
                    "Connected: " +
                        "${client.isConnected}\n"
                )

                output.append(
                    "\n[2] Ping...\n"
                )

                val ping =
                    client.ping()

                output.append(
                    "Ping: $ping\n"
                )

                output.append(
                    "\n[3] List tools...\n"
                )

                val tools =
                    client.listTools()

                tools.forEach { tool ->
                    output.append(
                        "- ${tool.name}\n"
                    )
                }

                val implementation =
                    "Python"

                output.append(
                    "\n[4] is_xenithpay_active\n"
                )

                output.append(
                    "Implementation: " +
                        "$implementation\n"
                )

                val active =
                    client.isXenithPayActive(
                        implementation
                    )

                output.append(
                    "Active: $active\n"
                )

                output.append(
                    "\n[5] get_xenithpay_payment_method\n"
                )

                val paymentMethod =
                    client.getPaymentMethod(
                        implementation
                    )

                output.append(
                    "Payment Method:\n"
                )

                output.append(
                    "$paymentMethod\n"
                )


                output.append(
                    "\n[6] create_xenithpay_payment\n"
                )

                val paymentArguments =
                    mapOf<String, Any?>(
                        "implementation" to implementation,

                        "initiatedAmount" to 10000,

                        "currency" to "IDR",

                        "referenceCode" to
                            "TEST-ANDROID-PYTHON-${System.currentTimeMillis()}",

                        "customerReference" to
                            "ANDROID-TEST-001",

                        "customerName" to
                            "XenithPay Android Test",

                        "callbackUrl" to
                            "https://example.com/callback",

                        "redirectUrl" to
                            "https://example.com/success",

                        "description" to
                            "XenithPay Android MCP test"
                    )

                val paymentResult =
                    client.callTool(
                        name =
                            "create_xenithpay_payment",

                        arguments =
                            paymentArguments
                    )

                output.append(
                    "Payment Result:\n"
                )

                paymentResult.content
                    .forEach { content ->
                        output.append(
                            content.toString() +
                                "\n"
                        )
                    }

            } catch (error: Throwable) {

                output.append(
                    "\nERROR:\n" +
                        "${error.message}\n"
                )

                error.printStackTrace()
            } finally {
                testButton.isEnabled = true
            }
        }
    }

    override fun onDestroy() {

        try {
            runBlocking {
                client.close()
            }
        } catch (_: Throwable) {
        }

        scope.cancel()

        super.onDestroy()
    }
}