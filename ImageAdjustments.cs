using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
namespace ShotOut {
static partial class Art {
    [ThreadStatic] static string beforeImageId;
    internal sealed class ImageComparison : IDisposable {
        readonly string previous;
        internal ImageComparison(string id){previous=beforeImageId;beforeImageId=id;}
        public void Dispose(){beforeImageId=previous;}
    }
    internal static bool HasImageAdjustments(Mark mark){return HasGeneralImageAdjustments(mark)||(mark!=null&&mark.ImageZones.Any(ActiveZone));} internal static bool HasGeneralImageAdjustments(Mark mark){return mark!=null&&mark.ImageAdjustmentsEnabled&&mark.AdjustmentStrength>0&&(mark.Exposure!=0||mark.Brightness!=0||mark.Contrast!=0||mark.Saturation!=0||mark.Temperature!=0)||HasImageFilter(mark);}
    // All five controls share one lookup-table pass and a bounded result cache. Source PNGs
    // stay immutable; adjustments are ordinary serialized image-layer parameters.
    internal static ColorMatrix ImageAdjustmentMatrix(Mark mark,bool includeFilter=true,bool includeGeneral=true){
        float exposure=(float)Math.Pow(2,Math.Max(-3,Math.Min(3,mark.Exposure))),contrast=1+Math.Max(-100,Math.Min(100,mark.Contrast))/100;
        float saturation=1+Math.Max(-100,Math.Min(100,mark.Saturation))/100,temperature=Math.Max(-100,Math.Min(100,mark.Temperature))/100;
        float amount=includeGeneral&&mark.ImageAdjustmentsEnabled?Math.Max(0,Math.Min(100,mark.AdjustmentStrength))/100:0,offset=.5f*(1-contrast)+Math.Max(-100,Math.Min(100,mark.Brightness))/100;
        float[] luma={.2126f,.7152f,.0722f},tint={1+.25f*temperature,1,1-.25f*temperature};var values=new float[5][];for(int i=0;i<5;i++)values[i]=new float[5];
        for(int input=0;input<3;input++)for(int output=0;output<3;output++){float identity=input==output?1:0;float adjusted=(luma[input]*(1-saturation)+identity*saturation)*exposure*contrast*tint[output];values[input][output]=identity+(adjusted-identity)*amount;}
        values[3][3]=values[4][4]=1;for(int c=0;c<3;c++)values[4][c]=offset*amount;return includeFilter?CombineImageFilter(new ColorMatrix(values),mark):new ColorMatrix(values);
    }
    static void DrawAdjustedLayer(Graphics graphics,Image image,RectangleF bounds,Mark mark){
        bool compared=!String.IsNullOrEmpty(beforeImageId)&&beforeImageId==mark.ImageAdjustmentId;if((!HasImageAdjustments(mark)||compared)&&!(mark.FrameRoundedImage&&mark.Radius>0)){DrawImageQuality(graphics,image,bounds);return;}
        var key=ImageToneKey(mark);
        if(tonePreview!=null&&Object.ReferenceEquals(toneSource,mark.Image)&&toneSettings.SequenceEqual(key)&&SameImageEffectOrder(toneMark,mark)){DrawImageQuality(graphics,tonePreview,bounds);return;}
        if(TryUpdateZonePixels((Bitmap)image,mark,key)){DrawImageQuality(graphics,tonePreview,bounds);return;}
        ClearImageToneCache();var adjusted=BuildOrderedImageEffects((Bitmap)image,mark,new Rectangle(Point.Empty,image.Size),compared);RoundImageCorners(adjusted,mark);
        if((long)adjusted.Width*adjusted.Height*4<=40L*1024*1024){tonePreview=adjusted;toneSource=mark.Image;toneSettings=key;toneMark=mark.Clone();DrawImageQuality(graphics,adjusted,bounds);}
        else using(adjusted)DrawImageQuality(graphics,adjusted,bounds);
    }
    static Bitmap tonePreview;static byte[] toneSource;static float[] toneSettings;static Mark toneMark;
    static bool TryUpdateZonePixels(Bitmap source,Mark mark,float[] key){
        if(tonePreview==null||toneMark==null||!Object.ReferenceEquals(toneSource,mark.Image)||!String.IsNullOrEmpty(beforeImageId)||toneSettings.Length!=key.Length||!toneSettings.Take(13).SequenceEqual(key.Take(13))||toneMark.ImageZones.Count!=mark.ImageZones.Count||!SameImageEffectOrder(toneMark,mark))return false;
        var area=Rectangle.Empty;for(int i=0;i<mark.ImageZones.Count;i++){if(mark.ImageZones[i].Id!=toneMark.ImageZones[i].Id)return false;if(!toneSettings.Skip(13+i*13).Take(13).SequenceEqual(key.Skip(13+i*13).Take(13))){var old=ZonePixelBounds(toneMark.ImageZones[i],source.Width,source.Height);var next=ZonePixelBounds(mark.ImageZones[i],source.Width,source.Height);if(!old.IsEmpty)area=area.IsEmpty?old:Rectangle.Union(area,old);if(!next.IsEmpty)area=area.IsEmpty?next:Rectangle.Union(area,next);}}
        if(!area.IsEmpty){using(var patch=BuildOrderedImageEffects(source,mark,area,false))using(var graphics=Graphics.FromImage(tonePreview)){graphics.CompositingMode=CompositingMode.SourceCopy;graphics.DrawImageUnscaled(patch,area.Location);}RoundImageCorners(tonePreview,mark,area);}
        toneSettings=key;toneMark=mark.Clone();return true;
    }
    internal static long ImageToneCacheBytes {get{return tonePreview==null?0:(long)tonePreview.Width*tonePreview.Height*4;}}
    internal static void ClearImageToneCache(){if(tonePreview!=null)tonePreview.Dispose();tonePreview=null;toneSource=null;toneSettings=null;toneMark=null;}
    static unsafe Bitmap BuildImageTone(Bitmap source,ColorMatrix matrix){
        int width=source.Width,height=source.Height;var image=new Bitmap(width,height,PixelFormat.Format32bppArgb);var bounds=new Rectangle(Point.Empty,source.Size);
        // 16-bit fixed-point lookup tables eliminate per-pixel floating-point math.
        bool diagonal=matrix.Matrix01==0&&matrix.Matrix02==0&&matrix.Matrix10==0&&matrix.Matrix12==0&&matrix.Matrix20==0&&matrix.Matrix21==0;
        int[][] table=new int[9][];for(int i=0;i<9;i++)table[i]=new int[256];int[] offset={ (int)Math.Round(matrix.Matrix40*255*65536), (int)Math.Round(matrix.Matrix41*255*65536), (int)Math.Round(matrix.Matrix42*255*65536)};
        for(int input=0;input<3;input++)for(int output=0;output<3;output++)for(int value=0;value<256;value++)table[output*3+input][value]=(int)Math.Round(matrix[input,output]*value*65536);
        var from=source.LockBits(bounds,ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);try{var to=image.LockBits(bounds,ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);try{
            var rr=table[0];var rg=table[1];var rb=table[2];var gr=table[3];var gg=table[4];var gb=table[5];var br=table[6];var bg=table[7];var bb=table[8];
            if(diagonal){var red=new byte[256];var green=new byte[256];var blue=new byte[256];for(int value=0;value<256;value++){red[value]=ToneByte(rr[value]+offset[0]);green[value]=ToneByte(gg[value]+offset[1]);blue[value]=ToneByte(bb[value]+offset[2]);}
                for(int y=0;y<height;y++){byte* input=(byte*)from.Scan0+y*from.Stride;byte* output=(byte*)to.Scan0+y*to.Stride;for(int x=0;x<width;x++,input+=4,output+=4){output[0]=blue[input[0]];output[1]=green[input[1]];output[2]=red[input[2]];output[3]=input[3];}}
            }else for(int y=0;y<height;y++){byte* input=(byte*)from.Scan0+y*from.Stride;byte* output=(byte*)to.Scan0+y*to.Stride;for(int x=0;x<width;x++,input+=4,output+=4){int b=input[0],g=input[1],r=input[2];output[2]=ToneByte(rr[r]+rg[g]+rb[b]+offset[0]);output[1]=ToneByte(gr[r]+gg[g]+gb[b]+offset[1]);output[0]=ToneByte(br[r]+bg[g]+bb[b]+offset[2]);output[3]=input[3];}}
        }finally{image.UnlockBits(to);}}catch{image.Dispose();throw;}finally{source.UnlockBits(from);}return image;
    }
    static byte ToneByte(int fixedValue){int value=(fixedValue+32768)>>16;return (byte)(value<0?0:value>255?255:value);}
}
sealed class AdjustmentField : UserControl {
    internal float UiScale=1f;
    internal readonly NumberField Number;
    readonly OpacitySlider slider;
    bool sync;
    internal event EventHandler Changed,Finished;
    internal decimal Value {get{return Number.Value;}set{sync=true;Number.Value=value;slider.Value=(Number.Value-Number.Minimum)*100/(Number.Maximum-Number.Minimum);slider.Invalidate();sync=false;}}
    internal bool Adjusting {get{return slider.Capture;}}
    internal AdjustmentField(string label,decimal min,decimal max){
        Height=57;Width=215;Margin=new Padding(0,0,0,4);BackColor=Color.FromArgb(35,38,45);ForeColor=Color.FromArgb(233,236,243);
        var caption=new Label {Text=label=="Exposure"?"Exposure · EV":label,Location=new Point(9,1),Size=new Size(130,28),ForeColor=ForeColor,TextAlign=ContentAlignment.MiddleLeft};
        Number=new NumberField {Minimum=min,Maximum=max,Value=0,Width=66,Height=28,BackColor=Color.FromArgb(43,46,54),ForeColor=ForeColor,AccessibleName=label};
        slider=new OpacitySlider {Height=22,BackColor=BackColor,AccessibleName=label+" slider"};Controls.Add(caption);Controls.Add(Number);Controls.Add(slider);
        Resize+=delegate{caption.Width=Math.Max((int)(70*UiScale),Width-(int)(85*UiScale));Number.Location=new Point(Width-(int)(73*UiScale),(int)(2*UiScale));slider.Bounds=new Rectangle((int)(8*UiScale),(int)(32*UiScale),Math.Max((int)(40*UiScale),Width-(int)(16*UiScale)),(int)(21*UiScale));};
        Number.ValueChanged+=delegate{if(sync)return;sync=true;slider.Value=(Number.Value-min)*100/(max-min);slider.Invalidate();sync=false;if(Changed!=null)Changed(this,EventArgs.Empty);};
        slider.ValueChanged+=delegate{if(sync)return;Number.Value=Math.Round(min+(max-min)*slider.Value/100,label=="Exposure"?2:0);};
        slider.MouseUp+=delegate{if(Finished!=null)Finished(this,EventArgs.Empty);};caption.DoubleClick+=delegate{Value=label=="Strength"?100:0;if(Changed!=null)Changed(this,EventArgs.Empty);if(Finished!=null)Finished(this,EventArgs.Empty);};Value=label=="Strength"?100:0;
    }
}
partial class Editor {
    Label appearanceHeading,typeHeading;
    Mark imageAdjustmentTarget;
    readonly Dictionary<string,AdjustmentField> imageAdjustmentFields=new Dictionary<string,AdjustmentField>();
    readonly List<Control> advancedImageControls=new List<Control>();
    Timer imageAdjustmentPaint,imageAdjustmentCommit;
    bool imageAdjustmentSync,imageAdjustmentDirty,imageAdjustmentUndo,advancedImages;
    string imageComparisonId;
    internal string ImageComparisonId {get{return imageComparisonId;}}
    Label imageAdjustmentStatus;
    InspectorDisclosure advancedImageButton;
    ThemeCheckBox imageBefore,imageEnabled;
    ModernButton imageBeforeButton,imageAfterButton;
    void PolishPropertySections(){
        appearanceHeading=new Label {Text="APPEARANCE",Height=25,Width=217,ForeColor=muted,Margin=new Padding(0,5,0,0)};typeHeading=new Label {Text="TEXT",Height=25,Width=217,ForeColor=muted,Margin=new Padding(0,6,0,0)};inspector.Controls.Add(appearanceHeading);inspector.Controls.Add(typeHeading);
        Control[] ordered={toolTitle,presetRow,arrangeRow,appearanceHeading,variantRow,strokeRow,fillRow,weightRow,tailRow,shadowToggleRow,shadowSizeRow,shadowIntensityRow,typeHeading,textRow,formatRow,textOptionsHeaderRow,fontRow,listRow,alignmentRow,editRow,stepNumberRow,stepSizeRow,geometryTitleRow,geometryPositionRow,geometrySizeRow};
        int index=0;foreach(var control in ordered)if(control!=null&&control.Parent==inspector)inspector.Controls.SetChildIndex(control,index++);
    }
    int PropertySectionSpacing(string kind){if(appearanceHeading==null)return 0;appearanceHeading.Visible=Art.IsArrow(kind)||new[]{"Rectangle","Rounded","Ellipse","Text","Balloon","Emoji","Pen","Step","Highlight","Magnify","Bucket","Blur","Pixelate","Redact"}.Contains(kind);typeHeading.Visible=kind=="Text"||kind=="Balloon"||kind=="Emoji";return (appearanceHeading.Visible?30:0)+(typeHeading.Visible?31:0);}
    void InstallImageAdjustments(){
        imageAdjustmentFields.Clear();advancedImageControls.Clear();imageAdjustmentTarget=null;imageComparisonId=null;
        var images=Marks.Where(mark=>mark.Kind=="Image"&&mark.Image!=null).ToList();
        if(images.Count==0&&!welcomeRequested){Push();PromoteImageLayer();images=Marks.Where(mark=>mark.Kind=="Image"&&mark.Image!=null).ToList();}
        var heading=new Label {Text=zoneAdjustmentMode?"Choose image and zone":"Choose image layer",Height=32,Width=210,ForeColor=text,Font=new Font(Font,FontStyle.Bold)};effectsPane.Controls.Add(heading);
        if(images.Count==0){effectsPane.Controls.Add(new Label {Text="Open or capture an image to adjust it.",Width=210,Height=48,ForeColor=muted});return;}
        imageAdjustmentTarget=Active!=null&&images.Contains(Active)?Active:images.First();
        var choose=new ChoiceButton {Width=210,Height=34,BackColor=Color.FromArgb(43,46,54),ForeColor=text,Font=Font,AccessibleName="Image layer to adjust"};foreach(var image in images)choose.Items.Add((images.IndexOf(image)+1)+" · "+(String.IsNullOrEmpty(image.Name)?"Image":image.Name));
        choose.SelectedItem=choose.Items[images.IndexOf(imageAdjustmentTarget)];choose.SelectedIndexChanged+=delegate{FinishImageAdjustment();imageComparisonId=null;imageAdjustmentTarget=images[choose.Items.IndexOf(choose.SelectedItem)];if(zoneAdjustmentMode){selectedImageZone=imageAdjustmentTarget.ImageZones.FirstOrDefault();zoneAdding=selectedImageZone==null;RefreshZoneChoices();}SyncImageAdjustmentFields();RefreshImage();};effectsPane.Controls.Add(choose);if(zoneAdjustmentMode)InstallZoneControls();
        imageBefore=new ThemeCheckBox {Visible=false};effectsPane.Controls.Add(imageBefore);
        var compare=new FlowLayoutPanel {Width=210,Height=36,WrapContents=false,Margin=new Padding(0,5,0,3)};
        imageBeforeButton=ActionButton("Before",null,"Compare with the original image; saved adjustments stay intact",delegate{imageBefore.Checked=true;},69);imageAfterButton=ActionButton("After",null,"Show the adjusted image",delegate{imageBefore.Checked=false;},69);var resetButton=ActionButton("Reset",null,"Reset adjustments on this image (undoable)",ResetImageAdjustment,62);
        foreach(var button in new[]{imageBeforeButton,imageAfterButton,resetButton}){button.Height=32;button.Margin=new Padding(0,0,3,0);compare.Controls.Add(button);}compare.Resize+=delegate{int width=Math.Max(UiPixels(48),(compare.Width-UiPixels(69))/2);imageBeforeButton.Width=imageAfterButton.Width=width;};effectsPane.Controls.Add(compare);
        imageBefore.CheckedChanged+=delegate{SyncComparisonButtons();if(imageAdjustmentSync)return;FlushImageAdjustmentPreview();imageComparisonId=imageBefore.Checked?CurrentComparisonId:null;View.InvalidateMarkupCache();RefreshImage();};SyncComparisonButtons();
        AddAdjustmentHeading("Light");AddImageAdjustmentField("Exposure",-3,3,false,"Exposure: light gain in stops (EV). Double-click the label to reset.");AddImageAdjustmentField("Contrast",-100,100,false,"Contrast: separation between light and dark tones.");AddImageAdjustmentField("Brightness",-100,100,false,"Brightness: shifts tones lighter or darker.");
        AddAdjustmentHeading("Color");AddImageAdjustmentField("Saturation",-100,100,false,"Saturation: color intensity. -100 removes color.");AddImageAdjustmentField("Temperature",-100,100,false,"Temperature: cooler blue to warmer amber. Relative adjustment, not camera Kelvin.");
        advancedImageButton=new InspectorDisclosure {SectionTitle="Advanced",Expanded=advancedImages,Width=210,Height=30,BackColor=Color.FromArgb(43,46,54),ForeColor=text,Font=Font,AccessibleName="Show advanced image adjustments"};advancedImageButton.Click+=delegate{advancedImages=!advancedImages;advancedImageButton.Expanded=advancedImages;advancedImageButton.Invalidate();foreach(var control in advancedImageControls)control.Visible=advancedImages;};effectsPane.Controls.Add(advancedImageButton);
        AddImageAdjustmentField("Strength",0,100,true,"Blend all adjustments with the original. 0 is original, 100 is full effect.");imageEnabled=new ThemeCheckBox {Text="Enable adjustments",Width=210,Height=32,ForeColor=text};imageEnabled.CheckedChanged+=delegate{if(!imageAdjustmentSync)ChangeImageAdjustment();};effectsPane.Controls.Add(imageEnabled);advancedImageControls.Add(imageEnabled);imageEnabled.Visible=advancedImages;
        imageAdjustmentStatus=new Label {Text="Original pixels stay intact. Changes save with your project.",Width=210,Height=60,ForeColor=muted};effectsPane.Controls.Add(imageAdjustmentStatus);SyncImageAdjustmentFields();
    }
    void SyncComparisonButtons(){if(imageBeforeButton==null)return;imageBeforeButton.Selected=imageBefore.Checked;imageAfterButton.Selected=!imageBefore.Checked;imageBeforeButton.Invalidate();imageAfterButton.Invalidate();}
    void ResetImageAdjustment(){if(imageAdjustmentTarget==null||!Marks.Contains(imageAdjustmentTarget)||CurrentImageSettings==null)return;FinishImageAdjustment();Push();CurrentImageSettings.Exposure=CurrentImageSettings.Contrast=CurrentImageSettings.Brightness=CurrentImageSettings.Saturation=CurrentImageSettings.Temperature=0;CurrentImageSettings.AdjustmentStrength=100;CurrentImageSettings.ImageAdjustmentsEnabled=true;imageComparisonId=null;SyncImageAdjustmentFields();RefreshImage();}
    void AddAdjustmentHeading(string name){effectsPane.Controls.Add(new Label {Text=name,Width=210,Height=27,ForeColor=text,Font=new Font(Font,FontStyle.Bold),Margin=new Padding(0,6,0,0)});}
    void AddImageAdjustmentField(string name,decimal min,decimal max,bool advanced,string help){var field=new AdjustmentField(name,min,max);field.Font=Font;field.Changed+=delegate{ChangeImageAdjustment();};field.Finished+=delegate{FinishImageAdjustment();};tips.SetToolTip(field,help);tips.SetToolTip(field.Number,help);imageAdjustmentFields[name]=field;effectsPane.Controls.Add(field);if(advanced){advancedImageControls.Add(field);field.Visible=advancedImages;}}
    void SyncImageAdjustmentFields(){
        if(imageAdjustmentTarget==null)return;imageAdjustmentSync=true;try{var settings=CurrentImageSettings;var target=settings??new Mark();foreach(var field in imageAdjustmentFields.Values)field.Enabled=settings!=null;
            imageAdjustmentFields["Exposure"].Value=(decimal)target.Exposure;imageAdjustmentFields["Contrast"].Value=(decimal)target.Contrast;imageAdjustmentFields["Brightness"].Value=(decimal)target.Brightness;imageAdjustmentFields["Saturation"].Value=(decimal)target.Saturation;imageAdjustmentFields["Temperature"].Value=(decimal)target.Temperature;imageAdjustmentFields["Strength"].Value=(decimal)target.AdjustmentStrength;
            if(zoneAdjustmentMode)imageAdjustmentFields["Feather"].Value=(decimal)(selectedImageZone==null?80:selectedImageZone.Feather);
            imageEnabled.Checked=target.ImageAdjustmentsEnabled;imageEnabled.Enabled=settings!=null;imageBefore.Checked=false;imageBeforeButton.Enabled=imageAfterButton.Enabled=settings!=null;SyncComparisonButtons();
        }finally{imageAdjustmentSync=false;}
    }
    void ChangeImageAdjustment(){
        if(imageAdjustmentSync||imageAdjustmentTarget==null||!Marks.Contains(imageAdjustmentTarget)||CurrentImageSettings==null)return;
        if(!imageAdjustmentUndo){FinishInlineText(true);FinishStrokeAdjustment();Push();imageAdjustmentUndo=true;}
        var target=CurrentImageSettings;if(String.IsNullOrEmpty(imageAdjustmentTarget.ImageAdjustmentId))imageAdjustmentTarget.ImageAdjustmentId=Guid.NewGuid().ToString("N");target.Exposure=(float)imageAdjustmentFields["Exposure"].Value;target.Contrast=(float)imageAdjustmentFields["Contrast"].Value;target.Brightness=(float)imageAdjustmentFields["Brightness"].Value;target.Saturation=(float)imageAdjustmentFields["Saturation"].Value;target.Temperature=(float)imageAdjustmentFields["Temperature"].Value;target.AdjustmentStrength=(float)imageAdjustmentFields["Strength"].Value;target.ImageAdjustmentsEnabled=imageEnabled.Checked;if(zoneAdjustmentMode)selectedImageZone.Feather=(float)imageAdjustmentFields["Feather"].Value;imageAdjustmentDirty=true;
        imageComparisonId=imageBefore.Checked?CurrentComparisonId:null;
        ScheduleImageAdjustment();
    }
    void ScheduleImageAdjustment(){
        if(imageAdjustmentPaint==null){imageAdjustmentPaint=new Timer {Interval=33};imageAdjustmentPaint.Tick+=delegate{FlushImageAdjustmentPreview();};imageAdjustmentCommit=new Timer {Interval=250};imageAdjustmentCommit.Tick+=delegate{if(!zoneGesture&&!imageAdjustmentFields.Values.Any(field=>field.Adjusting))FinishImageAdjustment();};Disposed+=delegate{imageAdjustmentPaint.Dispose();imageAdjustmentCommit.Dispose();};}
        imageAdjustmentPaint.Interval=zoneAdjustmentMode?16:33;imageAdjustmentPaint.Start();imageAdjustmentCommit.Stop();imageAdjustmentCommit.Start();
    }
    void FlushImageAdjustmentPreview(){if(imageAdjustmentPaint!=null)imageAdjustmentPaint.Stop();if(!imageAdjustmentDirty)return;imageAdjustmentDirty=false;View.InvalidateMarkupCache();if(zoneAdjustmentMode&&Preview!=null){using(new Art.ImageComparison(imageComparisonId)){var next=selectedImageZone==null?Art.DrawingBounds(imageAdjustmentTarget,Background.Size):Art.ZoneDocumentBounds(imageAdjustmentTarget,selectedImageZone);var area=zonePreviewBounds.IsEmpty?next:next.IsEmpty?zonePreviewBounds:Rectangle.Union(zonePreviewBounds,next);zonePreviewBounds=next;Art.CopyPixelRegion(Background,Preview,area);foreach(var mark in Marks)if(mark.Visible)Art.Draw(Preview,mark,true,area);}View.Invalidate();}else RefreshImage();}
    void FinishImageAdjustment(){if(imageAdjustmentCommit!=null)imageAdjustmentCommit.Stop();FlushImageAdjustmentPreview();imageAdjustmentUndo=false;}
    void CloseImageAdjustmentSession(){if(zoneAdjustmentMode&&View!=null)View.Invalidate();if(zoneGesture)FinishZoneGesture();FinishImageAdjustment();zoneAdjustmentMode=zoneAdding=false;selectedImageZone=null;zoneChoice=null;imageComparisonId=null;imageAdjustmentTarget=null;imageAdjustmentFields.Clear();advancedImageControls.Clear();}
    public void VerifyImageAdjustments(List<string> report,string folder){
        Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception(label);report.Add("PASS "+label);};
        using(var source=new Bitmap(240,160,PixelFormat.Format32bppArgb)){using(var graphics=Graphics.FromImage(source)){graphics.Clear(Color.FromArgb(80,80,80));graphics.FillRectangle(Brushes.CornflowerBlue,120,0,120,160);graphics.FillRectangle(Brushes.Goldenrod,20,90,70,50);}LoadImageDocument(new Bitmap(source),"Image adjustment example");}
        var image=Marks[0];byte[] bytes=image.Image;var annotation=new Mark {Kind="Rectangle",X=10,Y=10,W=60,H=35,Width=0,Fill=true,FillColor=Color.Red.ToArgb()};Marks.Add(annotation);Active=annotation;Selected.Clear();Selected.Add(annotation);SyncStyle();RefreshImage();Show();Application.DoEvents();
        using(var frame=new Bitmap(inspector.Parent.Width,inspector.Parent.Height)){inspector.Parent.DrawToBitmap(frame,new Rectangle(Point.Empty,frame.Size));frame.Save(Path.Combine(folder,"properties-appearance.png"));}
        using(var baseline=Art.Render(Background,Marks)){OpenImageAdjustmentCard(false);check(effectsPane!=null&&imageAdjustmentTarget==image&&imageAdjustmentFields.Count==6,"Image Effects contains approachable Light, Color and Advanced adjustment controls");check(Art.Bytes(baseline).SequenceEqual(Art.Bytes(Art.Render(Background,Marks))),"Opening neutral adjustments leaves original pixels unchanged");}
        int history=undo.Count;imageAdjustmentFields["Exposure"].Value=1;ChangeImageAdjustment();FlushImageAdjustmentPreview();using(var adjusted=Art.Render(Background,Marks)){check(Math.Abs(adjusted.GetPixel(90,30).R-160)<=1,"Exposure increases light by stops");check(adjusted.GetPixel(30,25).ToArgb()==Color.Red.ToArgb(),"Image adjustments preserve the colors of annotations above the image");}
        imageAdjustmentFields["Exposure"].Value=0;ChangeImageAdjustment();imageAdjustmentFields["Brightness"].Value=20;ChangeImageAdjustment();FlushImageAdjustmentPreview();using(var adjusted=Art.Render(Background,Marks))check(Math.Abs(adjusted.GetPixel(90,30).R-131)<=1,"Brightness shifts tones independently of exposure");
        imageAdjustmentFields["Brightness"].Value=0;ChangeImageAdjustment();imageAdjustmentFields["Contrast"].Value=100;ChangeImageAdjustment();FlushImageAdjustmentPreview();using(var adjusted=Art.Render(Background,Marks))check(Math.Abs(adjusted.GetPixel(90,30).R-32)<=2,"Contrast operates around the middle tone");
        imageAdjustmentFields["Contrast"].Value=0;ChangeImageAdjustment();imageAdjustmentFields["Saturation"].Value=-100;ChangeImageAdjustment();FlushImageAdjustmentPreview();using(var adjusted=Art.Render(Background,Marks)){var pixel=adjusted.GetPixel(180,30);check(pixel.R==pixel.G&&pixel.G==pixel.B,"Saturation can remove color from the selected image");}
        imageAdjustmentFields["Saturation"].Value=0;ChangeImageAdjustment();imageAdjustmentFields["Temperature"].Value=60;ChangeImageAdjustment();FlushImageAdjustmentPreview();using(var adjusted=Art.Render(Background,Marks)){var pixel=adjusted.GetPixel(90,30);check(pixel.R>80&&pixel.B<80&&pixel.G==80,"Temperature warms red and cools blue without affecting green");}
        FinishImageAdjustment();check(undo.Count==history+1&&Object.ReferenceEquals(bytes,image.Image),"A burst of slider changes uses one Undo step and preserves immutable source bytes");
        var saved=Snapshot();using(var stream=new MemoryStream()){var xml=new System.Xml.Serialization.XmlSerializer(typeof(Project));xml.Serialize(stream,saved);stream.Position=0;var restored=(Project)xml.Deserialize(stream);check(restored.Marks[0].Temperature==60&&restored.Marks[0].ImageAdjustmentsEnabled,"Reversible adjustments persist in editable projects");}
        using(var after=Art.Render(Background,Marks)){imageBefore.Checked=true;check(Preview.GetPixel(90,30).R==80,"Before comparison shows the original image");using(var exported=Art.Render(Background,Marks))check(Art.Bytes(after).SequenceEqual(Art.Bytes(exported)),"Before is preview-only: image exports keep the real adjustments");check(Snapshot().Marks[0].Temperature==60,"Comparison never overwrites saved adjustment values");imageBefore.Checked=false;check(Preview.GetPixel(90,30).R>80,"After restores the adjusted preview");}
        using(var basic=new Bitmap(inspector.Parent.Width,inspector.Parent.Height)){inspector.Parent.DrawToBitmap(basic,new Rectangle(Point.Empty,basic.Size));basic.Save(Path.Combine(folder,"properties-image-basic.png"));}advancedImageButton.PerformClick();imageAdjustmentFields["Strength"].Value=0;ChangeImageAdjustment();FinishImageAdjustment();using(var rendered=Art.Render(Background,Marks))check(rendered.GetPixel(90,30).R==80,"Advanced strength can blend back to the original");imageAdjustmentFields["Strength"].Value=100;ChangeImageAdjustment();FinishImageAdjustment();imageEnabled.Checked=false;FinishImageAdjustment();using(var rendered=Art.Render(Background,Marks))check(rendered.GetPixel(90,30).R==80,"Adjustments can be disabled without losing their stored values");imageEnabled.Checked=true;FinishImageAdjustment();
        inspector.Parent.Width=360;CloseImageEffects();OpenImageAdjustmentCard(false);Application.DoEvents();using(var frame=new Bitmap(inspector.Parent.Width,inspector.Parent.Height)){inspector.Parent.DrawToBitmap(frame,new Rectangle(Point.Empty,frame.Size));frame.Save(Path.Combine(folder,"properties-image-adjustments.png"));}
        imageBefore.Checked=true;Undo();check(effectsPane==null&&imageComparisonId==null,"Undo safely closes comparison and its pending adjustment controls");Redo();
        using(var transparent=new Bitmap(2,2,PixelFormat.Format32bppArgb)){transparent.SetPixel(0,0,Color.FromArgb(128,100,50,20));var target=new Mark {Kind="Image",W=2,H=2,Image=Art.Bytes(transparent),Brightness=10};using(var background=new Bitmap(2,2))using(var result=Art.Render(background,new List<Mark>{target})){check(Math.Abs(result.GetPixel(0,0).A-128)<=1&&result.GetPixel(1,1).A==0,"Color adjustments preserve partial and full transparency");}}
        using(var sample=new Bitmap(32,32)){var random=new Random(734);for(int y=0;y<32;y++)for(int x=0;x<32;x++)sample.SetPixel(x,y,Color.FromArgb(random.Next(256),random.Next(256),random.Next(256)));var adjusted=new Mark {Kind="Image",Image=Art.Bytes(sample),W=32,H=32,Exposure=.35f,Contrast=18,Brightness=-3,Saturation=22,Temperature=-30,AdjustmentStrength=65};using(var blank=new Bitmap(32,32))using(var actual=Art.Render(blank,new List<Mark>{adjusted}))using(var expected=new Bitmap(32,32)){
            using(var graphics=Graphics.FromImage(expected))using(var attributes=new ImageAttributes()){attributes.SetColorMatrix(Art.ImageAdjustmentMatrix(adjusted));graphics.DrawImage(sample,new Rectangle(0,0,32,32),0,0,32,32,GraphicsUnit.Pixel,attributes);}int difference=0;for(int y=0;y<32;y++)for(int x=0;x<32;x++){var a=actual.GetPixel(x,y);var b=expected.GetPixel(x,y);difference=Math.Max(difference,Math.Max(Math.Abs(a.R-b.R),Math.Max(Math.Abs(a.G-b.G),Math.Abs(a.B-b.B))));}check(difference<=2,"Fast color lookup matches a native color-matrix reference within rounding tolerance");
        }}
        LoadImageDocument(Blank(3840,2160),"4K image adjustment benchmark");image=Marks[0];OpenImageAdjustmentCard(false);var clock=Stopwatch.StartNew();for(int i=0;i<8;i++){imageAdjustmentFields["Exposure"].Value=i*.1m;ChangeImageAdjustment();}clock.Stop();double input=clock.Elapsed.TotalMilliseconds;clock.Restart();FlushImageAdjustmentPreview();clock.Stop();double render=clock.Elapsed.TotalMilliseconds;FinishImageAdjustment();check(image.Exposure==.7f,"Rapid input coalesces to the latest exact adjustment value");check(Art.ImageToneCacheBytes<=40L*1024*1024,"Adjusted image caching stays within its 40 MiB limit");report.Add("4K adjustment benchmark: 8 input changes "+input.ToString("F2")+" ms; one full-resolution preview "+render.ToString("F2")+" ms. Lookup-table color transform; no downscaled export.");File.WriteAllText(Path.Combine(folder,"image-adjustment-benchmark.txt"),report.Last());CloseImageEffects();Hide();LoadBitmap(Blank(100,100),"Test");
    }
}
}
