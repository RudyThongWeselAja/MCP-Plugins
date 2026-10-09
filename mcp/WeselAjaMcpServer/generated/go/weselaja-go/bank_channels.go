package weselaja

import (
	"fmt"
	"strings"
)

var bankChannels = map[string]string{
	"BRI":         "BRI.VA",
	"BANKBRI":     "BRI.VA",
	"MANDIRI":     "MDR.VA",
	"BANKMANDIRI": "MDR.VA",
	"MDR":         "MDR.VA",
	"BNI":         "BNI.VA",
	"BANKBNI":     "BNI.VA",
	"PERMATA":     "PTB.VA",
	"BANKPERMATA": "PTB.VA",
	"PTB":         "PTB.VA",
	"CIMB":        "CIMB.VA",
	"CIMBNIAGA":   "CIMB.VA",
	"BANKCIMB":    "CIMB.VA",
	"BCA":         "BCA.VA",
	"BANKBCA":     "BCA.VA",
}

func ResolveBankChannel(bank string) (string, error) {
	if strings.TrimSpace(bank) == "" {
		return "", fmt.Errorf("bank is required")
	}

	normalized := strings.NewReplacer(" ", "", "-", "", "_", "").Replace(strings.ToUpper(strings.TrimSpace(bank)))
	channel, ok := bankChannels[normalized]
	if !ok {
		return "", fmt.Errorf("unsupported bank %q; supported banks: BRI, Mandiri, BNI, Permata, CIMB, BCA", bank)
	}
	return channel, nil
}

func SupportedBanks() map[string]string {
	return map[string]string{
		"BRI":     "BRI.VA",
		"Mandiri": "MDR.VA",
		"BNI":     "BNI.VA",
		"Permata": "PTB.VA",
		"CIMB":    "CIMB.VA",
		"BCA":     "BCA.VA",
	}
}
