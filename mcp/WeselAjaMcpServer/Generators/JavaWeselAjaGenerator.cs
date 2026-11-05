using System.Reflection;
using System.Text;

namespace WeselAjaMcpServer.Generators;

public class JavaWeselAjaGenerator
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
                "java",
                safeProjectName);

        if (
            Directory.Exists(outputDirectory)
            &&
            !overwrite)
        {
            throw new InvalidOperationException(
                $"Generated project already exists: {outputDirectory}. " +
                "Set overwrite=true to replace it.");
        }

        if (Directory.Exists(outputDirectory))
        {
            Directory.Delete(
                outputDirectory,
                true);
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
            "pom.xml",
            PomXml(safeProjectName),
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
                "main",
                "java",
                "com",
                "weselaja",
                "integration",
                "RedirectServer.java"),
            RedirectServerJava(),
            files);

        AddGeneratedFile(
            outputDirectory,
            Path.Combine(
                "src",
                "main",
                "java",
                "com",
                "weselaja",
                "integration",
                "CallbackServer.java"),
            CallbackServerJava(),
            files);

        AddGeneratedFile(
            outputDirectory,
            Path.Combine(
                "src",
                "main",
                "java",
                "com",
                "weselaja",
                "integration",
                "WeselAjaConfig.java"),
            WeselAjaConfigJava(),
            files);

        AddGeneratedFile(
            outputDirectory,
            Path.Combine(
                "src",
                "main",
                "java",
                "com",
                "weselaja",
                "integration",
                "EnvLoader.java"),
            EnvLoaderJava(),
            files);

        AddGeneratedFile(
            outputDirectory,
            Path.Combine(
                "src",
                "main",
                "java",
                "com",
                "weselaja",
                "integration",
                "BankChannels.java"),
            BankChannelsJava(),
            files);

        AddGeneratedFile(
            outputDirectory,
            Path.Combine(
                "src",
                "main",
                "java",
                "com",
                "weselaja",
                "integration",
                "SignatureGenerator.java"),
            SignatureGeneratorJava(),
            files);

        AddGeneratedFile(
            outputDirectory,
            Path.Combine(
                "src",
                "main",
                "java",
                "com",
                "weselaja",
                "integration",
                "WeselAjaException.java"),
            WeselAjaExceptionJava(),
            files);

        AddGeneratedFile(
            outputDirectory,
            Path.Combine(
                "src",
                "main",
                "java",
                "com",
                "weselaja",
                "integration",
                "CreatePaymentRequest.java"),
            CreatePaymentRequestJava(),
            files);

        AddGeneratedFile(
            outputDirectory,
            Path.Combine(
                "src",
                "main",
                "java",
                "com",
                "weselaja",
                "integration",
                "CreatePaymentResponse.java"),
            CreatePaymentResponseJava(),
            files);

        AddGeneratedFile(
            outputDirectory,
            Path.Combine(
                "src",
                "main",
                "java",
                "com",
                "weselaja",
                "integration",
                "WeselAjaClient.java"),
            WeselAjaClientJava(),
            files);

        if (includeExample)
        {
            AddGeneratedFile(
                outputDirectory,
                Path.Combine(
                    "src",
                    "main",
                    "java",
                    "com",
                    "weselaja",
                    "integration",
                    "Main.java"),
                MainJava(),
                files);
        }

        if (includeTests)
        {
            AddGeneratedFile(
                outputDirectory,
                Path.Combine(
                    "src",
                    "test",
                    "java",
                    "com",
                    "weselaja",
                    "integration",
                    "WeselAjaClientTest.java"),
                TestJava(),
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

        if (!string.IsNullOrWhiteSpace(directory))
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

            SetProperty(
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

            SetProperty(
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

        foreach (
            var constructor
            in type.GetConstructors(
                BindingFlags.Public |
                BindingFlags.Instance))
        {
            var parameters =
                constructor.GetParameters();

            if (parameters.Length == 0)
            {
                continue;
            }

            var arguments =
                new object?[parameters.Length];

            var unknownStringIndex =
                0;

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
                    var name =
                        parameter.Name?
                            .ToLowerInvariant()
                        ?? string.Empty;

                    if (
                        name.Contains("content")
                        ||
                        name.Contains("text")
                        ||
                        name.Contains("source"))
                    {
                        arguments[index] =
                            content;

                        continue;
                    }

                    if (
                        name.Contains("path")
                        ||
                        name.Contains("file")
                        ||
                        name.Contains("name"))
                    {
                        arguments[index] =
                            relativePath;

                        continue;
                    }

                    arguments[index] =
                        unknownStringIndex == 0
                            ? relativePath
                            : content;

                    unknownStringIndex++;

                    continue;
                }

                if (
                    parameter.ParameterType ==
                    typeof(bool))
                {
                    arguments[index] = false;
                    continue;
                }

                if (parameter.HasDefaultValue)
                {
                    arguments[index] =
                        parameter.DefaultValue;

                    continue;
                }

                arguments[index] =
                    parameter.ParameterType.IsValueType
                        ? Activator.CreateInstance(
                            parameter.ParameterType)
                        : null;
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
            "Unable to create GeneratedFile.");
    }

    private static void SetProperty(
        Type type,
        GeneratedFile instance,
        IEnumerable<string> names,
        string value)
    {
        foreach (var name in names)
        {
            var property =
                type.GetProperty(
                    name,
                    BindingFlags.Public |
                    BindingFlags.Instance |
                    BindingFlags.IgnoreCase);

            if (
                property != null
                &&
                property.CanWrite
                &&
                property.PropertyType ==
                    typeof(string))
            {
                property.SetValue(
                    instance,
                    value);

                return;
            }
        }
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
            (projectName ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(value))
        {
            return "weselaja-java";
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

        return value
            .Replace(
                ' ',
                '-')
            .ToLowerInvariant();
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
        target/
        .env
        *.class
        *.log
        """;
    }

    private static string PomXml(
        string projectName)
    {
        return $$"""
        <?xml version="1.0" encoding="UTF-8"?>
        <project xmlns="http://maven.apache.org/POM/4.0.0"
                 xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
                 xsi:schemaLocation="http://maven.apache.org/POM/4.0.0 https://maven.apache.org/xsd/maven-4.0.0.xsd">

            <modelVersion>4.0.0</modelVersion>

            <groupId>com.weselaja</groupId>

            <artifactId>{{projectName}}</artifactId>

            <version>1.0.0</version>

            <properties>
                <maven.compiler.release>17</maven.compiler.release>
                <project.build.sourceEncoding>UTF-8</project.build.sourceEncoding>
                <jackson.version>2.22.2</jackson.version>
                <junit.version>5.12.2</junit.version>
            </properties>

            <dependencies>
                <dependency>
                    <groupId>com.fasterxml.jackson.core</groupId>
                    <artifactId>jackson-databind</artifactId>
                    <version>${jackson.version}</version>
                </dependency>

                <dependency>
                    <groupId>org.junit.jupiter</groupId>
                    <artifactId>junit-jupiter</artifactId>
                    <version>${junit.version}</version>
                    <scope>test</scope>
                </dependency>
            </dependencies>

            <build>
                <plugins>
                    <plugin>
                        <groupId>org.apache.maven.plugins</groupId>
                        <artifactId>maven-compiler-plugin</artifactId>
                        <version>3.16.0</version>
                    </plugin>

                    <plugin>
                        <groupId>org.apache.maven.plugins</groupId>
                        <artifactId>maven-surefire-plugin</artifactId>
                        <version>3.5.3</version>
                    </plugin>

                    <plugin>
                        <groupId>org.codehaus.mojo</groupId>
                        <artifactId>exec-maven-plugin</artifactId>
                        <version>3.5.0</version>
                        <configuration>
                            <mainClass>
                                com.weselaja.integration.Main
                            </mainClass>
                        </configuration>
                    </plugin>
                </plugins>
            </build>
        </project>
        """;
    }

    private static string WeselAjaConfigJava()
    {
        return """
        package com.weselaja.integration;

        public final class WeselAjaConfig {
            private final String apiUrl;
            private final String apiKey;
            private final String secretKey;
            private final String customerReference;
            private final String callbackUrl;
            private final String redirectUrl;
            private final String currency;
            private final String paymentMethod;

            public WeselAjaConfig() {
                apiUrl =
                    EnvLoader.get(
                        "WESELAJA_API_URL",
                        "https://sandbox.checkout.weselaja.id"
                    );

                apiKey =
                    EnvLoader.get(
                        "WESELAJA_API_KEY",
                        ""
                    );

                secretKey =
                    EnvLoader.get(
                        "WESELAJA_SECRET_KEY",
                        ""
                    );

                customerReference =
                    EnvLoader.get(
                        "WESELAJA_CUSTOMER_REFERENCE",
                        ""
                    );

                callbackUrl =
                    EnvLoader.get(
                        "WESELAJA_CALLBACK_URL",
                        ""
                    );

                redirectUrl =
                    EnvLoader.get(
                        "WESELAJA_REDIRECT_URL",
                        ""
                    );

                currency =
                    EnvLoader.get(
                        "WESELAJA_CURRENCY",
                        "IDR"
                    );

                paymentMethod =
                    EnvLoader.get(
                        "WESELAJA_PAYMENT_METHOD",
                        "VIRTUAL_ACCOUNT"
                    );
            }

            public String getApiUrl() {
                return apiUrl;
            }

            public String getApiKey() {
                return apiKey;
            }

            public String getSecretKey() {
                return secretKey;
            }

            public String getCustomerReference() {
                return customerReference;
            }

            public String getCallbackUrl() {
                return callbackUrl;
            }

            public String getRedirectUrl() {
                return redirectUrl;
            }

            public String getCurrency() {
                return currency;
            }

            public String getPaymentMethod() {
                return paymentMethod;
            }

            public void validate() {
                if (
                    apiKey == null ||
                    apiKey.isBlank()
                ) {
                    throw new WeselAjaException(
                        "WESELAJA_API_KEY is required."
                    );
                }

                if (
                    secretKey == null ||
                    secretKey.isBlank()
                ) {
                    throw new WeselAjaException(
                        "WESELAJA_SECRET_KEY is required."
                    );
                }

                if (
                    apiUrl == null ||
                    apiUrl.isBlank()
                ) {
                    throw new WeselAjaException(
                        "WESELAJA_API_URL is required."
                    );
                }
            }
        }
        """;
    }

    private static string EnvLoaderJava()
    {
        return """
        package com.weselaja.integration;

        import java.io.IOException;
        import java.nio.file.Files;
        import java.nio.file.Path;
        import java.util.HashMap;
        import java.util.Map;

        public final class EnvLoader {
            private static final Map<String, String> VALUES =
                new HashMap<>();

            static {
                load();
            }

            private EnvLoader() {
            }

            public static String get(
                String name,
                String defaultValue
            ) {
                String systemValue =
                    System.getenv(name);

                if (
                    systemValue != null &&
                    !systemValue.isBlank()
                ) {
                    return systemValue.trim();
                }

                String fileValue =
                    VALUES.get(name);

                if (
                    fileValue != null &&
                    !fileValue.isBlank()
                ) {
                    return fileValue.trim();
                }

                return defaultValue;
            }

            private static void load() {
                Path path =
                    Path.of(".env");

                if (!Files.exists(path)) {
                    return;
                }

                try {
                    for (
                        String raw :
                        Files.readAllLines(path)
                    ) {
                        String line =
                            raw.trim();

                        if (
                            line.isEmpty() ||
                            line.startsWith("#")
                        ) {
                            continue;
                        }

                        int index =
                            line.indexOf('=');

                        if (index <= 0) {
                            continue;
                        }

                        String key =
                            line.substring(
                                0,
                                index
                            ).trim();

                        String value =
                            line.substring(
                                index + 1
                            ).trim();

                        if (
                            value.length() >= 2 &&
                            value.startsWith("\"") &&
                            value.endsWith("\"")
                        ) {
                            value =
                                value.substring(
                                    1,
                                    value.length() - 1
                                );
                        }

                        VALUES.put(
                            key,
                            value
                        );
                    }
                }
                catch (IOException exception) {
                    throw new WeselAjaException(
                        "Failed to load .env.",
                        exception
                    );
                }
            }
        }
        """;
    }

    private static string BankChannelsJava()
    {
        return """
        package com.weselaja.integration;

        import java.util.Map;

        public final class BankChannels {
            private static final Map<String, String> CHANNELS =
                Map.ofEntries(
                    Map.entry("BRI", "BRI.VA"),
                    Map.entry("BANKBRI", "BRI.VA"),
                    Map.entry("MANDIRI", "MDR.VA"),
                    Map.entry("BANKMANDIRI", "MDR.VA"),
                    Map.entry("MDR", "MDR.VA"),
                    Map.entry("BNI", "BNI.VA"),
                    Map.entry("BANKBNI", "BNI.VA"),
                    Map.entry("PERMATA", "PTB.VA"),
                    Map.entry("BANKPERMATA", "PTB.VA"),
                    Map.entry("PTB", "PTB.VA"),
                    Map.entry("CIMB", "CIMB.VA"),
                    Map.entry("CIMBNIAGA", "CIMB.VA"),
                    Map.entry("BANKCIMB", "CIMB.VA"),
                    Map.entry("BCA", "BCA.VA"),
                    Map.entry("BANKBCA", "BCA.VA")
                );

            private BankChannels() {
            }

            public static String normalize(
                String bank
            ) {
                if (bank == null) {
                    return "";
                }

                return bank
                    .trim()
                    .toUpperCase()
                    .replace(
                        " ",
                        ""
                    )
                    .replace(
                        "-",
                        ""
                    )
                    .replace(
                        "_",
                        ""
                    );
            }

            public static String getPaymentChannel(
                String bank
            ) {
                String normalized =
                    normalize(bank);

                String channel =
                    CHANNELS.get(normalized);

                if (channel == null) {
                    throw new WeselAjaException(
                        "Unsupported bank: " +
                        bank +
                        ". Supported banks: " +
                        "BRI, Mandiri, BNI, Permata, CIMB, BCA"
                    );
                }

                return channel;
            }
        }
        """;
    }

    private static string SignatureGeneratorJava()
    {
        return """
        package com.weselaja.integration;

        import javax.crypto.Mac;
        import javax.crypto.spec.SecretKeySpec;

        import java.nio.charset.StandardCharsets;

        import java.security.GeneralSecurityException;
        import java.security.MessageDigest;

        import java.time.Instant;

        import java.util.Base64;
        import java.util.UUID;

        public final class SignatureGenerator {
            private SignatureGenerator() {
            }

            public static String generate(
                String method,
                String uri,
                String timestamp,
                String body,
                String secretKey
            ) {
                String payload =
                    method.toUpperCase() +
                    "\n" +
                    uri +
                    "\n" +
                    timestamp +
                    "\n" +
                    body;

                return hmacBase64(
                    payload,
                    secretKey
                );
            }

            public static String generateCallbackSignature(
                String method,
                String uri,
                String rawBody,
                String timestamp,
                String secretKey
            ) {
                String payload =
                    method.toUpperCase() +
                    "\n" +
                    uri +
                    "\n" +
                    rawBody +
                    "\n" +
                    timestamp;

                return hmacBase64(
                    payload,
                    secretKey
                );
            }

            public static boolean verifyCallbackSignature(
                String method,
                String uri,
                String rawBody,
                String timestamp,
                String signature,
                String secretKey
            ) {
                if (
                    signature == null ||
                    signature.isBlank()
                ) {
                    return false;
                }

                String expected =
                    generateCallbackSignature(
                        method,
                        uri,
                        rawBody,
                        timestamp,
                        secretKey
                    );

                return MessageDigest.isEqual(
                    expected.getBytes(
                        StandardCharsets.UTF_8
                    ),
                    signature
                        .trim()
                        .getBytes(
                            StandardCharsets.UTF_8
                        )
                );
            }

            public static String timestamp() {
                return Instant.now().toString();
            }

            public static String idempotencyKey() {
                return UUID.randomUUID().toString();
            }

            private static String hmacBase64(
                String payload,
                String secretKey
            ) {
                try {
                    Mac mac =
                        Mac.getInstance(
                            "HmacSHA256"
                        );

                    mac.init(
                        new SecretKeySpec(
                            secretKey.getBytes(
                                StandardCharsets.UTF_8
                            ),
                            "HmacSHA256"
                        )
                    );

                    byte[] digest =
                        mac.doFinal(
                            payload.getBytes(
                                StandardCharsets.UTF_8
                            )
                        );

                    return Base64
                        .getEncoder()
                        .encodeToString(
                            digest
                        );
                }
                catch (GeneralSecurityException exception) {
                    throw new WeselAjaException(
                        "Unable to generate signature.",
                        exception
                    );
                }
            }
        }
        """;
    }

    private static string WeselAjaExceptionJava()
    {
        return """
        package com.weselaja.integration;

        public class WeselAjaException
            extends RuntimeException {

            public WeselAjaException(
                String message
            ) {
                super(message);
            }

            public WeselAjaException(
                String message,
                Throwable cause
            ) {
                super(
                    message,
                    cause
                );
            }
        }
        """;
    }

    private static string CreatePaymentRequestJava()
    {
        return """
        package com.weselaja.integration;

        public final class CreatePaymentRequest {
            private final int amount;
            private final String customerName;
            private final String bank;
            private final String description;
            private final String referenceCode;

            public CreatePaymentRequest(
                int amount,
                String customerName,
                String bank
            ) {
                this(
                    amount,
                    customerName,
                    bank,
                    "",
                    ""
                );
            }

            public CreatePaymentRequest(
                int amount,
                String customerName,
                String bank,
                String description,
                String referenceCode
            ) {
                this.amount =
                    amount;

                this.customerName =
                    customerName;

                this.bank =
                    bank;

                this.description =
                    description;

                this.referenceCode =
                    referenceCode;
            }

            public int getAmount() {
                return amount;
            }

            public String getCustomerName() {
                return customerName;
            }

            public String getBank() {
                return bank;
            }

            public String getDescription() {
                return description;
            }

            public String getReferenceCode() {
                return referenceCode;
            }

            public void validate() {
                if (amount <= 0) {
                    throw new WeselAjaException(
                        "Payment amount must be greater than zero."
                    );
                }

                if (
                    customerName == null ||
                    customerName.isBlank()
                ) {
                    throw new WeselAjaException(
                        "Customer name is required."
                    );
                }

                if (
                    bank == null ||
                    bank.isBlank()
                ) {
                    throw new WeselAjaException(
                        "Bank is required."
                    );
                }
            }
        }
        """;
    }

    private static string CreatePaymentResponseJava()
    {
        return """
        package com.weselaja.integration;

        import com.fasterxml.jackson.annotation.JsonAlias;
        import com.fasterxml.jackson.annotation.JsonIgnoreProperties;
        import com.fasterxml.jackson.annotation.JsonProperty;

        @JsonIgnoreProperties(ignoreUnknown = true)
        public class CreatePaymentResponse {
            @JsonProperty("id")
            @JsonAlias({"paymentId"})
            private String id;

            @JsonProperty("initiatedAmount")
            private Integer initiatedAmount;

            @JsonProperty("paymentAmount")
            private Integer paymentAmount;

            @JsonProperty("feeAmount")
            private Integer feeAmount;

            @JsonProperty("currency")
            private String currency;

            @JsonProperty("paymentMethod")
            private String paymentMethod;

            @JsonProperty("paymentChannel")
            private String paymentChannel;

            @JsonProperty("paymentCode")
            private String paymentCode;

            @JsonProperty("paymentCodeType")
            private String paymentCodeType;

            @JsonProperty("referenceCode")
            private String referenceCode;

            @JsonProperty("customerReference")
            private String customerReference;

            @JsonProperty("customerName")
            private String customerName;

            @JsonProperty("status")
            private String status;

            @JsonProperty("createdTime")
            private String createdTime;

            @JsonProperty("updatedTime")
            private String updatedTime;

            @JsonProperty("expirationTime")
            private String expirationTime;

            @JsonProperty("description")
            private String description;

            @JsonProperty("callbackUrl")
            private String callbackUrl;

            @JsonProperty("redirectUrl")
            private String redirectUrl;

            @JsonProperty("success")
            @JsonAlias({"isSuccess"})
            private Boolean isSuccess;

            public String getId() {
                return id;
            }

            public Integer getInitiatedAmount() {
                return initiatedAmount;
            }

            public Integer getPaymentAmount() {
                return paymentAmount;
            }

            public Integer getFeeAmount() {
                return feeAmount;
            }

            public String getCurrency() {
                return currency;
            }

            public String getPaymentMethod() {
                return paymentMethod;
            }

            public String getPaymentChannel() {
                return paymentChannel;
            }

            public String getPaymentCode() {
                return paymentCode;
            }

            public String getPaymentCodeType() {
                return paymentCodeType;
            }

            public String getReferenceCode() {
                return referenceCode;
            }

            public String getCustomerReference() {
                return customerReference;
            }

            public String getCustomerName() {
                return customerName;
            }

            public String getStatus() {
                return status;
            }

            public String getCreatedTime() {
                return createdTime;
            }

            public String getUpdatedTime() {
                return updatedTime;
            }

            public String getExpirationTime() {
                return expirationTime;
            }

            public String getDescription() {
                return description;
            }

            public String getCallbackUrl() {
                return callbackUrl;
            }

            public String getRedirectUrl() {
                return redirectUrl;
            }

            public Boolean getIsSuccess() {
                return isSuccess;
            }
        }
        """;
    }

    private static string WeselAjaClientJava()
    {
        return """
        package com.weselaja.integration;

        import com.fasterxml.jackson.databind.JsonNode;
        import com.fasterxml.jackson.databind.ObjectMapper;

        import java.io.IOException;

        import java.net.URI;

        import java.net.URLEncoder;

        import java.net.http.HttpClient;
        import java.net.http.HttpRequest;
        import java.net.http.HttpResponse;

        import java.nio.charset.StandardCharsets;

        import java.time.Duration;

        import java.util.LinkedHashMap;
        import java.util.Map;

        public final class WeselAjaClient {
            private static final String PAYMENT_URI =
                "/v1/payins";

            private final WeselAjaConfig config;
            private final HttpClient httpClient;
            private final ObjectMapper objectMapper;

            public WeselAjaClient() {
                this(
                    new WeselAjaConfig()
                );
            }

            public WeselAjaClient(
                WeselAjaConfig config
            ) {
                this.config =
                    config;

                this.httpClient =
                    HttpClient.newBuilder()
                        .connectTimeout(
                            Duration.ofSeconds(30)
                        )
                        .build();

                this.objectMapper =
                    new ObjectMapper();
            }

            public CreatePaymentResponse createPayment(
                CreatePaymentRequest request
            ) {
                request.validate();
                config.validate();

                String paymentChannel =
                    BankChannels.getPaymentChannel(
                        request.getBank()
                    );

                String referenceCode =
                    request.getReferenceCode();

                if (
                    referenceCode == null ||
                    referenceCode.isBlank()
                ) {
                    referenceCode =
                        "JAVA-" +
                        System.currentTimeMillis();
                }

                Map<String, Object> payload =
                    new LinkedHashMap<>();

                payload.put(
                    "initiatedAmount",
                    request.getAmount()
                );

                payload.put(
                    "currency",
                    config.getCurrency()
                );

                payload.put(
                    "paymentMethod",
                    config.getPaymentMethod()
                );

                payload.put(
                    "paymentChannel",
                    paymentChannel
                );

                payload.put(
                    "referenceCode",
                    referenceCode
                );

                payload.put(
                    "customerName",
                    request.getCustomerName()
                );

                if (
                    !config
                        .getCustomerReference()
                        .isBlank()
                ) {
                    payload.put(
                        "customerReference",
                        config.getCustomerReference()
                    );
                }

                if (
                    !config
                        .getCallbackUrl()
                        .isBlank()
                ) {
                    payload.put(
                        "callbackUrl",
                        config.getCallbackUrl()
                    );
                }

                if (
                    !config
                        .getRedirectUrl()
                        .isBlank()
                ) {
                    payload.put(
                        "redirectUrl",
                        config.getRedirectUrl()
                    );
                }

                if (
                    request.getDescription() != null &&
                    !request
                        .getDescription()
                        .isBlank()
                ) {
                    payload.put(
                        "description",
                        request.getDescription()
                    );
                }

                try {
                    String body =
                        objectMapper
                            .writeValueAsString(
                                payload
                            );

                    String timestamp =
                        SignatureGenerator
                            .timestamp();

                    String signature =
                        SignatureGenerator.generate(
                            "POST",
                            PAYMENT_URI,
                            timestamp,
                            body,
                            config.getSecretKey()
                        );

                    String idempotencyKey =
                        SignatureGenerator
                            .idempotencyKey();

                    String requestUrl =
                        config
                            .getApiUrl()
                            .replaceAll(
                                "/$",
                                ""
                            )
                        +
                        PAYMENT_URI;

                    HttpRequest requestMessage =
                        HttpRequest.newBuilder()
                            .uri(
                                URI.create(
                                    requestUrl
                                )
                            )
                            .timeout(
                                Duration.ofSeconds(30)
                            )
                            .header(
                                "Content-Type",
                                "application/json"
                            )
                            .header(
                                "Accept",
                                "application/json"
                            )
                            .header(
                                "Xenith-Api-Key",
                                config.getApiKey()
                            )
                            .header(
                                "Xenith-Request-Timestamp",
                                timestamp
                            )
                            .header(
                                "Xenith-Request-Signature",
                                signature
                            )
                            .header(
                                "X-Idempotency-Key",
                                idempotencyKey
                            )
                            .POST(
                                HttpRequest.BodyPublishers
                                    .ofString(
                                        body
                                    )
                            )
                            .build();

                    HttpResponse<String> response =
                        httpClient.send(
                            requestMessage,
                            HttpResponse.BodyHandlers
                                .ofString()
                        );

                    if (
                        response.statusCode() < 200 ||
                        response.statusCode() >= 300
                    ) {
                        throw new WeselAjaException(
                            "WeselAja API request failed " +
                            "with HTTP " +
                            response.statusCode() +
                            ": " +
                            response.body()
                        );
                    }

                    return objectMapper.readValue(
                        response.body(),
                        CreatePaymentResponse.class
                    );
                }
                catch (IOException exception) {
                    throw new WeselAjaException(
                        "Failed to communicate with WeselAja API.",
                        exception
                    );
                }
                catch (InterruptedException exception) {
                    Thread.currentThread().interrupt();

                    throw new WeselAjaException(
                        "WeselAja request was interrupted.",
                        exception
                    );
                }
            }

            public JsonNode getPayins() {
                return getPayins(
                    null,
                    null
                );
            }

            public JsonNode getPayins(
                String status,
                Integer limit
            ) {
                if (
                    status != null &&
                    !status.isBlank()
                ) {
                    status =
                        status
                            .trim()
                            .toUpperCase();

                    if (
                        !status.equals("PENDING") &&
                        !status.equals("SUCCESS") &&
                        !status.equals("EXPIRED") &&
                        !status.equals("FAILED")
                    ) {
                        throw new WeselAjaException(
                            "status must be one of: " +
                            "PENDING, SUCCESS, EXPIRED, FAILED."
                        );
                    }
                }
                else {
                    status =
                        null;
                }

                if (
                    limit != null &&
                    limit <= 0
                ) {
                    throw new WeselAjaException(
                        "limit must be a positive integer."
                    );
                }

                StringBuilder query =
                    new StringBuilder();

                if (status != null) {
                    query.append(
                        "status="
                    ).append(
                        URLEncoder.encode(
                            status,
                            StandardCharsets.UTF_8
                        )
                    );
                }

                if (limit != null) {
                    if (query.length() > 0) {
                        query.append('&');
                    }

                    query.append(
                        "limit="
                    ).append(
                        limit
                    );
                }

                String requestUrl =
                    config
                        .getApiUrl()
                        .replaceAll(
                            "/$",
                            ""
                        )
                    +
                    PAYMENT_URI;

                if (query.length() > 0) {
                    requestUrl +=
                        "?" +
                        query;
                }

                try {
                    String timestamp =
                        SignatureGenerator
                            .timestamp();

                    String signature =
                        SignatureGenerator.generate(
                            "GET",
                            PAYMENT_URI,
                            timestamp,
                            "",
                            config.getSecretKey()
                        );

                    HttpRequest requestMessage =
                        HttpRequest.newBuilder()
                            .uri(
                                URI.create(
                                    requestUrl
                                )
                            )
                            .timeout(
                                Duration.ofSeconds(30)
                            )
                            .header(
                                "Accept",
                                "application/json"
                            )
                            .header(
                                "Xenith-Api-Key",
                                config.getApiKey()
                            )
                            .header(
                                "Xenith-Request-Timestamp",
                                timestamp
                            )
                            .header(
                                "Xenith-Request-Signature",
                                signature
                            )
                            .GET()
                            .build();

                    HttpResponse<String> response =
                        httpClient.send(
                            requestMessage,
                            HttpResponse.BodyHandlers
                                .ofString()
                        );

                    if (
                        response.statusCode() < 200 ||
                        response.statusCode() >= 300
                    ) {
                        throw new WeselAjaException(
                            "WeselAja API request failed " +
                            "with HTTP " +
                            response.statusCode() +
                            ": " +
                            response.body()
                        );
                    }

                    JsonNode result =
                        objectMapper.readTree(
                            response.body()
                        );

                    if (
                        result == null ||
                        !result.isObject()
                    ) {
                        throw new WeselAjaException(
                            "Unexpected response format from WeselAja."
                        );
                    }

                    return result;
                }
                catch (IOException exception) {
                    throw new WeselAjaException(
                        "Failed to communicate with WeselAja API.",
                        exception
                    );
                }
                catch (InterruptedException exception) {
                    Thread.currentThread().interrupt();

                    throw new WeselAjaException(
                        "WeselAja request was interrupted.",
                        exception
                    );
                }
            }
        }
        """;
    }

    private static string MainJava()
    {
        return """
        package com.weselaja.integration;

        import com.fasterxml.jackson.databind.JsonNode;

        public final class Main {
            private Main() {
            }

            public static void main(
                String[] args
            ) {
                if (
                    args.length > 0 &&
                    args[0].equalsIgnoreCase(
                        "callback"
                    )
                ) {
                    CallbackServer.run();

                    return;
                }

                if (
                    args.length > 0 &&
                    args[0].equalsIgnoreCase(
                        "redirect"
                    )
                ) {
                    RedirectServer.run();

                    return;
                }

                WeselAjaClient client =
                    new WeselAjaClient();

                if (
                    args.length > 0 &&
                    args[0].equalsIgnoreCase(
                        "payins"
                    )
                ) {
                    String status =
                        args.length > 1
                            ? args[1]
                            : null;

                    Integer limit =
                        null;

                    if (args.length > 2) {
                        try {
                            limit =
                                Integer.parseInt(
                                    args[2]
                                );
                        }
                        catch (NumberFormatException exception) {
                            System.err.println(
                                "Invalid limit: " +
                                args[2]
                            );

                            System.exit(1);

                            return;
                        }
                    }

                    try {
                        JsonNode result =
                            client.getPayins(
                                status,
                                limit
                            );

                        System.out.println(
                            result.toPrettyString()
                        );
                    }
                    catch (WeselAjaException exception) {
                        System.err.println(
                            exception.getMessage()
                        );

                        System.exit(1);
                    }

                    return;
                }

                CreatePaymentRequest request =
                    new CreatePaymentRequest(
                        10000,
                        "Andi",
                        "BRI",
                        "WeselAja Java sandbox example",
                        ""
                    );

                try {
                    CreatePaymentResponse payment =
                        client.createPayment(
                            request
                        );

                    System.out.println(
                        "Payment created successfully."
                    );

                    System.out.println(
                        "Status: " +
                        payment.getStatus()
                    );

                    System.out.println(
                        "Reference: " +
                        payment.getReferenceCode()
                    );

                    System.out.println(
                        "Channel: " +
                        payment.getPaymentChannel()
                    );

                    System.out.println(
                        "Payment: " +
                        payment.getPaymentCode()
                    );
                }
                catch (WeselAjaException exception) {
                    System.err.println(
                        "Payment creation failed."
                    );

                    System.err.println(
                        exception.getMessage()
                    );

                    System.exit(1);
                }
            }
        }
        """;
    }

    private static string CallbackServerJava()
    {
        return """
        package com.weselaja.integration;

        import com.fasterxml.jackson.databind.JsonNode;
        import com.fasterxml.jackson.databind.ObjectMapper;
        import com.fasterxml.jackson.databind.node.ObjectNode;

        import com.sun.net.httpserver.Headers;
        import com.sun.net.httpserver.HttpExchange;
        import com.sun.net.httpserver.HttpServer;

        import java.io.IOException;
        import java.io.OutputStream;

        import java.net.InetSocketAddress;
        import java.net.URI;

        import java.nio.charset.StandardCharsets;

        public final class CallbackServer {
            private static final String CALLBACK_PATH =
                "/payment/weselaja/callback";

            private CallbackServer() {
            }

            public static void run() {
                String secret =
                    EnvLoader.get(
                        "WEB_SIGNATURE_SECRET",
                        ""
                    );

                if (secret.isBlank()) {
                    throw new WeselAjaException(
                        "WEB_SIGNATURE_SECRET is required."
                    );
                }

                int port =
                    getPort(
                        "WESELAJA_CALLBACK_PORT",
                        8081
                    );

                try {
                    HttpServer server =
                        HttpServer.create(
                            new InetSocketAddress(port),
                            0
                        );

                    server.createContext(
                        CALLBACK_PATH,
                        exchange ->
                            handle(
                                exchange,
                                secret
                            )
                    );

                    server.start();

                    System.out.println(
                        "WeselAja callback handler listening on " +
                        "http://0.0.0.0:" +
                        port +
                        CALLBACK_PATH
                    );
                }
                catch (IOException exception) {
                    throw new WeselAjaException(
                        "Failed to start callback server.",
                        exception
                    );
                }
            }

            public static void main(
                String[] args
            ) {
                run();
            }

            private static void handle(
                HttpExchange exchange,
                String secret
            ) {
                try {
                    if (
                        !"POST".equalsIgnoreCase(
                            exchange.getRequestMethod()
                        )
                    ) {
                        sendJson(
                            exchange,
                            405,
                            jsonObject(
                                "success",
                                false,
                                "error",
                                "Method not allowed"
                            )
                        );

                        return;
                    }

                    URI uri =
                        exchange.getRequestURI();

                    String requestPath =
                        uri.getRawPath();

                    String timestamp =
                        getHeader(
                            exchange.getRequestHeaders(),
                            "x-middleware-timestamp"
                        );

                    String signature =
                        getHeader(
                            exchange.getRequestHeaders(),
                            "x-middleware-signature"
                        );

                    if (
                        timestamp == null ||
                        timestamp.isBlank()
                    ) {
                        sendJson(
                            exchange,
                            400,
                            jsonObject(
                                "success",
                                false,
                                "error",
                                "Missing x-middleware-timestamp"
                            )
                        );

                        return;
                    }

                    if (
                        signature == null ||
                        signature.isBlank()
                    ) {
                        sendJson(
                            exchange,
                            400,
                            jsonObject(
                                "success",
                                false,
                                "error",
                                "Missing x-middleware-signature"
                            )
                        );

                        return;
                    }

                    String rawBody =
                        new String(
                            exchange
                                .getRequestBody()
                                .readAllBytes(),
                            StandardCharsets.UTF_8
                        );

                    String signingUri =
                        requestPath;

                    if (
                        uri.getRawQuery() != null &&
                        !uri
                            .getRawQuery()
                            .isBlank()
                    ) {
                        signingUri +=
                            "?" +
                            uri.getRawQuery();
                    }

                    boolean verified =
                        SignatureGenerator
                            .verifyCallbackSignature(
                                "POST",
                                signingUri,
                                rawBody,
                                timestamp,
                                signature,
                                secret
                            );

                    if (!verified) {
                        sendJson(
                            exchange,
                            401,
                            jsonObject(
                                "success",
                                false,
                                "verified",
                                false,
                                "error",
                                "Invalid callback signature"
                            )
                        );

                        return;
                    }

                    ObjectMapper mapper =
                        new ObjectMapper();

                    JsonNode body;

                    try {
                        body =
                            rawBody.isBlank()
                                ? mapper.nullNode()
                                : mapper.readTree(
                                    rawBody
                                );
                    }
                    catch (Exception exception) {
                        body =
                            mapper.valueToTree(
                                rawBody
                            );
                    }

                    ObjectNode response =
                        mapper.createObjectNode();

                    response.put(
                        "success",
                        true
                    );

                    response.put(
                        "verified",
                        true
                    );

                    response.set(
                        "body",
                        body
                    );

                    sendJson(
                        exchange,
                        200,
                        response
                    );
                }
                catch (IOException exception) {
                    try {
                        sendJson(
                            exchange,
                            400,
                            jsonObject(
                                "success",
                                false,
                                "error",
                                "Unable to read request body"
                            )
                        );
                    }
                    catch (IOException ignored) {
                    }
                }
                finally {
                    exchange.close();
                }
            }

            private static String getHeader(
                Headers headers,
                String name
            ) {
                return headers.getFirst(name);
            }

            private static int getPort(
                String name,
                int fallback
            ) {
                String value =
                    EnvLoader.get(
                        name,
                        String.valueOf(
                            fallback
                        )
                    );

                try {
                    int port =
                        Integer.parseInt(
                            value
                        );

                    return port > 0
                        ? port
                        : fallback;
                }
                catch (NumberFormatException exception) {
                    return fallback;
                }
            }

            private static ObjectNode jsonObject(
                Object... values
            ) {
                ObjectMapper mapper =
                    new ObjectMapper();

                ObjectNode object =
                    mapper.createObjectNode();

                for (
                    int index = 0;
                    index + 1 < values.length;
                    index += 2
                ) {
                    String key =
                        String.valueOf(
                            values[index]
                        );

                    Object value =
                        values[index + 1];

                    if (value instanceof Boolean booleanValue) {
                        object.put(
                            key,
                            booleanValue
                        );
                    }
                    else {
                        object.put(
                            key,
                            String.valueOf(
                                value
                            )
                        );
                    }
                }

                return object;
            }

            private static void sendJson(
                HttpExchange exchange,
                int statusCode,
                JsonNode body
            ) throws IOException {
                byte[] response =
                    body
                        .toString()
                        .getBytes(
                            StandardCharsets.UTF_8
                        );

                exchange
                    .getResponseHeaders()
                    .set(
                        "Content-Type",
                        "application/json"
                    );

                exchange.sendResponseHeaders(
                    statusCode,
                    response.length
                );

                try (
                    OutputStream output =
                        exchange.getResponseBody()
                ) {
                    output.write(
                        response
                    );
                }
            }
        }
        """;
    }

    private static string RedirectServerJava()
    {
        return """
        package com.weselaja.integration;

        import com.fasterxml.jackson.databind.ObjectMapper;
        import com.fasterxml.jackson.databind.node.ObjectNode;

        import com.sun.net.httpserver.HttpExchange;
        import com.sun.net.httpserver.HttpServer;

        import java.io.IOException;
        import java.io.OutputStream;

        import java.net.InetSocketAddress;
        import java.net.URI;

        import java.net.URLDecoder;

        import java.nio.charset.StandardCharsets;

        import java.util.LinkedHashMap;
        import java.util.Map;

        public final class RedirectServer {
            private RedirectServer() {
            }

            public static void run() {
                int port =
                    getPort(
                        "WESELAJA_REDIRECT_PORT",
                        8080
                    );

                try {
                    HttpServer server =
                        HttpServer.create(
                            new InetSocketAddress(port),
                            0
                        );

                    server.createContext(
                        "/payment/weselaja/return",
                        RedirectServer::handle
                    );

                    server.start();

                    System.out.println(
                        "WeselAja redirect handler listening on " +
                        "http://0.0.0.0:" +
                        port +
                        "/payment/weselaja/return"
                    );
                }
                catch (IOException exception) {
                    throw new WeselAjaException(
                        "Failed to start redirect server.",
                        exception
                    );
                }
            }

            public static void main(
                String[] args
            ) {
                run();
            }

            private static void handle(
                HttpExchange exchange
            ) {
                try {
                    if (
                        !"GET".equalsIgnoreCase(
                            exchange.getRequestMethod()
                        )
                    ) {
                        sendJson(
                            exchange,
                            405,
                            jsonObject(
                                "success",
                                false,
                                "error",
                                "Method not allowed"
                            )
                        );

                        return;
                    }

                    URI uri =
                        exchange.getRequestURI();

                    Map<String, String> parameters =
                        parseQuery(
                            uri.getRawQuery()
                        );

                    ObjectMapper mapper =
                        new ObjectMapper();

                    ObjectNode response =
                        mapper.createObjectNode();

                    response.put(
                        "success",
                        true
                    );

                    response.put(
                        "path",
                        uri.getPath()
                    );

                    ObjectNode params =
                        mapper.createObjectNode();

                    for (
                        Map.Entry<String, String> entry :
                        parameters.entrySet()
                    ) {
                        params.put(
                            entry.getKey(),
                            entry.getValue()
                        );
                    }

                    response.set(
                        "parameters",
                        params
                    );

                    sendJson(
                        exchange,
                        200,
                        response
                    );
                }
                catch (IOException exception) {
                    try {
                        sendJson(
                            exchange,
                            500,
                            jsonObject(
                                "success",
                                false,
                                "error",
                                "Internal server error"
                            )
                        );
                    }
                    catch (IOException ignored) {
                    }
                }
                finally {
                    exchange.close();
                }
            }

            private static Map<String, String> parseQuery(
                String query
            ) {
                Map<String, String> result =
                    new LinkedHashMap<>();

                if (
                    query == null ||
                    query.isBlank()
                ) {
                    return result;
                }

                for (
                    String pair :
                    query.split("&")
                ) {
                    String[] parts =
                        pair.split(
                            "=",
                            2
                        );

                    String key =
                        decode(
                            parts[0]
                        );

                    String value =
                        parts.length == 2
                            ? decode(
                                parts[1]
                            )
                            : "";

                    result.put(
                        key,
                        value
                    );
                }

                return result;
            }

            private static String decode(
                String value
            ) {
                return URLDecoder.decode(
                    value,
                    StandardCharsets.UTF_8
                );
            }

            private static int getPort(
                String name,
                int fallback
            ) {
                String value =
                    EnvLoader.get(
                        name,
                        String.valueOf(
                            fallback
                        )
                    );

                try {
                    int port =
                        Integer.parseInt(
                            value
                        );

                    return port > 0
                        ? port
                        : fallback;
                }
                catch (NumberFormatException exception) {
                    return fallback;
                }
            }

            private static ObjectNode jsonObject(
                Object... values
            ) {
                ObjectMapper mapper =
                    new ObjectMapper();

                ObjectNode object =
                    mapper.createObjectNode();

                for (
                    int index = 0;
                    index + 1 < values.length;
                    index += 2
                ) {
                    String key =
                        String.valueOf(
                            values[index]
                        );

                    Object value =
                        values[index + 1];

                    if (
                        value instanceof Boolean booleanValue
                    ) {
                        object.put(
                            key,
                            booleanValue
                        );
                    }
                    else {
                        object.put(
                            key,
                            String.valueOf(
                                value
                            )
                        );
                    }
                }

                return object;
            }

            private static void sendJson(
                HttpExchange exchange,
                int statusCode,
                ObjectNode body
            ) throws IOException {
                byte[] response =
                    body
                        .toString()
                        .getBytes(
                            StandardCharsets.UTF_8
                        );

                exchange
                    .getResponseHeaders()
                    .set(
                        "Content-Type",
                        "application/json"
                    );

                exchange.sendResponseHeaders(
                    statusCode,
                    response.length
                );

                try (
                    OutputStream output =
                        exchange.getResponseBody()
                ) {
                    output.write(
                        response
                    );
                }
            }
        }
        """;
    }

    private static string TestJava()
    {
        return """
        package com.weselaja.integration;

        import org.junit.jupiter.api.Test;

        import static org.junit.jupiter.api.Assertions.*;

        public class WeselAjaClientTest {
            @Test
            void shouldMapSupportedBanks() {
                assertEquals(
                    "BRI.VA",
                    BankChannels.getPaymentChannel(
                        "BRI"
                    )
                );

                assertEquals(
                    "BRI.VA",
                    BankChannels.getPaymentChannel(
                        "Bank BRI"
                    )
                );

                assertEquals(
                    "MDR.VA",
                    BankChannels.getPaymentChannel(
                        "Mandiri"
                    )
                );

                assertEquals(
                    "MDR.VA",
                    BankChannels.getPaymentChannel(
                        "Bank Mandiri"
                    )
                );

                assertEquals(
                    "BNI.VA",
                    BankChannels.getPaymentChannel(
                        "BNI"
                    )
                );

                assertEquals(
                    "PTB.VA",
                    BankChannels.getPaymentChannel(
                        "Permata"
                    )
                );

                assertEquals(
                    "CIMB.VA",
                    BankChannels.getPaymentChannel(
                        "CIMB"
                    )
                );

                assertEquals(
                    "CIMB.VA",
                    BankChannels.getPaymentChannel(
                        "CIMB Niaga"
                    )
                );

                assertEquals(
                    "BCA.VA",
                    BankChannels.getPaymentChannel(
                        "BCA"
                    )
                );

                assertEquals(
                    "BCA.VA",
                    BankChannels.getPaymentChannel(
                        "Bank BCA"
                    )
                );
            }

            @Test
            void shouldNormalizeBankNames() {
                assertEquals(
                    "BCA.VA",
                    BankChannels.getPaymentChannel(
                        " bca "
                    )
                );

                assertEquals(
                    "MDR.VA",
                    BankChannels.getPaymentChannel(
                        "mandiri"
                    )
                );
            }

            @Test
            void shouldRejectUnsupportedBank() {
                assertThrows(
                    WeselAjaException.class,
                    () ->
                        BankChannels.getPaymentChannel(
                            "UNKNOWN"
                        )
                );
            }

            @Test
            void shouldGenerateSignature() {
                String signature =
                    SignatureGenerator.generate(
                        "POST",
                        "/v1/payins",
                        "2026-01-01T00:00:00Z",
                        "{\"hello\":\"world\"}",
                        "secret"
                    );

                assertNotNull(
                    signature
                );

                assertFalse(
                    signature.isBlank()
                );
            }

            @Test
            void shouldGenerateAndVerifyCallbackSignature() {
                String method =
                    "POST";

                String uri =
                    "/payment/weselaja/callback";

                String body =
                    "{\"hello\":\"world\"}";

                String timestamp =
                    "2026-01-01T00:00:00.000Z";

                String secret =
                    "test-secret";

                String signature =
                    SignatureGenerator
                        .generateCallbackSignature(
                            method,
                            uri,
                            body,
                            timestamp,
                            secret
                        );

                assertTrue(
                    SignatureGenerator
                        .verifyCallbackSignature(
                            method,
                            uri,
                            body,
                            timestamp,
                            signature,
                            secret
                        )
                );

                assertFalse(
                    SignatureGenerator
                        .verifyCallbackSignature(
                            method,
                            uri,
                            body,
                            timestamp,
                            signature + "x",
                            secret
                        )
                );
            }

            @Test
            void shouldValidatePaymentRequest() {
                CreatePaymentRequest request =
                    new CreatePaymentRequest(
                        10000,
                        "Andi",
                        "BCA"
                    );

                assertDoesNotThrow(
                    request::validate
                );
            }

            @Test
            void shouldRejectInvalidAmount() {
                CreatePaymentRequest request =
                    new CreatePaymentRequest(
                        0,
                        "Andi",
                        "BCA"
                    );

                assertThrows(
                    WeselAjaException.class,
                    request::validate
                );
            }
        }
        """;
    }

    private static string Readme(
        string projectName)
    {
        return $$"""
        # {{projectName}}

        WeselAja Java integration generated by the WeselAja MCP Code Generator.

        ## Requirements

        - Java 17+
        - Maven 3.8+

        ## Install

        ```bash
        mvn clean install
        ```

        ## Create payment

        ```bash
        mvn exec:java
        ```

        ## Get payins

        ```bash
        mvn -Dexec.args="payins SUCCESS 50" exec:java
        ```

        Supported statuses:

        ```text
        PENDING
        SUCCESS
        EXPIRED
        FAILED
        ```

        This uses:

        ```text
        GET /v1/payins
        ```

        ## Callback

        Start:

        ```bash
        mvn -Dexec.mainClass=com.weselaja.integration.CallbackServer exec:java
        ```

        Endpoint:

        ```text
        POST /payment/weselaja/callback
        ```

        Default:

        ```text
        http://127.0.0.1:8081/payment/weselaja/callback
        ```

        The callback verifies:

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

        HMAC-SHA256 is calculated using `WEB_SIGNATURE_SECRET`
        and encoded using Base64.

        ## Redirect

        Start:

        ```bash
        mvn -Dexec.mainClass=com.weselaja.integration.RedirectServer exec:java
        ```

        Endpoint:

        ```text
        GET /payment/weselaja/return
        ```

        Default:

        ```text
        http://127.0.0.1:8080/payment/weselaja/return
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

        ## Test

        ```bash
        mvn test
        ```

        ## Supported Banks

        | Bank | Channel |
        |---|---|
        | BRI | BRI.VA |
        | Mandiri | MDR.VA |
        | BNI | BNI.VA |
        | Permata | PTB.VA |
        | CIMB | CIMB.VA |
        | BCA | BCA.VA |

        ## Project Structure

        ```text
        {{projectName}}/
        ├── src/
        │   ├── main/
        │   │   └── java/
        │   │       └── com/
        │   │           └── weselaja/
        │   │               └── integration/
        │   │                   ├── BankChannels.java
        │   │                   ├── CallbackServer.java
        │   │                   ├── CreatePaymentRequest.java
        │   │                   ├── CreatePaymentResponse.java
        │   │                   ├── EnvLoader.java
        │   │                   ├── Main.java
        │   │                   ├── RedirectServer.java
        │   │                   ├── SignatureGenerator.java
        │   │                   ├── WeselAjaClient.java
        │   │                   ├── WeselAjaConfig.java
        │   │                   └── WeselAjaException.java
        │   └── test/
        │       └── java/
        │           └── com/
        │               └── weselaja/
        │                   └── integration/
        │                       └── WeselAjaClientTest.java
        ├── .env.example
        ├── .gitignore
        ├── pom.xml
        └── README.md
        ```
        """;
    }
}