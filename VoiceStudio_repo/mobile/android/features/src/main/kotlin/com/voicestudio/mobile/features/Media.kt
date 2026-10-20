package com.voicestudio.mobile.features

import android.content.Context
import android.content.Intent
import android.media.MediaRecorder
import android.net.Uri
import androidx.core.content.FileProvider
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.ui.Alignment
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.semantics.*
import androidx.media3.common.MediaItem
import androidx.media3.common.Player
import androidx.media3.datasource.DefaultDataSource
import androidx.media3.datasource.okhttp.OkHttpDataSource
import androidx.media3.exoplayer.ExoPlayer
import androidx.media3.exoplayer.source.DefaultMediaSourceFactory
import com.voicestudio.mobile.designsystem.VSTheme
import com.voicestudio.mobile.designsystem.role
import com.voicestudio.mobile.network.*
import kotlinx.coroutines.*
import java.io.File

/** Provider metadata is separate from the private, collision-free storage path. */
fun mediaDisplayName(context: Context, uri: Uri): String? = context.contentResolver.query(
    uri,arrayOf(android.provider.OpenableColumns.DISPLAY_NAME),null,null,null)?.use { cursor ->
        val column=cursor.getColumnIndex(android.provider.OpenableColumns.DISPLAY_NAME)
        if(column>=0 && cursor.moveToFirst()) cursor.getString(column)?.takeIf {it.isNotBlank()} else null
    }

suspend fun copyMedia(context: Context, uri: Uri, extension: String): File = withContext(Dispatchers.IO) {
    val original=mediaDisplayName(context,uri)
    val suffix=original?.substringAfterLast('.', "")?.lowercase()?.takeIf {Regex("[a-z0-9]{1,10}").matches(it)}
        ?: android.webkit.MimeTypeMap.getSingleton().getExtensionFromMimeType(context.contentResolver.getType(uri)) ?: extension
    val file=File(context.filesDir, "uploads/${java.util.UUID.randomUUID()}.$suffix").apply { parentFile!!.mkdirs() }
    try { context.contentResolver.openInputStream(uri)!!.use { input -> file.outputStream().use { input.copyTo(it) } }; file }
    catch(e: Exception) { file.delete(); throw e }
}
fun formatDuration(milliseconds: Long): String {
    val seconds = milliseconds.coerceAtLeast(0) / 1000
    return "%d:%02d".format(java.util.Locale.ROOT, seconds / 60, seconds % 60)
}
fun shareText(context: Context, text: String) { context.startActivity(Intent.createChooser(Intent(Intent.ACTION_SEND).setType("text/plain").putExtra(Intent.EXTRA_TEXT,text),null)) }
fun shareFile(context: Context, file: File, type: String) {
    val uri=FileProvider.getUriForFile(context, "${context.packageName}.files",file)
    context.startActivity(Intent.createChooser(Intent(Intent.ACTION_SEND).setType(type).putExtra(Intent.EXTRA_STREAM,uri).addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION),null))
}
suspend fun sharedDownload(context: Context, api: VoiceStudioApi, path: String, extension: String): File {
    val dir=File(context.cacheDir,"shared").apply {mkdirs()}
    val file=File(dir,"${java.util.UUID.randomUUID()}.$extension")
    try {
        val type=api.download(path,file)
        val suffix=when(type) {"audio/wav", "audio/x-wav" -> "wav";"audio/mp4", "audio/x-m4a" -> "m4a";"video/mp4" -> "mp4";"video/x-matroska", "video/matroska" -> "mkv";"audio/mpeg" -> "mp3";else -> extension}
        if(suffix==extension) return file
        val renamed=File(dir,"${java.util.UUID.randomUUID()}.$suffix")
        check(file.renameTo(renamed));return renamed
    } catch(e:Exception) {file.delete();throw e}
}
class SampleRecorder(private val context: Context) {
    private var recorder: MediaRecorder? = null
    var file: File? = null
        private set
    @Suppress("DEPRECATION")
    fun start(): File {
        val output=File(context.filesDir,"uploads/${java.util.UUID.randomUUID()}.m4a").apply {parentFile!!.mkdirs()}
        val recording=if(android.os.Build.VERSION.SDK_INT>=31) MediaRecorder(context) else MediaRecorder()
        recorder=recording;file=output
        try {
            recording.setAudioSource(MediaRecorder.AudioSource.MIC);recording.setOutputFormat(MediaRecorder.OutputFormat.MPEG_4)
            recording.setAudioEncoder(MediaRecorder.AudioEncoder.AAC);recording.setAudioSamplingRate(44100);recording.setAudioEncodingBitRate(128000)
            recording.setOutputFile(output.absolutePath);recording.prepare();recording.start()
        } catch(e:Exception) { discard();throw e }
        return output
    }
    fun stop(): File {
        val output=file ?: error("No recording")
        try {recorder?.stop()} catch(e:Exception) {output.delete();throw e} finally {recorder?.release();recorder=null}
        return output
    }
    fun discard() {runCatching {recorder?.stop()};recorder?.release();recorder=null;file?.delete();file=null}
}

