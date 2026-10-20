package com.voicestudio.mobile.features.settings

import android.Manifest
import androidx.compose.ui.Modifier
import android.content.Intent
import android.net.Uri
import android.os.Build
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.platform.LocalContext
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import com.voicestudio.mobile.features.*
import com.voicestudio.mobile.designsystem.*
import com.voicestudio.mobile.l10n.L10n

@Composable fun SettingsScreen(vm:StudioViewModel,version:String) {
    val prefs by vm.preferences.collectAsState()
    val state by vm.state.collectAsState()
    val context=LocalContext.current
    val l=LocalL10n.current
    var licenses by remember {mutableStateOf(false)}
    val notification=permissionAction(Manifest.permission.POST_NOTIFICATIONS,"mobile.settings.notifications_help",vm::fail) {vm.preference {it.copy(notifications=true)}}
    Screen {
        ScreenTitle("mobile.tabs.settings")
        if(state.connected) {
            Heading("mobile.settings.connection")
            Text(prefs.activeUrl.orEmpty())
            Secondary("mobile.connect.disconnect",!state.busy,vm::disconnect)
            prefs.hosts.forEach {host ->
                Text(host.label)
                Field("mobile.android.host_label",host.label,{label -> vm.preference {it.copy(hosts=it.hosts.map {h -> if(h.url==host.url) h.copy(label=label) else h})}})
                if(host.url!=prefs.activeUrl) {
                    Secondary("mobile.connect.connect",!state.busy) {vm.reconnect(host)}
                    Secondary("mobile.connect.forget",!state.busy) {vm.forget(host.url)}
                }
            }
            Heading("mobile.settings.appearance")
            val palettes=paletteChoices(l)
            Group {
            Picker("mobile.settings.mode",prefs.mode,listOf("dark","light","system").map {it to l.t("mobile.settings.mode_$it")}) {value -> vm.preference {it.copy(mode=value)}}
            Picker("mobile.settings.light_palette",prefs.lightPalette,palettes) {value -> vm.preference {it.copy(lightPalette=value)}}
            Picker("mobile.settings.dark_palette",prefs.darkPalette,palettes) {value -> vm.preference {it.copy(darkPalette=value)}}
            }
            Card(colors=CardDefaults.cardColors(containerColor=role("surface")),shape=MaterialTheme.shapes.large) {Column(Modifier.padding(VSTheme.inset),verticalArrangement=Arrangement.spacedBy(VSTheme.gap)) {
                Text(tr("mobile.android.theme_preview"));Secondary("mobile.speak.generate",onClick={})
            }}
            Heading("mobile.settings.language")
            Picker("mobile.settings.language",prefs.locale,listOf("system" to l.t("mobile.settings.mode_system"))+L10n.languages.map {it to languageName(it,it)}) {value -> vm.preference {it.copy(locale=value)}}
            Heading("mobile.settings.notifications")
            Text(tr("mobile.settings.notifications_help"))
            Toggle("mobile.settings.notifications",prefs.notifications) {enabled -> if(enabled&&Build.VERSION.SDK_INT>=33) notification() else vm.preference {it.copy(notifications=enabled)}}
        }
        Heading("mobile.settings.privacy")
        Text(tr("mobile.settings.privacy_help"))
        Text(tr("mobile.android.analytics_local"))
        Toggle("consent.title",prefs.consent==true) {vm.consent(it)}
        Heading("mobile.settings.about")
        Text(tr("mobile.android.app_name"),style=MaterialTheme.typography.titleMedium)
        Text(tr("mobile.settings.version",mapOf("version" to version)))
        Secondary("mobile.settings.licenses") {licenses=true}
        if(state.connected) Secondary("mobile.android.model_licenses") {context.startActivity(Intent(Intent.ACTION_VIEW,Uri.parse(vm.api!!.address.baseUrl.toString())))}
    }
    if(licenses) AlertDialog(shape=MaterialTheme.shapes.large,onDismissRequest={licenses=false},title={Text(tr("mobile.settings.licenses"))},text={
        val content=remember {context.assets.open("licenses/OFL.txt").bufferedReader().use {it.readText()}+"\n\n"+context.assets.open("licenses/Apache-2.0.txt").bufferedReader().use {it.readText()}+"\n\n"+context.assets.open("licenses/VoiceStudio-AGPL-3.0.txt").bufferedReader().use {it.readText()}+"\n\n"+context.assets.open("licenses/NOTICE.txt").bufferedReader().use {it.readText()}}
        Text(content,Modifier.verticalScroll(rememberScrollState()))
    },confirmButton={TextButton(shape=MaterialTheme.shapes.small,onClick={licenses=false}) {Text(tr("common.close"))}})
}
