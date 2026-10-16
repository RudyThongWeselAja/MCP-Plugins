
using System.Text;

namespace WeselAjaMcpServer.Generators;

public sealed class AndroidKotlinWeselAjaGenerator
{
    public GenerationResult Generate(
        string projectName,
        bool includeExample,
        bool includeTests,
        bool overwrite)
    {
        var safeProjectName = SanitizeProjectName(projectName);
        var outputDirectory = GetOutputDirectory(
            "android",
            safeProjectName);

        if (Directory.Exists(outputDirectory) && !overwrite)
        {
            throw new InvalidOperationException(
                $"Generated project already exists: {outputDirectory}. " +
                "Set overwrite=true to replace it.");
        }

        if (Directory.Exists(outputDirectory))
        {
            Directory.Delete(outputDirectory, recursive: true);
        }

        Directory.CreateDirectory(outputDirectory);

        var files = new List<GeneratedFile>();

        AddGeneratedFile(
            outputDirectory,
            ".gitignore",
            GitIgnoreTemplate,
            files);

        AddGeneratedFile(
            outputDirectory,
            "README.md",
            ReadmeTemplate(safeProjectName),
            files);

        AddGeneratedFile(
            outputDirectory,
            Path.Combine(
                "android",
                "src",
                "main",
                "java",
                "com",
                "weselaja",
                "integration",
                "PaymentModels.kt"),
            PaymentModelsTemplate,
            files);

        AddGeneratedFile(
            outputDirectory,
            Path.Combine(
                "android",
                "src",
                "main",
                "java",
                "com",
                "weselaja",
                "integration",
                "WeselAjaBackendApi.kt"),
            BackendApiTemplate,
            files);

        AddGeneratedFile(
            outputDirectory,
            Path.Combine(
                "android",
                "src",
                "main",
                "java",
                "com",
                "weselaja",
                "integration",
                "WeselAjaPaymentRepository.kt"),
            RepositoryTemplate,
            files);

        AddGeneratedFile(
            outputDirectory,
            Path.Combine(
                "android",
                "src",
                "main",
                "java",
                "com",
                "weselaja",
                "integration",
                "WeselAjaRedirectHandler.kt"),
            RedirectHandlerTemplate,
            files);

        AddGeneratedFile(
            outputDirectory,
            Path.Combine(
                "android",
                "src",
                "main",
                "AndroidManifest.intent-filter.xml"),
            ManifestTemplate,
            files);

        AddGeneratedFile(
            outputDirectory,
            Path.Combine(
                "backend-contract",
                "README.md"),
            BackendContractTemplate,
            files);

        if (includeExample)
        {
            AddGeneratedFile(
                outputDirectory,
                Path.Combine(
                    "examples",
                    "PaymentUsage.kt"),
                UsageExampleTemplate,
                files);
        }

        if (includeTests)
        {
            AddGeneratedFile(
                outputDirectory,
                Path.Combine(
                    "tests",
                    "MANUAL_TEST_CHECKLIST.md"),
                TestChecklistTemplate,
                files);
        }

        return new GenerationResult(
            safeProjectName,
            outputDirectory,
            files);
    }

    private static void AddGeneratedFile(
        string outputDirectory,
        string relativePath,
        string content,
        List<GeneratedFile> files)
    {
        var fullPath = Path.Combine(
            outputDirectory,
            relativePath);

        var directory = Path.GetDirectoryName(fullPath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(
            fullPath,
            content,
            new UTF8Encoding(false));

        files.Add(
            new GeneratedFile
            {
                Path = Path.GetRelativePath(
                    outputDirectory,
                    fullPath),
                Bytes = new FileInfo(fullPath).Length
            });
    }

    private static string GetOutputDirectory(
        string target,
        string projectName)
    {
        var configuredRoot =
            Environment.GetEnvironmentVariable(
                "WESELAJA_GENERATOR_OUTPUT_DIR");

        var outputRoot =
            string.IsNullOrWhiteSpace(configuredRoot)
                ? Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "generated")
                : configuredRoot.Trim();

        outputRoot = Path.GetFullPath(outputRoot);

        var targetRoot = Path.GetFullPath(
            Path.Combine(outputRoot, target));

        var outputDirectory = Path.GetFullPath(
            Path.Combine(targetRoot, projectName));

        EnsureInsideDirectory(outputDirectory, targetRoot);

        return outputDirectory;
    }

