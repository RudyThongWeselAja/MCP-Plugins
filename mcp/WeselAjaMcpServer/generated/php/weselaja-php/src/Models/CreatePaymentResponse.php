<?php
declare(strict_types=1);

namespace WeselAja\Models;

final class CreatePaymentResponse
{
    public function __construct(
        public readonly bool $success,
        public readonly ?string $paymentId,
        public readonly ?int $initiatedAmount,
        public readonly mixed $paymentAmount,
        public readonly mixed $feeAmount,
        public readonly ?string $currency,
        public readonly ?string $paymentMethod,
        public readonly ?string $paymentChannel,
        public readonly ?string $paymentCode,
        public readonly ?string $paymentCodeType,
        public readonly ?string $referenceCode,
        public readonly ?string $customerReference,
        public readonly ?string $customerName,
        public readonly ?string $status,
        public readonly ?string $createdTime,
        public readonly ?string $updatedTime,
        public readonly ?string $expirationTime,
        public readonly ?string $description,
        public readonly ?string $callbackUrl,
        public readonly ?string $redirectUrl,
        public readonly ?string $payerAccountName,
        public readonly ?string $payerAccountNumber,
        public readonly ?string $payerPaymentChannel,
        public readonly array $metadata,
        public readonly ?string $error = null
    ) {
    }

    public static function fromArray(
        array $response
    ): self {
        return new self(
            success:
                (bool) (
                    $response['success']
                    ?? false
                ),

            paymentId:
                self::stringValue(
                    $response,
                    'paymentId',
                    'id'
                ),

            initiatedAmount:
                self::intValue(
                    $response,
                    'initiatedAmount'
                ),

            paymentAmount:
                $response[
                    'paymentAmount'
                ]
                ?? null,

            feeAmount:
                $response[
                    'feeAmount'
                ]
                ?? null,

            currency:
                self::stringValue(
                    $response,
                    'currency'
                ),

            paymentMethod:
                self::stringValue(
                    $response,
                    'paymentMethod'
                ),

            paymentChannel:
                self::stringValue(
                    $response,
                    'paymentChannel'
                ),

            paymentCode:
                self::stringValue(
                    $response,
                    'paymentCode'
                ),

            paymentCodeType:
                self::stringValue(
                    $response,
                    'paymentCodeType'
                ),

            referenceCode:
                self::stringValue(
                    $response,
                    'referenceCode'
                ),

            customerReference:
                self::stringValue(
                    $response,
                    'customerReference'
                ),

            customerName:
                self::stringValue(
                    $response,
                    'customerName'
                ),

            status:
                self::stringValue(
                    $response,
                    'status'
                ),

            createdTime:
                self::stringValue(
                    $response,
                    'createdTime'
                ),

            updatedTime:
                self::stringValue(
                    $response,
                    'updatedTime'
                ),

            expirationTime:
                self::stringValue(
                    $response,
                    'expirationTime'
                ),

            description:
                self::stringValue(
                    $response,
                    'description'
                ),

            callbackUrl:
                self::stringValue(
                    $response,
                    'callbackUrl'
                ),

            redirectUrl:
                self::stringValue(
                    $response,
                    'redirectUrl'
                ),

            payerAccountName:
                self::stringValue(
                    $response,
                    'payerAccountName'
                ),

            payerAccountNumber:
                self::stringValue(
                    $response,
                    'payerAccountNumber'
                ),

            payerPaymentChannel:
                self::stringValue(
                    $response,
                    'payerPaymentChannel'
                ),

            metadata:
                isset(
                    $response['metadata']
                )
                &&
                is_array(
                    $response['metadata']
                )
                    ? $response['metadata']
                    : [],

            error:
                self::stringValue(
                    $response,
                    'error'
                )
        );
    }

    private static function stringValue(
        array $array,
        string ...$keys
    ): ?string {
        foreach ($keys as $key) {
            if (
                isset($array[$key])
                &&
                is_string(
                    $array[$key]
                )
            ) {
                return $array[$key];
            }
        }

        return null;
    }

    private static function intValue(
        array $array,
        string $key
    ): ?int {
        if (
            !isset(
                $array[$key]
            )
        ) {
            return null;
        }

        return (int) $array[$key];
    }
}