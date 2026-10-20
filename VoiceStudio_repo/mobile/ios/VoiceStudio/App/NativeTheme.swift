import UIKit
import VoiceStudioKit

@MainActor enum NativeTheme {
    static func apply(_ theme: Theme) {
        func color(_ role: String) -> UIColor {
            let c = theme[role]
            return UIColor(red: c.r, green: c.g, blue: c.b, alpha: c.a)
        }
        let navigation = UINavigationBarAppearance()
        navigation.configureWithOpaqueBackground()
        navigation.backgroundColor = color("chrome")
        navigation.shadowColor = color("border")
        let font = UIFontMetrics(forTextStyle: .body).scaledFont(for: UIFont(name: "InterVariable", size: Theme.controlTypeSize) ?? UIFont.preferredFont(forTextStyle: .body))
        navigation.titleTextAttributes = [.foregroundColor: color("toolbarForeground"), .font: font]
        navigation.largeTitleTextAttributes = [.foregroundColor: color("toolbarForeground"), .font: UIFontMetrics(forTextStyle: .largeTitle).scaledFont(for: UIFont(name: "InterVariable", size: Theme.titleTypeSize) ?? UIFont.preferredFont(forTextStyle: .largeTitle))]
        UINavigationBar.appearance().standardAppearance = navigation
        UINavigationBar.appearance().scrollEdgeAppearance = navigation
        UINavigationBar.appearance().compactAppearance = navigation
        UINavigationBar.appearance().tintColor = color("messageAction")
        let tab = UITabBarAppearance()
        tab.configureWithOpaqueBackground()
        tab.backgroundColor = color("chrome"); tab.shadowColor = color("border")
        let tabFont = UIFontMetrics(forTextStyle: .caption1).scaledFont(for: UIFont(name: "InterVariable", size: VSTokens.textCaption.size) ?? UIFont.preferredFont(forTextStyle: .caption1))
        for item in [tab.stackedLayoutAppearance, tab.inlineLayoutAppearance, tab.compactInlineLayoutAppearance] {
            item.normal.iconColor = color("toolbarForeground")
            item.normal.titleTextAttributes = [.foregroundColor: color("toolbarForeground"), .font: tabFont]
            item.selected.iconColor = color("messageAction")
            item.selected.titleTextAttributes = [.foregroundColor: color("messageAction"), .font: tabFont]
        }
        UITabBar.appearance().standardAppearance = tab
        UITabBar.appearance().scrollEdgeAppearance = tab
    }
}
