using System;using System.Collections.Generic;using System.Drawing;using System.IO;using System.Linq;using System.Windows.Forms;
namespace ShotOut {
partial class PaletteDialog {
    void AddRecentColorTools(){
        foreach(Control control in Controls)if(control.Top>=386)control.Top+=72;ClientSize=new Size(ClientSize.Width,ClientSize.Height+72);
        Controls.Add(new Label{Text="Recent colors",Location=new Point(20,386),Size=new Size(316,24),ForeColor=ForeColor});
        var row=new FlowLayoutPanel{Location=new Point(17,412),Size=new Size(324,36),WrapContents=false};Controls.Add(row);
        foreach(int value in AnnotationLibrary.Current.RecentColors){var color=Color.FromArgb(value);var button=new ModernButton{Swatch=true,BackColor=color,Size=new Size(25,28),Margin=new Padding(3),AccessibleName="Recent #"+(value&0xffffff).ToString("X6")};button.Click+=delegate{SelectPaletteColor(color);};colorTips.SetToolTip(button,button.AccessibleName);row.Controls.Add(button);}
        if(row.Controls.Count==0)row.Controls.Add(new Label{Text="Colors you apply will appear here.",Width=300,Height=30,ForeColor=Color.FromArgb(149,157,174)});
        FormClosed+=delegate{if(DialogResult!=DialogResult.OK)return;AnnotationLibrary.Current.RememberColor(Chosen);try{AnnotationLibrary.Persist();}catch(Exception ex){if(!Preferences.Testing)MessageBox.Show("Could not save recent colors: "+ex.Message,"ShotOut colors");}};
    }
}
partial class Editor {
    Label saveHint;bool documentCopied;Panel firstUseHint;
    string SaveHintText(){if(welcomeRequested)return "";bool saved=!String.IsNullOrEmpty(imageSavePath)||!String.IsNullOrEmpty(projectSavePath);if(saved)return dirty?"Unsaved changes":!String.IsNullOrEmpty(projectSavePath)?"Project saved":"Image saved";return documentCopied?"Copied · not saved":"Not saved";}
    void InstallSaveHint(Control bar){saveHint=new Label{Dock=DockStyle.Right,Width=180,TextAlign=ContentAlignment.MiddleCenter,AutoEllipsis=true,Cursor=Cursors.Hand,AccessibleName="Document save status"};saveHint.Click+=delegate{if(!welcomeRequested)Guard(delegate{SaveCurrentDocument();});};bar.Controls.Add(saveHint);}
    void UpdateSaveHint(){if(saveHint==null)return;saveHint.Text=SaveHintText();bool saved=saveHint.Text=="Image saved"||saveHint.Text=="Project saved";saveHint.ForeColor=saved?Color.FromArgb(111,210,166):Color.FromArgb(241,199,115);saveHint.AccessibleDescription=saveHint.Text;string path=projectSavePath??imageSavePath;tips.SetToolTip(saveHint,(String.IsNullOrEmpty(path)?"This image has no saved file.":path)+(dirty?"\nThere are unsaved changes.":"")+"\nClick to save · Ctrl+S");}
    static readonly Dictionary<string,string> firstUseTips=new Dictionary<string,string>{{"Clone stamp","Alt+click to sample a clean area, then drag to paint. The dim brush preview shows the pixels you will copy."},{"Text","Click to type. Shift+Enter adds a line; Enter finishes. Double-click existing text to edit it."},{"Crop","Drag or adjust the crop bounds, then press Enter to apply. Escape cancels."},{"Redact","Use a solid block to hide private details. Share the exported image; editable projects retain the original pixels."},{"Hand","Drag to pan around the image. Middle-mouse dragging also pans while using other tools."},{"Arrow","Drag to draw an arrow. Adjust its style and thickness in the annotation pane."}};
    void HideFirstUseHint(){if(firstUseHint==null)return;var panel=firstUseHint;firstUseHint=null;View.Controls.Remove(panel);panel.Dispose();}
    void PersistHintPreferences(){if(Preferences.Testing)return;try{Preferences.Current.Save(Preferences.FilePath);}catch(Exception ex){UpdateStatus("Could not save hint preferences: "+ex.Message);}}
    void ShowFirstUseHint(bool force=false){
        HideFirstUseHint();var p=Preferences.Current;string key=Family(tool),message;if(!p.ShowToolHints||(!force&&(Preferences.Testing||!Visible))||!firstUseTips.TryGetValue(key,out message)||p.SeenToolHints.Contains(key))return;
        p.SeenToolHints.Add(key);PersistHintPreferences();firstUseHint=new Panel{Width=500,Height=112,BackColor=Color.FromArgb(39,45,57),Anchor=AnchorStyles.Left|AnchorStyles.Bottom,AccessibleName="First-use hint for "+tool};var panel=firstUseHint;
        panel.Controls.Add(new Label{Text=tool+" tip",Location=new Point(12,8),Size=new Size(470,22),ForeColor=Color.White,Font=new Font(Font,FontStyle.Bold)});
        panel.Controls.Add(new Label{Text=message,Location=new Point(12,32),Size=new Size(470,42),ForeColor=text,Font=Font});
        var dismiss=new ModernButton{Text="Got it",Location=new Point(12,78),Size=new Size(86,28),BackColor=UiTheme.Accent,ForeColor=text,Font=Font};dismiss.Click+=delegate{HideFirstUseHint();};panel.Controls.Add(dismiss);
        var disable=new ModernButton{Text="Turn off tips",Location=new Point(108,78),Size=new Size(130,28),BackColor=panel.BackColor,ForeColor=muted,Font=Font};disable.Click+=delegate{p.ShowToolHints=false;PersistHintPreferences();HideFirstUseHint();};panel.Controls.Add(disable);
        View.Controls.Add(panel);panel.Location=new Point(12,Math.Max(0,View.ClientSize.Height-panel.Height-12));panel.BringToFront();
    }
}
}
