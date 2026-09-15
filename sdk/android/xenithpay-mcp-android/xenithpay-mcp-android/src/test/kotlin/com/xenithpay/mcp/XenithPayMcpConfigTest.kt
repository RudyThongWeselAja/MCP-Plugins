package com.xenithpay.mcp

import kotlin.test.Test
import kotlin.test.assertEquals

class XenithPayMcpConfigTest {
    @Test
    fun defaultClientConfigurationIsStable() {
        val config = XenithPayMcpConfig(
            serverUrl = "https://example.com/mcp",
        )

        assertEquals("xenithpay-android", config.clientName)
        assertEquals("0.1.0", config.clientVersion)
        assertEquals(emptyMap(), config.headers)
    }
}
