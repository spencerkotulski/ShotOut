using System;using System.Collections.Generic;using System.Drawing;using System.IO;using System.Linq;using System.Windows.Forms;
namespace ShotOut {partial class Editor {
void RememberDraggedCircleSize(){if(Active==null||Active.Kind!="Step")return;defaultCircleSize=Math.Max(4,Math.Min(2000,Math.Max(Math.Abs(Active.W),Math.Abs(Active.H))));SyncCircleSize();}
static readonly KeyValuePair<string,Size>[] blankPresets={
new KeyValuePair<string,Size>("Small",new Size(1000,620)),new KeyValuePair<string,Size>("HD",new Size(1280,720)),new KeyValuePair<string,Size>("Full HD",new Size(1920,1080)),new KeyValuePair<string,Size>("Square",new Size(1080,1080)),new KeyValuePair<string,Size>("Portrait",new Size(1080,1920)),new KeyValuePair<string,Size>("4K",new Size(3840,2160))};
ContextMenuStrip blankCanvasMenu;
void ShowBlankCanvasPresets(){
if(blankCanvasMenu==null)InstallVisualCanvasPresets();
blankCanvasMenu.Show(Cursor.Position);
}
void CreateBlankCanvas(Size size){
if(size.Width<1||size.Height<1||size.Width>16000||size.Height>16000||(long)size.Width*size.Height>40000000)throw new ArgumentException("Use dimensions between 1 and 16,000 pixels, up to 40 million pixels total.");
AddDocument(Blank(size.Width,size.Height),"Untitled",false);SetTool("Arrow");View.Focus();
}
public void VerifyColorNumberCanvas(List<string> report,string folder){
Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception(label);report.Add("PASS "+label);};
LoadBitmap(Blank(600,400),"Text colors");SetTool("Text");Show();Application.DoEvents();
var textMark=NewMark("Text",new PointF(30,30));BeginInlineText(textMark,true);inlineInput.SelectedText="Custom color\nSecond line";FlushInlineUpdate();
foreach(bool typing in new[]{true,false})foreach(int opacity in new[]{100,50,0,100}) {
if(!typing){FinishInlineText(true);Active=Marks.Last();Selected.Clear();Selected.Add(Active);SyncStyle();}
using(var timer=new Timer {Interval=40}){timer.Tick+=delegate{timer.Stop();var picker=Application.OpenForms.OfType<PaletteDialog>().FirstOrDefault();if(picker==null)throw new Exception("Color dialog did not open");picker.Controls.OfType<TextBox>().Single().Text="#713C2D";picker.Controls.OfType<ModernButton>().Single(button=>button.Text=="Apply color").PerformClick();};timer.Start();PickColor(false);}
var fontBefore=inlineDisplayFont;float sizeBefore=Marks.Last().Size;ApplyPaletteColor(false,Color.FromArgb(113,60,45),opacity);check(Object.ReferenceEquals(fontBefore,inlineDisplayFont)&&Marks.Last().Size==sizeBefore,"Applying a color preserves the current font and text size");if(inlineInput!=null){inlineInput.SelectedText=" more";FlushInlineUpdate();}
using(var frame=new Bitmap(View.Width,View.Height))View.DrawToBitmap(frame,new Rectangle(Point.Empty,frame.Size));check((Marks.Last().Color&0xffffff)==0x713c2d,"Custom reddish-brown text color applies and repaints, typing="+typing+", opacity="+opacity);
}
FinishInlineText(true);LoadBitmap(Blank(900,700),"Number resize");defaultCircleSize=40;SetTool("Step");View.Zoom=1;View.PanX=View.PanY=0;
Func<float,float,MouseEventArgs> mouse=(x,y)=>{var origin=View.Origin;return new MouseEventArgs(MouseButtons.Left,1,(int)(origin.X+x*View.Zoom),(int)(origin.Y+y*View.Zoom),0);};
CanvasDownWithModifiers(mouse(100,100),Keys.None);CanvasUp(mouse(100,100));var first=Marks.Last();SetTool("Select");Active=first;Selected.Clear();Selected.Add(first);SyncStyle();var corner=first.BodyBounds;CanvasDownWithModifiers(mouse(corner.Right,corner.Bottom),Keys.None);CanvasMove(mouse(corner.Right+50,corner.Bottom+50));CanvasUp(mouse(corner.Right+50,corner.Bottom+50));check(first.W==90&&first.H==90&&defaultCircleSize==90&&stepSize.Value==90,"Dragging a number circle updates its dial and the next-number size");SetTool("Step");CanvasDownWithModifiers(mouse(400,350),Keys.None);CanvasUp(mouse(400,350));var second=Marks.Last();check(second.W==90&&second.H==90&&second.Text!=first.Text,"The next numbered circle uses the dragged size and continues counting");
SetTool("Select");Active=first;Selected.Clear();Selected.Add(first);SyncStyle();corner=first.BodyBounds;CanvasDownWithModifiers(mouse(corner.Right,corner.Bottom),Keys.None);CanvasMove(mouse(corner.Right-20,corner.Bottom-20));CanvasUp(mouse(corner.Right-20,corner.Bottom-20));check(defaultCircleSize==70&&second.W==90,"Shrinking a number updates future circles without changing other existing numbers");
first.W=2500;first.H=2500;SyncCircleSize();check(stepSize.Value==2000,"Oversized dragged circles never overflow the size dial");first.W=first.H=70;RefreshImage();
foreach(var preset in blankPresets){int count=DocumentCount;AddWelcomeDocument();CreateBlankCanvas(preset.Value);check(Background.Size==preset.Value&&!welcomeRequested&&DocumentCount==count+1,"Blank canvas preset creates the chosen size in its current tab: "+preset.Key);CloseDocumentCore(currentDocument,false);}
using(var custom=new BlankCanvasSizeDialog()){custom.SetSizeForTest(1500,950);check(custom.CanCreate&&custom.SelectedSize==new Size(1500,950),"Custom blank canvas accepts width and height");custom.SetSizeForTest(16000,16000);check(!custom.CanCreate,"Custom blank canvas rejects dimensions above the total pixel limit");custom.SetSizeForTest(1500,950);custom.Show(this);Application.DoEvents();using(var frame=new Bitmap(custom.Width,custom.Height)){custom.DrawToBitmap(frame,new Rectangle(Point.Empty,frame.Size));frame.Save(Path.Combine(folder,"custom-canvas-size.png"));}custom.Hide();}
int previous=currentDocument;AddWelcomeDocument();check(screenshotFiles.DarkBrowserThemeApplied,"Screenshot browser applies the native dark theme");ShowBlankCanvasPresets();Application.DoEvents();check(blankCanvasMenu.Items.OfType<ToolStripControlHost>().Single().Control.Controls.OfType<CanvasPresetButton>().Count()==6,"Blank canvas menu includes six common sizes and Custom");blankCanvasMenu.Close();using(var frame=new Bitmap(Width,Height)){DrawToBitmap(frame,new Rectangle(Point.Empty,frame.Size));frame.Save(Path.Combine(folder,"dark-browser-scrollbar.png"));}ActivateDocument(previous);Hide();LoadBitmap(Blank(100,100),"Test");
}
}}namespace ShotOut {
class BlankCanvasSizeDialog : Form {
readonly NumberField widthField,heightField;readonly ModernButton create;readonly Label help;
public Size SelectedSize {get{return new Size((int)widthField.Value,(int)heightField.Value);}}
public BlankCanvasSizeDialog(){Text="Blank canvas";ClientSize=new Size(420,220);FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=MinimizeBox=false;StartPosition=FormStartPosition.CenterParent;BackColor=Color.FromArgb(30,32,38);ForeColor=Color.FromArgb(233,236,243);Font=new Font("Segoe UI",13,FontStyle.Regular,GraphicsUnit.Pixel);
Controls.Add(new Label {Text="Custom canvas size",Location=new Point(22,18),Size=new Size(370,26),ForeColor=ForeColor});
Controls.Add(new Label {Text="Width · px",Location=new Point(22,59),Size=new Size(155,24),ForeColor=ForeColor});Controls.Add(new Label {Text="Height · px",Location=new Point(222,59),Size=new Size(155,24),ForeColor=ForeColor});
widthField=new NumberField {Minimum=1,Maximum=16000,Value=1000,Location=new Point(22,85),Size=new Size(175,34),BackColor=Color.FromArgb(43,46,54),ForeColor=ForeColor,AccessibleName="Canvas width"};heightField=new NumberField {Minimum=1,Maximum=16000,Value=620,Location=new Point(222,85),Size=new Size(175,34),BackColor=Color.FromArgb(43,46,54),ForeColor=ForeColor,AccessibleName="Canvas height"};Controls.Add(widthField);Controls.Add(heightField);
help=new Label {Text="Up to 40 million pixels total.",Location=new Point(22,130),Size=new Size(375,24),ForeColor=Color.FromArgb(151,159,175)};Controls.Add(help);
create=new ModernButton {Text="Create canvas",Location=new Point(222,169),Size=new Size(175,34),BackColor=UiTheme.Accent,ForeColor=ForeColor,DialogResult=DialogResult.OK};var cancel=new ModernButton {Text="Cancel",Location=new Point(22,169),Size=new Size(175,34),BackColor=Color.FromArgb(43,46,54),ForeColor=ForeColor,DialogResult=DialogResult.Cancel};Controls.Add(create);Controls.Add(cancel);AcceptButton=create;CancelButton=cancel;
widthField.ValueChanged+=delegate{ValidateSize();};heightField.ValueChanged+=delegate{ValidateSize();};
}
void ValidateSize(){create.Enabled=(long)widthField.Value*(long)heightField.Value<=40000000;help.Text=create.Enabled?"Up to 40 million pixels total.":"Choose a smaller size: maximum 40 million pixels.";}
internal void SetSizeForTest(int width,int height){widthField.Value=width;heightField.Value=height;}
internal bool CanCreate {get{return create.Enabled;}}
}
}namespace ShotOut {
static class ErrorDetail {
internal static void Record(Exception error){if(Preferences.Testing)return;try{string directory=Path.GetDirectoryName(Preferences.FilePath);Directory.CreateDirectory(directory);File.WriteAllText(Path.Combine(directory,"last-error.log"),"ShotOut "+typeof(Editor).Assembly.GetName().Version+" · "+DateTime.UtcNow.ToString("u")+Environment.NewLine+error.ToString());}catch(IOException){}catch(UnauthorizedAccessException){}}
}
}