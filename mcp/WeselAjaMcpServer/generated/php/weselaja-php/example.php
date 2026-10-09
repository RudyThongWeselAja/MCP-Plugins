<?php
declare(strict_types=1);

require_once __DIR__ .
    '/src/Exceptions/WeselAjaException.php';

require_once __DIR__ .
    '/src/Models/CreatePaymentRequest.php';

require_once __DIR__ .
    '/src/Models/CreatePaymentResponse.php';

require_once __DIR__ .
    '/src/BankChannels.php';

require_once __DIR__ .
    '/src/EnvLoader.php';

require_once __DIR__ .
    '/src/WeselAjaConfig.php';

require_once __DIR__ .
    '/src/WeselAjaSignature.php';

require_once __DIR__ .
    '/src/WeselAjaClient.php';

use WeselAja\WeselAjaClient;
use WeselAja\Models\CreatePaymentRequest;

$client =
    WeselAjaClient::fromEnvironment();

$payment =
    $client->createPayment(
        new CreatePaymentRequest(
            amount: 10000,
            customerName: 'Andi alarik',
            bank: 'MDR',
            description:
                'WeselAja sandbox example'
        )
    );

echo
    'Payment created successfully.'
    . PHP_EOL;

echo
    'Status   : ' .
    ($payment->status ?? '-')
    . PHP_EOL;

echo
    'Reference: ' .
    ($payment->referenceCode ?? '-')
    . PHP_EOL;

echo
    'Channel  : ' .
    ($payment->paymentChannel ?? '-')
    . PHP_EOL;

echo
    'Payment  : ' .
    ($payment->paymentCode ?? '-')
    . PHP_EOL;