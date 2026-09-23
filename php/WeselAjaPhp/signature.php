<?php

declare(strict_types=1);

final class WeselAjaSignature
{
    public function generate(
        string $method,
        string $uri,
        string $timestamp,
        string $body,
        string $secret
    ): string {
        $payload =
            $method . "\n" .
            $uri . "\n" .
            $timestamp . "\n" .
            $body;

        return base64_encode(
            hash_hmac(
                'sha256',
                $payload,
                $secret,
                true
            )
        );
    }
}