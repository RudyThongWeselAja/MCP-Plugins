package id.weselaja;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

public record WeselAjaConfig(
        String apiUrl,
        String apiKey,
        String secretKey,
        String customerReference,
        String callbackUrl,
        String redirectUrl,
        String currency,
        String paymentMethod) {

    public WeselAjaConfig {
        apiUrl = clean(apiUrl);
        apiKey = clean(apiKey);
        secretKey = clean(secretKey);
        customerReference = clean(customerReference);
        callbackUrl = clean(callbackUrl);
        redirectUrl = clean(redirectUrl);
        currency = clean(currency);
        paymentMethod = clean(paymentMethod);
    }

    public static WeselAjaConfig fromEnvironment() throws WeselAjaConfigurationException {
        return fromEnvironment(Path.of(".env"));
    }

    public static WeselAjaConfig fromEnvironment(Path envFile) throws WeselAjaConfigurationException {
        Map<String, String> values = new LinkedHashMap<>(System.getenv());
        if (Files.exists(envFile)) {
            try {
                List<String> lines = Files.readAllLines(envFile);
                for (int index = 0; index < lines.size(); index++) {
                    String line = lines.get(index).trim();
                    if (line.isEmpty() || line.startsWith("#")) {
                        continue;
                    }
                    int separator = line.indexOf('=');
                    if (separator <= 0) {
                        throw new WeselAjaConfigurationException(
                                "Invalid environment entry at " + envFile + ":" + (index + 1) + ".");
                    }
                    String key = line.substring(0, separator).trim();
                    String value = stripQuotes(line.substring(separator + 1).trim());
                    values.putIfAbsent(key, value);
                }
            } catch (IOException exception) {
                throw new WeselAjaConfigurationException(
                        "Unable to read environment file '" + envFile + "'.", exception);
            }
        }

        WeselAjaConfig config = new WeselAjaConfig(
                value(values, "WESELAJA_API_URL", "https://sandbox.checkout.weselaja.id"),
                value(values, "WESELAJA_API_KEY", ""),
                value(values, "WESELAJA_SECRET_KEY", ""),
                value(values, "WESELAJA_CUSTOMER_REFERENCE", ""),
                value(values, "WESELAJA_CALLBACK_URL", ""),
                value(values, "WESELAJA_REDIRECT_URL", ""),
                value(values, "WESELAJA_CURRENCY", "IDR"),
                value(values, "WESELAJA_PAYMENT_METHOD", "VIRTUAL_ACCOUNT"));
        config.validate();
        return config;
    }

    public void validate() throws WeselAjaConfigurationException {
        Map<String, String> required = new LinkedHashMap<>();
        required.put("WESELAJA_API_URL", apiUrl);
        required.put("WESELAJA_API_KEY", apiKey);
        required.put("WESELAJA_SECRET_KEY", secretKey);
        required.put("WESELAJA_CUSTOMER_REFERENCE", customerReference);
        required.put("WESELAJA_CALLBACK_URL", callbackUrl);
        required.put("WESELAJA_REDIRECT_URL", redirectUrl);
        required.put("WESELAJA_CURRENCY", currency);
        required.put("WESELAJA_PAYMENT_METHOD", paymentMethod);
        List<String> missing = required.entrySet().stream()
                .filter(entry -> entry.getValue().isBlank())
                .map(Map.Entry::getKey)
                .toList();
        if (!missing.isEmpty()) {
            throw new WeselAjaConfigurationException("Missing configuration: " + String.join(", ", missing));
        }
        validateHttpUrl(apiUrl, "WESELAJA_API_URL");
        validateHttpUrl(callbackUrl, "WESELAJA_CALLBACK_URL");
        validateHttpUrl(redirectUrl, "WESELAJA_REDIRECT_URL");
    }

    private static void validateHttpUrl(String value, String key) throws WeselAjaConfigurationException {
        try {
            var uri = java.net.URI.create(value);
            if (uri.getHost() == null
                    || !("http".equalsIgnoreCase(uri.getScheme()) || "https".equalsIgnoreCase(uri.getScheme()))) {
                throw new IllegalArgumentException();
            }
        } catch (IllegalArgumentException exception) {
            throw new WeselAjaConfigurationException(key + " must be a valid HTTP or HTTPS URL.");
        }
    }

    private static String value(Map<String, String> values, String key, String fallback) {
        String value = values.get(key);
        return value == null || value.isBlank() ? fallback : value.trim();
    }

    private static String stripQuotes(String value) {
        if (value.length() >= 2
                && ((value.startsWith("\"") && value.endsWith("\""))
                        || (value.startsWith("'") && value.endsWith("'")))) {
            return value.substring(1, value.length() - 1);
        }
        return value;
    }

    private static String clean(String value) {
        return value == null ? "" : value.trim();
    }
}
