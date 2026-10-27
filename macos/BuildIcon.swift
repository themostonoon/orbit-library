import AppKit

let directory = URL(fileURLWithPath: CommandLine.arguments[1])
try FileManager.default.createDirectory(at: directory, withIntermediateDirectories: true)
let mint = NSColor(calibratedRed: 0.45, green: 0.93, blue: 0.81, alpha: 1)
for (points, scale) in [(16,1),(16,2),(32,1),(32,2),(128,1),(128,2),(256,1),(256,2),(512,1),(512,2)] {
    let size = points * scale
    let rep = NSBitmapImageRep(bitmapDataPlanes: nil, pixelsWide: size, pixelsHigh: size, bitsPerSample: 8, samplesPerPixel: 4, hasAlpha: true, isPlanar: false, colorSpaceName: .deviceRGB, bytesPerRow: 0, bitsPerPixel: 0)!
    NSGraphicsContext.saveGraphicsState(); NSGraphicsContext.current = NSGraphicsContext(bitmapImageRep: rep)
    let factor = CGFloat(size) / 1024
    let transform = NSAffineTransform(); transform.scale(by: factor); transform.concat()
    NSColor(calibratedRed: 0.035, green: 0.045, blue: 0.075, alpha: 1).setFill()
    NSBezierPath(roundedRect: NSRect(x: 28, y: 28, width: 968, height: 968), xRadius: 215, yRadius: 215).fill()
    let orbit = NSBezierPath(); orbit.appendArc(withCenter: NSPoint(x: 512, y: 512), radius: 238, startAngle: 20, endAngle: 300)
    orbit.lineWidth = 38; orbit.lineCapStyle = .round; mint.setStroke(); orbit.stroke()
    let ring = NSBezierPath(ovalIn: NSRect(x: 168, y: 363, width: 688, height: 298)); ring.lineWidth = 14
    NSColor(calibratedRed: 0.57, green: 0.47, blue: 0.97, alpha: 0.8).setStroke(); ring.stroke()
    NSColor.white.setFill(); NSBezierPath(ovalIn: NSRect(x: 452, y: 452, width: 120, height: 120)).fill()
    mint.setFill(); NSBezierPath(ovalIn: NSRect(x: 654, y: 301, width: 132, height: 132)).fill()
    NSGraphicsContext.restoreGraphicsState()
    let name = "icon_\(points)x\(points)" + (scale == 2 ? "@2x" : "") + ".png"
    try rep.representation(using: .png, properties: [:])!.write(to: directory.appendingPathComponent(name))
}
