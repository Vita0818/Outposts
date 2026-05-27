package com.vita0818.kikaria.ui.theme

import androidx.compose.ui.unit.Dp
import org.junit.Assert.*
import org.junit.Test

class KikariaPhoneMetricsTest {

    // ── Compact phone width (Pixel 8: 1080x2400 px, 420dpi -> ~411dp x ~914dp) ──

    @Test
    fun `compact phone horizontal padding is 24dp for width 360 or above`() {
        val m = KikariaPhoneMetrics.compute(
            widthPx = 1080f, heightPx = 2400f, density = 2.625f
        )
        assertEquals(24f, m.horizontalPadding.value, 0.01f)
    }

    @Test
    fun `narrow phone horizontal padding is 20dp for width below 360`() {
        val m = KikariaPhoneMetrics.compute(
            widthPx = 720f, heightPx = 1280f, density = 2.0f
        )
        assertEquals(24f, m.horizontalPadding.value, 0.01f)

        val m2 = KikariaPhoneMetrics.compute(
            widthPx = 640f, heightPx = 1136f, density = 2.0f
        )
        assertEquals(20f, m2.horizontalPadding.value, 0.01f)
    }

    // ── Scale bounds (compact phone -> all scales = 1) ──

    @Test
    fun `all scales are 1 for compact phone`() {
        val m = KikariaPhoneMetrics.compute(
            widthPx = 1080f, heightPx = 2400f, density = 2.625f
        )
        assertEquals(1f, m.homeScale)
        assertEquals(1f, m.headerScale)
        assertEquals(1f, m.reviewScale)
        assertEquals(1f, m.reviewButtonScale)
        assertEquals(1f, m.cardScale)
        assertEquals(1f, m.scopeScale)
        assertEquals(1f, m.overviewScale)
        assertEquals(1f, m.settingsScale)
        assertEquals(1f, m.settingsRowScale)
        assertEquals(1f, m.presetScale)
        assertEquals(1f, m.newPresetScale)
        assertEquals(1f, m.listCardScale)
    }

    // ── Max width (compact -> unspecified / infinity) ──

    @Test
    fun `max widths are unspecified for compact phone`() {
        val m = KikariaPhoneMetrics.compute(
            widthPx = 1080f, heightPx = 2400f, density = 2.625f
        )
        assertEquals(Dp.Unspecified, m.homeMaxWidth)
        assertEquals(Dp.Unspecified, m.mainMaxWidth)
        assertEquals(Dp.Unspecified, m.formMaxWidth)
        assertEquals(Dp.Unspecified, m.reviewMaxWidth)
        assertEquals(Dp.Unspecified, m.contentMaxWidth)
    }

    // ── Phone classification ──

    @Test
    fun `Pixel 8 is compact phone not tablet`() {
        val m = KikariaPhoneMetrics.compute(
            widthPx = 1080f, heightPx = 2400f, density = 2.625f
        )
        assertTrue(m.isCompactPhone)
        assertFalse(m.isTablet)
    }

    @Test
    fun `iPhone SE sized device is compact phone`() {
        val m = KikariaPhoneMetrics.compute(
            widthPx = 750f, heightPx = 1334f, density = 2.0f
        )
        assertTrue(m.isCompactPhone)
        assertFalse(m.isTablet)
    }

    @Test
    fun `compact phone has no iPad portrait classification`() {
        val m = KikariaPhoneMetrics.compute(
            widthPx = 1080f, heightPx = 2400f, density = 2.625f
        )
        assertFalse(m.isPadPortrait)
        assertFalse(m.isPadLandscape)
        assertFalse(m.isTwoColumnCapable)
    }

    // ── Review bottom padding ──

    @Test
    fun `review action bottom padding is 16 for compact`() {
        val m = KikariaPhoneMetrics.compute(
            widthPx = 1080f, heightPx = 2400f, density = 2.625f
        )
        assertEquals(16f, m.reviewActionBottomPadding.value, 0.01f)
    }

    // ── Back button size ──

