<?php

declare(strict_types=1);

$plugin = file_get_contents(__DIR__ . '/../includes/class-wc-weselaja-api.php');
if ($plugin === false) {
    fwrite(STDERR, "Could not read API client source.\n");
    exit(1);
}

$gateway = file_get_contents(__DIR__ . '/../includes/class-wc-gateway-weselaja.php');
if ($gateway === false) {
    fwrite(STDERR, "Could not read gateway source.\n");
    exit(1);
}

foreach (['VIRTUAL_ACCOUNT', 'BRI.VA', 'MDR.VA', 'BNI.VA', 'PTB.VA', 'CIMB.VA', 'BCA.VA'] as $expected) {
    if (strpos($gateway, $expected) === false) {
        fwrite(STDERR, "Missing expected payment configuration: {$expected}\n");
        exit(1);
    }
}

if (strpos($gateway, "'_weselaja_reference_code'") === false) {
    fwrite(STDERR, "Order reference persistence is missing.\n");
    exit(1);
}

echo "WeselAja WooCommerce integration checks passed.\n";
