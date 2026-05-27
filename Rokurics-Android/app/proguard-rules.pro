# Add project specific ProGuard rules here.
-keepattributes Signature
-keepattributes *Annotation*

# Kotlin serialization
-keepclassmembers class kotlinx.serialization.json.** { *** Companion; }
-keepclasseswithmembers class kotlinx.serialization.json.** { kotlinx.serialization.KSerializer serializer(...); }