    private static void EnsureInsideDirectory(
        string path,
        string root)
    {
        var relative = Path.GetRelativePath(
            Path.GetFullPath(root),
            Path.GetFullPath(path));

        if (
            relative == ".."
            || relative.StartsWith(
                ".." + Path.DirectorySeparatorChar,
                StringComparison.Ordinal)
            || Path.IsPathRooted(relative))
        {
            throw new InvalidOperationException(
                "Generated project path is outside the configured generator directory.");
        }
    }

    private static string SanitizeProjectName(
        string projectName)
    {
        var value = (projectName ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(value))
        {
            return "weselaja-android-kotlin";
        }

        var builder = new StringBuilder();
        var previousSeparator = false;

        foreach (var character in value)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
                previousSeparator = false;
            }
            else if (!previousSeparator)
            {
                builder.Append('-');
                previousSeparator = true;
            }
        }

        var result = builder.ToString().Trim('-');

        if (string.IsNullOrWhiteSpace(result))
        {
            return "weselaja-android-kotlin";
        }

        if (result.Length > 64)
        {
            result = result[..64].Trim('-');
        }

        return string.IsNullOrWhiteSpace(result)
            ? "weselaja-android-kotlin"
            : result;
    }

    private const string GitIgnoreTemplate = """
        .env
        .idea/
        .gradle/
        build/
        **/build/
        local.properties
        *.iml
        """;

    private static string ReadmeTemplate(
        string projectName)
    {
        return $$"""
        # {{projectName}}

        WeselAja Android Kotlin integration code generated by the WeselAja MCP Code Generator.

        This is an integration-code package for an existing Android application, not a complete Android project.

        ## Features

        - Request payment through your backend
        - Retrieve payment status through your backend
        - Parse the Android payment-return deep link
        - Provide a backend endpoint contract for payment creation, callback handling, redirect, and payin status checks
        - Keep WeselAja API credentials on the backend

        ## Generated files

        - `android/src/main/java/com/weselaja/integration/PaymentModels.kt`
        - `android/src/main/java/com/weselaja/integration/WeselAjaBackendApi.kt`
        - `android/src/main/java/com/weselaja/integration/WeselAjaPaymentRepository.kt`
        - `android/src/main/java/com/weselaja/integration/WeselAjaRedirectHandler.kt`
        - `android/src/main/AndroidManifest.intent-filter.xml`
        - `backend-contract/README.md`

        ## Requirements

        - An existing Android Kotlin project
        - Android SDK and Gradle
        - Internet permission
        - Retrofit and Gson
        - Kotlin coroutines for suspend-function usage

        ## Dependencies

        Add these dependencies to the existing app module's `build.gradle.kts`.
        Use compatible versions for your project.

        ```kotlin
        dependencies {
            implementation("com.squareup.retrofit2:retrofit:<version>")
            implementation("com.squareup.retrofit2:converter-gson:<same-version>")
            implementation("org.jetbrains.kotlinx:kotlinx-coroutines-android:<version>")
        }
        ```

        Add the following permission to your existing `AndroidManifest.xml`:

        ```xml
        <uses-permission android:name="android.permission.INTERNET" />
        ```

        ## Installation

        1. Copy the generated Kotlin files into the existing app module.
        2. Keep the package name `com.weselaja.integration`, or update the package declarations and file locations together.
        3. Add the dependencies shown above.
        4. Configure the backend base URL when creating `WeselAjaBackendApi`.
        5. Add the generated intent filter to the existing activity that should receive the payment-return deep link.
        6. Implement the server endpoints documented in `backend-contract/README.md`.
        7. Build the existing Android project with Gradle.

        The sample backend URL in the example must be replaced with your own backend URL.

        ## Security

        Do not place `WESELAJA_API_KEY`, `WESELAJA_SECRET_KEY`, or `WEB_SIGNATURE_SECRET` in Android resources, source code, or BuildConfig.

        The Android application calls your backend. The backend signs requests to WeselAja, receives and verifies callbacks, and retrieves payin status.

        The callback must be received on a public backend endpoint. An Android activity is not a webhook server.

        The Android deep link is used to return the user to the app. A status included in a redirect URL is not proof that the payment succeeded. Query the backend for the authoritative transaction status.

        ## Backend contract

        See `backend-contract/README.md` for the required routes and JSON envelope.

        ## Testing

        See `tests/MANUAL_TEST_CHECKLIST.md` when the checklist is generated.
        """;
    }

    private const string PaymentModelsTemplate = """
        package com.weselaja.integration

        data class CreatePaymentRequest(
            val amount: Long,
            val customerName: String,
            val bank: String,
            val customerPhoneNumber: String? = null,
            val description: String? = null,
            val referenceCode: String? = null
        )

        data class ApiEnvelope<T>(
            val success: Boolean = false,
            val data: T? = null,
            val error: String? = null,
            val message: String? = null
        )

        data class PaymentInfo(
            val id: String? = null,
            val initiatedAmount: Long? = null,
            val paymentAmount: Long? = null,
            val feeAmount: Long? = null,
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
            val description: String? = null
        )
        """;

    private const string BackendApiTemplate = """
        package com.weselaja.integration

        import retrofit2.Response
        import retrofit2.Retrofit
        import retrofit2.converter.gson.GsonConverterFactory
        import retrofit2.http.Body
        import retrofit2.http.GET
        import retrofit2.http.POST
        import retrofit2.http.Path

        interface WeselAjaBackendApi {
            @POST("api/weselaja/payments")
            suspend fun createPayment(
                @Body request: CreatePaymentRequest
            ): Response<ApiEnvelope<PaymentInfo>>

            @GET("api/weselaja/payins/{referenceCode}")
            suspend fun getPayin(
                @Path("referenceCode") referenceCode: String
            ): Response<ApiEnvelope<PaymentInfo>>
        }

        object WeselAjaApiFactory {
            fun create(baseUrl: String): WeselAjaBackendApi {
                val normalizedUrl = baseUrl.trim().let {
                    require(
                        it.startsWith("https://", ignoreCase = true)
                        || it.startsWith("http://", ignoreCase = true)
                    ) {
                        "Backend base URL must use HTTP or HTTPS."
                    }

                    if (it.endsWith("/")) it else "$it/"
                }

                return Retrofit.Builder()
                    .baseUrl(normalizedUrl)
                    .addConverterFactory(
                        GsonConverterFactory.create()
                    )
                    .build()
                    .create(WeselAjaBackendApi::class.java)
            }
        }
        """;

    private const string RepositoryTemplate = """
        package com.weselaja.integration

        import java.io.IOException
        import retrofit2.Response

        class WeselAjaPaymentRepository(
            private val api: WeselAjaBackendApi
        ) {
            suspend fun createPayment(
                request: CreatePaymentRequest
            ): PaymentInfo {
                require(request.amount > 0) {
                    "Payment amount must be greater than zero."
                }

                require(request.customerName.isNotBlank()) {
                    "Customer name is required."
                }

                require(request.bank.isNotBlank()) {
                    "Bank is required."
                }

                return unwrap(
                    api.createPayment(request),
                    "Create payment"
                )
            }

            suspend fun getPayin(
                referenceCode: String
            ): PaymentInfo {
                require(referenceCode.isNotBlank()) {
                    "Reference code is required."
                }

                return unwrap(
                    api.getPayin(referenceCode),
                    "Get payin"
                )
            }

            private fun <T> unwrap(
                response: Response<ApiEnvelope<T>>,
                operation: String
            ): T {
                if (!response.isSuccessful) {
                    val detail = response.errorBody()
                        ?.string()
                        ?.takeIf { it.isNotBlank() }

                    throw IOException(
                        "$operation failed with HTTP ${response.code()}: " +
                        (detail ?: response.message())
                    )
                }

                val envelope = response.body()
                    ?: throw IOException(
                        "$operation returned an empty response."
                    )

                if (!envelope.success) {
                    throw IOException(
                        envelope.error
                            ?: envelope.message
                            ?: "$operation was rejected by the backend."
                    )
                }

                return envelope.data
                    ?: throw IOException(
                        "$operation response did not contain payment data."
                    )
            }
        }
        """;

    private const string RedirectHandlerTemplate = """
        package com.weselaja.integration

        import android.content.Intent
        import android.net.Uri

        data class WeselAjaRedirect(
            val referenceCode: String,
            val statusHint: String?
        )

        object WeselAjaRedirectHandler {
            fun parse(intent: Intent?): WeselAjaRedirect? {
                return parse(intent?.data)
            }

            fun parse(uri: Uri?): WeselAjaRedirect? {
                if (uri == null) return null

                if (
                    uri.scheme != "weselaja"
                    || uri.host != "payment"
                    || uri.path != "/return"
                ) {
                    return null
                }

                val referenceCode = uri
                    .getQueryParameter("referenceCode")
                    ?.trim()
                    ?.takeIf { it.isNotEmpty() }
                    ?: return null

                return WeselAjaRedirect(
                    referenceCode = referenceCode,
                    statusHint = uri.getQueryParameter("status")
                )
            }
        }
        """;

    private const string ManifestTemplate = """
        Add this intent-filter inside the existing activity that should receive the payment return.
        Do not add a second activity entry if the activity is already declared.

        <intent-filter>
            <action android:name="android.intent.action.VIEW" />
            <category android:name="android.intent.category.DEFAULT" />
            <category android:name="android.intent.category.BROWSABLE" />
            <data
                android:scheme="weselaja"
                android:host="payment"
                android:path="/return" />
        </intent-filter>
        """;

    private const string BackendContractTemplate = """
        # WeselAja Android backend contract

        The Kotlin integration calls your application backend, not the WeselAja API directly.

        Implement these routes on your server. They are the contract expected by the generated Retrofit client. Update the Retrofit annotations if your routes differ.

        ## 1. Create payment

        `POST /api/weselaja/payments`

        Request:

        ```json
        {
          "amount": 10000,
          "customerName": "Andi",
          "bank": "BCA",
          "customerPhoneNumber": null,
          "description": "Android sandbox payment",
          "referenceCode": null
        }
        ```

        The backend validates the input, maps the bank to its WeselAja channel, creates a signed request to `POST /v1/payins`, and stores the payment ID and reference code.

        Expected response envelope:

        ```json
        {
          "success": true,
          "data": {
            "id": "PAYMENT_ID",
            "initiatedAmount": 10000,
            "currency": "IDR",
            "paymentMethod": "VIRTUAL_ACCOUNT",
            "paymentChannel": "BCA.VA",
            "paymentCode": "PAYMENT_CODE",
            "referenceCode": "WES-EXAMPLE-001",
            "status": "PENDING"
          },
          "error": null
        }
        ```

        ## 2. Check payin status

        `GET /api/weselaja/payins/{referenceCode}`

        The backend looks up the stored payment and retrieves its current status from WeselAja using the payment ID or the supported payin retrieval workflow. Return the result using the same success/data envelope.

        Do not trust a status value supplied by the mobile client.

        ## 3. Receive callback

        `POST /payment/weselaja/callback`

        This is a public server endpoint, not an Android endpoint.

        The backend must read the exact raw request body and verify the callback signature using the shared webhook secret and the documented signing payload. It must reject invalid signatures, associate the notification with the stored payment, and update the payment status.

        Configure the public `WESELAJA_CALLBACK_URL` to this endpoint. Never expose `WEB_SIGNATURE_SECRET` in the Android app.

        ## 4. Redirect back to Android

        `WESELAJA_REDIRECT_URL` should point to the public server redirect route.

        After handling the browser return, the server may redirect to this Android deep link:

        `weselaja://payment/return?referenceCode=WES-EXAMPLE-001`

        The Android app parses the reference code using `WeselAjaRedirectHandler` and asks the backend for the actual payment status.

        A `status` query parameter, if present, is only a hint for display and must not be treated as final payment confirmation.
        """;

    private const string UsageExampleTemplate = """
        package com.weselaja.integration

        suspend fun createExamplePayment(
            backendBaseUrl: String
        ): PaymentInfo {
            val api = WeselAjaApiFactory.create(
                backendBaseUrl
            )

            val repository = WeselAjaPaymentRepository(api)

            val payment = repository.createPayment(
                CreatePaymentRequest(
                    amount = 10_000,
                    customerName = "Andi",
                    bank = "BCA",
                    description = "WeselAja Android sandbox example"
                )
            )

            println("Status: ${payment.status}")
            println("Reference: ${payment.referenceCode}")
            println("Payment code: ${payment.paymentCode}")

            return payment
        }

        suspend fun checkExamplePayment(
            backendBaseUrl: String,
            referenceCode: String
        ): PaymentInfo {
            val api = WeselAjaApiFactory.create(
                backendBaseUrl
            )

            val repository = WeselAjaPaymentRepository(api)
            val payment = repository.getPayin(referenceCode)

            println("Current status: ${payment.status}")

            return payment
        }
        """;

    private const string TestChecklistTemplate = """
        # Android integration manual test checklist

        - [ ] Configure the backend base URL.
        - [ ] Build the existing Android project after copying the generated Kotlin files.
        - [ ] Create one sandbox payment using a supported bank.
        - [ ] Confirm the backend returns a payment ID, reference code, payment code, channel, and status.
        - [ ] Check the same payment through the backend payin-status route.
        - [ ] Configure the public WeselAja callback URL to point to the server callback endpoint.
        - [ ] Confirm a valid callback is accepted by the backend and an invalid signature is rejected.
        - [ ] Open the return deep link and confirm the app reads its reference code.
        - [ ] After redirect, query the backend again and confirm the displayed status comes from the server.
        - [ ] Confirm API keys and webhook secrets are not present in the Android project.
        """;
}
