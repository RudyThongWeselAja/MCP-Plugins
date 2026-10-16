using System.Text;

namespace WeselAjaMcpServer.Generators;

public sealed class OdooWeselAjaGenerator
{
    public GenerationResult Generate(
        string projectName,
        string odooVersion,
        bool includeExample,
        bool includeTests,
        bool overwrite)
    {
        var moduleName = SanitizeModuleName(projectName);
        var version = NormalizeOdooVersion(odooVersion);

        var configuredOutputRoot =
            Environment.GetEnvironmentVariable(
                "WESELAJA_GENERATOR_OUTPUT_DIR");

        var outputRoot = string.IsNullOrWhiteSpace(configuredOutputRoot)
            ? Path.Combine(Directory.GetCurrentDirectory(), "generated")
            : configuredOutputRoot.Trim();

        outputRoot = Path.GetFullPath(outputRoot);

        var odooRoot = Path.GetFullPath(
            Path.Combine(outputRoot, "odoo"));

        var projectDirectory = Path.GetFullPath(
            Path.Combine(odooRoot, moduleName));

        EnsureInsideDirectory(projectDirectory, odooRoot);

        if (Directory.Exists(projectDirectory))
        {
            if (!overwrite)
            {
                throw new InvalidOperationException(
                    $"Generated project already exists: {projectDirectory}. " +
                    "Set overwrite=true to replace it.");
            }

            Directory.Delete(projectDirectory, recursive: true);
        }

        Directory.CreateDirectory(projectDirectory);

        var files = BuildFiles(includeExample, includeTests);
        var generatedFiles = new List<GeneratedFile>();

        foreach (var file in files)
        {
            var filePath = Path.GetFullPath(
                Path.Combine(projectDirectory, file.Key));

            EnsureInsideDirectory(filePath, projectDirectory);

            var directory = Path.GetDirectoryName(filePath);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var content = file.Value
                .Replace(
                    "__MODULE_NAME__",
                    moduleName,
                    StringComparison.Ordinal)
                .Replace(
                    "__ODOO_VERSION__",
                    version,
                    StringComparison.Ordinal);

            File.WriteAllText(
                filePath,
                content,
                new UTF8Encoding(false));

            generatedFiles.Add(
                new GeneratedFile
                {
                    Path = Path.GetRelativePath(
                        projectDirectory,
                        filePath),
                    Bytes = new FileInfo(filePath).Length
                });
        }

        return new GenerationResult(
            moduleName,
            projectDirectory,
            generatedFiles);
    }

    private static string NormalizeOdooVersion(string? version)
    {
        var normalized = string.IsNullOrWhiteSpace(version)
            ? "18.0"
            : version.Trim();

        if (!string.Equals(
            normalized,
            "18.0",
            StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "This generator supports Odoo 18.0 only.");
        }

        return normalized;
    }

    private static string SanitizeModuleName(string? projectName)
    {
        if (string.IsNullOrWhiteSpace(projectName) ||
            string.Equals(
                projectName.Trim(),
                "weselaja",
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                projectName.Trim(),
                "weselaja-integration",
                StringComparison.OrdinalIgnoreCase))
        {
            return "payment_weselaja";
        }

        var builder = new StringBuilder();
        var separatorPending = false;

        foreach (var character in projectName.Trim().ToLowerInvariant())
        {
            if (char.IsAsciiLetterOrDigit(character))
            {
                if (separatorPending && builder.Length > 0)
                {
                    builder.Append('_');
                }

                builder.Append(character);
                separatorPending = false;
            }
            else if (builder.Length > 0)
            {
                separatorPending = true;
            }
        }

        var result = builder.ToString().Trim('_');

        if (string.IsNullOrWhiteSpace(result))
        {
            result = "payment_weselaja";
        }

        if (!char.IsAsciiLetter(result[0]))
        {
            result = "payment_" + result;
        }

        if (result.Length > 64)
        {
            result = result[..64].TrimEnd('_');
        }

        return result;
    }

    private static void EnsureInsideDirectory(string path, string root)
    {
        var fullPath = Path.GetFullPath(path);
        var fullRoot = Path.GetFullPath(root);

        var relative = Path.GetRelativePath(fullRoot, fullPath);

        if (relative == ".." ||
            relative.StartsWith(
                ".." + Path.DirectorySeparatorChar,
                StringComparison.Ordinal) ||
            Path.IsPathRooted(relative))
        {
            throw new InvalidOperationException(
                "Generated project path is outside the configured directory.");
        }
    }

    private static Dictionary<string, string> BuildFiles(
        bool includeExample,
        bool includeTests)
    {
        var files = new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase)
        {
            ["__manifest__.py"] = ManifestTemplate,
            ["__init__.py"] = RootInitTemplate,
            ["models/__init__.py"] = ModelsInitTemplate,
            ["models/payment_provider.py"] = PaymentProviderTemplate,
            ["models/payment_transaction.py"] = PaymentTransactionTemplate,
            ["controllers/__init__.py"] = ControllersInitTemplate,
            ["controllers/main.py"] = ControllerTemplate,
            ["lib/__init__.py"] = LibInitTemplate,
            ["lib/weselaja_api.py"] = ApiClientTemplate,
            ["views/payment_provider_views.xml"] = ProviderViewsTemplate,
            ["views/payment_weselaja_templates.xml"] = PaymentTemplatesTemplate,
            ["views/payment_transaction_views.xml"] = TransactionViewsTemplate,
            ["data/payment_provider_data.xml"] = ProviderDataTemplate,
            [".gitignore"] = GitIgnoreTemplate,
            ["README.md"] = ReadmeTemplate
        };

        if (includeExample)
        {
            files["examples/payin_payload.json"] = ExamplePayloadTemplate;
        }

        if (includeTests)
        {
            files["tests/__init__.py"] = TestsInitTemplate;
            files["tests/test_weselaja.py"] = TestsTemplate;
        }

