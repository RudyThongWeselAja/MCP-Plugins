
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WeselAjaMcpServer.Generators;

public sealed class AndroidKotlinWeselAjaGenerator
{
    private const string TargetDirectory = "android";
    private const string DefaultProjectName = "weselaja-android-kotlin";

    private static readonly string[] RequiredFiles =
    [
        ".gitignore",
        "README.md",
        "android/AndroidManifest.snippet.xml",
        "android/src/main/java/com/weselaja/integration/WeselAjaModels.kt",
        "android/src/main/java/com/weselaja/integration/WeselAjaApi.kt",
        "android/src/main/java/com/weselaja/integration/WeselAjaSigningInterceptor.kt",
        "android/src/main/java/com/weselaja/integration/WeselAjaClient.kt",
        "android/src/main/java/com/weselaja/integration/WeselAjaPaymentRepository.kt",
        "android/src/main/java/com/weselaja/integration/WeselAjaConfig.kt",
        "android/src/main/java/com/weselaja/integration/WeselAjaDemoActivity.kt",
        "android/src/main/java/com/weselaja/integration/WeselAjaWebViewActivity.kt"
    ];

    private static readonly Dictionary<string, string> TemplateFiles =
        new(StringComparer.Ordinal)
        {
            [".gitignore"] = """
                .idea/
                .gradle/
                build/
                **/build/
                local.properties
                *.iml
                """,

            ["README.md"] = """
                # WeselAja Android Kotlin Direct Payin

                Android Kotlin integration sample that calls the WeselAja middleware directly with Retrofit. It does not call or require an application backend.

                ## What is generated

                - Retrofit API for `POST /v2/payins` and `GET /v2/payins/{id}`.
                - OkHttp interceptor that signs the exact HTTP method, encoded path, UTC timestamp, and raw request body.
                - Direct Payin request models for Virtual Account payments.
                - A simple native Android form for amount, customer name, phone number, and bank.
                - A result panel for status, reference, payment ID, and Virtual Account payment code.
                - A WebView activity. It opens a checkout URL only when the Direct Payin response actually contains a supported URL field.
                - Manual testing checklist.

                ## Important security warning

                This direct-from-Android mode requires the WeselAja API key and API secret to be available inside the Android process so the app can sign requests. Values compiled into an APK can be extracted by a determined user, even when placed in BuildConfig, resources, native libraries, or obfuscated code.

                Use this implementation for sandbox testing, internal demonstrations, or a controlled environment only. Do not distribute an APK containing a production API secret to the public. For production, use a trusted backend or a provider-supported client-safe credential/token flow.

                The Android app cannot receive a server-to-server webhook because a device is not a publicly addressable webhook endpoint. This sample checks payment status by calling `GET /v2/payins/{id}` from the app. A verified webhook still requires a public server endpoint.

                ## Requirements

                - An existing Android project using Kotlin.
                - Android SDK with WebView support.
                - Internet permission.
                - Retrofit, Gson converter, OkHttp, and AndroidX lifecycle runtime KTX.

                ## Dependencies

                Add compatible versions to the app module's `build.gradle.kts`:

                ```kotlin
                dependencies {
                    implementation("com.squareup.retrofit2:retrofit:2.11.0")
                    implementation("com.squareup.retrofit2:converter-gson:2.11.0")
                    implementation("com.squareup.okhttp3:okhttp:4.12.0")
                    implementation("androidx.lifecycle:lifecycle-runtime-ktx:2.8.7")
                }
                ```

                Use versions compatible with the Android project if it already manages dependencies through a version catalog.

                ## Configure the middleware

                Edit `WeselAjaConfig.kt`:

                - `BASE_URL`: sandbox `https://sandbox.checkout.weselaja.id/` or the production base URL supplied for the merchant.
                - `API_KEY`: WeselAja API key.
                - `API_SECRET`: WeselAja API secret used to calculate `X-Signature`.
                - `CUSTOMER_REFERENCE`: merchant customer reference.
                - `CALLBACK_URL`: optional public callback URL if the merchant/API requires one. It must point to a real public server endpoint; an Android activity cannot receive a webhook.
                - `REDIRECT_URL`: optional return URL configured for the merchant. Set it only to a URL accepted by the middleware configuration.
                - `SETTLEMENTS_JSON`: optional JSON array for merchants that must explicitly specify settlement accounts. Use the exact structure and account identifiers supplied by WeselAja; do not guess these values.
                - `PAYIN_FEE_BEARER`: use the value configured for the merchant (sample default is `MERCHANT`).

                Do not commit real credentials. Remember that moving credentials to a local Kotlin file only avoids committing them; it does not protect them from extraction from a distributed APK.

                ## Add files to your Android app

                1. Copy the generated `android/src/main/java/com/weselaja/integration` Kotlin files into the app module's source set.
                2. Keep the package `com.weselaja.integration`, or update all package declarations and file locations together.
                3. Add the dependencies above.
                4. Merge the entries from `AndroidManifest.snippet.xml` into the existing manifest. Do not create a duplicate activity declaration. Add the Internet permission outside the `<application>` element.
                5. Open `WeselAjaDemoActivity` from your app's existing navigation or set it as the launcher activity for a standalone demo.
                6. Configure the sandbox credentials and merchant settings in `WeselAjaConfig.kt`.
                7. Build and run on a device or emulator with network access.

                ## Direct Payin flow

                1. The user fills the form and chooses a bank channel.
                2. `WeselAjaPaymentRepository` creates a reference and idempotency key.
                3. Retrofit sends `POST /v2/payins` directly to the WeselAja middleware.
                4. `WeselAjaSigningInterceptor` signs the exact outgoing request using HMAC-SHA256 and sends `X-API-Key`, `X-Timestamp`, and `X-Signature`.
                5. The app displays the payment code and current status.
                6. The user can tap Check Status to call `GET /v2/payins/{id}`.
                7. If the response includes a checkout URL (`paymentLinkUrl`, `paymentUrl`, or `checkoutUrl`), the app can open it in `WeselAjaWebViewActivity`. A Direct Virtual Account response may contain only a payment code; in that case there is no hosted checkout page to show, so the app displays the VA code instead.

                ## Callback and status

                No callback server is generated. Direct Android-to-middleware integration uses status polling from the app. If the merchant requires callback notifications, configure a public HTTPS webhook endpoint outside the Android application and verify the webhook signature there. Never trust a status passed back through a deep link or WebView URL as proof of payment.

                ## Documentation

                - [Create Payin Direct](https://docs.checkout.weselaja.id/payment#create-payin-direct)
                - [WeselAja Middleware Documentation](https://docs.checkout.weselaja.id/)

                ## Troubleshooting

                - HTTP 401/403: verify sandbox/production credentials, timestamp format, API secret, and request-signature format.
                - HTTP 400: inspect the middleware's validation message. Merchants with multiple settlement accounts may need the exact `settlements` array provided by WeselAja.
                - Network failure: confirm the emulator/device has Internet access and that the base URL is correct.
                - No WebView button: the Direct Payin response did not include a hosted checkout URL. Use the returned Virtual Account payment code and check status through the middleware.
                """,

            ["android/AndroidManifest.snippet.xml"] = """
                <!-- Add this permission outside the <application> element. -->
                <uses-permission android:name="android.permission.INTERNET" />

                <!-- Merge these activities inside the existing <application> element. -->
                <activity
                    android:name="com.weselaja.integration.WeselAjaWebViewActivity"
                    android:exported="false" />

                <activity
                    android:name="com.weselaja.integration.WeselAjaDemoActivity"
                    android:exported="true">
                    <!-- Optional: receive a configured app return deep link. -->
                    <intent-filter>
                        <action android:name="android.intent.action.VIEW" />
                        <category android:name="android.intent.category.DEFAULT" />
                        <category android:name="android.intent.category.BROWSABLE" />
                        <data
                            android:scheme="weselaja"
                            android:host="payment"
                            android:path="/return" />
                    </intent-filter>
                </activity>
                """,

            ["android/src/main/java/com/weselaja/integration/WeselAjaModels.kt"] = """
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
                """,

            ["android/src/main/java/com/weselaja/integration/WeselAjaApi.kt"] = """
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
                """,

            ["android/src/main/java/com/weselaja/integration/WeselAjaSigningInterceptor.kt"] = """
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
                """,

            ["android/src/main/java/com/weselaja/integration/WeselAjaClient.kt"] = """
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
                """,

            ["android/src/main/java/com/weselaja/integration/WeselAjaPaymentRepository.kt"] = """
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
                """,

            ["android/src/main/java/com/weselaja/integration/WeselAjaConfig.kt"] = """
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
                """,

            ["android/src/main/java/com/weselaja/integration/WeselAjaDemoActivity.kt"] = """
                package com.weselaja.integration

                import android.app.Activity
                import android.os.Bundle
                import android.view.View
                import android.view.ViewGroup
                import android.widget.ArrayAdapter
                import android.widget.Button
                import android.widget.EditText
                import android.widget.LinearLayout
                import android.widget.ScrollView
                import android.widget.Spinner
                import android.widget.TextView
                import androidx.lifecycle.lifecycleScope
                import kotlinx.coroutines.launch

                class WeselAjaDemoActivity : Activity() {
                    private lateinit var amountInput: EditText
                    private lateinit var customerNameInput: EditText
                    private lateinit var phoneInput: EditText
                    private lateinit var bankSpinner: Spinner
                    private lateinit var resultText: TextView
                    private lateinit var createButton: Button
                    private lateinit var statusButton: Button
                    private lateinit var webViewButton: Button

                    private val repository by lazy {
                        WeselAjaPaymentRepository(WeselAjaClient.create())
                    }

                    private var lastPayment: PaymentInfo? = null

                    override fun onCreate(savedInstanceState: Bundle?) {
                        super.onCreate(savedInstanceState)
                        buildForm()
                        handleReturnIntent(intent?.data?.toString())
                    }

                    override fun onNewIntent(intent: android.content.Intent?) {
                        super.onNewIntent(intent)
                        setIntent(intent)
                        handleReturnIntent(intent?.data?.toString())
                    }

                    private fun buildForm() {
                        val density = resources.displayMetrics.density
                        fun dp(value: Int) = (value * density).toInt()

                        val content = LinearLayout(this).apply {
                            orientation = LinearLayout.VERTICAL
                            setPadding(dp(20), dp(20), dp(20), dp(28))
                        }

                        currentContentRoot = content

                        val scroll = ScrollView(this).apply {
                            addView(content)
                        }

                        content.addView(TextView(this).apply {
                            text = "WeselAja Direct Payin"
                            textSize = 23f
                        }, matchWidthWrap())

                        content.addView(TextView(this).apply {
                            text = "Create a Virtual Account payment directly through the WeselAja middleware."
                            textSize = 14f
                        }, marginParams(dp(8), dp(4), dp(8), dp(18)))

                        amountInput = editField(
                            "Amount (IDR)",
                            "10000",
                            android.text.InputType.TYPE_CLASS_NUMBER
                        )

                        customerNameInput = editField(
                            "Customer name",
                            "Andi",
                            android.text.InputType.TYPE_CLASS_TEXT or
                                android.text.InputType.TYPE_TEXT_FLAG_CAP_WORDS
                        )

                        phoneInput = editField(
                            "Customer phone (optional)",
                            "",
                            android.text.InputType.TYPE_CLASS_PHONE
                        )

                        content.addView(
                            label("Bank / Virtual Account channel"),
                            marginParams(dp(12), dp(8), dp(4), dp(4))
                        )

                        bankSpinner = Spinner(this).apply {
                            adapter = ArrayAdapter(
                                this@WeselAjaDemoActivity,
                                android.R.layout.simple_spinner_dropdown_item,
                                listOf(
                                    "BCA.VA",
                                    "BRI.VA",
                                    "BNI.VA",
                                    "MDR.VA",
                                    "PTB.VA",
                                    "CIMB.VA"
                                )
                            )
                        }

                        content.addView(bankSpinner, matchWidthWrap())

                        createButton = Button(this).apply {
                            text = "Create Payment"
                            setOnClickListener { createPayment() }
                        }

                        content.addView(
                            createButton,
                            marginParams(dp(4), dp(20), dp(4), dp(4))
                        )

                        resultText = TextView(this).apply {
                            text = "Payment result will appear here."
                            textSize = 14f
                        }

                        content.addView(
                            resultText,
                            marginParams(dp(4), dp(16), dp(4), dp(8))
                        )

                        statusButton = Button(this).apply {
                            text = "Check Payment Status"
                            visibility = View.GONE
                            setOnClickListener { checkStatus() }
                        }

                        content.addView(statusButton, matchWidthWrap())

                        webViewButton = Button(this).apply {
                            text = "Open Payment WebView"
                            visibility = View.GONE
                            setOnClickListener { openPaymentPage() }
                        }

                        content.addView(webViewButton, matchWidthWrap())

                        setContentView(scroll)
                    }

                    private var currentContentRoot: LinearLayout? = null

                    private fun editField(
                        hintText: String,
                        initialValue: String,
                        inputType: Int
                    ): EditText {
                        val view = EditText(this).apply {
                            hint = hintText
                            setText(initialValue)
                            this.inputType = inputType
                            maxLines = 1
                        }

                        currentContentRoot?.addView(
                            view,
                            LinearLayout.LayoutParams(
                                ViewGroup.LayoutParams.MATCH_PARENT,
                                ViewGroup.LayoutParams.WRAP_CONTENT
                            )
                        )

                        return view
                    }

                    private fun label(value: String) =
                        TextView(this).apply {
                            text = value
                        }

                    private fun matchWidthWrap() =
                        LinearLayout.LayoutParams(
                            ViewGroup.LayoutParams.MATCH_PARENT,
                            ViewGroup.LayoutParams.WRAP_CONTENT
                        )

                    private fun marginParams(
                        left: Int,
                        top: Int,
                        right: Int,
                        bottom: Int
                    ) = LinearLayout.LayoutParams(
                        ViewGroup.LayoutParams.MATCH_PARENT,
                        ViewGroup.LayoutParams.WRAP_CONTENT
                    ).apply {
                        setMargins(left, top, right, bottom)
                    }

                    private fun createPayment() {
                        val amount = amountInput.text
                            .toString()
                            .trim()
                            .toLongOrNull()

                        val name = customerNameInput.text.toString().trim()
                        val phone = phoneInput.text.toString().trim()
                            .ifBlank { null }
                        val channel = bankSpinner.selectedItem?.toString()
                            .orEmpty()

                        if (amount == null || amount <= 0) {
                            resultText.text =
                                "Enter a valid amount greater than zero."
                            return
                        }

                        if (name.isBlank()) {
                            resultText.text = "Customer name is required."
                            return
                        }

                        setBusy(true, "Creating payment...")

                        lifecycleScope.launch {
                            try {
                                val payment = repository.createPayment(
                                    amount = amount,
                                    customerName = name,
                                    phoneNumber = phone,
                                    paymentChannel = channel,
                                    description = "Android Direct Payin"
                                )

                                lastPayment = payment
                                showPayment(payment)
                            } catch (error: Exception) {
                                resultText.text =
                                    "Payment creation failed: " +
                                    (error.message ?: "Unknown error")
                            } finally {
                                setBusy(false)
                            }
                        }
                    }

                    private fun checkStatus() {
                        val payinId = lastPayment?.middlewareId

                        if (payinId.isNullOrBlank()) {
                            resultText.text =
                                "The response does not contain a Payin ID."
                            return
                        }

                        setBusy(true, "Checking payment status...")

                        lifecycleScope.launch {
                            try {
                                val updated = repository.getPayin(payinId)
                                lastPayment = updated
                                showPayment(updated)
                            } catch (error: Exception) {
                                resultText.text =
                                    "Status check failed: " +
                                    (error.message ?: "Unknown error")
                            } finally {
                                setBusy(false)
                            }
                        }
                    }

                    private fun showPayment(payment: PaymentInfo) {
                        val lines = listOfNotNull(
                            "Status: ${payment.status ?: "UNKNOWN"}",
                            payment.referenceCode?.let { "Reference: $it" },
                            payment.middlewareId?.let { "Payin ID: $it" },
                            payment.paymentChannel?.let { "Channel: $it" },
                            payment.paymentCode?.let { "Payment code: $it" },
                            payment.expirationTime?.let { "Expires: $it" }
                        )

                        resultText.text = lines.joinToString("\n")

                        statusButton.visibility =
                            if (payment.middlewareId.isNullOrBlank()) {
                                View.GONE
                            } else {
                                View.VISIBLE
                            }

                        webViewButton.visibility =
                            if (payment.webViewUrl.isNullOrBlank()) {
                                View.GONE
                            } else {
                                View.VISIBLE
                            }

                        if (
                            payment.webViewUrl.isNullOrBlank() &&
                            !payment.paymentCode.isNullOrBlank()
                        ) {
                            resultText.append(
                                "\n\nThis Direct Payin response contains a VA payment code, not a hosted checkout URL."
                            )
                        }
                    }

                    private fun openPaymentPage() {
                        val url = lastPayment?.webViewUrl ?: return

                        startActivity(
                            android.content.Intent(
                                this,
                                WeselAjaWebViewActivity::class.java
                            ).putExtra(
                                WeselAjaWebViewActivity.EXTRA_URL,
                                url
                            )
                        )
                    }

                    private fun setBusy(
                        busy: Boolean,
                        message: String? = null
                    ) {
                        createButton.isEnabled = !busy
                        statusButton.isEnabled = !busy

                        if (message != null) {
                            resultText.text = message
                        }
                    }

                    private fun handleReturnIntent(raw: String?) {
                        if (raw.isNullOrBlank()) return

                        val uri = android.net.Uri.parse(raw)

                        if (
                            uri.scheme == "weselaja" &&
                            uri.host == "payment" &&
                            uri.path == "/return"
                        ) {
                            val reference =
                                uri.getQueryParameter("referenceCode")

                            if (!reference.isNullOrBlank()) {
                                resultText.text =
                                    "Returned from payment flow. Reference: $reference. Check status using the Payin ID from the created transaction."
                            }
                        }
                    }
                }
                """,

            ["android/src/main/java/com/weselaja/integration/WeselAjaWebViewActivity.kt"] = """
                package com.weselaja.integration

                import android.app.Activity
                import android.os.Bundle
                import android.net.Uri
                import android.webkit.WebChromeClient
                import android.webkit.WebResourceRequest
                import android.webkit.WebView
                import android.webkit.WebViewClient
                import android.widget.Toast
                import android.view.ViewGroup

                class WeselAjaWebViewActivity : Activity() {
                    companion object {
                        const val EXTRA_URL =
                            "com.weselaja.integration.EXTRA_PAYMENT_URL"
                    }

                    private var paymentWebView: WebView? = null

                    override fun onCreate(savedInstanceState: Bundle?) {
                        super.onCreate(savedInstanceState)

                        val url = intent.getStringExtra(EXTRA_URL).orEmpty()
                        val uri = runCatching { Uri.parse(url) }.getOrNull()

                        if (
                            uri == null ||
                            uri.scheme != "https" ||
                            uri.host.isNullOrBlank()
                        ) {
                            Toast.makeText(
                                this,
                                "Invalid payment URL.",
                                Toast.LENGTH_LONG
                            ).show()

                            finish()
                            return
                        }

                        val webView = WebView(this)
                        paymentWebView = webView

                        webView.settings.javaScriptEnabled = true
                        webView.settings.domStorageEnabled = true
                        webView.settings.allowFileAccess = false
                        webView.settings.allowContentAccess = false
                        webView.settings.mixedContentMode =
                            android.webkit.WebSettings.MIXED_CONTENT_NEVER_ALLOW

                        webView.webChromeClient = WebChromeClient()

                        webView.webViewClient = object : WebViewClient() {
                            override fun shouldOverrideUrlLoading(
                                view: WebView?,
                                request: WebResourceRequest?
                            ): Boolean {
                                val target = request?.url ?: return true

                                if (target.scheme == "https") {
                                    return false
                                }

                                if (target.scheme == "weselaja") {
                                    setResult(
                                        RESULT_OK,
                                        intent.setData(target)
                                    )

                                    finish()
                                    return true
                                }

                                return true
                            }
                        }

                        setContentView(webView)
                        webView.loadUrl(uri.toString())
                    }

                    override fun onDestroy() {
                        paymentWebView?.apply {
                            stopLoading()
                            loadUrl("about:blank")
                            removeAllViews()
                            (parent as? ViewGroup)?.removeView(this)
                            destroy()
                        }

                        paymentWebView = null
                        super.onDestroy()
                    }
                }
                """,

            ["examples/PaymentUsage.kt"] = """
                package com.weselaja.integration

                suspend fun createWeselAjaExamplePayment(): PaymentInfo {
                    val repository = WeselAjaPaymentRepository(
                        WeselAjaClient.create()
                    )

                    return repository.createPayment(
                        amount = 10_000L,
                        customerName = "Andi",
                        phoneNumber = "081234567890",
                        paymentChannel = "BCA.VA",
                        description = "WeselAja Android sandbox example"
                    )
                }

                suspend fun checkWeselAjaExamplePayment(
                    payinId: String
                ): PaymentInfo {
                    val repository = WeselAjaPaymentRepository(
                        WeselAjaClient.create()
                    )

                    return repository.getPayin(payinId)
                }
                """,

            ["tests/ANDROID_MANUAL_TEST_CHECKLIST.md"] = """
                # Android Direct Payin manual test checklist

                - [ ] Add Retrofit, Gson converter, OkHttp, and lifecycle runtime KTX dependencies.
                - [ ] Merge Internet permission and the two activity declarations into AndroidManifest.xml.
                - [ ] Configure sandbox API key, API secret, customer reference, fee bearer, and any required settlement JSON.
                - [ ] Confirm no production credentials are committed to source control.
                - [ ] Build the Android project.
                - [ ] Open `WeselAjaDemoActivity` and submit a valid IDR amount, customer name, phone (optional), and VA channel.
                - [ ] Confirm the request goes directly to `POST /v2/payins` on the WeselAja middleware.
                - [ ] Confirm request headers are `X-API-Key`, `X-Timestamp`, and `X-Signature`.
                - [ ] Confirm the response displays Payin ID, reference, payment channel, status, and payment code when returned.
                - [ ] Use Check Payment Status and confirm it calls `GET /v2/payins/{id}`.
                - [ ] If a payment URL is returned, open it in the generated WebView activity.
                - [ ] If no payment URL is returned, confirm the Virtual Account payment code is displayed instead.
                - [ ] Confirm the app never treats a redirect/deep-link status parameter as payment proof.
                - [ ] Confirm callback handling is not expected in the Android app; a public server endpoint is required for webhooks.
                - [ ] Confirm no API secret is included in a public/distributed production APK.
                """
        };

