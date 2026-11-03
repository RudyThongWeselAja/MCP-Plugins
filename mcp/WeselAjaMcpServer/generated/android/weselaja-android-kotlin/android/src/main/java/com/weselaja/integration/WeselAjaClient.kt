package com.weselaja.integration

import okhttp3.OkHttpClient
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory
import java.util.concurrent.TimeUnit

object WeselAjaClient {
    fun create(config: WeselAjaConfig = WeselAjaConfig): WeselAjaApi {
        require(config.baseUrl.startsWith("https://", ignoreCase = true)) {
            "WeselAja API base URL must use HTTPS."
        }
        require(config.baseUrl.endsWith("/")) {
            "WeselAja API base URL must end with '/'."
        }

        val httpClient = OkHttpClient.Builder()
            .addInterceptor(
                WeselAjaSigningInterceptor(
                    apiKey = config.apiKey,
                    apiSecret = config.apiSecret
                )
            )
            .connectTimeout(20, TimeUnit.SECONDS)
            .readTimeout(30, TimeUnit.SECONDS)
            .writeTimeout(30, TimeUnit.SECONDS)
            .build()

        return Retrofit.Builder()
            .baseUrl(config.baseUrl)
            .client(httpClient)
            .addConverterFactory(GsonConverterFactory.create())
            .build()
            .create(WeselAjaApi::class.java)
    }
}