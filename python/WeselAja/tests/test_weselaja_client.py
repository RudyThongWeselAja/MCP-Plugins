import unittest

from python.WeselAja.client import WeselAjaClient


class WeselAjaClientTests(unittest.TestCase):
    def test_generate_signature_uses_expected_payload(self):
        client = WeselAjaClient(api_key="api-key", secret_key="secret")

        signature = client.generate_signature(
            method="POST",
            uri="/v1/payins",
            timestamp="2026-10-01T00:00:00Z",
            body='{"initiatedAmount":10000}',
            secret_key="secret",
        )

        self.assertIsInstance(signature, str)
        self.assertTrue(signature)

    def test_build_payment_payload_sets_required_fields(self):
        client = WeselAjaClient(api_key="api-key", secret_key="secret")

        payload = client.build_payment_payload(
            amount=10000,
            currency="IDR",
            payment_method="VIRTUAL_ACCOUNT",
            payment_channel="BRI.VA",
            reference_code="REF-123",
            customer_reference="CUST-123",
            customer_name="farhan",
            callback_url="https://example.com/callback",
            redirect_url="https://example.com/return",
            customer_phone_number="08123456789",
            description="Pembayaran test",
        )

        self.assertEqual(payload["initiatedAmount"], 10000)
        self.assertEqual(payload["paymentMethod"], "VIRTUAL_ACCOUNT")
        self.assertEqual(payload["paymentChannel"], "BRI.VA")
        self.assertEqual(payload["customerName"], "farhan")


if __name__ == "__main__":
    unittest.main()
