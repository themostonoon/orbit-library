import AppKit
import SwiftUI
import GameController
import ImageIO
import UniformTypeIdentifiers

@MainActor final class LibraryModel: ObservableObject {
    @Published var library: LibraryData
    @Published var filter = "all" { didSet { refresh() } }
    @Published var query = "" { didSet { searchTask?.cancel(); searchTask = Task { try? await Task.sleep(nanoseconds: 100_000_000); if !Task.isCancelled { refresh() } } } }
    @Published var visible: [Game] = []
    @Published var selectedID: String?
    @Published var consoleID = "nes"
    @Published var consoleFocus = false
    @Published var status = "Up: consoles / Down: games / F11: window / ⌘K: search"
    @Published var error: String?
    @Published var scanning = false
    @Published var launching = false
    @Published var motion = true
    @Published var searchRequested = false
    @Published var artworkRevision = 0
    let storage: LibraryStorage
    var process: Process?
    var searchTask: Task<Void, Never>?
    var controller: ControllerBridge?
    var selected: Game? { visible.first { $0.id == selectedID } }
    var currentSystem: String { !["all", "favorites", "recent"].contains(filter) ? filter : selected?.system ?? consoleID }
    var system: SystemDef { Catalog.all.first { $0.id == currentSystem } ?? Catalog.all[0] }

