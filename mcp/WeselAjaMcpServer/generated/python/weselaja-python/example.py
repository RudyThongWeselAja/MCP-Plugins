from weselaja_client import (
    WeselAjaClient,
)


def main() -> None:

    client = WeselAjaClient.from_env()

    payment = client.create_payment(
            amount=10_000,
            customer_name="Andi",
            bank="BRI",
            description=
                "Sandbox payment example",
        )

    print(
        "Status:",
        payment.status,
    )

    print(
        "Payment code:",
        payment.payment_code,
    )

    print(
        "Reference:",
        payment.reference_code,
    )

    print(
        "Channel:",
        payment.payment_channel,
    )


if __name__ == "__main__":
    main()