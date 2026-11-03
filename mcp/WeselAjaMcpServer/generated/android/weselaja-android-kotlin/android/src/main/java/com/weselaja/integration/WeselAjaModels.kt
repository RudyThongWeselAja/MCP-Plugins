package com.weselaja.integration

import com.google.gson.JsonArray
import com.google.gson.annotations.SerializedName

data class CreatePayinRequest(
    @SerializedName("initiatedAmount") val initiatedAmount: Long,
    @SerializedName("currency") val currency: String = "IDR",
    @SerializedName("paymentMethod") val paymentMethod: String = "VIRTUAL_ACCOUNT",
    @SerializedName("paymentChannel") val paymentChannel: String,
    @SerializedName("payinFeeBearer") val payinFeeBearer: String,
    @SerializedName("referenceCode") val referenceCode: String,
    @SerializedName("customerReference") val customerReference: String,
    @SerializedName("customerName") val customerName: String,
    @SerializedName("customerPhoneNumber") val customerPhoneNumber: String? = null,
    @SerializedName("callbackUrl") val callbackUrl: String? = null,
    @SerializedName("redirectUrl") val redirectUrl: String? = null,
    @SerializedName("description") val description: String? = null,
    @SerializedName("settlements") val settlements: JsonArray? = null
)

data class PaymentInfo(
    val id: String? = null,
    val paymentId: String? = null,
    val payinId: String? = null,
    val initiatedAmount: Long? = null,
    val paymentAmount: Long? = null,
    val feeAmount: Long? = null,
    val currency: String? = null,
    val paymentMethod: String? = null,
    val paymentChannel: String? = null,
    val paymentCode: String? = null,
    val paymentCodeType: String? = null,
    val paymentLinkUrl: String? = null,
    val paymentUrl: String? = null,
    val checkoutUrl: String? = null,
    val referenceCode: String? = null,
    val customerReference: String? = null,
    val customerName: String? = null,
    val status: String? = null,
    val createdTime: String? = null,
    val updatedTime: String? = null,
    val expirationTime: String? = null,
    val description: String? = null
) {
    val middlewareId: String?
        get() = id ?: paymentId ?: payinId

    val webViewUrl: String?
        get() = paymentLinkUrl ?: paymentUrl ?: checkoutUrl
}