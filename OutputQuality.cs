using System;using System.Drawing;using System.Drawing.Imaging;using System.IO;using System.Linq;using System.Collections.Generic;using System.Windows.Forms;
namespace ShotOut {
static class JpegOutput {
    static readonly ImageCodecInfo Codec=ImageCodecInfo.GetImageEncoders().First(c=>c.FormatID==ImageFormat.Jpeg.Guid);
    internal static void Save(Bitmap image,Stream stream,int quality){using(var options=new EncoderParameters(1)){options.Param[0]=new EncoderParameter(System.Drawing.Imaging.Encoder.Quality,(long)Math.Max(1,Math.Min(100,quality)));image.Save(stream,Codec,options);}}
}
partial class SettingsDialog {
    NumberField captureQualityField,exportQualityField;
    NumberField QualityRow(FlowLayoutPanel page,string label,int value){page.Controls.Add(new Label {Text=label+" · %",Width=550,Height=28});var row=new FlowLayoutPanel {Width=550,Height=44,WrapContents=false};var slider=new OpacitySlider {Value=value,Width=300,Height=34,BackColor=BackColor,AccessibleName=label+" slider"};var number=new NumberField {Minimum=1,Maximum=100,Value=value,Width=84,Height=34,BackColor=Color.FromArgb(43,46,54),ForeColor=ForeColor,AccessibleName=label+" percent"};slider.ValueChanged+=delegate{number.Value=Math.Max(1,slider.Value);slider.Value=number.Value;slider.Invalidate();};number.ValueChanged+=delegate{slider.Value=number.Value;slider.Invalidate();};row.Controls.Add(slider);row.Controls.Add(number);page.Controls.Add(row);return number;}
}
partial class Editor {
    public void VerifyOutputQuality(List<string> report,string folder){
        var original=Preferences.Current.Clone();try{
            var prefs=new Preferences();if(prefs.CaptureJpegQuality!=100||prefs.ExportJpegQuality!=100)throw new Exception("Incorrect quality defaults");prefs.CaptureJpegQuality=80;prefs.ExportJpegQuality=90;string xml=Path.Combine(folder,"quality-settings.xml");prefs.Save(xml);var read=Preferences.Read(xml);if(read.CaptureJpegQuality!=80||read.ExportJpegQuality!=90||prefs.Clone().CaptureJpegQuality!=80)throw new Exception("Quality persistence failed");
            var image=Blank(320,240);var random=new Random(42);for(int y=0;y<image.Height;y++)for(int x=0;x<image.Width;x++)image.SetPixel(x,y,Color.FromArgb(random.Next(256),random.Next(256),random.Next(256)));LoadBitmap(image,"Quality fixture");
            string high=Path.Combine(folder,"quality100.jpg"),low=Path.Combine(folder,"quality80.jpg"),png=Path.Combine(folder,"quality.png");Preferences.Current.ExportJpegQuality=100;WriteCurrentImage(high);Preferences.Current.ExportJpegQuality=80;WriteCurrentImage(low);if(new FileInfo(high).Length<=new FileInfo(low).Length)throw new Exception("JPEG quality did not affect compression");using(var decoded=Image.FromFile(low))if(decoded.Size!=Background.Size)throw new Exception("Quality changed resolution");WriteCurrentImage(png,1);using(var decoded=new Bitmap(png))if(decoded.GetPixel(42,53)!=Background.GetPixel(42,53))throw new Exception("PNG pixels changed");
            string capture=Path.Combine(folder,"capture90.jpg"),expected=Path.Combine(folder,"expected90.jpg");Preferences.Current.CaptureJpegQuality=90;Preferences.Current.AfterCaptureSave=true;Preferences.Current.AfterCaptureSaveAs=true;Preferences.Current.AfterCaptureOpenFolder=false;Preferences.Current.AfterCaptureOpenViewer=false;Preferences.Current.AfterCaptureCopyPath=false;AfterCaptureSavePicker=path=>capture;RunAfterCaptureTasks();WriteCurrentImage(expected,90);if(!File.ReadAllBytes(capture).SequenceEqual(File.ReadAllBytes(expected)))throw new Exception("After-capture quality routing failed");
            report.Add("PASS Independent JPEG quality defaults/persistence, editor and after-capture routing, quality-dependent compression, unchanged image dimensions and lossless PNG pixels");
        }finally{AfterCaptureSavePicker=null;Preferences.Current=original;LoadBitmap(Blank(100,100),"Test");}
    }
}}
