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
            $this->validateArguments(
                $arguments
            );

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
                trim(
                    (string)
                    $arguments['customerPhoneNumber']
                ) !== ''
            ) {
                $payload['customerPhoneNumber'] =
                    (string)
                    $arguments['customerPhoneNumber'];
            }

            if (
                isset($arguments['description']) &&
                trim(
                    (string)
                    $arguments['description']
                ) !== ''
            ) {
                $payload['description'] =
                    (string)
                    $arguments['description'];
            }

            $body = json_encode(
                $payload,
                JSON_UNESCAPED_SLASHES |
                JSON_UNESCAPED_UNICODE |
                JSON_THROW_ON_ERROR
            );

            $timestamp =
                (
                    new DateTimeImmutable(
                        'now',
                        new DateTimeZone('UTC')
                    )
                )->format(
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
                'woocommerce-' .
                bin2hex(
                    random_bytes(16)
                );

            $url =
                $this->apiUrl .
                self::PAYMENT_URI;

            $headers = [
                'Content-Type: application/json',
                'Accept: application/json',
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

            $this->printRequestDebug(
                $url,
                $timestamp,
                $signature,
                $idempotencyKey,
                $body,
                $headers
            );

            $curlVersion =
                curl_version();

            fwrite(
                STDERR,
                PHP_EOL .
                '========== WOOCOMMERCE HTTP STACK ==========' .
                PHP_EOL
            );

            fwrite(
                STDERR,
                'PHP Version   : ' .
                PHP_VERSION .
                PHP_EOL
            );

            fwrite(
                STDERR,
                'cURL Version  : ' .
                ($curlVersion['version'] ?? '') .
                PHP_EOL
            );

            fwrite(
                STDERR,
                'SSL Version   : ' .
                ($curlVersion['ssl_version'] ?? '') .
                PHP_EOL
            );

            fwrite(
                STDERR,
                'Libz Version  : ' .
                ($curlVersion['libz_version'] ?? '') .
                PHP_EOL
            );

            fwrite(
                STDERR,
                '==============================================' .
                PHP_EOL
            );

            $curl =
                curl_init();

            if ($curl === false) {
                return $this->errorResponse(
                    'Unable to initialize PHP cURL.'
                );
            }

            curl_setopt(
                $curl,
                CURLINFO_HEADER_OUT,
                true
            );

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
                        true,

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

            $startedAt =
                microtime(true);

            $rawResponse =
                curl_exec(
                    $curl
                );

            $elapsedMs =
                (
                    microtime(true) -
                    $startedAt
                ) * 1000;

            $actualRequest =
                curl_getinfo(
                    $curl,
                    CURLINFO_HEADER_OUT
                );

            $statusCode =
                (int)
                curl_getinfo(
                    $curl,
                    CURLINFO_HTTP_CODE
                );

            $httpVersion =
                curl_getinfo(
                    $curl,
                    CURLINFO_HTTP_VERSION
                );

            $contentType =
                curl_getinfo(
                    $curl,
                    CURLINFO_CONTENT_TYPE
                );

            $effectiveUrl =
                curl_getinfo(
                    $curl,
                    CURLINFO_EFFECTIVE_URL
                );

            $remoteIp =
                curl_getinfo(
                    $curl,
                    CURLINFO_PRIMARY_IP
                );

            $localIp =
                curl_getinfo(
                    $curl,
                    CURLINFO_LOCAL_IP
                );

            $localPort =
                curl_getinfo(
                    $curl,
                    CURLINFO_LOCAL_PORT
                );

            $remotePort =
                curl_getinfo(
                    $curl,
                    CURLINFO_PRIMARY_PORT
                );

            $nameLookupTime =
                curl_getinfo(
                    $curl,
                    CURLINFO_NAMELOOKUP_TIME
                );

            $connectTime =
                curl_getinfo(
                    $curl,
                    CURLINFO_CONNECT_TIME
                );

            $appConnectTime =
                curl_getinfo(
                    $curl,
                    CURLINFO_APPCONNECT_TIME
                );

            $preTransferTime =
                curl_getinfo(
                    $curl,
                    CURLINFO_PRETRANSFER_TIME
                );

            $startTransferTime =
                curl_getinfo(
                    $curl,
                    CURLINFO_STARTTRANSFER_TIME
                );

            $totalTime =
                curl_getinfo(
                    $curl,
                    CURLINFO_TOTAL_TIME
                );

            $curlErrorNumber =
                curl_errno(
                    $curl
                );

            $curlError =
                curl_error(
                    $curl
                );

            $headerSize =
                (int)
                curl_getinfo(
                    $curl,
                    CURLINFO_HEADER_SIZE
                );

            $this->printActualRequestDebug(
                $actualRequest,
                $effectiveUrl,
                $remoteIp,
                $remotePort,
                $localIp,
                $localPort
            );

            if (
                $rawResponse === false
            ) {
                $this->printResponseDebug(
                    $statusCode,
                    $httpVersion,
                    $elapsedMs,
                    $contentType,
                    '',
                    '',
                    $curlErrorNumber,
                    $curlError,
                    $nameLookupTime,
                    $connectTime,
                    $appConnectTime,
                    $preTransferTime,
                    $startTransferTime,
                    $totalTime
                );

                return $this->errorResponse(
                    'Unable to connect to XenithPay. ' .
                    'cURL error ' .
                    $curlErrorNumber .
                    ': ' .
                    $curlError
                );
            }

            $responseHeaders =
                substr(
                    $rawResponse,
                    0,
                    $headerSize
                );

            $responseBody =
                substr(
                    $rawResponse,
                    $headerSize
                );

            $this->printResponseDebug(
                $statusCode,
                $httpVersion,
                $elapsedMs,
                $contentType,
                $responseHeaders,
                $responseBody,
                $curlErrorNumber,
                $curlError,
                $nameLookupTime,
                $connectTime,
                $appConnectTime,
                $preTransferTime,
                $startTransferTime,
                $totalTime
            );

            if (
                trim($responseBody) === ''
            ) {
                return $this->errorResponse(
                    'Empty response from XenithPay. HTTP ' .
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
            } catch (
                JsonException
            ) {
                return $this->errorResponse(
                    'Invalid JSON response from XenithPay. HTTP ' .
                    $statusCode .
                    ': ' .
                    $responseBody
                );
            }

            if (
                !is_array($response)
            ) {
                return $this->errorResponse(
                    'Unexpected XenithPay response format.'
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
                'success' =>
                    true,

                'paymentId' =>
                    isset($response['id'])
                        ? (string) $response['id']
                        : null,

                'initiatedAmount' =>
                    isset($response['initiatedAmount'])
                        ? (string)
                          $response['initiatedAmount']
                        : null,

                'paymentAmount' =>
                    isset($response['paymentAmount'])
                        ? (string)
                          $response['paymentAmount']
                        : null,

                'feeAmount' =>
                    isset($response['feeAmount'])
                        ? (string)
                          $response['feeAmount']
                        : null,

                'currency' =>
                    $response['currency']
                    ?? null,

                'paymentMethod' =>
                    $response['paymentMethod']
                    ?? null,

                'paymentChannel' =>
                    $response['paymentChannel']
                    ?? null,

                'paymentCode' =>
                    $response['paymentCode']
                    ?? null,

                'paymentCodeType' =>
                    $response['paymentCodeType']
                    ?? null,

                'referenceCode' =>
                    $response['referenceCode']
                    ?? null,

                'customerReference' =>
                    isset(
                        $response['customerReference']
                    )
                        ? (string)
                          $response['customerReference']
                        : null,

                'customerName' =>
                    $response['customerName']
                    ?? null,

                'status' =>
                    $response['status']
                    ?? null,

                'createdTime' =>
                    $response['createdTime']
                    ?? null,

                'updatedTime' =>
                    $response['updatedTime']
                    ?? null,

                'expirationTime' =>
                    $response['expirationTime']
                    ?? null,

                'description' =>
                    $response['description']
                    ?? null,

                'callbackUrl' =>
                    $response['callbackUrl']
                    ?? null,

                'redirectUrl' =>
                    $response['redirectUrl']
                    ?? null,

                'payerAccountName' =>
                    $response['payerAccountName']
                    ?? null,

                'payerAccountNumber' =>
                    $response['payerAccountNumber']
                    ?? null,

                'payerPaymentChannel' =>
                    $response['payerPaymentChannel']
                    ?? null,

                'metadata' =>
                    isset($response['metadata']) &&
                    is_array($response['metadata'])
                        ? $response['metadata']
                        : new stdClass(),

                'error' =>
                    null,
            ];
        } catch (
            Throwable $exception
        ) {
            return $this->errorResponse(
                $exception->getMessage()
            );
        }
    }

    private function printRequestDebug(
        string $url,
        string $timestamp,
        string $signature,
        string $idempotencyKey,
        string $body,
        array $headers
    ): void {
        $signaturePayload =
            'POST' .
            "\n" .
            self::PAYMENT_URI .
            "\n" .
            $timestamp .
            "\n" .
            $body;

        fwrite(
            STDERR,
            PHP_EOL .
            '========== WOOCOMMERCE -> WESELAJA ==========' .
            PHP_EOL
        );

        fwrite(
            STDERR,
            'API URL        : ' .
            $this->apiUrl .
            PHP_EOL
        );

        fwrite(
            STDERR,
            'Request URI    : ' .
            self::PAYMENT_URI .
            PHP_EOL
        );

        fwrite(
            STDERR,
            'Method         : POST' .
            PHP_EOL
        );

        fwrite(
            STDERR,
            'URL            : ' .
            $url .
            PHP_EOL
        );

        fwrite(
            STDERR,
            'HTTP Version   : 1.1' .
            PHP_EOL
        );

        fwrite(
            STDERR,
            'Timestamp      : ' .
            $timestamp .
            PHP_EOL
        );

        fwrite(
            STDERR,
            'API Key        : ' .
            $this->maskApiKey(
                $this->apiKey
            ) .
            PHP_EOL
        );

        fwrite(
            STDERR,
            'API Key SHA256 : ' .
            hash(
                'sha256',
                $this->apiKey
            ) .
            PHP_EOL
        );

        fwrite(
            STDERR,
            'Secret SHA256  : ' .
            hash(
                'sha256',
                $this->secretKey
            ) .
            PHP_EOL
        );

        fwrite(
            STDERR,
            'Signature      : ' .
            $signature .
            PHP_EOL
        );

        fwrite(
            STDERR,
            'Signature Payload SHA256: ' .
            hash(
                'sha256',
                $signaturePayload
            ) .
            PHP_EOL
        );

        fwrite(
            STDERR,
            'Idempotency    : ' .
            $idempotencyKey .
            PHP_EOL
        );

        fwrite(
            STDERR,
            'Body Length    : ' .
            strlen($body) .
            PHP_EOL
        );

        fwrite(
            STDERR,
            'Body SHA256    : ' .
            hash(
                'sha256',
                $body
            ) .
            PHP_EOL
        );

        fwrite(
            STDERR,
            'Request Body   : ' .
            $body .
            PHP_EOL
        );

        fwrite(
            STDERR,
            PHP_EOL .
            'Request Headers:' .
            PHP_EOL
        );

        foreach ($headers as $header) {
            if (
                stripos(
                    $header,
                    'Xenith-Api-Key:'
                ) === 0
            ) {
                $parts =
                    explode(
                        ':',
                        $header,
                        2
                    );

                fwrite(
                    STDERR,
                    '  Xenith-Api-Key: ' .
                    $this->maskApiKey(
                        trim(
                            $parts[1] ?? ''
                        )
                    ) .
                    PHP_EOL
                );

                continue;
            }

            fwrite(
                STDERR,
                '  ' .
                $header .
                PHP_EOL
            );
        }

        fwrite(
            STDERR,
            '==============================================' .
            PHP_EOL
        );
    }

    private function printActualRequestDebug(
        mixed $actualRequest,
        mixed $effectiveUrl,
        mixed $remoteIp,
        mixed $remotePort,
        mixed $localIp,
        mixed $localPort
    ): void {
        fwrite(
            STDERR,
            PHP_EOL .
            '========== WOOCOMMERCE ACTUAL CURL REQUEST ==========' .
            PHP_EOL
        );

        fwrite(
            STDERR,
            'Effective URL : ' .
            (string) $effectiveUrl .
            PHP_EOL
        );

        fwrite(
            STDERR,
            'Remote IP     : ' .
            (string) $remoteIp .
            PHP_EOL
        );

        fwrite(
            STDERR,
            'Remote Port   : ' .
            (string) $remotePort .
            PHP_EOL
        );

        fwrite(
            STDERR,
            'Local IP      : ' .
            (string) $localIp .
            PHP_EOL
        );

        fwrite(
            STDERR,
            'Local Port    : ' .
            (string) $localPort .
            PHP_EOL
        );

        fwrite(
            STDERR,
            PHP_EOL
        );

        if (
            !is_string($actualRequest) ||
            trim($actualRequest) === ''
        ) {
            fwrite(
                STDERR,
                'Unable to read actual cURL request.' .
                PHP_EOL
            );
        } else {
            $lines =
                preg_split(
                    "/\r\n|\n|\r/",
                    trim($actualRequest)
                );

            if (is_array($lines)) {
                foreach ($lines as $line) {
                    if (
                        trim($line) === ''
                    ) {
                        continue;
                    }

                    if (
                        preg_match(
                            '/^(Xenith-Api-Key:\s*)(.+)$/i',
                            $line,
                            $matches
                        )
                    ) {
                        fwrite(
                            STDERR,
                            $matches[1] .
                            $this->maskApiKey(
                                trim(
                                    $matches[2]
                                )
                            ) .
                            PHP_EOL
                        );

                        continue;
                    }

                    fwrite(
                        STDERR,
                        $line .
                        PHP_EOL
                    );
                }
            }
        }

        fwrite(
            STDERR,
            '====================================================' .
            PHP_EOL
        );
    }

    private function printResponseDebug(
        int $statusCode,
        mixed $httpVersion,
        float $elapsedMs,
        mixed $contentType,
        string $responseHeaders,
        string $responseBody,
        int $curlErrorNumber,
        string $curlError,
        mixed $nameLookupTime,
        mixed $connectTime,
        mixed $appConnectTime,
        mixed $preTransferTime,
        mixed $startTransferTime,
        mixed $totalTime
    ): void {
        fwrite(
            STDERR,
            PHP_EOL .
            '========== WOOCOMMERCE WESELAJA RESPONSE ==========' .
            PHP_EOL
        );

        fwrite(
            STDERR,
            'HTTP Status      : ' .
            $statusCode .
            PHP_EOL
        );

        fwrite(
            STDERR,
            'HTTP Version     : ' .
            $this->formatHttpVersion(
                $httpVersion
            ) .
            PHP_EOL
        );

        fwrite(
            STDERR,
            'Elapsed          : ' .
            number_format(
                $elapsedMs,
                2
            ) .
            ' ms' .
            PHP_EOL
        );

        fwrite(
            STDERR,
            'Content-Type     : ' .
            ($contentType ?: '') .
            PHP_EOL
        );

        fwrite(
            STDERR,
            'Content-Length   : ' .
            strlen($responseBody) .
            PHP_EOL
        );

        fwrite(
            STDERR,
            'Response Body Len: ' .
            strlen($responseBody) .
            PHP_EOL
        );

        fwrite(
            STDERR,
            'cURL Error No    : ' .
            $curlErrorNumber .
            PHP_EOL
        );

        fwrite(
            STDERR,
            'cURL Error       : ' .
            $curlError .
            PHP_EOL
        );

        fwrite(
            STDERR,
            PHP_EOL .
            'Timing:' .
            PHP_EOL
        );

        fwrite(
            STDERR,
            '  DNS Lookup     : ' .
            $this->formatSeconds(
                $nameLookupTime
            ) .
            PHP_EOL
        );

        fwrite(
            STDERR,
            '  Connect        : ' .
            $this->formatSeconds(
                $connectTime
            ) .
            PHP_EOL
        );

        fwrite(
            STDERR,
            '  TLS            : ' .
            $this->formatSeconds(
                $appConnectTime
            ) .
            PHP_EOL
        );

        fwrite(
            STDERR,
            '  Pre Transfer   : ' .
            $this->formatSeconds(
                $preTransferTime
            ) .
            PHP_EOL
        );

        fwrite(
            STDERR,
            '  Start Transfer : ' .
            $this->formatSeconds(
                $startTransferTime
            ) .
            PHP_EOL
        );

        fwrite(
            STDERR,
            '  Total          : ' .
            $this->formatSeconds(
                $totalTime
            ) .
            PHP_EOL
        );

        fwrite(
            STDERR,
            PHP_EOL .
            'Response Headers:' .
            PHP_EOL
        );

        $lines =
            preg_split(
                "/\r\n|\n|\r/",
                trim(
                    $responseHeaders
                )
            );

        if (is_array($lines)) {
            foreach ($lines as $line) {
                if (
                    trim($line) !== ''
                ) {
                    fwrite(
                        STDERR,
                        '  ' .
                        $line .
                        PHP_EOL
                    );
                }
            }
        }

        fwrite(
            STDERR,
            PHP_EOL .
            'RAW RESPONSE BODY:' .
            PHP_EOL
        );

        fwrite(
            STDERR,
            '--------------------------------------------------' .
            PHP_EOL
        );

        fwrite(
            STDERR,
            $responseBody .
            PHP_EOL
        );

        fwrite(
            STDERR,
            '--------------------------------------------------' .
            PHP_EOL
        );

        fwrite(
            STDERR,
            '====================================================' .
            PHP_EOL
        );
    }

    private function formatHttpVersion(
        mixed $version
    ): string {
        $version =
            (int) $version;

        if (
            defined('CURL_HTTP_VERSION_1_0') &&
            $version === CURL_HTTP_VERSION_1_0
        ) {
            return '1.0';
        }

        if (
            defined('CURL_HTTP_VERSION_1_1') &&
            $version === CURL_HTTP_VERSION_1_1
        ) {
            return '1.1';
        }

        if (
            defined('CURL_HTTP_VERSION_2_0') &&
            $version === CURL_HTTP_VERSION_2_0
        ) {
            return '2.0';
        }

        if (
            defined('CURL_HTTP_VERSION_2TLS') &&
            $version === CURL_HTTP_VERSION_2TLS
        ) {
            return '2TLS';
        }

        if (
            defined('CURL_HTTP_VERSION_3') &&
            $version === CURL_HTTP_VERSION_3
        ) {
            return '3';
        }

        return (string) $version;
    }

    private function formatSeconds(
        mixed $seconds
    ): string {
        if (
            !is_numeric($seconds)
        ) {
            return '';
        }

        return number_format(
            (float) $seconds * 1000,
            2
        ) . ' ms';
    }

    private function maskApiKey(
        string $apiKey
    ): string {
        if ($apiKey === '') {
            return '';
        }

        if (strlen($apiKey) <= 6) {
            return '***';
        }

        return
            substr(
                $apiKey,
                0,
                3
            ) .
            '***' .
            substr(
                $apiKey,
                -3
            );
    }

    private function validateConfiguration(): void
    {
        if ($this->apiKey === '') {
            throw new RuntimeException(
                'XenithPay API key is not configured.'
            );
        }

        if ($this->secretKey === '') {
            throw new RuntimeException(
                'XenithPay secret key is not configured.'
            );
        }

        if ($this->apiUrl === '') {
            throw new RuntimeException(
                'XenithPay API URL is not configured.'
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
                    $field .
                    ' is required.'
                );
            }

            if (
                is_string(
                    $arguments[$field]
                ) &&
                trim(
                    $arguments[$field]
                ) === ''
            ) {
                throw new InvalidArgumentException(
                    $field .
                    ' is required.'
                );
            }
        }

        if (
            !is_numeric(
                $arguments['amount']
            ) ||
            (int)
            $arguments['amount'] <= 0
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
            'metadata' => new stdClass(),

            'error' => $message,
        ];
    }
}