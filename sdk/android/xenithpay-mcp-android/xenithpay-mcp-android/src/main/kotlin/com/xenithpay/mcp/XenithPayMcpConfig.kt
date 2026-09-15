package com.xenithpay.mcp

import kotlin.time.Duration
import kotlin.time.Duration.Companion.seconds

public data class XenithPayMcpConfig(
    val serverUrl: String,
    val clientName: String = "xenithpay-android",
    val clientVersion: String = "0.1.0",
    val requestTimeout: Duration = 60.seconds,
    val headers: Map<String, String> = emptyMap(),
)
