import Foundation

struct Game: Codable, Identifiable, Equatable {
    var path: String
    var system: String
    var title: String
    var cover = ""
    var favorite = false
    var lastPlayed = ""
    var plays = 0
    var id: String { system + "|" + path }
    enum CodingKeys: String, CodingKey {
        case path = "Path", system = "System", title = "Title", cover = "Cover"
        case favorite = "Favorite", lastPlayed = "LastPlayed", plays = "Plays"
    }
}

struct PlatformSettings: Codable {
    var folder = ""
    var exe = ""
    var args = "\"{rom}\""
    var core = ""
    var retroArch = false
    enum CodingKeys: String, CodingKey {
        case folder = "Folder", exe = "Exe", args = "Args", core = "Core", retroArch = "RetroArch"
    }
}

struct LibraryData: Codable {
    var version = 1
    var platforms: [String: PlatformSettings] = [:]
    var games: [Game] = []
    enum CodingKeys: String, CodingKey {
        case version = "Version", platforms = "Platforms", games = "Games"
    }
    mutating func completePresets() {
        for item in Catalog.all where platforms[item.id] == nil {
            platforms[item.id] = PlatformSettings(args: item.arguments)
        }
    }
}

struct SystemDef: Identifiable {
    var id: String
    var name: String
    var emulator: String
    var core: String
    var extensions: String
    var aliases: String
    var arguments: String
    var shortName: String {
        let names = ["nes":"NES", "snes":"Super Nintendo", "n64":"Nintendo 64", "gc":"GameCube",
                     "nds":"Nintendo DS", "3ds":"Nintendo 3DS", "psx":"PlayStation", "psp":"PSP",
                     "megadrive":"Genesis", "mastersystem":"Master System", "pce":"PC Engine"]
        return names[id] ?? name.replacingOccurrences(of: "Nintendo ", with: "").replacingOccurrences(of: "Sega ", with: "")
    }
}

struct LibraryStorage {
    let file: URL
    init(directory: URL) { file = directory.appendingPathComponent("library.json") }
    func load() throws -> (LibraryData, String?) {
        let manager = FileManager.default
        try manager.createDirectory(at: file.deletingLastPathComponent(), withIntermediateDirectories: true)
        var data = LibraryData()
        var warning: String?
        if manager.fileExists(atPath: file.path) {
            do { data = try JSONDecoder().decode(LibraryData.self, from: Data(contentsOf: file)) }
            catch {
                let backup = file.appendingPathExtension("unreadable-\(Int(Date().timeIntervalSince1970)).bak")
                try manager.copyItem(at: file, to: backup)
                warning = "The saved library could not be read. A backup was preserved at \(backup.path)."
            }
        }
        data.completePresets()
        return (data, warning)
    }
    func save(_ library: LibraryData) throws {
        let manager = FileManager.default
        try manager.createDirectory(at: file.deletingLastPathComponent(), withIntermediateDirectories: true)
        let bytes = try JSONEncoder().encode(library)
        if manager.fileExists(atPath: file.path) {
            let backup = file.appendingPathExtension("bak")
            if manager.fileExists(atPath: backup.path) { try manager.removeItem(at: backup) }
            try manager.copyItem(at: file, to: backup)
        }
        try bytes.write(to: file, options: .atomic)
    }
}

enum OrbitError: LocalizedError {
    case message(String)
    var errorDescription: String? { if case let .message(text) = self { return text }; return nil }
}

