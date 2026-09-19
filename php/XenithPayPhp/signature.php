<?php

declare(strict_types=1);

final class SignatureGenerator
{
    public function generate(
        string $method,
        string $uri,
        string $timestamp,
        string $body,
        string $secretKey
    ): string {
        $payload =
            $method . "\n" .
            $uri . "\n" .
            $timestamp . "\n" .
            $body;

        $hash = hash_hmac(
            'sha256',
            $payload,
            $secretKey,
            true
        );

        return base64_encode($hash);
    }
}