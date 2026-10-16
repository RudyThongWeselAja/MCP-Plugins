<?php

if (!defined('ABSPATH')) {
    exit;
}

final class WC_WeselAja_Webhook
{
    public static function init()
    {
        add_action('rest_api_init', [self::class, 'register_route']);
    }

    public static function register_route()
    {
        register_rest_route('weselaja/v1', '/webhook', [
            'methods' => 'POST',
            'callback' => [self::class, 'handle'],
            'permission_callback' => '__return_true',
        ]);
    }

    public static function handle(WP_REST_Request $request)
    {
        $raw_body = $request->get_body();
        $gateway_settings = get_option('woocommerce_weselaja_settings', []);
        $secret = is_array($gateway_settings)
            ? trim((string) ($gateway_settings['webhook_secret'] ?? ''))
            : '';

        if ($secret === '') {
            return new WP_Error(
                'weselaja_webhook_not_configured',
                __('WeselAja webhook secret is not configured.', 'weselaja-woocommerce'),
                ['status' => 503]
            );
        }

        $timestamp = (string) $request->get_header('xenith-request-timestamp');
        $provided_signature = (string) $request->get_header('xenith-request-signature');
        if ($timestamp === '' || $provided_signature === '') {
            return new WP_Error(
                'weselaja_webhook_signature_missing',
                __('WeselAja webhook signature headers are missing.', 'weselaja-woocommerce'),
                ['status' => 401]
            );
        }

        $signature_payload = "POST\n/weselaja/v1/webhook\n" . $timestamp . "\n" . $raw_body;
        $expected_signature = base64_encode(hash_hmac('sha256', $signature_payload, $secret, true));
        if (!hash_equals($expected_signature, $provided_signature)) {
            return new WP_Error(
                'weselaja_webhook_signature_invalid',
                __('WeselAja webhook signature is invalid.', 'weselaja-woocommerce'),
                ['status' => 401]
            );
        }

        $event = json_decode($raw_body, true);
        if (!is_array($event)) {
            return new WP_Error(
                'weselaja_webhook_invalid_json',
                __('WeselAja webhook payload is not valid JSON.', 'weselaja-woocommerce'),
                ['status' => 400]
            );
        }

        $reference = sanitize_text_field((string) ($event['referenceCode'] ?? ''));
        $status = strtoupper(sanitize_text_field((string) ($event['status'] ?? '')));
        if ($reference === '' || $status === '') {
            return new WP_Error(
                'weselaja_webhook_missing_fields',
                __('WeselAja webhook must include referenceCode and status.', 'weselaja-woocommerce'),
                ['status' => 400]
            );
        }

        $orders = wc_get_orders([
            'limit' => 1,
            'meta_key' => '_weselaja_reference_code',
            'meta_value' => $reference,
            'return' => 'objects',
        ]);
        if (!$orders) {
            return new WP_Error(
                'weselaja_order_not_found',
                __('No WooCommerce order matches this WeselAja reference.', 'weselaja-woocommerce'),
                ['status' => 404]
            );
        }

        $order = $orders[0];
        $order->update_meta_data('_weselaja_payment_status', $status);
        if (in_array($status, ['SUCCESS', 'PAID', 'COMPLETED'], true)) {
            $order->payment_complete(sanitize_text_field((string) ($event['id'] ?? '')));
        } elseif (in_array($status, ['FAILED', 'EXPIRED', 'CANCELLED', 'CANCELED'], true)) {
            if (!$order->is_paid()) {
                $order->update_status('failed', __('WeselAja payment was not completed.', 'weselaja-woocommerce'));
            }
        } else {
            $order->add_order_note(
                sprintf(
                    /* translators: %s: WeselAja payment status. */
                    __('WeselAja payment status updated to %s.', 'weselaja-woocommerce'),
                    $status
                )
            );
        }
        $order->save();

        return new WP_REST_Response(['received' => true], 200);
    }
}
