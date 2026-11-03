package com.weselaja.integration

object WeselAjaConfig {
    const val baseUrl = "https://sandbox.checkout.weselaja.id/"
    const val apiKey = "REPLACE_WITH_SANDBOX_API_KEY"
    const val apiSecret = "REPLACE_WITH_SANDBOX_API_SECRET"
    const val customerReference = "ANDROID-TEST"
    const val payinFeeBearer = "MERCHANT"

    const val callbackUrl = ""
    const val redirectUrl = ""
    const val settlementsJson = ""

    val supportedChannels = setOf(
        "BRI.VA",
        "MDR.VA",
        "BNI.VA",
        "PTB.VA",
        "CIMB.VA",
        "BCA.VA"
    )
}