    public GenerationResult Generate(
        string projectName,
        bool includeExample,
        bool includeTests,
        bool overwrite,
        string? outputDirectory = null)
    {
        ValidateTemplates();

        var safeProjectName = SanitizeProjectName(projectName);
        var outputRoot = ResolveOutputRoot(outputDirectory);
        var androidRoot = Path.GetFullPath(
            Path.Combine(outputRoot, TargetDirectory));

        EnsureInsideDirectory(androidRoot, outputRoot);

        EnsureWritableDirectory(outputRoot);
        EnsureWritableDirectory(androidRoot);

        var projectOutputDirectory = Path.GetFullPath(
            Path.Combine(androidRoot, safeProjectName));

        EnsureInsideDirectory(projectOutputDirectory, androidRoot);

        if (Directory.Exists(projectOutputDirectory))
        {
            if (!overwrite)
            {
                throw new InvalidOperationException(
                    $"Generated project already exists: {projectOutputDirectory}. " +
                    "Set overwrite=true to replace it.");
            }

            EnsureWritableDirectory(projectOutputDirectory);
            Directory.Delete(projectOutputDirectory, recursive: true);
        }

        EnsureWritableDirectory(projectOutputDirectory);

        var filesToGenerate = new List<string>(RequiredFiles);

        if (includeExample)
        {
            filesToGenerate.Add("examples/PaymentUsage.kt");
        }

        if (includeTests)
        {
            filesToGenerate.Add("tests/ANDROID_MANUAL_TEST_CHECKLIST.md");
        }

        var generatedFiles = new List<GeneratedFile>();
        var encoding = new UTF8Encoding(
            encoderShouldEmitUTF8Identifier: false);

        foreach (var relativePath in filesToGenerate)
        {
            if (!TemplateFiles.TryGetValue(
                relativePath,
                out var contents))
            {
                throw new InvalidOperationException(
                    $"Embedded Android template is missing: {relativePath}");
            }

            var outputPath = Path.GetFullPath(
                Path.Combine(projectOutputDirectory, relativePath));

            EnsureInsideDirectory(outputPath, projectOutputDirectory);

            var directory = Path.GetDirectoryName(outputPath);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(
                outputPath,
                contents,
                encoding);

            generatedFiles.Add(new GeneratedFile
            {
                Path = Path.GetRelativePath(
                    projectOutputDirectory,
                    outputPath),
                Bytes = new FileInfo(outputPath).Length
            });
        }

        return new GenerationResult(
            safeProjectName,
            projectOutputDirectory,
            generatedFiles);
    }

