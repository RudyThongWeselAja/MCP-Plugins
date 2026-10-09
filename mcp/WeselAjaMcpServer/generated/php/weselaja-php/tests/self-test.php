<?php
declare(strict_types=1);

require_once __DIR__ .
    '/../src/BankChannels.php';

require_once __DIR__ .
    '/../src/Exceptions/WeselAjaException.php';

require_once __DIR__ .
    '/../src/WeselAjaSignature.php';

use WeselAja\BankChannels;
use WeselAja\WeselAjaSignature;

function assertTest(
    bool $condition,
    string $message
): void {
    if (!$condition) {
        throw new RuntimeException(
            $message
        );
    }
}

assertTest(
    BankChannels::resolve('BRI') ===
        'BRI.VA',
    'BRI mapping failed.'
);

assertTest(
    BankChannels::resolve(
        'Bank BRI'
    ) ===
        'BRI.VA',
    'Bank BRI mapping failed.'
);

assertTest(
    BankChannels::resolve(
        'Mandiri'
    ) ===
        'MDR.VA',
    'Mandiri mapping failed.'
);

assertTest(
    BankChannels::resolve(
        'Bank Mandiri'
    ) ===
        'MDR.VA',
    'Bank Mandiri mapping failed.'
);

assertTest(
    BankChannels::resolve(
        'BNI'
    ) ===
        'BNI.VA',
    'BNI mapping failed.'
);

assertTest(
    BankChannels::resolve(
        'Permata'
    ) ===
        'PTB.VA',
    'Permata mapping failed.'
);

assertTest(
    BankChannels::resolve(
        'CIMB'
    ) ===
        'CIMB.VA',
    'CIMB mapping failed.'
);

assertTest(
    BankChannels::resolve(
        'CIMB Niaga'
    ) ===
        'CIMB.VA',
    'CIMB Niaga mapping failed.'
);

assertTest(
    BankChannels::resolve(
        'BCA'
    ) ===
        'BCA.VA',
    'BCA mapping failed.'
);

$unsupportedRejected =
    false;

try {
    BankChannels::resolve(
        'Bank Tidak Ada'
    );
} catch (
    InvalidArgumentException
) {
    $unsupportedRejected =
        true;
}

assertTest(
    $unsupportedRejected,
    'Unsupported bank must be rejected.'
);

$method =
    'POST';

$uri =
    '/payment/weselaja/callback';

$body =
    '{"hello":"world"}';

$timestamp =
    '2026-01-01T00:00:00.000Z';

$secret =
    'test-secret';

$signature =
    WeselAjaSignature::generateCallback(
        $method,
        $uri,
        $body,
        $timestamp,
        $secret
    );

assertTest(
    WeselAjaSignature::verifyCallback(
        $method,
        $uri,
        $body,
        $timestamp,
        $signature,
        $secret
    ),
    'Callback signature verification failed.'
);

assertTest(
    !WeselAjaSignature::verifyCallback(
        $method,
        $uri,
        $body,
        $timestamp,
        $signature . 'x',
        $secret
    ),
    'Invalid callback signature must be rejected.'
);

echo
    'All PHP WeselAja self-tests passed.'
    . PHP_EOL;