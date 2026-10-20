package com.voicestudio.mobile.features.voices

import android.Manifest
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.material3.*
import androidx.compose.foundation.layout.*
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import com.voicestudio.mobile.designsystem.*
import androidx.compose.runtime.*
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.ui.platform.LocalContext
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.LifecycleEventObserver
import androidx.lifecycle.compose.LocalLifecycleOwner
import com.voicestudio.mobile.features.*
import com.voicestudio.mobile.network.Profile
import kotlinx.coroutines.*
import java.io.File

@Composable fun VoicesScreen(vm:StudioViewModel) {
    val state by vm.state.collectAsState()
    val context=LocalContext.current
    val scope=rememberCoroutineScope()
    val lifecycle=LocalLifecycleOwner.current
    var cloning by rememberSaveable {mutableStateOf(false)}
    var query by rememberSaveable {mutableStateOf("")}
    var name by rememberSaveable {mutableStateOf("")}
    var sample by rememberSaveable {mutableStateOf<String?>(null)}
    var permission by rememberSaveable {mutableStateOf(false)}
    var recording by remember {mutableStateOf(false)}
    var seconds by remember {mutableIntStateOf(0)}
    var edit by remember {mutableStateOf<Profile?>(null)}
    var editName by remember {mutableStateOf("")}
    var delete by remember {mutableStateOf<Profile?>(null)}
    var preview by remember {mutableStateOf<String?>(null)}
    val recorder=remember {SampleRecorder(context)}
    DisposableEffect(lifecycle) {
        val observer=LifecycleEventObserver {_,event -> if(event==Lifecycle.Event.ON_STOP && recording) {recorder.discard();recording=false;sample=null}}
        lifecycle.lifecycle.addObserver(observer)
        onDispose {lifecycle.lifecycle.removeObserver(observer);if(recording) {recorder.discard();sample=null}}
    }
    LaunchedEffect(recording) {seconds=0;while(recording) {delay(1000);seconds++}}
    val record=permissionAction(Manifest.permission.RECORD_AUDIO,"mobile.permissions.microphone",vm::fail) {
        try {sample?.let {File(it).delete()};sample=recorder.start().absolutePath;recording=true} catch(e:Exception) {vm.fail(e)}
    }
    val pick=rememberLauncherForActivityResult(ActivityResultContracts.OpenDocument()) {uri ->
        if(uri!=null) scope.launch {try {sample?.let {File(it).delete()};sample=copyMedia(context,uri,"audio").absolutePath} catch(e:Exception) {vm.fail(e)}}
    }
    Screen {
        ScreenTitle("mobile.tabs.voices")
        if(!cloning) Action("mobile.voices.new_clone") {cloning=true}
        if(cloning) {
        Heading("mobile.voices.new_clone")
        Field("mobile.voices.name_label",name,{name=it})
        Text(tr("mobile.voices.consent_title"),style=MaterialTheme.typography.titleMedium)
        Text(tr("mobile.voices.consent_body"))
        Toggle("mobile.voices.consent_confirm",permission,{permission=it})
        Secondary("mobile.voices.pick_file",!recording) {pick.launch(arrayOf("audio/*"))}
        if(recording) {
            Text(tr("mobile.voices.recording",mapOf("seconds" to seconds)))
            Secondary("common.stop") {try {sample=recorder.stop().absolutePath;recording=false} catch(e:Exception) {recording=false;sample=null;vm.fail(e)}}
        } else Secondary("mobile.voices.record",onClick=record)
        if(sample!=null&&!recording) Text(tr("mobile.android.sample_ready"))
        Action("mobile.voices.new_clone",permission&&sample!=null&&name.isNotBlank()&&!state.busy&&!recording) {vm.clone(name,File(sample!!));sample=null;name="";permission=false;cloning=false}
        Secondary("common.cancel",!recording) {cloning=false;sample?.let {File(it).delete()};sample=null}
        }
        Row(Modifier.fillMaxWidth(),verticalAlignment=Alignment.CenterVertically) {
            Box(Modifier.weight(1f)) {Field("mobile.voices.search",query,{query=it})}
            IconAction("common.refresh",Icons.Default.Refresh,!state.busy,onClick=vm::refresh)
        }
        val matches=state.profiles.filter {it.name.contains(query,true)}
        if(query.isNotBlank() && matches.isEmpty()) SearchEmpty(query)
        else if(state.catalogLoading) InlineLoading()
        else if(state.profiles.isEmpty()) Text(tr("mobile.voices.empty"),color=role("textMuted"))
        Group {
            matches.forEach {profile ->
                var menu by remember {mutableStateOf(false)}
                Row(Modifier.fillMaxWidth().padding(VSTheme.inset),verticalAlignment=Alignment.CenterVertically) {
                    Column(Modifier.weight(1f)) {
                        Text(profile.name,style=MaterialTheme.typography.titleMedium)
                        profile.language?.let {Text(it,style=MaterialTheme.typography.bodySmall,color=role("textMuted"))}
                    }
                    IconAction("player.play",Icons.Default.PlayArrow,profile.audioUrl!=null) {preview=profile.audioUrl}
                    Box {
                        IconAction("mobile.android.more_actions",Icons.Default.MoreVert,!state.busy,mapOf("name" to profile.name)) {menu=true}
                        DropdownMenu(expanded=menu,onDismissRequest={menu=false},shape=MaterialTheme.shapes.large,containerColor=role("surfaceOverlay")) {
                            DropdownMenuItem(text={Text(tr("mobile.voices.rename"))},onClick={menu=false;edit=profile;editName=profile.name})
                            DropdownMenuItem(text={Text(tr("common.delete"))},onClick={menu=false;delete=profile})
                        }
                    }
                }
                HorizontalDivider()
            }
        }
        if(preview!=null && vm.api!=null) HostPlayer(vm.api!!,preview!!,vm::fail)

    }
    if(edit!=null) AlertDialog(modifier=Modifier.dismissKeyboardOnTap(),shape=MaterialTheme.shapes.large,onDismissRequest={edit=null},title={Text(tr("mobile.voices.rename"))},text={Field("mobile.voices.name_label",editName,{editName=it})},
        confirmButton={TextButton(shape=MaterialTheme.shapes.small,onClick={vm.rename(edit!!.id,editName);edit=null},enabled=editName.isNotBlank()) {Text(tr("common.save"))}},dismissButton={TextButton(shape=MaterialTheme.shapes.small,onClick={edit=null}) {Text(tr("common.cancel"))}})
    if(delete!=null) AlertDialog(shape=MaterialTheme.shapes.large,onDismissRequest={delete=null},text={Text(tr("mobile.voices.delete_confirm",mapOf("name" to delete!!.name)))},
        confirmButton={TextButton(shape=MaterialTheme.shapes.small,onClick={vm.deleteProfile(delete!!.id);delete=null}) {Text(tr("common.delete"))}},dismissButton={TextButton(shape=MaterialTheme.shapes.small,onClick={delete=null}) {Text(tr("common.cancel"))}})
}
