package com.xenithpay.mcp.model

import kotlinx.serialization.Serializable

@Serializable
public data class PaymentResponse(
    val success: Boolean,
    val paymentId: String? = null,
    val initiatedAmount: String? = null,
    val paymentAmount: String? = null,
    val feeAmount: String? = null,
    val currency: String? = null,
    val paymentMethod: String? = null,
    val paymentChannel: String? = null,
    val paymentCode: String? = null,
    val paymentCodeType: String? = null,
    val referenceCode: String? = null,
    val customerReference: String? = null,
    val customerName: String? = null,
    val status: String? = null,
    val createdTime: String? = null,
    val updatedTime: String? = null,
    val expirationTime: String? = null,
    val description: String? = null,
    val callbackUrl: String? = null,
    val redirectUrl: String? = null,
    val payerAccountName: String? = null,
    val payerAccountNumber: String? = null,
    val payerPaymentChannel: String? = null,
    val metadata: Map<String, String> = emptyMap(),
    val error: String? = null,
)
