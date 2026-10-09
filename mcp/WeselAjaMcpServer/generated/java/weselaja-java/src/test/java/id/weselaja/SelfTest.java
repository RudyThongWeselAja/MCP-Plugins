package id.weselaja;

import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.net.ServerSocket;
import java.net.Socket;
import java.nio.charset.StandardCharsets;
import java.time.Clock;
import java.time.Instant;
import java.time.ZoneOffset;
import java.util.Base64;

public final class SelfTest {
    private SelfTest() {
    }

    public static void main(String[] args) throws Exception {
        testBankChannels();
        testSignature();
        testJson();
        testConfigValidation();
        testCreatePayment();
        testApiError();
        System.out.println("All Java WeselAja self-tests passed.");
    }

    private static void testBankChannels() throws Exception {
        check("BRI.VA".equals(BankChannels.resolve("Bank BRI")), "BRI channel mapping");
        check("MDR.VA".equals(BankChannels.resolve("Bank-Mandiri")), "Mandiri channel mapping");
        check("BNI.VA".equals(BankChannels.resolve("BNI")), "BNI channel mapping");
        check("PTB.VA".equals(BankChannels.resolve("Permata")), "Permata channel mapping");
        check("CIMB.VA".equals(BankChannels.resolve("CIMB Niaga")), "CIMB channel mapping");
        check("BCA.VA".equals(BankChannels.resolve("BCA")), "BCA channel mapping");
        expect(IllegalArgumentException.class, () -> BankChannels.resolve("Unknown"), "unsupported bank rejected");
    }

    private static void testSignature() {
        String signature = WeselAjaSignature.generate("POST", "/v1/payins", "time", "body", "secret");
        check(Base64.getDecoder().decode(signature).length == 32, "Base64 HMAC-SHA256 signature");
    }

    private static void testJson() throws Exception {
        String original = "{\"message\":\"hello\\nworld\",\"nested\":{\"number\":12},\"items\":[true,null]}";
        Object parsed = Json.parse(original);
        check(original.equals(Json.stringify(parsed)), "JSON parse and serialization round-trip");
        expect(IllegalArgumentException.class, () -> Json.parse("{broken"), "invalid JSON rejected");
    }

    private static void testConfigValidation() throws Exception {
        expect(
                WeselAjaConfigurationException.class,
                () -> new WeselAjaConfig("", "", "", "", "", "", "IDR", "VIRTUAL_ACCOUNT").validate(),
                "missing configuration rejected");
        expect(
                WeselAjaConfigurationException.class,
                () -> new WeselAjaConfig(
                        "ftp://merchant.example",
                        "key",
                        "secret",
                        "CUSTOMER_1",
                        "https://merchant.example/callback",
                        "https://merchant.example/return",
                        "IDR",
                        "VIRTUAL_ACCOUNT").validate(),
                "invalid URL scheme rejected");
    }

    private static void testCreatePayment() throws Exception {
        String response = "{\"paymentId\":\"pay_123\",\"status\":\"PENDING\",\"paymentCode\":\"123456\"}";
        try (MockServer server = new MockServer(200, response)) {
            WeselAjaClient client = testClient(server.url());
            CreatePaymentResponse payment = client.createPayment(
                    new CreatePaymentRequest(10_000, "Andi", "BCA", null, null, "ORDER_1"));
            check(payment.success(), "successful response marked success");
            check("pay_123".equals(payment.paymentId()), "payment id parsed");
            check("PENDING".equals(payment.status()), "payment status parsed");
            check(server.request().contains("\"paymentChannel\":\"BCA.VA\""), "payment channel sent");
            check(server.request().contains("\"referenceCode\":\"ORDER_1\""), "reference sent");
            check(server.header("Xenith-Api-Key").equals("test-api-key"), "API key header sent");
            check(server.header("Xenith-Request-Timestamp").equals("2026-10-05T02:03:04.567Z"),
                    "UTC timestamp header sent");
            String requestBody = server.body();
            check(server.header("Xenith-Request-Signature").equals(
                    WeselAjaSignature.generate(
                            "POST",
                            "/v1/payins",
                            server.header("Xenith-Request-Timestamp"),
                            requestBody,
                            "test-secret")), "request signature matches exact body");
            check(server.header("X-Idempotency-Key").startsWith("java-"), "idempotency key sent");
        }
    }

    private static void testApiError() throws Exception {
        try (MockServer server = new MockServer(400, "{\"message\":\"invalid payment\"}")) {
            try {
                testClient(server.url()).createPayment(new CreatePaymentRequest(100, "A", "BRI"));
                throw new AssertionError("API error expected");
            } catch (WeselAjaApiException exception) {
                check(exception.getStatusCode() == 400, "API status code retained");
                check(exception.getMessage().contains("invalid payment"), "API error message retained");
            }
        }
    }

