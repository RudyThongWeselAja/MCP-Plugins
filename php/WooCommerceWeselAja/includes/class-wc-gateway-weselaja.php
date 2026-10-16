<?php

if (!defined('ABSPATH')) {
    exit;
}

final class WC_Gateway_WeselAja extends WC_Payment_Gateway
{
    public function __construct()
    {
        $this->id = 'weselaja';
        $this->method_title = __('WeselAja', 'weselaja-woocommerce');
        $this->method_description = __('Accept Virtual Account payments through WeselAja.', 'weselaja-woocommerce');
        $this->has_fields = true;
        $this->supports = ['products'];

        $this->init_form_fields();
        $this->init_settings();
        $this->settings = $this->settings ?? [];

        $this->enabled = $this->get_option('enabled', 'no');
        $this->title = $this->get_option('title', __('WeselAja Virtual Account', 'weselaja-woocommerce'));
        $this->description = $this->get_option('description', __('Pay securely using a Virtual Account.', 'weselaja-woocommerce'));

        add_action('woocommerce_update_options_payment_gateways_' . $this->id, [$this, 'process_admin_options']);
        add_action('woocommerce_thankyou_' . $this->id, [$this, 'thankyou_page']);
        add_action('woocommerce_email_after_order_table', [$this, 'email_instructions'], 10, 3);
    }

    public function init_form_fields()
    {
        $this->form_fields = [
            'enabled' => [
                'title' => __('Enable/Disable', 'weselaja-woocommerce'),
                'type' => 'checkbox',
                'label' => __('Enable WeselAja', 'weselaja-woocommerce'),
                'default' => 'no',
            ],
            'title' => [
                'title' => __('Title', 'weselaja-woocommerce'),
                'type' => 'text',
                'default' => __('WeselAja Virtual Account', 'weselaja-woocommerce'),
            ],
            'description' => [
                'title' => __('Description', 'weselaja-woocommerce'),
                'type' => 'textarea',
                'default' => __('Pay securely using a Virtual Account.', 'weselaja-woocommerce'),
            ],
            'api_url' => [
                'title' => __('API URL', 'weselaja-woocommerce'),
                'type' => 'url',
                'description' => __('Use the WeselAja sandbox URL for testing.', 'weselaja-woocommerce'),
                'default' => 'https://sandbox.checkout.weselaja.id',
            ],
            'api_key' => [
                'title' => __('API Key', 'weselaja-woocommerce'),
                'type' => 'password',
                'default' => '',
            ],
            'secret_key' => [
                'title' => __('Secret Key', 'weselaja-woocommerce'),
                'type' => 'password',
                'default' => '',
            ],
            'payment_channel' => [
                'title' => __('Virtual Account Bank', 'weselaja-woocommerce'),
                'type' => 'select',
                'description' => __('Select the bank channel used for all WeselAja checkouts.', 'weselaja-woocommerce'),
                'default' => 'BRI.VA',
                'options' => [
                    'BRI.VA' => 'BRI',
                    'MDR.VA' => 'Mandiri',
                    'BNI.VA' => 'BNI',
                    'PTB.VA' => 'Permata',
                    'CIMB.VA' => 'CIMB',
                    'BCA.VA' => 'BCA',
                ],
            ],
            'webhook_secret' => [
                'title' => __('Webhook Secret', 'weselaja-woocommerce'),
                'type' => 'password',
                'description' => sprintf(
                    /* translators: %s: Webhook URL. */
                    __('Configure this URL in WeselAja: %s', 'weselaja-woocommerce'),
                    rest_url('weselaja/v1/webhook')
                ),
                'default' => '',
            ],
        ];
    }

    public function is_available()
    {
        if (!parent::is_available()) {
            return false;
        }

        return $this->get_option('api_url') !== ''
            && $this->get_option('api_key') !== ''
            && $this->get_option('secret_key') !== '';
    }

    public function payment_fields()
    {
        if ($this->description !== '') {
            echo wpautop(wp_kses_post($this->description));
        }

        $channel = $this->get_option('payment_channel', 'BRI.VA');
        $banks = [
            'BRI.VA' => 'BRI',
            'MDR.VA' => 'Mandiri',
            'BNI.VA' => 'BNI',
            'PTB.VA' => 'Permata',
            'CIMB.VA' => 'CIMB',
            'BCA.VA' => 'BCA',
        ];
        printf(
            '<p>%s</p>',
            esc_html(sprintf(
                /* translators: %s: Bank name. */
                __('Your payment will use %s Virtual Account.', 'weselaja-woocommerce'),
                $banks[$channel] ?? 'Virtual Account'
            ))
        );
    }

