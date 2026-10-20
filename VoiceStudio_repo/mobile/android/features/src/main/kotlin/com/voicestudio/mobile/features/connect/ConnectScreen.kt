package com.voicestudio.mobile.features.connect

import android.Manifest
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.runtime.saveable.rememberSaveable
import com.voicestudio.mobile.features.*
import com.voicestudio.mobile.network.PairingAddress
import java.text.DateFormat
import java.util.Date

@Composable fun ConnectScreen(vm:StudioViewModel) {
    val state by vm.state.collectAsState()
    val prefs by vm.preferences.collectAsState()
    var url by rememberSaveable {mutableStateOf("")}
    var pin by remember {mutableStateOf("")}
    var key by remember {mutableStateOf("")}
    var scanner by remember {mutableStateOf(false)}
    val camera=permissionAction(Manifest.permission.CAMERA,"mobile.permissions.camera",vm::fail) {scanner=true}
    Screen {
        ScreenTitle("mobile.connect.title")
        Text(tr("mobile.connect.subtitle"))
        Text(tr("mobile.connect.scan_help"),color=MaterialTheme.colorScheme.onSurfaceVariant)
        Secondary("mobile.connect.scan_qr",!state.busy,camera)
        if(scanner) QrScanner(onCode={raw ->
            try {val address=PairingAddress.parse(raw);url=address.baseUrl.toString();pin=address.pin.orEmpty();scanner=false}
            catch(e:Exception) {scanner=false;vm.fail(e)}
        },onClose={scanner=false},onError={scanner=false;vm.fail(it)})
        Field("mobile.connect.url_label",url,{url=it})
        Field("mobile.connect.pin_label",pin,{pin=it},secret=true)
        Field("mobile.connect.api_key_label",key,{key=it},secret=true)
        Text(tr("mobile.connect.api_key_help"),color=MaterialTheme.colorScheme.onSurfaceVariant)
        Text(tr("mobile.permissions.local_network"),color=MaterialTheme.colorScheme.onSurfaceVariant)
        Action(if(state.busy) "mobile.connect.connecting" else "mobile.connect.connect",url.isNotBlank()&&!state.busy) {
            val transientKey=key;key="";vm.connect(url,pin,transientKey)
        }
        if(prefs.hosts.isNotEmpty()) Heading("mobile.connect.saved_hosts")
        prefs.hosts.forEach {host ->
            Text(host.label,style=MaterialTheme.typography.titleMedium)
            Text(host.url)
            Text(tr("mobile.connect.last_seen",mapOf("time" to DateFormat.getDateTimeInstance().format(Date(host.lastSeen)))))
            Secondary("mobile.connect.connect",!state.busy) {url=host.url;vm.reconnect(host)}
            Secondary("mobile.connect.forget",!state.busy) {vm.forget(host.url)}
        }
    }
}
