<?php
if (!defined('ABSPATH')) exit;

class WC_Gateway_XenithPay extends WC_Payment_Gateway {

	private $next_secret_key;
	private $payment_url; // /api/payments/init
	private $web_name;
	private $merchant_num;

    // private $api_key;
    // private $secret_key;

    protected $webhook_secret_key;

    public $xenith_callback_url;

    public $showlogo = 'yes';

    public function __construct() {
        $this->id                 = 'xenithpay';
        $this->method_title       = 'XenithPay';
        $this->method_description = 'Custom payment gateway XenithPay for woocommerce.';
        $this->title              = 'XenithPay';
        $this->description        = 'Pay securely using XenithPay payment gateway.';
        $this->has_fields         = false;
        $this->supports           = ['products'];
        $this->enabled            = 'yes';

        // Initialize form fields
        $this->init_form_fields();
        $this->init_settings();

        // Load settings
        $this->title = $this->get_option('title');
        $this->description = $this->get_option('description');
        $this->enabled = $this->get_option('enabled');

        // Initialize credentials
        $this->next_secret_key = $this->get_option('next_secret_key', '');
        $this->webhook_secret_key = $this->get_option('webhook_secret_key', '');
		$this->payment_url = $this->get_option('payment_url', '');
		$this->web_name = $this->get_option('web_name', '');
		$this->merchant_num = $this->get_option('merchant_num', '');
        $this->xenith_callback_url = home_url() . '/wc-api/wc_xenith_callback';

        /////
        // $this->api_key = $this->get_option('api_key', '');
        // $this->secret_key = $this->get_option('secret_key', '');
        /////

        // Debug logging
        if (defined('WP_DEBUG') && WP_DEBUG) {
            error_log('WC XenithPay Gateway: Constructor called');
            error_log('WC XenithPay Gateway: ID = ' . $this->id);
            error_log('WC XenithPay Gateway: Title = ' . $this->title);
            error_log('WC XenithPay Gateway: Enabled = ' . $this->enabled);
            error_log('WC XenithPay Gateway: Available? ' . ($this->is_available() ? 'YES' : 'NO'));
        }

        add_action('woocommerce_update_options_payment_gateways_' . $this->id, [$this, 'process_admin_options']);
        $this->supports = array('products');
    
        // Tambahkan ini
        $this->supports[] = 'store-api';
            add_filter(
                'woocommerce_store_api_process_payment_' . $this->id,
                array($this, 'store_api_payment_handler'),
                10,
                2
            );
        }

    /**
     * Initialize Gateway Settings Form Fields
     */
    public function init_form_fields() {
        $this->form_fields = [
            'enabled' => [
                'title'   => 'Enable/Disable',
                'type'    => 'checkbox',
                'label'   => 'Enable XenithPay Gateway',
                'default' => 'yes'
            ],
            'title' => [
                'title'       => 'Title',
                'type'        => 'text',
                'description' => 'This controls the title customers see during checkout.',
                'default'     => 'XenithPay',
                'desc_tip'    => true,
            ],
            'description' => [
                'title'       => 'Description',
                'type'        => 'textarea',
                'description' => 'Payment method description customers will see on your checkout.',
                'default'     => 'Pay securely using XenithPay payment gateway.',
                'desc_tip'    => true,
            ],
			'next_secret_key' => [
                'title'       => 'Merchant Secret Key',
                'type'        => 'password',
                'description' => 'Enter NEXT Secret Key',
                'default'     => '',
                'desc_tip'    => true,
            ],
///////////////////////////////////////////////////////////////////////////////////
            // 'api_key' => [
            //     'title'       => 'API Key',
            //     'type'        => 'password',
            //     'description' => 'Enter your XenithPay API Key',
            //     'default'     => '',
            //     'desc_tip'    => true,
            // ],
            // 'secret_key' => [
            //     'title'       => 'Secret Key',
            //     'type'        => 'password',
            //     'description' => 'Enter your XenithPay Secret Key',
            //     'default'     => '',
            //     'desc_tip'    => true,
            // ],
///////////////////////////////////////////////////////////////////////////////////
            'webhook_secret_key' => [
                'title' => 'Webhook Signature Secret Key',
                'type'  => 'password',
                'description' => 'Used to verify Xenith webhooks',
                'default'     => '',
                'desc_tip'    => true,
            ],
            'payment_url' => [
                'title'       => 'Payment URL',
                'type'        => 'text',
                'default'     => '',
                'desc_tip'    => true,
            ],
			'merchant_num' => [
				'title' => 'Merchant Number',
				'type' => 'text',
				'default' => '',
				'desc_tip' => true,
			],
			'web_name' => [
				'title' => 'Web Name',
				'type' => 'text',
				'default' => '',
				'desc_tip' => true,
			]
        ];
    }

