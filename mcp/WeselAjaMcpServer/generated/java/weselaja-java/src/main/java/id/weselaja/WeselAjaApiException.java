package id.weselaja;

public final class WeselAjaApiException extends WeselAjaException {
    private static final long serialVersionUID = 1L;

    private final int statusCode;
    private final String responseBody;

    public WeselAjaApiException(int statusCode, String responseBody, String message) {
        super("WeselAja API returned HTTP " + statusCode + ": " + message);
        this.statusCode = statusCode;
        this.responseBody = responseBody;
    }

    public int getStatusCode() {
        return statusCode;
    }

    public String getResponseBody() {
        return responseBody;
    }
}
