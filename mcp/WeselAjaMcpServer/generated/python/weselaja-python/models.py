from __future__ import annotations
from dataclasses import dataclass, field
from typing import Any
BANK_TO_CHANNEL = {
    "BRI": "BRI.VA",
    "MANDIRI": "MDR.VA",
    "BNI": "BNI.VA",
    "PERMATA": "PTB.VA",
    "CIMB": "CIMB.VA",
    "BCA": "BCA.VA",
}
def normalize_bank(
    bank: str,
) -> str:
    value = (
        bank
        .strip()
        .upper()
        .replace(" ", "")
        .replace("-", "")
        .replace("_", "")
    )
    aliases = {
        "BANKBRI":
            "BRI",
        "BANKMANDIRI":
            "MANDIRI",
        "MDR":
            "MANDIRI",
        "BANKBNI":
            "BNI",
        "BANKPERMATA":
            "PERMATA",
        "PTB":
            "PERMATA",
        "CIMBNIAGA":
            "CIMB",
        "BANKCIMB":
            "CIMB",
        "BANKBCA":
            "BCA",
    }
    return aliases.get(
        value,
        value,
    )
def bank_to_channel(
    bank: str,
) -> str:
    normalized = (
        normalize_bank(
            bank
        )
    )
    try:
        return BANK_TO_CHANNEL[
            normalized
        ]
    except KeyError as exc:
        supported = (
            ", ".join(
                BANK_TO_CHANNEL.keys()
            )
        )
        raise ValueError(
            f"Unsupported bank '{bank}'. "
            f"Supported banks: {supported}."
        ) from exc
@dataclass(slots=True)
class CreatePaymentResponse:
    id: str | None = None
    initiated_amount: int | None = None
    payment_amount: int | None = None
    fee_amount: int | None = None
    currency: str | None = None
    payment_method: str | None = None
    payment_channel: str | None = None
    payment_code: str | None = None
    payment_code_type: str | None = None
    reference_code: str | None = None
    customer_reference: str | None = None
    customer_name: str | None = None
    status: str | None = None
    created_time: str | None = None
    updated_time: str | None = None
    expiration_time: str | None = None
    description: str | None = None
    callback_url: str | None = None
    redirect_url: str | None = None
    payer_account_name: str | None = None
    payer_account_number: str | None = None
    payer_payment_channel: str | None = None
    metadata: dict[str, Any] = field(
        default_factory=dict
    )
    @classmethod
    def from_dict(
        cls,
        data: dict[str, Any],
    ) -> "CreatePaymentResponse":
        return cls(
            id=data.get(
                "id"
            ),
            initiated_amount=data.get(
                "initiatedAmount"
            ),
            payment_amount=data.get(
                "paymentAmount"
            ),
            fee_amount=data.get(
                "feeAmount"
            ),
            currency=data.get(
                "currency"
            ),
            payment_method=data.get(
                "paymentMethod"
            ),
            payment_channel=data.get(
                "paymentChannel"
            ),
            payment_code=data.get(
                "paymentCode"
            ),
            payment_code_type=data.get(
                "paymentCodeType"
            ),
            reference_code=data.get(
                "referenceCode"
            ),
            customer_reference=data.get(
                "customerReference"
            ),
            customer_name=data.get(
                "customerName"
            ),
            status=data.get(
                "status"
            ),
            created_time=data.get(
                "createdTime"
            ),
            updated_time=data.get(
                "updatedTime"
            ),
            expiration_time=data.get(
                "expirationTime"
            ),
            description=data.get(
                "description"
            ),
            callback_url=data.get(
                "callbackUrl"
            ),
            redirect_url=data.get(
                "redirectUrl"
            ),
            payer_account_name=data.get(
                "payerAccountName"
            ),
            payer_account_number=data.get(
                "payerAccountNumber"
            ),
            payer_payment_channel=data.get(
                "payerPaymentChannel"
            ),
            metadata=(
                data.get(
                    "metadata"
                )
                if isinstance(
                    data.get(
                        "metadata"
                    ),
                    dict,
                )
                else {}
            ),
        )