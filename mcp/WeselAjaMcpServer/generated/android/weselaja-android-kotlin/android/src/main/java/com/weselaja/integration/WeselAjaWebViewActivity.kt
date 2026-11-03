package com.weselaja.integration

import android.app.Activity
import android.os.Bundle
import android.net.Uri
import android.webkit.WebChromeClient
import android.webkit.WebResourceRequest
import android.webkit.WebView
import android.webkit.WebViewClient
import android.widget.Toast
import android.view.ViewGroup

class WeselAjaWebViewActivity : Activity() {
    companion object {
        const val EXTRA_URL =
            "com.weselaja.integration.EXTRA_PAYMENT_URL"
    }

    private var paymentWebView: WebView? = null

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        val url = intent.getStringExtra(EXTRA_URL).orEmpty()
        val uri = runCatching { Uri.parse(url) }.getOrNull()

        if (
            uri == null ||
            uri.scheme != "https" ||
            uri.host.isNullOrBlank()
        ) {
            Toast.makeText(
                this,
                "Invalid payment URL.",
                Toast.LENGTH_LONG
            ).show()

            finish()
            return
        }

        val webView = WebView(this)
        paymentWebView = webView

        webView.settings.javaScriptEnabled = true
        webView.settings.domStorageEnabled = true
        webView.settings.allowFileAccess = false
        webView.settings.allowContentAccess = false
        webView.settings.mixedContentMode =
            android.webkit.WebSettings.MIXED_CONTENT_NEVER_ALLOW

        webView.webChromeClient = WebChromeClient()

        webView.webViewClient = object : WebViewClient() {
            override fun shouldOverrideUrlLoading(
                view: WebView?,
                request: WebResourceRequest?
            ): Boolean {
                val target = request?.url ?: return true

                if (target.scheme == "https") {
                    return false
                }

                if (target.scheme == "weselaja") {
                    setResult(
                        RESULT_OK,
                        intent.setData(target)
                    )

                    finish()
                    return true
                }

                return true
            }
        }

        setContentView(webView)
        webView.loadUrl(uri.toString())
    }

    override fun onDestroy() {
        paymentWebView?.apply {
            stopLoading()
            loadUrl("about:blank")
            removeAllViews()
            (parent as? ViewGroup)?.removeView(this)
            destroy()
        }

        paymentWebView = null
        super.onDestroy()
    }
}