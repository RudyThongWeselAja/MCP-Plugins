package id.weselaja;

import java.util.Collections;
import java.util.LinkedHashMap;
import java.util.Map;

public record CreatePaymentResponse(
        boolean success,
        String paymentId,
        Long initiatedAmount,
        Object paymentAmount,
        Object feeAmount,
        String currency,
        String paymentMethod,
        String paymentChannel,
        String paymentCode,
        String paymentCodeType,
        String referenceCode,
        String customerReference,
        String customerName,
        String status,
        String createdTime,
        String updatedTime,
        String expirationTime,
        String description,
        String callbackUrl,
        String redirectUrl,
        String payerAccountName,
        String payerAccountNumber,
        String payerPaymentChannel,
        Map<String, Object> metadata,
        Map<String, Object> raw) {

    public CreatePaymentResponse {
        metadata = metadata == null
                ? Map.of()
                : Collections.unmodifiableMap(new LinkedHashMap<>(metadata));
        raw = raw == null
                ? Map.of()
                : Collections.unmodifiableMap(new LinkedHashMap<>(raw));
    }

    static CreatePaymentResponse fromMap(Map<String, Object> values) {
        return new CreatePaymentResponse(
                true,
                string(values, "paymentId", "id"),
                longValue(values.get("initiatedAmount")),
                values.get("paymentAmount"),
                values.get("feeAmount"),
                string(values, "currency"),
                string(values, "paymentMethod"),
                string(values, "paymentChannel"),
                string(values, "paymentCode"),
                string(values, "paymentCodeType"),
                string(values, "referenceCode"),
                string(values, "customerReference"),
                string(values, "customerName"),
                string(values, "status"),
                string(values, "createdTime"),
                string(values, "updatedTime"),
                string(values, "expirationTime"),
                string(values, "description"),
                string(values, "callbackUrl"),
                string(values, "redirectUrl"),
                string(values, "payerAccountName"),
                string(values, "payerAccountNumber"),
                string(values, "payerPaymentChannel"),
                objectMap(values.get("metadata")),
                values);
    }

    private static String string(Map<String, Object> values, String... keys) {
        for (String key : keys) {
            Object value = values.get(key);
            if (value instanceof String text) {
                return text;
            }
        }
        return null;
    }

    private static Long longValue(Object value) {
        if (value instanceof Number number) {
            return number.longValue();
        }
        return null;
    }

    private static Map<String, Object> objectMap(Object value) {
        if (!(value instanceof Map<?, ?> map)) {
            return Map.of();
        }
        Map<String, Object> result = new LinkedHashMap<>();
        map.forEach((key, item) -> {
            if (key instanceof String text) {
                result.put(text, item);
            }
        });
        return result;
    }
}
