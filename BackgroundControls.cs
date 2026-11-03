using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;
namespace ShotOut {
public class BackgroundLayout {
    public float Margin,Padding,CornerRadius,ShadowRadius;
    public bool SmartPadding=true,Enabled,Gradient;
    public string Ratio="Auto";
    public BackgroundLayout Clone(){return (BackgroundLayout)MemberwiseClone();}
}
static partial class Art {
    static unsafe void RoundImageCorners(Bitmap image,Mark mark,Rectangle? clip=null){
        if(!mark.FrameRoundedImage||mark.Radius<=0)return;var size=LocalSize(mark);float radius=Math.Min(mark.Radius,Math.Min(size.Width,size.Height)/2);float rx=radius*image.Width/Math.Max(1,size.Width),ry=radius*image.Height/Math.Max(1,size.Height);if(rx<.01f||ry<.01f)return;
        int width=image.Width,height=image.Height;var region=new Rectangle(0,0,width,height);var bits=image.LockBits(region,ImageLockMode.ReadWrite,PixelFormat.Format32bppArgb);try{
            int rows=Math.Min(height,(int)Math.Ceiling(ry));for(int y=0;y<rows;y++){float dy=(ry-y-.5f)/ry;float edge=rx*(1-(float)Math.Sqrt(Math.Max(0,1-dy*dy)));int columns=Math.Min(width/2,(int)Math.Ceiling(edge+.5f));for(int side=0;side<2;side++){int yy=side==0?y:height-1-y;byte* row=(byte*)bits.Scan0+yy*bits.Stride;for(int x=0;x<columns;x++){float coverage=Math.Max(0,Math.Min(1,x+1-edge));int left=x*4+3,right=(width-1-x)*4+3;if(!clip.HasValue||clip.Value.Contains(x,yy))row[left]=(byte)Math.Round(row[left]*coverage);if(!clip.HasValue||clip.Value.Contains(width-1-x,yy))row[right]=(byte)Math.Round(row[right]*coverage);}}}
        }finally{image.UnlockBits(bits);}
    }
}
partial class Editor {
    readonly Dictionary<string,AdjustmentField> backgroundFields=new Dictionary<string,AdjustmentField>();
    readonly Dictionary<string,ModernButton> backgroundRatios=new Dictionary<string,ModernButton>();
    ThemeCheckBox backgroundEnabled,backgroundSmart,backgroundGradient;
    Mark backgroundFrame;
    Color backgroundStart,backgroundEnd;
    string backgroundRatio="Auto";
    bool backgroundSync,backgroundPending,backgroundUndo;
    Timer backgroundPaint,backgroundCommit;
    void AddBackgroundCard(){var card=new EffectTile {Caption="Background",Preview=MakeEffectPreview("Add presentation"),Width=210,Height=76,Text="",Margin=new Padding(0,0,0,6),BackColor=Color.FromArgb(43,46,54),ForeColor=text,Font=Font,AccessibleName="Background"};card.Click+=delegate{OpenBackgroundControls();};tips.SetToolTip(card,"Transparent or colored backgrounds, spacing, rounded corners, shadows and ratios");effectsPane.Controls.Add(card);}
    void OpenBackgroundControls(){
        CloseImageEffects();var host=inspector.Parent;inspector.Visible=layers.Visible=layerTitle.Visible=layerButtons.Visible=false;
        effectsPane=new VerticalInspector {Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,BackColor=host.BackColor,Padding=Padding.Empty};
        var title=new Panel {Width=210,Height=42,Margin=Padding.Empty};var back=ActionButton("‹",null,"Back to Image Effects",ImageMenu,32);back.Height=32;var label=new Label {Text="Background",Location=new Point(38,0),Size=new Size(172,36),ForeColor=text,Font=new Font("Segoe UI",15,FontStyle.Bold,GraphicsUnit.Pixel),TextAlign=ContentAlignment.MiddleLeft};title.Controls.Add(back);title.Controls.Add(label);title.Resize+=delegate{label.Width=Math.Max(UiPixels(60),title.Width-UiPixels(40));};effectsPane.Controls.Add(title);
        if(!Marks.Any(mark=>mark.Kind=="Image"&&mark.Image!=null)&&!welcomeRequested){Push();PromoteImageLayer();}
        if(!Marks.Any(mark=>mark.Kind=="Image"&&mark.Image!=null)){effectsPane.Controls.Add(new Label {Text="Open or capture an image first.",Height=48,Width=210,ForeColor=muted});}
        else InstallBackgroundControls();
        host.Controls.Add(effectsPane);effectsPane.BringToFront();effectsPane.ClientSizeChanged+=delegate{SizeAdjustmentPane();};InstallEffectsResizeGrip();ScaleEffectsControls();SizeAdjustmentPane();
    }
    void InstallBackgroundControls(){
        backgroundFrame=Marks.FirstOrDefault(mark=>mark.IsPresentation&&!mark.IsPresentationCard);var data=backgroundFrame==null?new BackgroundLayout():backgroundFrame.FrameLayout==null?null:backgroundFrame.FrameLayout.Clone();if(data!=null&&backgroundFrame!=null){data.Enabled=backgroundFrame.Visible;data.Gradient=backgroundFrame.Fill;}
        if(data==null){var image=Marks.First(mark=>mark.Kind=="Image");data=new BackgroundLayout {Padding=Math.Max(0,image.BodyBounds.Left-backgroundFrame.X),Margin=Math.Max(0,backgroundFrame.X),Enabled=backgroundFrame.Visible,Gradient=backgroundFrame.Fill,CornerRadius=backgroundFrame.Radius,ShadowRadius=backgroundFrame.Shadow?backgroundFrame.ShadowSize:0};}
        backgroundStart=backgroundFrame==null?Color.FromArgb(40,44,55):Color.FromArgb(backgroundFrame.Color);backgroundEnd=backgroundFrame==null?Color.FromArgb(80,110,180):Color.FromArgb(backgroundFrame.FillColor);backgroundRatio=data.Ratio;
        backgroundSync=true;backgroundFields.Clear();backgroundRatios.Clear();AddBackgroundField("Margin",0,500,data.Margin,"Space around the image frame, filled by the background when enabled.");AddBackgroundField("Padding",0,500,data.Padding,"Neutral frame between your content and the outer background.");
        backgroundSmart=new ThemeCheckBox {Text="Smart padding",Checked=data.SmartPadding,Width=210,Height=32,ForeColor=text};tips.SetToolTip(backgroundSmart,"Reserves enough transparent space to avoid clipping shadows.");backgroundSmart.CheckedChanged+=delegate{ChangeBackgroundControls();};effectsPane.Controls.Add(backgroundSmart);
        AddBackgroundField("Rounded corners",0,200,data.CornerRadius,"Round the image and background corners without changing source pixels.");AddBackgroundField("Shadow radius",0,64,data.ShadowRadius,"Soft shadow size. Set to zero to remove the shadow.");
        effectsPane.Controls.Add(new Label {Text="Canvas ratio",Width=210,Height=26,ForeColor=text,Font=new Font(Font,FontStyle.Bold)});
        var ratios=new TableLayoutPanel {Width=210,Height=112,ColumnCount=3,RowCount=3,Margin=new Padding(0,0,0,8)};for(int i=0;i<3;i++)ratios.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,33.333f));
        foreach(string name in new[]{"Auto","1:1","4:3","3:2","16:9","5:3","9:16","3:4","2:3"}){string choice=name;var button=new ModernButton {Text=name,Dock=DockStyle.Fill,Height=32,Margin=new Padding(1),BackColor=Color.FromArgb(43,46,54),ForeColor=text,Font=Font,Selected=name==backgroundRatio,AccessibleName="Canvas ratio "+name};button.Click+=delegate{backgroundRatio=choice;foreach(var pair in backgroundRatios){pair.Value.Selected=pair.Key==choice;pair.Value.Invalidate();}ChangeBackgroundControls();};backgroundRatios[name]=button;ratios.Controls.Add(button);}
        effectsPane.Controls.Add(ratios);backgroundEnabled=new ThemeCheckBox {Text="Background color",Checked=data.Enabled,Width=210,Height=32,ForeColor=text};backgroundEnabled.CheckedChanged+=delegate{ChangeBackgroundControls();};effectsPane.Controls.Add(backgroundEnabled);
        backgroundGradient=new ThemeCheckBox {Text="Gradient",Checked=data.Gradient,Width=210,Height=32,ForeColor=text};backgroundGradient.CheckedChanged+=delegate{ChangeBackgroundControls();};effectsPane.Controls.Add(backgroundGradient);
        var colors=new FlowLayoutPanel {Width=210,Height=36,WrapContents=false};var first=ActionButton("Color",null,"Choose background color; choosing a color enables the background",delegate{PickBackgroundColor(false);},101);var second=ActionButton("End color",null,"Choose gradient end color",delegate{PickBackgroundColor(true);},101);first.Swatch=second.Swatch=true;first.BackColor=backgroundStart;second.BackColor=backgroundEnd;colors.Controls.Add(first);colors.Controls.Add(second);colors.Resize+=delegate{first.Width=second.Width=Math.Max(60,(colors.Width-8)/2);};effectsPane.Controls.Add(colors);
        effectsPane.Controls.Add(ActionButton("Reset background",null,"Reset spacing, rounding, shadow and ratio to transparent defaults",ResetBackgroundControls,210));effectsPane.Controls.Add(new Label {Text="Starts transparent. Changes stay editable in your project. PNG preserves transparency.",Width=210,Height=64,ForeColor=muted});backgroundSync=false;
    }
    void AddBackgroundField(string name,decimal min,decimal max,float value,string hint){var field=new AdjustmentField(name,min,max);field.Value=(decimal)Math.Max((float)min,Math.Min((float)max,value));field.Changed+=delegate{ChangeBackgroundControls();};field.Finished+=delegate{FinishBackgroundAdjustment();};tips.SetToolTip(field,hint);tips.SetToolTip(field.Number,hint);backgroundFields[name]=field;effectsPane.Controls.Add(field);}
    void PickBackgroundColor(bool end){using(var dialog=new PaletteDialog(end?backgroundEnd:backgroundStart)){dialog.ConfigureOpacity(100,"Background opacity");if(dialog.ShowDialog(this)!=DialogResult.OK)return;var color=Color.FromArgb((int)Math.Round(255*dialog.OpacityPercent/100),dialog.Chosen);if(end)backgroundEnd=color;else backgroundStart=color;backgroundSync=true;backgroundEnabled.Checked=true;if(end)backgroundGradient.Checked=true;backgroundSync=false;ChangeBackgroundControls();FinishBackgroundAdjustment();foreach(var panel in effectsPane.Controls.OfType<FlowLayoutPanel>())foreach(var button in panel.Controls.OfType<ModernButton>())if(button.Swatch)button.BackColor=button.Text=="Color"?backgroundStart:backgroundEnd;}}
    void ResetBackgroundControls(){backgroundSync=true;foreach(var field in backgroundFields.Values)field.Value=0;backgroundSmart.Checked=true;backgroundEnabled.Checked=backgroundGradient.Checked=false;backgroundRatio="Auto";foreach(var pair in backgroundRatios){pair.Value.Selected=pair.Key=="Auto";pair.Value.Invalidate();}backgroundSync=false;ChangeBackgroundControls();FinishBackgroundAdjustment();}
    void ChangeBackgroundControls(){
        if(backgroundSync||backgroundFields.Count==0)return;if(!backgroundUndo){Push();backgroundUndo=true;}backgroundPending=true;
        if(backgroundPaint==null){backgroundPaint=new Timer {Interval=33};backgroundPaint.Tick+=delegate{FlushBackgroundAdjustment();};backgroundCommit=new Timer {Interval=250};backgroundCommit.Tick+=delegate{if(!backgroundFields.Values.Any(field=>field.Adjusting))FinishBackgroundAdjustment();};Disposed+=delegate{backgroundPaint.Dispose();backgroundCommit.Dispose();};}backgroundPaint.Start();backgroundCommit.Stop();backgroundCommit.Start();
    }
    void FlushBackgroundAdjustment(){
        if(backgroundPaint!=null)backgroundPaint.Stop();if(!backgroundPending||backgroundFields.Count==0)return;backgroundPending=false;
        var content=RectangleF.Empty;foreach(var mark in Marks)if(!mark.IsPresentation&&mark.Visible)content=content.IsEmpty?mark.Bounds:RectangleF.Union(content,mark.Bounds);if(content.Width<1||content.Height<1)return;
        var data=new BackgroundLayout {Margin=(float)backgroundFields["Margin"].Value,Padding=(float)backgroundFields["Padding"].Value,CornerRadius=(float)backgroundFields["Rounded corners"].Value,ShadowRadius=(float)backgroundFields["Shadow radius"].Value,SmartPadding=backgroundSmart.Checked,Enabled=backgroundEnabled.Checked,Gradient=backgroundGradient.Checked,Ratio=backgroundRatio};
        float margin=data.SmartPadding?Math.Max(data.Margin,data.ShadowRadius*2):data.Margin;float width=content.Width+2*(data.Padding+margin),height=content.Height+2*(data.Padding+margin);if(data.Ratio!="Auto"){var parts=data.Ratio.Split(':');float ratio=float.Parse(parts[0])/float.Parse(parts[1]);if(width/height<ratio)width=height*ratio;else height=width/ratio;}
        int w=(int)Math.Ceiling(width),h=(int)Math.Ceiling(height);if(w>16000||h>16000||(long)w*h>40000000){UpdateStatus("Background would exceed the canvas size limit. Reduce spacing or choose a different ratio.");return;}
        ResizeCanvas(w,h,(int)Math.Round((w-content.Width)/2-content.Left),(int)Math.Round((h-content.Height)/2-content.Top));ResetCanvasBase();
        if(backgroundFrame==null||!Marks.Contains(backgroundFrame)){backgroundFrame=new Mark {Kind="Rectangle",Name="Background",IsPresentation=true,Width=0};Marks.Insert(0,backgroundFrame);}
        backgroundFrame.FrameLayout=data;backgroundFrame.X=backgroundFrame.Y=0;backgroundFrame.W=w;backgroundFrame.H=h;backgroundFrame.Visible=data.Enabled;backgroundFrame.Fill=data.Gradient;backgroundFrame.Color=backgroundStart.ToArgb();backgroundFrame.FillColor=backgroundEnd.ToArgb();backgroundFrame.Radius=0;backgroundFrame.Shadow=false;backgroundFrame.ShadowSize=data.ShadowRadius;backgroundFrame.ShadowIntensity=30;
        var card=Marks.FirstOrDefault(mark=>mark.IsPresentationCard);if(card==null&&data.Padding>0){card=new Mark {Kind="Rectangle",Name="Padding",IsPresentation=true,IsPresentationCard=true,Width=0};Marks.Insert(Marks.IndexOf(backgroundFrame)+1,card);}if(card!=null){card.X=card.Y=margin;card.W=w-margin*2;card.H=h-margin*2;card.Visible=data.Padding>0;card.Color=Color.FromArgb(40,44,55).ToArgb();card.Fill=false;card.Radius=data.CornerRadius+data.Padding;card.Shadow=card.Visible&&data.ShadowRadius>0;card.ShadowSize=data.ShadowRadius;card.ShadowIntensity=30;}
        var image=Marks.FirstOrDefault(mark=>mark.Kind=="Image"&&(mark.IsCaptureBase||!Marks.Any(other=>other.IsCaptureBase)));if(image!=null){image.FrameRoundedImage=true;image.Radius=data.CornerRadius;image.Shadow=data.Padding==0&&data.ShadowRadius>0;image.ShadowSize=data.ShadowRadius;image.ShadowIntensity=30;}
        Active=null;Selected.Clear();RefreshImage();View.Fit();
    }
    void FinishBackgroundAdjustment(){if(backgroundCommit!=null)backgroundCommit.Stop();FlushBackgroundAdjustment();backgroundUndo=false;}
    void CloseBackgroundSession(){FinishBackgroundAdjustment();backgroundFields.Clear();backgroundRatios.Clear();backgroundFrame=null;}
}
}
