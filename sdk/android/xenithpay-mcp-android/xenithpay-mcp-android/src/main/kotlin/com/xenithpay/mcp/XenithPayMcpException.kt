package com.xenithpay.mcp

public class XenithPayMcpException(
    message: String,
    cause: Throwable? = null,
) : Exception(message, cause)
