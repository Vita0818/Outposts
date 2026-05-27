package com.rokurics.app.data

import org.junit.Assert.*
import org.junit.Test

class CertificatePinnerTest {

    @Test
    fun fingerPrintShortCode_returnsFormattedCode() {
        val fingerprint = "ABCDEF1234567890ABCDEF1234567890ABCDEF1234567890ABCDEF1234567890"
        val short = CertificatePinner.fingerPrintShortCode(fingerprint)
        assertEquals("ABCD...7890", short)
    }

    @Test
    fun fingerPrintShortCode_invalidReturnsReadyNot() {
        val short = CertificatePinner.fingerPrintShortCode("abc")
        assertEquals("未就绪", short)
    }

    @Test
    fun constructor_validatesFingerprintLength() {
        assertThrows(IllegalArgumentException::class.java) {
            CertificatePinner("too-short")
        }
    }

    @Test
    fun constructor_acceptsValidFingerprint() {
        val pinner = CertificatePinner("ABCDEF1234567890ABCDEF1234567890ABCDEF1234567890ABCDEF1234567890")
        assertNotNull(pinner)
    }
}
