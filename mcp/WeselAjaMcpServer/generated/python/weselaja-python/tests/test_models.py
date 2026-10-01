import unittest

from models import (
    bank_to_channel,
)


class BankMappingTests(
    unittest.TestCase,
):

    def test_bri(self):
        self.assertEqual(
            bank_to_channel("BRI"),
            "BRI.VA",
        )

    def test_mandiri(self):
        self.assertEqual(
            bank_to_channel("Mandiri"),
            "MDR.VA",
        )

    def test_bni(self):
        self.assertEqual(
            bank_to_channel("BNI"),
            "BNI.VA",
        )

    def test_permata(self):
        self.assertEqual(
            bank_to_channel("Permata"),
            "PTB.VA",
        )

    def test_cimb(self):
        self.assertEqual(
            bank_to_channel("CIMB"),
            "CIMB.VA",
        )

    def test_bca(self):
        self.assertEqual(
            bank_to_channel("BCA"),
            "BCA.VA",
        )


if __name__ == "__main__":
    unittest.main()