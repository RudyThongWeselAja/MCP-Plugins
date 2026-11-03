package com.weselaja.integration

suspend fun createWeselAjaExamplePayment(): PaymentInfo {
    val repository = WeselAjaPaymentRepository(
        WeselAjaClient.create()
    )

    return repository.createPayment(
        amount = 10_000L,
        customerName = "Andi",
        phoneNumber = "081234567890",
        paymentChannel = "BCA.VA",
        description = "WeselAja Android sandbox example"
    )
}

suspend fun checkWeselAjaExamplePayment(
    payinId: String
): PaymentInfo {
    val repository = WeselAjaPaymentRepository(
        WeselAjaClient.create()
    )

    return repository.getPayin(payinId)
}