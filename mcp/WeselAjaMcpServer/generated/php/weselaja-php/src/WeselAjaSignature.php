<?php
declare(strict_types=1);

namespace WeselAja;

final class WeselAjaSignature
{
    public static function generate(
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

    public static function generateCallback(
        string $method,
        string $uri,
        string $rawBody,
        string $timestamp,
        string $secret
    ): string {
        $payload =
            $method . "\n" .
            $uri . "\n" .
            $rawBody . "\n" .
            $timestamp;

        return base64_encode(
            hash_hmac(
                'sha256',
                $payload,
                $secret,
                true
            )
        );
    }

    public static function verifyCallback(
        string $method,
        string $uri,
        string $rawBody,
        string $timestamp,
        string $signature,
        string $secret
    ): bool {
        if (
            trim(
                $signature
            ) === ''
        ) {
            return false;
        }

        $expected =
            self::generateCallback(
                $method,
                $uri,
                $rawBody,
                $timestamp,
                $secret
            );

        return hash_equals(
            $expected,
            trim(
                $signature
            )
        );
    }
}