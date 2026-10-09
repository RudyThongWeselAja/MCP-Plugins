package weselaja

import (
	"bytes"
	"crypto/rand"
	"encoding/hex"
	"encoding/json"
	"fmt"
	"io"
	"net/http"
	"net/url"
	"regexp"
	"strings"
	"time"
)

const paymentURI = "/v1/payins"

type Client struct {
	config     Config
	httpClient *http.Client
	now        func() time.Time
}

type APIError struct {
	StatusCode int
	Body       string
	Message    string
}

func (e *APIError) Error() string {
	return fmt.Sprintf("WeselAja API returned HTTP %d: %s", e.StatusCode, e.Message)
}

func NewClient(config Config) (*Client, error) {
	if err := config.Validate(); err != nil {
		return nil, err
	}
	return &Client{
		config: config,
		httpClient: &http.Client{
			Timeout: 30 * time.Second,
		},
		now: time.Now,
	}, nil
}

func (c *Client) CreatePayment(request CreatePaymentRequest) (CreatePaymentResponse, error) {
	if err := request.validate(); err != nil {
		return CreatePaymentResponse{}, err
	}
	channel, err := ResolveBankChannel(request.Bank)
	if err != nil {
		return CreatePaymentResponse{}, err
	}

	referenceCode := strings.TrimSpace(request.ReferenceCode)
	if referenceCode == "" {
		referenceCode = generateReference(request.CustomerName, c.now())
	}
	if err := validateReference(referenceCode); err != nil {
		return CreatePaymentResponse{}, err
	}
	if err := validateReference(c.config.CustomerReference); err != nil {
		return CreatePaymentResponse{}, fmt.Errorf("invalid customer reference: %w", err)
	}

	payload := struct {
		InitiatedAmount     int64  `json:"initiatedAmount"`
		Currency            string `json:"currency"`
		PaymentMethod       string `json:"paymentMethod"`
		PaymentChannel      string `json:"paymentChannel"`
		ReferenceCode       string `json:"referenceCode"`
		CustomerReference   string `json:"customerReference"`
		CustomerName        string `json:"customerName"`
		CallbackURL         string `json:"callbackUrl"`
		RedirectURL         string `json:"redirectUrl"`
		CustomerPhoneNumber string `json:"customerPhoneNumber,omitempty"`
		Description         string `json:"description,omitempty"`
	}{
		InitiatedAmount:     request.Amount,
		Currency:            c.config.Currency,
		PaymentMethod:       c.config.PaymentMethod,
		PaymentChannel:      channel,
		ReferenceCode:       referenceCode,
		CustomerReference:   c.config.CustomerReference,
		CustomerName:        strings.TrimSpace(request.CustomerName),
		CallbackURL:         c.config.CallbackURL,
		RedirectURL:         c.config.RedirectURL,
		CustomerPhoneNumber: strings.TrimSpace(request.CustomerPhoneNumber),
		Description:         strings.TrimSpace(request.Description),
	}
	body, err := json.Marshal(payload)
	if err != nil {
		return CreatePaymentResponse{}, fmt.Errorf("encode payment request: %w", err)
	}

	timestamp := c.now().UTC().Format("2006-01-02T15:04:05.000Z")
	signature := GenerateSignature(http.MethodPost, paymentURI, timestamp, string(body), c.config.SecretKey)
	idempotencyKey, err := newIdempotencyKey()
	if err != nil {
		return CreatePaymentResponse{}, fmt.Errorf("generate idempotency key: %w", err)
	}

	baseURL := strings.TrimRight(c.config.APIURL, "/")
	parsedURL, err := url.Parse(baseURL + paymentURI)
	if err != nil {
		return CreatePaymentResponse{}, fmt.Errorf("build WeselAja URL: %w", err)
	}
	httpRequest, err := http.NewRequest(http.MethodPost, parsedURL.String(), bytes.NewReader(body))
	if err != nil {
		return CreatePaymentResponse{}, fmt.Errorf("create WeselAja request: %w", err)
	}
	httpRequest.Header.Set("Content-Type", "application/json")
	httpRequest.Header.Set("Accept", "application/json")
	httpRequest.Header.Set("User-Agent", "Python-urllib/3.13")
	httpRequest.Header.Set("Accept-Encoding", "identity")
	httpRequest.Header.Set("Connection", "close")
	httpRequest.Header.Set("Xenith-Api-Key", c.config.APIKey)
	httpRequest.Header.Set("Xenith-Request-Timestamp", timestamp)
	httpRequest.Header.Set("Xenith-Request-Signature", signature)
	httpRequest.Header.Set("X-Idempotency-Key", idempotencyKey)

	httpResponse, err := c.httpClient.Do(httpRequest)
	if err != nil {
		return CreatePaymentResponse{}, fmt.Errorf("send request to WeselAja: %w", err)
	}
	defer httpResponse.Body.Close()

	responseBody, err := io.ReadAll(io.LimitReader(httpResponse.Body, 1<<20))
	if err != nil {
		return CreatePaymentResponse{}, fmt.Errorf("read WeselAja response: %w", err)
	}
	if len(bytes.TrimSpace(responseBody)) == 0 {
		return CreatePaymentResponse{}, fmt.Errorf("empty response from WeselAja (HTTP %d)", httpResponse.StatusCode)
	}

	var response CreatePaymentResponse
	if httpResponse.StatusCode < http.StatusOK || httpResponse.StatusCode >= http.StatusMultipleChoices {
		return CreatePaymentResponse{}, apiError(httpResponse.StatusCode, responseBody)
	}
	if err := json.Unmarshal(responseBody, &response); err != nil {
		return CreatePaymentResponse{}, fmt.Errorf("decode WeselAja response (HTTP %d): %w", httpResponse.StatusCode, err)
	}
	response.Success = true
	return response, nil
}

var invalidReference = regexp.MustCompile(`[^A-Z0-9_-]`)
var validReference = regexp.MustCompile(`^[A-Za-z0-9_-]+$`)

func generateReference(customerName string, now time.Time) string {
	normalized := invalidReference.ReplaceAllString(strings.ToUpper(customerName), "")
	if normalized == "" {
		normalized = "CUSTOMER"
	}
	if len(normalized) > 20 {
		normalized = normalized[:20]
	}
	return "WES-" + normalized + "-" + now.UTC().Format("20060102150405")
}

func validateReference(value string) error {
	if strings.TrimSpace(value) == "" {
		return fmt.Errorf("reference value is required")
	}
	if !validReference.MatchString(value) {
		return fmt.Errorf("reference values may only contain letters, numbers, \"-\" and \"_\"")
	}
	return nil
}

func newIdempotencyKey() (string, error) {
	randomBytes := make([]byte, 16)
	if _, err := rand.Read(randomBytes); err != nil {
		return "", err
	}
	return "go-" + hex.EncodeToString(randomBytes), nil
}

func apiError(statusCode int, body []byte) error {
	message := strings.TrimSpace(string(body))
	var parsed map[string]any
	if err := json.Unmarshal(body, &parsed); err == nil {
		for _, key := range []string{"message", "error", "code"} {
			if value, ok := parsed[key].(string); ok && strings.TrimSpace(value) != "" {
				message = value
				break
			}
		}
	}
	return &APIError{StatusCode: statusCode, Body: string(body), Message: message}
}
