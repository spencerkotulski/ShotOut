using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
namespace ShotOut {
class OpacitySlider : Control {
    internal float UiScale=1f;
    public decimal Value=100;
    public event EventHandler ValueChanged;
    public OpacitySlider(){DoubleBuffered=true;TabStop=true;Cursor=Cursors.Hand;AccessibleName="Color opacity slider";}
    void SetValue(decimal value){value=Math.Max(0,Math.Min(100,value));if(value==Value)return;Value=value;Invalidate();if(ValueChanged!=null)ValueChanged(this,EventArgs.Empty);}
    protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button==MouseButtons.Left){Focus();Capture=true;SetValue(Math.Round((decimal)(e.X-7*UiScale)*100/(decimal)Math.Max(1,Width-14*UiScale)));}}
    protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(Capture)SetValue(Math.Round((decimal)(e.X-7*UiScale)*100/(decimal)Math.Max(1,Width-14*UiScale)));}
    protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);Capture=false;}
    protected override bool IsInputKey(Keys key){return (key&Keys.KeyCode)==Keys.Left||(key&Keys.KeyCode)==Keys.Right||base.IsInputKey(key);}
    protected override void OnKeyDown(KeyEventArgs e){if(e.KeyCode==Keys.Left||e.KeyCode==Keys.Right){SetValue(Value+(e.KeyCode==Keys.Right?1:-1));e.Handled=true;}base.OnKeyDown(e);}
    protected override void OnMouseWheel(MouseEventArgs e){base.OnMouseWheel(e);SetValue(Value+(e.Delta>0?5:-5));}
    protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(BackColor);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;float x=7*UiScale+(Width-14*UiScale)*(float)Value/100,y=Height/2f;using(var pen=new Pen(Color.FromArgb(66,72,84),4*UiScale))e.Graphics.DrawLine(pen,7*UiScale,y,Width-7*UiScale,y);using(var pen=new Pen(UiTheme.Accent,4*UiScale))e.Graphics.DrawLine(pen,7*UiScale,y,x,y);using(var brush=new SolidBrush(Focused?Color.White:Color.FromArgb(205,216,245)))e.Graphics.FillEllipse(brush,x-6*UiScale,y-6*UiScale,12*UiScale,12*UiScale);}
}
partial class PaletteDialog {
    NumberField opacityField;
    OpacitySlider opacitySlider;
    Label opacityLabel;
    public decimal OpacityPercent {get{return opacityField==null?100:opacityField.Value;}}
    public void ConfigureOpacity(decimal value,string label){opacityLabel.Text=label;opacityField.Value=value;}
    void AddOpacityControls(){
        foreach(Control control in Controls)if(control.Top>=326)control.Top+=60;ClientSize=new Size(ClientSize.Width,ClientSize.Height+60);
        opacityLabel=new Label {Text="Opacity · %",Location=new Point(20,322),Size=new Size(230,24),ForeColor=ForeColor};Controls.Add(opacityLabel);
        opacitySlider=new OpacitySlider {Location=new Point(20,347),Size=new Size(230,28),BackColor=BackColor};opacityField=new NumberField {Minimum=0,Maximum=100,Value=(decimal)Math.Round(Chosen.A*100d/255),Location=new Point(264,342),Size=new Size(72,34),BackColor=Color.FromArgb(43,46,54),ForeColor=ForeColor,AccessibleName="Color opacity percent"};
        opacitySlider.Value=opacityField.Value;opacitySlider.ValueChanged+=delegate{opacityField.Value=opacitySlider.Value;};opacityField.ValueChanged+=delegate{opacitySlider.Value=opacityField.Value;opacitySlider.Invalidate();swatch.Invalidate();};Controls.Add(opacitySlider);Controls.Add(opacityField);
        swatch.Paint+=delegate(object sender,PaintEventArgs e){for(int y=0;y<swatch.Height;y+=6)for(int x=0;x<swatch.Width;x+=6)using(var checker=new SolidBrush(((x/6+y/6)%2)==0?Color.FromArgb(220,220,220):Color.FromArgb(160,160,160)))e.Graphics.FillRectangle(checker,x,y,6,6);using(var brush=new SolidBrush(Color.FromArgb((int)Math.Round(255*OpacityPercent/100),Chosen)))e.Graphics.FillRectangle(brush,swatch.ClientRectangle);};
    }
}
partial class Editor {
    bool PrimaryFillOpacity(){string kind=Active==null?tool:Active.Kind;return kind=="Highlight"||kind=="Bucket"||kind=="Step";}
    static bool StandaloneOpacity(string kind){return kind=="Grayscale"||kind=="Redact"||kind=="Eraser"||kind=="Spotlight"||kind=="Cursor";}
    void ApplyPaletteColor(bool isFill,Color color,decimal opacity){
        FinishStrokeAdjustment();FlushInlineUpdate();opacity=Math.Max(0,Math.Min(100,opacity));bool primary=isFill||PrimaryFillOpacity();
        if(Active!=null&&inlineTarget==null)Push();
        bool before=syncing;syncing=true;try{
            if(primary){fillOpacity.Value=opacity;color=Color.FromArgb(255,color);}else color=Color.FromArgb((int)Math.Round(255*opacity/100),color);
            if(isFill){fill=color;fillBox.Checked=true;}else ink=color;
            colorButton.BackColor=ink;fillButton.BackColor=fill;
            var mark=inlineTarget??Active;if(mark!=null){if(isFill){mark.FillColor=color.ToArgb();mark.Fill=true;}else mark.Color=color.ToArgb();if(primary)mark.FillOpacity=(float)opacity;}
        }finally{syncing=before;}
        if(inlineTarget!=null){
            // Color changes must not recreate fonts or reapply unrelated geometry/style fields.
            bool old=inlineSync;inlineSync=true;using(((CanvasTextInput)inlineInput).Batch())try{int start=inlineInput.SelectionStart,length=inlineInput.SelectionLength;inlineInput.ForeColor=Color.FromArgb(inlineTarget.Color);inlineInput.SelectAll();inlineInput.SelectionColor=Color.FromArgb(inlineTarget.Color);inlineInput.Select(start,length);}finally{inlineSync=old;}
            inlineChanged=true;
        }
        RefreshImage();if(inlineTarget!=null)StartDragPreview();UpdateInspector();
    }
    public void VerifyColorOpacity(List<string> report,string folder){
        Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception(label);report.Add("PASS "+label);};LoadBitmap(Blank(400,300),"Color opacity");var shape=new Mark {Kind="Rectangle",X=40,Y=40,W=180,H=100,Width=4,Fill=true,Color=Color.Blue.ToArgb(),FillColor=Color.Red.ToArgb()};Marks.Add(shape);Active=shape;Selected.Clear();Selected.Add(shape);SyncStyle();int history=undo.Count;ApplyPaletteColor(false,Color.Blue,50);check(Color.FromArgb(shape.Color).A==128&&shape.FillOpacity==100,"Stroke opacity changes independently of fill");check(undo.Count==history+1,"Color and opacity apply as one Undo step");ApplyPaletteColor(true,Color.Red,25);check(shape.FillOpacity==25&&Color.FromArgb(shape.Color).A==128&&shape.Fill,"Fill color applies opacity and enables fill without changing stroke");using(var image=Art.Render(Background,Marks)){var center=image.GetPixel(100,80);check(center.R>245&&center.G>180&&center.G<200,"Fill opacity blends correctly in final export");var edge=image.GetPixel(40,80);check(edge.B>edge.R&&edge.R>60,"Stroke transparency blends with the underlying image");}Undo();check(Marks[0].FillOpacity==100,"Undo restores the previous fill opacity");Redo();shape=Marks[0];Active=shape;Selected.Clear();Selected.Add(shape);SyncStyle();var preset=AnnotationPreset.Capture("Opacity",shape);var restored=new Mark();preset.Apply(restored);check(restored.Color==shape.Color&&restored.FillOpacity==shape.FillOpacity,"Saved annotation styles preserve stroke and fill opacity");
        using(var picker=new PaletteDialog(Color.Blue)){picker.ConfigureOpacity(35,"Stroke opacity · %");picker.AcceptScreenColor(Color.Green);check(picker.OpacityPercent==35&&picker.Chosen.G==128,"Eyedropper color changes preserve the selected opacity");picker.Show();Application.DoEvents();using(var image=new Bitmap(picker.Width,picker.Height)){picker.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(Path.Combine(folder,"color-opacity-picker.png"));}picker.Hide();}
        Show();Application.DoEvents();check(!stylingHeaderRow.Visible&&!fillOpacityRow.Visible&&cornerRadius.Visible&&cornerRadius.Parent.Parent==weightRow,"Corner radius sits beside stroke size and opacity lives in color pickers");using(var image=new Bitmap(Width,Height)){DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(Path.Combine(folder,"direct-styling-controls.png"));}Hide();LoadBitmap(Blank(100,100),"Test");
    }
}
}

