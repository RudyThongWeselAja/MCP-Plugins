package com.weselaja.integration

import com.google.gson.Gson
import com.google.gson.JsonObject
import com.google.gson.JsonParser
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import retrofit2.Response
import java.io.IOException
import java.util.UUID

class WeselAjaPaymentRepository(
    private val api: WeselAjaApi,
    private val config: WeselAjaConfig = WeselAjaConfig
) {
    private val gson = Gson()

    suspend fun createPayment(
        amount: Long,
        customerName: String,
        phoneNumber: String?,
        paymentChannel: String,
        description: String? = null
    ): PaymentInfo = withContext(Dispatchers.IO) {
        require(amount > 0) { "Payment amount must be greater than zero." }
        require(customerName.isNotBlank()) { "Customer name is required." }
        require(paymentChannel in config.supportedChannels) {
            "Unsupported Virtual Account channel: $paymentChannel"
        }
        require(config.customerReference.isNotBlank()) {
            "Customer Reference is not configured."
        }

        val reference = "AND-${UUID.randomUUID().toString().replace("-", "").take(20).uppercase()}"
        val settlements = parseSettlements(config.settlementsJson)

        val request = CreatePayinRequest(
            initiatedAmount = amount,
            paymentChannel = paymentChannel,
            payinFeeBearer = config.payinFeeBearer,
            referenceCode = reference,
            customerReference = config.customerReference,
            customerName = customerName.trim(),
            customerPhoneNumber = phoneNumber?.trim()?.takeIf { it.isNotEmpty() },
            callbackUrl = config.callbackUrl.trim().takeIf { it.isNotEmpty() },
            redirectUrl = config.redirectUrl.trim().takeIf { it.isNotEmpty() },
            description = description?.trim()?.takeIf { it.isNotEmpty() },
            settlements = settlements
        )

        val idempotencyKey = "android-${UUID.randomUUID()}"
        val response = api.createPayin(idempotencyKey, request)

        parseResponse(response)
    }

    suspend fun getPayin(payinId: String): PaymentInfo =
        withContext(Dispatchers.IO) {
            require(payinId.isNotBlank()) {
                "Payin ID is required."
            }

            parseResponse(api.getPayin(payinId.trim()))
        }

    private fun parseSettlements(raw: String) =
        raw.trim().takeIf { it.isNotEmpty() }?.let {
            val parsed = JsonParser.parseString(it)
            require(parsed.isJsonArray) {
                "Settlements JSON must be a JSON array."
            }
            parsed.asJsonArray
        }

    private fun parseResponse(
        response: Response<JsonObject>
    ): PaymentInfo {
        if (!response.isSuccessful) {
            val detail = response.errorBody()
                ?.string()
                ?.takeIf { it.isNotBlank() }

            throw IOException(
                "WeselAja request failed with HTTP ${response.code()}: " +
                    (detail ?: response.message())
            )
        }

        val root = response.body()
            ?: throw IOException(
                "WeselAja returned an empty response."
            )

        if (
            root.get("success")
                ?.takeIf { it.isJsonPrimitive }
                ?.asBoolean == false
        ) {
            val message = root.get("message")
                ?.takeIf { it.isJsonPrimitive }
                ?.asString
                ?: root.get("error")
                    ?.takeIf { it.isJsonPrimitive }
                    ?.asString
                ?: "WeselAja rejected the request."

            throw IOException(message)
        }

        val envelopeData = root.getAsJsonObject("data")

        val data = when {
            envelopeData?.get("payin")?.isJsonObject == true ->
                envelopeData.getAsJsonObject("payin")

            envelopeData != null -> envelopeData

            root.get("payin")?.isJsonObject == true ->
                root.getAsJsonObject("payin")

            else -> root
        }

        val payment = gson.fromJson(
            data,
            PaymentInfo::class.java
        )

        if (
            payment.middlewareId.isNullOrBlank() &&
            payment.referenceCode.isNullOrBlank()
        ) {
            throw IOException(
                "WeselAja response did not include a payin ID or reference code."
            )
        }

        return payment
    }
}