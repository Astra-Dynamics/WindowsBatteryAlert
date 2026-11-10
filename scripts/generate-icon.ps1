# Generate high-quality Cortana-style icon for BatteryAlert
$src = @"
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

class IconGen {
    public static void Main(string[] args) {
        string outPath = args.Length > 0 ? args[0] : "app.ico";
        using (Bitmap bmp = new Bitmap(256, 256))
        using (Graphics g = Graphics.FromImage(bmp)) {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.Clear(Color.Transparent);

            // Dark rounded circular background
            using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(240, 18, 18, 20))) {
                g.FillEllipse(bgBrush, 8, 8, 240, 240);
            }

            // Outer cyan glowing ring
            using (Pen ringPen = new Pen(Color.FromArgb(255, 0, 216, 246), 18f)) {
                ringPen.StartCap = LineCap.Round;
                ringPen.EndCap = LineCap.Round;
                g.DrawArc(ringPen, 26, 26, 204, 204, -85, 290);
            }

            // Center electric lightning bolt
            using (SolidBrush boltBrush = new SolidBrush(Color.FromArgb(255, 255, 255, 255))) {
                PointF[] bolt = new PointF[] {
                    new PointF(142, 54),
                    new PointF(94, 136),
                    new PointF(130, 136),
                    new PointF(118, 204),
                    new PointF(168, 120),
                    new PointF(134, 120)
                };
                g.FillPolygon(boltBrush, bolt);
            }

            // Save as ICO
            IntPtr hIcon = bmp.GetHicon();
            using (Icon ico = Icon.FromHandle(hIcon))
            using (FileStream fs = new FileStream(outPath, FileMode.Create)) {
                ico.Save(fs);
            }
        }
        Console.WriteLine("Icon generated successfully: " + outPath);
    }
}
"@

$tempCs = Join-Path $env:TEMP "BatteryAlert_IconGen.cs"
$tempExe = Join-Path $env:TEMP "BatteryAlert_IconGen.exe"
$src | Out-File -FilePath $tempCs -Encoding utf8
& "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /target:exe /out:$tempExe /r:System.Drawing.dll $tempCs | Out-Null

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$outIco = Join-Path (Split-Path -Parent $scriptDir) "assets\app.ico"
& $tempExe $outIco
Remove-Item $tempCs, $tempExe -ErrorAction SilentlyContinue
