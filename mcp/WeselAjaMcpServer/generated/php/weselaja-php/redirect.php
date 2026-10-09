<?php
declare(strict_types=1);

$path =
    parse_url(
        $_SERVER['REQUEST_URI'] ?? '/',
        PHP_URL_PATH
    );

if (
    $path !==
    '/payment/weselaja/return'
) {
    http_response_code(404);

    header(
        'Content-Type: application/json'
    );

    echo json_encode(
        [
            'success' => false,
            'error' => 'Not found'
        ],
        JSON_PRETTY_PRINT
    );

    exit;
}

header(
    'Content-Type: application/json'
);

echo json_encode(
    [
        'success' => true,
        'path' => $path,
        'parameters' => $_GET
    ],
    JSON_PRETTY_PRINT
);