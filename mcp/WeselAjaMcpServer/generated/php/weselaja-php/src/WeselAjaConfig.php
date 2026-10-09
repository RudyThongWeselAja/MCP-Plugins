<?php
declare(strict_types=1);

namespace WeselAja;

use WeselAja\Exceptions\WeselAjaConfigurationException;

final class WeselAjaConfig
{
    public function __construct(
        public readonly string $apiUrl,
        public readonly string $apiKey,
        public readonly string $secretKey,
        public readonly string $customerReference,
        public readonly string $callbackUrl,
        public readonly string $redirectUrl,
        public readonly string $currency = 'IDR',
        public readonly string $paymentMethod = 'VIRTUAL_ACCOUNT',
        public readonly ?string $caInfo = null
    ) {
    }

    public static function fromEnvironment(): self
    {
        EnvLoader::load();

        $config =
            new self(
                apiUrl:
                    self::env(
                        'WESELAJA_API_URL',
                        'https://sandbox.checkout.weselaja.id'
                    ),

                apiKey:
                    self::env(
                        'WESELAJA_API_KEY'
                    ),

                secretKey:
                    self::env(
                        'WESELAJA_SECRET_KEY'
                    ),

                customerReference:
                    self::env(
                        'WESELAJA_CUSTOMER_REFERENCE'
                    ),

                callbackUrl:
                    self::env(
                        'WESELAJA_CALLBACK_URL'
                    ),

                redirectUrl:
                    self::env(
                        'WESELAJA_REDIRECT_URL'
                    ),

                currency:
                    self::env(
                        'WESELAJA_CURRENCY',
                        'IDR'
                    ),

                paymentMethod:
                    self::env(
                        'WESELAJA_PAYMENT_METHOD',
                        'VIRTUAL_ACCOUNT'
                    ),

                caInfo:
                    self::nullableEnv(
                        'WESELAJA_CAINFO'
                    )
            );

        $config->validate();

        return $config;
    }

    public function validate(): void
    {
        $required = [
            'WESELAJA_API_URL' =>
                $this->apiUrl,

            'WESELAJA_API_KEY' =>
                $this->apiKey,

            'WESELAJA_SECRET_KEY' =>
                $this->secretKey,

            'WESELAJA_CUSTOMER_REFERENCE' =>
                $this->customerReference,

            'WESELAJA_CALLBACK_URL' =>
                $this->callbackUrl,

            'WESELAJA_REDIRECT_URL' =>
                $this->redirectUrl,

            'WESELAJA_CURRENCY' =>
                $this->currency,

            'WESELAJA_PAYMENT_METHOD' =>
                $this->paymentMethod,
        ];

        $missing = [];

        foreach (
            $required as $key => $value
        ) {
            if (
                trim($value) === ''
            ) {
                $missing[] = $key;
            }
        }

        if ($missing !== []) {
            throw new WeselAjaConfigurationException(
                'Missing configuration: ' .
                implode(
                    ', ',
                    $missing
                )
            );
        }

        self::validateUrl(
            $this->apiUrl,
            'WESELAJA_API_URL'
        );

        self::validateUrl(
            $this->callbackUrl,
            'WESELAJA_CALLBACK_URL'
        );

        self::validateUrl(
            $this->redirectUrl,
            'WESELAJA_REDIRECT_URL'
        );

        if (
            $this->caInfo !== null
            &&
            !is_file(
                $this->caInfo
            )
        ) {
            throw new WeselAjaConfigurationException(
                'WESELAJA_CAINFO must point to a valid CA file.'
            );
        }
    }

    private static function env(
        string $key,
        string $fallback = ''
    ): string {
        $value =
            getenv($key);

        if (
            $value === false
            ||
            trim($value) === ''
        ) {
            return $fallback;
        }

        return trim($value);
    }

    private static function nullableEnv(
        string $key
    ): ?string {
        $value =
            self::env(
                $key
            );

        return $value === ''
            ? null
            : $value;
    }

    private static function validateUrl(
        string $value,
        string $key
    ): void {
        $parts =
            parse_url(
                $value
            );

        if (
            $parts === false
            ||
            !isset(
                $parts['scheme']
            )
            ||
            !in_array(
                strtolower(
                    $parts['scheme']
                ),
                [
                    'http',
                    'https'
                ],
                true
            )
        ) {
            throw new WeselAjaConfigurationException(
                $key .
                ' must be a valid HTTP or HTTPS URL.'
            );
        }
    }
}