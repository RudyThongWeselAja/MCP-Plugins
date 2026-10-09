package id.weselaja;

import java.nio.charset.StandardCharsets;
import java.util.Base64;
import javax.crypto.Mac;
import javax.crypto.spec.SecretKeySpec;

public final class WeselAjaSignature {
    private WeselAjaSignature() {
    }

    public static String generate(String method, String uri, String timestamp, String body, String secret) {
        String payload = method + "\n" + uri + "\n" + timestamp + "\n" + body;
        try {
            Mac mac = Mac.getInstance("HmacSHA256");
            mac.init(new SecretKeySpec(secret.getBytes(StandardCharsets.UTF_8), "HmacSHA256"));
            return Base64.getEncoder().encodeToString(mac.doFinal(payload.getBytes(StandardCharsets.UTF_8)));
        } catch (java.security.GeneralSecurityException exception) {
            throw new IllegalStateException("Unable to generate WeselAja request signature.", exception);
        }
    }
}
