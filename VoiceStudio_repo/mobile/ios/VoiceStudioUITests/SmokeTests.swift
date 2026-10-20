import XCTest

final class SmokeTests: XCTestCase {
    @MainActor func testConsentCopyEqualChoicesAndSwipeDefaultsToNo() throws {
        let app = XCUIApplication()
        app.launchArguments = ["--ui-smoke", "-AppleLanguages", "(en)"]
        app.launch()
        let no = app.buttons["consent.no"]
        XCTAssertTrue(no.waitForExistence(timeout: 10))
        // SwiftUI sheets aren't XCUIElement.sheets; identify the two choices directly.
        XCTAssertTrue(app.buttons["consent.yes"].exists)
        XCTAssertEqual(app.staticTexts["consent.title"].label, "Help improve VoiceStudio?")
        let yes = app.buttons["consent.yes"]
        XCTAssertEqual(yes.label, "Yes, share anonymous stats")
        XCTAssertEqual(no.label, "No thanks")
        XCTAssertEqual(yes.frame.width, no.frame.width, accuracy: 1)
        XCTAssertFalse(app.buttons["consent.dismiss"].exists)
        XCTAssertTrue(app.staticTexts["This companion app records your choice on this phone. It sends no analytics."].exists)
        // First opt in, then prove passive dismissal replaces a prior Yes with No.
        yes.tap()
        XCTAssertTrue(app.tabBars.buttons["Settings"].waitForExistence(timeout: 5))
        app.tabBars.buttons["Settings"].tap()
        XCTAssertEqual(app.switches["settings.analytics"].value as? String, "1")
        app.terminate(); app.launch()
        XCTAssertTrue(app.buttons["consent.no"].waitForExistence(timeout: 10))
        // Swipe the sheet down, as a person would.
        app.staticTexts["consent.title"].swipeDown(velocity: .fast)
        XCTAssertTrue(app.textFields["connect.url"].waitForExistence(timeout: 5))
        XCTAssertFalse(app.buttons["consent.no"].exists)
        app.tabBars.buttons["Settings"].tap()
        XCTAssertEqual(app.switches["settings.analytics"].value as? String, "0")
    }

    @MainActor func testDictationHasOneControlAndPalettesHaveNoExtraActions() throws {
        let app = XCUIApplication()
        app.launchArguments = ["--ui-smoke", "-AppleLanguages", "(en)"]
        app.launch()
        connect(app)
        XCTAssertTrue(app.textViews["speak.text"].waitForExistence(timeout: 5))
        app.tabBars.buttons["Dictate"].tap()
        let record = app.buttons["dictate.record"]
        XCTAssertTrue(record.waitForExistence(timeout: 5))
        XCTAssertEqual(record.label, "Tap to start")
        XCTAssertFalse(app.staticTexts["Hold to talk"].exists)
        XCTAssertEqual(app.buttons.matching(identifier: "dictate.record").count, 1)
        app.tabBars.buttons["Settings"].tap()
        let light = app.buttons["settings.palette.light"]
        XCTAssertTrue(light.waitForExistence(timeout: 5))
        XCTAssertTrue(app.buttons["settings.palette.dark"].exists)
        XCTAssertEqual(app.buttons.matching(NSPredicate(format: "label == %@", "Light palette")).count, 1)
        XCTAssertEqual(app.buttons.matching(NSPredicate(format: "label == %@", "Dark palette")).count, 1)
        light.tap()
        app.buttons["choice.option.default"].tap()
        XCTAssertEqual(light.value as? String, "Studio")
        light.tap()
        app.buttons["choice.option.signal"].tap()
        XCTAssertEqual(light.value as? String, "VoiceStudio Original")
    }

    @MainActor func testEveryTabRendersWhileHostInventoryIsLoading() throws {
        let app = XCUIApplication()
        app.launchArguments = ["--ui-smoke", "--ui-delayed-inventory", "-AppleLanguages", "(en)"]
        app.launch()
        connect(app)
        XCTAssertTrue(app.textViews["speak.text"].waitForExistence(timeout: 5))
        XCTAssertTrue(app.descendants(matching: .any)["page.loading"].firstMatch.exists)
        app.tabBars.buttons["Dub"].tap()
        XCTAssertTrue(app.textFields["dub.source_url"].waitForExistence(timeout: 0.5))
        app.tabBars.buttons["Voices"].tap()
        XCTAssertTrue(app.buttons["voices.create"].waitForExistence(timeout: 0.5))
        app.tabBars.buttons["Dictate"].tap()
        XCTAssertTrue(app.buttons["dictate.record"].waitForExistence(timeout: 0.5))
        app.tabBars.buttons["Settings"].tap()
        XCTAssertTrue(app.buttons["settings.language"].waitForExistence(timeout: 0.5))
    }

