<?php
declare(strict_types=1);

namespace WeselAja;

final class EnvLoader
{
    public static function load(
        string $file = '.env'
    ): void {
        $path =
            self::findFile(
                $file
            );

        if ($path === null) {
            return;
        }

        $lines =
            file(
                $path,
                FILE_IGNORE_NEW_LINES
                | FILE_SKIP_EMPTY_LINES
            );

        if ($lines === false) {
            return;
        }

        foreach ($lines as $line) {
            $line =
                trim(
                    $line
                );

            if (
                $line === ''
                ||
                str_starts_with(
                    $line,
                    '#'
                )
            ) {
                continue;
            }

            $separator =
                strpos(
                    $line,
                    '='
                );

            if (
                $separator === false
                ||
                $separator === 0
            ) {
                continue;
            }

            $key =
                trim(
                    substr(
                        $line,
                        0,
                        $separator
                    )
                );

            $value =
                trim(
                    substr(
                        $line,
                        $separator + 1
                    )
                );

            $value =
                self::removeQuotes(
                    $value
                );

            if (
                getenv($key) === false
                ||
                trim(
                    (string) getenv(
                        $key
                    )
                ) === ''
            ) {
                putenv(
                    $key . '=' . $value
                );
            }
        }
    }

    private static function findFile(
        string $file
    ): ?string {
        $directory =
            getcwd();

        while (
            is_string(
                $directory
            )
            &&
            $directory !== ''
        ) {
            $candidate =
                $directory .
                DIRECTORY_SEPARATOR .
                $file;

            if (
                is_file(
                    $candidate
                )
            ) {
                return $candidate;
            }

            $parent =
                dirname(
                    $directory
                );

            if (
                $parent ===
                $directory
            ) {
                break;
            }

            $directory =
                $parent;
        }

        return null;
    }

    private static function removeQuotes(
        string $value
    ): string {
        if (
            strlen($value) >= 2
            &&
            (
                (
                    str_starts_with(
                        $value,
                        '"'
                    )
                    &&
                    str_ends_with(
                        $value,
                        '"'
                    )
                )
                ||
                (
                    str_starts_with(
                        $value,
                        "'"
                    )
                    &&
                    str_ends_with(
                        $value,
                        "'"
                    )
                )
            )
        ) {
            return substr(
                $value,
                1,
                -1
            );
        }

        return $value;
    }
}