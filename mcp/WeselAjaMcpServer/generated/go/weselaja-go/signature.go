package weselaja

import (
	"crypto/hmac"
	"crypto/sha256"
	"encoding/base64"
)

func GenerateSignature(method, uri, timestamp, body, secret string) string {
	payload := method + "\n" + uri + "\n" + timestamp + "\n" + body
	mac := hmac.New(sha256.New, []byte(secret))
	_, _ = mac.Write([]byte(payload))
	return base64.StdEncoding.EncodeToString(mac.Sum(nil))
}
