package weselaja

import (
	"bufio"
	"fmt"
	"net/url"
	"os"
	"strings"
)

type Config struct {
	APIURL            string
	APIKey            string
	SecretKey         string
	CustomerReference string
	CallbackURL       string
	RedirectURL       string
	Currency          string
	PaymentMethod     string
}

func ConfigFromEnvironment() (Config, error) {
	if err := LoadEnv(".env"); err != nil {
		return Config{}, err
	}

	config := Config{
		APIURL:            envOrDefault("WESELAJA_API_URL", "https://sandbox.checkout.weselaja.id"),
		APIKey:            os.Getenv("WESELAJA_API_KEY"),
		SecretKey:         os.Getenv("WESELAJA_SECRET_KEY"),
		CustomerReference: os.Getenv("WESELAJA_CUSTOMER_REFERENCE"),
		CallbackURL:       os.Getenv("WESELAJA_CALLBACK_URL"),
		RedirectURL:       os.Getenv("WESELAJA_REDIRECT_URL"),
		Currency:          envOrDefault("WESELAJA_CURRENCY", "IDR"),
		PaymentMethod:     envOrDefault("WESELAJA_PAYMENT_METHOD", "VIRTUAL_ACCOUNT"),
	}
	if err := config.Validate(); err != nil {
		return Config{}, err
	}
	return config, nil
}

func (c Config) Validate() error {
	required := map[string]string{
		"WESELAJA_API_URL":            c.APIURL,
		"WESELAJA_API_KEY":            c.APIKey,
		"WESELAJA_SECRET_KEY":         c.SecretKey,
		"WESELAJA_CUSTOMER_REFERENCE": c.CustomerReference,
		"WESELAJA_CALLBACK_URL":       c.CallbackURL,
		"WESELAJA_REDIRECT_URL":       c.RedirectURL,
		"WESELAJA_CURRENCY":           c.Currency,
		"WESELAJA_PAYMENT_METHOD":     c.PaymentMethod,
	}
	missing := make([]string, 0)
	for name, value := range required {
		if strings.TrimSpace(value) == "" {
			missing = append(missing, name)
		}
	}
	if len(missing) > 0 {
		return fmt.Errorf("missing configuration: %s", strings.Join(missing, ", "))
	}

	for _, item := range []struct {
		name  string
		value string
	}{
		{"WESELAJA_API_URL", c.APIURL},
		{"WESELAJA_CALLBACK_URL", c.CallbackURL},
		{"WESELAJA_REDIRECT_URL", c.RedirectURL},
	} {
		parsed, err := url.ParseRequestURI(item.value)
		if err != nil || parsed.Host == "" || (parsed.Scheme != "http" && parsed.Scheme != "https") {
			return fmt.Errorf("%s must be a valid HTTP or HTTPS URL", item.name)
		}
	}
	return nil
}

// LoadEnv loads KEY=VALUE entries without overwriting variables already set in the process.
func LoadEnv(path string) error {
	file, err := os.Open(path)
	if err != nil {
		if os.IsNotExist(err) {
			return nil
		}
		return fmt.Errorf("open environment file %q: %w", path, err)
	}
	defer file.Close()

	scanner := bufio.NewScanner(file)
	lineNumber := 0
	for scanner.Scan() {
		lineNumber++
		line := strings.TrimSpace(scanner.Text())
		if line == "" || strings.HasPrefix(line, "#") {
			continue
		}
		key, value, ok := strings.Cut(line, "=")
		key = strings.TrimSpace(key)
		if !ok || key == "" {
			return fmt.Errorf("invalid environment entry at %s:%d", path, lineNumber)
		}
		value = strings.TrimSpace(value)
		if len(value) >= 2 && ((value[0] == '"' && value[len(value)-1] == '"') || (value[0] == '\'' && value[len(value)-1] == '\'')) {
			value = value[1 : len(value)-1]
		}
		if _, exists := os.LookupEnv(key); !exists {
			if err := os.Setenv(key, value); err != nil {
				return fmt.Errorf("set environment variable %q: %w", key, err)
			}
		}
	}
	if err := scanner.Err(); err != nil {
		return fmt.Errorf("read environment file %q: %w", path, err)
	}
	return nil
}

func envOrDefault(key, fallback string) string {
	if value := strings.TrimSpace(os.Getenv(key)); value != "" {
		return value
	}
	return fallback
}
