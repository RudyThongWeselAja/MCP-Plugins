<?php
declare(strict_types=1);

namespace WeselAja;

use WeselAja\Exceptions\WeselAjaApiException;
use WeselAja\Models\CreatePaymentRequest;
use WeselAja\Models\CreatePaymentResponse;

final class WeselAjaClient
{
    private const PAYMENT_URI = '/v1/payins';

    public function __construct(
        private readonly WeselAjaConfig $config
    ) {
        $this->config->validate();
    }

    public static function fromEnvironment(): self
    {
        return new self(WeselAjaConfig::fromEnvironment());
    }

    public function createPayment(
        CreatePaymentRequest $request
    ): CreatePaymentResponse {
        $request->validate();

        $paymentChannel = BankChannels::resolve($request->bank);
        $referenceCode = $request->referenceCode;

        if ($referenceCode === null || trim($referenceCode) === '') {
            $referenceCode = $this->generateReference(
                $request->customerName
            );
        }

        $this->validateReference($referenceCode);
        $this->validateReference($this->config->customerReference);

        $payload = [
            'initiatedAmount' => $request->amount,
            'currency' => $this->config->currency,
            'paymentMethod' => $this->config->paymentMethod,
            'paymentChannel' => $paymentChannel,
            'referenceCode' => $referenceCode,
            'customerReference' => $this->config->customerReference,
            'customerName' => trim($request->customerName),
            'callbackUrl' => $this->config->callbackUrl,
            'redirectUrl' => $this->config->redirectUrl,
        ];

        if (
            $request->customerPhoneNumber !== null
            && trim($request->customerPhoneNumber) !== ''
        ) {
            $payload['customerPhoneNumber'] = trim(
                $request->customerPhoneNumber
            );
        }

        if (
            $request->description !== null
            && trim($request->description) !== ''
        ) {
            $payload['description'] = trim($request->description);
        }

        $body = json_encode(
            $payload,
            JSON_UNESCAPED_SLASHES
                | JSON_UNESCAPED_UNICODE
                | JSON_THROW_ON_ERROR
        );

        $timestamp = (new \DateTimeImmutable(
            'now',
            new \DateTimeZone('UTC')
        ))->format('Y-m-d\TH:i:s.v\Z');

        $signature = WeselAjaSignature::generate(
            'POST',
            self::PAYMENT_URI,
            $timestamp,
            $body,
            $this->config->secretKey
        );

        $idempotencyKey = 'php-' . bin2hex(random_bytes(16));
        $url = rtrim($this->config->apiUrl, '/') . self::PAYMENT_URI;

        $curl = curl_init();

        if ($curl === false) {
            throw new WeselAjaApiException(
                'Unable to initialize PHP cURL.'
            );
        }

        $headers = [
            'Content-Type: application/json',
            'Accept: application/json',
            'User-Agent: WeselAja-PHP/1.0',
            'Accept-Encoding: identity',
            'Connection: close',
            'Xenith-Api-Key: ' . $this->config->apiKey,
            'Xenith-Request-Timestamp: ' . $timestamp,
            'Xenith-Request-Signature: ' . $signature,
            'X-Idempotency-Key: ' . $idempotencyKey,
        ];

        $curlOptions = [
            CURLOPT_URL => $url,
            CURLOPT_PROXY => '',
            CURLOPT_POST => true,
            CURLOPT_POSTFIELDS => $body,
            CURLOPT_HTTPHEADER => $headers,
            CURLOPT_RETURNTRANSFER => true,
            CURLOPT_HEADER => false,
            CURLOPT_CONNECTTIMEOUT => 10,
            CURLOPT_TIMEOUT => 30,
            CURLOPT_FOLLOWLOCATION => false,
            CURLOPT_HTTP_VERSION => CURL_HTTP_VERSION_1_1,
            CURLOPT_FRESH_CONNECT => true,
            CURLOPT_FORBID_REUSE => true,
            CURLOPT_SSL_VERIFYPEER => true,
            CURLOPT_SSL_VERIFYHOST => 2,
        ];

        if (
            $this->config->caInfo !== null
            && trim($this->config->caInfo) !== ''
        ) {
            $curlOptions[CURLOPT_CAINFO] = $this->config->caInfo;
        }

        curl_setopt_array($curl, $curlOptions);

        $responseBody = curl_exec($curl);
        $statusCode = (int) curl_getinfo(
            $curl,
            CURLINFO_HTTP_CODE
        );
        $contentType = (string) curl_getinfo(
            $curl,
            CURLINFO_CONTENT_TYPE
        );
        $curlError = curl_error($curl);

        curl_close($curl);

        if ($responseBody === false) {
            throw new WeselAjaApiException(
                'Unable to connect to WeselAja: ' . $curlError
            );
        }

        if (
            !is_string($responseBody)
            || trim($responseBody) === ''
        ) {
            throw new WeselAjaApiException(
                'Empty response from WeselAja. HTTP ' . $statusCode,
                $statusCode,
                is_string($responseBody) ? $responseBody : ''
            );
        }

        if ($statusCode < 200 || $statusCode >= 300) {
            $responsePreview = substr(trim($responseBody), 0, 800);

            throw new WeselAjaApiException(
                'WeselAja returned HTTP ' . $statusCode
                . '; Content-Type: '
                . ($contentType !== '' ? $contentType : 'unknown')
                . '; Response: ' . $responsePreview,
                $statusCode,
                $responseBody
            );
        }

        try {
            $decoded = json_decode(
                $responseBody,
                true,
                512,
                JSON_THROW_ON_ERROR
            );
        } catch (\JsonException $exception) {
            throw new WeselAjaApiException(
                'Invalid JSON response from WeselAja. HTTP '
                . $statusCode . ': ' . $exception->getMessage(),
                $statusCode,
                $responseBody
            );
        }

        if (!is_array($decoded)) {
            throw new WeselAjaApiException(
                'Unexpected WeselAja response format.',
                $statusCode,
                $responseBody
            );
        }

        if (
            isset($decoded['success'])
            && $decoded['success'] === false
        ) {
            throw new WeselAjaApiException(
                $this->extractError($decoded, $responseBody),
                $statusCode,
                $responseBody
            );
        }

        $paymentData =
            isset($decoded['data']) && is_array($decoded['data'])
                ? $decoded['data']
                : $decoded;

        $paymentData['success'] = true;

        return CreatePaymentResponse::fromArray($paymentData);
    }

