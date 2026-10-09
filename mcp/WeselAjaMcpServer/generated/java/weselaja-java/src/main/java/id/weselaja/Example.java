package id.weselaja;

public final class Example {
    private Example() {
    }

    public static void main(String[] args) throws Exception {
        WeselAjaClient client = WeselAjaClient.fromEnvironment();
        CreatePaymentResponse payment = client.createPayment(
                new CreatePaymentRequest(10_000, "Andi", "BNI", null, "WeselAja sandbox example", null));

        System.out.println("Payment created successfully.");
        System.out.println("Status   : " + valueOrDash(payment.status()));
        System.out.println("Reference: " + valueOrDash(payment.referenceCode()));
        System.out.println("Channel  : " + valueOrDash(payment.paymentChannel()));
        System.out.println("Payment  : " + valueOrDash(payment.paymentCode()));
    }

    private static String valueOrDash(String value) {
        return value == null || value.isBlank() ? "-" : value;
    }
}
