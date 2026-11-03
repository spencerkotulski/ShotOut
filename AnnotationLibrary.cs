using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Serialization;
namespace ShotOut {
public class AnnotationPreset {
    public string Name,Kind;
    public string FontFamily="Segoe UI",ArrowStyle="Modern";
    public int Color,FillColor,Alignment;
    public float Width,Size,Radius,FillOpacity,ShadowSize,ShadowIntensity,Magnification;
    public bool Fill,Bold,Italic,Underline,Shadow,Tail,MagnifyLinked;
    public static AnnotationPreset Capture(string name,Mark mark){return new AnnotationPreset {Name=name,ArrowStyle=mark.ArrowStyle,FontFamily=mark.FontFamily,Kind=mark.Kind=="Rounded"?"Rectangle":mark.Kind,Color=mark.Color,FillColor=mark.FillColor,Alignment=RichLayout.Alignment(mark),Width=mark.Width,Size=mark.Size,Radius=mark.Kind=="Rounded"?12:mark.Radius,FillOpacity=mark.FillOpacity,ShadowSize=mark.ShadowSize,ShadowIntensity=mark.ShadowIntensity,Magnification=mark.Magnification,Fill=mark.Fill,Bold=mark.Bold,Italic=mark.Italic,Underline=mark.Underline,Shadow=mark.Shadow,Tail=mark.Tail,MagnifyLinked=mark.MagnifyLinked};}
    public void Apply(Mark mark){
        mark.ArrowStyle=ArrowStyle;mark.FontFamily=FontFamily;mark.Color=Color;mark.FillColor=FillColor;mark.Width=Width;mark.Size=Size;mark.Radius=Radius;mark.FillOpacity=FillOpacity;mark.Shadow=mark.Kind!="Spotlight"&&Shadow;mark.ShadowSize=ShadowSize;mark.ShadowIntensity=ShadowIntensity;mark.Fill=Fill;mark.Tail=Tail;mark.TextAlignment=Alignment;mark.Magnification=Magnification;mark.MagnifyLinked=MagnifyLinked;
        RichLayout.ApplyStyle(mark,Bold,Italic,Underline);
    }
    public override string ToString(){return Name+" · "+Editor.ToolDisplayName(Kind);}
}
public class AnnotationLibrary {
    public List<AnnotationPreset> Styles=new List<AnnotationPreset>();
    public List<int> FavoriteColors=new List<int>();public List<int> RecentColors=new List<int>();
    static AnnotationLibrary current;
    public static string FilePath {get{return Path.Combine(Path.GetDirectoryName(Preferences.FilePath),"annotation-styles.xml");}}
    public static AnnotationLibrary Current {get{if(current==null){try{current=Preferences.Testing?new AnnotationLibrary():Read(FilePath);}catch(Exception ex){current=new AnnotationLibrary();if(!Preferences.Testing)MessageBox.Show("Your saved styles and colors could not be loaded: "+ex.Message,"ShotOut styles");}}return current;}}
    internal static void ResetForTests(){current=new AnnotationLibrary();}
    public static AnnotationLibrary Read(string path){
        if(!File.Exists(path))return new AnnotationLibrary();
        using(var stream=File.OpenRead(path)){
            var library=(AnnotationLibrary)new XmlSerializer(typeof(AnnotationLibrary)).Deserialize(stream);
            library.Styles=(library.Styles??new List<AnnotationPreset>()).Where(style=>style!=null&&!String.IsNullOrWhiteSpace(style.Name)&&Editor.PresetSupported(style.Kind)).ToList();
            library.FavoriteColors=(library.FavoriteColors??new List<int>()).Select(color=>System.Drawing.Color.FromArgb(color).ToArgb()|unchecked((int)0xff000000)).Distinct().ToList();
            library.RecentColors=(library.RecentColors??new List<int>()).Select(color=>color|unchecked((int)0xff000000)).Distinct().Take(10).ToList();
            foreach(var style in library.Styles){style.Width=Math.Max(0,Math.Min(80,Finite(style.Width,4)));style.Size=Math.Max(8,Math.Min(500*4f/3,Finite(style.Size,22)));style.Radius=Math.Max(0,Math.Min(500,Finite(style.Radius,0)));style.FillOpacity=Math.Max(0,Math.Min(100,Finite(style.FillOpacity,100)));style.ShadowSize=Math.Max(0,Math.Min(64,Finite(style.ShadowSize,8)));style.ShadowIntensity=Math.Max(0,Math.Min(100,Finite(style.ShadowIntensity,40)));style.Magnification=Math.Max(1,Math.Min(8,Finite(style.Magnification,2)));style.Alignment=Math.Max(0,Math.Min(2,style.Alignment));}
            return library;
        }
    }
    static float Finite(float value,float fallback){return float.IsNaN(value)||float.IsInfinity(value)?fallback:value;}
    public void Save(string path){Directory.CreateDirectory(Path.GetDirectoryName(path));string temp=path+".tmp";try{using(var stream=File.Create(temp))new XmlSerializer(typeof(AnnotationLibrary)).Serialize(stream,this);if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}finally{if(File.Exists(temp))File.Delete(temp);}}
    public static void Persist(){if(!Preferences.Testing)Current.Save(FilePath);}
    public void RememberColor(Color color){int value=Color.FromArgb(255,color.R,color.G,color.B).ToArgb();RecentColors.Remove(value);RecentColors.Insert(0,value);if(RecentColors.Count>10)RecentColors.RemoveRange(10,RecentColors.Count-10);}
    public bool AddFavorite(Color color){int argb=System.Drawing.Color.FromArgb(255,color.R,color.G,color.B).ToArgb();if(FavoriteColors.Contains(argb))return false;FavoriteColors.Add(argb);return true;}
}
partial class Editor {
    Control presetRow,arrangeRow;
    ContextMenuStrip annotationStylesPopup;
    readonly Dictionary<string,int> presetAlignments=new Dictionary<string,int>();
    internal static bool PresetSupported(string kind){return kind!=null&&(Art.IsArrow(kind)||new[]{"Rectangle","Rounded","Ellipse","Pen","Text","Balloon","Step","Highlight","Redact","Blur","Pixelate","Magnify","Spotlight","Eraser","Bucket","Emoji","Cursor"}.Contains(kind));}
    static string PresetFamily(string kind){return Art.IsArrow(kind)?"Arrow":kind=="Rounded"?"Rectangle":kind;}
    int NewTextAlignment(string kind){int value;return presetAlignments.TryGetValue(kind,out value)?value:kind=="Balloon"?1:kind=="Text"?alignmentChoiceIndex:0;}
    void RememberTextAlignment(){if(Active==null&&(tool=="Text"||tool=="Balloon"))presetAlignments[tool]=alignmentChoiceIndex;}
    void AddDocumentationInspector(){presetRow=Row();presetRow.Controls.Add(ActionButton("Annotation styles ▾",null,"Save or apply reusable annotation styles",ShowAnnotationStyles,210));inspector.Controls.Add(presetRow);arrangeRow=Row();arrangeRow.Controls.Add(ActionButton("Align && distribute ▾",null,"Align selected objects or distribute their spacing",ShowArrangeMenu,210));inspector.Controls.Add(arrangeRow);}
    int UpdateDocumentationInspector(string kind){bool styles=PresetSupported(kind)||kind=="Select",arrange=LayoutUnits().Count>=2;presetRow.Visible=styles;arrangeRow.Visible=arrange;return (styles?47:0)+(arrange?47:0);}
    void ShowAnnotationStyles(){
        if(annotationStylesPopup==null)annotationStylesPopup=PopupLifetime.Own(this,DarkMenu());var menu=annotationStylesPopup;PopupLifetime.Reset(menu);string kind=Active==null?tool:Active.Kind;var library=AnnotationLibrary.Current;
        foreach(var preset in library.Styles.OrderBy(style=>style.Name,StringComparer.CurrentCultureIgnoreCase)){var item=menu.Items.Add(preset.ToString().Replace("&","&&"),null,delegate{PopupLifetime.AfterClick(this,delegate{Guard(delegate{ApplyAnnotationPreset(preset);});});});item.Enabled=Active==null||PresetFamily(Active.Kind)==PresetFamily(preset.Kind);}
        if(library.Styles.Count==0)menu.Items.Add("No saved styles yet").Enabled=false;
        menu.Items.Add(new ToolStripSeparator());menu.Items.Add("Save current style…",null,delegate{PopupLifetime.AfterClick(this,delegate{Guard(SaveAnnotationPreset);});}).Enabled=PresetSupported(kind);
        menu.Items.Add("Manage styles…",null,delegate{PopupLifetime.AfterClick(this,ManageAnnotationPresets);}).Enabled=library.Styles.Count>0;menu.Show(presetRow,0,presetRow.Height);
    }
    void SaveAnnotationPreset(){
        FinishStrokeAdjustment();string kind=Active==null?tool:Active.Kind;if(!PresetSupported(kind))return;string name=Prompt.Ask(this,"Save style — same name replaces the existing style",kind+" style",false);if(name==null||String.IsNullOrWhiteSpace(name))return;name=name.Trim();if(name.Length>80)throw new ArgumentException("Use a style name of 80 characters or fewer.");
        var source=Active??NewMark(kind,PointF.Empty);var preset=AnnotationPreset.Capture(name,source);var library=AnnotationLibrary.Current;var previous=library.Styles.ToList();library.Styles.RemoveAll(style=>String.Equals(style.Name,name,StringComparison.OrdinalIgnoreCase));library.Styles.Add(preset);try{AnnotationLibrary.Persist();}catch{library.Styles=previous;throw;}UpdateStatus("Saved annotation style: "+name);
    }
    void ApplyAnnotationPreset(AnnotationPreset preset){
        FinishStrokeAdjustment();var chosen=SelectedObjects();if(Active==null){SetTool(preset.Kind);var defaults=NewMark(preset.Kind,PointF.Empty);preset.Apply(defaults);if(Art.IsArrow(preset.Kind))defaultArrowStyle=defaults.ArrowStyle;bool was=syncing;syncing=true;try{ink=Color.FromArgb(defaults.Color);fill=Color.FromArgb(defaults.FillColor);colorButton.BackColor=ink;fillButton.BackColor=fill;thickness.Minimum=OptionalOutline(preset.Kind)?0:1;thickness.Value=Math.Max(thickness.Minimum,(decimal)defaults.Width);fontSize.Value=TextFonts.Points(defaults.Size);manualTextPoints=fontSize.Value;SyncFontChoice(defaults.FontFamily);fillBox.Checked=defaults.Fill;fillOpacity.Value=(decimal)defaults.FillOpacity;cornerRadius.Value=(decimal)defaults.Radius;tailBox.Checked=defaults.Tail;boldBox.Checked=defaults.Bold;italicBox.Checked=defaults.Italic;underlineBox.Checked=defaults.Underline;shadowBox.Checked=defaults.Shadow;shadowSize.Value=(decimal)defaults.ShadowSize;shadowIntensity.Value=(decimal)defaults.ShadowIntensity;SyncMagnifySettings(defaults);SyncAlignment(defaults);presetAlignments[preset.Kind]=defaults.TextAlignment;if(preset.Kind=="Highlight")highlightColor=ink;else ordinaryColor=ink;if(HasWeight(preset.Kind))rememberedWeights[WeightFamily(preset.Kind)]=thickness.Value;}finally{syncing=was;}UpdateInspector();UpdateStatus("Style ready: "+preset.Name);return;}
        var targets=chosen.Where(mark=>PresetFamily(mark.Kind)==PresetFamily(preset.Kind)).ToList();if(targets.Count==0)return;Push();foreach(var mark in targets){float oldSize=mark.Size;if(Art.IsArrow(mark.Kind)&&mark.Kind!=preset.Kind){bool curved=IsCurved(mark);mark.Kind=preset.Kind;if(IsCurved(mark)&&!curved)InitCurve(mark);}preset.Apply(mark);if(mark.Kind=="Text"||mark.Kind=="Balloon"||mark.Kind=="Emoji")GrowTextBox(mark,oldSize);}EnsureCanvasFitsMarks();SyncStyle();RefreshImage();
    }
    void ManageAnnotationPresets(){
        using(var dialog=new Form {Text="Annotation styles",ClientSize=new Size(420,340),StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,BackColor=panel,ForeColor=text,Font=Font}){
            var list=new ListBox {Location=new Point(16,16),Size=new Size(388,245),BackColor=panel,ForeColor=text,BorderStyle=BorderStyle.FixedSingle};dialog.Controls.Add(list);Action reload=delegate{list.Items.Clear();foreach(var style in AnnotationLibrary.Current.Styles)list.Items.Add(style);if(list.Items.Count>0)list.SelectedIndex=0;};reload();
            var rename=ActionButton("Rename…",null,"Rename selected style",delegate{Guard(delegate{var style=list.SelectedItem as AnnotationPreset;if(style==null)return;string name=Prompt.Ask(dialog,"Rename style",style.Name,false);if(String.IsNullOrWhiteSpace(name))return;name=name.Trim();if(name.Length>80||AnnotationLibrary.Current.Styles.Any(other=>other!=style&&String.Equals(other.Name,name,StringComparison.OrdinalIgnoreCase)))throw new ArgumentException("Choose a unique name of 80 characters or fewer.");string old=style.Name;style.Name=name;try{AnnotationLibrary.Persist();}catch{style.Name=old;throw;}reload();});},118);rename.Location=new Point(16,280);dialog.Controls.Add(rename);
            var remove=ActionButton("Delete",null,"Delete selected style",delegate{Guard(delegate{var style=list.SelectedItem as AnnotationPreset;if(style==null)return;var previous=AnnotationLibrary.Current.Styles.ToList();AnnotationLibrary.Current.Styles.Remove(style);try{AnnotationLibrary.Persist();}catch{AnnotationLibrary.Current.Styles=previous;throw;}reload();});},118);remove.Location=new Point(148,280);dialog.Controls.Add(remove);
            var close=ActionButton("Done",null,"Close styles",delegate{dialog.Close();},118);close.Location=new Point(280,280);dialog.Controls.Add(close);dialog.CancelButton=close;dialog.ShowDialog(this);
        }
    }
}
}