    @Test
    fun `back button size is 42dp`() {
        val m = KikariaPhoneMetrics.compute(
            widthPx = 1080f, heightPx = 2400f, density = 2.625f
        )
        assertEquals(42f, m.backButtonSize.value, 0.01f)
    }

    // ── New preset text editor height bounds ──

    @Test
    fun `new preset text editor min height is 260dp`() {
        val m = KikariaPhoneMetrics.compute(
            widthPx = 1080f, heightPx = 2400f, density = 2.625f
        )
        assertEquals(260f, m.newPresetTextEditorMinHeight.value, 0.01f)
    }

    @Test
    fun `new preset text editor max height is 55 percent of screen height`() {
        val m = KikariaPhoneMetrics.compute(
            widthPx = 1080f, heightPx = 2400f, density = 2.625f
        )
        val expectedMax = (2400f / 2.625f) * 0.55f
        assertEquals(expectedMax, m.newPresetTextEditorMaxHeight.value, 0.01f)
    }

    // ── Scope grid ──

    @Test
    fun `scope grid minimum width is 132dp for compact`() {
        val m = KikariaPhoneMetrics.compute(
            widthPx = 1080f, heightPx = 2400f, density = 2.625f
        )
        assertEquals(132f, m.scopeGridMinimumWidth.value, 0.01f)
    }

    // ── Tablet classification (10" landscape tablet: 1920x1200 px, 224dpi -> ~858dp x ~536dp) ──

    @Test
    fun `tablet is classified as tablet and not compact`() {
        val m = KikariaPhoneMetrics.compute(
            widthPx = 1920f, heightPx = 1200f, density = 2.24f
        )
        val w = 1920f / 2.24f // ~857dp
        val h = 1200f / 2.24f // ~536dp
        assertFalse(m.isCompactPhone)
        assertTrue(m.isTablet)
        assertFalse(m.isPadPortrait) // height < width -> landscape
        assertTrue(m.isPadLandscape)
    }

    @Test
    fun `tablet landscape has correct horizontal padding by width tier`() {
        // widePad (>= 900dp): 40dp
        val mWide = KikariaPhoneMetrics.compute(
            widthPx = 2240f, heightPx = 1400f, density = 1.4f
        )
        // 2240/1.4 = 1600dp -> widePad
        assertEquals(40f, mWide.horizontalPadding.value, 0.01f)

        // regularPad (600-899dp): 32dp
        val mReg = KikariaPhoneMetrics.compute(
            widthPx = 1200f, heightPx = 1800f, density = 2.0f
        )
        // 1200/2 = 600dp -> regularPad
        assertEquals(32f, mReg.horizontalPadding.value, 0.01f)
    }

    @Test
    fun `tablet landscape has bounded max widths`() {
        val m = KikariaPhoneMetrics.compute(
            widthPx = 2240f, heightPx = 1400f, density = 1.6f
        )
        // 2240/1.6 = 1400dp -> widePad landscape
        assertEquals(780f, m.homeMaxWidth.value, 0.01f)
        assertEquals(820f, m.reviewMaxWidth.value, 0.01f)
        assertEquals(640f, m.formMaxWidth.value, 0.01f)
    }

    @Test
    fun `tablet landscape review uses two column when twoColumnCapable`() {
        val m = KikariaPhoneMetrics.compute(
            widthPx = 2240f, heightPx = 1400f, density = 1.6f
        )
        // 2240/1.6 = 1400dp, >= 950, landscape -> twoColumnCapable
        assertTrue(m.isTwoColumnCapable)
        assertTrue(m.reviewUsesTwoColumnLayout)
    }

    @Test
    fun `small tablet under 950dp does not use two column`() {
        val m = KikariaPhoneMetrics.compute(
            widthPx = 1200f, heightPx = 1800f, density = 2.0f
        )
        // 1200/2 = 600dp -> regularPad portrait
        assertTrue(m.isTablet)
        assertTrue(m.isPadPortrait)
        assertFalse(m.isTwoColumnCapable)
        assertFalse(m.reviewUsesTwoColumnLayout)
    }

    // ── Tablet portrait scaling ──

