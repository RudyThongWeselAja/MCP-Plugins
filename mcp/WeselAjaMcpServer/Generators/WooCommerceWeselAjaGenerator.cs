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
        "README.md"
    ];

    private static readonly Dictionary<string, string> TemplateFiles =
        new(StringComparer.Ordinal)
        {
            ["weselaja-woocommerce.php"] = """
                <?php
                /**
                 * Plugin Name: WooCommerce WeselAja
                 * Description: Payment gateway integration for WeselAja.
                 * Version: 1.0.0
                 * Requires PHP: 7.4
                 * License: GPL-3.0-or-later
                 */

                if (!defined('ABSPATH')) {
                    exit;
                }

                define('WESELAJA_WC_FILE', __FILE__);
                define('WESELAJA_WC_VERSION', '1.0.0');

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
                    require_once __DIR__ . '/includes/class-wc-gateway-weselaja.php';
                    require_once __DIR__ . '/includes/class-wc-weselaja-webhook.php';

                    add_filter(
                        'woocommerce_payment_gateways',
                        function (array $methods): array {
                            $methods[] = 'WC_Gateway_WeselAja';
                            return $methods;
                        }
                    );

                    add_action(
                        'woocommerce_api_weselaja_callback',
                        ['WC_WeselAja_Webhook', 'handle']
                    );
                }

                add_action('woocommerce_blocks_loaded', function (): void {
                    if (
                        !class_exists(
                            'Automattic\WooCommerce\Blocks\Payments\Integrations\AbstractPaymentMethodType'
                        )
                    ) {
                        return;
                    }

                    require_once __DIR__ .
                        '/includes/class-wc-weselaja-blocks-support.php';

                    add_action(
                        'woocommerce_blocks_payment_method_type_registration',
                        function ($registry): void {
                            $registry->register(
                                new WC_WeselAja_Blocks_Support()
                            );
                        }
                    );
                });
                """,

            ["includes/class-wc-weselaja-api.php"] = """
                <?php

                if (!defined('ABSPATH')) {
                    exit;
                }

                final class WC_WeselAja_API
                {
                    private const PAYMENT_URI = '/v1/payins';

                    public function create_payin(
                        string $api_url,
                        string $api_key,
                        string $secret_key,
                        array $payload,
                        string $idempotency_key
                    ): array {
                        $api_url = rtrim(trim($api_url), '/');

                        $scheme = strtolower(
                            (string) wp_parse_url(
                                $api_url,
                                PHP_URL_SCHEME
                            )
                        );

                        if (
                            !filter_var($api_url, FILTER_VALIDATE_URL) ||
                            !in_array($scheme, ['http', 'https'], true)
                        ) {
                            return [
                                'success' => false,
                                'message' => 'Invalid WeselAja API base URL.'
                            ];
                        }

                        if ($api_key === '' || $secret_key === '') {
                            return [
                                'success' => false,
                                'message' => 'API credentials are required.'
                            ];
                        }

                        if ($idempotency_key === '') {
                            return [
                                'success' => false,
                                'message' => 'Idempotency key is required.'
                            ];
                        }

                        $body = wp_json_encode(
                            $payload,
                            JSON_UNESCAPED_SLASHES |
                            JSON_UNESCAPED_UNICODE
                        );

                        if (!is_string($body)) {
                            return [
                                'success' => false,
                                'message' => 'Unable to encode payment request.'
                            ];
                        }

                        $timestamp = (
                            new DateTimeImmutable(
                                'now',
                                new DateTimeZone('UTC')
                            )
                        )->format('Y-m-d\TH:i:s.v\Z');

                        $string_to_sign =
                            'POST' . "\n" .
                            self::PAYMENT_URI . "\n" .
                            $timestamp . "\n" .
                            $body;

                        $signature = base64_encode(
                            hash_hmac(
                                'sha256',
                                $string_to_sign,
                                $secret_key,
                                true
                            )
                        );

                        $response = wp_remote_post(
                            $api_url . self::PAYMENT_URI,
                            [
                                'method' => 'POST',
                                'timeout' => 30,
                                'redirection' => 0,
                                'sslverify' => true,
                                'headers' => [
                                    'Content-Type' => 'application/json',
                                    'Accept' => 'application/json',
                                    'User-Agent' => 'Python-urllib/3.13',
                                    'Accept-Encoding' => 'identity',
                                    'Connection' => 'close',
                                    'Expect' => '',
                                    'Xenith-Api-Key' => $api_key,
                                    'Xenith-Request-Timestamp' => $timestamp,
                                    'Xenith-Request-Signature' => $signature,
                                    'X-Idempotency-Key' => $idempotency_key
                                ],
                                'body' => $body
                            ]
                        );

                        if (is_wp_error($response)) {
                            $this->log(
                                'error',
                                'WeselAja request failed: ' .
                                $response->get_error_message()
                            );

                            return [
                                'success' => false,
                                'message' => $response->get_error_message()
                            ];
                        }

                        $http_status = (int) wp_remote_retrieve_response_code(
                            $response
                        );

                        $response_body = (string) wp_remote_retrieve_body(
                            $response
                        );

                        $decoded = json_decode($response_body, true);

                        if (!is_array($decoded)) {
                            return [
                                'success' => false,
                                'message' => sprintf(
                                    'Invalid JSON response. HTTP %d.',
                                    $http_status
                                ),
                                'http_status' => $http_status
                            ];
                        }

                        $message = 'WeselAja payment request failed.';

                        if (
                            isset($decoded['message']) &&
                            is_string($decoded['message'])
                        ) {
                            $message = $decoded['message'];
                        } elseif (
                            isset($decoded['error']) &&
                            is_string($decoded['error'])
                        ) {
                            $message = $decoded['error'];
                        }

                        if (
                            $http_status < 200 ||
                            $http_status >= 300 ||
                            (
                                array_key_exists('success', $decoded) &&
                                $decoded['success'] === false
                            )
                        ) {
                            $this->log(
                                'error',
                                sprintf(
                                    'WeselAja returned HTTP %d: %s',
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
                        )
                            ? $decoded['data']
                            : $decoded;

                        return [
                            'success' => true,
                            'data' => $data,
                            'http_status' => $http_status
                        ];
                    }

                    private function log(
                        string $level,
                        string $message
                    ): void {
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
                    private string $api_url;
                    private string $api_key;
                    private string $secret_key;
                    private string $webhook_secret_key;
                    private string $payment_method;
                    private string $payment_channel;
                    private string $callback_url;

                    public function __construct()
                    {
                        $this->id = 'weselaja';
                        $this->method_title = 'WeselAja';
                        $this->method_description =
                            'Accept payments through WeselAja.';

                        $this->title = 'WeselAja';
                        $this->description = 'Pay using WeselAja.';
                        $this->has_fields = false;
                        $this->supports = ['products'];

                        $this->init_form_fields();
                        $this->init_settings();

                        $this->enabled = $this->get_option(
                            'enabled',
                            'no'
                        );

                        $this->title = $this->get_option(
                            'title',
                            'WeselAja'
                        );

                        $this->description = $this->get_option(
                            'description',
                            'Pay using WeselAja.'
                        );

                        $this->api_url = trim(
                            $this->get_option(
                                'api_url',
                                'https://sandbox.checkout.weselaja.id'
                            )
                        );

                        $this->api_key = trim(
                            $this->get_option('api_key', '')
                        );

                        $this->secret_key = trim(
                            $this->get_option('secret_key', '')
                        );

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

                        $this->callback_url = trim(
                            $this->get_option('callback_url', '')
                        );

                        add_action(
                            'woocommerce_update_options_payment_gateways_' .
                            $this->id,
                            [$this, 'process_admin_options']
                        );

                        add_action(
                            'woocommerce_thankyou_' . $this->id,
                            [$this, 'thankyou_page']
                        );
                    }

                    public function init_form_fields(): void
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
                                'default' => 'Pay using WeselAja.'
                            ],
                            'api_url' => [
                                'title' => 'API Base URL',
                                'type' => 'text',
                                'description' =>
                                    'Enter the base URL without /v1/payins.',
                                'default' =>
                                    'https://sandbox.checkout.weselaja.id'
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
                                'description' =>
                                    'Use the separate webhook signature secret.',
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
                            'callback_url' => [
                                'title' => 'Callback URL Override',
                                'type' => 'text',
                                'description' =>
                                    'Optional publicly reachable callback URL.',
                                'default' => ''
                            ]
                        ];
                    }

                    public function is_available(): bool
                    {
                        return parent::is_available() &&
                            $this->api_url !== '' &&
                            $this->api_key !== '' &&
                            $this->secret_key !== '' &&
                            $this->webhook_secret_key !== '';
                    }

                    public function process_payment($order_id): array
                    {
                        $order = wc_get_order($order_id);

                        if (!$order) {
                            wc_add_notice(
                                'Unable to load the WooCommerce order.',
                                'error'
                            );

                            return ['result' => 'failure'];
                        }

                        $previous_status = strtoupper(
                            (string) $order->get_meta(
                                '_weselaja_status'
                            )
                        );

                        $previous_payment_id = (string) $order->get_meta(
                            '_weselaja_payment_id'
                        );

                        if (
                            $previous_payment_id !== '' &&
                            !in_array(
                                $previous_status,
                                ['FAILED', 'EXPIRED'],
                                true
                            )
                        ) {
                            return [
                                'result' => 'success',
                                'redirect' => $this->get_return_url($order)
                            ];
                        }

                        $reference_code = (string) $order->get_meta(
                            '_weselaja_reference_code'
                        );

                        if (
                            $reference_code === '' ||
                            in_array(
                                $previous_status,
                                ['FAILED', 'EXPIRED'],
                                true
                            )
                        ) {
                            $suffix = strtoupper(
                                substr(
                                    str_replace(
                                        '-',
                                        '',
                                        wp_generate_uuid4()
                                    ),
                                    0,
                                    8
                                )
                            );

                            $reference_code = sprintf(
                                'WC-%d-%s',
                                $order_id,
                                $suffix
                            );

                            $order->update_meta_data(
                                '_weselaja_reference_code',
                                $reference_code
                            );

                            $order->delete_meta_data(
                                '_weselaja_payment_id'
                            );

                            $order->delete_meta_data(
                                '_weselaja_payment_code'
                            );

                            $order->delete_meta_data(
                                '_weselaja_status'
                            );

                            $order->delete_meta_data(
                                '_weselaja_idempotency_key'
                            );
                        }

                        $idempotency_key = (string) $order->get_meta(
                            '_weselaja_idempotency_key'
                        );

                        if ($idempotency_key === '') {
                            $idempotency_key = 'woocommerce-' .
                                str_replace(
                                    '-',
                                    '',
                                    wp_generate_uuid4()
                                );

                            $order->update_meta_data(
                                '_weselaja_idempotency_key',
                                $idempotency_key
                            );
                        }

                        $order->save();

                        $currency = strtoupper(
                            $order->get_currency()
                        );

                        $order_total = (float) $order->get_total();

                        $amount = $currency === 'IDR'
                            ? (int) round($order_total)
                            : round($order_total, 2);

                        if ($amount <= 0) {
                            wc_add_notice(
                                'Payment amount must be greater than zero.',
                                'error'
                            );

                            return ['result' => 'failure'];
                        }

                        $customer_id = (int) $order->get_customer_id();

                        $customer_reference = $customer_id > 0
                            ? 'WC-CUST-' . $customer_id
                            : 'WC-GUEST-' . $order_id;

                        $customer_name = trim(
                            $order->get_billing_first_name() . ' ' .
                            $order->get_billing_last_name()
                        );

                        if (strlen($customer_name) < 5) {
                            $customer_name = 'Customer ' . $order_id;
                        }

                        $callback_url = $this->callback_url !== ''
                            ? esc_url_raw($this->callback_url)
                            : add_query_arg(
                                'wc-api',
                                'weselaja_callback',
                                home_url('/')
                            );

                        $redirect_url = $this->get_return_url($order);

                        $payload = [
                            'initiatedAmount' => $amount,
                            'currency' => $currency,
                            'paymentMethod' => $this->payment_method,
                            'paymentChannel' => $this->payment_channel,
                            'referenceCode' => $reference_code,
                            'customerReference' => $customer_reference,
                            'customerName' => $customer_name,
                            'callbackUrl' => $callback_url,
                            'redirectUrl' => $redirect_url,
                            'description' => sprintf(
                                'WooCommerce order #%d',
                                $order_id
                            )
                        ];

                        $phone = trim(
                            (string) $order->get_billing_phone()
                        );

                        if ($phone !== '') {
                            $payload['customerPhoneNumber'] = $phone;
                        }

                        $api = new WC_WeselAja_API();

                        $result = $api->create_payin(
                            $this->api_url,
                            $this->api_key,
                            $this->secret_key,
                            $payload,
                            $idempotency_key
                        );

                        if (empty($result['success'])) {
                            $message = sanitize_text_field(
                                (string) (
                                    $result['message'] ??
                                    'WeselAja payment creation failed.'
                                )
                            );

                            $order->add_order_note(
                                'WeselAja payment creation failed: ' .
                                $message
                            );

                            wc_add_notice(
                                'Payment error: ' . $message,
                                'error'
                            );

                            return ['result' => 'failure'];
                        }

                        $data = $result['data'];

                        $payment_id = isset($data['id'])
                            ? sanitize_text_field(
                                (string) $data['id']
                            )
                            : '';

                        if ($payment_id === '') {
                            wc_add_notice(
                                'WeselAja response did not include a payment ID.',
                                'error'
                            );

                            return ['result' => 'failure'];
                        }

                        $payment_code = (string) (
                            $data['paymentCode'] ??
                            $data['paymentUrl'] ??
                            ''
                        );

                        $status = strtoupper(
                            sanitize_text_field(
                                (string) ($data['status'] ?? 'PENDING')
                            )
                        );

                        $order->update_meta_data(
                            '_weselaja_payment_id',
                            $payment_id
                        );

                        $order->update_meta_data(
                            '_weselaja_payment_code',
                            sanitize_text_field($payment_code)
                        );

                        $order->update_meta_data(
                            '_weselaja_payment_channel',
                            sanitize_text_field(
                                (string) (
                                    $data['paymentChannel'] ??
                                    $this->payment_channel
                                )
                            )
                        );

                        $order->update_meta_data(
                            '_weselaja_status',
                            $status
                        );

                        $order->add_order_note(
                            sprintf(
                                'WeselAja payment created. Reference: %s. Payment ID: %s. Status: %s.',
                                $reference_code,
                                $payment_id,
                                $status
                            )
                        );

                        if ($status === 'SUCCESS') {
                            $order->payment_complete($payment_id);
                        } elseif (
                            in_array(
                                $status,
                                ['FAILED', 'EXPIRED'],
                                true
                            )
                        ) {
                            $order->update_status(
                                'failed',
                                'WeselAja payment was not successful.'
                            );
                        } else {
                            $order->update_status(
                                'on-hold',
                                'Awaiting WeselAja payment confirmation.'
                            );
                        }

                        $order->save();

                        $scheme = strtolower(
                            (string) wp_parse_url(
                                $payment_code,
                                PHP_URL_SCHEME
                            )
                        );

                        if (
                            filter_var(
                                $payment_code,
                                FILTER_VALIDATE_URL
                            ) &&
                            in_array(
                                $scheme,
                                ['http', 'https'],
                                true
                            )
                        ) {
                            return [
                                'result' => 'success',
                                'redirect' => esc_url_raw($payment_code)
                            ];
                        }

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

                        $payment_code = (string) $order->get_meta(
                            '_weselaja_payment_code'
                        );

                        $scheme = strtolower(
                            (string) wp_parse_url(
                                $payment_code,
                                PHP_URL_SCHEME
                            )
                        );

                        if (
                            $payment_code === '' ||
                            in_array(
                                $scheme,
                                ['http', 'https'],
                                true
                            )
                        ) {
                            return;
                        }

                        $channel = (string) $order->get_meta(
                            '_weselaja_payment_channel'
                        );

                        echo '<section class="woocommerce-order-details">';
                        echo '<h2>' .
                            esc_html__(
                                'WeselAja payment instructions',
                                'weselaja'
                            ) .
                            '</h2>';
                        echo '<p>' .
                            esc_html__('Payment channel:', 'weselaja') .
                            ' ' . esc_html($channel) . '</p>';
                        echo '<p>' .
                            esc_html__('Payment code:', 'weselaja') .
                            '</p>';
                        echo '<p><strong><code>' .
                            esc_html($payment_code) .
                            '</code></strong></p>';
                        echo '<p>' .
                            esc_html__(
                                'The order will be updated after a verified payment notification is received.',
                                'weselaja'
                            ) .
                            '</p>';
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
                        if (
                            ($_SERVER['REQUEST_METHOD'] ?? '') !== 'POST'
                        ) {
                            self::respond(405, 'Method not allowed');
                        }

                        $settings = get_option(
                            'woocommerce_weselaja_settings',
                            []
                        );

                        $secret = trim(
                            (string) (
                                $settings['webhook_secret_key'] ?? ''
                            )
                        );

                        if ($secret === '') {
                            self::respond(
                                503,
                                'Webhook secret is not configured'
                            );
                        }

                        $timestamp = (string) (
                            $_SERVER['HTTP_X_XENITH_TIMESTAMP'] ?? ''
                        );

                        $signature = (string) (
                            $_SERVER['HTTP_X_XENITH_SIGNATURE'] ?? ''
                        );

                        $raw_body = file_get_contents('php://input');

                        if (
                            $timestamp === '' ||
                            $signature === '' ||
                            !is_string($raw_body) ||
                            $raw_body === ''
                        ) {
                            self::respond(
                                400,
                                'Missing webhook signature or body'
                            );
                        }

                        try {
                            $timestamp_time = (
                                new DateTimeImmutable($timestamp)
                            )->getTimestamp();
                        } catch (Exception $exception) {
                            self::respond(
                                400,
                                'Invalid webhook timestamp'
                            );
                        }

                        if (abs(time() - $timestamp_time) > 300) {
                            self::respond(
                                401,
                                'Webhook timestamp outside accepted window'
                            );
                        }

                        $path = (string) (
                            parse_url(
                                (string) (
                                    $_SERVER['REQUEST_URI'] ?? '/'
                                ),
                                PHP_URL_PATH
                            ) ?: '/'
                        );

                        $string_to_sign =
                            'POST' . '\n' .
                            $path . '\n' .
                            $raw_body . '\n' .
                            $timestamp;

                        $expected_signature = base64_encode(
                            hash_hmac(
                                'sha256',
                                $string_to_sign,
                                $secret,
                                true
                            )
                        );

                        if (!hash_equals(
                            $expected_signature,
                            $signature
                        )) {
                            self::log(
                                'error',
                                'Rejected webhook with invalid signature.'
                            );

                            self::respond(401, 'Invalid signature');
                        }

                        $payload = json_decode($raw_body, true);

                        if (!is_array($payload)) {
                            self::respond(400, 'Invalid JSON payload');
                        }

                        $event_type = (string) (
                            $payload['eventType'] ?? ''
                        );

                        if (
                            strncmp($event_type, 'maintenance.', 12) === 0
                        ) {
                            self::respond(200, 'OK');
                        }

                        $data = $payload['data'] ?? null;

                        if (!is_array($data)) {
                            self::respond(400, 'Missing payment data');
                        }

                        $reference_code = (string) (
                            $data['referenceCode'] ?? ''
                        );

                        $payment_id = (string) ($data['id'] ?? '');

                        $status = strtoupper(
                            (string) ($data['status'] ?? '')
                        );

                        if (
                            !preg_match(
                                '/^WC-(\d+)-([A-F0-9]{8})$/i',
                                $reference_code,
                                $matches
                            ) ||
                            $payment_id === ''
                        ) {
                            self::respond(
                                400,
                                'Invalid payment reference'
                            );
                        }

                        $order = wc_get_order((int) $matches[1]);

                        if (!$order) {
                            self::log(
                                'warning',
                                'Webhook references an unknown order.'
                            );

                            self::respond(200, 'Ignored');
                        }

                        if (
                            (string) $order->get_meta(
                                '_weselaja_reference_code'
                            ) !== $reference_code
                        ) {
                            self::respond(
                                400,
                                'Payment reference mismatch'
                            );
                        }

                        if (
                            (string) $order->get_meta(
                                '_weselaja_payment_id'
                            ) !== $payment_id
                        ) {
                            self::respond(
                                400,
                                'Payment ID mismatch'
                            );
                        }

                        $currency = strtoupper(
                            (string) ($data['currency'] ?? '')
                        );

                        if (
                            $currency !==
                            strtoupper($order->get_currency())
                        ) {
                            self::respond(
                                400,
                                'Payment currency mismatch'
                            );
                        }

                        if (!isset($data['initiatedAmount'])) {
                            self::respond(
                                400,
                                'Payment amount is missing'
                            );
                        }

                        $precision = $currency === 'IDR' ? 0 : 2;

                        $expected_amount = round(
                            (float) $order->get_total(),
                            $precision
                        );

                        $webhook_amount = round(
                            (float) $data['initiatedAmount'],
                            $precision
                        );

                        if ($expected_amount !== $webhook_amount) {
                            self::respond(
                                400,
                                'Payment amount mismatch'
                            );
                        }

                        if ($order->is_paid()) {
                            self::respond(200, 'OK');
                        }

                        if ($status === 'SUCCESS') {
                            $order->payment_complete($payment_id);

                            $order->add_order_note(
                                'WeselAja payment verified successfully.'
                            );
                        } elseif ($status === 'FAILED') {
                            $order->update_status(
                                'failed',
                                'WeselAja payment failed.'
                            );
                        } elseif ($status === 'EXPIRED') {
                            $order->update_status(
                                'cancelled',
                                'WeselAja payment expired.'
                            );
                        } elseif ($status === 'PENDING') {
                            $order->add_order_note(
                                'WeselAja payment remains pending.'
                            );
                        }

                        $order->update_meta_data(
                            '_weselaja_status',
                            $status
                        );

                        $order->save();

                        self::respond(200, 'OK');
                    }

                    private static function respond(
                        int $status_code,
                        string $message
                    ): void {
                        status_header($status_code);

                        header(
                            'Content-Type: text/plain; charset=utf-8'
                        );

                        echo $message;
                        exit;
                    }

                    private static function log(
                        string $level,
                        string $message
                    ): void {
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
                        return
                            !empty($this->settings['enabled']) &&
                            $this->settings['enabled'] === 'yes';
                    }

                    public function get_payment_method_script_handles()
                    {
                        wp_register_script(
                            'wc-weselaja-blocks-integration',
                            plugins_url(
                                'assets/js/weselaja-blocks.js',
                                WESELAJA_WC_FILE
                            ),
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

                    var registerPaymentMethod =
                        wc.wcBlocksRegistry.registerPaymentMethod;

                    var getSetting = wc.wcSettings.getSetting;
                    var createElement = wp.element.createElement;
                    var decodeEntities = wp.htmlEntities.decodeEntities;

                    var settings = getSetting('weselaja_data', {});

                    var label = decodeEntities(
                        settings.title || 'WeselAja'
                    );

                    var Content = function () {
                        return decodeEntities(
                            settings.description || ''
                        );
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

                WooCommerce payment gateway integration for WeselAja.

                ## Requirements

                - WordPress
                - WooCommerce
                - PHP 7.4 or newer
                - WeselAja API key and API secret
                - Separate webhook signature secret

                ## Installation

                1. Copy this plugin directory into `wp-content/plugins/`.
                2. Activate WooCommerce and WooCommerce WeselAja.
                3. Open WooCommerce > Settings > Payments.
                4. Configure API URL, API credentials, payment channel and webhook secret.
                5. Enable the gateway and save the settings.

                ## Callback

                The default callback URL is:

                `https://your-public-site.example/?wc-api=weselaja_callback`

                A local WordPress address is generally not reachable by the payment provider.
                Use a public HTTPS tunnel for local webhook testing and set the Callback URL Override.

                ## Security

                - Keep API keys and webhook secrets private.
                - Use sandbox credentials for tests.
                - Verify webhook signatures before changing order status.
                - Do not mark pending payments as paid.
                - Test the complete payment and callback flow before production use.
                """,

            ["examples/payin_payload.json"] = """
                {
                  "initiatedAmount": 10000,
                  "currency": "IDR",
                  "paymentMethod": "VIRTUAL_ACCOUNT",
                  "paymentChannel": "BCA.VA",
                  "referenceCode": "WC-123-AB12CD34",
                  "customerReference": "WC-GUEST-123",
                  "customerName": "Test Customer",
                  "callbackUrl": "https://example.com/?wc-api=weselaja_callback",
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
                            $this->assertTrue(
                                class_exists('WC_WeselAja_API')
                            );
                        }

                        public function test_gateway_class_is_available(): void
                        {
                            $this->assertTrue(
                                class_exists('WC_Gateway_WeselAja')
                            );
                        }

                        public function test_webhook_handler_is_available(): void
                        {
                            $this->assertTrue(
                                class_exists('WC_WeselAja_Webhook')
                            );
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

        var configuredOutputRoot = Environment.GetEnvironmentVariable(
            "WESELAJA_GENERATOR_OUTPUT_DIR");

        var outputRoot = string.IsNullOrWhiteSpace(configuredOutputRoot)
            ? Path.Combine(
                Directory.GetCurrentDirectory(),
                "generated")
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

        var encoding = new UTF8Encoding(
            encoderShouldEmitUTF8Identifier: false);

        foreach (var relativePath in filesToGenerate)
        {
            if (!TemplateFiles.TryGetValue(
                relativePath,
                out var contents))
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

            File.WriteAllText(
                outputPath,
                contents,
                encoding);

            generatedFiles.Add(new GeneratedFile
            {
                Path = Path.GetRelativePath(
                    outputDirectory,
                    outputPath),
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

        var fullParent = Path.GetFullPath(parentDirectory)
            .TrimEnd(
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