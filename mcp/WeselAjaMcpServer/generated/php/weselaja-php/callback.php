<?php
declare(strict_types=1);

require_once __DIR__ .
    '/src/EnvLoader.php';

require_once __DIR__ .
    '/src/WeselAjaSignature.php';

\WeselAja\EnvLoader::load();

$path =
    parse_url(
        $_SERVER['REQUEST_URI'] ?? '/',
        PHP_URL_PATH
    );

if (
    $path !==
    '/payment/weselaja/callback'
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

if (
    strtoupper(
        $_SERVER['REQUEST_METHOD'] ?? 'GET'
    ) !== 'POST'
) {
    http_response_code(405);

    header(
        'Content-Type: application/json'
    );

    echo json_encode(
        [
            'success' => false,
            'error' => 'Method not allowed'
        ],
        JSON_PRETTY_PRINT
    );

    exit;
}

$secret =
    getenv(
        'WEB_SIGNATURE_SECRET'
    );

if (
    $secret === false
    ||
    trim(
        $secret
    ) === ''
) {
    http_response_code(500);

    header(
        'Content-Type: application/json'
    );

    echo json_encode(
        [
            'success' => false,
            'error' =>
                'WEB_SIGNATURE_SECRET is required'
        ],
        JSON_PRETTY_PRINT
    );

    exit;
}

$timestamp =
    $_SERVER[
        'HTTP_X_MIDDLEWARE_TIMESTAMP'
    ]
    ??
    '';

$signature =
    $_SERVER[
        'HTTP_X_MIDDLEWARE_SIGNATURE'
    ]
    ??
    '';

if (
    trim(
        $timestamp
    ) === ''
) {
    http_response_code(400);

    header(
        'Content-Type: application/json'
    );

    echo json_encode(
        [
            'success' => false,
            'error' =>
                'Missing x-middleware-timestamp'
        ],
        JSON_PRETTY_PRINT
    );

    exit;
}

if (
    trim(
        $signature
    ) === ''
) {
    http_response_code(400);

    header(
        'Content-Type: application/json'
    );

    echo json_encode(
        [
            'success' => false,
            'error' =>
                'Missing x-middleware-signature'
        ],
        JSON_PRETTY_PRINT
    );

    exit;
}

$rawBody =
    file_get_contents(
        'php://input'
    );

if (
    !is_string(
        $rawBody
    )
) {
    $rawBody =
        '';
}

$valid =
    \WeselAja\WeselAjaSignature::verifyCallback(
        'POST',
        $path,
        $rawBody,
        $timestamp,
        $signature,
        $secret
    );

if (!$valid) {
    http_response_code(401);

    header(
        'Content-Type: application/json'
    );

    echo json_encode(
        [
            'success' => false,
            'verified' => false,
            'error' =>
                'Invalid callback signature'
        ],
        JSON_PRETTY_PRINT
    );

    exit;
}

$decoded =
    json_decode(
        $rawBody,
        true
    );

header(
    'Content-Type: application/json'
);

echo json_encode(
    [
        'success' => true,
        'verified' => true,
        'body' =>
            is_array(
                $decoded
            )
                ? $decoded
                : $rawBody
    ],
    JSON_PRETTY_PRINT |
    JSON_UNESCAPED_SLASHES |
    JSON_UNESCAPED_UNICODE
);