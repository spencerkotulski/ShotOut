$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
public static class ShotOutBrand {
    public static void Save(string file) {
        using(var b=new Bitmap(1024,1024,PixelFormat.Format32bppArgb))
        using(var g=Graphics.FromImage(b)) {
            g.Clear(Color.Transparent);g.SmoothingMode=SmoothingMode.AntiAlias;
            using(var p=new GraphicsPath()) {
                p.AddArc(24,24,400,400,180,90);p.AddArc(600,24,400,400,270,90);
                p.AddArc(600,600,400,400,0,90);p.AddArc(24,600,400,400,90,90);p.CloseFigure();
                using(var fill=new LinearGradientBrush(new Point(180,24),new Point(844,1000),Color.FromArgb(35,145,255),Color.FromArgb(15,111,238))) {fill.WrapMode=WrapMode.TileFlipXY;g.FillPath(fill,p);}
            }
            using(var pen=new Pen(Color.White,68)) {
                pen.StartCap=pen.EndCap=LineCap.Round;pen.LineJoin=LineJoin.Round;
                g.DrawLines(pen,new[]{new Point(386,266),new Point(266,266),new Point(266,386)});
                g.DrawLines(pen,new[]{new Point(638,266),new Point(758,266),new Point(758,386)});
                g.DrawLines(pen,new[]{new Point(266,638),new Point(266,758),new Point(386,758)});
                g.DrawLines(pen,new[]{new Point(638,758),new Point(758,758),new Point(758,638)});
            }
            b.Save(file,ImageFormat.Png);
        }
    }
}
'@
[ShotOutBrand]::Save((Join-Path $PSScriptRoot 'Logo.png'))
