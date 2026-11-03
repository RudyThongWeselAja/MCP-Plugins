package com.weselaja.integration

import android.util.Base64
import okhttp3.Interceptor
import okhttp3.Response
import okio.Buffer
import java.nio.charset.StandardCharsets
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale
import java.util.TimeZone
import javax.crypto.Mac
import javax.crypto.spec.SecretKeySpec

class WeselAjaSigningInterceptor(
    private val apiKey: String,
    private val apiSecret: String
) : Interceptor {
    override fun intercept(chain: Interceptor.Chain): Response {
        require(apiKey.isNotBlank()) { "WeselAja API Key is not configured." }
        require(apiSecret.isNotBlank()) { "WeselAja API Secret is not configured." }

        val original = chain.request()
        val timestamp = utcTimestamp()
        val path = original.url.encodedPath
        val bodyBuffer = Buffer()
        original.body?.writeTo(bodyBuffer)
        val rawBody = bodyBuffer.readUtf8()

        val stringToSign = listOf(
            original.method.uppercase(Locale.ROOT),
            path,
            timestamp,
            rawBody
        ).joinToString("\n")

        val mac = Mac.getInstance("HmacSHA256")
        mac.init(
            SecretKeySpec(
                apiSecret.toByteArray(StandardCharsets.UTF_8),
                "HmacSHA256"
            )
        )
        val signature = Base64.encodeToString(
            mac.doFinal(stringToSign.toByteArray(StandardCharsets.UTF_8)),
            Base64.NO_WRAP
        )

        val signedRequest = original.newBuilder()
            .header("Accept", "application/json")
            .header("X-API-Key", apiKey)
            .header("X-Timestamp", timestamp)
            .header("X-Signature", signature)
            .build()

        return chain.proceed(signedRequest)
    }

    private fun utcTimestamp(): String {
        val formatter = SimpleDateFormat(
            "yyyy-MM-dd'T'HH:mm:ss.SSS'Z'",
            Locale.US
        )
        formatter.timeZone = TimeZone.getTimeZone("UTC")
        return formatter.format(Date())
    }
}