    private static string ResolveOutputRoot(
        string? outputDirectoryOverride)
    {
        var configuredRoot =
            !string.IsNullOrWhiteSpace(outputDirectoryOverride)
                ? outputDirectoryOverride.Trim()
                : GeneratorOutputScope.GetConfiguredRoot()?.Trim();

        var selectedRoot =
            string.IsNullOrWhiteSpace(configuredRoot)
                ? Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "generated")
                : configuredRoot;

        if (
            OperatingSystem.IsLinux() &&
            IsWindowsAbsolutePath(selectedRoot))
        {
            throw new InvalidOperationException(
                $"The MCP process is running on Linux and cannot access the Windows path '{selectedRoot}' directly. " +
                "Mount the Windows generated directory into the container, then pass the container path, " +
                "for example '/app/generated', as outputDirectory.");
        }

        return Path.GetFullPath(selectedRoot);
    }

    private static bool IsWindowsAbsolutePath(string path)
    {
        return path.Length >= 3
            && char.IsAsciiLetter(path[0])
            && path[1] == ':'
            && (path[2] == '\\' || path[2] == '/');
    }

    private static void EnsureWritableDirectory(string directory)
    {
        try
        {
            Directory.CreateDirectory(directory);
        }
        catch (UnauthorizedAccessException exception)
        {
            throw new UnauthorizedAccessException(
                $"The MCP process cannot create or access directory '{directory}'. " +
                "Check the Docker bind mount and write permissions for the MCP process. " +
                "A Windows host path must be mounted into the container before it can be used.",
                exception);
        }
        catch (IOException exception)
        {
            throw new IOException(
                $"Unable to create or access generator directory '{directory}'. " +
                "Check the directory path, Docker mount, and filesystem permissions.",
                exception);
        }

        if (CanWriteToDirectory(directory))
        {
            return;
        }

        TryGrantDirectoryWriteAccess(directory);

        if (CanWriteToDirectory(directory))
        {
            return;
        }

        var processIdentity = OperatingSystem.IsLinux()
            ? GetLinuxIdentity()
            : Environment.UserName;

        throw new UnauthorizedAccessException(
            $"The MCP process cannot write to '{directory}'. " +
            $"Process identity: {processIdentity}. " +
            "The generator cannot grant access to a directory owned by another " +
            "user or to a read-only Docker mount. Ensure the host generated " +
            "directory is bind-mounted to the configured container path and " +
            "grant write permission to the MCP process. For Docker, check the " +
            "container UID/GID and the host directory ACL. Set " +
            "WESELAJA_GENERATOR_OUTPUT_DIR or the tool outputDirectory to the " +
            "writable mounted path.");
    }

    private static bool CanWriteToDirectory(string directory)
    {
        var probePath = Path.Combine(
            directory,
            $".weselaja-write-test-{Guid.NewGuid():N}.tmp");

        try
        {
            using (var stream = new FileStream(
                probePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None))
            {
                stream.WriteByte(0);
            }

            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
        finally
        {
            try
            {
                if (File.Exists(probePath))
                {
                    File.Delete(probePath);
                }
            }
            catch
            {
                // Preserve the original write-check result.
            }
        }
    }

    private static void TryGrantDirectoryWriteAccess(
        string directory)
    {
        if (
            !OperatingSystem.IsLinux() &&
            !OperatingSystem.IsMacOS())
        {
            return;
        }

        try
        {
            var currentMode = File.GetUnixFileMode(directory);

            var writableMode = currentMode
                | UnixFileMode.UserRead
                | UnixFileMode.UserWrite
                | UnixFileMode.UserExecute;

            File.SetUnixFileMode(
                directory,
                writableMode);
        }
        catch (UnauthorizedAccessException)
        {
            // The directory is owned by another user or the mount is read-only.
        }
        catch (IOException)
        {
            // Permission repair is best-effort.
        }
        catch (PlatformNotSupportedException)
        {
            // The platform does not support POSIX mode changes.
        }
    }

    private static string GetLinuxIdentity()
    {
        if (!OperatingSystem.IsLinux())
        {
            return Environment.UserName;
        }

        try
        {
            var status = File.ReadAllLines("/proc/self/status");

            var uid = status.FirstOrDefault(
                line => line.StartsWith(
                    "Uid:",
                    StringComparison.Ordinal));

            var gid = status.FirstOrDefault(
                line => line.StartsWith(
                    "Gid:",
                    StringComparison.Ordinal));

            return
                $"{uid?.Trim() ?? "Uid: unknown"}; " +
                $"{gid?.Trim() ?? "Gid: unknown"}";
        }
        catch
        {
            return "Linux user identity unavailable";
        }
    }

    private static void ValidateTemplates()
    {
        foreach (var relativePath in RequiredFiles)
        {
            if (!TemplateFiles.ContainsKey(relativePath))
            {
                throw new InvalidOperationException(
                    $"Required embedded Android template is missing: {relativePath}");
            }
        }

        if (!TemplateFiles.ContainsKey("examples/PaymentUsage.kt"))
        {
            throw new InvalidOperationException(
                "Embedded Android usage example is missing.");
        }

        if (!TemplateFiles.ContainsKey(
            "tests/ANDROID_MANUAL_TEST_CHECKLIST.md"))
        {
            throw new InvalidOperationException(
                "Embedded Android test checklist is missing.");
        }
    }

    private static string SanitizeProjectName(
        string? projectName)
    {
        var value = string.IsNullOrWhiteSpace(projectName)
            ? DefaultProjectName
            : projectName.Trim().ToLowerInvariant();

        var builder = new StringBuilder();
        var separatorPending = false;

        foreach (var character in value)
        {
            if (char.IsAsciiLetterOrDigit(character))
            {
                if (
                    separatorPending &&
                    builder.Length > 0)
                {
                    builder.Append('-');
                }

                builder.Append(character);
                separatorPending = false;
            }
            else if (builder.Length > 0)
            {
                separatorPending = true;
            }
        }

        var result = builder.ToString().Trim('-');

        if (string.IsNullOrWhiteSpace(result))
        {
            result = DefaultProjectName;
        }

        if (result.Length > 64)
        {
            result = result[..64].Trim('-');
        }

        return string.IsNullOrWhiteSpace(result)
            ? DefaultProjectName
            : result;
    }

    private static void EnsureInsideDirectory(
        string path,
        string parentDirectory)
    {
        var fullPath = Path.GetFullPath(path);

        var fullParent = Path.GetFullPath(parentDirectory)
            .TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(
            fullParent,
            OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "A generated path resolved outside its expected directory.");
        }
    }
}
