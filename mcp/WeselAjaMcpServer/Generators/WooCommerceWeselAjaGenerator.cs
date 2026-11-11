using System.Text;

namespace WeselAjaMcpServer.Generators;

public sealed class WooCommerceWeselAjaGenerator
{
    private static readonly string[] RequiredPluginFiles =
    [
        "weselaja-woocommerce.php",
        "includes/class-wc-weselaja-api.php",
        "includes/class-wc-gateway-weselaja.php",
        "includes/class-wc-weselaja-webhook.php",
        "includes/class-wc-weselaja-blocks-support.php",
        "assets/js/weselaja-blocks.js",
        "README.md",
    ];

    private static readonly Dictionary<string, string> TemplateFiles =
        new(StringComparer.Ordinal)
        {
            ["weselaja-woocommerce.php"] = """
            <?php
            /**
             * Plugin Name: WooCommerce WeselAja
             * Description: Direct Payin Virtual Account integration for WeselAja.
             * Version: 1.1.0
             * Requires at least: 6.0
             * Requires PHP: 7.4
             * License: GPL-3.0-or-later
             * Text Domain: weselaja
             */

            if (!defined('ABSPATH')) {
                exit;
            }

            define('WESELAJA_WC_FILE', __FILE__);
            define('WESELAJA_WC_VERSION', '1.1.0');

            add_action('plugins_loaded', 'weselaja_woocommerce_init', 11);

            function weselaja_woocommerce_init(): void
            {
                if (!class_exists('WC_Payment_Gateway')) {
                    add_action('admin_notices', function (): void {
                        echo '<div class="notice notice-error"><p>';
                        echo esc_html__(
                            'WooCommerce WeselAja requires WooCommerce to be installed and active.',
                            'weselaja'
                        );
                        echo '</p></div>';
                    });
                    return;
                }

                require_once __DIR__ . '/includes/class-wc-weselaja-api.php';
                require_once __DIR__ . '/includes/class-wc-weselaja-webhook.php';
                require_once __DIR__ . '/includes/class-wc-gateway-weselaja.php';

                add_filter(
                    'woocommerce_payment_gateways',
                    function (array $methods): array {
                        $methods[] = 'WC_Gateway_WeselAja';
                        return $methods;
                    }
                );

                add_action(
                    'woocommerce_api_wc_xenith_callback',
                    ['WC_WeselAja_Webhook', 'handle']
                );
            }

            add_action('before_woocommerce_init', 'weselaja_declare_blocks_compatibility');

            function weselaja_declare_blocks_compatibility(): void
            {
                if (class_exists('\Automattic\WooCommerce\Utilities\FeaturesUtil')) {
                    \Automattic\WooCommerce\Utilities\FeaturesUtil::declare_compatibility(
                        'cart_checkout_blocks',
                        WESELAJA_WC_FILE,
                        true
                    );
                }
            }

            add_action('woocommerce_blocks_loaded', 'weselaja_register_blocks_support');

            function weselaja_register_blocks_support(): void
            {
                if (!class_exists(
                    '\Automattic\WooCommerce\Blocks\Payments\Integrations\AbstractPaymentMethodType'
                )) {
                    return;
                }

                require_once __DIR__ . '/includes/class-wc-weselaja-blocks-support.php';

                add_action(
                    'woocommerce_blocks_payment_method_type_registration',
                    function ($registry): void {
                        $registry->register(new WC_WeselAja_Blocks_Support());
                    }
                );
            }
            """,

            ["includes/class-wc-weselaja-api.php"] = """
            <?php

            if (!defined('ABSPATH')) {
                exit;
            }

            final class WC_WeselAja_API
            {
                private const PAYINS_URI = '/v2/payins';

                public function create_payin(
                    string $api_url,
                    string $api_key,
                    string $secret_key,
                    array $payload,
                    string $idempotency_key
                ): array {
                    if ($idempotency_key === '') {
                        return [
                            'success' => false,
                            'message' => 'Idempotency key is required.'
                        ];
                    }

                    return $this->request(
                        'POST',
                        $api_url,
                        $api_key,
                        $secret_key,
                        self::PAYINS_URI,
                        $payload,
                        $idempotency_key
                    );
                }

                public function get_payin(
                    string $api_url,
                    string $api_key,
                    string $secret_key,
                    string $payin_id
                ): array {
                    $payin_id = trim($payin_id);

                    if ($payin_id === '') {
                        return [
                            'success' => false,
                            'message' => 'Payin ID is required.'
                        ];
                    }

                    $uri = self::PAYINS_URI . '/' . rawurlencode($payin_id);

                    return $this->request(
                        'GET',
                        $api_url,
                        $api_key,
                        $secret_key,
                        $uri,
                        null,
                        ''
                    );
                }

                private function request(
                    string $method,
                    string $api_url,
                    string $api_key,
                    string $secret_key,
                    string $uri,
                    ?array $payload,
                    string $idempotency_key
                ): array {
                    $base_url = $this->resolve_base_url($api_url);

                    if (is_wp_error($base_url)) {
                        return [
                            'success' => false,
                            'message' => $base_url->get_error_message()
                        ];
                    }

                    if (trim($api_key) === '' || trim($secret_key) === '') {
                        return [
                            'success' => false,
                            'message' => 'WeselAja API credentials are required.'
                        ];
                    }

                    $method = strtoupper($method);
                    $body = '';

                    if ($method === 'POST') {
                        $body = wp_json_encode(
                            $payload ?? [],
                            JSON_UNESCAPED_SLASHES | JSON_UNESCAPED_UNICODE
                        );

                        if (!is_string($body)) {
                            return [
                                'success' => false,
                                'message' => 'Unable to encode the WeselAja request body.'
                            ];
                        }
                    }

                    $timestamp = (new DateTimeImmutable(
                        'now',
                        new DateTimeZone('UTC')
                    ))->format('Y-m-d\\TH:i:s.v\\Z');

                    $string_to_sign =
                        $method . "\n" .
                        $uri . "\n" .
                        $timestamp . "\n" .
                        $body;

                    $signature = base64_encode(
                        hash_hmac('sha256', $string_to_sign, $secret_key, true)
                    );

                    $headers = [
                        'Accept' => 'application/json',
                        'X-API-Key' => $api_key,
                        'X-Timestamp' => $timestamp,
                        'X-Signature' => $signature
                    ];

                    if ($method === 'POST') {
                        $headers['Content-Type'] = 'application/json';
                        $headers['X-Idempotency-Key'] = $idempotency_key;
                    }

                    $args = [
                        'method' => $method,
                        'timeout' => 30,
                        'redirection' => 0,
                        'sslverify' => true,
                        'headers' => $headers
                    ];

                    if ($method === 'POST') {
                        $args['body'] = $body;
                    }

                    $response = wp_remote_request(
                        $base_url . $uri,
                        $args
                    );

                    if (is_wp_error($response)) {
                        $this->log(
                            'error',
                            'WeselAja API connection failed: ' .
                            $response->get_error_message()
                        );

                        return [
                            'success' => false,
                            'message' => $response->get_error_message()
                        ];
                    }

                    $http_status = (int) wp_remote_retrieve_response_code($response);
                    $response_body = (string) wp_remote_retrieve_body($response);
                    $decoded = json_decode($response_body, true);

                    if (!is_array($decoded)) {
                        $this->log(
                            'error',
                            sprintf(
                                'WeselAja returned non-JSON response. HTTP %d.',
                                $http_status
                            )
                        );

                        return [
                            'success' => false,
                            'message' => sprintf(
                                'WeselAja returned an invalid response. HTTP %d.',
                                $http_status
                            ),
                            'http_status' => $http_status
                        ];
                    }

                    $message = $this->extract_error_message($decoded);

                    if (
                        $http_status < 200 ||
                        $http_status >= 300 ||
                        (array_key_exists('success', $decoded) && $decoded['success'] === false)
                    ) {
                        $this->log(
                            'error',
                            sprintf(
                                'WeselAja API returned HTTP %d: %s',
                                $http_status,
                                $message
                            )
                        );

                        return [
                            'success' => false,
                            'message' => $message,
                            'http_status' => $http_status
                        ];
                    }

                    $data = (
                        isset($decoded['data']) &&
                        is_array($decoded['data'])
                    ) ? $decoded['data'] : $decoded;

                    return [
                        'success' => true,
                        'data' => $data,
                        'http_status' => $http_status
                    ];
                }

                private function resolve_base_url(string $api_url)
                {
                    $api_url = rtrim(trim($api_url), '/');

                    if ($api_url === '') {
                        return new WP_Error(
                            'weselaja_invalid_api_url',
                            'WeselAja API base URL is required.'
                        );
                    }

                    $scheme = strtolower((string) wp_parse_url($api_url, PHP_URL_SCHEME));
                    $host = (string) wp_parse_url($api_url, PHP_URL_HOST);

                    if (
                        !filter_var($api_url, FILTER_VALIDATE_URL) ||
                        $scheme !== 'https' ||
                        $host === ''
                    ) {
                        return new WP_Error(
                            'weselaja_invalid_api_url',
                            'WeselAja API URL must be a valid HTTPS base URL.'
                        );
                    }

                    $api_url = preg_replace(
                        '#/v[12](?:/payins)?$#i',
                        '',
                        $api_url
                    );

                    return rtrim((string) $api_url, '/');
                }

                private function extract_error_message(array $decoded): string
                {
                    foreach (['message', 'error', 'detail'] as $key) {
                        if (isset($decoded[$key]) && is_string($decoded[$key])) {
                            return sanitize_text_field($decoded[$key]);
                        }
                    }

                    if (isset($decoded['errors']) && is_array($decoded['errors'])) {
                        return sanitize_text_field(wp_json_encode($decoded['errors']));
                    }

                    return 'WeselAja payment request failed.';
                }

                private function log(string $level, string $message): void
                {
                    if (function_exists('wc_get_logger')) {
                        wc_get_logger()->log(
                            $level,
                            $message,
                            ['source' => 'weselaja']
                        );
                    }
                }
            }
            """,

            ["includes/class-wc-gateway-weselaja.php"] = """
            <?php

            if (!defined('ABSPATH')) {
                exit;
            }

            class WC_Gateway_WeselAja extends WC_Payment_Gateway
            {
                private $api_url;
                private $api_key;
                private $secret_key;
                private $webhook_secret_key;
                private $payment_method;
                private $payment_channel;
                private $callback_url;
                private $settlements_json;

                public function __construct()
                {
                    $this->id = 'weselaja';
                    $this->method_title = 'WeselAja';
                    $this->method_description = 'Accept Virtual Account payments through WeselAja Direct Payin.';
                    $this->title = 'WeselAja';
                    $this->description = 'Pay using a WeselAja Virtual Account.';
                    $this->has_fields = false;
                    $this->supports = ['products'];

                    $this->init_form_fields();
                    $this->init_settings();

                    $this->enabled = $this->get_option('enabled', 'no');
                    $this->title = $this->get_option('title', 'WeselAja');
                    $this->description = $this->get_option(
                        'description',
                        'Pay using a WeselAja Virtual Account.'
                    );
                    $this->api_url = trim($this->get_option(
                        'api_url',
                        'https://sandbox.checkout.weselaja.id'
                    ));
                    $this->api_key = trim($this->get_option('api_key', ''));
                    $this->secret_key = trim($this->get_option('secret_key', ''));
                    $this->webhook_secret_key = trim(
                        $this->get_option('webhook_secret_key', '')
                    );
                    $this->payment_method = $this->get_option(
                        'payment_method',
                        'VIRTUAL_ACCOUNT'
                    );
                    $this->payment_channel = $this->get_option(
                        'payment_channel',
                        'BCA.VA'
                    );
                    $this->callback_url = trim($this->get_option('callback_url', ''));
                    $this->settlements_json = trim(
                        $this->get_option('settlements_json', '')
                    );

                    add_action(
                        'woocommerce_update_options_payment_gateways_' . $this->id,
                        [$this, 'process_admin_options']
                    );

                    add_action(
                        'woocommerce_thankyou_' . $this->id,
                        [$this, 'thankyou_page']
                    );
                }

                public function init_form_fields()
                {
                    $this->form_fields = [
                        'enabled' => [
                            'title' => 'Enable/Disable',
                            'type' => 'checkbox',
                            'label' => 'Enable WeselAja',
                            'default' => 'no'
                        ],
                        'title' => [
                            'title' => 'Title',
                            'type' => 'text',
                            'default' => 'WeselAja'
                        ],
                        'description' => [
                            'title' => 'Description',
                            'type' => 'textarea',
                            'default' => 'Pay using a WeselAja Virtual Account.'
                        ],
                        'api_url' => [
                            'title' => 'API Base URL',
                            'type' => 'text',
                            'description' => 'Use the sandbox or production base URL. Do not append /v2/payins.',
                            'default' => 'https://sandbox.checkout.weselaja.id'
                        ],
                        'api_key' => [
                            'title' => 'API Key',
                            'type' => 'password',
                            'default' => ''
                        ],
                        'secret_key' => [
                            'title' => 'API Secret Key',
                            'type' => 'password',
                            'default' => ''
                        ],
                        'webhook_secret_key' => [
                            'title' => 'Webhook Signature Secret',
                            'type' => 'password',
                            'description' => 'Separate secret used to verify WeselAja payment callbacks.',
                            'default' => ''
                        ],
                        'payment_method' => [
                            'title' => 'Payment Method',
                            'type' => 'select',
                            'default' => 'VIRTUAL_ACCOUNT',
                            'options' => [
                                'VIRTUAL_ACCOUNT' => 'Virtual Account'
                            ]
                        ],
                        'payment_channel' => [
                            'title' => 'Payment Channel',
                            'type' => 'select',
                            'default' => 'BCA.VA',
                            'options' => [
                                'BRI.VA' => 'BRI Virtual Account',
                                'MDR.VA' => 'Mandiri Virtual Account',
                                'BNI.VA' => 'BNI Virtual Account',
                                'PTB.VA' => 'Permata Virtual Account',
                                'CIMB.VA' => 'CIMB Virtual Account',
                                'BCA.VA' => 'BCA Virtual Account'
                            ]
                        ],
                        'settlements_json' => [
                            'title' => 'Settlements JSON',
                            'type' => 'textarea',
                            'description' => 'Optional. Required by WeselAja merchants configured with multiple settlement accounts and no default settlement. Paste the exact settlements JSON structure supplied for your merchant account.',
                            'default' => ''
                        ],
                        'callback_url' => [
                            'title' => 'Callback URL Override',
                            'type' => 'text',
                            'description' => 'Optional public HTTPS callback URL. Leave blank to use the default WooCommerce callback route.',
                            'default' => ''
                        ]
                    ];
                }

                public function is_available()
                {
                    return parent::is_available() &&
                        $this->api_url !== '' &&
                        $this->api_key !== '' &&
                        $this->secret_key !== '' &&
                        $this->webhook_secret_key !== '';
                }

                public function process_payment($order_id)
                {
                    $order = wc_get_order($order_id);

                    if (!$order) {
                        wc_add_notice('Unable to load the WooCommerce order.', 'error');
                        return ['result' => 'failure'];
                    }

                    $previous_status = strtoupper((string) $order->get_meta('_weselaja_status'));
                    $previous_payment_id = (string) $order->get_meta('_weselaja_payment_id');

                    if (
                        $previous_payment_id !== '' &&
                        !in_array($previous_status, ['FAILED', 'EXPIRED'], true)
                    ) {
                        return [
                            'result' => 'success',
                            'redirect' => $this->get_return_url($order)
                        ];
                    }

                    $reference_code = (string) $order->get_meta('_weselaja_reference_code');

                    if (
                        $reference_code === '' ||
                        in_array($previous_status, ['FAILED', 'EXPIRED'], true)
                    ) {
                        $suffix = strtoupper(substr(
                            str_replace('-', '', wp_generate_uuid4()),
                            0,
                            8
                        ));

                        $reference_code = sprintf('WC-%d-%s', $order_id, $suffix);

                        $order->update_meta_data('_weselaja_reference_code', $reference_code);
                        $order->delete_meta_data('_weselaja_payment_id');
                        $order->delete_meta_data('_weselaja_payment_code');
                        $order->delete_meta_data('_weselaja_payment_url');
                        $order->delete_meta_data('_weselaja_status');
                        $order->delete_meta_data('_weselaja_idempotency_key');
                    }

                    $idempotency_key = (string) $order->get_meta('_weselaja_idempotency_key');

                    if ($idempotency_key === '') {
                        $idempotency_key = 'woocommerce-' . str_replace('-', '', wp_generate_uuid4());
                        $order->update_meta_data('_weselaja_idempotency_key', $idempotency_key);
                    }

                    $currency = strtoupper($order->get_currency());
                    $order_total = (float) $order->get_total();

                    if ($order_total <= 0) {
                        wc_add_notice('Payment amount must be greater than zero.', 'error');
                        return ['result' => 'failure'];
                    }

                    $amount = $currency === 'IDR'
                        ? (int) round($order_total)
                        : (float) wc_format_decimal($order_total, wc_get_price_decimals());

                    $customer_id = (int) $order->get_customer_id();
                    $customer_reference = $customer_id > 0
                        ? 'WC-CUST-' . $customer_id
                        : 'WC-GUEST-' . $order_id;

                    $customer_name = trim(
                        $order->get_billing_first_name() . ' ' .
                        $order->get_billing_last_name()
                    );

                    if ($customer_name === '') {
                        $customer_name = 'Customer ' . $order_id;
                    }

                    $callback_url = $this->callback_url !== ''
                        ? esc_url_raw($this->callback_url)
                        : home_url('/wc-api/wc_xenith_callback');

                    $payload = [
                        'initiatedAmount' => $amount,
                        'currency' => $currency,
                        'paymentMethod' => $this->payment_method,
                        'paymentChannel' => $this->payment_channel,
                        'payinFeeBearer' => 'MERCHANT',
                        'referenceCode' => $reference_code,
                        'customerReference' => $customer_reference,
                        'customerName' => $customer_name,
                        'callbackUrl' => $callback_url,
                        'redirectUrl' => $this->get_return_url($order)
                    ];

                    $phone = trim((string) $order->get_billing_phone());
                    if ($phone !== '') {
                        $payload['customerPhoneNumber'] = $phone;
                    }

                    if ($this->settlements_json !== '') {
                        $settlements = json_decode($this->settlements_json, true);

                        if (json_last_error() !== JSON_ERROR_NONE || !is_array($settlements) || $settlements === []) {
                            wc_add_notice(
                                'Invalid Settlements JSON. Paste the exact JSON structure configured for your WeselAja merchant account.',
                                'error'
                            );
                            return ['result' => 'failure'];
                        }

                        $payload['settlements'] = $settlements;
                    }

                    $order->update_meta_data('_weselaja_initiated_amount', (string) $amount);
                    $order->update_meta_data('_weselaja_currency', $currency);
                    $order->update_meta_data('_weselaja_payment_channel', $this->payment_channel);
                    $order->save();

                    $api = new WC_WeselAja_API();
                    $result = $api->create_payin(
                        $this->api_url,
                        $this->api_key,
                        $this->secret_key,
                        $payload,
                        $idempotency_key
                    );

                    if (empty($result['success'])) {
                        $message = sanitize_text_field((string) (
                            $result['message'] ?? 'WeselAja payment creation failed.'
                        ));

                        $order->add_order_note('WeselAja payment creation failed: ' . $message);
                        wc_add_notice('Payment error: ' . $message, 'error');
                        return ['result' => 'failure'];
                    }

                    $data = $result['data'];
                    $payment_id = '';

                    foreach (['id', 'paymentId', 'payinId', 'payInId'] as $key) {
                        if (isset($data[$key]) && is_scalar($data[$key]) && (string) $data[$key] !== '') {
                            $payment_id = sanitize_text_field((string) $data[$key]);
                            break;
                        }
                    }

                    if ($payment_id === '') {
                        $order->add_order_note('WeselAja response did not include a Payin ID.');
                        wc_add_notice('WeselAja response did not include a payment ID.', 'error');
                        return ['result' => 'failure'];
                    }

                    $payment_code = '';
                    foreach (['paymentCode', 'payment_code', 'virtualAccountNumber'] as $key) {
                        if (isset($data[$key]) && is_scalar($data[$key]) && (string) $data[$key] !== '') {
                            $payment_code = sanitize_text_field((string) $data[$key]);
                            break;
                        }
                    }

                    $payment_url = '';
                    foreach (['paymentUrl', 'paymentLinkUrl'] as $key) {
                        if (isset($data[$key]) && is_string($data[$key]) && $data[$key] !== '') {
                            $candidate = esc_url_raw($data[$key]);
                            if (wp_http_validate_url($candidate) && strtolower((string) wp_parse_url($candidate, PHP_URL_SCHEME)) === 'https') {
                                $payment_url = $candidate;
                                break;
                            }
                        }
                    }

                    $status = strtoupper(sanitize_text_field((string) ($data['status'] ?? 'PENDING')));
                    $returned_reference = (string) ($data['referenceCode'] ?? $reference_code);

                    if ($returned_reference !== $reference_code) {
                        $order->add_order_note('WeselAja response referenceCode did not match the WooCommerce order.');
                        wc_add_notice('WeselAja returned a different payment reference.', 'error');
                        return ['result' => 'failure'];
                    }

                    $returned_channel = sanitize_text_field((string) (
                        $data['paymentChannel'] ?? $this->payment_channel
                    ));

                    $order->update_meta_data('_weselaja_payment_id', $payment_id);
                    $order->update_meta_data('_weselaja_payment_code', $payment_code);
                    $order->update_meta_data('_weselaja_payment_url', $payment_url);
                    $order->update_meta_data('_weselaja_payment_channel', $returned_channel);
                    $order->update_meta_data('_weselaja_status', $status);

                    $order->add_order_note(sprintf(
                        'WeselAja Direct Payin created. Reference: %s. Payin ID: %s. Channel: %s. Status: %s.',
                        $reference_code,
                        $payment_id,
                        $returned_channel,
                        $status
                    ));

                    if ($status === 'SUCCESS') {
                        $order->payment_complete($payment_id);
                    } elseif ($status === 'FAILED') {
                        $order->update_status('failed', 'WeselAja payment failed.');
                    } elseif ($status === 'EXPIRED') {
                        $order->update_status('cancelled', 'WeselAja payment expired.');
                    } else {
                        $order->update_status('on-hold', 'Awaiting verified WeselAja payment confirmation.');
                    }

                    $order->save();

                    return [
                        'result' => 'success',
                        'redirect' => $this->get_return_url($order)
                    ];
                }

                public function thankyou_page($order_id): void
                {
                    $order = wc_get_order($order_id);

                    if (
                        !$order ||
                        $order->get_payment_method() !== $this->id ||
                        $order->is_paid()
                    ) {
                        return;
                    }

                    $payment_code = (string) $order->get_meta('_weselaja_payment_code');
                    $payment_url = (string) $order->get_meta('_weselaja_payment_url');
                    $channel = (string) $order->get_meta('_weselaja_payment_channel');
                    $reference = (string) $order->get_meta('_weselaja_reference_code');
                    $status = (string) $order->get_meta('_weselaja_status');

                    echo '<section class="woocommerce-order-details weselaja-payment-instructions">';
                    echo '<h2>' . esc_html__('WeselAja payment instructions', 'weselaja') . '</h2>';

                    if ($channel !== '') {
                        echo '<p>' . esc_html__('Payment channel:', 'weselaja') . ' ' . esc_html($channel) . '</p>';
                    }

                    if ($reference !== '') {
                        echo '<p>' . esc_html__('Reference:', 'weselaja') . ' ' . esc_html($reference) . '</p>';
                    }

                    if ($status !== '') {
                        echo '<p>' . esc_html__('Status:', 'weselaja') . ' ' . esc_html($status) . '</p>';
                    }

                    if ($payment_code !== '') {
                        echo '<p>' . esc_html__('Virtual Account / Payment Code:', 'weselaja') . '</p>';
                        echo '<p><strong><code>' . esc_html($payment_code) . '</code></strong></p>';
                    }

                    if ($payment_url !== '') {
                        echo '<p><a href="' . esc_url($payment_url) . '" target="_blank" rel="noopener noreferrer">';
                        echo esc_html__('Open payment page', 'weselaja');
                        echo '</a></p>';
                    }

                    echo '<p>' . esc_html__(
                        'The order will be updated after a verified WeselAja payment callback is received.',
                        'weselaja'
                    ) . '</p>';
                    echo '</section>';
                }
            }
            """,

            ["includes/class-wc-weselaja-webhook.php"] = """
            <?php

            if (!defined('ABSPATH')) {
                exit;
            }

            final class WC_WeselAja_Webhook
            {
                public static function handle(): void
                {
                    if (strtoupper((string) ($_SERVER['REQUEST_METHOD'] ?? '')) !== 'POST') {
                        self::respond(405, 'Method not allowed');
                    }

                    $settings = get_option('woocommerce_weselaja_settings', []);
                    $secret = trim((string) ($settings['webhook_secret_key'] ?? ''));

                    if ($secret === '') {
                        self::log('error', 'Webhook rejected because the webhook secret is not configured.');
                        self::respond(503, 'Webhook is not configured');
                    }

                    $header_pair = self::get_signature_headers();
                    $timestamp = $header_pair['timestamp'];
                    $signature = $header_pair['signature'];

                    if ($timestamp === '' || $signature === '') {
                        self::respond(400, 'Missing webhook signature headers');
                    }

                    $raw_body = file_get_contents('php://input');
                    if (!is_string($raw_body) || $raw_body === '') {
                        self::respond(400, 'Missing webhook body');
                    }

                    $timestamp_ms = self::parse_timestamp_ms($timestamp);
                    if ($timestamp_ms === null) {
                        self::respond(400, 'Invalid webhook timestamp');
                    }

                    $now_ms = (int) round(microtime(true) * 1000);
                    if (abs($now_ms - $timestamp_ms) > 300000) {
                        self::respond(401, 'Webhook timestamp outside accepted window');
                    }

                    $request_uri = (string) ($_SERVER['REQUEST_URI'] ?? '/');
                    $webhook_path = (string) (parse_url($request_uri, PHP_URL_PATH) ?: '/');

                    $signed_payload =
                        'POST' . '\\n' .
                        $webhook_path . '\\n' .
                        $raw_body . '\\n' .
                        $timestamp;

                    $expected_signature = base64_encode(
                        hash_hmac('sha256', $signed_payload, $secret, true)
                    );

                    if (!hash_equals($expected_signature, trim($signature))) {
                        self::log('error', 'Rejected WeselAja callback with an invalid signature.');
                        self::respond(401, 'Invalid webhook signature');
                    }

                    $payload = json_decode($raw_body, true);
                    if (!is_array($payload)) {
                        self::respond(400, 'Invalid webhook JSON');
                    }

                    $data = isset($payload['data']) && is_array($payload['data'])
                        ? $payload['data']
                        : $payload;

                    $reference_code = self::first_string($data, ['referenceCode', 'reference_code']);
                    $payment_id = self::first_string($data, ['id', 'payinId', 'payInId', 'paymentId']);
                    $callback_status = strtoupper(self::first_string(
                        $data,
                        ['status', 'paymentStatus', 'payment_status']
                    ));

                    if ($reference_code === '' || $payment_id === '' || $callback_status === '') {
                        self::respond(400, 'Callback is missing payment identity or status');
                    }

                    if (!preg_match('/^WC-(\d+)-([A-F0-9]{8})$/i', $reference_code, $matches)) {
                        self::respond(400, 'Invalid WooCommerce payment reference');
                    }

                    $order_id = (int) $matches[1];
                    $order = wc_get_order($order_id);

                    if (!$order) {
                        self::log('warning', 'Callback references an unknown WooCommerce order.');
                        self::respond(202, 'Order not found; callback acknowledged');
                    }

                    if ((string) $order->get_meta('_weselaja_reference_code') !== $reference_code) {
                        self::respond(400, 'Payment reference mismatch');
                    }

                    if ((string) $order->get_meta('_weselaja_payment_id') !== $payment_id) {
                        self::respond(400, 'Payin ID mismatch');
                    }

                    $api_url = trim((string) ($settings['api_url'] ?? ''));
                    $api_key = trim((string) ($settings['api_key'] ?? ''));
                    $api_secret = trim((string) ($settings['secret_key'] ?? ''));

                    if ($api_url === '' || $api_key === '' || $api_secret === '') {
                        self::log('error', 'Cannot verify callback because API credentials are incomplete.');
                        self::respond(503, 'Payment status verification is not configured');
                    }

                    $api = new WC_WeselAja_API();
                    $verification = $api->get_payin(
                        $api_url,
                        $api_key,
                        $api_secret,
                        $payment_id
                    );

                    if (empty($verification['success']) || !isset($verification['data']) || !is_array($verification['data'])) {
                        self::log(
                            'error',
                            'Could not verify WeselAja payment status: ' .
                            sanitize_text_field((string) ($verification['message'] ?? 'Unknown API error'))
                        );
                        self::respond(503, 'Could not verify payment status');
                    }

                    $payin = $verification['data'];
                    $verified_id = self::first_string($payin, ['id', 'paymentId', 'payinId', 'payInId']);
                    $verified_reference = self::first_string($payin, ['referenceCode', 'reference_code']);
                    $verified_status = strtoupper(self::first_string(
                        $payin,
                        ['status', 'paymentStatus', 'payment_status']
                    ));

                    if ($verified_id !== '' && $verified_id !== $payment_id) {
                        self::respond(400, 'Verified Payin ID mismatch');
                    }

                    if ($verified_reference !== '' && $verified_reference !== $reference_code) {
                        self::respond(400, 'Verified payment reference mismatch');
                    }

                    if ($verified_status === '') {
                        self::respond(502, 'WeselAja status response did not include a status');
                    }

                    $expected_currency = strtoupper((string) $order->get_meta('_weselaja_currency'));
                    $verified_currency = strtoupper(self::first_string($payin, ['currency', 'currencyCode']));
                    $callback_currency = strtoupper(self::first_string($data, ['currency', 'currencyCode']));
                    $actual_currency = $verified_currency !== '' ? $verified_currency : $callback_currency;

                    if ($actual_currency !== '' && $expected_currency !== '' && $actual_currency !== $expected_currency) {
                        self::respond(400, 'Payment currency mismatch');
                    }

                    $expected_amount = (string) $order->get_meta('_weselaja_initiated_amount');
                    $verified_amount = self::first_scalar_string($payin, ['initiatedAmount']);
                    $callback_amount = self::first_scalar_string($data, ['initiatedAmount']);
                    $actual_amount = $verified_amount !== '' ? $verified_amount : $callback_amount;

                    if ($actual_amount !== '' && $expected_amount !== '') {
                        $precision = ($expected_currency === 'IDR') ? 0 : 2;
                        if (self::normalize_amount($actual_amount, $precision) !== self::normalize_amount($expected_amount, $precision)) {
                            self::respond(400, 'Payment initiated amount mismatch');
                        }
                    }

                    if ($order->is_paid()) {
                        self::respond(200, 'Payment already completed');
                    }

                    $order->update_meta_data('_weselaja_status', $verified_status);
                    $order->save();

                    if ($verified_status === 'SUCCESS') {
                        $order->payment_complete($payment_id);
                        $order->add_order_note('WeselAja payment verified successfully through the V2 Payin status API.');
                    } elseif ($verified_status === 'FAILED') {
                        $order->update_status('failed', 'WeselAja payment failed.');
                    } elseif ($verified_status === 'EXPIRED') {
                        $order->update_status('cancelled', 'WeselAja payment expired.');
                    } else {
                        $order->add_order_note(
                            sprintf('WeselAja callback received. Verified status: %s.', $verified_status)
                        );
                    }

                    $order->save();
                    self::respond(200, 'OK');
                }

                private static function parse_timestamp_ms(string $timestamp): ?int
                {
                    $timestamp = trim($timestamp);

                    if (preg_match('/^\d{10}$/', $timestamp)) {
                        return ((int) $timestamp) * 1000;
                    }

                    if (preg_match('/^\d{13}$/', $timestamp)) {
                        return (int) $timestamp;
                    }

                    try {
                        $date = new DateTimeImmutable($timestamp);
                        return ((int) $date->format('U')) * 1000 +
                            (int) substr($date->format('u'), 0, 3);
                    } catch (Exception $exception) {
                        return null;
                    }
                }

                private static function get_signature_headers(): array
                {
                    $pairs = [
                        [
                            'timestamp' => (string) ($_SERVER['HTTP_X_WEBHOOK_TIMESTAMP'] ?? ''),
                            'signature' => (string) ($_SERVER['HTTP_X_WEBHOOK_SIGNATURE'] ?? '')
                        ],
                        [
                            'timestamp' => (string) ($_SERVER['HTTP_X_MIDDLEWARE_TIMESTAMP'] ?? ''),
                            'signature' => (string) ($_SERVER['HTTP_X_MIDDLEWARE_SIGNATURE'] ?? '')
                        ]
                    ];

                    foreach ($pairs as $pair) {
                        if ($pair['timestamp'] !== '' && $pair['signature'] !== '') {
                            return $pair;
                        }
                    }

                    return ['timestamp' => '', 'signature' => ''];
                }

                private static function first_string(array $source, array $keys): string
                {
                    foreach ($keys as $key) {
                        if (isset($source[$key]) && is_scalar($source[$key])) {
                            $value = trim((string) $source[$key]);
                            if ($value !== '') {
                                return sanitize_text_field($value);
                            }
                        }
                    }

                    return '';
                }

                private static function first_scalar_string(array $source, array $keys): string
                {
                    foreach ($keys as $key) {
                        if (isset($source[$key]) && is_scalar($source[$key])) {
                            $value = trim((string) $source[$key]);
                            if ($value !== '') {
                                return $value;
                            }
                        }
                    }

                    return '';
                }

                private static function normalize_amount(string $amount, int $precision): string
                {
                    if (!is_numeric($amount)) {
                        return '';
                    }

                    return number_format((float) $amount, $precision, '.', '');
                }

                private static function respond(int $status_code, string $message): void
                {
                    status_header($status_code);
                    header('Content-Type: text/plain; charset=utf-8');
                    echo $message;
                    exit;
                }

                private static function log(string $level, string $message): void
                {
                    if (function_exists('wc_get_logger')) {
                        wc_get_logger()->log(
                            $level,
                            $message,
                            ['source' => 'weselaja-webhook']
                        );
                    }
                }
            }
            """,

            ["includes/class-wc-weselaja-blocks-support.php"] = """
            <?php

            use Automattic\WooCommerce\Blocks\Payments\Integrations\AbstractPaymentMethodType;

            if (!defined('ABSPATH')) {
                exit;
            }

            final class WC_WeselAja_Blocks_Support extends AbstractPaymentMethodType
            {
                protected $name = 'weselaja';

                public function initialize()
                {
                    $this->settings = get_option(
                        'woocommerce_' . $this->name . '_settings',
                        []
                    );
                }

                public function is_active()
                {
                    return isset($this->settings['enabled']) &&
                        $this->settings['enabled'] === 'yes';
                }

                public function get_payment_method_script_handles()
                {
                    $script_path = WESELAJA_WC_FILE;
                    $script_url = plugins_url(
                        'assets/js/weselaja-blocks.js',
                        $script_path
                    );

                    wp_register_script(
                        'wc-weselaja-blocks-integration',
                        $script_url,
                        [
                            'wc-blocks-registry',
                            'wc-settings',
                            'wp-element',
                            'wp-html-entities',
                            'wp-i18n'
                        ],
                        WESELAJA_WC_VERSION,
                        true
                    );

                    return ['wc-weselaja-blocks-integration'];
                }

                public function get_payment_method_data()
                {
                    return [
                        'title' => $this->get_setting('title') ?: 'WeselAja',
                        'description' => $this->get_setting('description') ?: '',
                        'supports' => ['products']
                    ];
                }
            }
            """,

            ["assets/js/weselaja-blocks.js"] = """
            (function (wp, wc) {
                if (!wp || !wc || !wc.wcBlocksRegistry || !wc.wcSettings) {
                    return;
                }

                var registerPaymentMethod = wc.wcBlocksRegistry.registerPaymentMethod;
                var getSetting = wc.wcSettings.getSetting;
                var createElement = wp.element.createElement;
                var decodeEntities = wp.htmlEntities.decodeEntities;

                var settings = getSetting('weselaja_data', {});
                var label = decodeEntities(settings.title || 'WeselAja');

                var Content = function () {
                    return decodeEntities(settings.description || '');
                };

                registerPaymentMethod({
                    name: 'weselaja',
                    label: createElement('span', {}, label),
                    content: createElement(Content),
                    edit: createElement(Content),
                    canMakePayment: function () {
                        return true;
                    },
                    ariaLabel: label,
                    supports: {
                        features: settings.supports || ['products']
                    }
                });
            })(window.wp, window.wc);
            """,

            ["README.md"] = """
            # WooCommerce WeselAja

            WooCommerce payment gateway for WeselAja Direct Payin Virtual Account.

            ## Requirements

            - WordPress 6.0 or newer
            - WooCommerce
            - PHP 7.4 or newer
            - WeselAja API key and API secret
            - The separate webhook signature secret configured for the merchant

            ## Installation

            1. Copy this plugin directory into `wp-content/plugins/`.
            2. Activate WooCommerce and WooCommerce WeselAja.
            3. Open **WooCommerce → Settings → Payments → WeselAja**.
            4. Configure the API URL, API Key, API Secret Key, webhook signature secret, payment channel and fee bearer.
            5. Configure the Settlements JSON when the merchant has multiple settlement accounts and no default settlement account.
            6. Enable the gateway and save the settings.

            ## Direct Payin request

            The plugin creates Virtual Account payments with:

            - `POST /v2/payins`
            - `X-API-Key`
            - `X-Timestamp` (UTC ISO-8601 timestamp)
            - `X-Signature` (Base64 HMAC-SHA256)
            - `X-Idempotency-Key`

            The request signature is calculated from the uppercase HTTP method, URI path, timestamp and raw JSON body, separated by actual newline characters:

            ```text
            POST\n/v2/payins\n{timestamp}\n{raw-json-body}
            ```

            The plugin sends `paymentMethod`, `paymentChannel`, `payinFeeBearer`, `initiatedAmount`, `currency`, `referenceCode`, `customerReference`, `customerName`, `callbackUrl` and `redirectUrl`. Customer phone and `settlements` are included when configured/available.

            ## Settlements

            Some merchants with multiple settlement accounts and no default settlement account must include `settlements` in the Direct Payin request. In that case, paste the exact JSON value and structure configured for your WeselAja merchant account into **Settlements JSON**. The plugin validates that the setting is JSON and passes it through as the `settlements` request property. The exact structure must come from the merchant's WeselAja configuration/documentation; this plugin does not invent settlement account identifiers.

            ## Callback

            The default callback URL is:

            ```text
            https://your-public-site.example/wc-api/wc_xenith_callback
            ```

            The callback is registered through WooCommerce's `woocommerce_api_wc_xenith_callback` hook. A local WordPress site is generally not reachable by the payment provider; use a public HTTPS tunnel for sandbox callback testing and set **Callback URL Override** when needed.

            The callback handler:

            1. Reads the raw request body.
            2. Accepts the documented `x-webhook-timestamp` / `x-webhook-signature` headers and the legacy `x-middleware-timestamp` / `x-middleware-signature` pair.
            3. Verifies the Base64 HMAC-SHA256 signature using the separate webhook secret. The callback canonical payload uses literal `\\n` separators: `POST\\n{webhookPath}\\n{rawBody}\\n{timestamp}`.
            4. Matches the callback to the WooCommerce order using the generated reference and Payin ID.
            5. Calls the authenticated `GET /v2/payins/{id}` endpoint and uses the returned Payin status as the authoritative status before updating the order.
            6. Marks an order paid only when the verified Payin status is `SUCCESS`.

            The API Secret Key and webhook signature secret serve different purposes. Keep both private and configure the correct value for each setting.

            ## Sandbox testing

            1. Set API Base URL to `https://sandbox.checkout.weselaja.id`.
            2. Enter sandbox API credentials and webhook signature secret.
            3. Select the Virtual Account channel.
            4. Configure Settlements JSON if required by the merchant's sandbox settlement configuration.
            5. Ensure the callback URL is publicly reachable over HTTPS.
            6. Place a test order and verify the Payin creation response and callback in WooCommerce logs (**WooCommerce → Status → Logs**, source `weselaja` or `weselaja-webhook`).

            Do not mark an order paid from the initial `PENDING` response. The order is updated after the signed callback and authenticated Payin status lookup.
            """,

            ["examples/payin_payload.json"] = """
            {
              "initiatedAmount": 10000,
              "currency": "IDR",
              "paymentMethod": "VIRTUAL_ACCOUNT",
              "paymentChannel": "BCA.VA",
              "payinFeeBearer": "MERCHANT",
              "referenceCode": "WC-123-AB12CD34",
              "customerReference": "WC-GUEST-123",
              "customerName": "Test Customer",
              "callbackUrl": "https://example.com/wc-api/wc_xenith_callback",
              "redirectUrl": "https://example.com/checkout/order-received/123/"
            }
            """,

            ["tests/test-weselaja.php"] = """
            <?php

            if (!defined('ABSPATH')) {
                exit;
            }

            if (class_exists('WP_UnitTestCase')) {
                class WC_WeselAja_Plugin_Smoke_Test extends WP_UnitTestCase
                {
                    public function test_api_client_class_is_available(): void
                    {
                        $this->assertTrue(class_exists('WC_WeselAja_API'));
                    }

                    public function test_gateway_class_is_available(): void
                    {
                        $this->assertTrue(class_exists('WC_Gateway_WeselAja'));
                    }

                    public function test_webhook_handler_class_is_available(): void
                    {
                        $this->assertTrue(class_exists('WC_WeselAja_Webhook'));
                    }

                    public function test_blocks_support_class_is_available(): void
                    {
                        if (class_exists('Automattic\\WooCommerce\\Blocks\\Payments\\Integrations\\AbstractPaymentMethodType')) {
                            $this->assertTrue(class_exists('WC_WeselAja_Blocks_Support'));
                        } else {
                            $this->assertTrue(true);
                        }
                    }
                }
            }
            """
        };