    /**
     * Check if gateway is available
     */
    public function is_available() {
        $is_enabled = ('yes' === $this->enabled);
		$has_secret_key = !empty($this->next_secret_key);
        
        // TEMPORARY FIX FOR TESTING: Allow gateway to be available even without API keys
        // Remove this condition for production use
        $is_testing_mode = true; // Set to false for production
        
        if ($is_testing_mode) {
            $is_available = $is_enabled; // Only check if enabled
        }
        
        if (defined('WP_DEBUG') && WP_DEBUG) {
            error_log('WC XenithPay Gateway: is_available() called');
            error_log('WC XenithPay Gateway: Testing Mode: ' . ($is_testing_mode ? 'YES' : 'NO'));
            error_log('WC XenithPay Gateway: Enabled? ' . ($is_enabled ? 'YES' : 'NO'));
			error_log('WC XenithPay Gateway: Has Secret Key? ' . ($has_secret_key ? 'YES' : 'NO'));
            error_log('WC XenithPay Gateway: Final availability: ' . ($is_available ? 'AVAILABLE' : 'NOT AVAILABLE'));
        }
        
        return $is_available;
    }

    /**
     * Admin Panel Options
     */
    public function admin_options() {
        ?>
        <h3><?php echo esc_html($this->method_title); ?></h3>
        <p><?php echo esc_html($this->method_description); ?></p>
        <table class="form-table">
            <?php $this->generate_settings_html(); ?>
        </table>
        <?php
    }

    public function process_payment($order_id) {
        if (defined('WP_DEBUG') && WP_DEBUG) {
            error_log('WC XenithPay Gateway: process_payment called for order ' . $order_id);
        }
		
		$has_secret_key = !empty($this->next_secret_key);

        if (!$has_secret_key) {
            // Simulate successful test payment
            $order->add_order_note('XenithPay TEST MODE: Payment simulated.');
            $order->payment_complete();

            return [
                'result'   => 'success',
                'redirect' => $this->get_return_url($order),
            ];
        }

        $order = wc_get_order($order_id);
        $timestamp = gmdate("Y-m-d\TH:i:s\Z");
        $method = 'POST';
        $uri = '/api/payments/init';

        // Real API call
        $body = json_encode([
			'merchantNum' => $this->merchant_num,
            'initiatedAmount' => (int) $order->get_total(),
            'currency' => $order->get_currency(),
            'referenceCode' => $this->web_name . '-' . $order_id,
            'customerReference' => 'CUSTOMER-' . $order->get_customer_id(),
            'customerName' => trim($order->get_billing_first_name() . ' ' . $order->get_billing_last_name()),
            'callbackUrl' => $this->xenith_callback_url,
            'redirectUrl' => $this->get_return_url($order),
        ], JSON_UNESCAPED_SLASHES);

        $signature_payload = $method . "\n" . $uri . "\n" . $timestamp . "\n" . $body;
        $raw_hmac = hash_hmac('sha256', $signature_payload, $this->next_secret_key, true);
        $signature = base64_encode($raw_hmac);

        $response = wp_remote_post($this->payment_url, [
            'body'    => $body,
            'headers' => [
                'Content-Type' => 'application/json',
                'X-Timestamp' => $timestamp,
                'X-Signature' => $signature,
                'X-Idempotency-Key' => uniqid($this->web_name . '-', true),
                'Accept' => 'application/json',
            ],
            'timeout' => 60,
        ]);

        // $response = wp_remote_post($this->payment_url, [
        //     'body'    => $body,
        //     'headers' => [
        //         'Content-Type' => 'application/json',
        //         'Xenith-Api-Key' => $this->api_key,
        //         'Xenith-Request-Timestamp' => $timestamp,
        //         'Xenith-Request-Signature' => $signature,
        //         'X-Idempotency-Key' => uniqid('antiqpay-', true),
        //         'Accept' => 'application/json',
        //     ],
        //     'timeout' => 60,
        // ]);

        // ERROR: WordPress HTTP error
        if (is_wp_error($response)) {
            $msg = $response->get_error_message();
            wc_add_notice('Payment error: ' . $msg, 'error');

            return [
                'result'   => 'failure',
                'redirect' => '',
                'message'  => $msg,
            ];
        }

        $body = json_decode(wp_remote_retrieve_body($response), true);

        if (defined('WP_DEBUG') && WP_DEBUG) {
            error_log('XenithPay response: ' . print_r($body, true));
        }

        // SUCCESS: Redirect URL provided
        if (!empty($body['redirect_url'])) {
            return [
                'result'   => 'success',
                'redirect' => esc_url($body['redirect_url']),    
            ];
        }

        // FAILURE: Invalid response
        $msg = esc_url($body['error']);
        wc_add_notice($msg, 'error');

        return [
            'result'   => 'failure',
            'redirect' => '',
            'message'  => esc_url($body['error']),
        ];
    }

    public function get_icon()
    {
        $style = "style='margin-left: 0.3em; max-height: 28px; max-width: 65px;'";
        $icon = '<img src="' . plugins_url('assets/images/xenith.svg', WC_XENITH_MAIN_FILE) . '" alt="Xenith" ' . $style . ' />';

        return apply_filters('woocommerce_gateway_icon', $icon, $this->id);
    }
    
    public function store_api_payment_handler( $order, $request_data ) {
        // Tandai order complete (sandbox)
        $order->payment_complete();

        return array(
            'status'       => 'success',
            'redirect_url' => $this->get_return_url($order),
            'meta'         => array(
                'gateway' => $this->id,
                'message' => 'Sandbox payment completed via Store API.'
            ),
        );
    }
}