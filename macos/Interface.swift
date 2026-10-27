import AppKit
import SwiftUI

enum Palette {
    static let background = Color(red: 7/255, green: 9/255, blue: 15/255)
    static let muted = Color(red: 0.58, green: 0.65, blue: 0.74)
    static let mint = Color(red: 0.45, green: 0.93, blue: 0.81)
}

struct OrbitMark: View {
    var time: Double = 0
    var body: some View {
        GeometryReader { proxy in
            let size = min(proxy.size.width, proxy.size.height)
            ZStack {
                Circle().stroke(Palette.mint.opacity(0.15), lineWidth: size * 0.16).blur(radius: size * 0.1)
                Circle().trim(from: 0.03, to: 0.82).stroke(Palette.mint, style: StrokeStyle(lineWidth: size * 0.07, lineCap: .round)).rotationEffect(.degrees(time * 35))
                Ellipse().stroke(Color.purple.opacity(0.7), lineWidth: size * 0.025).frame(width: size * 1.4, height: size * 0.65)
                Circle().fill(Color.white).frame(width: size * 0.25, height: size * 0.25)
                Circle().fill(Palette.mint).frame(width: size * 0.25, height: size * 0.25).offset(x: cos(time * 1.7) * size * 0.5, y: sin(time * 1.7) * size * 0.5)
            }.frame(width: proxy.size.width, height: proxy.size.height)
        }
    }
}

struct SpaceBackground: View {
    var motion: Bool
    @Environment(\.scenePhase) private var phase
    @State private var active = true
    var body: some View {
        ZStack {
            LinearGradient(colors: [Palette.background, Color(red: 0.05, green: 0.05, blue: 0.09)], startPoint: .topLeading, endPoint: .bottomTrailing)
            RadialGradient(colors: [Color.blue.opacity(0.065), .clear], center: .trailing, startRadius: 20, endRadius: 1000)
            TimelineView(.animation(minimumInterval: 1/30, paused: !motion || !active)) { timeline in
                Canvas(rendersAsynchronously: true) { context, size in
                    let time = motion ? timeline.date.timeIntervalSinceReferenceDate : 0
                    for i in 0..<5 {
                        let x = size.width * (0.16 + Double(i) * 0.19) + sin(time * 0.035 + Double(i)) * 16
                        let y = size.height * (0.2 + Double(i % 3) * 0.29) + cos(time * 0.045 + Double(i)) * 12
                        let radius = Double(18 + i % 3 * 22)
                        let rect = CGRect(x: x-radius, y: y-radius, width: radius*2, height: radius*2)
                        context.fill(Path(ellipseIn: rect), with: .linearGradient(Gradient(colors: [Color.blue.opacity(0.035), .clear]), startPoint: rect.origin, endPoint: CGPoint(x: rect.maxX, y: rect.maxY)))
                        context.stroke(Path(ellipseIn: rect), with: .color(Color.gray.opacity(0.10)), lineWidth: 0.7)
                        if i == 3 {
                            var ring = context; ring.translateBy(x: x, y: y); ring.rotate(by: .degrees(-24))
                            ring.stroke(Path(ellipseIn: CGRect(x: -radius*1.7, y: -radius*0.37, width: radius*3.4, height: radius*0.74)), with: .color(Color.gray.opacity(0.13)), lineWidth: 0.7)
                        }
                    }
                    for i in 0..<76 {
                        let x = Double(i*173 % max(1, Int(size.width))) + sin(time*0.023+Double(i))*4
                        let y = Double(i*97 % max(1, Int(size.height))) + cos(time*0.028+Double(i))*4
                        let alpha = (29+12*sin(time*0.35+Double(i)))/255
                        let radius = i % 9 == 0 ? 2.3 : 1.3
                        context.fill(Path(ellipseIn: CGRect(x: x, y: y, width: radius, height: radius)), with: .color(Color.white.opacity(alpha)))
                    }
                }
            }
        }
        .onReceive(NotificationCenter.default.publisher(for: NSApplication.didBecomeActiveNotification)) { _ in active = true }
        .onReceive(NotificationCenter.default.publisher(for: NSApplication.didResignActiveNotification)) { _ in active = false }
    }
}

