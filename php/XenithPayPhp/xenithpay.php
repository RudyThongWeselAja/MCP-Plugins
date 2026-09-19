<?php

declare(strict_types=1);

require_once __DIR__ . '/client.php';

if (PHP_SAPI !== 'cli') {
    fwrite(
        STDERR,
        "CLI only\n"
    );

    exit(1);
}

$input = stream_get_contents(STDIN);

if ($input === false) {
    echo json_encode([
        'success' => false,
        'error' => 'Unable to read STDIN.',
    ]);

    exit(0);
}

fwrite(
    STDERR,
    '[PHP] Input bytes: ' .
    strlen($input) .
    PHP_EOL
);

$input = preg_replace(
    '/^\xEF\xBB\xBF/',
    '',
    $input
);

$input = trim($input);

fwrite(
    STDERR,
    '[PHP] Input length: ' .
    strlen($input) .
    PHP_EOL
);

fwrite(
    STDERR,
    '[PHP] Input preview: ' .
    var_export(
        substr($input, 0, 150),
        true
    ) .
    PHP_EOL
);

if ($input === '') {
    echo json_encode([
        'success' => false,
        'error' => 'No input received.',
    ]);

    exit(0);
}

try {
    $request = json_decode(
        $input,
        true,
        512,
        JSON_THROW_ON_ERROR
    );
} catch (JsonException $exception) {
    fwrite(
        STDERR,
        '[PHP] JSON decode error: ' .
        $exception->getMessage() .
        PHP_EOL
    );

    echo json_encode([
        'success' => false,
        'error' =>
            'Invalid JSON input: ' .
            $exception->getMessage(),
    ]);

    exit(0);
}

if (!is_array($request)) {
    echo json_encode([
        'success' => false,
        'error' => 'Invalid request format.',
    ]);

    exit(0);
}

$tool =
    isset($request['tool']) &&
    is_string($request['tool'])
        ? $request['tool']
        : '';

$arguments =
    isset($request['arguments']) &&
    is_array($request['arguments'])
        ? $request['arguments']
        : [];

fwrite(
    STDERR,
    '[PHP] Tool: ' .
    $tool .
    PHP_EOL
);

if ($tool === 'create_xenithpay_payment') {
    fwrite(
        STDERR,
        '[PHP] Arguments JSON: ' .
        json_encode(
            $arguments,
            JSON_UNESCAPED_SLASHES |
            JSON_UNESCAPED_UNICODE
        ) .
        PHP_EOL
    );
}

try {
    switch ($tool) {
        case 'is_xenithpay_active':
            echo json_encode([
                'success' => true,
                'active' => true,
                'error' => null,
            ]);

            exit(0);

        case 'get_xenithpay_payment_method':
            echo json_encode([
                'success' => true,
                'id' => 'xenithpay',
                'title' =>
                    getenv('XENITH_TITLE')
                    ?: 'XenithPay',
                'description' =>
                    getenv('XENITH_DESCRIPTION')
                    ?: 'Pay securely using XenithPay payment gateway.',
                'icon' => '',
                'supports' => [
                    'products',
                ],
                'error' => null,
            ]);

            exit(0);

        case 'create_xenithpay_payment':
            $client =
                new XenithPayClient();

            $result =
                $client->createPayment(
                    $arguments
                );

            echo json_encode(
                $result,
                JSON_UNESCAPED_SLASHES |
                JSON_UNESCAPED_UNICODE |
                JSON_THROW_ON_ERROR
            );

            exit(0);

        default:
            echo json_encode([
                'success' => false,
                'error' =>
                    'Unknown tool: ' .
                    $tool,
            ]);

            exit(0);
    }
} catch (
    InvalidArgumentException |
    RuntimeException $exception
) {
    fwrite(
        STDERR,
        '[PHP] Runtime error: ' .
        $exception->getMessage() .
        PHP_EOL
    );

    echo json_encode([
        'success' => false,
        'error' =>
            $exception->getMessage(),
    ]);

    exit(0);
} catch (Throwable $exception) {
    fwrite(
        STDERR,
        '[PHP] Unexpected error: ' .
        $exception->getMessage() .
        PHP_EOL
    );

    echo json_encode([
        'success' => false,
        'error' =>
            'Unexpected error: ' .
            $exception->getMessage(),
    ]);

    exit(0);
}
