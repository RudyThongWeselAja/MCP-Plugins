package main

import (
	"fmt"
	"log"

	"weselaja-go"
)

func main() {
	config, err := weselaja.ConfigFromEnvironment()
	if err != nil {
		log.Fatal(err)
	}
	client, err := weselaja.NewClient(config)
	if err != nil {
		log.Fatal(err)
	}

	payment, err := client.CreatePayment(weselaja.CreatePaymentRequest{
		Amount:       10000,
		CustomerName: "Andi",
		Bank:         "MANDIRI",
		Description:  "WeselAja sandbox example",
	})
	if err != nil {
		log.Fatal(err)
	}

	fmt.Printf("Payment created successfully.\nStatus: %s\nReference: %s\nChannel: %s\nPayment code: %s\n",
		payment.Status, payment.ReferenceCode, payment.PaymentChannel, payment.PaymentCode)
}