struct ArtworkView: View {
    var system: String
    var game: Game?
    var revision = 0
    var maxSize = 800
    @State private var image: NSImage?
    var key: String { (game?.id ?? system) + (game?.cover ?? "") + "|\(revision)|\(maxSize)" }
    var body: some View {
        Group {
            if let image { Image(nsImage: image).resizable().interpolation(.high).scaledToFit() }
            else { OrbitMark().padding(24).foregroundColor(Palette.muted) }
        }
        .task(id: key) {
            image = nil
            var result: NSImage?
            if let game { result = await ImageCache.shared.image(path: game.cover.isEmpty ? game.path : game.cover, size: maxSize, sidecar: game.cover.isEmpty) }
            if result == nil, let resource = Bundle.main.resourceURL { result = await ImageCache.shared.image(path: resource.appendingPathComponent("assets/consoles/\(system).png").path, size: maxSize) }
            if !Task.isCancelled { image = result }
        }
    }
}

struct OrbitButtonStyle: ButtonStyle {
    var primary = false
    func makeBody(configuration: Configuration) -> some View {
        configuration.label.font(.system(size: 13)).padding(.horizontal, 15).padding(.vertical, 11)
            .foregroundColor(.white.opacity(configuration.isPressed ? 0.7 : 0.94))
            .background(RoundedRectangle(cornerRadius: 9).fill(primary ? Color(red: 0.11, green: 0.17, blue: 0.25) : Color.white.opacity(configuration.isPressed ? 0.10 : 0.045)))
            .overlay(RoundedRectangle(cornerRadius: 9).stroke(Color.white.opacity(0.16), lineWidth: 0.7))
            .scaleEffect(configuration.isPressed ? 0.98 : 1).animation(.easeOut(duration: 0.12), value: configuration.isPressed)
    }
}

struct GameTile: View {
    var game: Game
    var active: Bool
    var revision: Int
    var select: () -> Void
    var play: () -> Void
    @State private var hover = false
    var body: some View {
        VStack(alignment: .leading, spacing: 10) {
            ZStack(alignment: .topTrailing) {
                RoundedRectangle(cornerRadius: 13).fill(Color(red: 0.075, green: 0.085, blue: 0.125))
                ArtworkView(system: game.system, game: game, revision: revision, maxSize: 300).padding(12)
                if game.favorite { Image(systemName: "heart").foregroundColor(.orange.opacity(0.85)).font(.system(size: 12)).padding(11) }
            }
            .frame(width: 146, height: 116)
            .overlay(RoundedRectangle(cornerRadius: 13).stroke(active ? Color.white : Color.white.opacity(hover ? 0.45 : 0.16), lineWidth: active ? 2 : 0.7))
            .offset(y: active || hover ? -4 : 0)
            Text(game.title).font(.system(size: 12, weight: active ? .semibold : .regular)).foregroundColor(active ? .white : Palette.muted).lineLimit(1).frame(width: 146, alignment: .leading)
        }
        .frame(width: 154, height: 164)
        .contentShape(Rectangle()).onHover { hover = $0 }
        .onTapGesture(count: 2, perform: play).onTapGesture(perform: select)
        .animation(.easeOut(duration: 0.18), value: active).animation(.easeOut(duration: 0.18), value: hover)
        .accessibilityLabel(game.title).accessibilityAddTraits(.isButton)
    }
}

