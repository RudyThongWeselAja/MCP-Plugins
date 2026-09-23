<?php

declare(strict_types=1);

require_once __DIR__ . '/signature.php';

final class WeselAjaClient
{
    private const PAYMENT_URI = '/v1/payins';

    private string $apiUrl;
    private string $apiKey;
    private string $secretKey;

    private WeselAjaSignature $signatureGenerator;

    public function __construct()
    {
        $this->apiUrl = trim(
            getenv('WESELAJA_API_URL')
                ?: 'https://sandbox.checkout.weselaja.id'
        );

        $this->apiUrl = rtrim(
            $this->apiUrl,
            '/'
        );

        $this->apiKey = trim(
            getenv('XENITH_API_KEY')
                ?: ''
        );

        $this->secretKey = trim(
            getenv('XENITH_SECRET_KEY')
                ?: ''
        );

        $this->signatureGenerator =
            new WeselAjaSignature();
    }

    public function createPayment(
        array $arguments
    ): array {
        try {
            $this->validateConfiguration();
            $this->validateArguments($arguments);

            $payload = [
                'initiatedAmount' =>
                    (int) $arguments['amount'],

                'currency' =>
                    (string) $arguments['currency'],

                'paymentMethod' =>
                    (string) $arguments['paymentMethod'],

                'paymentChannel' =>
                    (string) $arguments['paymentChannel'],

                'referenceCode' =>
                    (string) $arguments['referenceCode'],

                'customerReference' =>
                    (string) $arguments['customerReference'],

                'customerName' =>
                    (string) $arguments['customerName'],

                'callbackUrl' =>
                    (string) $arguments['callbackUrl'],

                'redirectUrl' =>
                    (string) $arguments['redirectUrl'],
            ];

            if (
                isset($arguments['customerPhoneNumber']) &&
                trim((string) $arguments['customerPhoneNumber']) !== ''
            ) {
                $payload['customerPhoneNumber'] =
                    (string) $arguments['customerPhoneNumber'];
            }

            if (
                isset($arguments['description']) &&
                trim((string) $arguments['description']) !== ''
            ) {
                $payload['description'] =
                    (string) $arguments['description'];
            }

            $body = json_encode(
                $payload,
                JSON_UNESCAPED_SLASHES |
                JSON_UNESCAPED_UNICODE |
                JSON_THROW_ON_ERROR
            );

            $timestamp =
                new DateTimeImmutable(
                    'now',
                    new DateTimeZone('UTC')
                );

            $timestamp =
                $timestamp->format(
                    'Y-m-d\TH:i:s.v\Z'
                );

            $signature =
                $this->signatureGenerator->generate(
                    'POST',
                    self::PAYMENT_URI,
                    $timestamp,
                    $body,
                    $this->secretKey
                );

            $idempotencyKey =
                'php-' .
                bin2hex(random_bytes(16));

            $url =
                $this->apiUrl .
                self::PAYMENT_URI;

            $headers = [
                'Content-Type: application/json',
                'Accept: application/json',

                // Dipertahankan karena profile ini
                // sudah terbukti mendapat HTTP 200.
                'User-Agent: Python-urllib/3.13',
                'Accept-Encoding: identity',
                'Connection: close',
                'Expect:',

                'Xenith-Api-Key: ' .
                    $this->apiKey,

                'Xenith-Request-Timestamp: ' .
                    $timestamp,

                'Xenith-Request-Signature: ' .
                    $signature,

                'X-Idempotency-Key: ' .
                    $idempotencyKey,
            ];

            $curl = curl_init();

            if ($curl === false) {
                return $this->errorResponse(
                    'Unable to initialize PHP cURL.'
                );
            }

            curl_setopt_array(
                $curl,
                [
                    CURLOPT_URL =>
                        $url,

                    CURLOPT_POST =>
                        true,

                    CURLOPT_POSTFIELDS =>
                        $body,

                    CURLOPT_HTTPHEADER =>
                        $headers,

                    CURLOPT_RETURNTRANSFER =>
                        true,

                    CURLOPT_HEADER =>
                        false,

                    CURLOPT_CONNECTTIMEOUT =>
                        10,

                    CURLOPT_TIMEOUT =>
                        30,

                    CURLOPT_FOLLOWLOCATION =>
                        false,

                    CURLOPT_HTTP_VERSION =>
                        CURL_HTTP_VERSION_1_1,

                    CURLOPT_FRESH_CONNECT =>
                        true,

                    CURLOPT_FORBID_REUSE =>
                        true,

                    CURLOPT_SSL_VERIFYPEER =>
                        true,

                    CURLOPT_SSL_VERIFYHOST =>
                        2,

                    CURLOPT_CAINFO =>
                        'C:\php-8.5.10\extras\ssl\cacert.pem',
                ]
            );

            $responseBody =
                curl_exec($curl);

            $statusCode =
                (int) curl_getinfo(
                    $curl,
                    CURLINFO_HTTP_CODE
                );

            $curlErrorNumber =
                curl_errno($curl);

            $curlError =
                curl_error($curl);

            // JANGAN gunakan curl_close() pada PHP 8.5.
            // Handle akan dibersihkan otomatis.

            if ($responseBody === false) {
                return $this->errorResponse(
                    'Unable to connect to WeselAja. ' .
                    'cURL error ' .
                    $curlErrorNumber .
                    ': ' .
                    $curlError
                );
            }

            if (
                !is_string($responseBody) ||
                trim($responseBody) === ''
            ) {
                return $this->errorResponse(
                    'Empty response from WeselAja. HTTP ' .
                    $statusCode
                );
            }

            try {
                $response =
                    json_decode(
                        $responseBody,
                        true,
                        512,
                        JSON_THROW_ON_ERROR
                    );
            } catch (JsonException) {
                return $this->errorResponse(
                    'Invalid JSON response from WeselAja. HTTP ' .
                    $statusCode .
                    ': ' .
                    $responseBody
                );
            }

            if (!is_array($response)) {
                return $this->errorResponse(
                    'Unexpected WeselAja response format.'
                );
            }

            if (
                $statusCode < 200 ||
                $statusCode >= 300
            ) {
                return $this->errorResponse(
                    $this->extractError(
                        $response,
                        $responseBody
                    )
                );
            }

            return [
                'success' => true,

                'paymentId' =>
                    $response['id'] ?? null,

                'initiatedAmount' =>
                    $response['initiatedAmount'] ?? null,

                'paymentAmount' =>
                    $response['paymentAmount'] ?? null,

                'feeAmount' =>
                    $response['feeAmount'] ?? null,

                'currency' =>
                    $response['currency'] ?? null,

                'paymentMethod' =>
                    $response['paymentMethod'] ?? null,

                'paymentChannel' =>
                    $response['paymentChannel'] ?? null,

                'paymentCode' =>
                    $response['paymentCode'] ??
                    $response['paymentUrl'] ??
                    null,

                'paymentCodeType' =>
                    $response['paymentCodeType'] ?? null,

                'referenceCode' =>
                    $response['referenceCode'] ?? null,

                'customerReference' =>
                    $response['customerReference'] ?? null,

                'customerName' =>
                    $response['customerName'] ?? null,

                'status' =>
                    $response['status'] ?? null,

                'createdTime' =>
                    $response['createdTime'] ?? null,

                'updatedTime' =>
                    $response['updatedTime'] ?? null,

                'expirationTime' =>
                    $response['expirationTime'] ?? null,

                'description' =>
                    $response['description'] ?? null,

                'callbackUrl' =>
                    $response['callbackUrl'] ?? null,

                'redirectUrl' =>
                    $response['redirectUrl'] ?? null,

                'payerAccountName' =>
                    $response['payerAccountName'] ?? null,

                'payerAccountNumber' =>
                    $response['payerAccountNumber'] ?? null,

                'payerPaymentChannel' =>
                    $response['payerPaymentChannel'] ?? null,

                'metadata' =>
                    isset($response['metadata']) &&
                    is_array($response['metadata'])
                        ? $response['metadata']
                        : [],

                'error' => null,
            ];

        } catch (Throwable $exception) {
            return $this->errorResponse(
                $exception->getMessage()
            );
        }
    }

