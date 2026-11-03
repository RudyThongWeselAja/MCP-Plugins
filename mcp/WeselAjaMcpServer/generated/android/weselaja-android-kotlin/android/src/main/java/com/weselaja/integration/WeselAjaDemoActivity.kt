package com.weselaja.integration

import android.app.Activity
import android.os.Bundle
import android.view.View
import android.view.ViewGroup
import android.widget.ArrayAdapter
import android.widget.Button
import android.widget.EditText
import android.widget.LinearLayout
import android.widget.ScrollView
import android.widget.Spinner
import android.widget.TextView
import androidx.lifecycle.lifecycleScope
import kotlinx.coroutines.launch

class WeselAjaDemoActivity : Activity() {
    private lateinit var amountInput: EditText
    private lateinit var customerNameInput: EditText
    private lateinit var phoneInput: EditText
    private lateinit var bankSpinner: Spinner
    private lateinit var resultText: TextView
    private lateinit var createButton: Button
    private lateinit var statusButton: Button
    private lateinit var webViewButton: Button

    private val repository by lazy {
        WeselAjaPaymentRepository(WeselAjaClient.create())
    }

    private var lastPayment: PaymentInfo? = null

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        buildForm()
        handleReturnIntent(intent?.data?.toString())
    }

    override fun onNewIntent(intent: android.content.Intent?) {
        super.onNewIntent(intent)
        setIntent(intent)
        handleReturnIntent(intent?.data?.toString())
    }

    private fun buildForm() {
        val density = resources.displayMetrics.density
        fun dp(value: Int) = (value * density).toInt()

        val content = LinearLayout(this).apply {
            orientation = LinearLayout.VERTICAL
            setPadding(dp(20), dp(20), dp(20), dp(28))
        }

        currentContentRoot = content

        val scroll = ScrollView(this).apply {
            addView(content)
        }

        content.addView(TextView(this).apply {
            text = "WeselAja Direct Payin"
            textSize = 23f
        }, matchWidthWrap())

        content.addView(TextView(this).apply {
            text = "Create a Virtual Account payment directly through the WeselAja middleware."
            textSize = 14f
        }, marginParams(dp(8), dp(4), dp(8), dp(18)))

        amountInput = editField(
            "Amount (IDR)",
            "10000",
            android.text.InputType.TYPE_CLASS_NUMBER
        )

        customerNameInput = editField(
            "Customer name",
            "Andi",
            android.text.InputType.TYPE_CLASS_TEXT or
                android.text.InputType.TYPE_TEXT_FLAG_CAP_WORDS
        )

        phoneInput = editField(
            "Customer phone (optional)",
            "",
            android.text.InputType.TYPE_CLASS_PHONE
        )

        content.addView(
            label("Bank / Virtual Account channel"),
            marginParams(dp(12), dp(8), dp(4), dp(4))
        )

        bankSpinner = Spinner(this).apply {
            adapter = ArrayAdapter(
                this@WeselAjaDemoActivity,
                android.R.layout.simple_spinner_dropdown_item,
                listOf(
                    "BCA.VA",
                    "BRI.VA",
                    "BNI.VA",
                    "MDR.VA",
                    "PTB.VA",
                    "CIMB.VA"
                )
            )
        }

        content.addView(bankSpinner, matchWidthWrap())

        createButton = Button(this).apply {
            text = "Create Payment"
            setOnClickListener { createPayment() }
        }

        content.addView(
            createButton,
            marginParams(dp(4), dp(20), dp(4), dp(4))
        )

        resultText = TextView(this).apply {
            text = "Payment result will appear here."
            textSize = 14f
        }

        content.addView(
            resultText,
            marginParams(dp(4), dp(16), dp(4), dp(8))
        )

        statusButton = Button(this).apply {
            text = "Check Payment Status"
            visibility = View.GONE
            setOnClickListener { checkStatus() }
        }

        content.addView(statusButton, matchWidthWrap())

        webViewButton = Button(this).apply {
            text = "Open Payment WebView"
            visibility = View.GONE
            setOnClickListener { openPaymentPage() }
        }

        content.addView(webViewButton, matchWidthWrap())

        setContentView(scroll)
    }

    private var currentContentRoot: LinearLayout? = null

    private fun editField(
        hintText: String,
        initialValue: String,
        inputType: Int
    ): EditText {
        val view = EditText(this).apply {
            hint = hintText
            setText(initialValue)
            this.inputType = inputType
            maxLines = 1
        }

        currentContentRoot?.addView(
            view,
            LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT,
                ViewGroup.LayoutParams.WRAP_CONTENT
            )
        )

        return view
    }

    private fun label(value: String) =
        TextView(this).apply {
            text = value
        }

    private fun matchWidthWrap() =
        LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MATCH_PARENT,
            ViewGroup.LayoutParams.WRAP_CONTENT
        )

    private fun marginParams(
        left: Int,
        top: Int,
        right: Int,
        bottom: Int
    ) = LinearLayout.LayoutParams(
        ViewGroup.LayoutParams.MATCH_PARENT,
        ViewGroup.LayoutParams.WRAP_CONTENT
    ).apply {
        setMargins(left, top, right, bottom)
    }

    private fun createPayment() {
        val amount = amountInput.text
            .toString()
            .trim()
            .toLongOrNull()

        val name = customerNameInput.text.toString().trim()
        val phone = phoneInput.text.toString().trim()
            .ifBlank { null }
        val channel = bankSpinner.selectedItem?.toString()
            .orEmpty()

        if (amount == null || amount <= 0) {
            resultText.text =
                "Enter a valid amount greater than zero."
            return
        }

        if (name.isBlank()) {
            resultText.text = "Customer name is required."
            return
        }

        setBusy(true, "Creating payment...")

        lifecycleScope.launch {
            try {
                val payment = repository.createPayment(
                    amount = amount,
                    customerName = name,
                    phoneNumber = phone,
                    paymentChannel = channel,
                    description = "Android Direct Payin"
                )

                lastPayment = payment
                showPayment(payment)
            } catch (error: Exception) {
                resultText.text =
                    "Payment creation failed: " +
                    (error.message ?: "Unknown error")
            } finally {
                setBusy(false)
            }
        }
    }

    private fun checkStatus() {
        val payinId = lastPayment?.middlewareId

        if (payinId.isNullOrBlank()) {
            resultText.text =
                "The response does not contain a Payin ID."
            return
        }

        setBusy(true, "Checking payment status...")

        lifecycleScope.launch {
            try {
                val updated = repository.getPayin(payinId)
                lastPayment = updated
                showPayment(updated)
            } catch (error: Exception) {
                resultText.text =
                    "Status check failed: " +
                    (error.message ?: "Unknown error")
            } finally {
                setBusy(false)
            }
        }
    }

    private fun showPayment(payment: PaymentInfo) {
        val lines = listOfNotNull(
            "Status: ${payment.status ?: "UNKNOWN"}",
            payment.referenceCode?.let { "Reference: $it" },
            payment.middlewareId?.let { "Payin ID: $it" },
            payment.paymentChannel?.let { "Channel: $it" },
            payment.paymentCode?.let { "Payment code: $it" },
            payment.expirationTime?.let { "Expires: $it" }
        )

        resultText.text = lines.joinToString("\n")

        statusButton.visibility =
            if (payment.middlewareId.isNullOrBlank()) {
                View.GONE
            } else {
                View.VISIBLE
            }

        webViewButton.visibility =
            if (payment.webViewUrl.isNullOrBlank()) {
                View.GONE
            } else {
                View.VISIBLE
            }

        if (
            payment.webViewUrl.isNullOrBlank() &&
            !payment.paymentCode.isNullOrBlank()
        ) {
            resultText.append(
                "\n\nThis Direct Payin response contains a VA payment code, not a hosted checkout URL."
            )
        }
    }

    private fun openPaymentPage() {
        val url = lastPayment?.webViewUrl ?: return

        startActivity(
            android.content.Intent(
                this,
                WeselAjaWebViewActivity::class.java
            ).putExtra(
                WeselAjaWebViewActivity.EXTRA_URL,
                url
            )
        )
    }

    private fun setBusy(
        busy: Boolean,
        message: String? = null
    ) {
        createButton.isEnabled = !busy
        statusButton.isEnabled = !busy

        if (message != null) {
            resultText.text = message
        }
    }

    private fun handleReturnIntent(raw: String?) {
        if (raw.isNullOrBlank()) return

        val uri = android.net.Uri.parse(raw)

        if (
            uri.scheme == "weselaja" &&
            uri.host == "payment" &&
            uri.path == "/return"
        ) {
            val reference =
                uri.getQueryParameter("referenceCode")

            if (!reference.isNullOrBlank()) {
                resultText.text =
                    "Returned from payment flow. Reference: $reference. Check status using the Payin ID from the created transaction."
            }
        }
    }
}