struct LibraryView: View {
    @ObservedObject var model: LibraryModel
    @State private var setup = false
    @State private var keyMonitor: Any?
    @FocusState private var searchFocused: Bool
    var body: some View {
        GeometryReader { geometry in
            let wide = min(1760, max(820, geometry.size.width - 112))
            let large = geometry.size.height >= 1100
            ZStack {
                SpaceBackground(motion: model.motion)
                VStack(alignment: .leading, spacing: 12) {
                    header
                    HStack(spacing: 24) {
                        TextField("Search games / ⌘K", text: $model.query).textFieldStyle(.roundedBorder).frame(width: 270).focused($searchFocused)
                        Text(model.filter == "all" ? "All games" : model.filter == "favorites" ? "Favorites" : model.filter == "recent" ? "Recently played" : model.system.name).font(.system(size: 14))
                        Spacer(); Text("\(model.visible.count) games / \(model.library.games.count) in your collection").font(.system(size: 11)).foregroundColor(Palette.muted)
                    }
                    consoleRail(large: large)
                    spotlight.frame(height: large ? 410 : max(150, min(320, geometry.size.height - 600)))
                    actions
                    Text("YOUR COLLECTION").font(.system(size: 11, weight: .semibold)).foregroundColor(.white.opacity(0.85)).padding(.top, 5)
                    gallery
                    Spacer(minLength: 0)
                    HStack {
                        Text(model.selected == nil ? "Connect your emulators to start playing." : model.system.name).lineLimit(1)
                        Spacer(); Text(model.status).lineLimit(1)
                    }.font(.system(size: 10)).foregroundColor(Palette.muted).padding(.bottom, 18)
                }
                .frame(width: wide).padding(.top, 20).frame(maxWidth: .infinity, maxHeight: .infinity, alignment: .top)
            }
        }
        .preferredColorScheme(.dark)
        .sheet(isPresented: $setup) { SetupView(model: model) }
        .alert("Orbit Library", isPresented: Binding(get: { model.error != nil }, set: { if !$0 { model.error = nil } })) { Button("OK") { model.error = nil } } message: { Text(model.error ?? "") }
        .onChange(of: model.searchRequested) { _ in searchFocused = true }
        .onAppear { installKeys() }
        .onDisappear { if let keyMonitor { NSEvent.removeMonitor(keyMonitor) }; keyMonitor = nil }
    }
    var header: some View {
        HStack(spacing: 14) {
            HStack(spacing: 17) {
                OrbitMark(time: 0.4).frame(width: 42, height: 42)
                VStack(alignment: .leading, spacing: 4) { Text("ORBIT").font(.system(size: 28, weight: .bold)); Text("A UNIVERSE OF PLAY").font(.system(size: 7.5, weight: .bold)).foregroundColor(Palette.muted) }
            }.frame(width: 220, alignment: .leading)
            Button { model.filter = "all" } label: { Label("All games", systemImage: "gamecontroller") }
            Button { model.filter = "favorites" } label: { Label("Favorites", systemImage: "heart") }
            Button { model.filter = "recent" } label: { Label("Recently played", systemImage: "clock") }
            Spacer(minLength: 0)
            Button { setup = true } label: { Label("System setup", systemImage: "gearshape") }
            Menu {
                Button("Import ROM folders", action: model.importFolders)
                Button("Scan library") { model.scan() }
                Button("Add game files", action: model.addGames)
                Button("Set selected game cover", action: model.setCover).disabled(model.selected == nil)
                Button("Remove selected game", action: model.remove).disabled(model.selected == nil)
                Divider()
                Button("Export library JSON", action: model.exportLibrary)
                Button("Import library JSON", action: model.importLibrary)
                Button("Open library data folder") { NSWorkspace.shared.open(model.storage.file.deletingLastPathComponent()) }
                Toggle("Animated background", isOn: $model.motion)
                Button("Toggle fullscreen", action: model.toggleFullscreen)
                Button("Help") { model.error = "System setup: select a ROM folder, a Mac emulator .app or executable, and launch arguments. RetroArch uses a Mac .dylib core. Scan to add games.\n\nUp: consoles; Down: games; arrows: select; Enter: play; F: favorite; ⌘K: search; F11 or Control-⌘F: fullscreen.\n\nControllers: D-pad/stick navigates, A plays, X favorites, shoulder buttons switch systems, Menu toggles fullscreen.\n\nGames, BIOS, and emulators are not included. Settings are stored locally in Application Support/OrbitLibrary." }
                Divider(); Button("Quit Orbit Library") { NSApp.terminate(nil) }
            } label: { Label("More", systemImage: "ellipsis") }.menuStyle(.borderlessButton).fixedSize()
        }.buttonStyle(OrbitButtonStyle()).padding(.bottom, 3)
    }
    func consoleRail(large: Bool) -> some View {
        ScrollViewReader { reader in
            ScrollView(.horizontal, showsIndicators: false) {
                LazyHStack(spacing: 14) {
                    ForEach(Catalog.all) { item in
                        Button { model.chooseConsole(item.id) } label: {
                            VStack(spacing: 10) {
                                ArtworkView(system: item.id).padding(12).frame(width: large ? 190 : 146, height: large ? 112 : 86)
                                    .background(RoundedRectangle(cornerRadius: 13).fill(Color.white.opacity(item.id == model.consoleID ? 0.035 : 0.012)))
                                    .overlay(RoundedRectangle(cornerRadius: 13).stroke(item.id == model.consoleID ? Color.white.opacity(0.9) : .clear, lineWidth: 2))
                                Text(item.shortName).font(.system(size: 11, weight: item.id == model.consoleID ? .semibold : .regular)).foregroundColor(item.id == model.consoleID ? .white : Palette.muted)
                            }.padding(.vertical, 5)
                        }.buttonStyle(.plain).id(item.id)
                    }
                }.padding(.horizontal, 3)
            }
            .onChange(of: model.consoleID) { value in withAnimation(.easeOut(duration: 0.22)) { reader.scrollTo(value, anchor: .center) } }
        }.frame(height: large ? 155 : 123)
    }
    var spotlight: some View {
        HStack(spacing: 30) {
            VStack(alignment: .leading, spacing: 14) {
                Text(model.system.name.uppercased()).font(.system(size: 11, weight: .semibold)).foregroundColor(Color(red: 0.56, green: 0.77, blue: 0.93))
                Text(model.selected?.title ?? "Your next adventure").font(.system(size: 45, weight: .regular)).lineLimit(2).minimumScaleFactor(0.65).id(model.selectedID)
                Text(model.selected == nil ? "Connect your emulator and add games to start playing." : "Choose your game and pick up where you left off.").font(.system(size: 14)).foregroundColor(Palette.muted)
                Text("\(model.visible.count) games in this collection" + (model.selected?.favorite == true ? " / Favorite" : "")).font(.system(size: 11)).foregroundColor(Palette.muted)
                Spacer(minLength: 0)
            }.frame(maxWidth: .infinity, alignment: .leading).padding(.top, 22)
            ArtworkView(system: model.currentSystem, game: model.selected, revision: model.artworkRevision, maxSize: 1100).frame(maxWidth: 590).padding(22)
        }.padding(.horizontal, 4)
    }
    var actions: some View {
        HStack(spacing: 16) {
            Button(action: model.play) { Label(model.launching ? "Playing…" : "Play game [Enter]", systemImage: "play") }.buttonStyle(OrbitButtonStyle(primary: true)).disabled(model.selected == nil || model.launching)
            Button(action: model.favorite) { Label(model.selected?.favorite == true ? "Unfavorite [F]" : "Favorite [F]", systemImage: "heart") }.disabled(model.selected == nil)
            Button { model.scan() } label: { Label(model.scanning ? "Scanning…" : "Scan library", systemImage: "arrow.clockwise") }.disabled(model.scanning)
            Button(action: model.importFolders) { Label("Import ROMs", systemImage: "folder") }.disabled(model.scanning)
        }.buttonStyle(OrbitButtonStyle())
    }
    var gallery: some View {
        ScrollViewReader { reader in
            ScrollView(.horizontal) {
                if model.visible.isEmpty {
                    Text(model.library.games.isEmpty ? "Import your ROM folders, connect an emulator, and bring your collection together." : "No matching games. Clear your search, choose another system, or scan your library.").foregroundColor(Palette.muted).padding(.vertical, 35).frame(maxWidth: .infinity, alignment: .leading)
                } else {
                    LazyHStack(alignment: .top, spacing: 17) {
                        ForEach(model.visible) { game in GameTile(game: game, active: game.id == model.selectedID, revision: model.artworkRevision, select: { model.choose(game) }, play: { model.choose(game); model.play() }).id(game.id) }
                    }.padding(.vertical, 6).padding(.horizontal, 3)
                }
            }
            .onChange(of: model.selectedID) { value in if let value { withAnimation(.easeOut(duration: 0.18)) { reader.scrollTo(value, anchor: .center) } } }
        }.frame(height: 192)
    }
    func installKeys() {
        guard keyMonitor == nil else { return }
        keyMonitor = NSEvent.addLocalMonitorForEvents(matching: .keyDown) { event in
            if event.modifierFlags.contains(.command), event.charactersIgnoringModifiers?.lowercased() == "k" { model.searchRequested.toggle(); return nil }
            if event.modifierFlags.contains([.command, .control]), event.charactersIgnoringModifiers?.lowercased() == "f" { model.toggleFullscreen(); return nil }
            if setup { return event }
            if let text = NSApp.keyWindow?.firstResponder as? NSTextView, text.isFieldEditor {
                if event.keyCode == 53 { model.query = ""; searchFocused = false; return nil }; return event
            }
            switch event.keyCode {
            case 123: model.move(-1, 0)
            case 124: model.move(1, 0)
            case 125: model.move(0, 1)
            case 126: model.move(0, -1)
            case 36, 76: model.confirm()
            case 3: model.favorite()
            case 103: model.toggleFullscreen()
            default: return event
            }
            return nil
        }
    }
}

