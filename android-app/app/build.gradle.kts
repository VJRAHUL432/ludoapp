plugins {
    alias(libs.plugins.android.application)
    alias(libs.plugins.kotlin.android)
    alias(libs.plugins.kotlin.compose)
}

android {
    namespace = "com.ludo.app"
    compileSdk = 34

    defaultConfig {
        applicationId = "com.ludo.app"
        minSdk = 26
        targetSdk = 34
        versionCode = 1
        versionName = "1.0.0"

        // Used by your code (in BuildConfig.AD_*) — switch to false and
        // paste real IDs before publishing to Play Store.
        buildConfigField("boolean", "USE_TEST_ADS", "true")
        buildConfigField("String",  "AD_APP_ID",          "\"ca-app-pub-3940256099942544~3347511713\"")
        buildConfigField("String",  "AD_BANNER_UNIT",     "\"ca-app-pub-3940256099942544/6300978111\"")
        buildConfigField("String",  "AD_INTERSTITIAL_UNIT","\"ca-app-pub-3940256099942544/1033173712\"")
        buildConfigField("String",  "AD_REWARDED_UNIT",   "\"ca-app-pub-3940256099942544/5224354917\"")
    }

    buildTypes {
        release {
            isMinifyEnabled = true
            proguardFiles(
                getDefaultProguardFile("proguard-android-optimize.txt"),
                "proguard-rules.pro"
            )
        }
    }
    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }
    kotlinOptions {
        jvmTarget = "17"
    }
    buildFeatures {
        compose = true
        buildConfig = true
    }
    packaging {
        resources {
            excludes += "/META-INF/{AL2.0,LGPL2.1}"
        }
    }
}

dependencies {
    implementation(libs.androidx.core.ktx)
    implementation(libs.androidx.lifecycle.runtime.ktx)
    implementation(libs.androidx.activity.compose)
    implementation(platform(libs.androidx.compose.bom))
    implementation(libs.androidx.ui)
    implementation(libs.androidx.ui.graphics)
    implementation(libs.androidx.ui.tooling.preview)
    implementation(libs.androidx.material3)
    implementation(libs.play.services.ads)
}