    init(directory: URL) {
        storage = LibraryStorage(directory: directory)
        do { let loaded = try storage.load(); library = loaded.0; error = loaded.1 }
        catch { library = LibraryData(); library.completePresets(); self.error = error.localizedDescription }
        refresh()
    }
    func save() { do { try storage.save(library) } catch { self.error = error.localizedDescription } }
    func refresh() {
        var games = library.games.filter { game in
            let category = filter == "all" || (filter == "favorites" && game.favorite) || (filter == "recent" && !game.lastPlayed.isEmpty) || filter == game.system
            return category && (query.isEmpty || game.title.localizedCaseInsensitiveContains(query)) && Catalog.all.contains { $0.id == game.system }
        }
        if filter == "recent" { games.sort { $0.lastPlayed > $1.lastPlayed } }
        else { games.sort { $0.title.localizedStandardCompare($1.title) == .orderedAscending } }
        visible = games
        if !games.contains(where: { $0.id == selectedID }) { selectedID = games.first?.id }
    }
    func chooseConsole(_ id: String) { consoleFocus = true; consoleID = id; filter = id }
    func choose(_ game: Game) { selectedID = game.id; consoleFocus = false }
    func move(_ dx: Int, _ dy: Int) {
        if dy < 0 { consoleFocus = true; return }
        if dy > 0 { consoleFocus = false; return }
        if consoleFocus {
            let old = Catalog.all.firstIndex { $0.id == consoleID } ?? 0
            chooseConsole(Catalog.all[max(0, min(Catalog.all.count - 1, old + dx))].id)
        } else if !visible.isEmpty {
            let old = visible.firstIndex { $0.id == selectedID } ?? 0
            selectedID = visible[max(0, min(visible.count - 1, old + dx))].id
        }
    }
    func confirm() { if consoleFocus { consoleFocus = false } else { play() } }
    func favorite() {
        guard let id = selectedID, let index = library.games.firstIndex(where: { $0.id == id }) else { return }
        library.games[index].favorite.toggle(); save(); refresh()
    }
    func setCover() {
        guard let id = selectedID, let index = library.games.firstIndex(where: { $0.id == id }) else { return }
        let dialog = NSOpenPanel(); dialog.title = "Choose game artwork"; dialog.allowedContentTypes = [.png, .jpeg, .bmp]
        if dialog.runModal() == .OK, let file = dialog.url {
            library.games[index].cover = file.path; artworkRevision += 1; ImageCache.shared.clear(); save(); refresh()
        }
    }
    func addGames() {
        let id = currentSystem
        let dialog = NSOpenPanel(); dialog.title = "Add games to \(system.name)"; dialog.allowsMultipleSelection = true
        if dialog.runModal() == .OK {
            var known = Set(library.games.map { $0.id })
            for file in dialog.urls {
                let game = Game(path: file.path, system: id, title: file.deletingPathExtension().lastPathComponent)
                if known.insert(game.id).inserted { library.games.append(game) }
            }
            save(); refresh()
        }
    }
    func remove() { guard let id = selectedID else { return }; library.games.removeAll { $0.id == id }; save(); refresh(); status = "Removed from the library. The game file was kept." }
    func scan(importRoot: URL? = nil) {
        guard !scanning else { return }
        scanning = true; status = "Scanning in the background. You can keep browsing."
        let existing = library.games, settings = library.platforms
        Task {
            do {
                let result = try await Task.detached(priority: .utility) { () throws -> ([Game], [String: String]) in
                    let folders = try importRoot.map { try ScanEngine.folders(in: $0) } ?? settings.mapValues { $0.folder }.filter { !$0.value.isEmpty }
                    return (try ScanEngine.scan(existing: existing, folders: folders), folders)
                }.value
                var known = Set(library.games.map { $0.id }); var count = 0
                for game in result.0 where known.insert(game.id).inserted { library.games.append(game); count += 1 }
                if importRoot != nil { for (id, folder) in result.1 { library.platforms[id]?.folder = folder } }
                ImageCache.shared.clear(); artworkRevision += 1; save(); refresh()
                status = "Scan complete: \(count) new games in \(result.1.count) system folders."
            } catch { self.error = error.localizedDescription }
            scanning = false
        }
    }
    func importFolders() {
        let dialog = NSOpenPanel(); dialog.title = "Import the root containing console folders"; dialog.canChooseFiles = false; dialog.canChooseDirectories = true
        if dialog.runModal() == .OK, let root = dialog.url { scan(importRoot: root) }
    }
    func exportLibrary() {
        let dialog = NSSavePanel(); dialog.nameFieldStringValue = "library.json"
        if dialog.runModal() == .OK, let file = dialog.url { do { try JSONEncoder().encode(library).write(to: file, options: .atomic) } catch { self.error = error.localizedDescription } }
    }
    func importLibrary() {
        let dialog = NSOpenPanel(); dialog.title = "Import an Orbit library JSON file"; dialog.allowedContentTypes = [.json]
        if dialog.runModal() == .OK, let file = dialog.url {
            do { var data = try JSONDecoder().decode(LibraryData.self, from: Data(contentsOf: file)); data.completePresets(); library = data; save(); refresh(); status = "Library imported. Configure Mac emulator and ROM paths in System setup." }
            catch { self.error = error.localizedDescription }
        }
    }
    func play() {
        guard !launching, let game = selected, let settings = library.platforms[game.system] else { return }
        do {
            let command = try LaunchCommand.build(game: game, settings: settings)
            let child = Process(); child.executableURL = command.0; child.arguments = command.1; child.currentDirectoryURL = command.0.deletingLastPathComponent()
            child.terminationHandler = { [weak self] process in DispatchQueue.main.async {
                guard let self else { return }; self.launching = false; self.process = nil
                NSApp.unhide(nil); NSApp.activate(ignoringOtherApps: true); NSApp.windows.first?.makeKeyAndOrderFront(nil)
                self.status = process.terminationStatus == 0 ? "Welcome back. Choose your next game." : "The emulator exited with code \(process.terminationStatus). Check System setup."
            } }
            try child.run(); process = child; launching = true
            if let index = library.games.firstIndex(where: { $0.id == game.id }) { library.games[index].plays += 1; library.games[index].lastPlayed = ISO8601DateFormatter().string(from: Date()) }
            save(); refresh(); status = "Playing \(game.title). Close the emulator to return to Orbit."; NSApp.hide(nil)
        } catch { self.error = error.localizedDescription }
    }
    func toggleFullscreen() { NSApp.windows.first(where: { $0.title == "Orbit Library" })?.toggleFullScreen(nil) }
    func nextCategory(_ delta: Int) {
        let categories = ["all", "favorites", "recent"] + Catalog.all.map { $0.id }
        let index = categories.firstIndex(of: filter) ?? 0; filter = categories[max(0, min(categories.count - 1, index + delta))]
        if Catalog.all.contains(where: { $0.id == filter }) { consoleID = filter }
    }
}

