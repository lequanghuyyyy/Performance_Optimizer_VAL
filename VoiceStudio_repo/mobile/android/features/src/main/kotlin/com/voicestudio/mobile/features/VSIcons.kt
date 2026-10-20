package com.voicestudio.mobile.features

import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.SolidColor
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.graphics.vector.path
import androidx.compose.ui.unit.dp
import com.voicestudio.mobile.designsystem.*

object VSIcons {
    val Download: ImageVector by lazy {
        val size=VSTokens.SPACING_INSET
        ImageVector.Builder("Download",size.dp,size.dp,size,size).path(fill=SolidColor(Color(VSTheme.roles("default",true).getValue("text")))) {
            moveTo(size*5/12,size/8);lineTo(size*7/12,size/8);lineTo(size*7/12,size/2)
            lineTo(size*3/4,size/2);lineTo(size/2,size*3/4);lineTo(size/4,size/2);lineTo(size*5/12,size/2);close()
            moveTo(size/4,size*7/8);lineTo(size*3/4,size*7/8);lineTo(size*3/4,size);lineTo(size/4,size);close()
        }.build()
    }
    // Material's core icon artifact omits Pause; keep this simple glyph local.
    val Pause: ImageVector by lazy {
        val size=VSTokens.SPACING_INSET
        ImageVector.Builder("Pause",size.dp,size.dp,size,size).path(fill=SolidColor(Color(VSTheme.roles("default",true).getValue("text")))) {
            moveTo(size/4,size/4);lineTo(size*5/12,size/4);lineTo(size*5/12,size*3/4);lineTo(size/4,size*3/4);close()
            moveTo(size*7/12,size/4);lineTo(size*3/4,size/4);lineTo(size*3/4,size*3/4);lineTo(size*7/12,size*3/4);close()
        }.build()
    }
}