    private function validateConfiguration(): void
    {
        if ($this->apiKey === '') {
            throw new RuntimeException(
                'WeselAja API key is not configured.'
            );
        }

        if ($this->secretKey === '') {
            throw new RuntimeException(
                'WeselAja secret key is not configured.'
            );
        }

        if ($this->apiUrl === '') {
            throw new RuntimeException(
                'WeselAja API URL is not configured.'
            );
        }
    }

    private function validateArguments(
        array $arguments
    ): void {
        $required = [
            'amount',
            'currency',
            'paymentMethod',
            'paymentChannel',
            'referenceCode',
            'customerReference',
            'customerName',
            'callbackUrl',
            'redirectUrl',
        ];

        foreach ($required as $field) {
            if (
                !array_key_exists(
                    $field,
                    $arguments
                )
            ) {
                throw new InvalidArgumentException(
                    $field . ' is required.'
                );
            }

            if (
                is_string($arguments[$field]) &&
                trim($arguments[$field]) === ''
            ) {
                throw new InvalidArgumentException(
                    $field . ' is required.'
                );
            }
        }

        if (
            !is_numeric($arguments['amount']) ||
            (int) $arguments['amount'] <= 0
        ) {
            throw new InvalidArgumentException(
                'Payment amount must be greater than zero.'
            );
        }
    }

    private function extractError(
        array $response,
        string $rawBody
    ): string {
        if (
            isset($response['message']) &&
            is_string($response['message'])
        ) {
            return $response['message'];
        }

        if (
            isset($response['error']) &&
            is_string($response['error'])
        ) {
            return $response['error'];
        }

        if (
            isset($response['code']) &&
            is_string($response['code'])
        ) {
            return $response['code'];
        }

        return $rawBody;
    }

    private function errorResponse(
        string $message
    ): array {
        return [
            'success' => false,

            'paymentId' => null,
            'initiatedAmount' => null,
            'paymentAmount' => null,
            'feeAmount' => null,
            'currency' => null,
            'paymentMethod' => null,
            'paymentChannel' => null,
            'paymentCode' => null,
            'paymentCodeType' => null,
            'referenceCode' => null,
            'customerReference' => null,
            'customerName' => null,
            'status' => null,
            'createdTime' => null,
            'updatedTime' => null,
            'expirationTime' => null,
            'description' => null,
            'callbackUrl' => null,
            'redirectUrl' => null,
            'payerAccountName' => null,
            'payerAccountNumber' => null,
            'payerPaymentChannel' => null,
            'metadata' => [],

            'error' => $message,
        ];
    }
}