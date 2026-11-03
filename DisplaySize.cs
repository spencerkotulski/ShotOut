using System;using System.Collections.Generic;using System.Drawing;using System.Linq;using System.Windows.Forms;
namespace ShotOut {
partial class SettingsDialog {
    ChoiceButton uiSizeChoice;
    void AddDisplayOptions(FlowLayoutPanel page){Toggle(page,"toolLabels","Show names beside toolbox icons",Value.ShowToolLabels);SettingsHelp(page,"Use Ctrl+K to search for tools by name or shortcut. Labels use a scrolling single-column toolbox.",60);AddTitle(page,"UI size");uiSizeChoice=new ChoiceButton{Width=210,Height=36,Font=Font,ForeColor=ForeColor,BackColor=Color.FromArgb(43,46,54),AccessibleName="UI size percentage"};for(int n=75;n<=200;n+=5)uiSizeChoice.Items.Add(n+"%");uiSizeChoice.SelectedItem=Value.UiSize+"%";page.Controls.Add(uiSizeChoice);SettingsHelp(page,"Default: 75% (the original toolbar size). Ctrl + plus enlarges toolbars, annotation properties, layers and image-effects controls; Ctrl + minus reduces them, in 5% steps. Both the main keyboard and number pad work. Image zoom and annotation sizes are unchanged. Large toolbars scroll when space is limited.",105);}
}
partial class Editor {
    sealed class ToolbarMetrics {internal Size Size;internal Point Location;internal Padding Padding,Margin;internal Font Font;}
    readonly Dictionary<Control,ToolbarMetrics> toolbarMetrics=new Dictionary<Control,ToolbarMetrics>();readonly List<Font> toolbarFonts=new List<Font>();Control[] uiSizeRoots;Control uiHeader;bool applyingUiSize;float appliedUiScale=1;
    float ToolbarScale {get{return Preferences.Current.UiSize/75f;}}
    int UiPixels(int pixels){return Math.Max(1,(int)Math.Round(pixels*ToolbarScale));}
    Padding UiPadding(Padding p){return new Padding((int)Math.Round(p.Left*ToolbarScale),(int)Math.Round(p.Top*ToolbarScale),(int)Math.Round(p.Right*ToolbarScale),(int)Math.Round(p.Bottom*ToolbarScale));}
    void InitializeUiSize(Control header,FlowLayoutPanel primary,Control documentBar){
        WireToolbarBlankDrag(header);uiHeader=header;primary.Dock=DockStyle.Fill;primary.AutoScroll=true;primary.FlowDirection=FlowDirection.LeftToRight;primary.WrapContents=false;
        uiSizeRoots=new[]{header,toolRail,documentBar,inspector.Parent};foreach(var root in uiSizeRoots)RememberToolbarMetrics(root);
        header.Layout+=delegate{AlignHeaderLogo();};primary.Layout+=delegate{AlignHeaderLogo();};
        Disposed+=delegate{foreach(var font in toolbarFonts)font.Dispose();};
    }
    void RememberToolbarMetrics(Control control){toolbarMetrics[control]=new ToolbarMetrics{Size=control.Size,Location=control.Location,Padding=control.Padding,Margin=control.Margin,Font=control.Font};foreach(Control child in control.Controls)RememberToolbarMetrics(child);}
    void ApplyUiSize(){
        if(uiSizeRoots==null||applyingUiSize)return;applyingUiSize=true;SuspendLayout();foreach(var root in uiSizeRoots)root.SuspendLayout();
        int previousEffectsWidth=effectsPane==null?0:inspector.Parent.Width;
        foreach(var dead in toolbarMetrics.Keys.Where(c=>c.IsDisposed).ToArray())toolbarMetrics.Remove(dead);
        try{foreach(var pair in toolbarMetrics){var c=pair.Key;if(c.IsDisposed)continue;var b=pair.Value;
            c.Padding=UiPadding(b.Padding);c.Margin=UiPadding(b.Margin);
            if(c.Dock==DockStyle.None)c.Bounds=new Rectangle((int)Math.Round(b.Location.X*ToolbarScale),(int)Math.Round(b.Location.Y*ToolbarScale),UiPixels(b.Size.Width),UiPixels(b.Size.Height));
            else if(c.Dock==DockStyle.Left||c.Dock==DockStyle.Right)c.Width=UiPixels(b.Size.Width);else if(c.Dock==DockStyle.Top||c.Dock==DockStyle.Bottom)c.Height=UiPixels(b.Size.Height);
            var font=toolbarFonts.FirstOrDefault(f=>f.Name==b.Font.Name&&Math.Abs(f.Size-b.Font.Size*ToolbarScale)<.001f&&f.Style==b.Font.Style&&f.Unit==b.Font.Unit);if(font==null){font=new Font(b.Font.FontFamily,b.Font.Size*ToolbarScale,b.Font.Style,b.Font.Unit);toolbarFonts.Add(font);}c.Font=font;var button=c as ModernButton;if(button!=null)button.UiScale=ToolbarScale;var number=c as NumberField;if(number!=null)number.UiScale=ToolbarScale;var check=c as ThemeCheckBox;if(check!=null)check.UiScale=ToolbarScale;var slider=c as OpacitySlider;if(slider!=null)slider.UiScale=ToolbarScale;var adjustment=c as AdjustmentField;if(adjustment!=null)adjustment.UiScale=ToolbarScale;
        }if(ToolbarScale>1)uiHeader.Height+=SystemInformation.HorizontalScrollBarHeight+8;}finally{foreach(var root in uiSizeRoots)root.ResumeLayout(true);ResumeLayout(true);applyingUiSize=false;}
        layers.ItemHeight=UiPixels(44);FitInspectorControls();toolRailLayoutKey=null;ReflowToolbar();
        if(effectsPane!=null){effectsDefaultWidth=UiPixels(268);inspector.Parent.Width=Math.Max(effectsDefaultWidth,(int)Math.Round(previousEffectsWidth*ToolbarScale/appliedUiScale));SizeAdjustmentPane();}else UpdateInspector();
        appliedUiScale=ToolbarScale;AlignHeaderLogo();Invalidate(true);
    }
    static bool ToolbarDragSurface(Control control){return control is Panel||control is Label||control is PictureBox;}
    void WireToolbarBlankDrag(Control root){foreach(Control child in root.Controls){if(ToolbarDragSurface(child)){child.MouseDown+=delegate(object sender,MouseEventArgs e){if(e.Button==MouseButtons.Left){ReleaseCapture();SendMessage(Handle,0xA1,new IntPtr(2),IntPtr.Zero);}};child.DoubleClick+=delegate{ToggleMaximize();};WireToolbarBlankDrag(child);}}}
    void AlignHeaderLogo(){if(brandLogo==null||brandLogo.Parent==null||captureSplit==null||captureSplit.Parent==null)return;var center=brandLogo.Parent.PointToClient(captureSplit.Parent.PointToScreen(new Point(captureSplit.Left,captureSplit.Top+captureSplit.Height/2)));brandLogo.Location=new Point((brandLogo.Parent.ClientSize.Width-brandLogo.Width)/2,center.Y-brandLogo.Height/2);}
    void ScaleEffectsControls(){if(effectsPane==null)return;effectsPane.Font=Font;RememberToolbarMetrics(effectsPane);ApplyUiSize();}
    internal bool HandleUiSizeKey(Keys key){
        Keys modifiers=key&Keys.Modifiers,code=key&Keys.KeyCode;bool plus=code==Keys.Oemplus||code==Keys.Add,minus=code==Keys.OemMinus||code==Keys.Subtract;
        if((modifiers!=Keys.Control&&modifiers!=(Keys.Control|Keys.Shift))||(!plus&&!minus))return false;
        int old=Preferences.Current.UiSize;Preferences.Current.UiSize=Math.Max(75,Math.Min(200,old+(plus?5:-5)));ApplyUiSize();
        if(!Preferences.Testing&&old!=Preferences.Current.UiSize)try{Preferences.Current.Save(Preferences.FilePath);}catch(Exception ex){UpdateStatus("UI size changed, but could not save settings: "+ex.Message);return true;}
        UpdateStatus("UI size: "+Preferences.Current.UiSize+"%");return true;
    }
}
}
