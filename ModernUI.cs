using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ShotOut {
// Vector icons stay crisp and consistent without depending on a symbol font.
static class Glyph {
    public static void Draw(Graphics g,string name,Rectangle area,Color color) {
        var state=g.Save(); g.TranslateTransform(area.X,area.Y); g.ScaleTransform(area.Width/24f,area.Height/24f); g.SmoothingMode=SmoothingMode.AntiAlias;
        using(var p=new Pen(color,1.7f)) using(var brush=new SolidBrush(color)) {
            p.StartCap=p.EndCap=LineCap.Round; p.LineJoin=LineJoin.Round;
            switch(name) {
                case "Select": case "Cursor": g.DrawPolygon(p,new PointF[]{new PointF(5,3),new PointF(6,20),new PointF(10,15),new PointF(14,21),new PointF(17,19),new PointF(13,13),new PointF(20,12)}); break;
                case "Arrow": case "Double arrow": g.DrawLine(p,4,19,20,5); g.DrawLines(p,new Point[]{new Point(11,5),new Point(20,5),new Point(20,14)}); if(name=="Double arrow")g.DrawLines(p,new Point[]{new Point(4,10),new Point(4,19),new Point(13,19)}); break;
                case "Line": g.DrawLine(p,5,19,19,5); break;
                case "Rectangle": using(var path=Art.Round(new RectangleF(4,5,16,14),2))g.DrawPath(p,path); break;
                case "Ellipse": g.DrawEllipse(p,4,4,16,16); break;
                case "Pen": g.DrawLines(p,new Point[]{new Point(5,16),new Point(16,5),new Point(20,9),new Point(9,20),new Point(4,21),new Point(5,16)}); g.DrawLine(p,13,8,17,12); break;
                case "Text": g.DrawLine(p,5,5,19,5); g.DrawLine(p,12,5,12,20); g.DrawLine(p,8,20,16,20); break;
                case "Balloon": using(var path=Art.Round(new RectangleF(3,4,18,13),3))g.DrawPath(p,path); g.DrawLines(p,new Point[]{new Point(7,17),new Point(6,21),new Point(13,17)}); break;
                case "Step": g.DrawEllipse(p,3,3,18,18); g.DrawLines(p,new Point[]{new Point(10,9),new Point(12,7),new Point(12,17)}); g.DrawLine(p,9,17,15,17); break;
                case "Highlight": {var saved=g.Save();g.TranslateTransform(12,12);g.RotateTransform(45);g.TranslateTransform(-12,-12);using(var body=Art.Round(new RectangleF(8,3,8,12),1.4f))g.DrawPath(p,body);g.DrawLine(p,8.5f,6,15.5f,6);g.DrawPolygon(p,new[]{new PointF(8,15),new PointF(16,15),new PointF(14,17),new PointF(10,17)});g.FillPolygon(brush,new[]{new PointF(10,17),new PointF(14,17),new PointF(14,21),new PointF(10,19)});g.Restore(saved);g.DrawLine(p,3,22,12,22);break;}
                case "Redact": using(var path=Art.Round(new RectangleF(3,8,18,9),2))g.FillPath(brush,path); g.DrawLine(p,5,4,13,4); g.DrawLine(p,5,21,19,21); break;
                case "Pixelate": for(int y=4;y<21;y+=6)for(int x=4;x<21;x+=6)g.FillRectangle(brush,x,y,4,4); break;
                case "Blur": for(int y=5;y<21;y+=5)for(int x=4;x<22;x+=5) { using(var b=new SolidBrush(Color.FromArgb((x+y)%3==0?230:110,color)))g.FillEllipse(b,x,y,2.5f,2.5f); } break;
                case "Spotlight": {var saved=g.Save();g.TranslateTransform(12,12);g.RotateTransform(45);g.TranslateTransform(-12,-12);using(var body=Art.Round(new RectangleF(9,11,6,10),1.3f))g.DrawPath(p,body);g.DrawPolygon(p,new[]{new PointF(7,7),new PointF(17,7),new PointF(15,11),new PointF(9,11)});g.DrawLine(p,10.7f,14,13.3f,14);g.DrawLine(p,12,2,12,4);g.DrawLine(p,6.5f,3.5f,8,5);g.DrawLine(p,17.5f,3.5f,16,5);g.Restore(saved);break;}
                case "Project": g.DrawLines(p,new[]{new Point(5,2),new Point(15,2),new Point(20,7),new Point(20,22),new Point(5,22),new Point(5,2)});g.DrawLines(p,new[]{new Point(15,2),new Point(15,8),new Point(20,8)});g.DrawLine(p,9,13,16,13);g.DrawLine(p,9,17,16,17);break;
                case "Search": g.DrawEllipse(p,3,3,13,13);g.DrawLine(p,15,15,21,21);break;
                case "Magnify": g.DrawEllipse(p,3,3,13,13); g.DrawLine(p,15,15,21,21); g.DrawLine(p,7,9,12,9); g.DrawLine(p,9.5f,6.5f,9.5f,11.5f); break;
                case "Bucket": g.FillPolygon(brush,new[]{new Point(6,14),new Point(18,14),new Point(12,20)});g.DrawPolygon(p,new[]{new Point(4,12),new Point(12,4),new Point(20,12),new Point(12,20)});g.DrawLine(p,6,14,18,14);g.DrawLines(p,new[]{new Point(7,7),new Point(7,3),new Point(10,3),new Point(15,8)});g.FillEllipse(brush,19,17,4,5);break;
                case "Hand":g.DrawLines(p,new Point[]{new Point(5,12),new Point(5,8),new Point(7,7),new Point(9,8),new Point(9,12),new Point(9,4),new Point(11,3),new Point(13,4),new Point(13,11),new Point(13,5),new Point(15,4),new Point(17,5),new Point(17,12),new Point(17,8),new Point(19,7),new Point(21,9),new Point(21,16),new Point(18,21),new Point(9,21),new Point(3,15),new Point(3,12),new Point(5,12),new Point(9,16)});break;
                case "Eraser": g.DrawPolygon(p,new Point[]{new Point(3,15),new Point(13,4),new Point(21,11),new Point(13,20),new Point(8,20)}); g.DrawLine(p,8,10,16,17); g.DrawLine(p,13,21,22,21); break;
                case "Crop": g.DrawLines(p,new Point[]{new Point(7,3),new Point(7,17),new Point(21,17)}); g.DrawLines(p,new Point[]{new Point(3,7),new Point(17,7),new Point(17,21)}); break;
                case "Cut rows": case "Cut columns": g.DrawEllipse(p,3,14,5,5); g.DrawEllipse(p,3,4,5,5); g.DrawLine(p,8,7,21,20); g.DrawLine(p,8,16,21,3); break;
                case "Image": g.DrawRectangle(p,3,4,18,16); g.DrawEllipse(p,7,7,3,3); g.DrawLines(p,new Point[]{new Point(4,18),new Point(10,12),new Point(14,16),new Point(17,12),new Point(21,16)}); break;
                case "Eyedropper": g.DrawPolygon(p,new Point[]{new Point(4,20),new Point(5,16),new Point(14,7),new Point(18,11),new Point(9,20)});g.DrawLine(p,12,6,19,13);g.DrawPolygon(p,new Point[]{new Point(14,5),new Point(18,1),new Point(22,5),new Point(18,9)});break;
                case "Clone stamp": g.DrawEllipse(p,8,3,8,8);g.DrawLine(p,9,11,7,16);g.DrawLine(p,15,11,17,16);g.DrawRectangle(p,4,16,16,5); break;
                case "Grayscale": g.DrawEllipse(p,3,3,18,18);g.FillPie(brush,3,3,18,18,90,180); break;
                case "Visibility": g.DrawArc(p,2,5,20,14,10,160);g.DrawArc(p,2,5,20,14,190,160);g.DrawEllipse(p,9,9,6,6);break;
                case "Locked": case "Unlocked": g.DrawRectangle(p,5,10,14,11);if(name=="Locked")g.DrawArc(p,8,3,8,13,180,180);else g.DrawArc(p,12,2,8,13,180,180);g.DrawLine(p,12,14,12,17);break;
                case "Layers": g.DrawPolygon(p,new Point[]{new Point(3,8),new Point(12,3),new Point(21,8),new Point(12,13)});g.DrawLines(p,new Point[]{new Point(3,13),new Point(12,18),new Point(21,13)});g.DrawLines(p,new Point[]{new Point(3,17),new Point(12,22),new Point(21,17)});break;
                case "Emoji": g.DrawEllipse(p,3,3,18,18); g.FillEllipse(brush,7,8,2,2); g.FillEllipse(brush,15,8,2,2); g.DrawArc(p,7,9,10,8,15,150); break;
                case "Capture": g.DrawLines(p,new Point[]{new Point(3,9),new Point(3,4),new Point(8,4)}); g.DrawLines(p,new Point[]{new Point(16,4),new Point(21,4),new Point(21,9)}); g.DrawLines(p,new Point[]{new Point(21,15),new Point(21,20),new Point(16,20)}); g.DrawLines(p,new Point[]{new Point(8,20),new Point(3,20),new Point(3,15)}); g.DrawEllipse(p,9,9,6,6); break;
                case "Open": g.DrawLines(p,new Point[]{new Point(3,19),new Point(3,6),new Point(9,6),new Point(11,9),new Point(21,9),new Point(18,19),new Point(3,19),new Point(7,10),new Point(16,10)}); break;
                case "Paste": g.DrawRectangle(p,5,5,14,16); g.DrawRectangle(p,9,3,6,4); g.DrawLine(p,9,12,15,12); g.DrawLine(p,9,16,15,16); break;
                case "Copy": using(var path=Art.Round(new RectangleF(8,8,12,13),2))g.DrawPath(p,path); g.DrawLines(p,new Point[]{new Point(15,4),new Point(4,4),new Point(4,16)}); break;
                case "Save": g.DrawPolygon(p,new[]{new Point(4,3),new Point(17,3),new Point(21,7),new Point(21,21),new Point(4,21)});g.DrawRectangle(p,8,3,8,6);g.DrawRectangle(p,8,14,9,7);g.DrawLine(p,14,4,14,7);break;
                case "SaveAs":g.DrawLines(p,new[]{new Point(13,3),new Point(4,3),new Point(4,21),new Point(21,21),new Point(21,13)});g.DrawLines(p,new[]{new Point(12,12),new Point(21,3),new Point(15,3)});g.DrawLine(p,21,3,21,9);break;
                case "Undo": g.DrawLines(p,new Point[]{new Point(9,4),new Point(4,9),new Point(9,14)}); g.DrawArc(p,4,8,16,13,260,210); break;
                case "RotateLeft": case "RotateRight": if(name=="RotateRight"){g.TranslateTransform(24,0);g.ScaleTransform(-1,1);}g.DrawArc(p,5,5,15,15,210,290);g.DrawLines(p,new Point[]{new Point(3,4),new Point(3,10),new Point(9,10)});break;
                case "FlipHorizontal": case "FlipVertical": if(name=="FlipVertical"){g.TranslateTransform(24,0);g.RotateTransform(90);}p.DashStyle=DashStyle.Dash;g.DrawLine(p,12,2,12,22);p.DashStyle=DashStyle.Solid;g.DrawPolygon(p,new Point[]{new Point(3,6),new Point(9,12),new Point(3,18)});g.DrawPolygon(p,new Point[]{new Point(21,6),new Point(15,12),new Point(21,18)});break;
                case "Link": g.DrawArc(p,3,7,12,8,40,280);g.DrawArc(p,9,10,12,8,220,280);g.DrawLine(p,8,12,16,12);break;
                case "Redo": g.DrawLines(p,new Point[]{new Point(15,4),new Point(20,9),new Point(15,14)}); g.DrawArc(p,4,8,16,13,70,210); break;
                case "ChevronLeft":g.DrawLines(p,new[]{new Point(15,5),new Point(8,12),new Point(15,19)});break;
                case "ChevronRight":g.DrawLines(p,new[]{new Point(9,5),new Point(16,12),new Point(9,19)});break;
                case "Refresh":g.DrawArc(p,4,4,16,16,40,290);g.DrawLines(p,new[]{new Point(20,4),new Point(20,10),new Point(14,10)});break;
                case "Up": g.DrawLines(p,new Point[]{new Point(6,12),new Point(12,6),new Point(18,12)}); g.DrawLine(p,12,6,12,20); break;
                case "Down": g.DrawLines(p,new Point[]{new Point(6,12),new Point(12,18),new Point(18,12)}); g.DrawLine(p,12,4,12,18); break;
                case "Delete": g.DrawLine(p,5,6,19,6); g.DrawLine(p,9,3,15,3); g.DrawLines(p,new Point[]{new Point(7,9),new Point(8,21),new Point(16,21),new Point(17,9)}); g.DrawLine(p,11,10,11,17); g.DrawLine(p,14,10,14,17); break;
                case "Fit": g.DrawRectangle(p,6,7,12,10); g.DrawLines(p,new Point[]{new Point(3,8),new Point(3,3),new Point(8,3)}); g.DrawLines(p,new Point[]{new Point(16,21),new Point(21,21),new Point(21,16)}); break;
                case "AlignLeft": case "AlignCenter": case "AlignRight": for(int i=0;i<4;i++){float width=i%2==0?17:11;float x=name=="AlignLeft"?3:name=="AlignRight"?20-width:(24-width)/2;g.DrawLine(p,x,5+i*4,x+width,5+i*4);}break;
                case "Effects": g.DrawLines(p,new Point[]{new Point(12,4),new Point(3,4),new Point(3,21),new Point(21,21),new Point(21,14)});g.DrawLines(p,new Point[]{new Point(4,18),new Point(9,12),new Point(14,17),new Point(17,13),new Point(21,17)});g.DrawPolygon(p,new PointF[]{new PointF(18,1),new PointF(19.5f,5),new PointF(23,6.5f),new PointF(19.5f,8),new PointF(18,12),new PointF(16.5f,8),new PointF(13,6.5f),new PointF(16.5f,5)});break;
                case "Settings": var cog=new PointF[32];for(int tooth=0;tooth<8;tooth++)for(int corner=0;corner<4;corner++){double angle=(-90+tooth*45+new[]{0d,10d,25d,35d}[corner])*Math.PI/180;float radius=corner==1||corner==2?9.4f:7.4f;cog[tooth*4+corner]=new PointF(12+(float)Math.Cos(angle)*radius,12+(float)Math.Sin(angle)*radius);}g.DrawPolygon(p,cog);g.DrawEllipse(p,8.7f,8.7f,6.6f,6.6f);break;
                case "Adjust": g.DrawLine(p,5,3,5,21); g.DrawLine(p,12,3,12,21); g.DrawLine(p,19,3,19,21); g.FillRectangle(brush,2,7,6,4); g.FillRectangle(brush,9,14,6,4); g.FillRectangle(brush,16,6,6,4); break;
                case "More": for(int x=4;x<22;x+=8)g.FillEllipse(brush,x-1,11,2.5f,2.5f); break;
            }
        } g.Restore(state);
    }
}
class ModernButton : Button {
    public string IconName; public float UiScale=1f;
    public bool Selected, Swatch, ShowDropDown;
    bool hover, pressed;
    public ModernButton() { SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true); FlatStyle=FlatStyle.Flat; FlatAppearance.BorderSize=0; UseVisualStyleBackColor=false; }
    protected override void OnMouseEnter(EventArgs e) { hover=true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { hover=pressed=false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { pressed=true; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { pressed=false; Invalidate(); base.OnMouseUp(e); }
    protected override void OnPaint(PaintEventArgs e) {
        var g=e.Graphics; g.Clear(Parent==null?Color.FromArgb(32,34,39):Parent.BackColor); g.SmoothingMode=SmoothingMode.AntiAlias;
        Color bg=Swatch?Color.FromArgb(44,47,55):BackColor;
        if(Selected)bg=UiTheme.Accent; else if(hover)bg=ControlPaint.Light(bg,.12f); if(pressed)bg=ControlPaint.Dark(bg,.1f);
        using(var path=Art.Round(new RectangleF(0,0,Width-1,Height-1),8))using(var brush=new SolidBrush(bg))g.FillPath(brush,path);
        Color fg=Enabled?(Selected||BackColor.ToArgb()==UiTheme.Accent.ToArgb()?UiTheme.AccentInk:ForeColor):Color.FromArgb(95,101,113);
        int offset=0;
        if(Swatch) { int swatchX=String.IsNullOrEmpty(Text)?(Width-16)/2:12;using(var brush=new SolidBrush(BackColor))g.FillEllipse(brush,swatchX,(Height-16)/2,16,16); using(var p=new Pen(Color.FromArgb(130,143,158),1))g.DrawEllipse(p,swatchX,(Height-16)/2,16,16); offset=35; }
        else if(!String.IsNullOrEmpty(IconName)) { int icon=(int)Math.Round(20*UiScale),x=String.IsNullOrEmpty(Text)?(Width-icon)/2:(int)Math.Round(12*UiScale); Glyph.Draw(g,IconName,new Rectangle(x,(Height-icon)/2,icon,icon),fg); offset=(int)Math.Round(38*UiScale); }
        if(!String.IsNullOrEmpty(Text)) TextRenderer.DrawText(g,Text,Font,new Rectangle(offset,0,Width-offset-(ShowDropDown?(int)Math.Round(26*UiScale):0),Height),fg,(ShowDropDown?TextFormatFlags.NoPadding:0)|TextFormatFlags.VerticalCenter|(offset==0?TextFormatFlags.HorizontalCenter:TextFormatFlags.Left)|TextFormatFlags.EndEllipsis);
        if(ShowDropDown){int arrowWidth=(int)Math.Round(26*UiScale);TextRenderer.DrawText(g,"▾",Font,new Rectangle(Width-arrowWidth,0,arrowWidth,Height),fg,TextFormatFlags.VerticalCenter|TextFormatFlags.HorizontalCenter|TextFormatFlags.NoPadding);}
        if(Selected) using(var brush=new SolidBrush(UiTheme.AccentLight))g.FillRectangle(brush,0,Height/2-6,2,12);
    }
}
class DarkColors : ProfessionalColorTable {
    public override Color ToolStripDropDownBackground { get { return Color.FromArgb(38,41,48); } }
    public override Color ImageMarginGradientBegin { get { return ToolStripDropDownBackground; } }
    public override Color ImageMarginGradientMiddle { get { return ToolStripDropDownBackground; } }
    public override Color ImageMarginGradientEnd { get { return ToolStripDropDownBackground; } }
    public override Color MenuItemSelected { get { return Color.FromArgb(58,68,91); } }
    public override Color MenuItemBorder { get { return Color.FromArgb(78,91,120); } }
    public override Color MenuBorder { get { return Color.FromArgb(63,67,78); } }
}
class ChoiceButton : ModernButton {
    public readonly List<string> Items=new List<string>();
    string selected;
    public event EventHandler SelectedIndexChanged;
    public string SelectedItem { get { return selected; } set { if(selected==value)return; selected=value; Text=value==null?"":value+"   ▾"; if(SelectedIndexChanged!=null)SelectedIndexChanged(this,EventArgs.Empty); Invalidate(); } }
    protected override void OnClick(EventArgs e) { base.OnClick(e); var menu=new ModernContextMenu { BackColor=Color.FromArgb(38,41,48),ForeColor=ForeColor,Font=Font,Renderer=new ModernMenuRenderer(),ShowImageMargin=false }; foreach(string item in Items) { string value=item; var choice=menu.Items.Add(item); choice.Click+=delegate { SelectedItem=value; }; } menu.Show(this,new Point(0,Height+3)); }
}
class NumberField : UserControl {
    public float UiScale=1f;
    public decimal Minimum, Maximum=100;
    public bool AcceleratedWheel;int wheelRemainder;
    decimal current;
    TextBox edit;
    public event EventHandler ValueChanged;
    public decimal Value { get { return current; } set { decimal next=Math.Max(Minimum,Math.Min(Maximum,value)); bool changed=next!=current; current=next; if(edit!=null&&edit.Text!=current.ToString("0.##"))edit.Text=current.ToString("0.##"); if(changed&&ValueChanged!=null)ValueChanged(this,EventArgs.Empty); Invalidate(); } }
    public NumberField() {
        Height=34; SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true); BackColor=Color.FromArgb(43,46,54);
        edit=new ThemedTextBox { BorderStyle=BorderStyle.None,BackColor=BackColor,ForeColor=Color.FromArgb(233,236,243),Location=new Point(9,8),Width=34,Text="0",TextAlign=HorizontalAlignment.Left,Font=new Font("Segoe UI",13,FontStyle.Regular,GraphicsUnit.Pixel) }; Controls.Add(edit);ModernMenus.TextMenu(edit);
        edit.TextChanged+=delegate { decimal n; if(decimal.TryParse(edit.Text,out n)&&n>=Minimum&&n<=Maximum&&n!=current) { current=n; if(ValueChanged!=null)ValueChanged(this,EventArgs.Empty); } };
        edit.MouseWheel+=delegate(object sender,MouseEventArgs e){OnMouseWheel(e);};
        edit.Leave+=delegate { Value=current; }; edit.KeyDown+=delegate(object s,KeyEventArgs e) { if(e.KeyCode==Keys.Up||e.KeyCode==Keys.Down) { Value=current+(e.KeyCode==Keys.Up?1:-1); e.Handled=e.SuppressKeyPress=true; } };
        Resize+=delegate { edit.Width=Math.Max(15,Width-(int)(36*UiScale)); };
    }
    protected override void OnPaint(PaintEventArgs e) { e.Graphics.Clear(Parent==null?BackColor:Parent.BackColor); e.Graphics.SmoothingMode=SmoothingMode.AntiAlias; using(var path=Art.Round(new RectangleF(0,0,Width-1,Height-1),6))using(var b=new SolidBrush(BackColor))e.Graphics.FillPath(b,path); using(var p=new Pen(Color.FromArgb(164,175,196),1.3f)) { float x=Width-13*UiScale,y=Height/2f; e.Graphics.DrawLines(p,new PointF[]{new PointF(x-3*UiScale,y-5*UiScale),new PointF(x,y-8*UiScale),new PointF(x+3*UiScale,y-5*UiScale)}); e.Graphics.DrawLines(p,new PointF[]{new PointF(x-3*UiScale,y+5*UiScale),new PointF(x,y+8*UiScale),new PointF(x+3*UiScale,y+5*UiScale)}); } }
    protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); if(e.X>Width-25*UiScale)Value+=e.Y<Height/2?1:-1; else edit.Focus(); }
    protected override void OnMouseWheel(MouseEventArgs e) {var handled=e as HandledMouseEventArgs;if(handled!=null)handled.Handled=true;ApplyWheel(e.Delta);}
    internal void ApplyWheel(int delta){wheelRemainder+=delta;int ticks=wheelRemainder/120;wheelRemainder%=120;ticks=Math.Max(-100,Math.Min(100,ticks));decimal next=Value;for(int i=0;i<Math.Abs(ticks);i++){decimal step=AcceleratedWheel?Math.Max(2,Math.Round(next*.15m,0,MidpointRounding.AwayFromZero)):1;next=Math.Max(Minimum,Math.Min(Maximum,next+(ticks>0?step:-step)));}Value=next;}
    internal void TestArrow(bool up){OnMouseDown(new MouseEventArgs(MouseButtons.Left,1,Width-10,up?8:Height-8,0));}
}
class ThemeCheckBox : CheckBox {
    public float UiScale=1f;
    public ThemeCheckBox() { SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true); }
    protected override void OnPaint(PaintEventArgs e) {
        var g=e.Graphics; g.Clear(Parent==null?BackColor:Parent.BackColor); g.SmoothingMode=SmoothingMode.AntiAlias;
        if(Appearance==Appearance.Button) { using(var path=Art.Round(new RectangleF(0,0,Width-1,Height-1),6))using(var b=new SolidBrush(Checked?UiTheme.Accent:Color.FromArgb(43,46,54)))g.FillPath(b,path); TextRenderer.DrawText(g,Text,Font,ClientRectangle,Checked?UiTheme.AccentInk:ForeColor,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter); }
        else { var r=new RectangleF(UiScale,(Height-16*UiScale)/2,16*UiScale,16*UiScale); using(var path=Art.Round(r,4)) { using(var b=new SolidBrush(Checked?UiTheme.Accent:Color.FromArgb(43,46,54)))g.FillPath(b,path); using(var p=new Pen(Color.FromArgb(105,116,137),1))g.DrawPath(p,path); } if(Checked)using(var p=new Pen(Color.White,1.7f))g.DrawLines(p,new PointF[]{new PointF(5*UiScale,r.Y+8*UiScale),new PointF(8*UiScale,r.Y+11*UiScale),new PointF(13*UiScale,r.Y+5*UiScale)}); TextRenderer.DrawText(g,Text,Font,new Rectangle((int)(26*UiScale),0,Width-(int)(26*UiScale),Height),ForeColor,TextFormatFlags.VerticalCenter); }
    }
}
partial class Editor {
    ToolTip tips;
    NumberField cornerRadius;
    Label zoomLabel, inspectorHint, widthCaption;
    Panel cropActions;
    FlowLayoutPanel inspector;
    Control layerTitle, layerButtons;
    Control variantRow, strokeRow, fillRow, weightRow, radiusRow, textRow, formatRow, editRow;
    ChoiceButton variant;
    Button applyCrop;
    Dictionary<string,string> lastVariants=new Dictionary<string,string>();
    [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr h,int attribute,ref int value,int size);
    public static string ToolDisplayName(string name){return name=="Bucket"?"Bucket fill":name=="Balloon"?"Speech balloon":name;}
    static string Family(string name) { switch(name) { case "Rounded":return "Rectangle"; case "Line":case "Double arrow":case "Dashed arrow":case "Curved arrow":case "Dashed curved arrow":return "Arrow"; case "Blur":case "Pixelate":return "Redact";  case "Cut columns":return "Cut rows"; case "Emoji":case "Cursor":return "Image"; default:return name; } }
    static string DefaultKeyFor(string name) { switch(name) { case "Select":return "V";case "Arrow":case "Dashed arrow":case "Curved arrow":case "Dashed curved arrow":return "A";case "Double arrow":return "D";case "Line":return "L";case "Rectangle":case "Rounded":return "R";case "Ellipse":return "E";case "Pen":return "F";case "Text":return "T";case "Balloon":return "O";case "Step":return "N";case "Highlight":return "H";case "Redact":return "X";case "Blur":return "B";case "Pixelate":return "P";case "Spotlight":return "S";case "Magnify":return "M";case "Bucket":return "Q";case "Clone stamp":return "Z";case "Hand":return "Y";case "Eraser":return "W";case "Crop":return "C";case "Cut rows":return "U";case "Cut columns":return "G";case "Image":return "I";case "Emoji":return "J";case "Cursor":return "K"; default:return ""; } }
    static string KeyFor(string name) { var item=Preferences.Current.Tools.FirstOrDefault(b=>b.Action==name);return item==null?DefaultKeyFor(name):((Keys)item.Chord).ToString(); }
    void RememberVariant(string name) { lastVariants[Family(name)]=name; }
    void ChooseTool(string name) { string value; SetTool(lastVariants.TryGetValue(name,out value)?value:name); }
    ContextMenuStrip DarkMenu() { return new ModernContextMenu { Renderer=new ModernMenuRenderer(),BackColor=Color.FromArgb(38,41,48),ForeColor=text,Font=Font,ShowImageMargin=false,Padding=new Padding(6) }; }
    ModernButton ActionButton(string label,string icon,string tooltip,Action action,int width) {
        var b=new ModernButton { Text=label,IconName=icon,Width=width,Height=36,Margin=new Padding(3),BackColor=Color.FromArgb(36,39,45),ForeColor=text,Cursor=Cursors.Hand,TabStop=false,Font=Font,AccessibleName=tooltip };
        b.Click+=delegate { Guard(action); }; tips.SetToolTip(b,tooltip); return b;
    }
    Label Caption(string value,int width) { return new Label { Text=value,Width=width,Height=34,ForeColor=muted,TextAlign=ContentAlignment.MiddleLeft,Margin=new Padding(0),AutoEllipsis=true }; }
    FlowLayoutPanel Row() { return new ResizeBufferedFlowPanel { Width=217,Height=42,Margin=new Padding(0,0,0,5),WrapContents=false }; }
    NumberField Number(decimal min,decimal max,decimal value) { return new NumberField { Minimum=min,Maximum=max,Value=value,Width=72,Height=34,BackColor=Color.FromArgb(43,46,54),ForeColor=text,Margin=new Padding(0),Font=Font }; }
    void BuildModernUI() {
        panel=Color.FromArgb(30,32,38); BackColor=panel; text=Color.FromArgb(233,236,243); muted=Color.FromArgb(149,157,174);
        tips=new ToolTip { InitialDelay=350,ReshowDelay=100,AutoPopDelay=6000,BackColor=Color.FromArgb(48,52,62),ForeColor=text };
        Disposed+=delegate { tips.Dispose(); };
        var header=new ResizeBufferedPanel { Dock=DockStyle.Top,Height=56,Padding=new Padding(12,6,16,6),BackColor=Color.FromArgb(38,40,47) };
        StyleTopToolbar(header);
        var brand=new ResizeBufferedPanel {Dock=DockStyle.Left,Width=44};brandLogo=new PictureBox {Size=new Size(30,30),SizeMode=PictureBoxSizeMode.Zoom,AccessibleName="ShotOut logo"};brand.Controls.Add(brandLogo);brand.Layout+=delegate {AlignHeaderLogo();};

        var primary=new ResizeBufferedFlowPanel { Dock=DockStyle.Left,Width=650,WrapContents=false,Padding=new Padding(0,1,0,0) };
        primary.Controls.Add(CreateCaptureSplit());
        primary.Controls.Add(ToolbarDivider());
        primary.Controls.Add(CreateFileMenuButton());
        primary.Controls.Add(ToolbarDivider());
        primary.Controls.Add(CreateHistoryButton(false));
        primary.Controls.Add(CreateHistoryButton(true));
        var output=new ResizeBufferedFlowPanel { Dock=DockStyle.Right,Width=46,WrapContents=false,Padding=new Padding(0,1,0,0) };
        output.Controls.Add(ActionButton("","Settings","Settings",ShowSettings,36));
        primary.Controls.Add(ToolbarDivider());
        primary.Controls.Add(ActionButton("Effects","Effects","Image effects",ImageMenu,100));
        primary.Controls.Add(ToolbarDivider());
        primary.Controls.Add(ActionButton("Save","Save","Save image or current project (Ctrl+S)",delegate{SaveImage();},90));
        primary.Controls.Add(ToolbarDivider());
        primary.Controls.Add(CreateExportMenuButton());
        
        header.Controls.Add(primary); header.Controls.Add(brand); header.Controls.Add(output);InstallIntegratedChrome(header);
        var documentBar=new ResizeBufferedPanel { Dock=DockStyle.Top,Height=47,Padding=new Padding(18,5,15,5),BackColor=Color.FromArgb(30,32,38) };editingDocumentBar=documentBar;
        documentLabel=new Label { Dock=DockStyle.Left,Width=280,ForeColor=muted,TextAlign=ContentAlignment.MiddleLeft,AutoEllipsis=true };
        var zoom=new ResizeBufferedFlowPanel { Dock=DockStyle.Right,Width=170,WrapContents=false }; zoomLabel=Caption("100%",56); zoomLabel.TextAlign=ContentAlignment.MiddleCenter; zoom.Controls.Add(zoomLabel); zoom.Controls.Add(ActionButton("","Fit","Fit image (0)",delegate { View.Fit(); UpdateStatus(); },34)); zoom.Controls.Add(ActionButton("1:1",null,"Actual pixels",delegate { View.Zoom=1; View.PanX=View.PanY=0; View.Invalidate(); UpdateStatus(); },50));
        cropActions=new ResizeBufferedPanel { Dock=DockStyle.Right,Width=485,Visible=false }; var cropButtons=new ResizeBufferedFlowPanel { Dock=DockStyle.Fill,WrapContents=false }; applyCrop=ActionButton("Apply crop",null,"Apply selection (Enter)",CommitCrop,118); applyCrop.BackColor=UiTheme.Accent; InstallCropRatio(cropButtons); cropButtons.Controls.Add(applyCrop); cropButtons.Controls.Add(ActionButton("Cancel",null,"Cancel selection (Esc)",delegate { CancelCut(); SetTool("Select"); },82)); cropActions.Controls.Add(cropButtons);
        InstallSaveHint(documentBar);documentBar.Controls.Add(cropActions); documentBar.Controls.Add(zoom); documentBar.Controls.Add(documentLabel);
        toolRail=new ResizeBufferedFlowPanel { Dock=DockStyle.Left,Width=68,Padding=new Padding(12,10,7,8),BackColor=Color.FromArgb(26,28,33),FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true };
        var rail=toolRail;
        string[][] groups={new[]{"Select","Hand"},new[]{"Arrow","Rectangle","Ellipse","Pen"},new[]{"Text","Balloon","Step","Highlight"},new[]{"Redact","Spotlight","Magnify","Eraser","Bucket","Clone stamp"},new[]{"Crop","Cut rows","Image"}};
        foreach(var group in groups) { if(rail.Controls.Count>0) { var line=new ResizeBufferedPanel { Width=29,Height=1,BackColor=Color.FromArgb(54,58,68),Margin=new Padding(7,8,0,8) }; line.Tag="separator";rail.Controls.Add(line); } foreach(string name in group) { string n=name; string label=n=="Bucket"?"Bucket fill":n=="Cut rows"?"Cut out":n=="Redact"?"Redact / blur / pixelate":n=="Image"?"Insert image":ToolDisplayName(n); var b=ActionButton("",n,label+" ("+KeyFor(n)+")",delegate { if(n=="Image") InsertOptions(); else ChooseTool(n); },40); b.Height=32; b.Margin=new Padding(0,1,0,1); tools[n]=b; rail.Controls.Add(b);if(n==group.Last())b.Tag="group-end"; } }
        WireToolRail();
        var right=new ResizeBufferedPanel { Dock=DockStyle.Right,Width=268,Padding=new Padding(17,12,17,10),BackColor=Color.FromArgb(30,32,38) };
        inspector=new VerticalInspector { Dock=DockStyle.Top,Height=350,FlowDirection=FlowDirection.TopDown,WrapContents=false,Margin=new Padding(0) };
        toolTitle=new Label { Width=217,Height=36,Font=new Font("Segoe UI",16,FontStyle.Bold,GraphicsUnit.Pixel),ForeColor=text,Margin=new Padding(0) }; inspector.Controls.Add(toolTitle);
        var vr=Row(); vr.Controls.Add(Caption("Style",82)); variant=new ArrowChoiceButton { Width=130,Height=34,BackColor=Color.FromArgb(43,46,54),ForeColor=text,Margin=new Padding(0),Font=Font,Cursor=Cursors.Hand,TabStop=false,AccessibleName="Tool style" }; variant.SelectedIndexChanged+=delegate { if(!syncing&&variant.SelectedItem!=null)ChangeVariant(variant.SelectedItem); }; vr.Controls.Add(variant); variantRow=vr; inspector.Controls.Add(vr);
        colorButton=ActionButton("Stroke color",null,"Choose stroke or text color",delegate { PickColor(false); },210); ((ModernButton)colorButton).Swatch=true; colorButton.BackColor=ink; var sr=Row(); sr.Controls.Add(colorButton); strokeRow=sr; inspector.Controls.Add(sr);
        var fr=Row(); fillBox=new ThemeCheckBox { Text="Fill",ForeColor=muted,Width=70,Height=36,Margin=new Padding(0) }; fillBox.CheckedChanged+=delegate { ChangeStyle(); UpdateInspector(); }; fr.Controls.Add(fillBox); fillButton=ActionButton("Fill color",null,"Choose fill color",delegate { PickColor(true); },135); ((ModernButton)fillButton).Swatch=true; fillButton.BackColor=fill; fr.Controls.Add(fillButton); fillRow=fr; inspector.Controls.Add(fr);
        var wr=Row(); widthCaption=Caption("Stroke",145); wr.Controls.Add(widthCaption); thickness=Number(1,80,4);thickness.AcceleratedWheel=true;tips.SetToolTip(thickness,"Mouse wheel: faster thickness changes · Arrow buttons: 1 px"); thickness.ValueChanged+=delegate { ChangeStrokeWidth(); }; wr.Controls.Add(thickness); weightRow=wr; inspector.Controls.Add(wr);
        var rr=Row(); rr.Width=108;rr.Margin=Padding.Empty;rr.Padding=new Padding(4,0,0,0);rr.Controls.Add(Caption("Radius",48)); cornerRadius=Number(0,500,0);cornerRadius.Width=56;tips.SetToolTip(cornerRadius,"Corner radius in pixels"); cornerRadius.ValueChanged+=delegate { ChangeStyle(); }; rr.Controls.Add(cornerRadius); radiusRow=rr; wr.Controls.Add(rr); tailRow=Row(); tailBox=new ThemeCheckBox { Text="Speech bubble tail",Checked=true,Width=225,Height=34,ForeColor=text }; tailBox.CheckedChanged+=delegate { ChangeStyle(); }; tailRow.Controls.Add(tailBox); inspector.Controls.Add(tailRow);
        var tr=Row(); tr.Controls.Add(Caption("Font size · pt",145)); fontSize=Number(6,500,16.5m);fontSize.AcceleratedWheel=true;tips.SetToolTip(fontSize,"Mouse wheel: about 15% per notch · Arrow buttons: 1 pt"); fontSize.ValueChanged+=delegate { ChangeTextSize(); }; tr.Controls.Add(fontSize); textRow=tr; inspector.Controls.Add(tr);
        var fmt=Row(); boldBox=new ThemeCheckBox { Text="B",Checked=true,Width=62,Height=32,Appearance=Appearance.Button,FlatStyle=FlatStyle.Flat,TextAlign=ContentAlignment.MiddleCenter,BackColor=Color.FromArgb(43,46,54),Margin=new Padding(0,0,7,0) }; italicBox=new ThemeCheckBox { Text="I",Width=62,Height=32,Appearance=Appearance.Button,FlatStyle=FlatStyle.Flat,TextAlign=ContentAlignment.MiddleCenter,BackColor=Color.FromArgb(43,46,54),Margin=new Padding(0) }; underlineBox=new ThemeCheckBox { Text="U",Width=80,Height=32,Appearance=Appearance.Button,FlatStyle=FlatStyle.Flat,TextAlign=ContentAlignment.MiddleCenter,BackColor=Color.FromArgb(43,46,54),Margin=new Padding(0) }; foreach(var c in new[]{boldBox,italicBox,underlineBox}) { c.FlatAppearance.BorderSize=0; c.FlatAppearance.CheckedBackColor=UiTheme.Accent; c.CheckedChanged+=delegate { if(!HandleInlineFormat(c))ChangeStyle(); }; fmt.Controls.Add(c); } formatRow=fmt; inspector.Controls.Add(fmt);StyleFormatButtons();
        var er=Row(); er.Controls.Add(ActionButton("Edit text", "Text","Edit selected text",EditText,210)); editRow=er; inspector.Controls.Add(er); AddAlignmentInspector();AddTextWorkflowInspector();AddGeometryInspector(); AddShadowInspector();AddAdjustmentInspector();AddCloneInspector();AddDocumentationInspector();AddFontInspector();
        AddInspectorSections();PolishPropertySections();
        inspectorHint=new Label { Width=213,Height=70,ForeColor=muted,Margin=new Padding(0,9,0,6) }; inspector.Controls.Add(inspectorHint);
        layerTitle=new Label { Text="Layers",Dock=DockStyle.Top,Height=35,ForeColor=text,Font=new Font("Segoe UI",13,FontStyle.Bold,GraphicsUnit.Pixel),Padding=new Padding(0,10,0,0) };
        var layerButtonFlow=new ResizeBufferedFlowPanel { Dock=DockStyle.Bottom,Height=42,WrapContents=false }; layerButtons=layerButtonFlow; layerButtons.Controls.Add(ActionButton("","Up","Bring forward (Page Up)",delegate { Reorder(1); },48)); layerButtons.Controls.Add(ActionButton("","Down","Send backward (Page Down)",delegate { Reorder(-1); },48)); layerButtons.Controls.Add(ActionButton("","Copy","Duplicate layer (Ctrl+D)",Duplicate,48)); layerButtons.Controls.Add(ActionButton("","Delete","Delete layer (Delete)",Delete,48));
        layers=new BufferedLayerList {OwnerEditor=this, Dock=DockStyle.Fill,BackColor=right.BackColor,ForeColor=text,BorderStyle=BorderStyle.None,SelectionMode=SelectionMode.MultiExtended,ItemHeight=44,DrawMode=DrawMode.OwnerDrawFixed,IntegralHeight=false }; layers.DrawItem+=DrawLayer;layers.KeyDown+=delegate(object sender,KeyEventArgs e){if(TryNudgeSelection(e.KeyData))e.Handled=e.SuppressKeyPress=true;};
        layers.SelectedIndexChanged+=delegate { if(syncing)return;FinishStrokeAdjustment();selectedEffectRow=layers.SelectedItems.OfType<ImageEffectRow>().LastOrDefault(); Selected.Clear();foreach(Mark mark in layers.SelectedItems)if(Marks.Contains(mark))Selected.Add(mark);Active=Selected.LastOrDefault();if(Active!=null&&(Active.Kind=="Image"||Active.IsPresentation))tool="Select"; SyncStyle(); UpdateInspector(); View.Invalidate(); }; layers.MouseDoubleClick+=delegate(object sender,MouseEventArgs e) {int index=layers.IndexFromPoint(e.Location);if(e.Button==MouseButtons.Left&&index>=0&&e.X<layers.ClientSize.Width-UiPixels(30)){Active=(Mark)layers.Items[index];RenameLayer();}}; WireLayers();
        right.Controls.Add(layers); right.Controls.Add(layerButtons); right.Controls.Add(layerTitle); right.Controls.Add(inspector);
        status=new Label { Dock=DockStyle.Bottom,Height=29,BackColor=Color.FromArgb(26,28,33),ForeColor=muted,Padding=new Padding(18,6,0,0) };
        View=new Canvas(this); View.BackColor=Color.FromArgb(22,24,29);
        Controls.Add(View); Controls.Add(right); Controls.Add(rail); Controls.Add(documentBar); InitializeDocuments(); Controls.Add(header); Controls.Add(status);
        // Capture settings live in the capture menu instead of the editing surface.
        delay=Number(0,15,0); cursorBox=new ThemeCheckBox { Text="Include cursor",Width=190,Height=30,ForeColor=text,BackColor=panel };
        SetTool("Arrow"); BuildWelcome();InitializeUiSize(header,primary,documentBar);
        HandleCreated+=delegate { try { int enabled=1; DwmSetWindowAttribute(Handle,20,ref enabled,4);int rounded=2;DwmSetWindowAttribute(Handle,33,ref rounded,4); } catch(DllNotFoundException) { } catch(EntryPointNotFoundException) { } };
    }
    void CaptureOptions() {
        var m=DarkMenu(); m.Items.Add("Capture region  ("+CaptureLabel("Region")+")",null,delegate { Capture("Region"); }); m.Items.Add("Capture all screens  ("+CaptureLabel("Screen")+")",null,delegate { Capture("Screen"); }); m.Items.Add("Capture window  ("+CaptureLabel("Window")+")",null,delegate { Capture("Window"); }); m.Items.Add(new ToolStripSeparator());
        var settings=new ResizeBufferedFlowPanel {Width=274,Height=46,WrapContents=false,Padding=new Padding(12,6,10,6),Margin=Padding.Empty,BackColor=m.BackColor,ForeColor=m.ForeColor,Font=m.Font};
        var caption=new Label {Text="Delay · seconds",Width=154,Height=34,TextAlign=ContentAlignment.MiddleLeft,Margin=Padding.Empty,ForeColor=m.ForeColor,BackColor=m.BackColor,Font=m.Font};settings.Controls.Add(caption);delay.BackColor=Color.FromArgb(43,46,54);delay.ForeColor=m.ForeColor;delay.Font=m.Font;delay.Margin=Padding.Empty;settings.Controls.Add(delay);
        var host=new ToolStripControlHost(settings){AutoSize=false,Size=settings.Size,Margin=Padding.Empty,Padding=Padding.Empty,BackColor=m.BackColor};m.Items.Add(host);m.Closed+=delegate{settings.Controls.Remove(delay);};m.Show(captureSplit,new Point(0,captureSplit.Height+4));
    }
    void InsertOptions() { var m=DarkMenu(); m.Items.Add("Image  (I)",null,delegate { Guard(InsertImage); }); m.Items.Add("Emoji  (J)",null,delegate { SetTool("Emoji"); }); m.Items.Add("Cursor  (K)",null,delegate { SetTool("Cursor"); }); m.Show(Cursor.Position); }
    void ChangeVariant(string name) {if(new[]{"Classic","Double","Modern","Basic","Line"}.Contains(name)){ChangeArrowStyle(name);return;}
        string k=name=="Both ends"?"Double arrow":name=="Plain line"?"Line":name=="Speech balloon"?"Balloon":name=="Horizontal"?"Cut rows":name=="Vertical"?"Cut columns":name=="Solid block"?"Redact":name;
        if(Active!=null&&Family(Active.Kind)==Family(k)) { Push(); bool wasCurved=IsCurved(Active);Active.ArrowCurved=wasCurved; if(k.IndexOf("Curved",StringComparison.OrdinalIgnoreCase)>=0&&!IsCurved(Active))InitCurve(Active); Active.Kind=k; if(tool!="Select")tool=k; RememberVariant(k); RefreshImage(); } else SetTool(k);
        View.Focus();
    }
    void DrawLayer(object sender,DrawItemEventArgs e) {
        if(e.Index<0)return;if(DrawImageEffectRow(e))return; var m=(Mark)layers.Items[e.Index]; bool selected=(e.State&DrawItemState.Selected)!=0;bool hovered=e.Index==layerHover;
        using(var bg=new SolidBrush(layers.BackColor))e.Graphics.FillRectangle(bg,e.Bounds); if(selected||hovered) using(var path=Art.Round(new RectangleF(e.Bounds.X,e.Bounds.Y+UiPixels(2),e.Bounds.Width-3,e.Bounds.Height-4),7))using(var b=new SolidBrush(selected?UiTheme.SelectionSurface:Color.FromArgb(39,44,55)))e.Graphics.FillPath(b,path);
        DrawLayerInsertion(e);
        bool image=m.Kind=="Image";if(image)Glyph.Draw(e.Graphics,ImageMovementIsLocked(m)?"Locked":"Unlocked",new Rectangle(e.Bounds.X+UiPixels(5),e.Bounds.Y+UiPixels(15),UiPixels(14),UiPixels(14)),ImageMovementIsLocked(m)?Color.FromArgb(181,197,222):muted);
        Glyph.Draw(e.Graphics,Family(m.Kind)=="Rectangle"?"Rectangle":m.Kind,new Rectangle(e.Bounds.X+UiPixels(image?25:10),e.Bounds.Y+UiPixels(12),UiPixels(18),UiPixels(18)),m.Visible?Color.FromArgb(182,195,222):muted);
        DrawEye(e.Graphics,new Rectangle(e.Bounds.Right-UiPixels(23),e.Bounds.Y+UiPixels(15),UiPixels(15),UiPixels(14)),m.Visible);
        string name=m.Kind=="Rounded"?"Rectangle":ToolDisplayName(m.Kind); if(m.Text.Length>0)name=m.Text.Replace('\n',' '); if(!String.IsNullOrEmpty(m.Name))name=m.Name;if(!String.IsNullOrEmpty(m.LinkId))name+=" · linked";
        TextRenderer.DrawText(e.Graphics,name,layers.Font,new Rectangle(e.Bounds.X+UiPixels(image?50:36),e.Bounds.Y,e.Bounds.Width-UiPixels(image?82:68),e.Bounds.Height),m.Visible?text:muted,TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
    }
    public void UpdateInspector() {if(effectsPane!=null&&effectsPane.Visible&&imageAdjustmentTarget!=null&&!Marks.Contains(imageAdjustmentTarget)){CloseImageEffects();return;}
        if(inspector==null||cornerRadius==null||(effectsPane!=null&&effectsPane.Visible))return; bool was=syncing; syncing=true;inspector.Parent.SuspendLayout();inspector.SuspendLayout();try{
        string k=Active!=null?Active.Kind:tool, family=Family(k); bool shape=family=="Rectangle"||family=="Ellipse", isText=family=="Text"||k=="Balloon"||k=="Emoji", isLine=family=="Arrow", privacy=family=="Redact", cut=family=="Cut rows", crop=tool=="Crop"||tool.StartsWith("Cut ");
        toolTitle.Text=k=="Bucket"?"Bucket fill":Active!=null&&Active.IsPresentation?"Presentation":k=="Rounded"?"Rectangle":cut?"Cut out":privacy?"Redact":k=="Select"?"Select a layer":ToolDisplayName(k);
        string[] modes=isLine?new[]{"Classic","Double","Modern","Basic","Line","Dashed arrow"}:privacy?new[]{"Solid block","Blur","Pixelate"}:new string[0];
        string selected=k=="Double arrow"?"Both ends":k=="Line"?"Plain line":k=="Balloon"?"Speech balloon":k=="Cut rows"?"Horizontal":k=="Cut columns"?"Vertical":k=="Redact"?"Solid block":k;
        if(!variant.Items.SequenceEqual(modes)) { variant.Items.Clear(); variant.Items.AddRange(modes); } variant.SelectedItem=isLine?(k=="Double arrow"?"Double":k=="Line"?"Line":k.IndexOf("Dashed",StringComparison.OrdinalIgnoreCase)>=0?"Dashed arrow":Active!=null?Active.ArrowStyle:defaultArrowStyle):selected;
        variantRow.Visible=modes.Length>0; strokeRow.Visible=shape||isLine||(isText&&k!="Emoji")||k=="Pen"||k=="Step"||k=="Highlight"||k=="Magnify"||k=="Bucket";
        fillOpacityRow.Visible=StandaloneOpacity(k);UpdateAdjustmentInspector(k);
        colorButton.Text=k=="Bucket"?"Fill color":isText?"Text color":"Stroke color";
        fillRow.Visible=shape||k=="Balloon"; fillButton.Enabled=true;
        weightRow.Visible=shape||isLine||k=="Pen"||k=="Balloon"||k=="Blur"||k=="Pixelate"||k=="Magnify";
        thickness.Minimum=OptionalOutline(k)?0:1; RefreshUnselectedStroke(k); thickness.Value=thickness.Value;
        widthCaption.Text=k=="Blur"||k=="Pixelate"?"Strength":family=="Rectangle"||k=="Balloon"?"Stroke":"Stroke · px";
        alignmentRow.Visible=(family=="Text"||k=="Balloon")&&Preferences.Current.TextOptionsExpanded;UpdateTextWorkflowInspector(k);
        radiusRow.Visible=(family=="Rectangle"||k=="Balloon"); tailRow.Visible=k=="Balloon"; textRow.Visible=isText; formatRow.Visible=isText&&k!="Emoji"; editRow.Visible=Active!=null&&(isText||k=="Step");
        fillOpacityRow.Controls[0].Text=k=="Grayscale"?"Effect opacity · %":"Fill opacity · %";
        inspectorHint.Text=k=="Grayscale"?"Affects layers below. Reorder to choose what changes. Drag handles to limit its area.":k=="Hand"?"Drag the canvas to pan.\nMiddle mouse dragging also works.":k=="Bucket"?"Click a connected area to fill.\nTolerance controls color matching.\nFill stays on its own layer.":k=="Magnify"?"Drag the enlarged view to move it.\nAmber source point changes the detail.":k=="Balloon"?"Drag the amber point to move the tail.\nShadow controls apply to this annotation.":cut?"Drag sideways to cut columns.\nDrag up/down to cut rows. Release to apply.":crop?"Drag the edges inward to crop.\nDrag inside to move · Enter to apply.":k=="Select"?"Click a mark to select it.\nDrag empty space to select a group.\nDelete removes · Ctrl+D duplicates.":k=="Rectangle"||k=="Rounded"?"Drag to draw a rectangle.\nHold Shift for a square.\nAdjust rounding here anytime.":isLine?"Drag the shaft to move. Amber bends.\nEndpoints aim; blue handle rotates.":privacy?"Drag over the area to hide.\nChoose solid, blur, or pixelate above.":k=="Eraser"?"Drag to cover a detail using a nearby sampled color.":k=="Image"&&Active!=null?"Drag to align with center, thirds, or image edges. Hold Alt to move freely.":k=="Image"?"Insert an image, emoji, or cursor.\nPaste an image with Ctrl+V.":"Click or drag on the canvas.\n"+(KeyFor(k).Length>0?KeyFor(k)+" to activate · V to select.":"");
        int height=36+85; foreach(bool show in new[]{modes.Length>0,shape||isLine||isText||k=="Pen"||k=="Step"||k=="Highlight"||k=="Magnify"||k=="Bucket",shape||k=="Balloon",shape||isLine||k=="Pen"||k=="Balloon"||k=="Blur"||k=="Pixelate"||k=="Magnify",k=="Balloon",isText,isText&&k!="Emoji",Active!=null&&(isText||k=="Step")})if(show)height+=47;
        if((family=="Text"||k=="Balloon")&&Preferences.Current.TextOptionsExpanded)height+=94;if(k=="Step")height+=94;
        if(StandaloneOpacity(k))height+=47;height+=UpdateCloneInspector(k);height+=UpdateDocumentationInspector(k);fontRow.Visible=isText&&k!="Emoji"&&Preferences.Current.TextOptionsExpanded;if(isText&&k!="Emoji"&&Preferences.Current.TextOptionsExpanded)height+=47;if(k=="Grayscale")height+=45;
        if(k=="Emoji")inspectorHint.Text="Click the screenshot to choose an emoji in the Windows picker. Win + . reopens it.";
        UpdatePresentationInspector();height+=PropertySectionSpacing(k);height+=InspectorSectionHeight(k);height+=GeometryInspectorHeight();height+=ShadowInspectorHeight(k);inspector.Height=Math.Min(UiPixels(height),Math.Max(UiPixels(150),inspector.Parent.ClientSize.Height-UiPixels(145)));inspector.AutoScroll=true; cropActions.Visible=tool=="Crop"; cropRatio.Visible=tool=="Crop"; cropActions.Width=UiPixels(tool=="Crop"?485:236); applyCrop.Text="Apply crop";
        foreach(var kv in tools) { var b=(ModernButton)kv.Value; b.Selected=Family(tool)==kv.Key; string current; b.IconName=lastVariants.TryGetValue(kv.Key,out current)?current:kv.Key; if(Art.IsArrow(b.IconName)&&b.IconName!="Line"&&b.IconName!="Double arrow")b.IconName="Arrow"; string label=b.IconName=="Bucket"?"Bucket fill":b.IconName=="Cut rows"?"Cut out":b.IconName=="Image"?"Insert image":ToolDisplayName(b.IconName); tips.SetToolTip(b,label+" ("+KeyFor(b.IconName)+")"); b.Invalidate(); }
        }finally{syncing=was;inspector.ResumeLayout(true);inspector.Parent.ResumeLayout(true);}
    }
    void BuildWelcome() {
        welcome=new ResizeBufferedPanel { Dock=DockStyle.Fill,BackColor=View.BackColor }; var card=new ResizeBufferedFlowPanel { Size=new Size(570,300),FlowDirection=FlowDirection.TopDown,WrapContents=false };
        card.Controls.Add(new Label { Text="A little clarity goes a long way.",Font=new Font("Segoe UI",28,FontStyle.Bold,GraphicsUnit.Pixel),ForeColor=text,Width=560,Height=65,TextAlign=ContentAlignment.MiddleCenter });
        card.Controls.Add(new Label { Text="Capture a moment. Mark what matters. Share it anywhere.",ForeColor=muted,Width=560,Height=46,TextAlign=ContentAlignment.MiddleCenter });
        var actions=new ResizeBufferedFlowPanel { Width=560,Height=54,Padding=new Padding(25,4,0,0) }; var cap=ActionButton("Capture region","Capture","Capture region ("+CaptureLabel("Region")+")",delegate { Capture("Region"); },176); cap.BackColor=UiTheme.Accent; actions.Controls.Add(cap); actions.Controls.Add(ActionButton("Open image","Open","Open image (Ctrl+O)",Open,154)); actions.Controls.Add(ActionButton("Paste image","Paste","Paste image (Ctrl+V)",Paste,154)); card.Controls.Add(actions);
        card.Controls.Add(new Label { Text="Ctrl+Shift+2 to capture  ·  Hover any tool to learn its shortcut",ForeColor=muted,Width=560,Height=45,TextAlign=ContentAlignment.MiddleCenter });
        var blank=ActionButton("Start with a blank canvas",null,"Start drawing on a blank canvas",ShowBlankCanvasPresets,220); blank.Margin=new Padding(170,10,0,0); card.Controls.Add(blank);
        InstallScreenshotBrowser(card); View.Controls.Add(welcome);ApplyWorkspaceVisibility();
    }
    public void VerifyModernUI(List<string> report,string folder) {
        Action<bool,string> verify=delegate(bool ok,string label) { if(!ok)throw new Exception("FAILED: "+label); report.Add("PASS "+label); };
        verify(!tools.ContainsKey("Rounded")&&!tools.ContainsKey("Double arrow")&&!tools.ContainsKey("Pixelate"),"Related tools consolidated in rail");
        SetTool("Rectangle"); verify(!cropActions.Visible&&radiusRow.Visible,"Rectangle has rounding, no crop actions");
        var m=Marks.First(x=>x.Kind=="Rounded"||x.Kind=="Rectangle"); Active=m; SyncStyle(); float oldRadius=m.Kind=="Rounded"?12:m.Radius; cornerRadius.Value=24;
        verify(m.Kind=="Rectangle"&&m.Radius==24,"Radius control edits an existing rectangle"); Undo(); verify(Marks.Any(x=>x.Kind=="Rounded"||x.Radius==oldRadius),"Rounding edit can be undone");
        SetTool("Text"); verify(textRow.Visible&&!radiusRow.Visible&&!cropActions.Visible,"Text shows contextual options only");
        SetTool("Blur"); verify(variant.SelectedItem=="Blur"&&weightRow.Visible&&!strokeRow.Visible,"Privacy shortcut selects grouped blur options");
        SetTool("Arrow"); variant.SelectedItem="Double"; verify(tool=="Double arrow"&&((ModernButton)tools["Arrow"]).IconName=="Double arrow","Arrow style updates active tool and icon");
        SetTool("Crop"); verify(cropActions.Visible&&applyCrop.Text=="Apply crop","Crop controls appear only in crop mode");
        using(var preview=new Bitmap(Width,Height)) { DrawToBitmap(preview,new Rectangle(Point.Empty,preview.Size)); preview.Save(System.IO.Path.Combine(folder,"crop-interface.png")); }
        SetTool("Pen"); verify(!cropActions.Visible&&!radiusRow.Visible&&!textRow.Visible,"Leaving crop hides its controls");
        SetTool("Select"); Active=Marks.First(x=>x.Kind=="Rounded"||x.Kind=="Rectangle"); SyncStyle(); SyncLayers();
        using(var preview=new Bitmap(Width,Height)) { DrawToBitmap(preview,new Rectangle(Point.Empty,preview.Size)); preview.Save(System.IO.Path.Combine(folder,"rectangle-interface.png")); }
        SetTool("Arrow");
    }
}
}

