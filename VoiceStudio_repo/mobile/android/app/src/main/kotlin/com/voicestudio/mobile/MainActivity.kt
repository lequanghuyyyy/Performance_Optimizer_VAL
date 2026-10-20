package com.voicestudio.mobile

import android.app.Application
import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.activity.SystemBarStyle
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.toArgb
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.List
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalConfiguration
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.unit.LayoutDirection
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewmodel.compose.viewModel
import androidx.navigation.compose.*
import com.voicestudio.mobile.designsystem.*
import com.voicestudio.mobile.l10n.L10n
import com.voicestudio.mobile.features.*
import com.voicestudio.mobile.features.connect.ConnectScreen
import com.voicestudio.mobile.features.speak.SpeakScreen
import com.voicestudio.mobile.features.voices.VoicesScreen
import com.voicestudio.mobile.features.dub.DubScreen
import com.voicestudio.mobile.features.dictate.DictateScreen
import com.voicestudio.mobile.features.settings.SettingsScreen
import java.util.Locale

class VoiceStudioApplication:Application()
class MainActivity:ComponentActivity() {
    override fun onCreate(savedInstanceState:Bundle?) {
        super.onCreate(savedInstanceState);enableEdgeToEdge()
        setContent {StudioApp()}
    }
}

@Composable fun StudioApp(vm:StudioViewModel = viewModel()) {
    val prefs by vm.preferences.collectAsStateWithLifecycle()
    val state by vm.state.collectAsStateWithLifecycle()
    val context=LocalContext.current
    val configuration=LocalConfiguration.current
    val locale=if(prefs.locale=="system") configuration.locales[0].toLanguageTag() else prefs.locale
    val l=remember(locale) {L10n.load(context,locale)}
    val dark=when(prefs.mode) {"light" -> false;"system" -> (configuration.uiMode and android.content.res.Configuration.UI_MODE_NIGHT_MASK)==android.content.res.Configuration.UI_MODE_NIGHT_YES;else -> true}
    val chrome=Color(VSTheme.roles(if(dark) prefs.darkPalette else prefs.lightPalette,dark).getValue("chrome")).toArgb()
    SideEffect {
        (context as? ComponentActivity)?.let {activity ->
            val style=if(dark) SystemBarStyle.dark(chrome) else SystemBarStyle.light(chrome,chrome)
            activity.enableEdgeToEdge(statusBarStyle=style,navigationBarStyle=style)
            activity.window.isNavigationBarContrastEnforced=false
        }
    }
    val direction=if(Locale.forLanguageTag(locale).language=="ar") LayoutDirection.Rtl else LayoutDirection.Ltr
    CompositionLocalProvider(LocalL10n provides l,LocalLayoutDirection provides direction) {
        VoiceStudioTheme(ThemePreferences(prefs.mode,prefs.lightPalette,prefs.darkPalette)) {
            key(state.connectionId) {
            val nav=rememberNavController()
            val back by nav.currentBackStackEntryAsState()
            val route=back?.destination?.route ?: "connect"
            val dismissKeyboard=rememberKeyboardDismiss()
            LaunchedEffect(state.connected,state.loading,back!=null) {
                // Scaffold subcomposes its content after root effects are installed.
                // A back-stack entry proves NavHost has actually installed the graph.
                if(!state.loading&&back!=null) nav.navigate(if(state.connected) "speak" else "connect") {popUpTo(nav.graph.id) {inclusive=true};launchSingleTop=true}
            }
            Scaffold(containerColor=role("canvas"),bottomBar={
                NavigationBar(containerColor=role("chrome"),contentColor=role("toolbarForeground")) {
                    val tabs=if(state.connected) listOf("speak","voices","dub","dictate","settings") else listOf("connect","settings")
                    tabs.forEach {tab ->
                        val label=if(tab=="connect") l.t("mobile.tabs.connect") else l.t("mobile.tabs.$tab")
                        NavigationBarItem(selected=route==tab,onClick={dismissKeyboard();nav.navigate(tab) {popUpTo(nav.graph.startDestinationId) {saveState=true};launchSingleTop=true;restoreState=true}},
                            icon={Icon(when(tab) {"speak" -> Icons.Default.PlayArrow;"voices" -> Icons.Default.Person;"dub" -> Icons.AutoMirrored.Filled.List;"dictate" -> Icons.Default.Edit;"connect" -> Icons.Default.Add;else -> Icons.Default.Settings},contentDescription=label)},
                            label=if(LocalDensity.current.fontScale<=1.5f) {{Text(label)}} else null,
                            colors=NavigationBarItemDefaults.colors(indicatorColor=role("messageSurface"),selectedIconColor=role("messageAction"),selectedTextColor=role("toolbarForeground"),unselectedIconColor=role("textMuted"),unselectedTextColor=role("textMuted")))
                    }
                }
            }) {padding ->
                Surface(Modifier.fillMaxSize().padding(padding).consumeWindowInsets(padding).dismissKeyboardOnTap(),color=role("canvas")) {
                    Column {
                    state.error?.let {error ->
                        Surface(color=role("errorSurface"),shape=MaterialTheme.shapes.large,modifier=Modifier.fillMaxWidth().padding(VSTheme.inset)) {
                            Column(Modifier.padding(VSTheme.inset)) {
                                Text(l.t(error.key,error.params),color=role("errorForeground"),modifier=Modifier.heightIn(max=VSTheme.touchTarget*4).verticalScroll(rememberScrollState()))
                                TextButton(shape=MaterialTheme.shapes.small,onClick=vm::clearError) {Text(tr("common.close"))}
                                if(error.key=="mobile.permissions.denied") TextButton(shape=MaterialTheme.shapes.small,onClick={vm.clearError();openSystemSettings(context)}) {Text(tr("mobile.permissions.open_settings"))}
                            }
                        }
                    }
                    if(state.busy) LinearProgressIndicator(Modifier.fillMaxWidth())
                    Box(Modifier.weight(1f)) {
                    if(state.loading) Screen {Text(tr("common.loading"));LinearProgressIndicator()}
                    else NavHost(navController=nav,startDestination="connect",
                        enterTransition={androidx.compose.animation.EnterTransition.None}, exitTransition={androidx.compose.animation.ExitTransition.None},
                        popEnterTransition={androidx.compose.animation.EnterTransition.None}, popExitTransition={androidx.compose.animation.ExitTransition.None}) {
                        composable("connect") {ConnectScreen(vm)}
                        composable("settings") {SettingsScreen(vm,BuildConfig.VERSION_NAME)}
                        composable("speak") {if(state.connected) SpeakScreen(vm)}
                        composable("voices") {if(state.connected) VoicesScreen(vm)}
                        composable("dub") {if(state.connected) DubScreen(vm)}
                        composable("dictate") {if(state.connected) DictateScreen(vm)}
                    }
                    }
                    }
                }
            }
            }
            if(!state.loading&&prefs.consent==null) AlertDialog(shape=MaterialTheme.shapes.large,onDismissRequest={vm.consent(false)},title={Text(tr("consent.title"))},
                text={Column(Modifier.verticalScroll(rememberScrollState()),verticalArrangement=Arrangement.spacedBy(VSTheme.gap)) {Text(tr("consent.body"));Text(tr("mobile.android.analytics_local"))}},
                confirmButton={TextButton(shape=MaterialTheme.shapes.small,onClick={vm.consent(true)}) {Text(tr("consent.yes"))}},
                dismissButton={TextButton(shape=MaterialTheme.shapes.small,onClick={vm.consent(false)}) {Text(tr("consent.no"))}})
        }
    }
}
