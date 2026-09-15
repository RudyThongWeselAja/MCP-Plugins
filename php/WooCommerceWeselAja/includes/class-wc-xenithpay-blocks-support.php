<?php
use Automattic\WooCommerce\Blocks\Payments\Integrations\AbstractPaymentMethodType;

final class WC_XenithPay_Blocks_Support extends AbstractPaymentMethodType {
    
    private $gateway;
    
    protected $name = 'xenithpay'; // payment gateway id

    public function initialize() {
        // get payment gateway settings
        $this->settings = get_option( "woocommerce_{$this->name}_settings", array() );
        
        if (defined('WP_DEBUG') && WP_DEBUG) {
            error_log('WC XenithPay Blocks: Initialize. Settings found? ' . (empty($this->settings) ? 'NO' : 'YES'));
        }
    }

    public function is_active() {
        $active = ! empty( $this->settings[ 'enabled' ] ) && 'yes' === $this->settings[ 'enabled' ];
        
        if (defined('WP_DEBUG') && WP_DEBUG) {
            error_log('WC XenithPay Blocks: is_active check.');
            error_log('WC XenithPay Blocks: Active result: ' . ($active ? 'YES' : 'NO'));
        }

        // Force active for testing if settings are empty (first run)
        if (empty($this->settings)) {
             if (defined('WP_DEBUG') && WP_DEBUG) {
                error_log('WC XenithPay Blocks: Settings empty, forcing active for testing.');
             }
             return true;
        }

        return $active;
    }

    public function get_payment_method_script_handles() {
        // Fix URL generation
        $script_url = plugins_url( 'assets/js/xenithpay-blocks.js', dirname( __DIR__ ) . '/wc-xenithpay.php' );
        
        if (defined('WP_DEBUG') && WP_DEBUG) {
            error_log('WC XenithPay Blocks: Registering script at ' . $script_url);
        }

        $dependencies = array(
            'wc-blocks-registry',
            'wc-settings',
            'wp-element',
            'wp-html-entities',
            'wp-i18n',
        );

        wp_register_script(
            'wc-xenithpay-blocks-integration',
            $script_url,
            $dependencies,
            time(), // Use time() to bust cache
            true
        );

        return array( 'wc-xenithpay-blocks-integration' );
    }

    public function get_payment_method_data() {
        return array(
            'title'       => $this->get_setting( 'title' ),
            'description' => $this->get_setting( 'description' ),
            'icon'        => plugins_url(
                'assets/images/xenith.svg',
                dirname( __DIR__ ) . '/wc-xenithpay.php'
            ),
            'supports'    => array( 'products' ),
        );
    }
}