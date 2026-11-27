using System.Text;

namespace WeselAjaMcpServer.Generators;

public sealed class OdooWeselAjaGenerator
{
    public const string GeneratorVersion = "odoo-weselaja-v2-shortlink";

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
            GeneratorOutputScope.GetConfiguredRoot();

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
            relative.StartsWith(
                ".." + Path.AltDirectorySeparatorChar,
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
            ["GENERATOR_VERSION.txt"] =
                GeneratorVersion + Environment.NewLine,

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
            files["examples/short_link_query.json"] =
                ExamplePayloadTemplate;
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
        'name': 'WeselAja Hosted Checkout',
        'summary': 'WeselAja V1 Short Link payment integration',
        'version': '__ODOO_VERSION__.2.0.0',
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
    from urllib.parse import urlencode, urlsplit

    from odoo import _, api, fields, models
    from odoo.exceptions import ValidationError


    CALLBACK_PATH = '/payment/weselaja/webhook'
    RETURN_PATH = '/payment/weselaja/return'


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
            help=(
                'Merchant customer reference registered at WeselAja. '
                'For auto-settlement merchants with multiple settlement '
                'accounts and no default account, prefix this value with '
                'the exact case-sensitive subAccount code.'
            ),
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
                'Optional public HTTPS callback URL. Leave empty to use '
                'the public Odoo base URL.'
            ),
            copy=False,
            groups='base.group_system',
        )

        weselaja_redirect_url = fields.Char(
            string='Return URL Override',
            help=(
                'Optional HTTPS return URL. Leave empty to use the '
                'Odoo payment return page.'
            ),
            copy=False,
            groups='base.group_system',
        )

        @api.constrains(
            'weselaja_sandbox_url',
            'weselaja_production_url',
            'weselaja_callback_url',
            'weselaja_redirect_url',
        )
        def _check_weselaja_urls(self):
            for provider in self.filtered(
                lambda record: record.code == 'weselaja'
            ):
                urls = [
                    provider.weselaja_sandbox_url,
                    provider.weselaja_production_url,
                    provider.weselaja_callback_url,
                    provider.weselaja_redirect_url,
                ]

                for url in urls:
                    if not url:
                        continue

                    parsed = urlsplit(url.strip())

                    if (
                        parsed.scheme != 'https'
                        or not parsed.netloc
                        or parsed.username
                        or parsed.password
                        or parsed.query
                        or parsed.fragment
                    ):
                        raise ValidationError(_(
                            'WeselAja URLs must be valid HTTPS URLs without '
                            'credentials, query strings, or fragments.'
                        ))

                if provider.weselaja_callback_url:
                    callback_path = urlsplit(
                        provider.weselaja_callback_url
                    ).path

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
                or parsed.username
                or parsed.password
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
                    'Configure a public HTTPS webhook URL ending with %s.',
                    CALLBACK_PATH,
                ))

            return callback_url

        def _get_weselaja_return_url(self, reference, access_token):
            self.ensure_one()

            base_return_url = (
                self.weselaja_redirect_url.strip()
                if self.weselaja_redirect_url
                else self.get_base_url().rstrip('/') + RETURN_PATH
            )

            parsed = urlsplit(base_return_url)

            if (
                parsed.scheme != 'https'
                or not parsed.netloc
                or parsed.query
                or parsed.fragment
            ):
                raise ValidationError(_(
                    'Configure a valid HTTPS WeselAja return URL.'
                ))

            query = urlencode({
                'reference': reference,
                'access_token': access_token,
            })

            return base_return_url + '?' + query

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

            if self.code == 'weselaja':
                return {'weselaja_checkout'}

            return super()._get_default_payment_method_codes()

         def _get_redirect_form_view(self, is_validation=False):
            self.ensure_one()

            if self.code == 'weselaja':
                return self.env.ref(
                    '__MODULE_NAME__.redirect_form'
                )

            return super()._get_redirect_form_view(
                is_validation=is_validation
            )

        def _get_removal_values(self):
            values = super()._get_removal_values()
            values.update({
                'weselaja_api_key': False,
                'weselaja_secret_key': False,
                'weselaja_customer_reference': False,
                'weselaja_callback_url': False,
                'weselaja_redirect_url': False,
            })
            return values
    """;

    private const string ApiClientTemplate = """
    import json
    import logging
    import re
    from datetime import datetime, timezone
    from urllib.parse import quote, urlsplit

    import requests

    from odoo import _
    from odoo.exceptions import ValidationError


    _logger = logging.getLogger(__name__)

    SHORT_LINK_URI = '/v1/short-link'
    PAYIN_SESSION_PREFIX = '/v2/payins/'


    def build_request_signature(secret, method, uri, timestamp, body=''):
        message = (
            method.upper()
            + '\n'
            + uri
            + '\n'
            + timestamp
            + '\n'
            + body
        )

        digest = __import__('hmac').new(
            secret.encode('utf-8'),
            message.encode('utf-8'),
            __import__('hashlib').sha256,
        ).digest()

        return __import__('base64').b64encode(
            digest
        ).decode('ascii')


    def make_timestamp():
        return (
            datetime.now(timezone.utc)
            .isoformat(timespec='milliseconds')
            .replace('+00:00', 'Z')
        )


    def normalize_customer_phone(phone):
        if not phone:
            return None

        digits = ''.join(
            character
            for character in str(phone)
            if character.isdigit()
        )

        if digits.startswith('08') or digits.startswith('62'):
            return digits

        return None


    def is_valid_reference_code(reference):
        return bool(
            isinstance(reference, str)
            and re.fullmatch(r'[A-Za-z0-9_-]+', reference)
        )


    def unwrap_payin(response_data):
        current = response_data

        for _ in range(5):
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
                'WeselAja returned an unexpected payment response.'
            ))

        return current


    def map_payin_status(status):
        if not isinstance(status, str):
            return None

        return {
            'SUCCESS': 'done',
            'SUCCESSFUL': 'done',
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
            'ACTIVE': 'pending',
        }.get(status.strip().upper())


    class WeselAjaApi:
        def __init__(self, provider):
            self.provider = provider

        def _base_url(self):
            return self.provider._get_weselaja_api_base_url()

        def _decode_response(self, response, endpoint):
            try:
                response_data = response.json()
            except ValueError:
                response_data = None

            if not response.ok:
                message = 'The payment provider rejected the request.'

                if isinstance(response_data, dict):
                    message = (
                        response_data.get('message')
                        or response_data.get('error')
                        or response_data.get('detail')
                        or message
                    )

                if response_data is None:
                    body_text = (response.text or '').strip()
                    if body_text:
                        message = body_text[:300]

                _logger.warning(
                    'WeselAja API returned HTTP %s for %s.',
                    response.status_code,
                    endpoint,
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

        def create_short_link(self, payload):
            if not self.provider.weselaja_api_key:
                raise ValidationError(_(
                    'Configure the WeselAja API Key first.'
                ))

            if not self.provider.weselaja_customer_reference:
                raise ValidationError(_(
                    'Configure the WeselAja Customer Reference first.'
                ))

            params = dict(payload)
            params['apiKey'] = self.provider.weselaja_api_key

            try:
                response = requests.get(
                    self._base_url() + SHORT_LINK_URI,
                    params=params,
                    headers={
                        'Accept': 'application/json',
                        'User-Agent': 'WeselAja-Odoo/2.0',
                    },
                    timeout=(5, 10),
                    allow_redirects=False,
                )
            except requests.RequestException as error:
                _logger.warning(
                    'Could not connect to WeselAja Short Link API.'
                )

                raise ValidationError(_(
                    'Could not connect to WeselAja. Check the API URL '
                    'and server network connection.'
                )) from error

            if response.status_code == 409:
                try:
                    existing = self.get_payin_session(
                        payload['referenceCode'],
                        sync=False,
                    )

                    if (
                        existing.get('referenceCode')
                        == payload['referenceCode']
                    ):
                        return existing

                except (ValidationError, KeyError):
                    pass

            response_data = self._decode_response(
                response,
                'GET /v1/short-link',
            )

            return unwrap_payin(response_data)

        def get_payin_session(
            self,
            identifier,
            sync=False,
            timeout=(5, 10),
        ):
            if not self.provider.weselaja_api_key:
                raise ValidationError(_(
                    'Configure the WeselAja API Key first.'
                ))

            if not self.provider.weselaja_secret_key:
                raise ValidationError(_(
                    'Configure the WeselAja API Secret Key first.'
                ))

            identifier = str(identifier or '').strip()

            if not identifier:
                raise ValidationError(_(
                    'A WeselAja session ID or reference is required.'
                ))

            uri = PAYIN_SESSION_PREFIX + quote(identifier, safe='')

            params = {'sync': 'true'} if sync else None
            timestamp = make_timestamp()

            signature = build_request_signature(
                self.provider.weselaja_secret_key,
                'GET',
                uri,
                timestamp,
                '',
            )

            headers = {
                'Accept': 'application/json',
                'X-API-Key': self.provider.weselaja_api_key,
                'X-Timestamp': timestamp,
                'X-Signature': signature,
            }

            try:
                response = requests.get(
                    self._base_url() + uri,
                    params=params,
                    headers=headers,
                    timeout=timeout,
                    allow_redirects=False,
                )
            except requests.RequestException as error:
                _logger.warning(
                    'Could not connect to WeselAja Payin Session API.'
                )

                raise ValidationError(_(
                    'Could not verify the WeselAja payment status. '
                    'The status request can be retried.'
                )) from error

            response_data = self._decode_response(
                response,
                'GET /v2/payins/{id}',
            )

            return unwrap_payin(response_data)

        def validate_payment_link(self, payment_link_url):
            if not payment_link_url:
                raise ValidationError(_(
                    'WeselAja did not return a payment link.'
                ))

            parsed = urlsplit(str(payment_link_url))
            api_host = urlsplit(self._base_url()).hostname

            if (
                parsed.scheme != 'https'
                or not parsed.hostname
                or parsed.hostname.lower() != (api_host or '').lower()
                or parsed.username
                or parsed.password
                or parsed.fragment
            ):
                raise ValidationError(_(
                    'WeselAja returned an invalid or unexpected payment URL.'
                ))

            return str(payment_link_url)
    """;

    private const string PaymentTransactionTemplate = """
    from odoo import _, api, fields, models
    from odoo.exceptions import UserError, ValidationError

    from odoo.addons.payment import utils as payment_utils
    from odoo.addons.__MODULE_NAME__.lib.weselaja_api import (
        WeselAjaApi,
        is_valid_reference_code,
        map_payin_status,
        normalize_customer_phone,
    )


    class PaymentTransaction(models.Model):
        _inherit = 'payment.transaction'

        weselaja_reference_code = fields.Char(
            string='WeselAja Reference Code',
            copy=False,
            readonly=True,
            index=True,
        )

        weselaja_payment_id = fields.Char(
            string='WeselAja Payment ID',
            copy=False,
            readonly=True,
        )

        weselaja_session_id = fields.Char(
            string='WeselAja Session ID',
            copy=False,
            readonly=True,
            index=True,
        )

        weselaja_payment_link_url = fields.Char(
            string='WeselAja Payment Link',
            copy=False,
            readonly=True,
        )

        weselaja_payment_code = fields.Char(
            string='Payment Code',
            copy=False,
            readonly=True,
        )

        weselaja_payment_channel = fields.Char(
            string='Selected Payment Channel',
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

            self.ensure_one()

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
                    'WeselAja Short Link payments must use IDR.'
                ))

            if not is_valid_reference_code(self.reference):
                raise ValidationError(_(
                    'The Odoo transaction reference must contain only '
                    'letters, numbers, hyphens, and underscores for '
                    'WeselAja Short Link.'
                ))

            provider = self.provider_id

            if not provider.weselaja_customer_reference:
                raise ValidationError(_(
                    'Configure the WeselAja Customer Reference first.'
                ))

            access_token = (
                processing_values.get('access_token')
                or payment_utils.generate_access_token(
                    self.reference,
                    self.amount,
                )
            )

            if not self.weselaja_payment_link_url:
                amount = int(
                    self.currency_id.round(self.amount)
                )

                if amount <= 0:
                    raise ValidationError(_(
                        'WeselAja payment amount must be greater than zero.'
                    ))

                base_url = provider.get_base_url().rstrip('/')

                callback_url = provider._get_weselaja_callback_url()

                redirect_url = provider._get_weselaja_return_url(
                    self.reference,
                    access_token,
                )

                partner = self.partner_id

                customer_name = (
                    self.partner_name
                    or (partner.name if partner else '')
                    or ''
                ).strip()

                phone = (
                    normalize_customer_phone(partner.mobile)
                    if partner and partner.mobile
                    else normalize_customer_phone(partner.phone)
                    if partner
                    else None
                )

                payload = {
                    'amount': amount,
                    'currency': self.currency_id.name,
                    'referenceCode': self.reference,
                    'customerReference':
                        provider.weselaja_customer_reference.strip(),
                    'callbackUrl': callback_url,
                    'redirectUrl': redirect_url,
                }

                if customer_name:
                    payload['customerName'] = customer_name

                if phone:
                    payload['customerPhoneNumber'] = phone

                payin = WeselAjaApi(provider).create_short_link(payload)

                payment_link_url = payin.get('paymentLinkUrl')

                session_id = (
                    payin.get('sessionId')
                    or payin.get('id')
                )

                if not session_id:
                    raise ValidationError(_(
                        'WeselAja did not return a session ID.'
                    ))

                if not payment_link_url:
                    raise ValidationError(_(
                        'WeselAja did not return paymentLinkUrl.'
                    ))

                payment_link_url = (
                    WeselAjaApi(provider).validate_payment_link(
                        payment_link_url
                    )
                )

                returned_reference = payin.get('referenceCode')

                if (
                    returned_reference
                    and returned_reference != self.reference
                ):
                    raise ValidationError(_(
                        'The reference returned by WeselAja does not '
                        'match the Odoo transaction.'
                    ))

                payment_id = payin.get('id') or session_id

                write_values = {
                    'weselaja_reference_code': self.reference,
                    'weselaja_session_id': str(session_id),
                    'weselaja_payment_id': str(payment_id),
                    'weselaja_payment_link_url': payment_link_url,
                    'weselaja_status': (
                        payin.get('status') or 'ACTIVE'
                    ),
                    'provider_reference': str(session_id),
                }

                if payin.get('paymentChannel'):
                    write_values['weselaja_payment_channel'] = (
                        payin['paymentChannel']
                    )

                if payin.get('paymentCode'):
                    write_values['weselaja_payment_code'] = (
                        str(payin['paymentCode'])
                    )

                self.write(write_values)

                if self.state == 'draft':
                    self._set_pending()

            return {
                'instructions_url': (
                    base_url
                    + '/payment/weselaja/instructions'
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

            session_id = notification_data.get('sessionId')

            payment_id = (
                notification_data.get('id')
                or notification_data.get('paymentId')
                or notification_data.get('payinId')
            )

            provider_id = notification_data.get(
                '_weselaja_provider_id'
            )

            domain = [
                ('provider_code', '=', 'weselaja'),
            ]

            if provider_id:
                try:
                    provider_id = int(provider_id)
                except (TypeError, ValueError) as error:
                    raise ValidationError(_(
                        'Invalid WeselAja provider identifier.'
                    )) from error

                domain.append(('provider_id', '=', provider_id))

            transaction = self.browse()

            if reference:
                transaction = self.search([
                    *domain,
                    '|',
                    ('weselaja_reference_code', '=', reference),
                    ('reference', '=', reference),
                ], limit=1)

            if not transaction and session_id:
                transaction = self.search([
                    *domain,
                    ('weselaja_session_id', '=', str(session_id)),
                ], limit=1)

            if not transaction and payment_id:
                transaction = self.search([
                    *domain,
                    '|',
                    ('weselaja_payment_id', '=', str(payment_id)),
                    ('provider_reference', '=', str(payment_id)),
                ], limit=1)

            if not transaction:
                raise ValidationError(_(
                    'No WeselAja transaction matches the supplied '
                    'reference or session ID.'
                ))

            if (
                reference
                and transaction.reference != reference
                and transaction.weselaja_reference_code != reference
            ):
                raise ValidationError(_(
                    'WeselAja reference does not match the transaction.'
                ))

            if (
                session_id
                and transaction.weselaja_session_id
                and str(session_id) != str(
                    transaction.weselaja_session_id
                )
            ):
                raise ValidationError(_(
                    'WeselAja session ID does not match the transaction.'
                ))

            return transaction

        def _process_notification_data(self, notification_data):
            super()._process_notification_data(notification_data)

            if self.provider_code != 'weselaja':
                return

            self.ensure_one()

            if notification_data.get('_weselaja_verified') is not True:
                raise ValidationError(_(
                    'WeselAja status must be verified using the '
                    'authenticated Payin Session API.'
                ))

            reference = (
                notification_data.get('referenceCode')
                or notification_data.get('reference')
            )

            if reference != self.reference:
                raise ValidationError(_(
                    'Verified WeselAja reference does not match '
                    'the Odoo transaction.'
                ))

            session_id = notification_data.get('sessionId')

            if (
                self.weselaja_session_id
                and session_id
                and str(session_id) != str(self.weselaja_session_id)
            ):
                raise ValidationError(_(
                    'Verified WeselAja session ID does not match '
                    'the Odoo transaction.'
                ))

            status = str(
                notification_data.get('status') or ''
            ).strip().upper()

            if not status:
                raise ValidationError(_(
                    'WeselAja Payin Session response is missing status.'
                ))

            transaction_state = map_payin_status(status)

            if transaction_state is None:
                raise ValidationError(_(
                    'WeselAja returned an unsupported payment status: %s',
                    status,
                ))

            if self.state in ('done', 'cancel'):
                return

            values = {
                'weselaja_status': status,
            }

            if session_id:
                values['weselaja_session_id'] = str(session_id)

            payment_id = notification_data.get('id')

            if payment_id:
                values['weselaja_payment_id'] = str(payment_id)

            channel = notification_data.get('paymentChannel')

            if channel:
                values['weselaja_payment_channel'] = str(channel)

            payment_code = notification_data.get('paymentCode')

            if payment_code:
                values['weselaja_payment_code'] = str(payment_code)

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

        def _refresh_weselaja_status(
            self,
            sync=True,
            callback_request=False,
        ):
            self.ensure_one()
            payment_utils.check_rights_on_recordset(self)

            if self.provider_code != 'weselaja':
                raise UserError(_(
                    'This transaction does not use WeselAja.'
                ))

            identifier = (
                self.weselaja_session_id
                or self.weselaja_payment_id
                or self.reference
            )

            timeout = (1.5, 2.5) if callback_request else (5, 10)

            verified_payin = WeselAjaApi(
                self.provider_id
            ).get_payin_session(
                identifier,
                sync=sync,
                timeout=timeout,
            )

            verified_reference = (
                verified_payin.get('referenceCode')
                or verified_payin.get('reference')
            )

            if verified_reference != self.reference:
                raise ValidationError(_(
                    'WeselAja Payin Session reference does not match '
                    'the Odoo transaction.'
                ))

            returned_session_id = verified_payin.get('sessionId')

            if (
                self.weselaja_session_id
                and returned_session_id
                and str(returned_session_id)
                != str(self.weselaja_session_id)
            ):
                raise ValidationError(_(
                    'WeselAja Payin Session ID does not match '
                    'the Odoo transaction.'
                ))

            if not verified_payin.get('status'):
                raise ValidationError(_(
                    'WeselAja Payin Session response has no status.'
                ))

            notification_data = dict(verified_payin)
            notification_data['_weselaja_verified'] = True
            notification_data['_weselaja_provider_id'] = (
                self.provider_id.id
            )

            self.env['payment.transaction'].sudo()._handle_notification_data(
                'weselaja',
                notification_data,
            )

            return verified_payin

        def action_weselaja_refresh_status(self):
            self.ensure_one()
            self._refresh_weselaja_status(sync=True)

            return {
                'type': 'ir.actions.client',
                'tag': 'reload',
            }
    """;

    private const string ControllerTemplate = """
    import json
    import logging
    from urllib.parse import urlencode

    from odoo import http
    from odoo.http import request
    from odoo.exceptions import UserError, ValidationError
    from odoo.addons.payment import utils as payment_utils

    from odoo.addons.__MODULE_NAME__.lib.weselaja_api import (
        WeselAjaApi,
    )


    _logger = logging.getLogger(__name__)

    CALLBACK_PATH = '/payment/weselaja/webhook'


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


    def _render_status(tx, access_token, refresh_error=False):
        status_query = urlencode({
            'reference': tx.reference,
            'access_token': access_token,
        })

        return request.render(
            '__MODULE_NAME__.payment_status',
            {
                'tx': tx,
                'status_url': (
                    '/payment/weselaja/status?' + status_query
                ),
                'refresh_error': refresh_error,
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

            if len(raw_body) > 256 * 1024:
                return _make_response('Payload too large', 413)

            try:
                payload = json.loads(raw_body.decode('utf-8'))
            except (UnicodeDecodeError, json.JSONDecodeError):
                return _make_response('Invalid JSON', 400)

            if not isinstance(payload, dict):
                return _make_response('Invalid payload', 400)

            data = payload.get('data', payload)

            if not isinstance(data, dict):
                return _make_response('Invalid callback data', 400)

            reference = (
                data.get('referenceCode')
                or data.get('reference')
                or data.get('merchantReference')
            )

            session_id = (
                data.get('sessionId')
                or data.get('id')
            )

            if not reference and not session_id:
                return _make_response(
                    'Missing referenceCode and sessionId',
                    400,
                )

            domain = [
                ('provider_code', '=', 'weselaja'),
            ]

            transaction = request.env[
                'payment.transaction'
            ].sudo().browse()

            if reference:
                transaction = request.env[
                    'payment.transaction'
                ].sudo().search([
                    *domain,
                    '|',
                    ('weselaja_reference_code', '=', str(reference)),
                    ('reference', '=', str(reference)),
                ], limit=1)

            if not transaction and session_id:
                transaction = request.env[
                    'payment.transaction'
                ].sudo().search([
                    *domain,
                    ('weselaja_session_id', '=', str(session_id)),
                ], limit=1)

            if not transaction:
                _logger.warning(
                    'WeselAja Short Link callback did not match '
                    'an Odoo transaction.'
                )

                return _make_response('OK', 200)

            if (
                reference
                and transaction.reference != str(reference)
                and transaction.weselaja_reference_code != str(reference)
            ):
                _logger.warning(
                    'WeselAja callback reference mismatch.'
                )
                return _make_response('OK', 200)

            if (
                session_id
                and transaction.weselaja_session_id
                and str(session_id)
                != str(transaction.weselaja_session_id)
            ):
                _logger.warning(
                    'WeselAja callback session ID mismatch.'
                )
                return _make_response('OK', 200)

            try:
                transaction._refresh_weselaja_status(
                    sync=True,
                    callback_request=True,
                )

            except (UserError, ValidationError):
                _logger.warning(
                    'Could not verify WeselAja callback using '
                    'Payin Session API for transaction %s.',
                    transaction.reference,
                    exc_info=True,
                )

                return _make_response(
                    'Payment status verification failed',
                    500,
                )

            except Exception:
                _logger.exception(
                    'Unexpected error processing WeselAja callback '
                    'for transaction %s.',
                    transaction.reference,
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

            if not tx.weselaja_payment_link_url:
                return request.not_found()

            try:
                payment_link_url = WeselAjaApi(
                    tx.provider_id
                ).validate_payment_link(
                    tx.weselaja_payment_link_url
                )
            except ValidationError:
                _logger.warning(
                    'Invalid WeselAja payment link for transaction %s.',
                    tx.reference,
                )
                return request.not_found()

            return request.redirect(payment_link_url, code=303)

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

            refresh_error = False

            try:
                tx._refresh_weselaja_status(sync=True)
            except (UserError, ValidationError):
                refresh_error = True
                _logger.warning(
                    'Could not refresh WeselAja status for %s.',
                    tx.reference,
                    exc_info=True,
                )

            return _render_status(
                tx,
                access_token,
                refresh_error=refresh_error,
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
                '/payment/weselaja/status?' + status_query,
                code=303,
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
                        <field name="weselaja_redirect_url"/>
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
                    Continue to WeselAja Checkout
                </button>
            </form>
        </template>

        <template id="payment_status">
            <t t-call="portal.frontend_layout">
                <t t-set="page_title">WeselAja Payment Status</t>

                <div id="wrap" class="oe_structure">
                    <div class="container my-5">
                        <div class="row justify-content-center">
                            <div class="col-12 col-lg-8">
                                <h1>WeselAja Payment Status</h1>

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
                                            <strong>Payment Status</strong>
                                            <div>
                                                <span t-out="tx.weselaja_status or 'PENDING'"/>
                                            </div>
                                        </div>

                                        <div class="mb-3">
                                            <strong>Odoo Transaction State</strong>
                                            <div>
                                                <span t-out="tx.state"/>
                                            </div>
                                        </div>

                                        <div
                                            t-if="tx.weselaja_payment_channel"
                                            class="mb-3"
                                        >
                                            <strong>Selected Payment Channel</strong>
                                            <div>
                                                <span t-out="tx.weselaja_payment_channel"/>
                                            </div>
                                        </div>
                                    </div>
                                </div>

                                <div
                                    t-if="refresh_error"
                                    class="alert alert-warning"
                                    role="alert"
                                >
                                    The payment status could not be refreshed
                                    from WeselAja. Please try again.
                                </div>

                                <div
                                    t-if="tx.state == 'done'"
                                    class="alert alert-success"
                                    role="alert"
                                >
                                    Payment completed successfully.
                                </div>

                                <div
                                    t-elif="tx.state == 'cancel'"
                                    class="alert alert-warning"
                                    role="alert"
                                >
                                    This payment has expired or been cancelled.
                                </div>

                                <div
                                    t-elif="tx.state == 'error'"
                                    class="alert alert-danger"
                                    role="alert"
                                >
                                    The payment failed.
                                </div>

                                <div
                                    t-else=""
                                    class="alert alert-info"
                                    role="alert"
                                >
                                    Waiting for payment confirmation from WeselAja.
                                </div>

                                <div class="d-flex flex-wrap gap-2">
                                    <a
                                        t-att-href="status_url"
                                        class="btn btn-primary"
                                    >
                                        Refresh Payment Status
                                    </a>

                                    <a
                                        t-if="tx.weselaja_payment_link_url and tx.state not in ('done', 'cancel', 'error')"
                                        t-att-href="tx.weselaja_payment_link_url"
                                        class="btn btn-outline-primary"
                                        target="_blank"
                                        rel="noopener noreferrer"
                                    >
                                        Open Checkout Again
                                    </a>
                                </div>
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
                        <field name="weselaja_reference_code"/>
                        <field name="weselaja_payment_id"/>
                        <field name="weselaja_session_id"/>
                        <field name="weselaja_payment_link_url"/>
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

        <record id="payment_method_weselaja_checkout" model="payment.method">
            <field name="name">WeselAja Hosted Checkout</field>
            <field name="code">weselaja_checkout</field>
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
      "apiKey": "YOUR_MERCHANT_API_KEY",
      "amount": 10000,
      "currency": "IDR",
      "referenceCode": "ODOO-EXAMPLE-001",
      "customerReference": "CUSTOMER-001",
      "customerName": "Example Customer",
      "customerPhoneNumber": "081234567890",
      "callbackUrl": "https://your-public-domain.example/payment/weselaja/webhook",
      "redirectUrl": "https://your-public-domain.example/payment/weselaja/return"
    }
    """;

    private const string TestsInitTemplate = """
    from . import test_weselaja
    """;

    private const string TestsTemplate = """
    from odoo.tests import TransactionCase, tagged

    from odoo.addons.__MODULE_NAME__.lib.weselaja_api import (
        build_request_signature,
        is_valid_reference_code,
        map_payin_status,
        normalize_customer_phone,
        unwrap_payin,
    )


    @tagged('post_install', '-at_install')
    class TestWeselAjaHelpers(TransactionCase):

        def test_request_signature_is_deterministic(self):
            first = build_request_signature(
                'test-secret',
                'GET',
                '/v2/payins/session-001',
                '2026-10-14T00:00:00.000Z',
                '',
            )

            second = build_request_signature(
                'test-secret',
                'GET',
                '/v2/payins/session-001',
                '2026-10-14T00:00:00.000Z',
                '',
            )

            changed_uri = build_request_signature(
                'test-secret',
                'GET',
                '/v2/payins/session-002',
                '2026-10-14T00:00:00.000Z',
                '',
            )

            self.assertEqual(first, second)
            self.assertNotEqual(first, changed_uri)

        def test_request_signature_changes_with_body(self):
            first = build_request_signature(
                'test-secret',
                'POST',
                '/v2/payins',
                '2026-10-14T00:00:00.000Z',
                '{"amount":10000}',
            )

            second = build_request_signature(
                'test-secret',
                'POST',
                '/v2/payins',
                '2026-10-14T00:00:00.000Z',
                '{"amount":20000}',
            )

            self.assertNotEqual(first, second)

        def test_reference_code_validation(self):
            self.assertTrue(is_valid_reference_code('S00011'))
            self.assertTrue(is_valid_reference_code('ORDER-2026_001'))
            self.assertFalse(is_valid_reference_code('ORDER 001'))
            self.assertFalse(is_valid_reference_code('ORDER/001'))
            self.assertFalse(is_valid_reference_code(''))

        def test_phone_normalization(self):
            self.assertEqual(
                normalize_customer_phone('0812-3456-7890'),
                '081234567890',
            )

            self.assertEqual(
                normalize_customer_phone('+62 812-3456-7890'),
                '6281234567890',
            )

            self.assertIsNone(
                normalize_customer_phone('+1 555 123456'),
            )

            self.assertIsNone(
                normalize_customer_phone(None),
            )

        def test_payin_status_mapping(self):
            self.assertEqual(map_payin_status('SUCCESS'), 'done')
            self.assertEqual(map_payin_status('COMPLETED'), 'done')
            self.assertEqual(map_payin_status('PAID'), 'done')
            self.assertEqual(map_payin_status('PENDING'), 'pending')
            self.assertEqual(map_payin_status('ACTIVE'), 'pending')
            self.assertEqual(map_payin_status('FAILED'), 'error')
            self.assertEqual(map_payin_status('EXPIRED'), 'cancel')
            self.assertIsNone(map_payin_status('UNKNOWN'))

        def test_unwrap_payin(self):
            result = unwrap_payin({
                'data': {
                    'id': 'sl-001',
                    'sessionId': 'sl-001',
                    'referenceCode': 'ODOO-001',
                },
            })

            self.assertEqual(result['id'], 'sl-001')
            self.assertEqual(result['sessionId'], 'sl-001')

        def test_unwrap_nested_payin(self):
            result = unwrap_payin({
                'data': {
                    'payin': {
                        'id': 'payin-001',
                        'status': 'SUCCESS',
                    },
                },
            })

            self.assertEqual(result['id'], 'payin-001')
            self.assertEqual(result['status'], 'SUCCESS')

    """;

    private const string ReadmeTemplate = """
    # WeselAja Odoo 18 Hosted Checkout

    Generated module: `__MODULE_NAME__`

    Target Odoo version: `__ODOO_VERSION__`

    Generator version: `odoo-weselaja-v2-shortlink`

    ## Integration design

    This module creates payment links using the V1 Short Link API.

    The customer chooses the payment channel on the WeselAja hosted checkout
    page. Odoo does not send a `paymentChannel` parameter when creating
    a V1 Short Link.

    The module uses the V2 Payin Session API only to verify transaction status.
    The V2 session request is signed with the merchant Secret Key.

    ## Requirements

    - Odoo 18.0
    - Python requests library
    - WeselAja merchant API Key
    - WeselAja merchant Secret Key
    - Merchant Customer Reference
    - A publicly reachable HTTPS Odoo base URL

    ## Installation

    1. Copy this module into an Odoo 18 addons directory.
    2. Ensure Odoo's Python environment can import `requests`.
    3. Restart the Odoo service.
    4. Update the Apps list.
    5. Install WeselAja Hosted Checkout.
    6. Open Payment Providers and select WeselAja.
    7. Configure credentials and the Customer Reference.
    8. Configure the sandbox URL and test payments before production use.

    ## Provider configuration

    - API Key: Merchant API Key.
    - API Secret Key: Merchant Secret Key. Used for signed V2 status requests.
    - Customer Reference: Merchant customer identifier.
    - Sandbox API Base URL: `https://sandbox.checkout.weselaja.id`.
    - Production API Base URL: `https://checkout.weselaja.id`.
    - Webhook URL Override: Optional public HTTPS callback URL.
    - Return URL Override: Optional HTTPS return URL.

    For auto-settlement merchants with more than one settlement account
    and no default account, the Customer Reference must start with the exact
    case-sensitive `subAccount` code, as described in the WeselAja Short Link
    documentation.

    This V1 Short Link flow does not send a `settlements` array.

    ## Create a payment

    The module creates a checkout link with:

    ```http
    GET /v1/short-link
    ```

    The API Key is passed using the `apiKey` query parameter.

    The request parameters include:

    - `amount`
    - `currency`
    - `referenceCode`
    - `customerReference`
    - `customerName`
    - `customerPhoneNumber` when valid
    - `callbackUrl`
    - `redirectUrl`

    The module does not send:

    - `paymentChannel`
    - `paymentMethod`
    - `settlements`
    - `X-Idempotency-Key`

    The Short Link response must contain `sessionId` and `paymentLinkUrl`.
    Odoo stores those values in `payment.transaction`.

    ## Authentication and signatures

    V1 Short Link uses the `apiKey` query parameter and does not use
    request-signature headers.

    The Payin Session status request uses:

    ```http
    GET /v2/payins/{id}
    ```

    It uses these mandatory headers:

    ```http
    X-API-Key
    X-Timestamp
    X-Signature
    ```

    The request signature is generated from:

    ```text
    UPPERCASE_METHOD
    URI_PATH
    ISO-8601_TIMESTAMP
    RAW_REQUEST_BODY
    ```

    The query string is not included in the signed URI path.

    ## Callback verification

    V1 Short Link callbacks do not contain a signature.

    The callback is treated only as a notification that a transaction may
    have changed. Odoo does not trust the callback's `status` or amount.

    Odoo uses the callback's reference and session ID to locate the original
    transaction. It then retrieves the current transaction using the
    authenticated and signed V2 Payin Session API.

    Odoo only processes the status returned by the authenticated API response
    after verifying the returned reference and session ID.

    This prevents an unsigned callback payload from independently marking
    an order as paid.

    The callback endpoint is:

    ```text
    https://your-public-domain/payment/weselaja/webhook
    ```

    The endpoint acknowledges successful processing with HTTP 200.
    If status verification fails, it returns a non-200 response so WeselAja
    can retry delivery.

    The callback handler uses short API timeouts to target the documented
    five-second acknowledgement window. Network and server latency can still
    affect the actual response time.

    ## Payin status refresh

    The module queries the V2 Payin Session API using the stored `sessionId`.

    The request is signed with the merchant Secret Key. When `sync=true`
    is used, WeselAja checks the payment provider while the session is pending.

    The returned reference and session ID must match the Odoo transaction.

    ## Payment statuses

    | WeselAja status | Odoo transaction state |
    |---|---|
    | SUCCESS | Done |
    | SUCCESSFUL | Done |
    | PAID | Done |
    | COMPLETED | Done |
    | SETTLED | Done |
    | PENDING | Pending |
    | PROCESSING | Pending |
    | ACTIVE | Pending |
    | FAILED | Error |
    | DECLINED | Error |
    | REJECTED | Error |
    | EXPIRED | Cancel |
    | CANCELLED | Cancel |
    | CANCELED | Cancel |

    ## Security notes

    - Never log the V1 Short Link URL because it contains the API Key query parameter.
    - Do not mark an Odoo transaction paid based only on the unsigned callback.
    - Do not trust the browser return URL as payment confirmation.
    - Use the authenticated V2 Payin Session response as the status source.
    - Keep the merchant API Secret Key private.
    - Use sandbox credentials until the complete payment flow has been tested.

    ## Testing

    Run the Odoo test suite with:

    ```bash
    ./odoo-bin \
      -d your_database \
      -i __MODULE_NAME__ \
      --test-enable \
      --stop-after-init
    ```

    The generated tests cover signature generation, reference validation,
    phone normalization, status mapping, and response parsing.

    They do not make live calls to WeselAja.
    """;
}
