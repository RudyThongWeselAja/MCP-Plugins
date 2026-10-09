package id.weselaja;

public final class WeselAjaConfigurationException extends Exception {
    private static final long serialVersionUID = 1L;

    public WeselAjaConfigurationException(String message) {
        super(message);
    }

    public WeselAjaConfigurationException(String message, Throwable cause) {
        super(message, cause);
    }
}
