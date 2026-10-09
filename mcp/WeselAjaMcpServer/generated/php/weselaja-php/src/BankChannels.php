<?php
declare(strict_types=1);

namespace WeselAja;

final class BankChannels
{
    private const CHANNELS = [
        'BRI' =>
            'BRI.VA',

        'BANKBRI' =>
            'BRI.VA',

        'MANDIRI' =>
            'MDR.VA',

        'BANKMANDIRI' =>
            'MDR.VA',

        'MDR' =>
            'MDR.VA',

        'BNI' =>
            'BNI.VA',

        'BANKBNI' =>
            'BNI.VA',

        'PERMATA' =>
            'PTB.VA',

        'BANKPERMATA' =>
            'PTB.VA',

        'PTB' =>
            'PTB.VA',

        'CIMB' =>
            'CIMB.VA',

        'CIMBNIAGA' =>
            'CIMB.VA',

        'BANKCIMB' =>
            'CIMB.VA',

        'BCA' =>
            'BCA.VA',

        'BANKBCA' =>
            'BCA.VA',
    ];

    public static function resolve(
        string $bank
    ): string {
        if (
            trim($bank) === ''
        ) {
            throw new \InvalidArgumentException(
                'Bank is required.'
            );
        }

        $normalized =
            self::normalize(
                $bank
            );

        if (
            isset(
                self::CHANNELS[
                    $normalized
                ]
            )
        ) {
            return self::CHANNELS[
                $normalized
            ];
        }

        throw new \InvalidArgumentException(
            "Unsupported bank '{$bank}'. " .
            'Supported banks: ' .
            'BRI, Mandiri, BNI, Permata, CIMB, BCA.'
        );
    }

    public static function normalize(
        string $bank
    ): string {
        return str_replace(
            [
                ' ',
                '-',
                '_'
            ],
            '',
            strtoupper(
                trim($bank)
            )
        );
    }

    public static function supported(): array
    {
        return [
            'BRI' =>
                'BRI.VA',

            'Mandiri' =>
                'MDR.VA',

            'BNI' =>
                'BNI.VA',

            'Permata' =>
                'PTB.VA',

            'CIMB' =>
                'CIMB.VA',

            'BCA' =>
                'BCA.VA',
        ];
    }
}