    public function process_payment($order_id)
    {
        $order = wc_get_order($order_id);
        if (!$order) {
            wc_add_notice(__('Unable to find the order for payment.', 'weselaja-woocommerce'), 'error');
            return ['result' => 'failure'];
        }

        if ($order->get_payment_method() !== $this->id) {
            wc_add_notice(__('Invalid payment method for this order.', 'weselaja-woocommerce'), 'error');
            return ['result' => 'failure'];
        }

        $reference = 'WC-' . $order->get_id() . '-' . wp_generate_password(8, false, false);
        $customer_name = trim($order->get_billing_first_name() . ' ' . $order->get_billing_last_name());
        if ($customer_name === '') {
            $customer_name = __('WooCommerce customer', 'weselaja-woocommerce');
        }

        $payload = [
            'initiatedAmount' => (int) round((float) $order->get_total()),
            'currency' => $order->get_currency(),
            'paymentMethod' => 'VIRTUAL_ACCOUNT',
            'paymentChannel' => $this->get_option('payment_channel', 'BRI.VA'),
            'referenceCode' => $reference,
            'customerReference' => (string) ($order->get_customer_id() ?: $order->get_id()),
            'customerName' => $customer_name,
            'callbackUrl' => rest_url('weselaja/v1/webhook'),
            'redirectUrl' => $this->get_return_url($order),
            'description' => sprintf(
                /* translators: %s: WooCommerce order number. */
                __('WooCommerce order %s', 'weselaja-woocommerce'),
                $order->get_order_number()
            ),
        ];

        $phone = $order->get_billing_phone();
        if ($phone !== '') {
            $payload['customerPhoneNumber'] = $phone;
        }

        $api = new WC_WeselAja_API($this->settings);
        $payment = $api->create_payment($payload);
        if (is_wp_error($payment)) {
            wc_get_logger()->error(
                'WeselAja payment creation failed: ' . $payment->get_error_message(),
                ['source' => 'weselaja']
            );
            wc_add_notice(
                sprintf(
                    /* translators: %s: Payment error message. */
                    __('Payment could not be created: %s', 'weselaja-woocommerce'),
                    $payment->get_error_message()
                ),
                'error'
            );
            return ['result' => 'failure'];
        }

        $order->update_meta_data('_weselaja_reference_code', $reference);
        $order->update_meta_data('_weselaja_payment_id', sanitize_text_field((string) ($payment['id'] ?? '')));
        $order->update_meta_data('_weselaja_payment_code', sanitize_text_field((string) ($payment['paymentCode'] ?? '')));
        $order->update_meta_data('_weselaja_payment_code_type', sanitize_text_field((string) ($payment['paymentCodeType'] ?? '')));
        $order->update_meta_data('_weselaja_expiration_time', sanitize_text_field((string) ($payment['expirationTime'] ?? '')));
        $order->update_meta_data('_weselaja_payment_status', sanitize_text_field((string) ($payment['status'] ?? 'PENDING')));
        $order->update_status('on-hold', __('Awaiting WeselAja Virtual Account payment.', 'weselaja-woocommerce'));
        $order->save();

        if (!empty($payment['redirectUrl'])) {
            $redirect = esc_url_raw((string) $payment['redirectUrl']);
            if (wp_parse_url($redirect, PHP_URL_SCHEME) === 'https') {
                return ['result' => 'success', 'redirect' => $redirect];
            }
        }

        return [
            'result' => 'success',
            'redirect' => $this->get_return_url($order),
        ];
    }

    public function thankyou_page($order_id)
    {
        $order = wc_get_order($order_id);
        if (!$order || $order->get_payment_method() !== $this->id) {
            return;
        }

        $code = $order->get_meta('_weselaja_payment_code');
        $expiration = $order->get_meta('_weselaja_expiration_time');
        if ($code === '') {
            return;
        }

        echo '<section class="woocommerce-order-details">';
        echo '<h2>' . esc_html__('WeselAja payment instructions', 'weselaja-woocommerce') . '</h2>';
        echo '<p>' . esc_html__('Complete your payment using this Virtual Account code:', 'weselaja-woocommerce') . '</p>';
        echo '<p><strong>' . esc_html($code) . '</strong></p>';
        if ($expiration !== '') {
            echo '<p>' . esc_html(sprintf(
                /* translators: %s: Expiration timestamp. */
                __('Payment expires at %s.', 'weselaja-woocommerce'),
                $expiration
            )) . '</p>';
        }
        echo '</section>';
    }

    public function email_instructions($order, $sent_to_admin, $plain_text)
    {
        if ($sent_to_admin || !$order instanceof WC_Order || $order->get_payment_method() !== $this->id) {
            return;
        }

        $code = $order->get_meta('_weselaja_payment_code');
        if ($code === '') {
            return;
        }

        if ($plain_text) {
            echo "\n" . esc_html__('WeselAja Virtual Account code:', 'weselaja-woocommerce') . ' ' . esc_html($code) . "\n";
            return;
        }

        echo '<p>' . esc_html__('WeselAja Virtual Account code:', 'weselaja-woocommerce') . ' <strong>' . esc_html($code) . '</strong></p>';
    }
}
