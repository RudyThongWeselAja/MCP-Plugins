package com.weselaja.integration

import com.google.gson.JsonObject
import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.Header
import retrofit2.http.POST
import retrofit2.http.Path

interface WeselAjaApi {
    @POST("v2/payins")
    suspend fun createPayin(
        @Header("X-Idempotency-Key") idempotencyKey: String,
        @Body request: CreatePayinRequest
    ): Response<JsonObject>

    @GET("v2/payins/{id}")
    suspend fun getPayin(
        @Path("id") payinId: String
    ): Response<JsonObject>
}