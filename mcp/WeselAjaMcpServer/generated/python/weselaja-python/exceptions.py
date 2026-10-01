class WeselAjaError(Exception):
    pass


class WeselAjaConfigurationError(
    WeselAjaError
):
    pass


class WeselAjaApiError(
    WeselAjaError
):
    def __init__(
        self,
        message: str,
        *,
        status_code: int | None = None,
        details: object | None = None,
    ) -> None:

        super().__init__(
            message
        )

        self.status_code = status_code

        self.details = details