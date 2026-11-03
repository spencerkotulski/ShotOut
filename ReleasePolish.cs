using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.IO;
using System.Windows.Forms;
namespace ShotOut {
static partial class Art {
    static void PaintPresentation(Graphics graphics,Mark mark,RectangleF bounds){
        if(bounds.Width<1||bounds.Height<1)return;
        using(var path=Round(bounds,mark.Radius)){
            if(mark.Fill)using(var gradient=new LinearGradientBrush(bounds,Color.FromArgb(mark.Color),FillAlpha(mark,Color.FromArgb(mark.FillColor)),45f))graphics.FillPath(gradient,path);
            else using(var brush=new SolidBrush(Color.FromArgb(mark.Color)))graphics.FillPath(brush,path);
        }
    }
}
// The same vector drawing routine supplies the menu previews and exported arrowheads.
class ArrowChoiceButton : ChoiceButton {
    ContextMenuStrip arrowMenu;
    static bool ArrowOption(string name){return new[]{"Classic","Double","Modern","Basic","Line","Dashed arrow"}.Contains(name);}
    protected override void OnPaint(PaintEventArgs e){
        if(!ArrowOption(SelectedItem)){base.OnPaint(e);return;}
        var graphics=e.Graphics;graphics.Clear(Parent==null?BackColor:Parent.BackColor);graphics.SmoothingMode=SmoothingMode.AntiAlias;bool hover=ClientRectangle.Contains(PointToClient(Cursor.Position));using(var path=Art.Round(new RectangleF(0,0,Width-1,Height-1),8))using(var brush=new SolidBrush(hover?ControlPaint.Light(BackColor,.12f):BackColor))graphics.FillPath(brush,path);
        TextRenderer.DrawText(graphics,SelectedItem=="Dashed arrow"?"Dashed":SelectedItem,Font,new Rectangle(8,0,Width-70,Height),ForeColor,TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);DrawPreview(graphics,SelectedItem,new Rectangle(Width-62,(Height-20)/2,45,20),ForeColor);TextRenderer.DrawText(graphics,"▾",Font,new Rectangle(Width-17,0,16,Height),ForeColor,TextFormatFlags.VerticalCenter|TextFormatFlags.HorizontalCenter);
    }
    protected override void OnClick(EventArgs e){
        if(!Items.Any(ArrowOption)){base.OnClick(e);return;}
        if(arrowMenu==null)arrowMenu=PopupLifetime.Own(this,new ModernContextMenu {AutoSize=false,Padding=new Padding(2),BackColor=Color.FromArgb(38,41,48),ForeColor=ForeColor,Renderer=new ModernMenuRenderer(),ShowImageMargin=false});
        PopupLifetime.Reset(arrowMenu);
        foreach(string option in Items){string value=option;var row=new ToolStripMenuItem(option) {AutoSize=false,Width=180,Height=36};row.Paint+=delegate(object sender,PaintEventArgs paint){DrawPreview(paint.Graphics,value,new Rectangle(110,8,58,20),ForeColor);};row.Click+=delegate{PopupLifetime.AfterClick(this,delegate{SelectedItem=value;});};arrowMenu.Items.Add(row);}
        arrowMenu.Size=new Size(190,Items.Count*36+4);arrowMenu.Show(this,new Point(0,Height+3));
    }
    internal static void DrawPreview(Graphics graphics,string style,Rectangle bounds,Color color){
        var mark=new Mark {Kind=style=="Double"?"Double arrow":style=="Line"?"Line":style=="Dashed arrow"?"Dashed arrow":"Arrow",ArrowStyle=style,Width=style=="Modern"?2.6f:1.5f,X=bounds.Left+3,Y=bounds.Top+bounds.Height/2f,W=bounds.Width-12,H=.01f,Color=color.ToArgb()};
        graphics.SmoothingMode=SmoothingMode.AntiAlias;using(var pen=new Pen(color,mark.Width))using(var path=Art.ArrowPath(mark)){
            if(mark.Kind!="Line")using(var cap=new AdjustableArrowCap(style=="Basic"?3:4,style=="Basic"?3:5,style!="Classic")){
                pen.CustomEndCap=cap;if(style=="Double")pen.CustomStartCap=cap;if(style=="Dashed arrow")pen.DashStyle=DashStyle.Dash;graphics.DrawPath(pen,path);
            }else graphics.DrawPath(pen,path);
        }
    }
}
class ScreenshotListView : ListView {
    [System.Runtime.InteropServices.DllImport("uxtheme.dll",CharSet=System.Runtime.InteropServices.CharSet.Unicode)]static extern int SetWindowTheme(IntPtr window,string theme,string sub);
    internal bool DarkBrowserThemeApplied;bool reflowingGallery;internal int GalleryColumns,GalleryCellWidth;
    [System.Runtime.InteropServices.DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr window,int message,IntPtr wParam,IntPtr lParam);
    public ScreenshotListView(){DoubleBuffered=true;}
    int lastGalleryWidth=-1,lastGalleryCount=-1;internal int GalleryLayoutPasses;
    protected override void OnResize(EventArgs e){base.OnResize(e);ReflowGallery(false);}
    internal void ReflowGallery(bool force=true){if(!IsHandleCreated||View!=System.Windows.Forms.View.LargeIcon||ClientSize.Width<1||reflowingGallery)return;if(!force&&lastGalleryWidth==ClientSize.Width&&lastGalleryCount==Items.Count)return;lastGalleryWidth=ClientSize.Width;lastGalleryCount=Items.Count;GalleryLayoutPasses++;reflowingGallery=true;BeginUpdate();try{int available=Math.Max(1,ClientSize.Width-8);GalleryColumns=Math.Max(1,available/168);GalleryCellWidth=Math.Max(144,available/GalleryColumns);int height=150;AutoArrange=false;Alignment=ListViewAlignment.Top;SendMessage(Handle,0x1035,IntPtr.Zero,new IntPtr((height<<16)|(GalleryCellWidth&65535)));for(int i=0;i<Items.Count;i++){var item=Items[i];if(item!=null)item.Position=new Point((i%GalleryColumns)*GalleryCellWidth,(i/GalleryColumns)*height);}Invalidate();}finally{EndUpdate();reflowingGallery=false;}}
    protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);try{DarkBrowserThemeApplied=SetWindowTheme(Handle,"DarkMode_Explorer",null)==0;}catch(DllNotFoundException){}catch(EntryPointNotFoundException){}ReflowGallery();}
    string[] clickedSelection,activationSelection;
    int clickedIndex=-1,clickedTick;
    public string[] SelectedPaths(){var paths=activationSelection;activationSelection=null;return paths??SelectedItems.Cast<ListViewItem>().Select(item=>item.Tag as string).Where(path=>path!=null).ToArray();}
    protected override void WndProc(ref Message message){
        if(message.Msg==0x201){int packed=message.LParam.ToInt32();var item=GetItemAt((short)(packed&65535),(short)(packed>>16));clickedSelection=null;clickedIndex=-1;if(ModifierKeys==Keys.None&&item!=null&&item.Selected&&SelectedItems.Count>1){clickedSelection=SelectedPaths();clickedIndex=item.Index;clickedTick=Environment.TickCount;}}
        if(message.Msg==0x203){int packed=message.LParam.ToInt32();var item=GetItemAt((short)(packed&65535),(short)(packed>>16));if(item!=null&&item.Index==clickedIndex&&unchecked(Environment.TickCount-clickedTick)<=SystemInformation.DoubleClickTime*2)activationSelection=clickedSelection;}
        base.WndProc(ref message);
    }
}
partial class Editor {
    ContextMenuStrip screenshotOpenMenu;
    void InstallScreenshotOpenMenu(){
        screenshotOpenMenu=PopupLifetime.Own(screenshotFiles,DarkMenu());
        screenshotFiles.MouseUp+=delegate(object sender,MouseEventArgs e){
            if(e.Button!=MouseButtons.Right)return;var hit=screenshotFiles.GetItemAt(e.X,e.Y);if(hit==null||hit.Tag==null)return;if(!hit.Selected){screenshotFiles.SelectedItems.Clear();hit.Selected=true;}
            PopulateScreenshotOpenMenu();screenshotOpenMenu.Show(screenshotFiles,e.Location);
        };
    }
    bool HandleLibraryKey(Keys key){if(!welcomeRequested)return false;if(key==Keys.E){Guard(delegate{OpenSelectedScreenshots(false);});return true;}if(key==(Keys.Alt|Keys.Up)){Guard(BrowseScreenshotParent);return true;}if(key==Keys.F5){RefreshScreenshotBrowser();return true;}return false;}
    void PopulateScreenshotOpenMenu(){
        PopupLifetime.Reset(screenshotOpenMenu);var paths=screenshotFiles.SelectedItems.Cast<ListViewItem>().Select(item=>item.Tag as string).Where(path=>path!=null).ToArray();bool directory=paths.Length==1&&Directory.Exists(paths[0]);bool projects=paths.Length>0&&paths.All(IsProjectPath);
        var edit=new ToolStripMenuItem(directory?"Open folder":projects?"Edit project":"Edit image"){ShortcutKeyDisplayString="E"};edit.Click+=delegate{PopupLifetime.AfterClick(this,delegate{Guard(delegate{OpenSelectedScreenshots(false);});});};screenshotOpenMenu.Items.Add(edit);
        if(!directory){var together=screenshotOpenMenu.Items.Add("Open in one tab as layers",null,delegate{PopupLifetime.AfterClick(this,delegate{Guard(delegate{OpenSelectedScreenshots(true);});});});together.Enabled=paths.All(path=>!IsProjectPath(path));screenshotOpenMenu.Items.Add(new ToolStripSeparator());
        screenshotOpenMenu.Items.Add("Open file",null,delegate{var selected=paths.ToArray();PopupLifetime.AfterClick(this,delegate{Guard(delegate{foreach(var path in selected)System.Diagnostics.Process.Start(BrowserShellAction(path,false));});});});}
        screenshotOpenMenu.Items.Add("Open file location",null,delegate{var selected=paths.FirstOrDefault();PopupLifetime.AfterClick(this,delegate{Guard(delegate{if(selected!=null)System.Diagnostics.Process.Start(BrowserShellAction(selected,true));});});});
    }
    static System.Diagnostics.ProcessStartInfo BrowserShellAction(string path,bool location){path=Path.GetFullPath(path);if(!File.Exists(path)&&!Directory.Exists(path))throw new FileNotFoundException("This file is no longer available.",path);return location?new System.Diagnostics.ProcessStartInfo("explorer.exe","/select,\""+path+"\""){UseShellExecute=true}:new System.Diagnostics.ProcessStartInfo(path){UseShellExecute=true,Verb="open"};}
    void OpenSelectedScreenshots(bool together){
        var paths=screenshotFiles.SelectedPaths();
        if(paths.Length==1&&Directory.Exists(paths[0])){OpenScreenshotEntry(paths[0]);return;}
        var files=paths.Where(File.Exists).ToArray();if(files.Length==0)return;
        if(!together){foreach(string path in files)OpenScreenshotEntry(path);return;}
        // Decode before changing documents so an unreadable file never leaves a partial import.
        var images=new List<Mark>();int width=1,height=1;
        foreach(string path in files){if(Path.GetExtension(path).Equals(".shotout",StringComparison.OrdinalIgnoreCase)||Path.GetExtension(path).Equals(".snapcanvas",StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Editable projects open in separate tabs. Choose image files to open as layers.");
            using(var image=Image.FromFile(path))using(var bitmap=new Bitmap(image)){images.Add(new Mark {Kind="Image",Name=Path.GetFileName(path),W=image.Width,H=image.Height,Width=0,Image=Art.Bytes(bitmap)});width=Math.Max(width,image.Width);height=Math.Max(height,image.Height);}
        }
        if(width>16000||height>16000||(long)width*height>40000000)throw new ArgumentException("Canvas size limit exceeded.");
        AddDocument(new Bitmap(width,height),"Combined images",true);Marks.AddRange(images);Active=images.Last();Selected.Clear();Selected.Add(Active);ResetCanvasBase();SyncStyle();RefreshImage();View.Fit();
    }
    string defaultArrowStyle="Modern";
    bool cancellableCut;
    int cancellableCutHistory;
    bool cutWasDirty;
    void RememberCut(){cancellableCut=true;cancellableCutHistory=undo.Count;cutWasDirty=dirty;}
    void AcceptCut(){cancellableCut=false;}
    void CancelCut(){
        Crop=null;cropGesture=false;
        if(cancellableCut&&undo.Count==cancellableCutHistory+1){cancellableCut=false;Undo();dirty=cutWasDirty;UpdateStatus();}
        AcceptCut();View.Invalidate();
    }
    void ChangeArrowStyle(string style){
        string kind=style=="Double"?"Double arrow":style=="Line"?"Line":"Arrow";defaultArrowStyle=style=="Double"||style=="Line"?"Modern":style;
        if(Active!=null&&Art.IsArrow(Active.Kind)){Push();bool curved=IsCurved(Active);Active.Kind=kind;Active.ArrowCurved=curved;Active.ArrowStyle=style;if(tool!="Select")tool=kind;RememberVariant(kind);RefreshImage();}
        else SetTool(kind);
        View.Focus();
    }
    void UpdatePresentationInspector(){
        bool presentation=Active!=null&&Active.IsPresentation;
        fillBox.Text=presentation?"Gradient":"Fill";fillButton.Text=presentation?"End color":"Fill color";
        if(presentation){colorButton.Text="Start color";strokeRow.Visible=true;weightRow.Visible=false;radiusRow.Visible=true;}
    }
    static void PrepareBubbleBounds(Mark bubble){
        var bounds=bubble.BodyBounds;bubble.X=bounds.Left;bubble.Y=bounds.Top;bubble.W=bounds.Width;bubble.H=bounds.Height;
        if(bubble.W<4&&bubble.H<4){bubble.W=240;bubble.H=100;}
        else {bubble.W=Math.Max(40,bubble.W);bubble.H=Math.Max(40,bubble.H);}
        bubble.BubbleSized=true;
    }
    static void FitBubbleText(Graphics graphics,Mark mark,RectangleF bounds,SizeF natural){
        float width=Math.Max(1,bounds.Width-22);int lines=1;float used=0;var fonts=new Dictionary<int,Font>();var widths=new Dictionary<string,float>();
        var styles=new List<int>();bool rich=mark.Runs!=null&&String.Concat(mark.Runs.Select(run=>run.Text))==mark.Text;
        if(rich)foreach(var run in mark.Runs)for(int i=0;i<run.Text.Length;i++)styles.Add(run.Style&7);
        using(var format=(StringFormat)StringFormat.GenericTypographic.Clone())try{
            format.FormatFlags|=StringFormatFlags.NoWrap|StringFormatFlags.MeasureTrailingSpaces;
            Func<int,float> charWidth=delegate(int index){int style=rich?styles[index]:(int)RichLayout.BaseStyle(mark);string key=style+":"+mark.Text[index];float size;if(widths.TryGetValue(key,out size))return size;Font font;if(!fonts.TryGetValue(style,out font)){font=TextFonts.Create(mark.FontFamily,mark.Size,(FontStyle)style);fonts[style]=font;}size=graphics.MeasureString(mark.Text[index].ToString(),font,PointF.Empty,format).Width;widths[key]=size;return size;};
            for(int i=0;i<mark.Text.Length;i++){
                char c=mark.Text[i];if(c=='\r')continue;if(c=='\n'){lines++;used=0;continue;}
                if(!Char.IsWhiteSpace(c)&&(i==0||Char.IsWhiteSpace(mark.Text[i-1]))){float word=0;for(int j=i;j<mark.Text.Length&&!Char.IsWhiteSpace(mark.Text[j]);j++)word+=charWidth(j);if(used>0&&word<=width&&used+word>width){lines++;used=0;}}
                float advance=charWidth(i);if(used>0&&used+advance>width){lines++;used=0;}used+=advance;
            }
        }finally{foreach(var font in fonts.Values)font.Dispose();}
        mark.X=bounds.X;mark.Y=bounds.Y;mark.W=Math.Max(bounds.Width,mark.Size*1.5f+22);mark.H=Math.Max(bounds.Height,(float)Math.Ceiling(lines*mark.Size*1.4f+20+(mark.Tail?14:0)));
    }
    public void VerifyReleasePolish(List<string> report,string folder){
        Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception(label);report.Add("PASS "+label);};
        check(new Preferences().ConfirmUnsaved,"Unsaved-change confirmation is enabled by default");var settings=new Preferences {ConfirmUnsaved=false};using(var stream=new MemoryStream()){var xml=new System.Xml.Serialization.XmlSerializer(typeof(Preferences));xml.Serialize(stream,settings);stream.Position=0;check(!((Preferences)xml.Deserialize(stream)).ConfirmUnsaved,"Disabling save confirmation survives settings serialization");}
        LoadImageDocument(Blank(400,300),"Bubble gesture");Show();Application.DoEvents();SetTool("Balloon");View.Zoom=1;View.PanX=View.PanY=0;var origin=View.Origin;
        CanvasDownWithModifiers(new MouseEventArgs(MouseButtons.Left,1,(int)origin.X+20,(int)origin.Y+20,0),Keys.Control);
        check(draft!=null&&inlineInput==null,"Speech bubble starts with a rectangle sizing gesture");CanvasMove(new MouseEventArgs(MouseButtons.Left,0,(int)origin.X+220,(int)origin.Y+120,0));CanvasUp(new MouseEventArgs(MouseButtons.Left,1,(int)origin.X+220,(int)origin.Y+120,0));
        check(inlineTarget!=null&&inlineTarget.BubbleSized&&Math.Abs(inlineTarget.W-200)<2,"Dragging defines bubble width before inline text editing");HandleInlineKey(Keys.Escape);check(Marks.Any(mark=>mark.Kind=="Balloon"&&mark.Text==""),"Escape preserves an empty speech balloon");
        var bubble=Marks.First(mark=>mark.Kind=="Balloon");BeginInlineText(bubble,false);inlineInput.Text="This long speech balloon contains multiple words that wrap safely.\nAnd an additional line.";FlushInlineUpdate();FinishInlineText(true);
        check(bubble.W<205&&bubble.H>100,"Wrapped speech text grows vertically without losing the drawn width");check(bubble.Text.Contains("additional line"),"Finishing text retains every speech bubble line");
        using(var image=Art.Render(Background,Marks))image.Save(Path.Combine(folder,"wrapped-speech-bubble.png"));
        foreach(string style in new[]{"Classic","Double","Modern","Basic","Line"}){SetTool("Arrow");ChangeArrowStyle(style);var arrow=NewMark(tool,new PointF(20,30));arrow.W=200;arrow.H=20;Marks.Add(arrow);Active=arrow;check(ArrowHandles(arrow).Length==4,"Every "+style+" arrow exposes a bend handle");resizeOriginal=arrow.Clone();resizeHandle=102;EditArrow(new PointF(120,5));check(arrow.ArrowCurved&&Art.ArrowPath(arrow).PointCount==4,"Dragging bends "+style+" arrows");using(var image=Art.Render(Background,Marks)){} }
        LoadImageDocument(Blank(240,160),"Cut cancellation");Marks.Add(new Mark {Kind="Rectangle",X=10,Y=10,W=70,H=40,Color=Color.Red.ToArgb()});var before=Snapshot();int history=undo.Count;bool wasDirty=dirty;SetTool("Cut rows");Crop=new RectangleF(0,60,240,20);CommitCrop();check(Background.Height==140,"Cut is applied immediately without an Apply click");CancelCut();check(Background.Height==160&&Marks.Count==before.Marks.Count&&undo.Count==history&&dirty==wasDirty,"Cancel cut restores image, editable annotations and undo history");
        SetTool("Cut columns");Crop=new RectangleF(60,0,20,160);CommitCrop();SetTool("Arrow");CancelCut();check(Background.Width==220,"Switching tools retains the completed cut");Undo();check(Background.Width==240,"A retained cut remains undoable");
        SetTool("Cut rows");Crop=new RectangleF(0,40,240,10);CommitCrop();Crop=new RectangleF(0,40,240,15);CommitCrop();CancelCut();check(Background.Height==150,"Cancel reverses only the latest cut in a series");Undo();
        LoadImageDocument(Blank(240,160),"Presentation editing");AddEditableBackdrop(30);var backdrop=Active;check(backdrop!=null&&backdrop.IsPresentation&&Marks[0]==backdrop&&Background.Size==new Size(300,220),"Presentation background is a selected editable layer behind the screenshot");
        ApplyPaletteColor(false,Color.Red,100);ApplyPaletteColor(true,Color.Blue,100);using(var image=Art.Render(Background,Marks)){var corner=image.GetPixel(4,4);check(corner.R>corner.B,"Presentation colors change the rendered background");image.Save(Path.Combine(folder,"editable-presentation.png"));}
        fillBox.Checked=false;using(var image=Art.Render(Background,Marks))check(image.GetPixel(4,4).ToArgb()==Color.Red.ToArgb(),"Presentation supports solid color as well as a gradient");
        var saved=Snapshot();Restore(saved);check(Marks[0].IsPresentation&&Marks[0].Color==Color.Red.ToArgb(),"Presentation editability survives restoring a project");
        var fixture=Path.Combine(folder,"browser-fixture");Directory.CreateDirectory(fixture);using(var image=Blank(90,60))image.Save(Path.Combine(fixture,"one.png"));using(var image=Blank(120,80))image.Save(Path.Combine(fixture,"two.png"));
        AddWelcomeDocument();screenshotBrowsing=fixture;screenshotPage=0;RefreshScreenshotBrowser();Application.DoEvents();check(screenshotFiles.Activation==ItemActivation.Standard&&screenshotFiles.MultiSelect,"Browser uses double-click activation and allows multiple selections");int count=DocumentCount;foreach(ListViewItem item in screenshotFiles.Items)item.Selected=true;check(screenshotFiles.SelectedItems.Count==2&&DocumentCount==count,"Single and multiple image selection does not open documents");OpenSelectedScreenshots(false);check(DocumentCount==count+1&&DocumentTitles().Any(title=>title.StartsWith("one"))&&DocumentTitles().Any(title=>title.StartsWith("two")),"Selected images open in separate editor tabs");
        AddWelcomeDocument();screenshotBrowsing=fixture;RefreshScreenshotBrowser();Application.DoEvents();foreach(ListViewItem item in screenshotFiles.Items)item.Selected=true;count=DocumentCount;OpenSelectedScreenshots(true);check(DocumentCount==count&&Marks.Count==2&&Marks.All(mark=>mark.Kind=="Image")&&Background.Size==new Size(120,80),"Open in one tab imports independent image layers on a transparent canvas");
        SetTool("Arrow");using(var preview=new Bitmap(Width,Height)){DrawToBitmap(preview,new Rectangle(Point.Empty,preview.Size));preview.Save(Path.Combine(folder,"release-polish-interface.png"));}
        Hide();while(DocumentCount>1)CloseDocumentCore(DocumentCount-1,false);LoadBitmap(Blank(100,100),"Test");AcceptCut();
    }
}
}