struct SetupView: View {
    @ObservedObject var model: LibraryModel
    @Environment(\.dismiss) private var dismiss
    @State private var selected: String
    @State private var drafts: [String: PlatformSettings]
    init(model: LibraryModel) { self.model = model; _selected = State(initialValue: model.currentSystem); _drafts = State(initialValue: model.library.platforms) }
    func field(_ key: WritableKeyPath<PlatformSettings, String>) -> Binding<String> { Binding(get: { drafts[selected]?[keyPath: key] ?? "" }, set: { drafts[selected]?[keyPath: key] = $0 }) }
    var body: some View {
        VStack(alignment: .leading, spacing: 19) {
            Text("System setup").font(.title2.weight(.semibold))
            Picker("System", selection: $selected) { ForEach(Catalog.all) { Text($0.name).tag($0.id) } }
            Text("Suggested: \(Catalog.all.first { $0.id == selected }?.emulator ?? "")").foregroundColor(Palette.muted).font(.caption)
            row("ROM folder", field(\.folder), folder: true)
            row("Mac emulator .app or executable", field(\.exe), app: true)
            TextField("Launch arguments", text: field(\.args)).textFieldStyle(.roundedBorder)
            Text("Tokens: {rom} = full path · {name} = filename · {romdir} = folder").font(.caption).foregroundColor(Palette.muted)
            Toggle("Launch through RetroArch", isOn: Binding(get: { drafts[selected]?.retroArch ?? false }, set: { drafts[selected]?.retroArch = $0 }))
            row("Mac RetroArch core (.dylib)", field(\.core))
            Text("Use Mac emulators and cores. Windows .exe / .dll files are not Mac applications.").font(.caption).foregroundColor(Palette.muted)
            HStack { Button("Cancel") { dismiss() }; Spacer(); Button("Save setup") { model.library.platforms = drafts; model.save(); model.refresh(); dismiss() }.keyboardShortcut(.defaultAction) }
        }.padding(28).frame(width: 660).preferredColorScheme(.dark)
    }
    func row(_ name: String, _ binding: Binding<String>, folder: Bool = false, app: Bool = false) -> some View {
        HStack {
            TextField(name, text: binding).textFieldStyle(.roundedBorder)
            Button("Browse") {
                let dialog = NSOpenPanel(); dialog.title = name; dialog.canChooseDirectories = folder || app; dialog.canChooseFiles = !folder; dialog.treatsFilePackagesAsDirectories = false
                if dialog.runModal() == .OK, let file = dialog.url { binding.wrappedValue = file.path }
            }
        }
    }
}