    private static WeselAjaClient testClient(String url) throws Exception {
        WeselAjaConfig config = new WeselAjaConfig(
                url,
                "test-api-key",
                "test-secret",
                "CUSTOMER_123",
                "https://merchant.example/callback",
                "https://merchant.example/return",
                "IDR",
                "VIRTUAL_ACCOUNT");
        Clock clock = Clock.fixed(Instant.parse("2026-10-05T02:03:04.567Z"), ZoneOffset.UTC);
        return new WeselAjaClient(config, java.net.http.HttpClient.newHttpClient(), clock);
    }

    private static void expect(Class<? extends Throwable> type, ThrowingRunnable action, String message)
            throws Exception {
        try {
            action.run();
        } catch (Throwable exception) {
            if (type.isInstance(exception)) return;
            throw new AssertionError(message + ": unexpected exception", exception);
        }
        throw new AssertionError(message + ": expected " + type.getSimpleName());
    }

    private static void check(boolean condition, String message) {
        if (!condition) throw new AssertionError(message);
    }

    @FunctionalInterface
    private interface ThrowingRunnable {
        void run() throws Exception;
    }

    private static final class MockServer implements AutoCloseable {
        private final ServerSocket serverSocket;
        private final Thread thread;
        private final int status;
        private final String responseBody;
        private volatile String requestText;
        private volatile IOException failure;

        private MockServer(int status, String responseBody) throws IOException {
            this.status = status;
            this.responseBody = responseBody;
            serverSocket = new ServerSocket(0, 1, java.net.InetAddress.getLoopbackAddress());
            thread = new Thread(this::serve, "weselaja-test-server");
            thread.start();
        }

        private String url() {
            return "http://" + serverSocket.getInetAddress().getHostAddress() + ":" + serverSocket.getLocalPort();
        }

        private void serve() {
            try (Socket socket = serverSocket.accept()) {
                socket.setSoTimeout(5_000);
                var input = socket.getInputStream();
                ByteArrayOutputStream requestBytes = new ByteArrayOutputStream();
                int matched = 0;
                int value;
                byte[] terminator = {'\r', '\n', '\r', '\n'};
                while ((value = input.read()) != -1) {
                    requestBytes.write(value);
                    matched = value == terminator[matched] ? matched + 1 : (value == '\r' ? 1 : 0);
                    if (matched == terminator.length) break;
                }
                String headers = requestBytes.toString(StandardCharsets.ISO_8859_1);
                int contentLength = headerInt(headers, "Content-Length");
                byte[] requestBody = input.readNBytes(contentLength);
                requestBytes.write(requestBody);
                requestText = requestBytes.toString(StandardCharsets.ISO_8859_1);

                byte[] response = responseBody.getBytes(StandardCharsets.UTF_8);
                String reason = status == 200 ? "OK" : "Bad Request";
                String responseHeaders = "HTTP/1.1 " + status + " " + reason + "\r\n"
                        + "Content-Type: application/json\r\n"
                        + "Content-Length: " + response.length + "\r\n"
                        + "Connection: close\r\n\r\n";
                socket.getOutputStream().write(responseHeaders.getBytes(StandardCharsets.US_ASCII));
                socket.getOutputStream().write(response);
                socket.getOutputStream().flush();
            } catch (IOException exception) {
                failure = exception;
            }
        }

        private String request() throws Exception {
            thread.join(5_000);
            if (thread.isAlive()) throw new AssertionError("Mock server did not receive request");
            if (failure != null) throw failure;
            return requestText;
        }

        private String body() throws Exception {
            String request = request();
            int separator = request.indexOf("\r\n\r\n");
            return request.substring(separator + 4);
        }

        private String header(String name) throws Exception {
            for (String line : request().split("\r\n")) {
                if (line.regionMatches(true, 0, name + ":", 0, name.length() + 1)) {
                    return line.substring(name.length() + 1).trim();
                }
            }
            throw new AssertionError("Missing request header: " + name);
        }

        private static int headerInt(String headers, String name) {
            for (String line : headers.split("\r\n")) {
                if (line.regionMatches(true, 0, name + ":", 0, name.length() + 1)) {
                    return Integer.parseInt(line.substring(name.length() + 1).trim());
                }
            }
            return 0;
        }

        @Override
        public void close() throws IOException {
            serverSocket.close();
            try {
                thread.join(5_000);
            } catch (InterruptedException exception) {
                Thread.currentThread().interrupt();
                throw new IOException("Interrupted while closing mock server.", exception);
            }
            if (thread.isAlive()) throw new AssertionError("Mock server thread did not stop");
        }
    }
}