@androidx.annotation.OptIn(androidx.media3.common.util.UnstableApi::class)
val LocalHostPlayerFactory=staticCompositionLocalOf<(Context,VoiceStudioApi)->Player> {
    { context, api -> ExoPlayer.Builder(context).setMediaSourceFactory(DefaultMediaSourceFactory(DefaultDataSource.Factory(context,OkHttpDataSource.Factory(api.client)))).build() }
}
@androidx.annotation.OptIn(androidx.media3.common.util.UnstableApi::class)
@Composable fun HostPlayer(api: VoiceStudioApi, path: String, failure: (Throwable)->Unit) {
    val context=LocalContext.current
    val factory=LocalHostPlayerFactory.current
    val player=remember(api) {factory(context,api)}
    var playing by remember { mutableStateOf(false) }
    var duration by remember { mutableLongStateOf(0L) }
    var position by remember { mutableLongStateOf(0L) }
    var error by remember { mutableStateOf(false) }
    DisposableEffect(player) {
        val listener=object: Player.Listener {
            override fun onIsPlayingChanged(isPlaying:Boolean) {playing=isPlaying}
            override fun onPlayerError(e:androidx.media3.common.PlaybackException) {error=true;failure(generateSequence(e as Throwable?) {it.cause}.filterIsInstance<ClientFailure>().firstOrNull() ?: ClientFailure("mobile.android.playback_failed"))}
        }
        player.addListener(listener)
        onDispose {player.removeListener(listener);player.release()}
    }
    LaunchedEffect(path,api) {
        try {error=false;player.setMediaItem(MediaItem.fromUri(api.url(path).toString()));player.prepare()}
        catch(e:Exception) {failure(e)}
    }
    LaunchedEffect(player) {while(isActive) {duration=player.duration.coerceAtLeast(0);position=player.currentPosition.coerceAtLeast(0);delay(250)}}
    Card(Modifier.fillMaxWidth(),colors=CardDefaults.cardColors(containerColor=role("surface")),shape=MaterialTheme.shapes.large) {
        Column(Modifier.padding(VSTheme.inset),verticalArrangement=Arrangement.spacedBy(VSTheme.gap)) {
            Text(tr("player.generated_audio"),style=MaterialTheme.typography.titleMedium)
            Row(verticalAlignment=Alignment.CenterVertically) {
                IconAction(if(playing) "player.pause" else "player.play",if(playing) VSIcons.Pause else Icons.Default.PlayArrow) {
                    if(playing) player.pause() else {if(error) player.prepare();if(player.playbackState==Player.STATE_ENDED) player.seekTo(0);player.play()}
                }
                val label=tr("player.seek")
                Slider(value=if(duration>0) (position.toFloat()/duration).coerceIn(0f,1f) else 0f,
                    onValueChange={player.seekTo((it*duration).toLong())},enabled=duration>0,
                    modifier=Modifier.weight(1f).semantics {contentDescription=label})
            }
            Text(tr("mobile.android.playback_time",mapOf("position" to formatDuration(position),"duration" to formatDuration(duration))),style=MaterialTheme.typography.bodySmall,color=role("textMuted"))
        }
    }
}
