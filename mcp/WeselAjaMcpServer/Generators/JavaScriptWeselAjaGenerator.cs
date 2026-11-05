using System.Reflection;
using System.Text;

namespace WeselAjaMcpServer.Generators;

public class JavaScriptWeselAjaGenerator
{
    public GenerationResult Generate(
        string projectName,
        bool includeExample,
        bool includeTests,
        bool overwrite)
    {
        var safeProjectName =
            SanitizeProjectName(
                projectName);

        var outputDirectory =
            GetOutputDirectory(
                "javascript",
                safeProjectName);

        if (
            Directory.Exists(
                outputDirectory)
            &&
            !overwrite)
        {
            throw new InvalidOperationException(
                $"Generated project already exists: " +
                $"{outputDirectory}. " +
                $"Set overwrite=true to replace it.");
        }

        if (Directory.Exists(outputDirectory))
        {
            Directory.Delete(
                outputDirectory,
                recursive: true);
        }

        Directory.CreateDirectory(
            outputDirectory);

        var files =
            new List<GeneratedFile>();

        AddGeneratedFile(
            outputDirectory,
            ".env.example",
            EnvExample(),
            files);

        AddGeneratedFile(
            outputDirectory,
            ".gitignore",
            GitIgnore(),
            files);

        AddGeneratedFile(
            outputDirectory,
            "package.json",
            PackageJson(safeProjectName),
            files);

        AddGeneratedFile(
            outputDirectory,
            "README.md",
            Readme(safeProjectName),
            files);

        AddGeneratedFile(
            outputDirectory,
            "redirect-server.js",
            RedirectServerJs(),
            files);

        AddGeneratedFile(
            outputDirectory,
            "callback-server.js",
            CallbackServerJs(),
            files);

        if (includeExample)
        {
            AddGeneratedFile(
                outputDirectory,
                "index.js",
                IndexJs(),
                files);
        }

        AddGeneratedFile(
            outputDirectory,
            Path.Combine(
                "src",
                "bank-channels.js"),
            BankChannelsJs(),
            files);

        AddGeneratedFile(
            outputDirectory,
            Path.Combine(
                "src",
                "config.js"),
            ConfigJs(),
            files);

        AddGeneratedFile(
            outputDirectory,
            Path.Combine(
                "src",
                "errors.js"),
            ErrorsJs(),
            files);

        AddGeneratedFile(
            outputDirectory,
            Path.Combine(
                "src",
                "signature.js"),
            SignatureJs(),
            files);

        AddGeneratedFile(
            outputDirectory,
            Path.Combine(
                "src",
                "weselaja-client.js"),
            WeselAjaClientJs(),
            files);

        if (includeTests)
        {
            AddGeneratedFile(
                outputDirectory,
                Path.Combine(
                    "tests",
                    "weselaja.test.js"),
                TestJs(),
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
        var fullPath =
            Path.Combine(
                outputDirectory,
                relativePath);

        var directory =
            Path.GetDirectoryName(
                fullPath);

        if (
            !string.IsNullOrWhiteSpace(
                directory))
        {
            Directory.CreateDirectory(
                directory);
        }

        File.WriteAllText(
            fullPath,
            content,
            new UTF8Encoding(false));

        files.Add(
            CreateGeneratedFile(
                relativePath,
                content));
    }

    private static GeneratedFile CreateGeneratedFile(
        string relativePath,
        string content)
    {
        var type =
            typeof(GeneratedFile);

        var parameterlessConstructor =
            type.GetConstructor(
                Type.EmptyTypes);

        if (parameterlessConstructor != null)
        {
            var instance =
                (GeneratedFile)
                parameterlessConstructor.Invoke(
                    null);

            SetPropertyIfWritable(
                type,
                instance,
                new[]
                {
                    "RelativePath",
                    "Path",
                    "FilePath",
                    "Filename",
                    "FileName",
                    "Name"
                },
                relativePath);

            SetPropertyIfWritable(
                type,
                instance,
                new[]
                {
                    "Content",
                    "Text",
                    "FileContent",
                    "Source"
                },
                content);

            return instance;
        }

        var constructors =
            type.GetConstructors(
                BindingFlags.Public |
                BindingFlags.Instance);

        foreach (var constructor in constructors)
        {
            var parameters =
                constructor.GetParameters();

            if (
                parameters.Length == 0)
            {
                continue;
            }

            var arguments =
                new object?[parameters.Length];

            for (
                var index = 0;
                index < parameters.Length;
                index++)
            {
                var parameter =
                    parameters[index];

                if (
                    parameter.ParameterType ==
                    typeof(string))
                {
                    var parameterName =
                        parameter.Name?
                            .ToLowerInvariant()
                        ?? string.Empty;

                    if (
                        parameterName.Contains(
                            "content")
                        ||
                        parameterName.Contains(
                            "text")
                        ||
                        parameterName.Contains(
                            "source"))
                    {
                        arguments[index] =
                            content;

                        continue;
                    }

                    if (
                        parameterName.Contains(
                            "path")
                        ||
                        parameterName.Contains(
                            "file")
                        ||
                        parameterName.Contains(
                            "name"))
                    {
                        arguments[index] =
                            relativePath;

                        continue;
                    }

                    var previousUnknownStringCount =
                        arguments
                            .Take(index)
                            .Count(
                                value =>
                                    value is string);

                    arguments[index] =
                        previousUnknownStringCount == 0
                            ? relativePath
                            : content;

                    continue;
                }

                if (
                    parameter.ParameterType ==
                    typeof(bool))
                {
                    arguments[index] =
                        false;

                    continue;
                }

                if (
                    parameter.HasDefaultValue)
                {
                    arguments[index] =
                        parameter.DefaultValue;

                    continue;
                }

                arguments[index] =
                    GetDefaultValue(
                        parameter.ParameterType);
            }

            try
            {
                return
                    (GeneratedFile)
                    constructor.Invoke(
                        arguments);
            }
            catch
            {
            }
        }

        throw new InvalidOperationException(
            "Unable to create GeneratedFile. " +
            "The GeneratedFile type does not expose " +
            "a supported constructor or writable properties.");
    }

    private static void SetPropertyIfWritable(
        Type type,
        GeneratedFile instance,
        IEnumerable<string> possibleNames,
        string value)
    {
        foreach (var propertyName in possibleNames)
        {
            var property =
                type.GetProperty(
                    propertyName,
                    BindingFlags.Public |
                    BindingFlags.Instance |
                    BindingFlags.IgnoreCase);

            if (
                property == null
                ||
                !property.CanWrite
                ||
                property.PropertyType != typeof(string))
            {
                continue;
            }

            property.SetValue(
                instance,
                value);

            return;
        }
    }

    private static object? GetDefaultValue(
        Type type)
    {
        if (type.IsValueType)
        {
            return Activator.CreateInstance(type);
        }

        return null;
    }

    private static string GetOutputDirectory(
        string language,
        string projectName)
    {
        var outputRoot =
            Environment.GetEnvironmentVariable(
                "WESELAJA_GENERATOR_OUTPUT_DIR");

        outputRoot =
            string.IsNullOrWhiteSpace(outputRoot)
                ? Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "generated")
                : outputRoot.Trim();

        outputRoot =
            Path.GetFullPath(
                outputRoot);

        var languageRoot =
            Path.GetFullPath(
                Path.Combine(
                    outputRoot,
                    language));

        var projectDirectory =
            Path.GetFullPath(
                Path.Combine(
                    languageRoot,
                    projectName));

        EnsureInsideDirectory(
            projectDirectory,
            languageRoot);

        return projectDirectory;
    }

    private static void EnsureInsideDirectory(
        string path,
        string root)
    {
        var relative =
            Path.GetRelativePath(
                Path.GetFullPath(root),
                Path.GetFullPath(path));

        if (
            relative == ".."
            ||
            relative.StartsWith(
                ".." +
                Path.DirectorySeparatorChar,
                StringComparison.Ordinal)
            ||
            Path.IsPathRooted(relative))
        {
            throw new InvalidOperationException(
                "Generated project path is outside the configured generator directory.");
        }
    }

    private static string SanitizeProjectName(
        string projectName)
    {
        var value =
            (projectName ?? string.Empty)
            .Trim();

        if (
            string.IsNullOrWhiteSpace(
                value))
        {
            return "weselaja-javascript";
        }

        foreach (
            var invalidCharacter
            in Path.GetInvalidFileNameChars())
        {
            value =
                value.Replace(
                    invalidCharacter,
                    '-');
        }

        value =
            value
                .Replace(
                    ' ',
                    '-')
                .ToLowerInvariant();

        return value;
    }

    private static string EnvExample()
    {
        return """
        WESELAJA_API_URL=https://sandbox.checkout.weselaja.id

        WESELAJA_API_KEY=
        WESELAJA_SECRET_KEY=

        WESELAJA_CUSTOMER_REFERENCE=

        WESELAJA_CALLBACK_URL=
        WESELAJA_REDIRECT_URL=

        WESELAJA_CURRENCY=IDR
        WESELAJA_PAYMENT_METHOD=VIRTUAL_ACCOUNT

        WESELAJA_CALLBACK_PORT=8081
        WESELAJA_REDIRECT_PORT=8080

        WEB_SIGNATURE_SECRET=
        """;
    }

    private static string GitIgnore()
    {
        return """
        node_modules/
        .env
        *.log
        npm-debug.log*
        """;
    }

    private static string PackageJson(
        string projectName)
    {
        return $$"""
        {
          "name": "{{projectName}}",
          "version": "1.0.0",
          "private": true,
          "description": "WeselAja integration generated by WeselAja MCP",
          "main": "index.js",
          "scripts": {
            "start": "node index.js",
            "payins": "node index.js payins",
            "callback": "node callback-server.js",
            "redirect": "node redirect-server.js",
            "test": "node --test"
          },
          "engines": {
            "node": ">=18"
          },
          "dependencies": {
            "dotenv": "^16.4.7"
          }
        }
        """;
    }

    private static string IndexJs()
    {
        return """
        const {
            WeselAjaClient
        } = require("./src/weselaja-client");

        async function main() {
            const client =
                new WeselAjaClient();

            if (
                process.argv[2] ===
                "payins"
            ) {
                const status =
                    process.argv[3] || "";

                const limitValue =
                    process.argv[4];

                const limit =
                    limitValue
                        ? Number(limitValue)
                        : null;

                try {
                    const result =
                        await client.getPayins({
                            status,
                            limit
                        });

                    console.log(
                        JSON.stringify(
                            result,
                            null,
                            2
                        )
                    );
                }
                catch (error) {
                    console.error(
                        error?.message ?? error
                    );

                    process.exitCode = 1;
                }

                return;
            }

            try {
                const payment =
                    await client.createPayment({
                        amount:
                            10000,

                        customerName:
                            "Andi",

                        bank:
                            "BRI",

                        description:
                            "WeselAja JavaScript sandbox example"
                    });

                console.log(
                    "Payment created successfully."
                );

                console.log(
                    `Status: ${payment?.status ?? "-"}`
                );

                console.log(
                    `Reference: ${
                        payment?.referenceCode ?? "-"
                    }`
                );

                console.log(
                    `Channel: ${
                        payment?.paymentChannel ?? "-"
                    }`
                );

                console.log(
                    `Payment: ${
                        payment?.paymentCode ?? "-"
                    }`
                );
            }
            catch (error) {
                console.error(
                    "Payment creation failed."
                );

                console.error(
                    error?.message ?? error
                );

                process.exitCode = 1;
            }
        }

        main();
        """;
    }

    private static string BankChannelsJs()
    {
        return """
        const BANK_CHANNELS =
            Object.freeze({
                BRI:
                    "BRI.VA",

                MANDIRI:
                    "MDR.VA",

                BNI:
                    "BNI.VA",

                PERMATA:
                    "PTB.VA",

                CIMB:
                    "CIMB.VA",

                BCA:
                    "BCA.VA"
            });

        function normalizeBankName(
            bank
        ) {
            return String(
                bank ?? ""
            )
                .trim()
                .toUpperCase()
                .replace(
                    /[\s_-]/g,
                    ""
                )
                .replace(
                    /^BANK/,
                    ""
                );
        }

        function getPaymentChannel(
            bank
        ) {
            const normalizedBank =
                normalizeBankName(
                    bank
                );

            const channel =
                BANK_CHANNELS[
                    normalizedBank
                ];

            if (!channel) {
                throw new Error(
                    `Unsupported bank: ${bank}. ` +
                    `Supported banks: ` +
                    `${Object.keys(
                        BANK_CHANNELS
                    ).join(", ")}`
                );
            }

            return channel;
        }

        module.exports = {
            BANK_CHANNELS,
            normalizeBankName,
            getPaymentChannel
        };
        """;
    }

    private static string ConfigJs()
    {
        return """
        const dotenv =
            require("dotenv");

        dotenv.config();

        const config = {
            apiUrl:
                (
                    process.env
                        .WESELAJA_API_URL
                    ||
                    "https://sandbox.checkout.weselaja.id"
                ).trim(),

            apiKey:
                (
                    process.env
                        .WESELAJA_API_KEY
                    ||
                    ""
                ).trim(),

            secretKey:
                (
                    process.env
                        .WESELAJA_SECRET_KEY
                    ||
                    ""
                ).trim(),

            customerReference:
                (
                    process.env
                        .WESELAJA_CUSTOMER_REFERENCE
                    ||
                    ""
                ).trim(),

            callbackUrl:
                (
                    process.env
                        .WESELAJA_CALLBACK_URL
                    ||
                    ""
                ).trim(),

            redirectUrl:
                (
                    process.env
                        .WESELAJA_REDIRECT_URL
                    ||
                    ""
                ).trim(),

            currency:
                (
                    process.env
                        .WESELAJA_CURRENCY
                    ||
                    "IDR"
                ).trim(),

            paymentMethod:
                (
                    process.env
                        .WESELAJA_PAYMENT_METHOD
                    ||
                    "VIRTUAL_ACCOUNT"
                ).trim()
        };

        function validateConfig() {
            if (!config.apiKey) {
                throw new Error(
                    "WESELAJA_API_KEY is required."
                );
            }

            if (!config.secretKey) {
                throw new Error(
                    "WESELAJA_SECRET_KEY is required."
                );
            }
        }

        module.exports = {
            config,
            validateConfig
        };
        """;
    }

    private static string ErrorsJs()
    {
        return """
        class WeselAjaError
            extends Error {

            constructor(
                message,
                code = "WESELAJA_ERROR",
                status = null,
                response = null
            ) {
                super(message);

                this.name =
                    "WeselAjaError";

                this.code =
                    code;

                this.status =
                    status;

                this.response =
                    response;
            }
        }

        class WeselAjaConfigurationError
            extends WeselAjaError {

            constructor(message) {
                super(
                    message,
                    "CONFIGURATION_ERROR"
                );

                this.name =
                    "WeselAjaConfigurationError";
            }
        }

        class WeselAjaApiError
            extends WeselAjaError {

            constructor(
                message,
                status,
                response = null
            ) {
                super(
                    message,
                    "PAYMENT_FAILED",
                    status,
                    response
                );

                this.name =
                    "WeselAjaApiError";
            }
        }

        module.exports = {
            WeselAjaError,
            WeselAjaConfigurationError,
            WeselAjaApiError
        };
        """;
    }

    private static string SignatureJs()
    {
        return """
        const crypto =
            require("crypto");

        function generateTimestamp() {
            return new Date()
                .toISOString();
        }

        function generateIdempotencyKey() {
            return crypto.randomUUID();
        }

        function generateSignature({
            method,
            uri,
            timestamp,
            body,
            secretKey
        }) {
            const payload = [
                String(method).toUpperCase(),
                uri,
                timestamp,
                body
            ].join("\n");

            return crypto
                .createHmac(
                    "sha256",
                    secretKey
                )
                .update(
                    payload,
                    "utf8"
                )
                .digest("base64");
        }

        function generateCallbackSignature({
            method,
            uri,
            rawBody,
            timestamp,
            secretKey
        }) {
            const payload = [
                String(method).toUpperCase(),
                uri,
                rawBody,
                timestamp
            ].join("\n");

            return crypto
                .createHmac(
                    "sha256",
                    secretKey
                )
                .update(
                    payload,
                    "utf8"
                )
                .digest("base64");
        }

        function verifyCallbackSignature({
            method,
            uri,
            rawBody,
            timestamp,
            signature,
            secretKey
        }) {
            if (
                !signature ||
                !secretKey
            ) {
                return false;
            }

            const expected =
                generateCallbackSignature({
                    method,
                    uri,
                    rawBody,
                    timestamp,
                    secretKey
                });

            const expectedBuffer =
                Buffer.from(
                    expected,
                    "utf8"
                );

            const receivedBuffer =
                Buffer.from(
                    String(signature).trim(),
                    "utf8"
                );

            if (
                expectedBuffer.length !==
                receivedBuffer.length
            ) {
                return false;
            }

            return crypto.timingSafeEqual(
                expectedBuffer,
                receivedBuffer
            );
        }

        module.exports = {
            generateTimestamp,
            generateIdempotencyKey,
            generateSignature,
            generateCallbackSignature,
            verifyCallbackSignature
        };
        """;
    }

    private static string WeselAjaClientJs()
    {
        return """
        const {
            config,
            validateConfig
        } = require("./config");

        const {
            getPaymentChannel
        } = require("./bank-channels");

        const {
            generateTimestamp,
            generateIdempotencyKey,
            generateSignature
        } = require("./signature");

        const {
            WeselAjaConfigurationError,
            WeselAjaApiError
        } = require("./errors");

        class WeselAjaClient {
            constructor(
                options = {}
            ) {
                this.config = {
                    ...config,
                    ...options
                };
            }

            async createPayment({
                amount,
                customerName,
                bank,
                description = "",
                referenceCode = ""
            }) {
                this.validateRequest({
                    amount,
                    customerName,
                    bank
                });

                this.validateCredentials();

                const paymentChannel =
                    getPaymentChannel(
                        bank
                    );

                const actualReferenceCode =
                    referenceCode ||
                    this.generateReferenceCode();

                const payload = {
                    initiatedAmount:
                        amount,

                    currency:
                        this.config.currency,

                    paymentMethod:
                        this.config.paymentMethod,

                    paymentChannel,

                    referenceCode:
                        actualReferenceCode,

                    customerName
                };

                if (
                    this.config
                        .customerReference
                ) {
                    payload.customerReference =
                        this.config
                            .customerReference;
                }

                if (
                    this.config
                        .callbackUrl
                ) {
                    payload.callbackUrl =
                        this.config
                            .callbackUrl;
                }

                if (
                    this.config
                        .redirectUrl
                ) {
                    payload.redirectUrl =
                        this.config
                            .redirectUrl;
                }

                if (description) {
                    payload.description =
                        description;
                }

                const body =
                    JSON.stringify(
                        payload
                    );

                const uri =
                    "/v1/payins";

                const timestamp =
                    generateTimestamp();

                const signature =
                    generateSignature({
                        method:
                            "POST",

                        uri,

                        timestamp,

                        body,

                        secretKey:
                            this.config
                                .secretKey
                    });

                const idempotencyKey =
                    generateIdempotencyKey();

                const baseUrl =
                    this.config.apiUrl
                        .replace(
                            /\/$/,
                            ""
                        );

                const response =
                    await fetch(
                        `${baseUrl}${uri}`,
                        {
                            method:
                                "POST",

                            redirect:
                                "manual",

                            headers: {
                                "Content-Type":
                                    "application/json",

                                "Accept":
                                    "application/json",

                                "Xenith-Api-Key":
                                    this.config
                                        .apiKey,

                                "Xenith-Request-Timestamp":
                                    timestamp,

                                "Xenith-Request-Signature":
                                    signature,

                                "X-Idempotency-Key":
                                    idempotencyKey
                            },

                            body
                        }
                    );

                const rawResponse =
                    await response.text();

                let parsedResponse;

                try {
                    parsedResponse =
                        rawResponse
                            ? JSON.parse(
                                rawResponse
                            )
                            : null;
                }
                catch {
                    parsedResponse =
                        rawResponse;
                }

                if (!response.ok) {
                    throw new WeselAjaApiError(
                        `WeselAja API request ` +
                        `failed with HTTP ` +
                        `${response.status}.`,
                        response.status,
                        parsedResponse
                    );
                }

                return parsedResponse;
            }

            async getPayins({
                status = "",
                limit = null
            } = {}) {
                this.validateCredentials();

                const normalizedStatus =
                    status
                        ? String(
                            status
                        )
                            .trim()
                            .toUpperCase()
                        : "";

                if (
                    normalizedStatus
                    &&
                    ![
                        "PENDING",
                        "SUCCESS",
                        "EXPIRED",
                        "FAILED"
                    ].includes(
                        normalizedStatus
                    )
                ) {
                    throw new Error(
                        "status must be one of: PENDING, SUCCESS, EXPIRED, FAILED."
                    );
                }

                if (
                    limit !== null
                    &&
                    (
                        !Number.isInteger(
                            limit
                        )
                        ||
                        limit <= 0
                    )
                ) {
                    throw new Error(
                        "limit must be a positive integer."
                    );
                }

                const query =
                    new URLSearchParams();

                if (normalizedStatus) {
                    query.set(
                        "status",
                        normalizedStatus
                    );
                }

                if (limit !== null) {
                    query.set(
                        "limit",
                        String(limit)
                    );
                }

                const uri =
                    "/v1/payins";

                const queryString =
                    query.toString();

                const baseUrl =
                    this.config.apiUrl.endsWith("/")
                        ? this.config.apiUrl.slice(
                            0,
                            -1
                        )
                        : this.config.apiUrl;

                const requestUrl =
                    baseUrl +
                    uri +
                    (
                        queryString
                            ? `?${queryString}`
                            : ""
                    );

                const timestamp =
                    generateTimestamp();

                const signature =
                    generateSignature({
                        method:
                            "GET",

                        uri,

                        timestamp,

                        body:
                            "",

                        secretKey:
                            this.config
                                .secretKey
                    });

                const response =
                    await fetch(
                        requestUrl,
                        {
                            method:
                                "GET",

                            redirect:
                                "manual",

                            headers: {
                                "Accept":
                                    "application/json",

                                "Xenith-Api-Key":
                                    this.config
                                        .apiKey,

                                "Xenith-Request-Timestamp":
                                    timestamp,

                                "Xenith-Request-Signature":
                                    signature
                            }
                        }
                    );

                const rawResponse =
                    await response.text();

                let parsedResponse;

                try {
                    parsedResponse =
                        rawResponse
                            ? JSON.parse(
                                rawResponse
                            )
                            : null;
                }
                catch {
                    parsedResponse =
                        rawResponse;
                }

                if (!response.ok) {
                    throw new WeselAjaApiError(
                        `WeselAja API request failed ` +
                        `with HTTP ${response.status}.`,
                        response.status,
                        parsedResponse
                    );
                }

                return parsedResponse;
            }

            validateCredentials() {
                try {
                    validateConfig();
                }
                catch (error) {
                    throw new
                        WeselAjaConfigurationError(
                            error.message
                        );
                }

                if (
                    !this.config.apiKey
                ) {
                    throw new
                        WeselAjaConfigurationError(
                            "WESELAJA_API_KEY is required."
                        );
                }

                if (
                    !this.config.secretKey
                ) {
                    throw new
                        WeselAjaConfigurationError(
                            "WESELAJA_SECRET_KEY is required."
                        );
                }
            }

            validateRequest({
                amount,
                customerName,
                bank
            }) {
                if (
                    !Number.isInteger(
                        amount
                    )
                    ||
                    amount <= 0
                ) {
                    throw new Error(
                        "amount must be a positive integer."
                    );
                }

                if (
                    !customerName
                    ||
                    !String(
                        customerName
                    ).trim()
                ) {
                    throw new Error(
                        "customerName is required."
                    );
                }

                if (
                    !bank
                    ||
                    !String(
                        bank
                    ).trim()
                ) {
                    throw new Error(
                        "bank is required."
                    );
                }
            }

            generateReferenceCode() {
                return `JS-${Date.now()}`;
            }
        }

        module.exports = {
            WeselAjaClient
        };
        """;
    }

    private static string RedirectServerJs()
    {
        return """
        const http =
            require(
                "node:http"
            );

        const {
            URL
        } =
            require(
                "node:url"
            );

        require("dotenv").config();

        const port =
            Number(
                process.env
                    .WESELAJA_REDIRECT_PORT
                ||
                8080
            );

        function renderJson(
            value
        ) {
            return JSON.stringify(
                value,
                null,
                2
            );
        }

        const server =
            http.createServer(
                (
                    request,
                    response
                ) => {
                    const requestUrl =
                        new URL(
                            request.url
                            ||
                            "/",
                            `http://${
                                request.headers.host
                                ||
                                "localhost"
                            }`
                        );

                    if (
                        requestUrl.pathname !==
                        "/payment/weselaja/return"
                    ) {
                        response.writeHead(
                            404,
                            {
                                "content-type":
                                    "application/json"
                            }
                        );

                        response.end(
                            renderJson({
                                success:
                                    false,

                                error:
                                    "Not found"
                            })
                        );

                        return;
                    }

                    if (
                        request.method !==
                        "GET"
                    ) {
                        response.writeHead(
                            405,
                            {
                                "content-type":
                                    "application/json"
                            }
                        );

                        response.end(
                            renderJson({
                                success:
                                    false,

                                error:
                                    "Method not allowed"
                            })
                        );

                        return;
                    }

                    const params =
                        Object.fromEntries(
                            requestUrl
                                .searchParams
                                .entries()
                        );

                    response.writeHead(
                        200,
                        {
                            "content-type":
                                "application/json"
                        }
                    );

                    response.end(
                        renderJson({
                            success:
                                true,

                            path:
                                requestUrl
                                    .pathname,

                            parameters:
                                params
                        })
                    );
                }
            );

        server.listen(
            port,
            "0.0.0.0",
            () => {
                console.log(
                    `WeselAja redirect handler ` +
                    `listening on ` +
                    `http://0.0.0.0:${port}` +
                    `/payment/weselaja/return`
                );
            }
        );
        """;
    }

    private static string CallbackServerJs()
    {
        return """
        const http =
            require(
                "node:http"
            );

        const {
            URL
        } =
            require(
                "node:url"
            );

        const {
            verifyCallbackSignature
        } =
            require(
                "./src/signature"
            );

        require("dotenv").config();

        const port =
            Number(
                process.env
                    .WESELAJA_CALLBACK_PORT
                ||
                8081
            );

        const secret =
            (
                process.env
                    .WEB_SIGNATURE_SECRET
                ||
                ""
            ).trim();

        if (!secret) {
            console.error(
                "WEB_SIGNATURE_SECRET is required."
            );

            process.exit(1);
        }

        function sendJson(
            response,
            statusCode,
            value
        ) {
            response.writeHead(
                statusCode,
                {
                    "content-type":
                        "application/json"
                }
            );

            response.end(
                JSON.stringify(
                    value,
                    null,
                    2
                )
            );
        }

        const server =
            http.createServer(
                (
                    request,
                    response
                ) => {
                    const requestUrl =
                        new URL(
                            request.url
                            ||
                            "/",
                            `http://${
                                request.headers.host
                                ||
                                "localhost"
                            }`
                        );

                    if (
                        requestUrl.pathname !==
                        "/payment/weselaja/callback"
                    ) {
                        sendJson(
                            response,
                            404,
                            {
                                success:
                                    false,

                                error:
                                    "Not found"
                            }
                        );

                        return;
                    }

                    if (
                        request.method !==
                        "POST"
                    ) {
                        sendJson(
                            response,
                            405,
                            {
                                success:
                                    false,

                                error:
                                    "Method not allowed"
                            }
                        );

                        return;
                    }

                    const timestamp =
                        request.headers[
                            "x-middleware-timestamp"
                        ];

                    const signature =
                        request.headers[
                            "x-middleware-signature"
                        ];

                    if (!timestamp) {
                        sendJson(
                            response,
                            400,
                            {
                                success:
                                    false,

                                error:
                                    "Missing x-middleware-timestamp"
                            }
                        );

                        return;
                    }

                    if (!signature) {
                        sendJson(
                            response,
                            400,
                            {
                                success:
                                    false,

                                error:
                                    "Missing x-middleware-signature"
                            }
                        );

                        return;
                    }

                    const chunks = [];

                    request.on(
                        "data",
                        chunk => {
                            chunks.push(
                                chunk
                            );
                        }
                    );

                    request.on(
                        "end",
                        () => {
                            const rawBody =
                                Buffer
                                    .concat(
                                        chunks
                                    )
                                    .toString(
                                        "utf8"
                                    );

                            const uri =
                                requestUrl
                                    .pathname +
                                (
                                    requestUrl
                                        .search
                                    ||
                                    ""
                                );

                            const verified =
                                verifyCallbackSignature({
                                    method:
                                        "POST",

                                    uri,

                                    rawBody,

                                    timestamp:
                                        String(
                                            timestamp
                                        ),

                                    signature:
                                        String(
                                            signature
                                        ),

                                    secretKey:
                                        secret
                                });

                            if (!verified) {
                                sendJson(
                                    response,
                                    401,
                                    {
                                        success:
                                            false,

                                        verified:
                                            false,

                                        error:
                                            "Invalid callback signature"
                                    }
                                );

                                return;
                            }

                            let body;

                            try {
                                body =
                                    rawBody
                                        ? JSON.parse(
                                            rawBody
                                        )
                                        : null;
                            }
                            catch {
                                body =
                                    rawBody;
                            }

                            sendJson(
                                response,
                                200,
                                {
                                    success:
                                        true,

                                    verified:
                                        true,

                                    body
                                }
                            );
                        }
                    );

                    request.on(
                        "error",
                        () => {
                            sendJson(
                                response,
                                400,
                                {
                                    success:
                                        false,

                                    error:
                                        "Unable to read request body"
                                }
                            );
                        }
                    );
                }
            );

        server.listen(
            port,
            "0.0.0.0",
            () => {
                console.log(
                    `WeselAja callback handler ` +
                    `listening on ` +
                    `http://0.0.0.0:${port}` +
                    `/payment/weselaja/callback`
                );
            }
        );
        """;
    }

    private static string TestJs()
    {
        return """
        const test =
            require(
                "node:test"
            );

        const assert =
            require(
                "node:assert/strict"
            );

        const crypto =
            require(
                "crypto"
            );

        const {
            getPaymentChannel
        } =
            require(
                "../src/bank-channels"
            );

        const {
            generateSignature,
            generateCallbackSignature,
            verifyCallbackSignature
        } =
            require(
                "../src/signature"
            );

        test(
            "should map supported banks",
            () => {
                assert.equal(
                    getPaymentChannel(
                        "BRI"
                    ),
                    "BRI.VA"
                );

                assert.equal(
                    getPaymentChannel(
                        "Mandiri"
                    ),
                    "MDR.VA"
                );

                assert.equal(
                    getPaymentChannel(
                        "BNI"
                    ),
                    "BNI.VA"
                );

                assert.equal(
                    getPaymentChannel(
                        "Permata"
                    ),
                    "PTB.VA"
                );

                assert.equal(
                    getPaymentChannel(
                        "CIMB"
                    ),
                    "CIMB.VA"
                );

                assert.equal(
                    getPaymentChannel(
                        "BCA"
                    ),
                    "BCA.VA"
                );
            }
        );

        test(
            "should normalize bank names",
            () => {
                assert.equal(
                    getPaymentChannel(
                        " bca "
                    ),
                    "BCA.VA"
                );

                assert.equal(
                    getPaymentChannel(
                        "Bank BCA"
                    ),
                    "BCA.VA"
                );

                assert.equal(
                    getPaymentChannel(
                        "CIMB Niaga"
                    ),
                    "CIMB.VA"
                );

                assert.equal(
                    getPaymentChannel(
                        "Bank Mandiri"
                    ),
                    "MDR.VA"
                );
            }
        );

        test(
            "should reject unsupported banks",
            () => {
                assert.throws(
                    () =>
                        getPaymentChannel(
                            "UNKNOWN"
                        ),
                    /Unsupported bank/
                );
            }
        );

        test(
            "should generate valid HMAC-SHA256 signature",
            () => {
                const method =
                    "POST";

                const uri =
                    "/v1/payins";

                const timestamp =
                    "2026-01-01T00:00:00.000Z";

                const body =
                    "{\"hello\":\"world\"}";

                const secretKey =
                    "secret";

                const signingPayload =
                    [
                        method,
                        uri,
                        timestamp,
                        body
                    ].join(
                        "\n"
                    );

                const expectedSignature =
                    crypto
                        .createHmac(
                            "sha256",
                            secretKey
                        )
                        .update(
                            signingPayload,
                            "utf8"
                        )
                        .digest(
                            "base64"
                        );

                const actualSignature =
                    generateSignature({
                        method,
                        uri,
                        timestamp,
                        body,
                        secretKey
                    });

                assert.equal(
                    actualSignature,
                    expectedSignature
                );
            }
        );

        test(
            "should verify callback signature",
            () => {
                const method =
                    "POST";

                const uri =
                    "/payment/weselaja/callback";

                const body =
                    "{\"hello\":\"world\"}";

                const timestamp =
                    "2026-01-01T00:00:00.000Z";

                const secretKey =
                    "test-secret";

                const signature =
                    generateCallbackSignature({
                        method,
                        uri,
                        rawBody:
                            body,
                        timestamp,
                        secretKey
                    });

                assert.equal(
                    verifyCallbackSignature({
                        method,
                        uri,
                        rawBody:
                            body,
                        timestamp,
                        signature,
                        secretKey
                    }),
                    true
                );

                assert.equal(
                    verifyCallbackSignature({
                        method,
                        uri,
                        rawBody:
                            body,
                        timestamp,
                        signature:
                            signature + "x",
                        secretKey
                    }),
                    false
                );
            }
        );
        """;
    }

    private static string Readme(
        string projectName)
    {
        return $$"""
        # {{projectName}}

        WeselAja payment integration generated by the WeselAja MCP Code Generator.

        ## Scope

        This generated project supports:

        - Create payment
        - Get payins
        - Virtual Account payment
        - BRI
        - Mandiri
        - BNI
        - Permata
        - CIMB
        - BCA
        - HMAC-SHA256 request signature
        - Idempotency key
        - Environment-based configuration
        - API error handling
        - Callback handling
        - Callback signature verification
        - Redirect handling

        ## Requirements

        - Node.js 18+
        - npm

        ## Install

        ```bash
        npm install
        ```

        ## Configuration

        ```powershell
        Copy-Item .env.example .env
        ```

        Configure:

        ```env
        WESELAJA_API_URL=https://sandbox.checkout.weselaja.id
        WESELAJA_API_KEY=
        WESELAJA_SECRET_KEY=
        WESELAJA_CUSTOMER_REFERENCE=
        WESELAJA_CALLBACK_URL=
        WESELAJA_REDIRECT_URL=
        WESELAJA_CURRENCY=IDR
        WESELAJA_PAYMENT_METHOD=VIRTUAL_ACCOUNT

        WESELAJA_CALLBACK_PORT=8081
        WESELAJA_REDIRECT_PORT=8080

        WEB_SIGNATURE_SECRET=
        ```

        ## Create payment

        ```bash
        npm start
        ```

        ## Get payins

        ```bash
        npm run payins
        ```

        With filters:

        ```bash
        node index.js payins SUCCESS 50
        ```

        Supported statuses:

        ```text
        PENDING
        SUCCESS
        EXPIRED
        FAILED
        ```

        The client uses:

        ```text
        GET /v1/payins
        ```

        ## Callback

        Start:

        ```bash
        npm run callback
        ```

        Endpoint:

        ```text
        POST /payment/weselaja/callback
        ```

        Default local address:

        ```text
        http://127.0.0.1:8081/payment/weselaja/callback
        ```

        The handler verifies:

        ```text
        x-middleware-timestamp
        x-middleware-signature
        ```

        Signature payload:

        ```text
        POST
        /payment/weselaja/callback
        <raw body>
        <timestamp>
        ```

        HMAC-SHA256 is used with `WEB_SIGNATURE_SECRET`
        and the result is Base64 encoded.

        ## Redirect

        Start:

        ```bash
        npm run redirect
        ```

        Endpoint:

        ```text
        GET /payment/weselaja/return
        ```

        Default local address:

        ```text
        http://127.0.0.1:8080/payment/weselaja/return
        ```

        ## Test

        ```bash
        npm test
        ```

        ## Project Structure

        ```text
        {{projectName}}/
        ├── src/
        │   ├── bank-channels.js
        │   ├── config.js
        │   ├── errors.js
        │   ├── signature.js
        │   └── weselaja-client.js
        ├── tests/
        │   └── weselaja.test.js
        ├── .env.example
        ├── .gitignore
        ├── callback-server.js
        ├── index.js
        ├── package.json
        └── redirect-server.js
        ```

        ## Request signing

        ```text
        METHOD
        URI
        TIMESTAMP
        BODY
        ```

        Headers:

        ```text
        Xenith-Api-Key
        Xenith-Request-Timestamp
        Xenith-Request-Signature
        X-Idempotency-Key
        ```

        ## Callback signing

        ```text
        POST
        /payment/weselaja/callback
        <raw body>
        <timestamp>
        ```

        """;
    }
}