    @MainActor func testSearchEmptyStatesAndCompactDubHistory() throws {
        let app = XCUIApplication()
        app.launchArguments = ["--ui-smoke", "-AppleLanguages", "(en)"]
        app.launch()
        connect(app)
        XCTAssertTrue(app.textViews["speak.text"].waitForExistence(timeout: 5))
        app.tabBars.buttons["Voices"].tap()
        let search = app.textFields["search.field"]
        XCTAssertTrue(search.waitForExistence(timeout: 5))
        search.tap(); search.typeText("unmatched-voice")
        XCTAssertTrue(app.staticTexts["search.empty"].exists)
        XCTAssertEqual(app.staticTexts["search.empty"].label, "No matches for \"unmatched-voice\".")
        dismissUsingDone(app)
        app.tabBars.buttons["Speak"].tap()
        app.buttons["speak.engine"].tap()
        XCTAssertTrue(search.waitForExistence(timeout: 5))
        search.tap(); search.typeText("unmatched-engine")
        XCTAssertTrue(app.staticTexts["search.empty"].exists)
        dismissUsingDone(app)
        app.buttons["Close"].tap()
        app.tabBars.buttons["Dub"].tap()
        XCTAssertEqual(app.buttons["dub.languages"].value as? String, "Choose")
        app.buttons["dub.languages"].tap()
        XCTAssertTrue(search.waitForExistence(timeout: 5))
        search.tap(); search.typeText("unmatched-language")
        XCTAssertTrue(app.staticTexts["search.empty"].exists)
        dismissUsingDone(app)
        app.buttons["Confirm"].tap()
        let download = app.buttons["dub.download.smoke-dub"]
        for _ in 0..<4 { if download.isHittable { break }; app.swipeUp() }
        XCTAssertTrue(download.exists)
        XCTAssertTrue(app.buttons["dub.share.smoke-dub"].exists)
        XCTAssertEqual(download.label, "Download: vs-dub-test.wav")
        XCTAssertLessThanOrEqual(download.frame.width, download.frame.height * 1.5)
        XCTAssertTrue(app.staticTexts["vs-dub-test.wav"].exists)
        XCTAssertEqual(app.staticTexts["dub.metadata.smoke-dub"].label, "Spanish dub, 2:05 long")
    }

    @MainActor private func connect(_ app: XCUIApplication) {
        XCTAssertTrue(app.buttons["consent.no"].waitForExistence(timeout: 10))
        app.buttons["consent.no"].tap()
        let address = app.textFields["connect.url"]
        XCTAssertTrue(address.waitForExistence(timeout: 5))
        address.tap(); address.typeText("http://127.0.0.1:3900")
        dismissUsingDone(app)
        app.swipeUp()
        app.buttons["connect.submit"].tap()
    }

    @MainActor func testConsentConnectGeneratePlay() throws {
        let app = XCUIApplication()
        app.launchArguments = ["--ui-smoke", "-AppleLanguages", "(en)"]
        app.launch()
        XCTAssertTrue(app.buttons["consent.no"].waitForExistence(timeout: 10))
        app.buttons["consent.no"].tap()
        XCTAssertTrue(app.tabBars.buttons["Connect"].exists)
        let address = app.textFields["connect.url"]
        XCTAssertTrue(address.waitForExistence(timeout: 5))
        address.tap(); address.typeText("http://127.0.0.1:3900")
        dismissUsingDone(app)
        app.swipeUp()
        app.buttons["connect.submit"].tap()
        let editor = app.textViews["speak.text"]
        XCTAssertTrue(editor.waitForExistence(timeout: 10))
        XCTAssertEqual(editor.label, "Type what you want to hear…")
        XCTAssertTrue((editor.value as? String ?? "").isEmpty)
        XCTAssertEqual(app.alerts.count, 0)
        let engine = app.buttons["speak.engine"]
        XCTAssertTrue(engine.waitForExistence(timeout: 5))
        engine.tap()
        let preset = app.buttons["choice.option.kittentts"]
        XCTAssertTrue(preset.waitForExistence(timeout: 5))
        XCTAssertTrue(preset.label.contains("KittenTTS"))
        XCTAssertFalse(app.buttons["choice.option.unavailable"].isEnabled)
        preset.tap()
        let rowHeight = engine.frame.height
        engine.tap()
        let longName = "OuteTTS (MLX Audio, Apple Silicon)"
        let longOption = app.buttons["choice.option.mlx-audio"]
        XCTAssertTrue(longOption.waitForExistence(timeout: 5))
        XCTAssertEqual(longOption.label, longName)
        longOption.tap()
        XCTAssertEqual(engine.value as? String, longName)
        XCTAssertEqual(engine.frame.height, rowHeight, accuracy: 1)
        app.buttons["speak.language"].tap()
        let search = app.textFields["search.field"]
        XCTAssertTrue(search.waitForExistence(timeout: 5))
        search.tap(); search.typeText("Spanish")
        XCTAssertTrue(app.buttons["choice.option.spanish"].exists)
        app.buttons["Close"].tap()
        editor.tap(); editor.typeText("VoiceStudio smoke test")
        XCTAssertEqual(editor.value as? String, "VoiceStudio smoke test")
        dismissUsingDone(app)
        app.swipeUp()
        app.buttons["speak.generate"].tap()
        let play = app.buttons["audio.play"]
        XCTAssertTrue(play.waitForExistence(timeout: 10))
        play.tap()
        XCTAssertEqual(play.label, "Pause")
        XCTAssertTrue(app.staticTexts["0:01"].exists)
    }

