package com.voicestudio.mobile.features.dictate

import android.Manifest
import kotlinx.coroutines.launch
import android.content.ClipData
import android.content.ClipboardManager
import androidx.compose.foundation.gestures.detectTapGestures
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.input.pointer.pointerInput
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.semantics.*
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.LifecycleEventObserver
import androidx.lifecycle.compose.LocalLifecycleOwner
import com.voicestudio.mobile.features.*
import com.voicestudio.mobile.designsystem.*
import com.voicestudio.mobile.network.*
import kotlinx.coroutines.CancellationException

@Composable fun DictateScreen(vm:StudioViewModel) {
    val context=LocalContext.current
    val scope=rememberCoroutineScope()
    val api=vm.api ?: return
    val controller=remember(api) {DictationController(api,scope,vm::fail)}
    val transcript by controller.text.collectAsState()
    val active by controller.active.collectAsState()
    val lifecycle=LocalLifecycleOwner.current
    var readiness by remember {mutableStateOf<Readiness?>(null)}
    var checking by remember {mutableStateOf(true)}
    var readinessError by remember {mutableStateOf<ClientFailure?>(null)}
    suspend fun check() {
        checking=true;readinessError=null
        try {readiness=api.readiness()}
        catch(e:CancellationException) {throw e}
        catch(e:Exception) {readiness=null;readinessError=e as? ClientFailure ?: ClientFailure("mobile.connect.error_unreachable",mapOf("host" to api.address.baseUrl.host))}
        finally {checking=false}
    }
    LaunchedEffect(api) {check()}
    val ready=readiness?.ready==true && !checking
    DisposableEffect(lifecycle,controller) {
        val observer=LifecycleEventObserver {_,event -> if(event==Lifecycle.Event.ON_STOP) controller.close()}
        lifecycle.lifecycle.addObserver(observer)
        onDispose {lifecycle.lifecycle.removeObserver(observer);controller.close()}
    }
    var holding by remember {mutableStateOf(false)}
    val holdStart=permissionAction(Manifest.permission.RECORD_AUDIO,"mobile.permissions.microphone",vm::fail) {if(holding) controller.start()}
    val start=permissionAction(Manifest.permission.RECORD_AUDIO,"mobile.permissions.microphone",vm::fail,controller::start)
    Screen {
        ScreenTitle("mobile.tabs.dictate")
        if(checking) {Text(tr("common.loading"));LinearProgressIndicator()}
        readinessError?.let {error ->
            Text(LocalL10n.current.t(error.key,error.params),color=role("error"))
            if(error.status in listOf(401,403) && error.key!="mobile.errors.model_licence_required") Text(tr("mobile.dictate.authorization"))
        }
        if(!checking && readinessError==null && !ready) {
            val (key,params)=(readiness ?: Readiness(false)).guidance()
            Text(tr(key,params))
        }
        Secondary("common.refresh",!checking) {scope.launch {check()}}
        Action(if(active) "common.stop" else "mobile.dictate.tap_to_start",ready||active) {if(active) controller.finish() else start()}
        val hold=tr("mobile.dictate.hold_to_talk")
        Surface(modifier=Modifier.fillMaxWidth().heightIn(min=VSTheme.touchTarget).semantics {
            role=Role.Button;contentDescription=hold;onClick {if(active) controller.finish() else start();true}
        }.pointerInput(ready) {
            detectTapGestures(onPress={if(ready) {holding=true;holdStart();tryAwaitRelease();holding=false;controller.finish()}})
        },shape=MaterialTheme.shapes.small,color=role("surface")) {
            Text(hold,Modifier.padding(VSTheme.inset))
        }
        if(active) Text(tr("mobile.dictate.listening"))
        Text(transcript,modifier=Modifier.semantics {liveRegion=LiveRegionMode.Polite})
        Secondary("mobile.dictate.copy",transcript.isNotBlank()) {
            (context.getSystemService(ClipboardManager::class.java)).setPrimaryClip(ClipData.newPlainText(null,transcript))
        }
        Secondary("mobile.speak.share",transcript.isNotBlank()) {shareText(context,transcript)}
    }
}
