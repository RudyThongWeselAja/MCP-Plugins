package id.weselaja;

import java.io.IOException;
import java.io.InputStream;
import java.net.URI;
import java.net.http.HttpClient;
import java.net.http.HttpRequest;
import java.net.http.HttpResponse;
import java.nio.charset.StandardCharsets;
import java.time.Clock;
import java.time.Duration;
import java.time.Instant;
import java.time.ZoneOffset;
import java.time.format.DateTimeFormatter;
import java.util.LinkedHashMap;
import java.util.Map;
import java.util.UUID;

public final class WeselAjaClient {
    private static final String PAYMENT_URI = "/v1/payins";
    private static final int MAX_RESPONSE_BYTES = 1_048_576;
    private static final DateTimeFormatter TIMESTAMP_FORMAT =
            DateTimeFormatter.ofPattern("yyyy-MM-dd'T'HH:mm:ss.SSS'Z'").withZone(ZoneOffset.UTC);

    private final WeselAjaConfig config;
    private final HttpClient httpClient;
    private final Clock clock;

    public WeselAjaClient(WeselAjaConfig config) throws WeselAjaConfigurationException {
        this(config, HttpClient.newBuilder()
                .connectTimeout(Duration.ofSeconds(10))
                .followRedirects(HttpClient.Redirect.NEVER)
                .build(), Clock.systemUTC());
    }

    WeselAjaClient(WeselAjaConfig config, HttpClient httpClient, Clock clock)
            throws WeselAjaConfigurationException {
        config.validate();
        this.config = config;
        this.httpClient = httpClient;
        this.clock = clock;
    }

    public static WeselAjaClient fromEnvironment() throws WeselAjaConfigurationException {
        return new WeselAjaClient(WeselAjaConfig.fromEnvironment());
    }

    public CreatePaymentResponse createPayment(CreatePaymentRequest request) throws WeselAjaException {
        request.validate();
        String paymentChannel = BankChannels.resolve(request.bank());
        String referenceCode = request.referenceCode() == null || request.referenceCode().isBlank()
                ? generateReference(request.customerName(), clock.instant())
                : request.referenceCode().trim();
        validateReference(referenceCode);
        validateReference(config.customerReference());

        Map<String, Object> payload = new LinkedHashMap<>();
        payload.put("initiatedAmount", request.amount());
        payload.put("currency", config.currency());
        payload.put("paymentMethod", config.paymentMethod());
        payload.put("paymentChannel", paymentChannel);
        payload.put("referenceCode", referenceCode);
        payload.put("customerReference", config.customerReference());
        payload.put("customerName", request.customerName().trim());
        payload.put("callbackUrl", config.callbackUrl());
        payload.put("redirectUrl", config.redirectUrl());
        putOptional(payload, "customerPhoneNumber", request.customerPhoneNumber());
        putOptional(payload, "description", request.description());

        String body = Json.stringify(payload);
        String timestamp = TIMESTAMP_FORMAT.format(clock.instant());
        String signature = WeselAjaSignature.generate("POST", PAYMENT_URI, timestamp, body, config.secretKey());
        HttpRequest httpRequest;
        try {
            httpRequest = HttpRequest.newBuilder()
                    .uri(URI.create(config.apiUrl().replaceAll("/+$", "") + PAYMENT_URI))
                    .timeout(Duration.ofSeconds(30))
                    .header("Content-Type", "application/json")
                    .header("Accept", "application/json")
                    .header("Xenith-Api-Key", config.apiKey())
                    .header("Xenith-Request-Timestamp", timestamp)
                    .header("Xenith-Request-Signature", signature)
                    .header("X-Idempotency-Key", "java-" + UUID.randomUUID())
                    .POST(HttpRequest.BodyPublishers.ofString(body, StandardCharsets.UTF_8))
                    .build();
        } catch (IllegalArgumentException exception) {
            throw new WeselAjaException("Unable to create WeselAja request.", exception);
        }

        HttpResponse<InputStream> httpResponse;
        try {
            httpResponse = httpClient.send(httpRequest, HttpResponse.BodyHandlers.ofInputStream());
        } catch (InterruptedException exception) {
            Thread.currentThread().interrupt();
            throw new WeselAjaException("Request to WeselAja was interrupted.", exception);
        } catch (IOException exception) {
            throw new WeselAjaException("Unable to connect to WeselAja: " + exception.getMessage(), exception);
        }

        String responseBody;
        try (InputStream responseStream = httpResponse.body()) {
            byte[] responseBytes = responseStream.readNBytes(MAX_RESPONSE_BYTES + 1);
            if (responseBytes.length > MAX_RESPONSE_BYTES) {
                throw new WeselAjaException("WeselAja response exceeded the 1 MiB size limit.");
            }
            responseBody = new String(responseBytes, StandardCharsets.UTF_8);
        } catch (IOException exception) {
            throw new WeselAjaException("Unable to read WeselAja response.", exception);
        }
        if (responseBody.isBlank()) {
            throw new WeselAjaException("Empty response from WeselAja. HTTP " + httpResponse.statusCode());
        }

        Object decoded;
        try {
            decoded = Json.parse(responseBody);
        } catch (IllegalArgumentException exception) {
            throw new WeselAjaException(
                    "Invalid JSON response from WeselAja. HTTP " + httpResponse.statusCode() + ": "
                            + exception.getMessage(),
                    exception);
        }
        if (!(decoded instanceof Map<?, ?> rawMap)) {
            throw new WeselAjaException("Unexpected WeselAja response format.");
        }
        Map<String, Object> response = new LinkedHashMap<>();
        rawMap.forEach((key, value) -> {
            if (key instanceof String text) response.put(text, value);
        });
        if (httpResponse.statusCode() < 200 || httpResponse.statusCode() >= 300) {
            throw apiException(httpResponse.statusCode(), responseBody, response);
        }
        return CreatePaymentResponse.fromMap(response);
    }

    private static void putOptional(Map<String, Object> payload, String key, String value) {
        if (value != null && !value.isBlank()) payload.put(key, value.trim());
    }

    private static String generateReference(String customerName, Instant now) {
        String normalized = customerName.toUpperCase(java.util.Locale.ROOT)
                .replaceAll("[^A-Z0-9_-]", "");
        if (normalized.isBlank()) normalized = "CUSTOMER";
        if (normalized.length() > 20) normalized = normalized.substring(0, 20);
        return "WES-" + normalized + "-" + DateTimeFormatter.ofPattern("yyyyMMddHHmmss")
                .withZone(ZoneOffset.UTC)
                .format(now);
    }

    private static void validateReference(String value) {
        if (value == null || value.isBlank()) {
            throw new IllegalArgumentException("Reference value is required.");
        }
        if (!value.matches("^[A-Za-z0-9_-]+$")) {
            throw new IllegalArgumentException(
                    "Reference values may only contain letters, numbers, \"-\" and \"_\".");
        }
    }

    private static WeselAjaApiException apiException(
            int statusCode,
            String body,
            Map<String, Object> response) {
        String message = body;
        for (String key : new String[] {"message", "error", "code"}) {
            Object value = response.get(key);
            if (value instanceof String text && !text.isBlank()) {
                message = text;
                break;
            }
        }
        return new WeselAjaApiException(statusCode, body, message);
    }
}
