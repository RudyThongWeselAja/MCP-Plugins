<?php

declare(strict_types=1);

if (PHP_SAPI !== 'cli') {
    fwrite(
        STDERR,
        "CLI only\n"
    );

    exit(1);
}

require_once __DIR__ . '/client.php';

$input = stream_get_contents(STDIN);

if ($input === false) {
    echo json_encode([
        'success' => false,
        'error' => 'Unable to read STDIN.',
    ]);

    exit(0);
}

$input = preg_replace(
    '/^\xEF\xBB\xBF/',
    '',
    $input
);

$input = trim($input);

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
        '[WOO-MCP] JSON decode error: ' .
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

try {
    switch ($tool) {

        case 'is_weselaja_active':

            echo json_encode([
                'success' => true,
                'active' => true,
                'error' => null,
            ]);

            exit(0);

        case 'get_weselaja_payment_method':

            echo json_encode([
                'success' => true,

                'id' => 'weselaja',

                'title' =>
                    getenv('XENITH_TITLE')
                    ?: 'WeselAja',

                'description' =>
                    getenv('XENITH_DESCRIPTION')
                    ?: 'Pay securely using WeselAja payment gateway.',

                'icon' => '',

                'supports' => [
                    'products',
                ],

                'error' => null,
            ]);

            exit(0);

        case 'create_weselaja_payment':

            if (
                !isset($request['arguments']) ||
                !is_array($request['arguments'])
            ) {
                echo json_encode([
                    'success' => false,
                    'error' =>
                        'arguments must be an object.',
                ]);

                exit(0);
            }

            $client =
                new WeselAjaClient();

            $result =
                $client->createPayment(
                    $request['arguments']
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
    echo json_encode([
        'success' => false,
        'error' =>
            $exception->getMessage(),
    ]);

    exit(0);

} catch (Throwable $exception) {

    fwrite(
        STDERR,
        '[WOO-MCP] Unexpected error: ' .
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
