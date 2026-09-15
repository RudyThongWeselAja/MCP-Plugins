<?php

if (!defined('ABSPATH')) {
    exit; // Exit if accessed directly
}

define('WC_XENITH_MAIN_FILE', __FILE__);

/*
 * ==========================================
 * XenithPay MCP Audit Logger
 * ==========================================
 */

$audit_file = plugin_dir_path(__FILE__) . 'mcp/audit.php';

if (file_exists($audit_file)) {
    require_once $audit_file;
}

/*
 * Create audit table when plugin is activated.
 */
if (class_exists('XenithPay_Mcp_Audit')) {
    register_activation_hook(
        __FILE__,
        ['XenithPay_Mcp_Audit', 'create_table']
    );
}


/*
 * ==========================================
 * XenithPay Initialization
 * ==========================================
 */

add_action('plugins_loaded', 'wc_xenithpay_init', 11);


/*
 * ==========================================
 * Capture raw webhook body
 * ==========================================
 */

add_action(
    'woocommerce_api_wc_xenith_callback',
    function () {
        global $xenith_raw_body;

        $xenith_raw_body =
            file_get_contents('php://input');
    },
    1
);


/*
 * ==========================================
 * Handle webhook
 * ==========================================
 */

add_action(
    'woocommerce_api_wc_xenith_callback',
    'wc_xenith_handle_webhook',
    10
);


/*
 * ==========================================
 * Initialize XenithPay Gateway
 * ==========================================
 */

function wc_xenithpay_init()
{
    if (
        defined('WP_DEBUG')
        && WP_DEBUG
    ) {
        error_log(
            'WC XenithPay: Initializing XenithPay Gateway plugin.'
        );

        error_log(
            'WC XenithPay: WooCommerce active? ' .
            (
                class_exists('WooCommerce')
                    ? 'YES'
                    : 'NO'
            )
        );

        error_log(
            'WC XenithPay: WC_Payment_Gateway exists? ' .
            (
                class_exists('WC_Payment_Gateway')
                    ? 'YES'
                    : 'NO'
            )
        );
    }


    /*
     * WooCommerce check
     */

    if (!class_exists('WC_Payment_Gateway')) {

        add_action(
            'admin_notices',
            'wc_xenithpay_woocommerce_missing_notice'
        );

        error_log(
            'WC XenithPay: WooCommerce not active or WC_Payment_Gateway not found.'
        );

        return;
    }


    /*
     * Gateway class file
     */

    $file =
        plugin_dir_path(__FILE__) .
        'includes/class-wc-gateway-xenithpay.php';


    if (!file_exists($file)) {

        error_log(
            'WC XenithPay: includes file not found: ' .
            $file
        );

        add_action(
            'admin_notices',
            function () use ($file) {

                echo '<div class="notice notice-error"><p>';

                echo 'WC XenithPay: missing file ';

                echo esc_html($file);

                echo '</p></div>';
            }
        );

        return;
    }


    /*
     * Load gateway class
     */

    require_once $file;


    /*
     * Verify gateway class exists
     */

    if (!class_exists('WC_Gateway_XenithPay')) {

        error_log(
            'WC XenithPay: class WC_Gateway_XenithPay not found after include.'
        );

        add_action(
            'admin_notices',
            function () {

                echo '<div class="notice notice-error"><p>';

                echo 'WC XenithPay: gateway class not found. ';
                echo 'Periksa includes/class-wc-gateway-xenithpay.php';

                echo '</p></div>';
            }
        );

        return;
    }


    /*
     * Register payment gateway
     */

    add_filter(
        'woocommerce_payment_gateways',
        'wc_xenithpay_add_gateway'
    );
}