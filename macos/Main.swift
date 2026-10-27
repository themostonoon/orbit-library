import AppKit
import SwiftUI

@main enum OrbitMain {
    @MainActor static func main() {
        if CommandLine.arguments.contains("--self-test") {
            do { try validate(); print("PASS: Mac presets, JSON compatibility, persistence, backups, scanning, CUE tracks, quoted paths, native launches, filters, navigation, and 10,000-game data model."); exit(0) }
            catch { fputs("SELF-TEST FAILED: \(error)\n", stderr); exit(1) }
        }
        let app = NSApplication.shared
        let delegate = OrbitDelegate()
        app.delegate = delegate; app.setActivationPolicy(.regular)
        withExtendedLifetime(delegate) { app.run() }
    }
    @MainActor static func validate() throws {
        func check(_ condition: @autoclosure () throws -> Bool, _ text: String) throws { if try condition() == false { throw OrbitError.message(text) } }
        let manager = FileManager.default, root = manager.temporaryDirectory.appendingPathComponent("OrbitTest-" + UUID().uuidString)
        try manager.createDirectory(at: root, withIntermediateDirectories: true)
        defer { try? manager.removeItem(at: root) }
        let storage = LibraryStorage(directory: root)
        var library = try storage.load().0
        try check(Catalog.all.count == 29 && Set(Catalog.all.map { $0.id }).count == 29, "29 unique system presets")
        try check(library.platforms.count == 29, "Platform defaults")
        let roms = root.appendingPathComponent("ROMs with spaces"); try manager.createDirectory(at: roms, withIntermediateDirectories: true)
        let rom = roms.appendingPathComponent("Example 'quotes' $(literal).nes"); try Data().write(to: rom)
        try Data().write(to: roms.appendingPathComponent("ignored.txt"))
        var added = try ScanEngine.scan(existing: [], folders: ["nes":roms.path]); try check(added.count == 1, "ROM extension filter")
        try check(try ScanEngine.scan(existing: added, folders: ["nes":roms.path]).isEmpty, "Duplicate scan")
        added[0].favorite = true; library.games = added; try storage.save(library); try storage.save(library)
        try check(try storage.load().0.games.first?.favorite == true, "Favorite persistence")
        let json = try JSONSerialization.jsonObject(with: Data(contentsOf: storage.file)) as! [String:Any]
        try check(json["Games"] != nil && json["Platforms"] != nil && json["Version"] != nil, "Windows-compatible JSON keys")
        let gameJSON = (json["Games"] as! [[String:Any]])[0]; try check(gameJSON["Path"] != nil && gameJSON["Favorite"] != nil, "Windows-compatible game keys")
        try check(manager.fileExists(atPath: storage.file.appendingPathExtension("bak").path), "Save backup")
        try "FILE \"Disc.bin\" BINARY".write(to: roms.appendingPathComponent("Disc.cue"), atomically: true, encoding: .utf8)
        try Data().write(to: roms.appendingPathComponent("Disc.bin"))
        let discs = try ScanEngine.scan(existing: [], folders: ["psx":roms.path]); try check(discs.count == 1 && discs[0].path.hasSuffix(".cue"), "CUE track suppression")
        let settings = PlatformSettings(exe: "/usr/bin/true", args: "\"{rom}\" --name \"{name}\" --dir \"{romdir}\"")
        let command = try LaunchCommand.build(game: added[0], settings: settings)
        try check(command.1[0] == rom.path && command.1[2] == rom.deletingPathExtension().lastPathComponent, "Path tokens preserve quotes and literal shell characters")
        try check(try LaunchCommand.tokenize("'one two' \"three four\" five") == ["one two", "three four", "five"], "Argument tokenizer")
        let process = Process(); process.executableURL = command.0; process.arguments = command.1; try process.run(); process.waitUntilExit(); try check(process.terminationStatus == 0, "Native process launch")
        var retro = settings; retro.retroArch = true; retro.core = root.appendingPathComponent("test core.dylib").path; try Data().write(to: URL(fileURLWithPath: retro.core))
        try check(try Array(LaunchCommand.build(game: added[0], settings: retro).1.prefix(2)) == ["-L", retro.core], "RetroArch core arguments")
        var rejected = false; retro.core = "/missing/orbit-core.dylib"; do { _ = try LaunchCommand.build(game: added[0], settings: retro) } catch { rejected = true }; try check(rejected, "Missing core rejected")
        for system in Catalog.all {
            let file = Bundle.main.resourceURL!.appendingPathComponent("assets/consoles/\(system.id).png")
            try check(manager.fileExists(atPath: file.path) && NSImage(contentsOf: file) != nil, "Logo asset \(system.id)")
        }
        let model = LibraryModel(directory: root)
        model.library.games = (0..<10000).map { Game(path: "/test/\($0).nes", system: "nes", title: "Game \($0)", favorite: $0 % 5 == 0) }
        model.refresh(); try check(model.visible.count == 10000, "Large-library model")
        model.selectedID = model.visible.last!.id; model.move(-1, 0); try check(model.selectedID == model.visible[9998].id, "Last-game navigation")
        model.filter = "favorites"; try check(model.visible.count == 2000, "Favorites filter")
        model.query = "Game 420"; model.refresh(); try check(model.visible.allSatisfy { $0.title.contains("Game 420") }, "Search")
        model.query = ""; model.chooseConsole("snes"); try check(model.visible.isEmpty, "Console filter")
        try Data("broken JSON".utf8).write(to: storage.file)
        let recovery = try storage.load(); try check(recovery.1 != nil && recovery.0.games.isEmpty, "Corrupt library recovery")
    }
}

