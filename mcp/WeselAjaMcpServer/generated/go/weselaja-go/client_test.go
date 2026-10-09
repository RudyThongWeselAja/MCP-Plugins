package weselaja

import (
	"encoding/base64"
	"encoding/json"
	"errors"
	"io"
	"net/http"
	"net/http/httptest"
	"strings"
	"testing"
	"time"
)

func testConfig(apiURL string) Config {
	return Config{
		APIURL:            apiURL,
		APIKey:            "test-api-key",
		SecretKey:         "test-secret",
		CustomerReference: "CUSTOMER_123",
		CallbackURL:       "https://merchant.example/callback",
		RedirectURL:       "https://merchant.example/return",
		Currency:          "IDR",
		PaymentMethod:     "VIRTUAL_ACCOUNT",
	}
}

func TestResolveBankChannel(t *testing.T) {
	tests := map[string]string{
		"BRI":          "BRI.VA",
		"Bank BRI":     "BRI.VA",
		"Bank-Mandiri": "MDR.VA",
		"BNI":          "BNI.VA",
		"Permata":      "PTB.VA",
		"CIMB Niaga":   "CIMB.VA",
		"BCA":          "BCA.VA",
	}
	for bank, expected := range tests {
		t.Run(bank, func(t *testing.T) {
			actual, err := ResolveBankChannel(bank)
			if err != nil {
				t.Fatal(err)
			}
			if actual != expected {
				t.Fatalf("ResolveBankChannel(%q) = %q, want %q", bank, actual, expected)
			}
		})
	}
	if _, err := ResolveBankChannel("Unknown"); err == nil {
		t.Fatal("unsupported bank must return an error")
	}
}

func TestCreatePaymentSignsAndSendsRequest(t *testing.T) {
	fixedTime := time.Date(2026, 10, 5, 2, 3, 4, 567000000, time.UTC)
	server := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost || r.URL.Path != paymentURI {
			t.Errorf("unexpected request: %s %s", r.Method, r.URL.Path)
		}
		if r.Header.Get("Xenith-Api-Key") != "test-api-key" {
			t.Error("API key header was not set")
		}
		timestamp := r.Header.Get("Xenith-Request-Timestamp")
		if timestamp != "2026-10-05T02:03:04.567Z" {
			t.Errorf("unexpected timestamp: %s", timestamp)
		}
		if key := r.Header.Get("X-Idempotency-Key"); !strings.HasPrefix(key, "go-") || len(key) != 35 {
			t.Errorf("invalid idempotency key: %q", key)
		}

		requestBody, err := io.ReadAll(r.Body)
		if err != nil {
			t.Errorf("read request body: %v", err)
		}
		var payload map[string]any
		if err := json.Unmarshal(requestBody, &payload); err != nil {
			t.Errorf("decode request body: %v", err)
		}
		if payload["paymentChannel"] != "BCA.VA" || payload["initiatedAmount"] != float64(10000) {
			t.Errorf("unexpected request payload: %#v", payload)
		}
		expected := GenerateSignature(http.MethodPost, paymentURI, timestamp, string(requestBody), "test-secret")
		if got := r.Header.Get("Xenith-Request-Signature"); got != expected {
			t.Errorf("signature mismatch: got %s, want %s", got, expected)
		}
		w.Header().Set("Content-Type", "application/json")
		_, _ = w.Write([]byte(`{"paymentId":"pay_123","status":"PENDING","paymentCode":"123456","paymentChannel":"BCA.VA"}`))
	}))
	defer server.Close()

	client, err := NewClient(testConfig(server.URL))
	if err != nil {
		t.Fatal(err)
	}
	client.now = func() time.Time { return fixedTime }
	response, err := client.CreatePayment(CreatePaymentRequest{
		Amount:        10000,
		CustomerName:  " Andi ",
		Bank:          "BCA",
		ReferenceCode: "ORDER_1",
	})
	if err != nil {
		t.Fatal(err)
	}
	if !response.Success || response.PaymentID != "pay_123" || response.Status != "PENDING" {
		t.Fatalf("unexpected response: %#v", response)
	}
	decoded, err := base64.StdEncoding.DecodeString(GenerateSignature("POST", paymentURI, "time", "body", "secret"))
	if err != nil || len(decoded) != 32 {
		t.Fatalf("signature is not a base64-encoded SHA-256 HMAC: %v", err)
	}
}

func TestCreatePaymentReportsAPIError(t *testing.T) {
	server := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		w.WriteHeader(http.StatusBadRequest)
		_, _ = w.Write([]byte(`{"message":"invalid payment"}`))
	}))
	defer server.Close()

	client, err := NewClient(testConfig(server.URL))
	if err != nil {
		t.Fatal(err)
	}
	_, err = client.CreatePayment(CreatePaymentRequest{Amount: 1, CustomerName: "A", Bank: "BRI"})
	var apiErr *APIError
	if !errors.As(err, &apiErr) {
		t.Fatalf("expected APIError, got %v", err)
	}
	if apiErr.StatusCode != http.StatusBadRequest || apiErr.Message != "invalid payment" {
		t.Fatalf("unexpected API error: %#v", apiErr)
	}
}

func TestValidation(t *testing.T) {
	if _, err := NewClient(Config{}); err == nil {
		t.Fatal("empty config must be rejected")
	}
	client, err := NewClient(testConfig("https://example.com"))
	if err != nil {
		t.Fatal(err)
	}
	_, err = client.CreatePayment(CreatePaymentRequest{Amount: -1, CustomerName: "A", Bank: "BRI"})
	if err == nil {
		t.Fatal("non-positive amount must be rejected")
	}
	_, err = client.CreatePayment(CreatePaymentRequest{Amount: 1, CustomerName: "A", Bank: "BRI", ReferenceCode: "BAD REF"})
	if err == nil {
		t.Fatal("invalid reference code must be rejected")
	}
}
