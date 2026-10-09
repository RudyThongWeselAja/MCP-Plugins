using System.Reflection;
using System.Text;

namespace WeselAjaMcpServer.Generators;

public class TypeScriptWeselAjaGenerator
{
    public GenerationResult Generate(
        string projectName,
        bool includeExample,
        bool includeTests,
        bool overwrite)
    {
        var safeProjectName =
            SanitizeProjectName(projectName);

        var outputDirectory =
            GetOutputDirectory(
                "typescript",
                safeProjectName);

        if (
            Directory.Exists(outputDirectory)
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
            "tsconfig.json",
            TsConfig(),
            files);

        AddGeneratedFile(
            outputDirectory,
            "README.md",
            Readme(safeProjectName),
            files);

        AddGeneratedFile(
            outputDirectory,
            Path.Combine(
                "src",
                "redirect-server.ts"),
            RedirectServerTs(),
            files);

        AddGeneratedFile(
            outputDirectory,
            Path.Combine(
                "src",
                "callback-server.ts"),
            CallbackServerTs(),
            files);

        if (includeExample)
        {
            AddGeneratedFile(
                outputDirectory,
                Path.Combine(
                    "src",
                    "index.ts"),
                IndexTs(),
                files);
        }

        AddGeneratedFile(
            outputDirectory,
            Path.Combine(
                "src",
                "bank-channels.ts"),
            BankChannelsTs(),
            files);

        AddGeneratedFile(
            outputDirectory,
            Path.Combine(
                "src",
                "config.ts"),
            ConfigTs(),
            files);

        AddGeneratedFile(
            outputDirectory,
            Path.Combine(
                "src",
                "errors.ts"),
            ErrorsTs(),
            files);

        AddGeneratedFile(
            outputDirectory,
            Path.Combine(
                "src",
                "signature.ts"),
            SignatureTs(),
            files);

        AddGeneratedFile(
            outputDirectory,
            Path.Combine(
                "src",
                "models.ts"),
            ModelsTs(),
            files);

        AddGeneratedFile(
            outputDirectory,
            Path.Combine(
                "src",
                "weselaja-client.ts"),
            WeselAjaClientTs(),
            files);

        if (includeTests)
        {
            AddGeneratedFile(
                outputDirectory,
                Path.Combine(
                    "tests",
                    "weselaja.test.ts"),
                TestTs(),
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
                parameterlessConstructor.Invoke(null);

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

                    var previousStringCount =
                        arguments
                            .Take(index)
                            .Count(
                                value =>
                                    value is string);

                    arguments[index] =
                        previousStringCount == 0
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
            Path.GetFullPath(outputRoot);

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
                ".." + Path.DirectorySeparatorChar,
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
            return "weselaja-typescript";
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
        dist/
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
          "description": "WeselAja TypeScript integration generated by WeselAja MCP",
          "type": "module",
          "scripts": {
            "start": "tsx src/index.ts",
            "payins": "tsx src/index.ts payins",
            "callback": "tsx src/callback-server.ts",
            "redirect": "tsx src/redirect-server.ts",
            "build": "tsc",
            "test": "tsx --test tests/weselaja.test.ts"
          },
          "engines": {
            "node": ">=18"
          },
          "dependencies": {
            "dotenv": "^16.4.7"
          },
          "devDependencies": {
            "@types/node": "^22.13.4",
            "tsx": "^4.19.3",
            "typescript": "^5.8.3"
          }
        }
        """;
    }

    private static string TsConfig()
    {
        return """
        {
          "compilerOptions": {
            "target": "ES2022",
            "module": "NodeNext",
            "moduleResolution": "NodeNext",
            "lib": [
              "ES2022",
              "DOM"
            ],
            "types": [
              "node"
            ],
            "strict": true,
            "esModuleInterop": true,
            "forceConsistentCasingInFileNames": true,
            "skipLibCheck": true,
            "outDir": "dist",
            "rootDir": ".",
            "declaration": true,
            "sourceMap": true
          },
          "include": [
            "src/**/*.ts",
            "tests/**/*.ts"
          ],
          "exclude": [
            "node_modules",
            "dist"
          ]
        }
        """;
    }

    private static string IndexTs()
    {
        return """
        import {
            WeselAjaClient
        } from "./weselaja-client.js";

        async function main(): Promise<void> {
            const client =
                new WeselAjaClient();

            if (
                process.argv[2] ===
                "payins"
            ) {
                const status =
                    process.argv[3] ?? "";

                const limitValue =
                    process.argv[4];

                const limit =
                    limitValue !== undefined
                        ? Number(limitValue)
                        : undefined;

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
                catch (error: unknown) {
                    if (
                        error instanceof Error
                    ) {
                        console.error(
                            error.message
                        );
                    }
                    else {
                        console.error(
                            error
                        );
                    }

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
                            "WeselAja TypeScript sandbox example"
                    });

                console.log(
                    "Payment created successfully."
                );

                console.log(
                    `Status: ${payment.status ?? "-"}`
                );

                console.log(
                    `Reference: ${
                        payment.referenceCode ?? "-"
                    }`
                );

                console.log(
                    `Channel: ${
                        payment.paymentChannel ?? "-"
                    }`
                );

                console.log(
                    `Payment: ${
                        payment.paymentCode ?? "-"
                    }`
                );
            }
            catch (error: unknown) {
                console.error(
                    "Payment creation failed."
                );

                if (
                    error instanceof Error
                ) {
                    console.error(
                        error.message
                    );
                }
                else {
                    console.error(
                        error
                    );
                }

                process.exitCode = 1;
            }
        }

        void main();
        """;
    }

    private static string ModelsTs()
    {
        return """
        export interface CreatePaymentRequest {
            amount: number;
            customerName: string;
            bank: string;
            description?: string;
            referenceCode?: string;
        }

        export interface CreatePaymentResponse {
            id?: string;
            initiatedAmount?: number;
            paymentAmount?: number;
            feeAmount?: number;
            currency?: string;
            paymentMethod?: string;
            paymentChannel?: string;
            paymentCode?: string;
            paymentCodeType?: string;
            referenceCode?: string;
            customerReference?: string;
            customerName?: string;
            status?: string;
            createdTime?: string;
            updatedTime?: string;
            expirationTime?: string;
            description?: string;
            callbackUrl?: string;
            redirectUrl?: string;
            isSuccess?: boolean;
            [key: string]: unknown;
        }

        export interface WeselAjaConfig {
            apiUrl: string;
            apiKey: string;
            secretKey: string;
            customerReference: string;
            callbackUrl: string;
            redirectUrl: string;
            currency: string;
            paymentMethod: string;
        }
        """;
    }

    private static string BankChannelsTs()
    {
        return """
        export const BANK_CHANNELS:
            Readonly<Record<string, string>> =
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

        export function normalizeBankName(
            bank: string
        ): string {
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

        export function getPaymentChannel(
            bank: string
        ): string {
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
        """;
    }

    private static string ConfigTs()
    {
        return """
        import dotenv from "dotenv";

        import type {
            WeselAjaConfig
        } from "./models.js";

        dotenv.config();

        export const config:
            WeselAjaConfig = {
                apiUrl:
                    (
                        process.env
                            .WESELAJA_API_URL
                        ??
                        "https://sandbox.checkout.weselaja.id"
                    ).trim(),

                apiKey:
                    (
                        process.env
                            .WESELAJA_API_KEY
                        ??
                        ""
                    ).trim(),

                secretKey:
                    (
                        process.env
                            .WESELAJA_SECRET_KEY
                        ??
                        ""
                    ).trim(),

                customerReference:
                    (
                        process.env
                            .WESELAJA_CUSTOMER_REFERENCE
                        ??
                        ""
                    ).trim(),

                callbackUrl:
                    (
                        process.env
                            .WESELAJA_CALLBACK_URL
                        ??
                        ""
                    ).trim(),

                redirectUrl:
                    (
                        process.env
                            .WESELAJA_REDIRECT_URL
                        ??
                        ""
                    ).trim(),

                currency:
                    (
                        process.env
                            .WESELAJA_CURRENCY
                        ??
                        "IDR"
                    ).trim(),

                paymentMethod:
                    (
                        process.env
                            .WESELAJA_PAYMENT_METHOD
                        ??
                        "VIRTUAL_ACCOUNT"
                    ).trim()
            };

        export function validateConfig(): void {
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
        """;
    }

    private static string ErrorsTs()
    {
        return """
        export class WeselAjaError
            extends Error {

            public readonly code: string;

            public readonly status:
                number | null;

            public readonly response:
                unknown;

            public constructor(
                message: string,
                code =
                    "WESELAJA_ERROR",
                status:
                    number | null = null,
                response:
                    unknown = null
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

        export class WeselAjaConfigurationError
            extends WeselAjaError {

            public constructor(
                message: string
            ) {
                super(
                    message,
                    "CONFIGURATION_ERROR"
                );

                this.name =
                    "WeselAjaConfigurationError";
            }
        }

        export class WeselAjaApiError
            extends WeselAjaError {

            public constructor(
                message: string,
                status: number,
                response:
                    unknown = null
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
        """;
    }

    private static string SignatureTs()
    {
        return """
        import crypto from "node:crypto";

        export function generateTimestamp():
            string {
            return new Date().toISOString();
        }

        export function generateIdempotencyKey():
            string {
            return crypto.randomUUID();
        }

        export interface SignatureInput {
            method: string;
            uri: string;
            timestamp: string;
            body: string;
            secretKey: string;
        }

        export function generateSignature(
            input: SignatureInput
        ): string {
            const payload = [
                input.method.toUpperCase(),
                input.uri,
                input.timestamp,
                input.body
            ].join("\n");

            return crypto
                .createHmac(
                    "sha256",
                    input.secretKey
                )
                .update(
                    payload,
                    "utf8"
                )
                .digest("base64");
        }

        export interface CallbackSignatureInput {
            method: string;
            uri: string;
            rawBody: string;
            timestamp: string;
            secretKey: string;
        }

        export function generateCallbackSignature(
            input:
                CallbackSignatureInput
        ): string {
            const payload = [
                input.method.toUpperCase(),
                input.uri,
                input.rawBody,
                input.timestamp
            ].join("\n");

            return crypto
                .createHmac(
                    "sha256",
                    input.secretKey
                )
                .update(
                    payload,
                    "utf8"
                )
                .digest("base64");
        }

        export function verifyCallbackSignature(
            input: {
                method: string;
                uri: string;
                rawBody: string;
                timestamp: string;
                signature: string;
                secretKey: string;
            }
        ): boolean {
            if (
                !input.signature ||
                !input.secretKey
            ) {
                return false;
            }

            const expected =
                generateCallbackSignature({
                    method:
                        input.method,

                    uri:
                        input.uri,

                    rawBody:
                        input.rawBody,

                    timestamp:
                        input.timestamp,

                    secretKey:
                        input.secretKey
                });

            const expectedBuffer =
                Buffer.from(
                    expected,
                    "utf8"
                );

            const receivedBuffer =
                Buffer.from(
                    input.signature.trim(),
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
        """;
    }

    private static string WeselAjaClientTs()
    {
        return """
        import {
            config,
            validateConfig
        } from "./config.js";

        import {
            getPaymentChannel
        } from "./bank-channels.js";

        import {
            generateTimestamp,
            generateIdempotencyKey,
            generateSignature
        } from "./signature.js";

        import {
            WeselAjaConfigurationError,
            WeselAjaApiError
        } from "./errors.js";

        import type {
            CreatePaymentRequest,
            CreatePaymentResponse
        } from "./models.js";

        export class WeselAjaClient {
            private readonly config = {
                ...config
            };

            public constructor(
                options:
                    Partial<typeof config> = {}
            ) {
                Object.assign(
                    this.config,
                    options
                );
            }

            public async createPayment(
                request:
                    CreatePaymentRequest
            ): Promise<CreatePaymentResponse> {
                this.validateRequest(
                    request
                );

                this.validateCredentials();

                const paymentChannel =
                    getPaymentChannel(
                        request.bank
                    );

                const referenceCode =
                    request.referenceCode
                    ??
                    this.generateReferenceCode();

                const payload:
                    Record<string, unknown> = {
                    initiatedAmount:
                        request.amount,

                    currency:
                        this.config.currency,

                    paymentMethod:
                        this.config.paymentMethod,

                    paymentChannel,

                    referenceCode,

                    customerName:
                        request.customerName
                };

                if (
                    this.config.customerReference
                ) {
                    payload.customerReference =
                        this.config.customerReference;
                }

                if (
                    this.config.callbackUrl
                ) {
                    payload.callbackUrl =
                        this.config.callbackUrl;
                }

                if (
                    this.config.redirectUrl
                ) {
                    payload.redirectUrl =
                        this.config.redirectUrl;
                }

                if (
                    request.description
                ) {
                    payload.description =
                        request.description;
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
                            this.config.secretKey
                    });

                const idempotencyKey =
                    generateIdempotencyKey();

                const baseUrl =
                    this.config.apiUrl
                        .replace(
                            /\/$/,
                            ""
                        );

                let response: Response;

                try {
                    response =
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
                                        this.config.apiKey,

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
                }
                catch (
                    error: unknown
                ) {
                    const message =
                        error instanceof Error
                            ? error.message
                            : String(error);

                    throw new WeselAjaApiError(
                        `Failed to connect to WeselAja: ${message}`,
                        0
                    );
                }

                const rawResponse =
                    await response.text();

                let parsedResponse:
                    unknown = null;

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

                return parsedResponse as
                    CreatePaymentResponse;
            }

            public async getPayins(
                options: {
                    status?: string;
                    limit?: number;
                } = {}
            ): Promise<
                Record<string, unknown>
            > {
                this.validateCredentials();

                const normalizedStatus =
                    options.status
                        ?.trim()
                        .toUpperCase()
                    ??
                    "";

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
                    options.limit !== undefined
                    &&
                    (
                        !Number.isInteger(
                            options.limit
                        )
                        ||
                        options.limit <= 0
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

                if (
                    options.limit !== undefined
                ) {
                    query.set(
                        "limit",
                        String(
                            options.limit
                        )
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
                            this.config.secretKey
                    });

                let response: Response;

                try {
                    response =
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
                                        this.config.apiKey,

                                    "Xenith-Request-Timestamp":
                                        timestamp,

                                    "Xenith-Request-Signature":
                                        signature
                                }
                            }
                        );
                }
                catch (
                    error: unknown
                ) {
                    const message =
                        error instanceof Error
                            ? error.message
                            : String(error);

                    throw new WeselAjaApiError(
                        `Failed to connect to WeselAja: ${message}`,
                        0
                    );
                }

                const rawResponse =
                    await response.text();

                let parsedResponse:
                    unknown = null;

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

                if (
                    parsedResponse === null
                    ||
                    typeof parsedResponse !==
                        "object"
                    ||
                    Array.isArray(
                        parsedResponse
                    )
                ) {
                    throw new WeselAjaApiError(
                        "Unexpected response format from WeselAja.",
                        response.status,
                        parsedResponse
                    );
                }

                return parsedResponse as
                    Record<string, unknown>;
            }

            private validateCredentials():
                void {
                try {
                    validateConfig();
                }
                catch (
                    error: unknown
                ) {
                    const message =
                        error instanceof Error
                            ? error.message
                            : String(error);

                    throw new
                        WeselAjaConfigurationError(
                            message
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

            private validateRequest(
                request:
                    CreatePaymentRequest
            ): void {
                if (
                    !Number.isInteger(
                        request.amount
                    )
                    ||
                    request.amount <= 0
                ) {
                    throw new Error(
                        "amount must be a positive integer."
                    );
                }

                if (
                    !request.customerName
                    ||
                    !request.customerName.trim()
                ) {
                    throw new Error(
                        "customerName is required."
                    );
                }

                if (
                    !request.bank
                    ||
                    !request.bank.trim()
                ) {
                    throw new Error(
                        "bank is required."
                    );
                }
            }

            private generateReferenceCode():
                string {
                return `TS-${Date.now()}`;
            }
        }
        """;
    }

    private static string RedirectServerTs()
    {
        return """
        import dotenv from "dotenv";

        import http from "node:http";

        import { URL } from "node:url";

        dotenv.config();

        const port =
            Number(
                process.env
                    .WESELAJA_REDIRECT_PORT
                ||
                8080
            );

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
                            JSON.stringify(
                                {
                                    success:
                                        false,

                                    error:
                                        "Not found"
                                }
                            )
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
                            JSON.stringify(
                                {
                                    success:
                                        false,

                                    error:
                                        "Method not allowed"
                                }
                            )
                        );

                        return;
                    }

                    const parameters =
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
                        JSON.stringify(
                            {
                                success:
                                    true,

                                path:
                                    requestUrl.pathname,

                                parameters
                            },
                            null,
                            2
                        )
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

    private static string CallbackServerTs()
    {
        return """
        import dotenv from "dotenv";

        import http from "node:http";

        import {
            verifyCallbackSignature
        } from "./signature.js";

        import { URL } from "node:url";

        dotenv.config();

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
                ??
                ""
            ).trim();

        if (!secret) {
            console.error(
                "WEB_SIGNATURE_SECRET is required."
            );

            process.exit(1);
        }

        function sendJson(
            response:
                http.ServerResponse,
            statusCode:
                number,
            value:
                unknown
        ): void {
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

                    const timestampHeader =
                        request.headers[
                            "x-middleware-timestamp"
                        ];

                    const signatureHeader =
                        request.headers[
                            "x-middleware-signature"
                        ];

                    const timestamp =
                        Array.isArray(
                            timestampHeader
                        )
                            ? (
                                timestampHeader[0]
                                ??
                                ""
                            )
                            : (
                                timestampHeader
                                ??
                                ""
                            );

                    const signature =
                        Array.isArray(
                            signatureHeader
                        )
                            ? (
                                signatureHeader[0]
                                ??
                                ""
                            )
                            : (
                                signatureHeader
                                ??
                                ""
                            );

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

                    const chunks:
                        Buffer[] = [];

                    request.on(
                        "data",
                        (
                            chunk:
                                Buffer
                                |
                                string
                        ) => {
                            chunks.push(
                                Buffer.isBuffer(
                                    chunk
                                )
                                    ? chunk
                                    : Buffer.from(
                                        chunk
                                    )
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
                                requestUrl.pathname +
                                (
                                    requestUrl.search
                                    ||
                                    ""
                                );

                            const verified =
                                verifyCallbackSignature({
                                    method:
                                        "POST",

                                    uri,

                                    rawBody,

                                    timestamp,

                                    signature,

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

                            let body:
                                unknown;

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

    private static string TestTs()
    {
        return """
        import test from "node:test";

        import assert from "node:assert/strict";

        import crypto from "node:crypto";

        import {
            getPaymentChannel
        } from "../src/bank-channels.js";

        import {
            generateSignature,
            generateCallbackSignature,
            verifyCallbackSignature
        } from "../src/signature.js";

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
                        "mandiri"
                    ),
                    "MDR.VA"
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
            "should generate and verify callback signature",
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

        WeselAja TypeScript payment integration
        generated by the WeselAja MCP Code Generator.

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
        - TypeScript strict typing
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

        Windows PowerShell:

        ```powershell
        Copy-Item .env.example .env
        ```

        Linux/macOS:

        ```bash
        cp .env.example .env
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
        npx tsx src/index.ts payins SUCCESS 50
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

        The callback handler verifies:

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

        HMAC-SHA256 is calculated using
        `WEB_SIGNATURE_SECRET` and encoded using Base64.

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

        ## Type Check / Build

        ```bash
        npm run build
        ```

        ## Test

        ```bash
        npm test
        ```

        ## Run Example

        ```bash
        npm start
        ```

        The example creates:

        - Amount: IDR 10,000
        - Customer: Andi
        - Bank: BRI

        ## Supported Bank Channels

        | Bank | WeselAja Channel |
        |---|---|
        | BRI | BRI.VA |
        | Mandiri | MDR.VA |
        | BNI | BNI.VA |
        | Permata | PTB.VA |
        | CIMB | CIMB.VA |
        | BCA | BCA.VA |

        ## Signature

        Payment signature:

        ```text
        METHOD
        URI
        TIMESTAMP
        BODY
        ```

        Callback signature:

        ```text
        POST
        /payment/weselaja/callback
        <raw body>
        <timestamp>
        ```

        ## Headers

        Payment requests send:

        ```text
        Xenith-Api-Key
        Xenith-Request-Timestamp
        Xenith-Request-Signature
        X-Idempotency-Key
        ```

        ## Project Structure

        ```text
        {{projectName}}/
        ├── src/
        │   ├── bank-channels.ts
        │   ├── callback-server.ts
        │   ├── config.ts
        │   ├── errors.ts
        │   ├── index.ts
        │   ├── models.ts
        │   ├── redirect-server.ts
        │   ├── signature.ts
        │   └── weselaja-client.ts
        ├── tests/
        │   └── weselaja.test.ts
        ├── .env.example
        ├── .gitignore
        ├── package.json
        ├── tsconfig.json
        └── README.md
        ```
        """;
    }
}