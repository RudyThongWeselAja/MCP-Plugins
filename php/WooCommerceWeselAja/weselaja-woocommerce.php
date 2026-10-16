<?php
/**
 * Plugin Name: WeselAja for WooCommerce
 * Description: Virtual Account payments with WeselAja for WooCommerce.
 * Version: 1.0.0
 * Requires at least: 6.2
 * Requires PHP: 8.0
 * WC requires at least: 8.0
 * Text Domain: weselaja-woocommerce
 *
 * @package WeselAjaWooCommerce
 */

if (!defined('ABSPATH')) {
    exit;
}

define('WESELAJA_WC_VERSION', '1.0.0');
define('WESELAJA_WC_FILE', __FILE__);
define('WESELAJA_WC_PATH', plugin_dir_path(__FILE__));

require_once WESELAJA_WC_PATH . 'includes/class-wc-weselaja-api.php';
require_once WESELAJA_WC_PATH . 'includes/class-wc-weselaja-webhook.php';

add_action('plugins_loaded', 'weselaja_wc_init', 11);

function weselaja_wc_init()
{
    if (!class_exists('WooCommerce')) {
        add_action('admin_notices', 'weselaja_wc_woocommerce_missing_notice');
        return;
    }

    require_once WESELAJA_WC_PATH . 'includes/class-wc-gateway-weselaja.php';

    add_filter('woocommerce_payment_gateways', static function ($gateways) {
        $gateways[] = 'WC_Gateway_WeselAja';
        return $gateways;
    });

    WC_WeselAja_Webhook::init();

    add_action('woocommerce_blocks_loaded', static function () {
        if (!class_exists('\Automattic\WooCommerce\Blocks\Payments\Integrations\AbstractPaymentMethodType')) {
            return;
        }

        require_once WESELAJA_WC_PATH . 'includes/class-wc-weselaja-blocks-support.php';

        add_action(
            'woocommerce_blocks_payment_method_type_registration',
            static function ($registry) {
                $registry->register(new WC_WeselAja_Blocks_Support());
            }
        );
    });
}

function weselaja_wc_woocommerce_missing_notice()
{
    if (!current_user_can('activate_plugins')) {
        return;
    }

    echo '<div class="notice notice-error"><p>';
    echo esc_html__('WeselAja for WooCommerce requires WooCommerce to be installed and active.', 'weselaja-woocommerce');
    echo '</p></div>';
}

add_filter('plugin_action_links_' . plugin_basename(__FILE__), static function ($links) {
    $settings_url = admin_url('admin.php?page=wc-settings&tab=checkout&section=weselaja');
    array_unshift(
        $links,
        '<a href="' . esc_url($settings_url) . '">' . esc_html__('Settings', 'weselaja-woocommerce') . '</a>'
    );
    return $links;
});
