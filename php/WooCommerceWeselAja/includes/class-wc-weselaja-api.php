<?php

if (!defined('ABSPATH')) {
    exit;
}

final class WC_WeselAja_API
{
    private const PAYMENT_URI = '/v1/payins';

    private string $api_url;
    private string $api_key;
    private string $secret_key;

    public function __construct(array $settings)
    {
        $this->api_url = untrailingslashit(
            trim((string) ($settings['api_url'] ?? 'https://sandbox.checkout.weselaja.id'))
        );
        $this->api_key = trim((string) ($settings['api_key'] ?? ''));
        $this->secret_key = trim((string) ($settings['secret_key'] ?? ''));
    }

    public function create_payment(array $payment)
    {
        if ($this->api_url === '' || $this->api_key === '' || $this->secret_key === '') {
            return new WP_Error(
                'weselaja_configuration',
                __('WeselAja API URL, API key, and secret key must be configured.', 'weselaja-woocommerce')
            );
        }

        $required = [
            'initiatedAmount',
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
            if (!isset($payment[$field]) || $payment[$field] === '') {
                return new WP_Error(
                    'weselaja_invalid_payment',
                    sprintf(
                        /* translators: %s: API payment field name. */
                        __('Missing required payment field: %s', 'weselaja-woocommerce'),
                        $field
                    )
                );
            }
        }

        try {
            $body = wp_json_encode($payment, JSON_UNESCAPED_SLASHES | JSON_UNESCAPED_UNICODE | JSON_THROW_ON_ERROR);
            $timestamp = gmdate('Y-m-d\TH:i:s') . '.' . sprintf('%03d', (int) (microtime(true) * 1000) % 1000) . 'Z';
            $signature_payload = "POST\n" . self::PAYMENT_URI . "\n" . $timestamp . "\n" . $body;
            $signature = base64_encode(hash_hmac('sha256', $signature_payload, $this->secret_key, true));
            $idempotency_key = 'wc-' . wp_generate_uuid4();
        } catch (JsonException $exception) {
            return new WP_Error('weselaja_encode_error', $exception->getMessage());
        }

        $response = wp_remote_post($this->api_url . self::PAYMENT_URI, [
            'timeout' => 30,
            'redirection' => 0,
            'headers' => [
                'Content-Type' => 'application/json',
                'Accept' => 'application/json',
                'Xenith-Api-Key' => $this->api_key,
                'Xenith-Request-Timestamp' => $timestamp,
                'Xenith-Request-Signature' => $signature,
                'X-Idempotency-Key' => $idempotency_key,
            ],
            'body' => $body,
        ]);

        if (is_wp_error($response)) {
            return new WP_Error(
                'weselaja_http_error',
                sprintf(
                    /* translators: %s: HTTP client error. */
                    __('Could not connect to WeselAja: %s', 'weselaja-woocommerce'),
                    $response->get_error_message()
                )
            );
        }

        $status_code = (int) wp_remote_retrieve_response_code($response);
        $response_body = (string) wp_remote_retrieve_body($response);
        $decoded = json_decode($response_body, true);

        if (!is_array($decoded)) {
            return new WP_Error(
                'weselaja_invalid_response',
                __('WeselAja returned an invalid response.', 'weselaja-woocommerce')
            );
        }

        if ($status_code < 200 || $status_code >= 300) {
            $message = $decoded['message'] ?? $decoded['error'] ?? $decoded['code'] ?? '';
            if (is_array($message)) {
                $message = wp_json_encode($message);
            }

            return new WP_Error(
                'weselaja_api_error',
                $message !== '' ? sanitize_text_field((string) $message) : sprintf(
                    /* translators: %d: HTTP status code. */
                    __('WeselAja returned HTTP status %d.', 'weselaja-woocommerce'),
                    $status_code
                )
            );
        }

        if (empty($decoded['paymentCode']) && empty($decoded['redirectUrl'])) {
            return new WP_Error(
                'weselaja_missing_payment_details',
                __('WeselAja response did not include payment instructions or a redirect URL.', 'weselaja-woocommerce')
            );
        }

        return $decoded;
    }
}
