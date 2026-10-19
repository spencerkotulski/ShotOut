using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
namespace ShotOut {
class VerticalInspector : FlowLayoutPanel {
    [DllImport("user32.dll")] static extern bool ShowScrollBar(IntPtr window,int bar,bool show);
    bool updating;
    protected override void OnLayout(LayoutEventArgs e){base.OnLayout(e);if(!updating&&IsHandleCreated){updating=true;try{ShowScrollBar(Handle,0,false);}finally{updating=false;}}}
}
static partial class Art {
    public static PointF TailPoint(Mark m){var r=m.BodyBounds;var size=LocalSize(m);var point=OrientPoint(new PointF(m.TailTipX*size.Width,m.TailTipY*size.Height),size,m.Orientation);return new PointF(r.Left+point.X,r.Top+point.Y);}
    public static GraphicsPath BalloonPath(Mark m){
        var r=m.BodyBounds;if(m.Tail)r.Height=Math.Max(1,r.Height-14);float radius=Math.Min(Math.Max(0,m.Radius),Math.Min(r.Width,r.Height)/2);var tip=TailPoint(m);int side=-1;
        if(m.Tail){float[] outside={r.Top-tip.Y,tip.X-r.Right,tip.Y-r.Bottom,r.Left-tip.X};float max=0;for(int i=0;i<4;i++)if(outside[i]>max){max=outside[i];side=i;}}
        var path=new GraphicsPath();
        Action<PointF,PointF,int> edge=delegate(PointF a,PointF b,int index){float dx=b.X-a.X,dy=b.Y-a.Y,length=(float)Math.Sqrt(dx*dx+dy*dy);if(index!=side||length<3){path.AddLine(a,b);return;}dx/=length;dy/=length;float half=Math.Min(11,length/4);float center=Math.Max(half,Math.Min(length-half,(tip.X-a.X)*dx+(tip.Y-a.Y)*dy));var first=new PointF(a.X+dx*(center-half),a.Y+dy*(center-half));var last=new PointF(a.X+dx*(center+half),a.Y+dy*(center+half));path.AddLine(a,first);path.AddLine(first,tip);path.AddLine(tip,last);path.AddLine(last,b);};
        edge(new PointF(r.Left+radius,r.Top),new PointF(r.Right-radius,r.Top),0);if(radius>0)path.AddArc(r.Right-2*radius,r.Top,2*radius,2*radius,270,90);
        edge(new PointF(r.Right,r.Top+radius),new PointF(r.Right,r.Bottom-radius),1);if(radius>0)path.AddArc(r.Right-2*radius,r.Bottom-2*radius,2*radius,2*radius,0,90);
        edge(new PointF(r.Right-radius,r.Bottom),new PointF(r.Left+radius,r.Bottom),2);if(radius>0)path.AddArc(r.Left,r.Bottom-2*radius,2*radius,2*radius,90,90);
        edge(new PointF(r.Left,r.Bottom-radius),new PointF(r.Left,r.Top+radius),3);if(radius>0)path.AddArc(r.Left,r.Top,2*radius,2*radius,180,90);path.CloseFigure();return path;
    }
    class ShadowCache {public string Key;public Bitmap Image;public float Left,Top,Width,Height;}
    static readonly ConditionalWeakTable<Mark,ShadowCache> Shadows=new ConditionalWeakTable<Mark,ShadowCache>();
    static string ShadowKey(Mark m){var key=new StringBuilder();key.Append(m.Orientation).Append(m.Kind).Append('|').Append(m.W).Append('|').Append(m.H).Append('|').Append(m.Width).Append('|').Append(m.Radius).Append('|').Append(m.ShadowSize).Append('|').Append(m.ShadowIntensity).Append('|').Append(m.Fill).Append('|').Append(m.FillOpacity).Append('|').Append(m.Tail).Append('|').Append(m.TailTipX).Append('|').Append(m.TailTipY).Append('|').Append(m.CurveX).Append('|').Append(m.CurveY).Append('|').Append(m.Text).Append('|').Append(m.TextAlignment).Append('|').Append(m.Size).Append('|').Append(m.Bold).Append(m.Italic).Append(m.Underline);foreach(var p in m.Points)key.Append('|').Append(Math.Round(p.X-m.X,2)).Append(',').Append(Math.Round(p.Y-m.Y,2));foreach(var run in m.Runs)key.Append('|').Append(run.Style).Append(':').Append(run.Text);if(m.Image!=null)key.Append('|').Append(RuntimeHelpers.GetHashCode(m.Image));return key.ToString();}
    static void DrawShadow(Bitmap target,Mark mark,Rectangle? updateRegion){
        if(mark.ShadowIntensity<=0)return;var key=ShadowKey(mark);ShadowCache cache;
        if(!Shadows.TryGetValue(mark,out cache)){cache=new ShadowCache();Shadows.Add(mark,cache);}
        if(cache.Key!=key){
            float pad=Math.Max(6,mark.Width*6+4)+mark.ShadowSize*2+6;var bounds=mark.Kind=="Magnify"?mark.BodyBounds:mark.Bounds;bounds.Inflate(pad,pad);var box=Rectangle.Ceiling(bounds);if(box.Width<1||box.Height<1)return;
            float scale=Math.Min(1,1000f/Math.Max(box.Width,box.Height));int width=Math.Max(1,(int)Math.Ceiling(box.Width*scale)),height=Math.Max(1,(int)Math.Ceiling(box.Height*scale));
            using(var mask=new Bitmap(width,height,PixelFormat.Format32bppArgb)){
                var clone=mark.Clone();clone.Move(-box.X,-box.Y);clone.Shadow=false;clone.Color=Color.White.ToArgb();clone.FillColor=Color.White.ToArgb();if(clone.Kind=="Blur"||clone.Kind=="Pixelate"||clone.Kind=="Magnify"||clone.Kind=="Spotlight"){clone.Kind="Rectangle";clone.Fill=true;}
                using(var g=Graphics.FromImage(mask)){g.ScaleTransform(scale,scale);PaintVector(g,mask,clone);}
                int radius=(int)Math.Round(Math.Max(0,mark.ShadowSize)*scale);var shadow=radius>0?Blur(mask,radius):new Bitmap(mask);
                var data=shadow.LockBits(new Rectangle(0,0,shadow.Width,shadow.Height),ImageLockMode.ReadWrite,PixelFormat.Format32bppArgb);try{var pixels=new byte[data.Stride*shadow.Height];Marshal.Copy(data.Scan0,pixels,0,pixels.Length);for(int i=0;i<pixels.Length;i+=4){pixels[i]=pixels[i+1]=pixels[i+2]=0;pixels[i+3]=(byte)(pixels[i+3]*Math.Max(0,Math.Min(100,mark.ShadowIntensity))/100);}Marshal.Copy(pixels,0,data.Scan0,pixels.Length);}finally{shadow.UnlockBits(data);}
                if(cache.Image!=null)cache.Image.Dispose();cache.Image=shadow;cache.Key=key;float distance=Math.Max(2,Math.Min(10,mark.ShadowSize*.5f));cache.Left=box.X-mark.X+distance;cache.Top=box.Y-mark.Y+distance;cache.Width=box.Width;cache.Height=box.Height;
            }
        }
        using(var g=Graphics.FromImage(target)){if(updateRegion.HasValue)g.SetClip(updateRegion.Value);g.InterpolationMode=InterpolationMode.HighQualityBilinear;g.DrawImage(cache.Image,new RectangleF(mark.X+cache.Left,mark.Y+cache.Top,cache.Width,cache.Height));}
    }
}
partial class Editor {
    CheckBox shadowBox;NumberField shadowSize,shadowIntensity;FlowLayoutPanel shadowToggleRow,shadowSizeRow,shadowIntensityRow;
    void AddShadowInspector(){
        shadowToggleRow=Row();shadowBox=new ThemeCheckBox {Text="Drop shadow",Width=213,Height=34};shadowBox.CheckedChanged+=delegate{ChangeStyle();UpdateInspector();};shadowToggleRow.Controls.Add(shadowBox);inspector.Controls.Add(shadowToggleRow);
        shadowSizeRow=Row();shadowSizeRow.Controls.Add(Caption("Shadow size · px",145));shadowSize=Number(0,64,8);shadowSize.ValueChanged+=delegate{ChangeStyle();};shadowSizeRow.Controls.Add(shadowSize);inspector.Controls.Add(shadowSizeRow);
        shadowIntensityRow=Row();shadowIntensityRow.Controls.Add(Caption("Shadow intensity · %",145));shadowIntensity=Number(0,100,40);shadowIntensity.ValueChanged+=delegate{ChangeStyle();};shadowIntensityRow.Controls.Add(shadowIntensity);inspector.Controls.Add(shadowIntensityRow);
        AddMagnifyInspector();AddBucketInspector();AddFillOpacityInspector();
        inspector.ClientSizeChanged+=delegate{FitInspectorControls();};
    }
    void FitInspectorControls(){int width=Math.Max(170,Math.Min(217,inspector.ClientSize.Width-3));inspector.SuspendLayout();foreach(Control child in inspector.Controls){child.Width=width;var row=child as FlowLayoutPanel;if(row==null||row.Controls.Count==0)continue;if(row.Controls.Count==2&&row.Controls[1] is NumberField){row.Controls[0].Width=Math.Max(90,width-row.Controls[1].Width-row.Controls[0].Margin.Horizontal-row.Controls[1].Margin.Horizontal);}else{int used=0;for(int i=0;i<row.Controls.Count;i++){var control=row.Controls[i];if(i==row.Controls.Count-1)control.Width=Math.Max(30,width-used-control.Margin.Horizontal);used+=control.Width+control.Margin.Horizontal;}}}inspector.ResumeLayout();}
    int ShadowInspectorHeight(string kind){bool available=kind!="Spotlight"&&kind!="Clone stamp"&&kind!="Grayscale"&&kind!="Hand"&&kind!="Select"&&kind!="Crop"&&!kind.StartsWith("Cut ");shadowToggleRow.Visible=available;shadowSizeRow.Visible=shadowIntensityRow.Visible=available&&shadowBox.Checked;bucketToleranceRow.Visible=kind=="Bucket";int extra=MagnifyInspectorHeight(kind)+(kind=="Bucket"?47:0);var order=new Control[]{toolTitle,variantRow,strokeRow,fillRow,fillOpacityRow,adjustmentRow,weightRow,bucketToleranceRow,cloneRow,cloneHardnessRow,cloneOpacityRow,magnifyZoomRow,magnifyLinkRow,radiusRow,tailRow,textRow,formatRow,alignmentRow,editRow,shadowToggleRow,shadowSizeRow,shadowIntensityRow,inspectorHint};inspector.SuspendLayout();for(int i=0;i<order.Length;i++)if(order[i]!=null)inspector.Controls.SetChildIndex(order[i],i);inspector.ResumeLayout();return extra+(available?(shadowBox.Checked?141:47):0);}
    public void DrawBubbleHandles(Graphics g,Mark mark){var body=mark.BodyBounds;using(var pen=new Pen(Color.FromArgb(131,170,246),1/View.Zoom)){pen.DashStyle=DashStyle.Dash;g.DrawRectangle(pen,body.X,body.Y,body.Width,body.Height);}float d=7/View.Zoom;foreach(var p in Handles(body))g.FillRectangle(Brushes.White,p.X-d/2,p.Y-d/2,d,d);if(mark.Tail){var tip=Art.TailPoint(mark);d=6/View.Zoom;using(var b=new SolidBrush(Color.FromArgb(255,194,91)))g.FillPolygon(b,new[]{new PointF(tip.X,tip.Y-d),new PointF(tip.X+d,tip.Y),new PointF(tip.X,tip.Y+d),new PointF(tip.X-d,tip.Y)});}}
    public void VerifyShadowUpdate(List<string> report,string folder){
        Action<bool,string> check=delegate(bool ok,string label){if(!ok)throw new Exception(label);report.Add("PASS "+label);};
        LoadBitmap(Blank(700,450),"Bubble and shadow");var bubble=new Mark{Kind="Balloon",X=180,Y=100,W=230,H=130,Radius=18,Text="Drag my tail",Size=22,Color=Color.RoyalBlue.ToArgb()};Marks.Add(bubble);Active=bubble;SetTool("Select");Active=bubble;SyncStyle();resizeOriginal=bubble.Clone();resizeHandle=200;ResizeActive(new PointF(460,300));check(Math.Abs(Art.TailPoint(bubble).X-460)<.01&&Math.Abs(Art.TailPoint(bubble).Y-300)<.01&&bubble.W==230,"Tail tip drags independently without resizing bubble");bubble.Move(10,15);check(Math.Abs(Art.TailPoint(bubble).X-470)<.01&&Math.Abs(Art.TailPoint(bubble).Y-315)<.01,"Bubble tail follows whole-object movement");
        foreach(string kind in new[]{"Rectangle","Ellipse","Arrow","Curved arrow","Pen","Text","Balloon","Step","Highlight","Blur","Pixelate","Magnify","Spotlight","Redact","Eraser","Image","Emoji","Cursor"}){
            var mark=new Mark{Kind=kind,X=60,Y=60,W=130,H=75,Radius=12,Text="Shadow",ShadowSize=10,ShadowIntensity=70};mark.Points.Add(new PointF(60,60));mark.Points.Add(new PointF(190,135));if(kind=="Image")using(var img=Blank(20,20))mark.Image=Art.Bytes(img);
            using(var bg=Blank(300,240))using(var plain=Art.Render(bg,new List<Mark>{mark})){mark.Shadow=true;using(var shaded=Art.Render(bg,new List<Mark>{mark})){bool changed=false;for(int y=0;y<240&&!changed;y++)for(int x=0;x<300;x++)if(plain.GetPixel(x,y)!=shaded.GetPixel(x,y)){changed=true;break;}check(changed,"Toggle shadow changes "+kind+" export");}}
        }
        Active=bubble;SyncStyle();shadowBox.Checked=true;shadowSize.Value=14;shadowIntensity.Value=55;check(bubble.Shadow&&bubble.ShadowSize==14&&bubble.ShadowIntensity==55,"Shadow size and intensity controls edit selected annotation");var snapshot=Snapshot();check(snapshot.Marks[0].TailTipX==bubble.TailTipX&&snapshot.Marks[0].Shadow,"Tail and shadow saved in editable snapshots");RefreshImage();Show();Application.DoEvents();UpdateInspector();FitInspectorControls();inspector.PerformLayout();Application.DoEvents();System.IO.File.WriteAllLines(System.IO.Path.Combine(folder,"inspector-layout.txt"),inspector.Controls.Cast<Control>().Select(c=>c.GetType().Name+" "+c.Bounds+" visible="+c.Visible+" "+String.Join("/",c.Controls.Cast<Control>().Select(v=>v.Text))));RenameLayer();layerNameEditor.Text="Rename directly here";using(var preview=new Bitmap(Width,Height)){DrawToBitmap(preview,new Rectangle(Point.Empty,preview.Size));preview.Save(System.IO.Path.Combine(folder,"shadow-interface.png"));}CloseInlineRename(true);check(bubble.Name=="Rename directly here"&&layerNameEditor.Parent==layers,"Layer rename remains inside the actual layer row");Hide();LoadBitmap(Blank(100,100),"Test");
    }
}
}