    public GenerationResult Generate(
        string projectName,
        bool includeExample,
        bool includeTests,
        bool overwrite)
    {
        ValidateTemplates();

        var safeProjectName = SanitizeProjectName(projectName);
        var configuredOutputRoot = GeneratorOutputScope.GetConfiguredRoot();

        var outputRoot = string.IsNullOrWhiteSpace(configuredOutputRoot)
            ? Path.Combine(Directory.GetCurrentDirectory(), "generated")
            : configuredOutputRoot.Trim();

        outputRoot = Path.GetFullPath(outputRoot);

        var wooCommerceRoot = Path.GetFullPath(
            Path.Combine(outputRoot, "woocommerce"));
        EnsureInsideDirectory(wooCommerceRoot, outputRoot);

        var outputDirectory = Path.GetFullPath(
            Path.Combine(wooCommerceRoot, safeProjectName));
        EnsureInsideDirectory(outputDirectory, wooCommerceRoot);

        if (Directory.Exists(outputDirectory))
        {
            if (!overwrite)
            {
                throw new InvalidOperationException(
                    $"Generated project already exists: {outputDirectory}. " +
                    "Set overwrite=true to replace it.");
            }

            Directory.Delete(outputDirectory, recursive: true);
        }

        Directory.CreateDirectory(outputDirectory);

        var filesToGenerate = new List<string>(RequiredPluginFiles);

        if (includeExample)
        {
            filesToGenerate.Add("examples/payin_payload.json");
        }

        if (includeTests)
        {
            filesToGenerate.Add("tests/test-weselaja.php");
        }

        var generatedFiles = new List<GeneratedFile>();
        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        foreach (var relativePath in filesToGenerate)
        {
            if (!TemplateFiles.TryGetValue(relativePath, out var contents))
            {
                throw new InvalidOperationException(
                    $"Embedded WooCommerce template is missing: {relativePath}");
            }

            var outputPath = Path.GetFullPath(
                Path.Combine(outputDirectory, relativePath));
            EnsureInsideDirectory(outputPath, outputDirectory);

            var outputFileDirectory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrWhiteSpace(outputFileDirectory))
            {
                Directory.CreateDirectory(outputFileDirectory);
            }

            File.WriteAllText(outputPath, contents, encoding);

            generatedFiles.Add(new GeneratedFile
            {
                Path = Path.GetRelativePath(outputDirectory, outputPath),
                Bytes = new FileInfo(outputPath).Length
            });
        }

