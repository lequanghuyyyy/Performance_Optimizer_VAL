package com.voicestudio.mobile.features

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.gestures.detectTapGestures
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.interaction.MutableInteractionSource
import androidx.compose.foundation.interaction.collectIsPressedAsState
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material.icons.automirrored.filled.KeyboardArrowRight
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.composed
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.focus.onFocusChanged
import androidx.compose.ui.input.pointer.pointerInput
import androidx.compose.ui.platform.LocalFocusManager
import androidx.compose.ui.platform.LocalSoftwareKeyboardController
import androidx.compose.ui.semantics.*
import androidx.compose.ui.text.input.*
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.foundation.text.KeyboardActions
import androidx.compose.ui.unit.dp
import com.voicestudio.mobile.designsystem.*
import com.voicestudio.mobile.l10n.L10n
import java.util.Locale

val LocalL10n = staticCompositionLocalOf<L10n> { error("L10n provider required") }
@Composable fun tr(key: String, params: Map<String, Any> = emptyMap()) = LocalL10n.current.t(key, params)

/** Clear focus as well as hiding the IME so it does not reappear after an action. */
@Composable fun rememberKeyboardDismiss(): () -> Unit {
    val focus=LocalFocusManager.current
    val keyboard=LocalSoftwareKeyboardController.current
    return remember(focus,keyboard) {{focus.clearFocus();keyboard?.hide()}}
}

/** Unconsumed background taps dismiss; field selection and scrolling keep their gestures. */
fun Modifier.dismissKeyboardOnTap(): Modifier = composed {
    // Resolve at the modifier's owner: a Dialog has its own focus manager.
    val dismiss=rememberKeyboardDismiss()
    pointerInput(dismiss) {detectTapGestures(onTap={dismiss()})}
}

