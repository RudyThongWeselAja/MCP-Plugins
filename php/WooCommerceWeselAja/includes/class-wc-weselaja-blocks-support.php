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
        $this->settings = get_option('woocommerce_weselaja_settings', []);
    }

    public function is_active()
    {
        return isset($this->settings['enabled'], $this->settings['api_key'], $this->settings['secret_key'])
            && $this->settings['enabled'] === 'yes'
            && $this->settings['api_key'] !== ''
            && $this->settings['secret_key'] !== '';
    }

    public function get_payment_method_script_handles()
    {
        $script_asset_path = WESELAJA_WC_PATH . 'assets/js/weselaja-blocks.asset.php';
        $script_asset = file_exists($script_asset_path)
            ? require $script_asset_path
            : ['dependencies' => ['wc-blocks-registry', 'wc-settings', 'wp-element'], 'version' => WESELAJA_WC_VERSION];

        wp_register_script(
            'weselaja-woocommerce-blocks',
            plugins_url('assets/js/weselaja-blocks.js', WESELAJA_WC_FILE),
            $script_asset['dependencies'],
            $script_asset['version'],
            true
        );

        return ['weselaja-woocommerce-blocks'];
    }

    public function get_payment_method_data()
    {
        return [
            'title' => $this->settings['title'] ?? __('WeselAja Virtual Account', 'weselaja-woocommerce'),
            'description' => $this->settings['description'] ?? '',
            'supports' => ['products'],
        ];
    }
}
