package com.xenithpay.mcp.model

public data class PaymentRequest(
    val implementation: String,
    val amount: Int,
    val currency: String,
    val paymentMethod: String,
    val paymentChannel: String,
    val referenceCode: String,
    val customerReference: String,
    val customerName: String,
    val callbackUrl: String,
    val redirectUrl: String,
    val customerPhoneNumber: String? = null,
    val description: String? = null,
) {
    init {
        require(implementation.isNotBlank()) {
            "implementation must not be blank"
        }
        require(amount > 0) {
            "amount must be greater than zero"
        }
        require(currency.isNotBlank()) {
            "currency must not be blank"
        }
        require(paymentMethod.isNotBlank()) {
            "paymentMethod must not be blank"
        }
        require(paymentChannel.isNotBlank()) {
            "paymentChannel must not be blank"
        }
        require(referenceCode.isNotBlank()) {
            "referenceCode must not be blank"
        }
        require(customerReference.isNotBlank()) {
            "customerReference must not be blank"
        }
        require(customerName.isNotBlank()) {
            "customerName must not be blank"
        }
        require(callbackUrl.isNotBlank()) {
            "callbackUrl must not be blank"
        }
        require(redirectUrl.isNotBlank()) {
            "redirectUrl must not be blank"
        }
    }

    internal fun toArguments(): Map<String, Any?> = buildMap {
        put("implementation", implementation)
        put("amount", amount)
        put("currency", currency)
        put("paymentMethod", paymentMethod)
        put("paymentChannel", paymentChannel)
        put("referenceCode", referenceCode)
        put("customerReference", customerReference)
        put("customerName", customerName)
        put("callbackUrl", callbackUrl)
        put("redirectUrl", redirectUrl)
        customerPhoneNumber?.takeIf { it.isNotBlank() }?.let {
            put("customerPhoneNumber", it)
        }
        description?.takeIf { it.isNotBlank() }?.let {
            put("description", it)
        }
    }
}