@Composable fun Screen(content: @Composable ColumnScope.() -> Unit) {
    // Insets shrink the viewport, not the scrollable content. Every form shares this path.
    Column(Modifier.fillMaxSize().imePadding().dismissKeyboardOnTap().verticalScroll(rememberScrollState()).padding(VSTheme.inset), verticalArrangement = Arrangement.spacedBy(VSTheme.gap), content = content)
}
@Composable fun ScreenTitle(key: String) { Text(tr(key), style = MaterialTheme.typography.headlineLarge, modifier = Modifier.semantics { heading() }) }
@Composable fun Heading(key: String) { Text(tr(key), style = MaterialTheme.typography.titleMedium, modifier = Modifier.semantics { heading() }) }
@Composable fun Group(content: @Composable ColumnScope.() -> Unit) {
    Surface(Modifier.fillMaxWidth(), color=role("surface"), shape=MaterialTheme.shapes.large, border=BorderStroke(VSTheme.borderWidth,role("border"))) {
        Column(content=content)
    }
}
@Composable fun Action(key: String, enabled: Boolean = true, onClick: () -> Unit) {
    val dismiss=rememberKeyboardDismiss()
    val interaction=remember {MutableInteractionSource()}
    val pressed by interaction.collectIsPressedAsState()
    Button(interactionSource=interaction, colors=ButtonDefaults.buttonColors(containerColor=role(if(pressed) "messageActionHover" else "messageAction"),contentColor=role("messageActionForeground")), onClick = {dismiss();onClick()}, enabled = enabled, modifier = Modifier.fillMaxWidth().heightIn(min = VSTheme.touchTarget), shape = MaterialTheme.shapes.small) { Text(tr(key)) }
}
@Composable fun Secondary(key: String, enabled: Boolean = true, onClick: () -> Unit) {
    val dismiss=rememberKeyboardDismiss()
    OutlinedButton(onClick={dismiss();onClick()}, enabled=enabled, modifier=Modifier.heightIn(min=VSTheme.touchTarget), shape=MaterialTheme.shapes.small,
        border=BorderStroke(VSTheme.borderWidth,role("border")), colors=ButtonDefaults.outlinedButtonColors(containerColor=role("surface"),contentColor=role("text"))) { Text(tr(key)) }
}
@Composable fun IconAction(key: String, icon: ImageVector, enabled: Boolean = true, params: Map<String,Any> = emptyMap(), onClick: () -> Unit) {
    val label=tr(key,params)
    val dismiss=rememberKeyboardDismiss()
    // Surface keeps a token radius; Material's default icon button is circular.
    Surface(onClick={dismiss();onClick()}, enabled=enabled, shape=MaterialTheme.shapes.small, color=role("surface"),
        modifier=Modifier.sizeIn(minWidth=VSTheme.touchTarget,minHeight=VSTheme.touchTarget).semantics {contentDescription=label;role=Role.Button}) {
        Box(contentAlignment=Alignment.Center) {Icon(icon,contentDescription=null,tint=role(if(enabled) "text" else "textMuted"))}
    }
}
@Composable fun Field(key: String, value: String, change: (String) -> Unit, secret: Boolean = false, multiline: Boolean = false) {
    val dismiss=rememberKeyboardDismiss()
    var focused by remember {mutableStateOf(false)}
    Column(Modifier.fillMaxWidth()) {
    OutlinedTextField(value = value, onValueChange = change, label = { Text(tr(key)) }, modifier = Modifier.fillMaxWidth().onFocusChanged {focused=it.isFocused},
        singleLine = !multiline, minLines = if(multiline) 4 else 1,
        visualTransformation = if(secret) PasswordVisualTransformation() else VisualTransformation.None,
        // Multiline keeps Return for newlines; its explicit toolbar supplies Done.
        keyboardOptions = androidx.compose.foundation.text.KeyboardOptions(autoCorrectEnabled = !secret, keyboardType = if(secret) KeyboardType.Password else KeyboardType.Text,
            imeAction=if(multiline) ImeAction.Default else ImeAction.Done),
        keyboardActions=KeyboardActions(onDone={dismiss()}),
        shape = MaterialTheme.shapes.small,
        colors = OutlinedTextFieldDefaults.colors(focusedBorderColor=role("focus"), unfocusedBorderColor=role("border"), focusedContainerColor=role("input"), unfocusedContainerColor=role("input")))
    if(multiline && focused) Row(Modifier.fillMaxWidth(),horizontalArrangement=Arrangement.End) {
        TextButton(onClick=dismiss,shape=MaterialTheme.shapes.small,modifier=Modifier.heightIn(min=VSTheme.touchTarget)) {Text(tr("mobile.keyboard.done"))}
    }
    }
}
@Composable private fun PickerRow(label: String, value: String, enabled: Boolean = true, onClick: () -> Unit) {
    val dismiss=rememberKeyboardDismiss()
    val description=tr("mobile.android.selection",mapOf("label" to tr(label),"value" to value))
    Surface(onClick={dismiss();onClick()},enabled=enabled,color=role("surface"),shape=MaterialTheme.shapes.small,modifier=Modifier.fillMaxWidth().heightIn(min=VSTheme.touchTarget).semantics {contentDescription=description}) {
        Row(Modifier.padding(VSTheme.inset),verticalAlignment=Alignment.CenterVertically,horizontalArrangement=Arrangement.spacedBy(VSTheme.gap)) {
            Text(tr(label),style=MaterialTheme.typography.labelLarge,maxLines=1,overflow=TextOverflow.Ellipsis)
            Text(value,Modifier.weight(1f),color=role("textMuted"),style=MaterialTheme.typography.labelLarge,textAlign=TextAlign.End,maxLines=1,overflow=TextOverflow.Ellipsis)
            Icon(Icons.AutoMirrored.Filled.KeyboardArrowRight,contentDescription=null,tint=role("textMuted"))
        }
    }
}
@Composable fun Picker(label: String, value: String, choices: List<Pair<String, String>>, change: (String) -> Unit) =
    ChoicePicker(label,value,choices.map {Choice(it.first,it.second)},change)