@MainActor final class OrbitDelegate: NSObject, NSApplicationDelegate {
    var window: NSWindow!
    var model: LibraryModel!
    var smokeDirectory: URL?
    func applicationDidFinishLaunching(_ notification: Notification) {
        let arguments = CommandLine.arguments
        if let index = arguments.firstIndex(of: "--smoke-test"), arguments.count > index + 1 { smokeDirectory = URL(fileURLWithPath: arguments[index+1]) }
        let folder: URL
        if let smokeDirectory { folder = smokeDirectory.appendingPathComponent("data") }
        else { folder = FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask)[0].appendingPathComponent("OrbitLibrary") }
        model = LibraryModel(directory: folder)
        if smokeDirectory != nil {
            model.library.games = (0..<10000).map { Game(path: "/test/\($0).nes", system: Catalog.all[$0 % Catalog.all.count].id, title: $0 == 0 ? "Aurora Quest" : "Game \($0)", favorite: $0 % 5 == 0) }
            model.refresh()
        }
        model.controller = ControllerBridge(model)
        window = NSWindow(contentRect: NSRect(x: 0, y: 0, width: 1280, height: 860), styleMask: [.titled, .closable, .resizable, .miniaturizable], backing: .buffered, defer: false)
        window.title = "Orbit Library"; window.minSize = NSSize(width: 1080, height: 740); window.collectionBehavior = [.fullScreenPrimary]
        window.backgroundColor = NSColor(red: 7/255, green: 9/255, blue: 15/255, alpha: 1)
        window.contentView = NSHostingView(rootView: RootView(model: model))
        window.center(); installMenu(); window.makeKeyAndOrderFront(nil); NSApp.activate(ignoringOtherApps: true)
        window.toggleFullScreen(nil)
        if smokeDirectory != nil {
            DispatchQueue.main.asyncAfter(deadline: .now()+6) { [weak self] in self?.smoke() }
        }
    }
    func installMenu() {
        let main = NSMenu(), application = NSMenuItem(), menu = NSMenu(title: "Orbit Library")
        main.addItem(application); application.submenu = menu
        menu.addItem(withTitle: "About Orbit Library", action: #selector(NSApplication.orderFrontStandardAboutPanel(_:)), keyEquivalent: "")
        menu.addItem(.separator()); menu.addItem(withTitle: "Hide Orbit Library", action: #selector(NSApplication.hide(_:)), keyEquivalent: "h")
        menu.addItem(withTitle: "Quit Orbit Library", action: #selector(NSApplication.terminate(_:)), keyEquivalent: "q")
        let view = NSMenuItem(), viewMenu = NSMenu(title: "View"); main.addItem(view); view.submenu = viewMenu
        let full = NSMenuItem(title: "Toggle Full Screen", action: #selector(NSWindow.toggleFullScreen(_:)), keyEquivalent: "f"); full.keyEquivalentModifierMask = [.command, .control]; viewMenu.addItem(full)
        NSApp.mainMenu = main
    }
    func smoke() {
        guard let directory = smokeDirectory, let content = window.contentView else { exit(1) }
        do {
            try FileManager.default.createDirectory(at: directory, withIntermediateDirectories: true)
            guard window.styleMask.contains(.fullScreen) else { throw OrbitError.message("Fullscreen startup failed") }
            guard model.visible.count == 10000 else { throw OrbitError.message("10,000-game gallery initialization failed") }
            content.layoutSubtreeIfNeeded()
            guard let bitmap = content.bitmapImageRepForCachingDisplay(in: content.bounds) else { throw OrbitError.message("Could not render the UI") }
            content.cacheDisplay(in: content.bounds, to: bitmap)
            guard let bytes = bitmap.representation(using: .png, properties: [:]) else { throw OrbitError.message("Could not encode the UI preview") }
            try bytes.write(to: directory.appendingPathComponent("mac-preview.png"))
            let result = "PASS: native macOS process; fullscreen startup; three-second intro completed; 10,000-game SwiftUI gallery; rendered preview at \(Int(content.bounds.width))x\(Int(content.bounds.height)); \(Catalog.all.count) console presets.\n"
            try result.write(to: directory.appendingPathComponent("smoke-test.txt"), atomically: true, encoding: .utf8)
            print(result); exit(0)
        } catch { try? error.localizedDescription.write(to: directory.appendingPathComponent("failure.txt"), atomically: true, encoding: .utf8); fputs("SMOKE TEST FAILED: \(error)\n", stderr); exit(1) }
    }
    func applicationShouldTerminateAfterLastWindowClosed(_ sender: NSApplication) -> Bool { true }
    func applicationWillTerminate(_ notification: Notification) { model?.save() }
}