// CGImage is fully decoded before this immutable ownership handoff. AppKit images
// are created and cached only on the main actor; the worker never mutates them.
private struct DecodedArtwork: @unchecked Sendable { let pixels: CGImage? }
@MainActor final class ImageCache {
    static let shared = ImageCache()
    private let cache = NSCache<NSString, NSImage>()
    private var pending: [String: Task<DecodedArtwork, Never>] = [:]
    init() { cache.totalCostLimit = 64 * 1024 * 1024; cache.countLimit = 160 }
    func clear() { cache.removeAllObjects() }
    func image(path: String, size: Int, sidecar: Bool = false) async -> NSImage? {
        let key = path + "|\(size)|\(sidecar)"
        if let image = cache.object(forKey: key as NSString) { return image }
        let task: Task<DecodedArtwork, Never>
        if let existing = pending[key] { task = existing }
        else {
            task = Task.detached(priority: .utility) { () -> DecodedArtwork in autoreleasepool {
                var file = URL(fileURLWithPath: path)
                if sidecar {
                    guard let found = ["png", "jpg", "jpeg"].map({ file.deletingPathExtension().appendingPathExtension($0) }).first(where: { FileManager.default.fileExists(atPath: $0.path) }) else { return DecodedArtwork(pixels: nil) }
                    file = found
                }
                guard let source = CGImageSourceCreateWithURL(file as CFURL, nil) else { return DecodedArtwork(pixels: nil) }
                let options: [CFString: Any] = [kCGImageSourceCreateThumbnailFromImageAlways: true, kCGImageSourceThumbnailMaxPixelSize: size, kCGImageSourceCreateThumbnailWithTransform: true, kCGImageSourceShouldCacheImmediately: true]
                return DecodedArtwork(pixels: CGImageSourceCreateThumbnailAtIndex(source, 0, options as CFDictionary))
            } }
            pending[key] = task
        }
        let decoded = await task.value; pending[key] = nil
        if let image = cache.object(forKey: key as NSString) { return image }
        guard let pixels = decoded.pixels else { return nil }
        let image = NSImage(cgImage: pixels, size: NSSize(width: pixels.width, height: pixels.height))
        cache.setObject(image, forKey: key as NSString, cost: pixels.width * pixels.height * 4)
        return image
    }
}

@MainActor final class ControllerBridge {
    weak var model: LibraryModel?
    var observer: NSObjectProtocol?
    var repeatTimer: Timer?
    var direction = (0, 0)
    init(_ model: LibraryModel) {
        self.model = model
        observer = NotificationCenter.default.addObserver(forName: .GCControllerDidConnect, object: nil, queue: .main) { [weak self] _ in Task { @MainActor in self?.connect() } }
        connect()
    }
    func connect() {
        for controller in GCController.controllers() {
            guard let pad = controller.extendedGamepad else { continue }
            pad.buttonA.pressedChangedHandler = { [weak self] _, _, pressed in if pressed { DispatchQueue.main.async { if NSApp.isActive { self?.model?.confirm() } } } }
            pad.buttonX.pressedChangedHandler = { [weak self] _, _, pressed in if pressed { DispatchQueue.main.async { if NSApp.isActive { self?.model?.favorite() } } } }
            pad.leftShoulder.pressedChangedHandler = { [weak self] _, _, pressed in if pressed { DispatchQueue.main.async { if NSApp.isActive { self?.model?.nextCategory(-1) } } } }
            pad.rightShoulder.pressedChangedHandler = { [weak self] _, _, pressed in if pressed { DispatchQueue.main.async { if NSApp.isActive { self?.model?.nextCategory(1) } } } }
            pad.buttonMenu.pressedChangedHandler = { [weak self] _, _, pressed in if pressed { DispatchQueue.main.async { if NSApp.isActive { self?.model?.toggleFullscreen() } } } }
            let move: GCControllerDirectionPadValueChangedHandler = { [weak self] _, x, y in DispatchQueue.main.async {
                guard let self else { return }; self.direction = (x > 0.5 ? 1 : x < -0.5 ? -1 : 0, y > 0.5 ? -1 : y < -0.5 ? 1 : 0)
                self.repeatTimer?.invalidate(); self.repeatTimer = nil
                if self.direction != (0, 0), NSApp.isActive {
                    self.model?.move(self.direction.0, self.direction.1)
                    self.repeatTimer = Timer.scheduledTimer(withTimeInterval: 0.18, repeats: true) { [weak self] _ in Task { @MainActor in guard let self, NSApp.isActive else { return }; self.model?.move(self.direction.0, self.direction.1) } }
                }
            } }
            pad.dpad.valueChangedHandler = move; pad.leftThumbstick.valueChangedHandler = move
        }
    }
    deinit { if let observer { NotificationCenter.default.removeObserver(observer) }; repeatTimer?.invalidate() }
}