        return files;
    }

    private const string ManifestTemplate = """
    {
        'name': 'WeselAja Payment Provider',
        'summary': 'WeselAja Virtual Account payment integration',
        'version': '__ODOO_VERSION__.1.0.0',
        'category': 'Accounting/Payment Providers',
        'author': 'WeselAja Integration Generator',
        'license': 'LGPL-3',
        'depends': [
            'base',
            'payment',
            'portal',
            'website_payment',
            'website_sale',
        ],
        'external_dependencies': {
            'python': ['requests'],
        },
        'data': [
            'views/payment_weselaja_templates.xml',
            'views/payment_provider_views.xml',
            'views/payment_transaction_views.xml',
            'data/payment_provider_data.xml',
        ],
        'installable': True,
        'application': False,
        'auto_install': False,
    }
    """;

    private const string RootInitTemplate = """
    from . import models
    from . import controllers
    """;

    private const string ModelsInitTemplate = """
    from . import payment_provider
    from . import payment_transaction
    """;

    private const string ControllersInitTemplate = """
    from . import main
    """;

    private const string LibInitTemplate = """
    from . import weselaja_api
    """;

    private const string PaymentProviderTemplate = """
    from urllib.parse import urlsplit

    from odoo import _, api, fields, models
    from odoo.exceptions import ValidationError


    CALLBACK_PATH = '/payment/weselaja/webhook'


    class PaymentProvider(models.Model):
        _inherit = 'payment.provider'

        code = fields.Selection(
            selection_add=[('weselaja', 'WeselAja')],
            ondelete={'weselaja': 'set default'},
        )

        weselaja_api_key = fields.Char(
            string='API Key',
            required_if_provider='weselaja',
            copy=False,
            groups='base.group_system',
        )

        weselaja_secret_key = fields.Char(
            string='API Secret Key',
            required_if_provider='weselaja',
            copy=False,
            groups='base.group_system',
        )

        weselaja_customer_reference = fields.Char(
            string='Customer Reference',
            required_if_provider='weselaja',
            copy=False,
            groups='base.group_system',
        )

        weselaja_webhook_secret = fields.Char(
            string='Webhook Signature Secret',
            required_if_provider='weselaja',
            copy=False,
            groups='base.group_system',
        )

        weselaja_payment_channel = fields.Selection(
            selection=[
                ('BRI.VA', 'BRI Virtual Account'),
                ('MDR.VA', 'Mandiri Virtual Account'),
                ('BNI.VA', 'BNI Virtual Account'),
                ('PTB.VA', 'Permata Virtual Account'),
                ('CIMB.VA', 'CIMB Virtual Account'),
                ('BCA.VA', 'BCA Virtual Account'),
            ],
            string='Virtual Account Bank',
            default='BRI.VA',
            required_if_provider='weselaja',
            copy=False,
        )

        weselaja_sandbox_url = fields.Char(
            string='Sandbox API Base URL',
            default='https://sandbox.checkout.weselaja.id',
            required_if_provider='weselaja',
            copy=False,
            groups='base.group_system',
        )

        weselaja_production_url = fields.Char(
            string='Production API Base URL',
            default='https://checkout.weselaja.id',
            required_if_provider='weselaja',
            copy=False,
            groups='base.group_system',
        )

        weselaja_callback_url = fields.Char(
            string='Webhook URL Override',
            help=(
                "Optional public HTTPS webhook URL. Leave empty to use "
                "the public Odoo base URL."
            ),
            copy=False,
            groups='base.group_system',
        )

        @api.constrains(
            'weselaja_sandbox_url',
            'weselaja_production_url',
            'weselaja_callback_url',
        )
        def _check_weselaja_urls(self):
            for provider in self.filtered(
                lambda record: record.code == 'weselaja'
            ):
                urls = [
                    provider.weselaja_sandbox_url,
                    provider.weselaja_production_url,
                ]

                if provider.weselaja_callback_url:
                    urls.append(provider.weselaja_callback_url)

                for url in urls:
                    if not url:
                        continue

                    parsed = urlsplit(url.strip())

                    if (
                        parsed.scheme != 'https'
                        or not parsed.netloc
                        or parsed.query
                        or parsed.fragment
                    ):
                        raise ValidationError(_(
                            'WeselAja URLs must be valid HTTPS URLs without '
                            'query strings or fragments.'
                        ))

                callback_url = provider.weselaja_callback_url
                if callback_url:
                    callback_path = urlsplit(callback_url).path
                    if callback_path != CALLBACK_PATH:
                        raise ValidationError(_(
                            'The webhook URL path must be %s.',
                            CALLBACK_PATH,
                        ))

        def _get_weselaja_api_base_url(self):
            self.ensure_one()

            if self.state == 'test':
                base_url = self.weselaja_sandbox_url
            else:
                base_url = self.weselaja_production_url

            base_url = (base_url or '').strip().rstrip('/')
            parsed = urlsplit(base_url)

            if (
                parsed.scheme != 'https'
                or not parsed.netloc
                or parsed.query
                or parsed.fragment
            ):
                raise ValidationError(_(
                    'Configure a valid HTTPS WeselAja API base URL.'
                ))

            return base_url

        def _get_weselaja_callback_url(self):
            self.ensure_one()

            callback_url = (
                self.weselaja_callback_url.strip()
                if self.weselaja_callback_url
                else self.get_base_url().rstrip('/') + CALLBACK_PATH
            )

            parsed = urlsplit(callback_url)

            if (
                parsed.scheme != 'https'
                or not parsed.netloc
                or parsed.path != CALLBACK_PATH
                or parsed.query
                or parsed.fragment
            ):
                raise ValidationError(_(
                    'The webhook URL must use HTTPS and end with %s.',
                    CALLBACK_PATH,
                ))

            return callback_url

        def _get_supported_currencies(self):
            self.ensure_one()

            if self.code != 'weselaja':
                return super()._get_supported_currencies()

            return self.env['res.currency'].with_context(
                active_test=False
            ).search([
                ('name', '=', 'IDR'),
            ])

        def _get_default_payment_method_codes(self):
            self.ensure_one()
            default_codes = super()._get_default_payment_method_codes()

            if self.code == 'weselaja':
                return {'weselaja_va'}

            return default_codes

        def _get_removal_values(self):
            values = super()._get_removal_values()
            values.update({
                'weselaja_api_key': False,
                'weselaja_secret_key': False,
                'weselaja_customer_reference': False,
                'weselaja_webhook_secret': False,
            })
            return values
    """;

    private const string ApiClientTemplate = """
    import base64
    import hashlib
    import hmac
    import json
    import logging
    from datetime import datetime, timezone
    from urllib.parse import urlencode

    import requests

    from odoo import _
    from odoo.exceptions import ValidationError


    _logger = logging.getLogger(__name__)

    PAYINS_URI = '/v1/payins'


    def build_request_signature(secret, method, uri, timestamp, body):
        message = (
            method.upper()
            + '\n'
            + uri
            + '\n'
            + timestamp
            + '\n'
            + body
        )

        digest = hmac.new(
            secret.encode('utf-8'),
            message.encode('utf-8'),
            hashlib.sha256,
        ).digest()

        return base64.b64encode(digest).decode('ascii')


    def build_webhook_signature(secret, method, path, raw_body, timestamp):
        message = (
            method.upper().encode('utf-8')
            + b'\n'
            + path.encode('utf-8')
            + b'\n'
            + raw_body
            + b'\n'
            + timestamp.encode('utf-8')
        )

        digest = hmac.new(
            secret.encode('utf-8'),
            message,
            hashlib.sha256,
        ).digest()

        return base64.b64encode(digest).decode('ascii')


    def is_webhook_timestamp_valid(timestamp, now=None):
        if not timestamp:
            return False

        try:
            parsed_timestamp = datetime.fromisoformat(
                timestamp.replace('Z', '+00:00')
            )
        except (AttributeError, ValueError):
            return False

        if parsed_timestamp.tzinfo is None:
            parsed_timestamp = parsed_timestamp.replace(
                tzinfo=timezone.utc
            )

        current_time = now or datetime.now(timezone.utc)

        age_seconds = abs((
            current_time.astimezone(timezone.utc)
            - parsed_timestamp.astimezone(timezone.utc)
        ).total_seconds())

        return age_seconds <= 300


    def map_payin_status(status):
        if not isinstance(status, str):
            return None

        return {
            'SUCCESS': 'done',
            'PAID': 'done',
            'COMPLETED': 'done',
            'SETTLED': 'done',
            'FAILED': 'error',
            'DECLINED': 'error',
            'REJECTED': 'error',
            'EXPIRED': 'cancel',
            'CANCELLED': 'cancel',
            'CANCELED': 'cancel',
            'PENDING': 'pending',
            'PROCESSING': 'pending',
            'INITIATED': 'pending',
        }.get(status.strip().upper())


    def extract_payins(response_data):
        if isinstance(response_data, list):
            return [
                item for item in response_data
                if isinstance(item, dict)
            ]

        if not isinstance(response_data, dict):
            return []

        for key in ('data', 'items', 'results', 'payins', 'records'):
            value = response_data.get(key)

            if isinstance(value, list):
                return [
                    item for item in value
                    if isinstance(item, dict)
                ]

            if isinstance(value, dict):
                nested = extract_payins(value)
                if nested:
                    return nested

        return []


    def unwrap_payin(response_data):
        current = response_data

        for _ in range(4):
            if not isinstance(current, dict):
                break

            nested = None

            for key in ('data', 'payin', 'payment', 'result'):
                value = current.get(key)
                if isinstance(value, dict):
                    nested = value
                    break

            if nested is None:
                break

            current = nested

        if not isinstance(current, dict):
            raise ValidationError(_(
                'WeselAja returned an unexpected pay-in response.'
            ))

        return current


    class WeselAjaApi:
        def __init__(self, provider):
            self.provider = provider

        def _request(
            self,
            method,
            uri,
            payload=None,
            idempotency_key=None,
            signature_uri=None,
        ):
            method = method.upper()

            if not self.provider.weselaja_api_key:
                raise ValidationError(_(
                    'Configure the WeselAja API Key first.'
                ))

            if not self.provider.weselaja_secret_key:
                raise ValidationError(_(
                    'Configure the WeselAja API Secret Key first.'
                ))

            raw_body = ''

            if payload is not None:
                raw_body = json.dumps(
                    payload,
                    separators=(',', ':'),
                    ensure_ascii=False,
                )

            timestamp = (
                datetime.now(timezone.utc)
                .isoformat(timespec='milliseconds')
                .replace('+00:00', 'Z')
            )

            canonical_uri = signature_uri or uri

            signature = build_request_signature(
                self.provider.weselaja_secret_key,
                method,
                canonical_uri,
                timestamp,
                raw_body,
            )

            headers = {
                'Accept': 'application/json',
                'Xenith-Api-Key': self.provider.weselaja_api_key,
                'Xenith-Request-Timestamp': timestamp,
                'Xenith-Request-Signature': signature,
            }

            if payload is not None:
                headers['Content-Type'] = 'application/json'

            if idempotency_key:
                headers['X-Idempotency-Key'] = idempotency_key

            url = self.provider._get_weselaja_api_base_url() + uri

            try:
                response = requests.request(
                    method,
                    url,
                    data=(
                        raw_body.encode('utf-8')
                        if payload is not None
                        else None
                    ),
                    headers=headers,
                    timeout=(10, 30),
                    allow_redirects=False,
                )
            except requests.RequestException as error:
                _logger.warning(
                    'Could not connect to WeselAja API using %s.',
                    method,
                )
                raise ValidationError(_(
                    'Could not connect to WeselAja. Check the API URL '
                    'and server network connection.'
                )) from error

            try:
                response_data = response.json()
            except ValueError:
                response_data = {}

            if not response.ok:
                message = 'The payment provider rejected the request.'

                if isinstance(response_data, dict):
                    message = (
                        response_data.get('message')
                        or response_data.get('error')
                        or message
                    )

                _logger.warning(
                    'WeselAja API returned HTTP %s.',
                    response.status_code,
                )

                raise ValidationError(_(
                    'WeselAja API returned HTTP %s: %s',
                    response.status_code,
                    str(message)[:500],
                ))

            if not isinstance(response_data, (dict, list)):
                raise ValidationError(_(
                    'WeselAja returned an unexpected response format.'
                ))

            return response_data

        def create_payin(self, payload, idempotency_key):
            response_data = self._request(
                'POST',
                PAYINS_URI,
                payload=payload,
                idempotency_key=idempotency_key,
                signature_uri=PAYINS_URI,
            )

            return unwrap_payin(response_data)

        def get_payins(self, status=None, limit=100):
            limit = max(1, min(int(limit), 100))
            params = {'limit': limit}

            if status:
                params['status'] = str(status).strip().upper()

            request_uri = PAYINS_URI + '?' + urlencode(params)

            return self._request(
                'GET',
                request_uri,
                signature_uri=PAYINS_URI,
            )
    """;

    private const string PaymentTransactionTemplate = """
    from urllib.parse import urlencode

    from odoo import _, api, fields, models
    from odoo.exceptions import UserError, ValidationError

    from odoo.addons.payment import utils as payment_utils
    from odoo.addons.__MODULE_NAME__.lib.weselaja_api import (
        WeselAjaApi,
        extract_payins,
        map_payin_status,
    )


    class PaymentTransaction(models.Model):
        _inherit = 'payment.transaction'

        weselaja_payment_id = fields.Char(
            string='WeselAja Payment ID',
            copy=False,
            readonly=True,
        )

        weselaja_payment_code = fields.Char(
            string='Payment Code / Virtual Account',
            copy=False,
            readonly=True,
        )

        weselaja_payment_channel = fields.Char(
            string='WeselAja Payment Channel',
            copy=False,
            readonly=True,
        )

        weselaja_status = fields.Char(
            string='WeselAja Status',
            copy=False,
            readonly=True,
        )

        def _get_specific_processing_values(self, processing_values):
            values = super()._get_specific_processing_values(
                processing_values
            )

            if self.provider_code != 'weselaja':
                return values

            return {
                **values,
                'access_token': payment_utils.generate_access_token(
                    self.reference,
                    self.amount,
                ),
            }

        def _get_specific_rendering_values(self, processing_values):
            values = super()._get_specific_rendering_values(
                processing_values
            )

            if self.provider_code != 'weselaja':
                return values

            self.ensure_one()

            if self.currency_id.name != 'IDR':
                raise ValidationError(_(
                    'WeselAja Virtual Account transactions must use IDR.'
                ))

            provider = self.provider_id
            base_url = provider.get_base_url().rstrip('/')
            access_token = payment_utils.generate_access_token(
                self.reference,
                self.amount,
            )

            redirect_url = (
                base_url
                + '/payment/weselaja/return?'
                + urlencode({
                    'reference': self.reference,
                    'access_token': access_token,
                })
            )

            if not self.weselaja_payment_code:
                customer_name = (
                    self.partner_name
                    or self.partner_id.name
                    or ''
                ).strip()

                if not customer_name:
                    raise ValidationError(_(
                        'A customer name is required for WeselAja.'
                    ))

                if not provider.weselaja_customer_reference:
                    raise ValidationError(_(
                        'Configure the WeselAja Customer Reference '
                        'in the payment provider settings.'
                    ))

                payload = {
                    'initiatedAmount': int(
                        self.currency_id.round(self.amount)
                    ),
                    'currency': self.currency_id.name,
                    'paymentMethod': 'VIRTUAL_ACCOUNT',
                    'paymentChannel': provider.weselaja_payment_channel,
                    'referenceCode': self.reference,
                    'customerReference':
                        provider.weselaja_customer_reference,
                    'customerName': customer_name,
                    'callbackUrl': provider._get_weselaja_callback_url(),
                    'redirectUrl': redirect_url,
                    'description': self.reference,
                }

                if self.partner_phone:
                    payload['customerPhoneNumber'] = self.partner_phone

                payin = WeselAjaApi(provider).create_payin(
                    payload,
                    payment_utils.generate_idempotency_key(
                        self,
                        scope='/v1/payins',
                    ),
                )

                payment_code = (
                    payin.get('paymentCode')
                    or payin.get('payment_code')
                    or payin.get('virtualAccountNumber')
                )

                if not payment_code:
                    raise ValidationError(_(
                        'WeselAja did not return a payment code.'
                    ))

                payment_id = (
                    payin.get('id')
                    or payin.get('paymentId')
                    or payin.get('payinId')
                )

                write_values = {
                    'weselaja_payment_id': (
                        str(payment_id) if payment_id else False
                    ),
                    'weselaja_payment_code': str(payment_code),
                    'weselaja_payment_channel': (
                        payin.get('paymentChannel')
                        or provider.weselaja_payment_channel
                    ),
                    'weselaja_status': payin.get('status') or 'PENDING',
                }

                if payment_id:
                    write_values['provider_reference'] = str(payment_id)

                self.write(write_values)

                if self.state == 'draft':
                    self._set_pending()

            return {
                'instructions_url': (
                    base_url + '/payment/weselaja/instructions'
                ),
                'reference': self.reference,
                'access_token': access_token,
            }

        @api.model
        def _get_tx_from_notification_data(
            self,
            provider_code,
            notification_data,
        ):
            if provider_code != 'weselaja':
                return super()._get_tx_from_notification_data(
                    provider_code,
                    notification_data,
                )

            reference = (
                notification_data.get('referenceCode')
                or notification_data.get('reference')
                or notification_data.get('merchantReference')
            )

            payment_id = (
                notification_data.get('id')
                or notification_data.get('paymentId')
                or notification_data.get('payinId')
            )

            provider_id = notification_data.get('_weselaja_provider_id')

            if not reference and not payment_id:
                raise ValidationError(_(
                    'WeselAja notification is missing its transaction '
                    'reference and payment ID.'
                ))

            domain = [('provider_code', '=', 'weselaja')]

            if provider_id:
                try:
                    provider_id = int(provider_id)
                except (TypeError, ValueError) as error:
                    raise ValidationError(_(
                        'Invalid WeselAja provider identifier.'
                    )) from error

                domain.append(('provider_id', '=', provider_id))

            tx = self.browse()

            if reference:
                tx = self.search([
                    *domain,
                    ('reference', '=', reference),
                ], limit=1)

            if not tx and payment_id:
                tx = self.search([
                    *domain,
                    '|',
                    ('weselaja_payment_id', '=', str(payment_id)),
                    ('provider_reference', '=', str(payment_id)),
                ], limit=1)

            if not tx:
                raise ValidationError(_(
                    'No WeselAja transaction matches the supplied '
                    'reference or payment ID.'
                ))

            if (
                tx.weselaja_payment_id
                and payment_id
                and str(tx.weselaja_payment_id) != str(payment_id)
            ):
                raise ValidationError(_(
                    'WeselAja payment ID does not match the transaction.'
                ))

            if (
                tx.provider_reference
                and payment_id
                and str(tx.provider_reference) != str(payment_id)
            ):
                raise ValidationError(_(
                    'WeselAja provider reference does not match the transaction.'
                ))

            return tx

        def _process_notification_data(self, notification_data):
            super()._process_notification_data(notification_data)

            if self.provider_code != 'weselaja':
                return

            self.ensure_one()

            reference = (
                notification_data.get('referenceCode')
                or notification_data.get('reference')
                or notification_data.get('merchantReference')
            )

            if reference and reference != self.reference:
                raise ValidationError(_(
                    'WeselAja transaction reference does not match.'
                ))

            payment_id = (
                notification_data.get('id')
                or notification_data.get('paymentId')
                or notification_data.get('payinId')
            )

            if (
                self.weselaja_payment_id
                and payment_id
                and str(self.weselaja_payment_id) != str(payment_id)
            ):
                raise ValidationError(_(
                    'WeselAja payment ID does not match the transaction.'
                ))

            amount_data = (
                notification_data.get('initiatedAmount')
                or notification_data.get('paymentAmount')
                or notification_data.get('amount')
            )
            currency_code = notification_data.get('currency')

            if isinstance(amount_data, dict):
                currency_code = (
                    amount_data.get('currency') or currency_code
                )
                amount_data = (
                    amount_data.get('value')
                    if amount_data.get('value') is not None
                    else amount_data.get('amount')
                )

            if amount_data is not None or currency_code:
                if amount_data is None or not currency_code:
                    raise ValidationError(_(
                        'WeselAja notification contains incomplete amount data.'
                    ))

                try:
                    received_amount = float(amount_data)
                except (TypeError, ValueError) as error:
                    raise ValidationError(_(
                        'WeselAja notification contains an invalid amount.'
                    )) from error

                if (
                    currency_code != self.currency_id.name
                    or self.currency_id.compare_amounts(
                        received_amount,
                        self.amount,
                    ) != 0
                ):
                    raise ValidationError(_(
                        'WeselAja notification amount or currency does not '
                        'match the transaction.'
                    ))

            status = str(
                notification_data.get('status') or ''
            ).strip().upper()

            if not status:
                raise ValidationError(_(
                    'WeselAja notification is missing its payment status.'
                ))

            transaction_state = map_payin_status(status)

            if transaction_state is None:
                raise ValidationError(_(
                    'WeselAja returned an unsupported payment status: %s',
                    status,
                ))

            if self.state in ('done', 'cancel'):
                return

            values = {}

            if payment_id:
                values['provider_reference'] = str(payment_id)
                values['weselaja_payment_id'] = str(payment_id)

            payment_code = (
                notification_data.get('paymentCode')
                or notification_data.get('payment_code')
                or notification_data.get('virtualAccountNumber')
            )

            if payment_code:
                values['weselaja_payment_code'] = str(payment_code)

            channel = notification_data.get('paymentChannel')
            if channel:
                values['weselaja_payment_channel'] = channel

            values['weselaja_status'] = status
            self.write(values)

            if transaction_state == 'done':
                self._set_done()
            elif transaction_state == 'error':
                if self.state != 'error':
                    self._set_error(_(
                        'WeselAja reports that the payment failed.'
                    ))
            elif transaction_state == 'cancel':
                if self.state in ('draft', 'pending', 'authorized'):
                    self._set_canceled()
            elif transaction_state == 'pending':
                if self.state == 'draft':
                    self._set_pending()

        def _refresh_weselaja_status(self):
            self.ensure_one()
            payment_utils.check_rights_on_recordset(self)

            if self.provider_code != 'weselaja':
                raise UserError(_(
                    'This transaction does not use WeselAja.'
                ))

            response_data = WeselAjaApi(
                self.provider_id
            ).get_payins(limit=100)

            matching_payin = next(
                (
                    item
                    for item in extract_payins(response_data)
                    if (
                        item.get('referenceCode') == self.reference
                        or item.get('reference') == self.reference
                        or (
                            self.weselaja_payment_id
                            and str(
                                item.get('id')
                                or item.get('paymentId')
                                or item.get('payinId')
                                or ''
                            ) == str(self.weselaja_payment_id)
                        )
                    )
                ),
                None,
            )

            if not matching_payin:
                raise UserError(_(
                    'The transaction was not found in the latest '
                    '100 WeselAja pay-in records.'
                ))

            notification_data = dict(matching_payin)
            notification_data['_weselaja_provider_id'] = self.provider_id.id

            self.env['payment.transaction'].sudo()._handle_notification_data(
                'weselaja',
                notification_data,
            )

            return matching_payin

        def action_weselaja_refresh_status(self):
            self.ensure_one()
            self._refresh_weselaja_status()

            return {
                'type': 'ir.actions.client',
                'tag': 'reload',
            }
    """;

    private const string ControllerTemplate = """
    import hmac
    import json
    import logging
    from urllib.parse import urlencode

    from odoo import http
    from odoo.http import request
    from odoo.exceptions import UserError, ValidationError
    from odoo.addons.payment import utils as payment_utils

    from odoo.addons.__MODULE_NAME__.lib.weselaja_api import (
        build_webhook_signature,
        is_webhook_timestamp_valid,
    )


    _logger = logging.getLogger(__name__)

    CALLBACK_PATH = '/payment/weselaja/webhook'

    BANK_NAMES = {
        'BRI.VA': 'BRI',
        'MDR.VA': 'Mandiri',
        'BNI.VA': 'BNI',
        'PTB.VA': 'Permata',
        'CIMB.VA': 'CIMB',
        'BCA.VA': 'BCA',
    }


    def _make_response(body, status):
        return request.make_response(
            body,
            headers=[('Content-Type', 'text/plain; charset=utf-8')],
            status=status,
        )


    def _has_valid_access_token(tx, access_token):
        return bool(
            access_token
            and payment_utils.check_access_token(
                access_token,
                tx.reference,
                tx.amount,
            )
        )


    def _render_instructions(
        tx,
        access_token,
        status_refresh_error=False,
    ):
        status_query = urlencode({
            'reference': tx.reference,
            'access_token': access_token,
        })

        return request.render(
            '__MODULE_NAME__.payment_instructions',
            {
                'tx': tx,
                'bank_name': BANK_NAMES.get(
                    tx.weselaja_payment_channel,
                    tx.weselaja_payment_channel or '',
                ),
                'status_url': (
                    '/payment/weselaja/status?' + status_query
                ),
                'status_refresh_error': status_refresh_error,
            },
        )


    class PaymentWeselajaController(http.Controller):

        @http.route(
            CALLBACK_PATH,
            type='http',
            auth='public',
            methods=['POST'],
            csrf=False,
            save_session=False,
        )
        def weselaja_webhook(self, **kwargs):
            raw_body = request.httprequest.get_data(cache=True)

            providers = request.env['payment.provider'].sudo().search([
                ('code', '=', 'weselaja'),
                ('weselaja_webhook_secret', '!=', False),
            ])

            if not providers:
                _logger.error(
                    'No WeselAja provider has a webhook secret configured.'
                )
                return _make_response(
                    'Webhook secret is not configured',
                    500,
                )

            timestamp = request.httprequest.headers.get(
                'x-middleware-timestamp',
                '',
            )
            signature = request.httprequest.headers.get(
                'x-middleware-signature',
                '',
            )

            if not timestamp or not signature:
                return _make_response('Missing signature headers', 401)

            if not is_webhook_timestamp_valid(timestamp):
                return _make_response('Invalid or expired timestamp', 401)

            signature_uri = CALLBACK_PATH
            query_string = request.httprequest.query_string
            if query_string:
                signature_uri += '?' + query_string.decode('ascii')

            provider = request.env['payment.provider']

            for candidate in providers:
                expected_signature = build_webhook_signature(
                    candidate.weselaja_webhook_secret,
                    'POST',
                    signature_uri,
                    raw_body,
                    timestamp,
                )

                if hmac.compare_digest(
                    expected_signature,
                    signature.strip(),
                ):
                    provider = candidate
                    break

            if not provider:
                _logger.warning(
                    'WeselAja webhook signature validation failed.'
                )
                return _make_response('Invalid signature', 401)

            try:
                payload = json.loads(raw_body.decode('utf-8'))
            except (UnicodeDecodeError, json.JSONDecodeError):
                return _make_response('Invalid JSON', 400)

            if not isinstance(payload, dict):
                return _make_response('Invalid payload', 400)

            data = payload.get('data', payload)

            if not isinstance(data, dict):
                return _make_response('Invalid transaction data', 400)

            if isinstance(data.get('payin'), dict):
                data = data['payin']
            elif isinstance(data.get('payment'), dict):
                data = data['payment']

            data = dict(data)
            data['_weselaja_provider_id'] = provider.id

            reference = (
                data.get('referenceCode')
                or data.get('reference')
                or data.get('merchantReference')
            )

            try:
                request.env['payment.transaction'].sudo()._handle_notification_data(
                    'weselaja',
                    data,
                )
            except (ValidationError, UserError):
                _logger.warning(
                    'WeselAja callback failed transaction validation for %s.',
                    reference or '(missing reference)',
                )
                return _make_response(
                    'Invalid transaction notification',
                    400,
                )
            except Exception:
                _logger.exception(
                    'Unexpected error processing WeselAja callback for %s.',
                    reference or '(missing reference)',
                )
                return _make_response('Processing error', 500)

            return _make_response('OK', 200)

        @http.route(
            '/payment/weselaja/instructions',
            type='http',
            auth='public',
            methods=['GET'],
            csrf=False,
            save_session=False,
        )
        def weselaja_instructions(
            self,
            reference=None,
            access_token=None,
            **kwargs,
        ):
            if not reference or not access_token:
                return request.not_found()

            tx = request.env['payment.transaction'].sudo().search([
                ('reference', '=', reference),
                ('provider_code', '=', 'weselaja'),
            ], limit=1)

            if not tx or not _has_valid_access_token(tx, access_token):
                return request.not_found()

            return _render_instructions(tx, access_token)

        @http.route(
            '/payment/weselaja/status',
            type='http',
            auth='public',
            methods=['GET'],
            csrf=False,
            save_session=False,
        )
        def weselaja_status(
            self,
            reference=None,
            access_token=None,
            **kwargs,
        ):
            if not reference or not access_token:
                return request.not_found()

            tx = request.env['payment.transaction'].sudo().search([
                ('reference', '=', reference),
                ('provider_code', '=', 'weselaja'),
            ], limit=1)

            if not tx or not _has_valid_access_token(tx, access_token):
                return request.not_found()

            refresh_failed = False

            try:
                tx._refresh_weselaja_status()
            except (UserError, ValidationError):
                refresh_failed = True
                _logger.warning(
                    'Could not refresh WeselAja status for %s.',
                    tx.reference,
                )

            return _render_instructions(
                tx,
                access_token,
                status_refresh_error=refresh_failed,
            )

        @http.route(
            '/payment/weselaja/return',
            type='http',
            auth='public',
            methods=['GET'],
            csrf=False,
            save_session=False,
        )
        def weselaja_return(
            self,
            reference=None,
            access_token=None,
            **kwargs,
        ):
            if not reference or not access_token:
                return request.not_found()

            tx = request.env['payment.transaction'].sudo().search([
                ('reference', '=', reference),
                ('provider_code', '=', 'weselaja'),
            ], limit=1)

            if not tx or not _has_valid_access_token(tx, access_token):
                return request.not_found()

            status_query = urlencode({
                'reference': tx.reference,
                'access_token': access_token,
            })

            return request.redirect(
                '/payment/weselaja/status?' + status_query
            )
    """;

    private const string ProviderViewsTemplate = """
    <?xml version="1.0" encoding="utf-8"?>
    <odoo>
        <record id="payment_provider_form_weselaja" model="ir.ui.view">
            <field name="name">WeselAja Provider Form</field>
            <field name="model">payment.provider</field>
            <field name="inherit_id" ref="payment.payment_provider_form"/>
            <field name="arch" type="xml">
                <group name="provider_credentials" position="inside">
                    <group invisible="code != 'weselaja'">
                        <field
                            name="weselaja_api_key"
                            required="code == 'weselaja' and state != 'disabled'"
                        />
                        <field
                            name="weselaja_secret_key"
                            password="True"
                            required="code == 'weselaja' and state != 'disabled'"
                        />
                        <field
                            name="weselaja_customer_reference"
                            required="code == 'weselaja' and state != 'disabled'"
                        />
                        <field
                            name="weselaja_webhook_secret"
                            password="True"
                            required="code == 'weselaja' and state != 'disabled'"
                        />
                        <field
                            name="weselaja_payment_channel"
                            required="code == 'weselaja' and state != 'disabled'"
                        />
                    </group>
                </group>
                <group name="provider_config" position="inside">
                    <group
                        string="WeselAja API URLs"
                        invisible="code != 'weselaja'"
                    >
                        <field
                            name="weselaja_sandbox_url"
                            required="code == 'weselaja' and state != 'disabled'"
                        />
                        <field
                            name="weselaja_production_url"
                            required="code == 'weselaja' and state != 'disabled'"
                        />
                        <field name="weselaja_callback_url"/>
                    </group>
                </group>
            </field>
        </record>
    </odoo>
    """;

    private const string PaymentTemplatesTemplate = """
    <?xml version="1.0" encoding="utf-8"?>
    <odoo>
        <template id="redirect_form">
            <form t-att-action="instructions_url" method="get">
                <input
                    type="hidden"
                    name="reference"
                    t-att-value="reference"
                />
                <input
                    type="hidden"
                    name="access_token"
                    t-att-value="access_token"
                />
                <button type="submit" class="btn btn-primary">
                    Continue to payment instructions
                </button>
            </form>
        </template>

        <template id="payment_instructions">
            <t t-call="portal.frontend_layout">
                <t t-set="page_title">WeselAja Payment Instructions</t>
                <div id="wrap" class="oe_structure">
                    <div class="container my-5">
                        <div class="row justify-content-center">
                            <div class="col-12 col-lg-8">
                                <h1>Payment Instructions</h1>
                                <p>
                                    Complete the bank transfer using the
                                    Virtual Account details below.
                                </p>

                                <div class="card my-4">
                                    <div class="card-body">
                                        <div class="mb-3">
                                            <strong>Order Reference</strong>
                                            <div>
                                                <span t-out="tx.reference"/>
                                            </div>
                                        </div>

                                        <div class="mb-3">
                                            <strong>Amount</strong>
                                            <div>
                                                <span
                                                    t-out="tx.amount"
                                                    t-options="{'widget': 'monetary', 'display_currency': tx.currency_id}"
                                                />
                                            </div>
                                        </div>

                                        <div class="mb-3">
                                            <strong>Bank</strong>
                                            <div>
                                                <span t-out="bank_name"/>
                                            </div>
                                        </div>

                                        <div class="mb-3">
                                            <strong>Payment Code / Virtual Account</strong>
                                            <div class="fs-4 fw-bold">
                                                <code
                                                    t-out="tx.weselaja_payment_code or ''"
                                                />
                                            </div>
                                        </div>

                                        <div class="mb-3">
                                            <strong>Payment Status</strong>
                                            <div>
                                                <span t-out="tx.weselaja_status or 'PENDING'"/>
                                            </div>
                                        </div>
                                    </div>
                                </div>

                                <div
                                    t-if="status_refresh_error"
                                    class="alert alert-warning"
                                    role="alert"
                                >
                                    The current status could not be refreshed.
                                    Please try again later.
                                </div>

                                <div class="alert alert-info" role="alert">
                                    Your payment status will be updated after
                                    WeselAja confirms the transaction.
                                    Returning to this page does not itself
                                    confirm a successful payment.
                                </div>

                                <a
                                    t-att-href="status_url"
                                    class="btn btn-primary"
                                >
                                    Check Payment Status
                                </a>
                            </div>
                        </div>
                    </div>
                </div>
            </t>
        </template>
    </odoo>
    """;

    private const string TransactionViewsTemplate = """
    <?xml version="1.0" encoding="utf-8"?>
    <odoo>
        <record id="payment_transaction_form_weselaja" model="ir.ui.view">
            <field name="name">WeselAja Transaction Form</field>
            <field name="model">payment.transaction</field>
            <field name="inherit_id" ref="payment.payment_transaction_form"/>
            <field name="arch" type="xml">
                <xpath expr="//form/header" position="inside">
                    <button
                        name="action_weselaja_refresh_status"
                        type="object"
                        string="Refresh WeselAja Status"
                        class="oe_highlight"
                        groups="base.group_system"
                        invisible="provider_code != 'weselaja'"
                    />
                </xpath>
                <xpath expr="//sheet" position="inside">
                    <group
                        string="WeselAja Payment Details"
                        invisible="provider_code != 'weselaja'"
                    >
                        <field name="weselaja_payment_id"/>
                        <field name="weselaja_payment_code"/>
                        <field name="weselaja_payment_channel"/>
                        <field name="weselaja_status"/>
                    </group>
                </xpath>
            </field>
        </record>
    </odoo>
    """;

    private const string ProviderDataTemplate = """
    <?xml version="1.0" encoding="utf-8"?>
    <odoo noupdate="1">
        <record id="payment_provider_weselaja" model="payment.provider">
            <field name="name">WeselAja</field>
            <field name="code">weselaja</field>
            <field name="sequence">350</field>
            <field name="redirect_form_view_id" ref="redirect_form"/>
        </record>

        <record id="payment_method_weselaja_va" model="payment.method">
            <field name="name">WeselAja Virtual Account</field>
            <field name="code">weselaja_va</field>
            <field name="sequence">350</field>
            <field name="active">True</field>
            <field name="image" type="base64" file="payment/static/img/bank.png"/>
            <field name="support_tokenization">False</field>
            <field name="support_express_checkout">False</field>
            <field name="support_refund">none</field>
            <field name="supported_currency_ids"
                   eval="[(6, 0, [ref('base.IDR')])]"/>
            <field name="provider_ids"
                   eval="[(4, ref('payment_provider_weselaja'))]"/>
        </record>
    </odoo>
    """;

    private const string GitIgnoreTemplate = """
    __pycache__/
    *.py[cod]
    .pytest_cache/
    .coverage
    """;

    private const string ExamplePayloadTemplate = """
    {
      "initiatedAmount": 10000,
      "currency": "IDR",
      "paymentMethod": "VIRTUAL_ACCOUNT",
      "paymentChannel": "BRI.VA",
      "referenceCode": "ODOO-EXAMPLE-001",
      "customerReference": "CUSTOMER-001",
      "customerName": "Example Customer",
      "callbackUrl": "https://your-public-domain.example/payment/weselaja/webhook",
      "redirectUrl": "https://your-public-domain.example/payment/weselaja/return"
    }
    """;

    private const string TestsInitTemplate = """
    from . import test_weselaja
    """;

    private const string TestsTemplate = """
    from datetime import datetime, timezone

    from odoo.tests import TransactionCase, tagged

    from odoo.addons.__MODULE_NAME__.lib.weselaja_api import (
        build_request_signature,
        build_webhook_signature,
        extract_payins,
        is_webhook_timestamp_valid,
        map_payin_status,
        unwrap_payin,
    )


    @tagged('post_install', '-at_install')
    class TestWeselAjaHelpers(TransactionCase):

        def test_request_signature_is_deterministic(self):
            first = build_request_signature(
                'test-secret',
                'POST',
                '/v1/payins',
                '2026-10-14T00:00:00.000Z',
                '{"x":1}',
            )

            second = build_request_signature(
                'test-secret',
                'POST',
                '/v1/payins',
                '2026-10-14T00:00:00.000Z',
                '{"x":1}',
            )

            changed_body = build_request_signature(
                'test-secret',
                'POST',
                '/v1/payins',
                '2026-10-14T00:00:00.000Z',
                '{"x":2}',
            )

            self.assertEqual(first, second)
            self.assertNotEqual(first, changed_body)

        def test_webhook_signature_uses_exact_raw_body(self):
            raw_body = b'{"status":"PAID"}'

            signature = build_webhook_signature(
                'test-webhook-secret',
                'POST',
                '/payment/weselaja/webhook',
                raw_body,
                '2026-10-14T00:00:00.000Z',
            )

            self.assertEqual(
                signature,
                build_webhook_signature(
                    'test-webhook-secret',
                    'POST',
                    '/payment/weselaja/webhook',
                    raw_body,
                    '2026-10-14T00:00:00.000Z',
                ),
            )

            self.assertNotEqual(
                signature,
                build_webhook_signature(
                    'test-webhook-secret',
                    'POST',
                    '/payment/weselaja/webhook',
                    b'{"status": "PAID"}',
                    '2026-10-14T00:00:00.000Z',
                ),
            )

        def test_webhook_timestamp_window(self):
            now = datetime(
                2026, 10, 14, 0, 0,
                tzinfo=timezone.utc,
            )

            self.assertTrue(is_webhook_timestamp_valid(
                '2026-10-14T00:04:59Z',
                now,
            ))

            self.assertFalse(is_webhook_timestamp_valid(
                '2026-10-14T00:05:01Z',
                now,
            ))

            self.assertFalse(is_webhook_timestamp_valid(
                'not-a-timestamp',
                now,
            ))

        def test_extract_payins_from_response(self):
            response = {
                'data': {
                    'items': [
                        {'referenceCode': 'ODOO-001'},
                        {'referenceCode': 'ODOO-002'},
                    ],
                },
            }

            payins = extract_payins(response)

            self.assertEqual(len(payins), 2)
            self.assertEqual(
                payins[0]['referenceCode'],
                'ODOO-001',
            )

        def test_unwrap_payin(self):
            result = unwrap_payin({
                'data': {
                    'payin': {
                        'id': 'PAYIN-001',
                        'status': 'PENDING',
                    },
                },
            })

            self.assertEqual(result['id'], 'PAYIN-001')
            self.assertEqual(result['status'], 'PENDING')

        def test_extract_payins_rejects_invalid_response(self):
            self.assertEqual(extract_payins(None), [])
            self.assertEqual(extract_payins('invalid'), [])

        def test_payin_status_mapping(self):
            self.assertEqual(map_payin_status('PAID'), 'done')
            self.assertEqual(map_payin_status('processing'), 'pending')
            self.assertEqual(map_payin_status('failed'), 'error')
            self.assertEqual(map_payin_status('expired'), 'cancel')
            self.assertIsNone(map_payin_status('UNKNOWN'))
            self.assertIsNone(map_payin_status(None))
    """;

    private const string ReadmeTemplate = """
    # WeselAja Odoo 18 Payment Provider

    Generated module: `__MODULE_NAME__`

    Target Odoo version: `__ODOO_VERSION__`

    ## Features

    - Provider configuration for API Key, Secret Key, Customer Reference,
      webhook secret, sandbox/production URLs, and bank channel.
    - Virtual Account payment method in IDR.
    - Pay-in creation through `POST /v1/payins`.
    - HMAC request signatures and a transaction idempotency key.
    - Customer-facing payment instructions page.
    - Browser return route that does not independently confirm payment.
    - Webhook signature verification over the raw body and timestamp check.
    - Customer and administrator status refresh.

    ## Installation

    1. Copy this module into an Odoo 18 addons directory.
    2. Ensure Odoo's Python environment can import `requests`.
    3. Restart the Odoo service.
    4. Update the Apps list.
    5. Install the WeselAja Payment Provider module.
    6. Open Payment Providers and select WeselAja.
    7. Configure credentials, customer reference, webhook secret, and bank.
    8. Test using sandbox credentials before enabling production payments.

    ## Supported Virtual Account channels

    - BRI.VA
    - MDR.VA (Mandiri)
    - BNI.VA
    - PTB.VA (Permata)
    - CIMB.VA
    - BCA.VA

    ## Webhook endpoint

    `https://your-public-domain/payment/weselaja/webhook`

    The callback implementation reads `x-middleware-timestamp` and
    `x-middleware-signature`, following the existing WeselAja generator
    convention. Confirm these headers and the signature payload against the
    callback contract configured for your WeselAja environment.

    ## API signing

    The request signature uses the HTTP method, canonical endpoint path,
    timestamp, and compact JSON body. Confirm this signing format and the
    GET pay-in listing response schema against the WeselAja sandbox.

    ## Pay-in status

    The status refresh implementation checks up to 100 records returned by
    the pay-in listing API and matches the Odoo transaction reference or
    payment ID. Pagination and the API response envelope must be confirmed
    against the actual provider API.

    Never enable production payments before completing sandbox testing.
    """;
}