@Composable fun ChoicePicker(label: String, value: String, choices: List<Choice>, change: (String) -> Unit) {
    var open by rememberSaveable { mutableStateOf(false) }
    var query by rememberSaveable { mutableStateOf("") }
    PickerRow(label,choices.firstOrNull {it.id==value}?.title ?: tr("mobile.android.choose"),choices.isNotEmpty()) {open=true}
    if(open) AlertDialog(modifier=Modifier.dismissKeyboardOnTap(),shape=MaterialTheme.shapes.large,onDismissRequest={open=false}, title={Text(tr(label))}, containerColor=role("surfaceOverlay"),
        text={ Column(verticalArrangement=Arrangement.spacedBy(VSTheme.gap)) {
            Field("mobile.android.search", query, {query=it})
            val matches=choices.filter {it.title.contains(query,true)}
            if(matches.isEmpty()) SearchEmpty(query)
            LazyColumn(Modifier.heightIn(max=(VSTokens.SPACING_GAP*20).dp)) {
                items(matches,key={it.id}) { choice ->
                    TextButton(shape=MaterialTheme.shapes.small,onClick={change(choice.id);open=false;query=""},enabled=choice.enabled,
                        modifier=Modifier.fillMaxWidth().heightIn(min=VSTheme.touchTarget).semantics { selected = choice.id==value }) {
                        Column(Modifier.fillMaxWidth()) {
                            Text(choice.title,maxLines=2,overflow=TextOverflow.Ellipsis)
                            choice.supporting?.let {Text(it,style=MaterialTheme.typography.bodySmall,color=role("textMuted"))}
                        }
                    }
                }
            }
        }}, confirmButton={TextButton(shape=MaterialTheme.shapes.small,onClick={open=false}) {Text(tr("common.close"))}})
}
@OptIn(ExperimentalLayoutApi::class)
@Composable fun MultiPicker(label: String, selected: List<String>, choices: List<Pair<String,String>>, change: (List<String>) -> Unit) {
    var open by rememberSaveable {mutableStateOf(false)}
    var query by rememberSaveable {mutableStateOf("")}
    val dismiss=rememberKeyboardDismiss()
    Group {PickerRow(label,selected.mapNotNull {id -> choices.firstOrNull {it.first==id}?.second}.joinToString().ifBlank {tr("mobile.android.choose")},choices.isNotEmpty()) {open=true}}
    FlowRow(horizontalArrangement=Arrangement.spacedBy(VSTheme.gap)) {
        choices.filter {it.first in selected}.forEach {(id,name) ->
            val remove=tr("mobile.android.remove_selection",mapOf("name" to name))
            InputChip(shape=MaterialTheme.shapes.small,selected=true,onClick={dismiss();change(selected-id)}, label={Text(name)},
                trailingIcon={Icon(Icons.Default.Close,contentDescription=null)},modifier=Modifier.heightIn(min=VSTheme.touchTarget).semantics {contentDescription=remove})
        }
    }
    if(open) AlertDialog(modifier=Modifier.dismissKeyboardOnTap(),shape=MaterialTheme.shapes.large,onDismissRequest={open=false},containerColor=role("surfaceOverlay"),title={Text(tr(label))},text={
        Column(verticalArrangement=Arrangement.spacedBy(VSTheme.gap)) {
            val dismissSearch=rememberKeyboardDismiss()
            Field("mobile.android.search",query,{query=it})
            val matches=choices.filter {it.second.contains(query,true)}
            if(matches.isEmpty()) SearchEmpty(query)
            LazyColumn(Modifier.heightIn(max=(VSTokens.SPACING_GAP*20).dp)) {
                items(matches,key={it.first}) {(id,name) ->
                    Surface(onClick={dismissSearch();change(if(id in selected) selected-id else selected+id)},color=role("surfaceOverlay"),shape=MaterialTheme.shapes.small,
                        modifier=Modifier.fillMaxWidth().heightIn(min=VSTheme.touchTarget).semantics {this.selected=id in selected}) {
                        Row(Modifier.padding(VSTheme.inset),verticalAlignment=Alignment.CenterVertically) {
                            Text(name,Modifier.weight(1f),maxLines=2,overflow=TextOverflow.Ellipsis)
                            Checkbox(checked=id in selected,onCheckedChange=null)
                        }
                    }
                }
            }
        }
    },confirmButton={TextButton(shape=MaterialTheme.shapes.small,onClick={open=false}) {Text(tr("common.close"))}})
}
@Composable fun Toggle(key: String, value: Boolean, change: (Boolean) -> Unit) {
    val label = tr(key)
    val dismiss=rememberKeyboardDismiss()
    Row(Modifier.fillMaxWidth().heightIn(min=VSTheme.touchTarget), horizontalArrangement=Arrangement.spacedBy(VSTheme.gap)) {
        Text(label, Modifier.weight(1f))
        Switch(checked=value, onCheckedChange={dismiss();change(it)}, modifier=Modifier.semantics { contentDescription=label })
    }
}
fun languageName(code: String, locale: String): String = Locale.forLanguageTag(code).getDisplayName(Locale.forLanguageTag(locale))

/** Shared empty state for every searchable list. */
@Composable fun SearchEmpty(query: String) {
    Text(tr("projects.no_matches",mapOf("query" to query)),color=role("textMuted"),modifier=Modifier.semantics {liveRegion=LiveRegionMode.Polite})
}
@Composable fun InlineLoading() {
    Text(tr("common.loading"),color=role("textMuted"))
    LinearProgressIndicator(Modifier.fillMaxWidth())
}
