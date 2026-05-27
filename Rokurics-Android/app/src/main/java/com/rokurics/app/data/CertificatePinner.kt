package com.rokurics.app.data

import java.security.MessageDigest
import java.security.cert.X509Certificate
import javax.net.ssl.HttpsURLConnection
import javax.net.ssl.SSLContext
import javax.net.ssl.TrustManager
import javax.net.ssl.TrustManagerFactory
import javax.net.ssl.X509TrustManager
import javax.net.ssl.HostnameVerifier

class CertificatePinner(private val expectedSha256Fingerprint: String) {

    private val normalizedFingerprint: String =
        SecureUploadUtilities.normalizedCertificateFingerprint(expectedSha256Fingerprint)

    init {
        require(normalizedFingerprint.length == 64) {
            "Certificate fingerprint must be 64 hex characters, got ${normalizedFingerprint.length}"
        }
    }

    fun applyTo(connection: HttpsURLConnection) {
        val trustManager = createTrustManager()
        val sslContext = SSLContext.getInstance("TLS")
        sslContext.init(null, arrayOf<TrustManager>(trustManager), null)
        connection.sslSocketFactory = sslContext.socketFactory
        connection.hostnameVerifier = PinningHostnameVerifier()
    }

    private fun createTrustManager(): X509TrustManager {
        val systemTrustManager = createDefaultTrustManager()
        val expectedFingerprint = normalizedFingerprint

        return object : X509TrustManager {
            override fun checkClientTrusted(chain: Array<out X509Certificate>?, authType: String?) {
                systemTrustManager.checkClientTrusted(chain, authType)
            }

            override fun checkServerTrusted(chain: Array<out X509Certificate>?, authType: String?) {
                systemTrustManager.checkServerTrusted(chain, authType)
                if (chain == null || chain.isEmpty()) {
                    throw SecurityException("certificate_mismatch: no certificate chain")
                }
                val serverCert = chain[0]
                val actualFingerprint = computeSha256Fingerprint(serverCert)
                if (actualFingerprint != expectedFingerprint) {
                    throw SecurityException("certificate_mismatch: expected $expectedFingerprint but got $actualFingerprint")
                }
            }

            override fun getAcceptedIssuers(): Array<X509Certificate> = systemTrustManager.acceptedIssuers
        }
    }

    private fun createDefaultTrustManager(): X509TrustManager {
        val tmf = TrustManagerFactory.getInstance(TrustManagerFactory.getDefaultAlgorithm())
        tmf.init(null as java.security.KeyStore?)
        val managers = tmf.trustManagers
        for (manager in managers) {
            if (manager is X509TrustManager) return manager
        }
        throw IllegalStateException("No default X509TrustManager found")
    }

    private inner class PinningHostnameVerifier : HostnameVerifier {
        override fun verify(hostname: String?, session: javax.net.ssl.SSLSession?): Boolean {
            // Accept all hostnames for local network connections when pinning is active
            return true
        }
    }

    companion object {
        fun computeSha256Fingerprint(certificate: X509Certificate): String {
            val digest = MessageDigest.getInstance("SHA-256")
            val encoded = digest.digest(certificate.encoded)
            return encoded.joinToString("") { "%02x".format(it) }
        }

        fun fingerPrintShortCode(fingerprint: String): String {
            val normalized = SecureUploadUtilities.normalizedCertificateFingerprint(fingerprint)
            if (normalized.length < 8) return "未就绪"
            return "${normalized.take(4)}...${normalized.takeLast(4)}"
        }
    }
}
