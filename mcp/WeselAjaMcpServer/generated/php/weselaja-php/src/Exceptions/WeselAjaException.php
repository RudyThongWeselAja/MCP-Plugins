<?php
declare(strict_types=1);

namespace WeselAja\Exceptions;

class WeselAjaException
    extends \RuntimeException
{
}

final class
    WeselAjaConfigurationException
    extends WeselAjaException
{
}

final class
    WeselAjaApiException
    extends WeselAjaException
{
    public function __construct(
        string $message,
        public readonly ?int $statusCode = null,
        public readonly ?string $responseBody = null
    ) {
        parent::__construct(
            $message
        );
    }
}