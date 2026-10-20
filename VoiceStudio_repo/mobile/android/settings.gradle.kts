pluginManagement { repositories { google(); mavenCentral(); gradlePluginPortal() } }
dependencyResolutionManagement { repositoriesMode.set(RepositoriesMode.FAIL_ON_PROJECT_REPOS); repositories { google(); mavenCentral() } }
rootProject.name = "VoiceStudioMobile"
include(":app", ":core:designsystem", ":core:network", ":core:data", ":core:l10n", ":features")
