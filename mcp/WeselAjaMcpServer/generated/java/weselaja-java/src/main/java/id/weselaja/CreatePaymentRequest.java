package id.weselaja;

public record CreatePaymentRequest(
        long amount,
        String customerName,
        String bank,
        String customerPhoneNumber,
        String description,
        String referenceCode) {

    public CreatePaymentRequest(long amount, String customerName, String bank) {
        this(amount, customerName, bank, null, null, null);
    }

    public void validate() {
        if (amount <= 0) {
            throw new IllegalArgumentException("Payment amount must be greater than zero.");
        }
        if (customerName == null || customerName.isBlank()) {
            throw new IllegalArgumentException("Customer name is required.");
        }
        if (bank == null || bank.isBlank()) {
            throw new IllegalArgumentException("Bank is required.");
        }
    }
}