    @Test
    fun `tablet portrait applies per-page scale factors`() {
        val m = KikariaPhoneMetrics.compute(
            widthPx = 1200f, heightPx = 1800f, density = 2.0f
        )
        // 600dp portrait -> isPadPortrait, regularPad
        assertTrue(m.isPadPortrait)
        assertEquals(1.30f, m.homeScale, 0.01f)
        assertEquals(1.16f, m.headerScale, 0.01f)
        assertEquals(1.18f, m.reviewScale, 0.01f)
        assertEquals(1.14f, m.reviewButtonScale, 0.01f) // iPad portrait: 1.14
        assertEquals(1.18f, m.cardScale, 0.01f)
        assertEquals(1.10f, m.settingsScale, 0.01f)
    }

    @Test
    fun `large iPad portrait applies higher scale factors`() {
        val m = KikariaPhoneMetrics.compute(
            widthPx = 1800f, heightPx = 2400f, density = 2.0f
        )
        // 900dp portrait -> isLargePadPortrait
        assertTrue(m.isPadPortrait)
        assertTrue(m.isLargePadPortrait)
        assertEquals(1.36f, m.homeScale, 0.01f)
        assertEquals(1.20f, m.headerScale, 0.01f)
        assertEquals(1.20f, m.reviewScale, 0.01f)
        assertEquals(1.18f, m.reviewButtonScale, 0.01f)
        assertEquals(1.24f, m.cardScale, 0.01f)
        assertEquals(1.16f, m.settingsScale, 0.01f)
    }

    @Test
    fun `tablet portrait has larger scope grid`() {
        val m = KikariaPhoneMetrics.compute(
            widthPx = 1200f, heightPx = 1800f, density = 2.0f
        )
        assertTrue(m.isPadPortrait)
        assertEquals(164f, m.scopeGridMinimumWidth.value, 0.01f)
        assertEquals(16f, m.scopeGridSpacing.value, 0.01f)
    }

    @Test
    fun `large iPad portrait has larger scope grid`() {
        val m = KikariaPhoneMetrics.compute(
            widthPx = 1800f, heightPx = 2400f, density = 2.0f
        )
        assertEquals(176f, m.scopeGridMinimumWidth.value, 0.01f)
    }

    @Test
    fun `tablet portrait review action bottom padding depends on height`() {
        val mTall = KikariaPhoneMetrics.compute(
            widthPx = 1200f, heightPx = 1800f, density = 2.0f
        )
        // portrait, height=900dp >= 760 -> 34dp
        assertEquals(34f, mTall.reviewActionBottomPadding.value, 0.01f)

        val mShort = KikariaPhoneMetrics.compute(
            widthPx = 1200f, heightPx = 1400f, density = 2.0f
        )
        // portrait, height=700dp < 760 -> 24dp
        assertEquals(24f, mShort.reviewActionBottomPadding.value, 0.01f)
    }

    @Test
    fun `tablet portrait has top insets`() {
        val m = KikariaPhoneMetrics.compute(
            widthPx = 1200f, heightPx = 1800f, density = 2.0f
        )
        assertEquals(38f, m.ipadPortraitListPageTopInset.value, 0.01f)
        assertEquals(36f, m.ipadPortraitOverviewTopInset.value, 0.01f)
        assertEquals(38f, m.ipadPortraitFormPageTopInset.value, 0.01f)
        assertEquals(38f, m.ipadPortraitSettingsTopInset.value, 0.01f)
        assertEquals(84f, m.ipadPortraitPageTitleTopInset.value, 0.01f)
    }

    @Test
    fun `compact phone has zero iPad top insets`() {
        val m = KikariaPhoneMetrics.compute(
            widthPx = 1080f, heightPx = 2400f, density = 2.625f
        )
        assertEquals(0f, m.ipadPortraitListPageTopInset.value, 0.01f)
        assertEquals(0f, m.ipadPortraitPageTitleTopInset.value, 0.01f)
    }

    @Test
    fun `compact phone has no two column layout`() {
        val m = KikariaPhoneMetrics.compute(
            widthPx = 1080f, heightPx = 2400f, density = 2.625f
        )
        assertFalse(m.homeUsesTwoColumnLayout)
    }
}
