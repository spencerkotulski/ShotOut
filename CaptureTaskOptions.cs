using System;using System.Collections.Generic;using System.Drawing;using System.Globalization;using System.IO;using System.Linq;using System.Windows.Forms;
namespace ShotOut {
public class CaptureTaskPreset {
    public string Name;public bool Save,SaveAs,Folder,Viewer,Copy=true,CopyPath,Editor,Preview=true,Sound;
    public static List<CaptureTaskPreset> Defaults(){return new List<CaptureTaskPreset>{new CaptureTaskPreset{Name="Custom"},new CaptureTaskPreset{Name="Quick copy"},new CaptureTaskPreset{Name="Save quietly",Save=true,Copy=false,Preview=false},new CaptureTaskPreset{Name="Capture and edit",Editor=true}};}
}
static class CaptureFileNames {
    internal static string Extension(string format){return format=="JPEG"?".jpg":format=="BMP"?".bmp":".png";}
    internal static string Name(string pattern,DateTime utc){var time=utc.ToLocalTime();return pattern.Replace("{date}",time.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)).Replace("{time}",time.ToString("HHmmss",CultureInfo.InvariantCulture)).Replace("{milliseconds}",time.ToString("fff",CultureInfo.InvariantCulture));}
    internal static void Validate(string pattern,string format){
        if(format!="PNG"&&format!="JPEG"&&format!="BMP")throw new ArgumentException("Choose PNG, JPEG or BMP.");
        string name=Name(pattern??"",DateTime.UtcNow);
        if(String.IsNullOrWhiteSpace(name)||name.Length>120||name.IndexOfAny(Path.GetInvalidFileNameChars())>=0||name.IndexOf('{')>=0||name.IndexOf('}')>=0||name.EndsWith(".")||name.EndsWith(" "))throw new ArgumentException("Use a filename pattern up to 120 characters, with {date}, {time}, or {milliseconds}. Do not include folders or an extension.");
        string stem=name.Split('.')[0].ToUpperInvariant();if(new[]{"CON","PRN","AUX","NUL","COM1","COM2","COM3","COM4","COM5","COM6","COM7","COM8","COM9","LPT1","LPT2","LPT3","LPT4","LPT5","LPT6","LPT7","LPT8","LPT9"}.Contains(stem))throw new ArgumentException("That filename is reserved by Windows.");
    }
}
partial class SettingsDialog {
    TextBox screenshotPatternEdit;ChoiceButton screenshotFormatChoice,capturePresetChoice;Label filenameExample;string editingCapturePreset;
    static readonly string[] taskKeys={"afterSave","afterSaveAs","afterFolder","afterViewer","autocopy","copyPath","openaftercapture","thumbnail","captureSound"};
    bool[] ReadPreset(CaptureTaskPreset p){return new[]{p.Save,p.SaveAs,p.Folder,p.Viewer,p.Copy,p.CopyPath,p.Editor,p.Preview,p.Sound};}
    void RememberCapturePreset(){var preset=Value.CapturePresets.First(x=>x.Name==editingCapturePreset);var v=taskKeys.Select(k=>checks[k].Checked).ToArray();preset.Save=v[0];preset.SaveAs=v[1];preset.Folder=v[2];preset.Viewer=v[3];preset.Copy=v[4];preset.CopyPath=v[5];preset.Editor=v[6];preset.Preview=v[7];preset.Sound=v[8];}
    void AddCapturePresets(FlowLayoutPanel page){
        editingCapturePreset=Value.CapturePreset;AddTitle(page,"Task preset");capturePresetChoice=new ChoiceButton{Width=350,Height=36,Font=Font,ForeColor=ForeColor,BackColor=Color.FromArgb(43,46,54),AccessibleName="After-capture preset"};capturePresetChoice.Items.AddRange(Value.CapturePresets.Select(x=>x.Name));capturePresetChoice.SelectedItem=editingCapturePreset;
        capturePresetChoice.SelectedIndexChanged+=delegate{RememberCapturePreset();editingCapturePreset=capturePresetChoice.SelectedItem;var flags=ReadPreset(Value.CapturePresets.First(x=>x.Name==editingCapturePreset));for(int i=0;i<taskKeys.Length;i++)checks[taskKeys[i]].Checked=false;for(int i=0;i<taskKeys.Length;i++)checks[taskKeys[i]].Checked=flags[i];};page.Controls.Add(capturePresetChoice);
        SettingsHelp(page,"Switch presets to apply their tasks. Each preset remembers your checkbox changes when you switch or save settings. Folder, filename, format and sound options are shared.",66);
    }
    void AddAfterCaptureSaveOptions(FlowLayoutPanel page){
        AddTitle(page,"Save options");AddScreenshotFolderSetting(page);Toggle(page,"monthly","Organize screenshots by month (YYYY-MM)",Value.MonthlyScreenshots);
        page.Controls.Add(new Label{Text="Filename pattern (without extension)",Width=550,Height=28});screenshotPatternEdit=new ThemedTextBox{Text=Value.ScreenshotPattern,Width=530,BackColor=Color.FromArgb(43,46,54),ForeColor=ForeColor,BorderStyle=BorderStyle.FixedSingle};page.Controls.Add(screenshotPatternEdit);
        page.Controls.Add(new Label{Text="Image format",Width=550,Height=28});screenshotFormatChoice=new ChoiceButton{Width=210,Height=36,Font=Font,ForeColor=ForeColor,BackColor=Color.FromArgb(43,46,54),AccessibleName="Screenshot image format"};screenshotFormatChoice.Items.AddRange(new[]{"PNG","JPEG","BMP"});screenshotFormatChoice.SelectedItem=Value.ScreenshotFormat;page.Controls.Add(screenshotFormatChoice);
        captureQualityField=QualityRow(page,"After-capture JPEG quality",Value.CaptureJpegQuality);SettingsHelp(page,"Applies to JPEG files saved by after-capture tasks, including Save as. PNG and BMP stay full quality. The captured image and clipboard are unchanged. JPEG at 100% still uses lossy compression.",75);
        filenameExample=new Label{Width=550,Height=44,ForeColor=Color.FromArgb(159,169,186)};page.Controls.Add(filenameExample);EventHandler refresh=delegate{filenameExample.Text="Example: "+CaptureFileNames.Name(screenshotPatternEdit.Text,DateTime.UtcNow)+CaptureFileNames.Extension(screenshotFormatChoice.SelectedItem);};screenshotPatternEdit.TextChanged+=refresh;screenshotFormatChoice.SelectedIndexChanged+=refresh;refresh(null,EventArgs.Empty);
        SettingsHelp(page,"Tokens: {date}, {time}, {milliseconds}. Existing files receive a numbered suffix. PNG preserves screenshot quality; JPEG uses smaller, lossy files; BMP uses uncompressed files.",65);
    }
    void SaveAfterCaptureOptions(){Value.CaptureJpegQuality=(int)captureQualityField.Value;Value.ExportJpegQuality=(int)exportQualityField.Value;RememberCapturePreset();Value.CapturePreset=editingCapturePreset;Value.ScreenshotPattern=screenshotPatternEdit.Text;Value.ScreenshotFormat=screenshotFormatChoice.SelectedItem;Value.AfterCaptureCopyPath=checks["copyPath"].Checked;Value.ShowCaptureThumbnail=checks["thumbnail"].Checked;Value.UiSize=Int32.Parse(uiSizeChoice.SelectedItem.TrimEnd('%'));}
}
}