    public function getPayins(
        ?string $status = null,
        ?int $limit = null
    ): array {
        if ($status !== null && trim($status) !== '') {
            $status = strtoupper(trim($status));

            if (!in_array(
                $status,
                ['PENDING', 'SUCCESS', 'EXPIRED', 'FAILED'],
                true
            )) {
                throw new \InvalidArgumentException(
                    'status must be one of: PENDING, SUCCESS, EXPIRED, FAILED.'
                );
            }
        } else {
            $status = null;
        }

        if ($limit !== null && $limit <= 0) {
            throw new \InvalidArgumentException(
                'limit must be a positive integer.'
            );
        }

        $query = [];

        if ($status !== null) {
            $query['status'] = $status;
        }

        if ($limit !== null) {
            $query['limit'] = $limit;
        }

        $requestUri = self::PAYMENT_URI;

        if ($query !== []) {
            $requestUri .= '?' . http_build_query(
                $query,
                '',
                '&',
                PHP_QUERY_RFC3986
            );
        }

        $url = rtrim($this->config->apiUrl, '/') . $requestUri;

        $timestamp = (new \DateTimeImmutable(
            'now',
            new \DateTimeZone('UTC')
        ))->format('Y-m-d\TH:i:s.v\Z');

        $signature = WeselAjaSignature::generate(
            'GET',
            self::PAYMENT_URI,
            $timestamp,
            '',
            $this->config->secretKey
        );

        $curl = curl_init();

        if ($curl === false) {
            throw new WeselAjaApiException(
                'Unable to initialize PHP cURL.'
            );
        }

        $headers = [
            'Accept: application/json',
            'User-Agent: WeselAja-PHP/1.0',
            'Accept-Encoding: identity',
            'Connection: close',
            'Xenith-Api-Key: ' . $this->config->apiKey,
            'Xenith-Request-Timestamp: ' . $timestamp,
            'Xenith-Request-Signature: ' . $signature,
        ];

        $curlOptions = [
            CURLOPT_URL => $url,
            CURLOPT_PROXY => '',
            CURLOPT_HTTPGET => true,
            CURLOPT_HTTPHEADER => $headers,
            CURLOPT_RETURNTRANSFER => true,
            CURLOPT_HEADER => false,
            CURLOPT_CONNECTTIMEOUT => 10,
            CURLOPT_TIMEOUT => 30,
            CURLOPT_FOLLOWLOCATION => false,
            CURLOPT_HTTP_VERSION => CURL_HTTP_VERSION_1_1,
            CURLOPT_FRESH_CONNECT => true,
            CURLOPT_FORBID_REUSE => true,
            CURLOPT_SSL_VERIFYPEER => true,
            CURLOPT_SSL_VERIFYHOST => 2,
        ];

        if (
            $this->config->caInfo !== null
            && trim($this->config->caInfo) !== ''
        ) {
            $curlOptions[CURLOPT_CAINFO] = $this->config->caInfo;
        }

        curl_setopt_array($curl, $curlOptions);

        $responseBody = curl_exec($curl);
        $statusCode = (int) curl_getinfo(
            $curl,
            CURLINFO_HTTP_CODE
        );
        $contentType = (string) curl_getinfo(
            $curl,
            CURLINFO_CONTENT_TYPE
        );
        $curlError = curl_error($curl);

        curl_close($curl);

        if ($responseBody === false) {
            throw new WeselAjaApiException(
                'Unable to connect to WeselAja: ' . $curlError
            );
        }

        if (
            !is_string($responseBody)
            || trim($responseBody) === ''
        ) {
            throw new WeselAjaApiException(
                'Empty response from WeselAja. HTTP ' . $statusCode,
                $statusCode,
                is_string($responseBody) ? $responseBody : ''
            );
        }

        if ($statusCode < 200 || $statusCode >= 300) {
            $responsePreview = substr(trim($responseBody), 0, 800);

            throw new WeselAjaApiException(
                'WeselAja returned HTTP ' . $statusCode
                . '; Content-Type: '
                . ($contentType !== '' ? $contentType : 'unknown')
                . '; Response: ' . $responsePreview,
                $statusCode,
                $responseBody
            );
        }

        try {
            $decoded = json_decode(
                $responseBody,
                true,
                512,
                JSON_THROW_ON_ERROR
            );
        } catch (\JsonException $exception) {
            throw new WeselAjaApiException(
                'Invalid JSON response from WeselAja. HTTP '
                . $statusCode . ': ' . $exception->getMessage(),
                $statusCode,
                $responseBody
            );
        }

        if (!is_array($decoded)) {
            throw new WeselAjaApiException(
                'Unexpected WeselAja response format.',
                $statusCode,
                $responseBody
            );
        }

        return $decoded;
    }

    private function generateReference(
        string $customerName
    ): string {
        $normalized = preg_replace(
            '/[^A-Z0-9_-]/',
            '',
            strtoupper($customerName)
        );

        if (
            !is_string($normalized)
            || trim($normalized) === ''
        ) {
            $normalized = 'CUSTOMER';
        }

        $normalized = substr($normalized, 0, 20);

        return 'WES-' . $normalized . '-' . gmdate('YmdHis');
    }

    private function validateReference(
        string $value
    ): void {
        if (trim($value) === '') {
            throw new \InvalidArgumentException(
                'Reference value is required.'
            );
        }

        if (preg_match('/^[A-Za-z0-9_-]+$/', $value) !== 1) {
            throw new \InvalidArgumentException(
                'Reference values may only contain letters, numbers, "-" and "_".'
            );
        }
    }

    private function extractError(
        array $response,
        string $rawBody
    ): string {
        foreach (['message', 'error', 'code'] as $key) {
            if (
                isset($response[$key])
                && is_string($response[$key])
                && trim($response[$key]) !== ''
            ) {
                return $response[$key];
            }
        }

        return $rawBody;
    }
}