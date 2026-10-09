package weselaja

import (
	"encoding/json"
	"fmt"
	"strconv"
	"strings"
)

type CreatePaymentRequest struct {
	Amount              int64
	CustomerName        string
	Bank                string
	CustomerPhoneNumber string
	Description         string
	ReferenceCode       string
}

func (r CreatePaymentRequest) validate() error {
	if r.Amount <= 0 {
		return fmt.Errorf("payment amount must be greater than zero")
	}

	if strings.TrimSpace(r.CustomerName) == "" {
		return fmt.Errorf("customer name is required")
	}

	if strings.TrimSpace(r.Bank) == "" {
		return fmt.Errorf("bank is required")
	}

	return nil
}

type FlexibleInt64 int64

func (v *FlexibleInt64) UnmarshalJSON(data []byte) error {
	if string(data) == "null" {
		*v = 0
		return nil
	}

	var number int64
	if err := json.Unmarshal(data, &number); err == nil {
		*v = FlexibleInt64(number)
		return nil
	}

	var value string
	if err := json.Unmarshal(data, &value); err != nil {
		return err
	}

	number, err := strconv.ParseInt(value, 10, 64)
	if err != nil {
		return err
	}

	*v = FlexibleInt64(number)

	return nil
}

type CreatePaymentResponse struct {
	Success             bool            `json:"success"`
	PaymentID           string          `json:"paymentId"`
	InitiatedAmount     FlexibleInt64   `json:"initiatedAmount"`
	PaymentAmount       json.RawMessage `json:"paymentAmount"`
	FeeAmount           json.RawMessage `json:"feeAmount"`
	Currency            string          `json:"currency"`
	PaymentMethod       string          `json:"paymentMethod"`
	PaymentChannel      string          `json:"paymentChannel"`
	PaymentCode         string          `json:"paymentCode"`
	PaymentCodeType     string          `json:"paymentCodeType"`
	ReferenceCode       string          `json:"referenceCode"`
	CustomerReference   string          `json:"customerReference"`
	CustomerName        string          `json:"customerName"`
	Status              string          `json:"status"`
	CreatedTime         string          `json:"createdTime"`
	UpdatedTime         string          `json:"updatedTime"`
	ExpirationTime      string          `json:"expirationTime"`
	Description         string          `json:"description"`
	CallbackURL         string          `json:"callbackUrl"`
	RedirectURL         string          `json:"redirectUrl"`
	PayerAccountName    string          `json:"payerAccountName"`
	PayerAccountNumber  string          `json:"payerAccountNumber"`
	PayerPaymentChannel string          `json:"payerPaymentChannel"`
	Metadata            map[string]any  `json:"metadata"`
}