enum LaunchCommand {
    // Parse the argument template first, then substitute paths, preserving spaces and quotes in Mac filenames.
    static func tokenize(_ template: String) throws -> [String] {
        var result: [String] = [], token = "", quote: Character?, escaped = false, started = false
        for character in template {
            if escaped { token.append(character); escaped = false; started = true; continue }
            if character == "\\", quote != "'" { escaped = true; started = true; continue }
            if let active = quote {
                if character == active { quote = nil } else { token.append(character) }
                continue
            }
            if character == "\"" || character == "'" { quote = character; started = true }
            else if character.isWhitespace {
                if started { result.append(token); token = ""; started = false }
            } else { token.append(character); started = true }
        }
        guard quote == nil else { throw OrbitError.message("Launch arguments contain an unmatched quote.") }
        if escaped { token.append("\\") }
        if started { result.append(token) }
        return result
    }
    static func build(game: Game, settings: PlatformSettings) throws -> (URL, [String]) {
        let manager = FileManager.default
        guard manager.fileExists(atPath: game.path) else { throw OrbitError.message("The game file is missing. Reconnect its drive or import its new location.") }
        var executable = URL(fileURLWithPath: settings.exe)
        if executable.pathExtension.lowercased() == "app" {
            guard let binary = Bundle(url: executable)?.executableURL else { throw OrbitError.message("Choose a valid emulator application in System setup.") }
            executable = binary
        }
        guard !settings.exe.isEmpty, manager.isExecutableFile(atPath: executable.path) else { throw OrbitError.message("Choose a Mac emulator .app or executable in System setup first. Windows .exe files cannot run on macOS.") }
        if settings.retroArch, !manager.fileExists(atPath: settings.core) { throw OrbitError.message("Choose the installed Mac RetroArch core (.dylib) in System setup.") }
        let rom = URL(fileURLWithPath: game.path)
        var arguments = try tokenize(settings.args).map { argument in
            argument.replacingOccurrences(of: "{rom}", with: game.path)
                .replacingOccurrences(of: "{name}", with: rom.deletingPathExtension().lastPathComponent)
                .replacingOccurrences(of: "{romdir}", with: rom.deletingLastPathComponent().path)
        }
        if settings.retroArch { arguments = ["-L", settings.core] + arguments }
        return (executable, arguments)
    }
}

enum ScanEngine {
    static func folders(in root: URL) throws -> [String: String] {
        var result: [String: String] = [:]
        for directory in try FileManager.default.contentsOfDirectory(at: root, includingPropertiesForKeys: [.isDirectoryKey], options: .skipsHiddenFiles) {
            guard (try? directory.resourceValues(forKeys: [.isDirectoryKey]).isDirectory) == true else { continue }
            let name = directory.lastPathComponent.lowercased().filter { $0.isLetter || $0.isNumber }
            if let system = Catalog.all.first(where: { $0.aliases.split(separator: "|").contains(Substring(name)) }) { result[system.id] = directory.path }
        }
        return result
    }
    static func scan(existing: [Game], folders: [String: String]) throws -> [Game] {
        var known = Set(existing.map { $0.id }), added: [Game] = []
        let manager = FileManager.default
        for (id, path) in folders {
            guard let system = Catalog.all.first(where: { $0.id == id }), !path.isEmpty else { continue }
            let suffixes = Set(system.extensions.split(separator: "|").map(String.init))
            let root = URL(fileURLWithPath: path)
            let keys: [URLResourceKey] = [.isDirectoryKey, .isSymbolicLinkKey, .isRegularFileKey]
            guard let iterator = manager.enumerator(at: root, includingPropertiesForKeys: keys, options: [.skipsHiddenFiles, .skipsPackageDescendants], errorHandler: { _, _ in true }) else { continue }
            var files: [URL] = []
            for case let file as URL in iterator {
                guard let values = try? file.resourceValues(forKeys: Set(keys)) else { continue }
                if values.isSymbolicLink == true { iterator.skipDescendants(); continue }
                if values.isRegularFile == true { files.append(file) }
            }
            var tracks = Set<String>()
            let regex = try NSRegularExpression(pattern: #"(?im)^\s*FILE\s+"([^"]+)""#)
            for cue in files where cue.pathExtension.lowercased() == "cue" {
                guard let text = try? String(contentsOf: cue) else { continue }
                for match in regex.matches(in: text, range: NSRange(text.startIndex..., in: text)) {
                    if let range = Range(match.range(at: 1), in: text) { tracks.insert(cue.deletingLastPathComponent().appendingPathComponent(String(text[range])).standardizedFileURL.path) }
                }
            }
            for file in files where suffixes.contains("." + file.pathExtension.lowercased()) && !tracks.contains(file.standardizedFileURL.path) {
                let game = Game(path: file.path, system: id, title: file.deletingPathExtension().lastPathComponent)
                if known.insert(game.id).inserted { added.append(game) }
            }
        }
        return added
    }
}
