<?php

if (!defined('ABSPATH')) {
    exit;
}

class XenithPay_Mcp_Audit
{
    public static function table_name()
    {
        global $wpdb;

        return $wpdb->prefix . 'xenithpay_audit_logs';
    }

    public static function create_table()
    {
        global $wpdb;

        $table_name = self::table_name();

        $charset_collate = $wpdb->get_charset_collate();

        require_once ABSPATH . 'wp-admin/includes/upgrade.php';

        $sql = "CREATE TABLE {$table_name} (
            id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
            event VARCHAR(50) NOT NULL,
            source VARCHAR(50) NOT NULL,
            tool VARCHAR(100) NULL,
            user_id BIGINT UNSIGNED NULL,
            ip_address VARCHAR(45) NULL,
            user_agent TEXT NULL,
            reference_code VARCHAR(255) NULL,
            request_id VARCHAR(255) NULL,
            status VARCHAR(30) NOT NULL,
            error_message TEXT NULL,
            created_at DATETIME NOT NULL,
            PRIMARY KEY (id),
            KEY event (event),
            KEY source (source),
            KEY status (status),
            KEY created_at (created_at),
            KEY reference_code (reference_code)
        ) {$charset_collate};";

        dbDelta($sql);
    }

    public static function log(array $data)
    {
        global $wpdb;

        $table_name = self::table_name();

        $wpdb->insert(
            $table_name,
            [
                'event' => isset($data['event'])
                    ? sanitize_text_field($data['event'])
                    : 'unknown',

                'source' => isset($data['source'])
                    ? sanitize_text_field($data['source'])
                    : 'unknown',

                'tool' => isset($data['tool'])
                    ? sanitize_text_field($data['tool'])
                    : null,

                'user_id' => isset($data['user_id'])
                    ? absint($data['user_id'])
                    : null,

                'ip_address' => isset($data['ip_address'])
                    ? sanitize_text_field($data['ip_address'])
                    : null,

                'user_agent' => isset($data['user_agent'])
                    ? sanitize_textarea_field($data['user_agent'])
                    : null,

                'reference_code' => isset($data['reference_code'])
                    ? sanitize_text_field($data['reference_code'])
                    : null,

                'request_id' => isset($data['request_id'])
                    ? sanitize_text_field($data['request_id'])
                    : null,

                'status' => isset($data['status'])
                    ? sanitize_text_field($data['status'])
                    : 'unknown',

                'error_message' => isset($data['error_message'])
                    ? sanitize_textarea_field($data['error_message'])
                    : null,

                'created_at' => current_time('mysql'),
            ],
            [
                '%s',
                '%s',
                '%s',
                '%d',
                '%s',
                '%s',
                '%s',
                '%s',
                '%s',
                '%s',
                '%s',
            ]
        );
    }

    public static function request_id()
    {
        return 'mcp-' . wp_generate_uuid4();
    }

    public static function client_ip()
    {
        return isset($_SERVER['REMOTE_ADDR'])
            ? sanitize_text_field(wp_unslash($_SERVER['REMOTE_ADDR']))
            : null;
    }

    public static function user_agent()
    {
        return isset($_SERVER['HTTP_USER_AGENT'])
            ? sanitize_textarea_field(
                wp_unslash($_SERVER['HTTP_USER_AGENT'])
            )
            : null;
    }

    public static function current_user_id()
    {
        $user_id = get_current_user_id();

        return $user_id > 0
            ? $user_id
            : null;
    }
}