    @MainActor func testKeyboardDismissalAcrossFormsAndSearch() throws {
        let app = XCUIApplication()
        app.launchArguments = ["--ui-smoke", "-AppleLanguages", "(en)"]
        app.launch()
        XCTAssertTrue(app.buttons["consent.no"].waitForExistence(timeout: 10))
        app.buttons["consent.no"].tap()
        let address = app.textFields["connect.url"]
        XCTAssertTrue(address.waitForExistence(timeout: 5))
        address.tap(); address.typeText("http://127.0.0.1:3900")
        // A field tap must retain focus; a tap on surrounding chrome dismisses it.
        XCTAssertTrue(app.keyboards.firstMatch.waitForExistence(timeout: 5))
        app.navigationBars.firstMatch.tap()
        assertKeyboardDismissed(app)
        address.tap(); address.typeText("\n")
        assertKeyboardDismissed(app)
        app.swipeUp()
        app.buttons["connect.submit"].tap()
        let editor = app.textViews["speak.text"]
        XCTAssertTrue(editor.waitForExistence(timeout: 10))
        editor.tap(); editor.typeText("An editable multiline message\nwith a second line")
        dismissUsingDone(app)
        app.swipeUp()
        XCTAssertTrue(app.buttons["speak.generate"].isHittable)

        app.tabBars.buttons["Voices"].tap()
        app.buttons["voices.actions.smoke-profile"].tap()
        app.buttons["Rename"].tap()
        let rename = app.textFields["voices.rename.name"]
        XCTAssertTrue(rename.waitForExistence(timeout: 5))
        rename.tap(); rename.typeText(" updated\n")
        assertKeyboardDismissed(app)
        app.buttons["Cancel"].tap()
        app.buttons["Clone a voice"].tap()
        let name = app.textFields["voices.clone.name"]
        XCTAssertTrue(name.waitForExistence(timeout: 5))
        name.tap(); name.typeText("Sample voice")
        dismissUsingDone(app)
        app.buttons["Cancel"].tap()

        app.tabBars.buttons["Dub"].tap()
        let source = app.textFields["dub.source_url"]
        XCTAssertTrue(source.waitForExistence(timeout: 5))
        source.tap(); source.typeText("https://media.example/sample\n")
        assertKeyboardDismissed(app)

        app.tabBars.buttons["Settings"].tap()
        let language = app.buttons["settings.language"]
        for _ in 0..<5 {
            if language.isHittable { break }
            app.swipeUp()
        }
        language.tap()
        let search = app.textFields["search.field"]
        XCTAssertTrue(search.waitForExistence(timeout: 5))
        search.tap(); search.typeText("English")
        dismissUsingDone(app)
        XCTAssertTrue(app.buttons["choice.option.en"].isHittable)
    }

    @MainActor private func dismissUsingDone(_ app: XCUIApplication) {
        XCTAssertTrue(app.keyboards.firstMatch.waitForExistence(timeout: 5))
        let done = app.buttons["keyboard.done"]
        XCTAssertTrue(done.waitForExistence(timeout: 5))
        done.tap()
        assertKeyboardDismissed(app)
    }

    @MainActor private func assertKeyboardDismissed(_ app: XCUIApplication) {
        let gone = XCTNSPredicateExpectation(predicate: NSPredicate(format: "exists == false"), object: app.keyboards.firstMatch)
        XCTAssertEqual(XCTWaiter.wait(for: [gone], timeout: 5), .completed)
    }
}
