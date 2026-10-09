<?php
declare(strict_types=1);

namespace WeselAja\Models;

final class CreatePaymentRequest
{
    public function __construct(
        public readonly int $amount,
        public readonly string $customerName,
        public readonly string $bank,
        public readonly ?string $customerPhoneNumber = null,
        public readonly ?string $description = null,
        public readonly ?string $referenceCode = null
    ) {
    }

    public function validate(): void
    {
        if ($this->amount <= 0) {
            throw new \InvalidArgumentException(
                'Payment amount must be greater than zero.'
            );
        }

        if (
            trim($this->customerName) === ''
        ) {
            throw new \InvalidArgumentException(
                'Customer name is required.'
            );
        }

        if (
            trim($this->bank) === ''
        ) {
            throw new \InvalidArgumentException(
                'Bank is required.'
            );
        }
    }
}