        return new GenerationResult(
            safeProjectName,
            outputDirectory,
            generatedFiles);
    }

    private static void ValidateTemplates()
    {
        foreach (var relativePath in RequiredPluginFiles)
        {
            if (!TemplateFiles.ContainsKey(relativePath))
            {
                throw new InvalidOperationException(
                    $"Required embedded WooCommerce template is missing: {relativePath}");
            }
        }

        if (!TemplateFiles.ContainsKey("examples/payin_payload.json"))
        {
            throw new InvalidOperationException(
                "Embedded WooCommerce example payload is missing.");
        }

        if (!TemplateFiles.ContainsKey("tests/test-weselaja.php"))
        {
            throw new InvalidOperationException(
                "Embedded WooCommerce test template is missing.");
        }
    }

    private static string SanitizeProjectName(string? projectName)
    {
        var value = string.IsNullOrWhiteSpace(projectName)
            ? "weselaja-woocommerce"
            : projectName.Trim().ToLowerInvariant();

        var builder = new StringBuilder();
        var dashPending = false;

        foreach (var character in value)
        {
            if (char.IsAsciiLetterOrDigit(character))
            {
                if (dashPending && builder.Length > 0)
                {
                    builder.Append('-');
                }

                builder.Append(character);
                dashPending = false;
            }
            else if (builder.Length > 0)
            {
                dashPending = true;
            }
        }

        var result = builder.ToString().Trim('-');
        if (string.IsNullOrWhiteSpace(result))
        {
            result = "weselaja-woocommerce";
        }

        if (result.Length > 64)
        {
            result = result[..64].Trim('-');
        }

        return string.IsNullOrWhiteSpace(result)
            ? "weselaja-woocommerce"
            : result;
    }

    private static void EnsureInsideDirectory(
        string path,
        string parentDirectory)
    {
        var fullPath = Path.GetFullPath(path);
        var fullParent = Path.GetFullPath(parentDirectory).TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(
            fullParent,
            OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "A generated path resolved outside its expected directory.");
        }
    }
}