struct RootView: View {
    @ObservedObject var model: LibraryModel
    @State private var intro = true
    @State private var started = Date()
    var body: some View {
        ZStack {
            if intro {
                TimelineView(.animation(minimumInterval: 1/60)) { context in
                    let time = context.date.timeIntervalSince(started)
                    ZStack {
                        Palette.background
                        VStack(spacing: 22) {
                            OrbitMark(time: time).frame(width: 114, height: 114)
                            Text("O R B I T").font(.system(size: 45, weight: .bold))
                            Text("A  U N I V E R S E  O F  P L A Y").font(.system(size: 11, weight: .semibold)).foregroundColor(Palette.muted)
                            GeometryReader { geometry in Capsule().fill(Palette.mint).frame(width: geometry.size.width * min(1, time/3)) }.frame(width: 170, height: 3).padding(.top, 22)
                        }.opacity(min(1, time/0.5)).offset(y: -30)
                    }
                }.transition(.opacity)
            } else { LibraryView(model: model).transition(.opacity) }
        }
        .task {
            started = Date()
            Task { for system in Catalog.all { if let root = Bundle.main.resourceURL { _ = await ImageCache.shared.image(path: root.appendingPathComponent("assets/consoles/\(system.id).png").path, size: 800) } } }
            try? await Task.sleep(nanoseconds: 3_000_000_000)
            withAnimation(.easeOut(duration: 0.25)) { intro = false }
        }
    }
}

