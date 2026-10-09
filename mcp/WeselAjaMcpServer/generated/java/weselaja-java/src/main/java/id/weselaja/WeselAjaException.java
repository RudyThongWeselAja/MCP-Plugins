package id.weselaja;

public class WeselAjaException extends Exception {
    private static final long serialVersionUID = 1L;

    public WeselAjaException(String message) {
        super(message);
    }

    public WeselAjaException(String message, Throwable cause) {
        super(message, cause);
    }
}
