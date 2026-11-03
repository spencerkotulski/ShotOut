using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace ShotOut {
// Rich Edit supports transparent painting through WS_EX_TRANSPARENT.
class CanvasTextInput : RichTextBox { public CanvasTextInput(){ModernMenus.TextMenu(this);}
    int paintDepth;
    [System.Runtime.InteropServices.DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr h,int message,IntPtr w,IntPtr l);
    public IDisposable Batch(){return new PaintBatch(this);}
    sealed class PaintBatch : IDisposable {CanvasTextInput input;public PaintBatch(CanvasTextInput value){input=value;if(input.paintDepth++==0&&input.IsHandleCreated)SendMessage(input.Handle,0xB,IntPtr.Zero,IntPtr.Zero);}public void Dispose(){if(input==null)return;if(--input.paintDepth==0&&!input.IsDisposed&&input.IsHandleCreated){SendMessage(input.Handle,0xB,new IntPtr(1),IntPtr.Zero);input.Invalidate();}input=null;}}
    protected override CreateParams CreateParams {get{var p=base.CreateParams;p.ExStyle|=0x20;return p;}}
}
partial class Editor {
    Mark InlinePreviewMark(Mark mark){if(mark!=inlineTarget)return mark;var copy=mark.Clone();copy.Text="";copy.Runs.Clear();return copy;}

    RichTextBox inlineInput;
    Label inlineHint;
    Mark inlineTarget;
    Project inlineBefore;
    List<Project> inlineUndo,inlineRedo;
    bool inlineSync,inlineNew,inlineChanged,inlineWasDirty,inlineUpdatePending;
    int inlineContentHeight;
    Font inlineDisplayFont;
    protected override void OnResizeBegin(EventArgs e){LiveWindowResize=true;if(inlineTarget!=null){FinishInlineText(true);Selected.Clear();Active=null;SyncLayers();View.Invalidate();}base.OnResizeBegin(e);}
    void BeginInlineText(Mark mark,bool created){
        FinishInlineText(true);FinishStrokeAdjustment();moving=resizing=dragging=false;draft=null;View.Capture=false;EndDragPreview();
        inlineBefore=Snapshot();inlineUndo=undo.ToList();inlineRedo=redo.ToList();inlineWasDirty=dirty;Push();inlineTarget=mark;inlineNew=created;inlineChanged=false;if(created)Marks.Add(mark);Active=mark;Selected.Clear();Selected.Add(mark);SyncStyle();RefreshImage();
        inlineContentHeight=0;inlineSync=true;inlineInput=new CanvasTextInput {ForeColor=Color.FromArgb(mark.Color),BorderStyle=BorderStyle.None,DetectUrls=false,AcceptsTab=true,WordWrap=mark.Kind=="Balloon",ScrollBars=RichTextBoxScrollBars.None,Multiline=true,ShortcutsEnabled=true,Text=mark.Text,AccessibleName="Edit annotation on canvas"};View.Controls.Add(inlineInput);inlineHint=new Label {Text="Enter / Esc: finish · Shift+Enter: new line",AutoSize=true,BackColor=Color.FromArgb(38,41,48),ForeColor=Color.White,Font=Font,Padding=new Padding(5)};View.Controls.Add(inlineHint);UpdateInlineFont();inlineSync=false;
        inlineInput.TextChanged+=delegate{if(!inlineSync)QueueInlineUpdate();};inlineInput.ContentsResized+=delegate(object sender,ContentsResizedEventArgs e){inlineContentHeight=e.NewRectangle.Height;};inlineInput.SelectionChanged+=delegate{if(inlineSync||syncing)return;using(var selected=inlineInput.SelectionFont){if(selected==null)return;bool was=syncing;syncing=true;boldBox.Checked=selected.Bold;italicBox.Checked=selected.Italic;underlineBox.Checked=selected.Underline;syncing=was;UpdateListButtons();}};inlineInput.KeyDown+=delegate(object sender,KeyEventArgs e){if(HandleInlineKey(e.KeyData))e.Handled=e.SuppressKeyPress=true;};
        StartDragPreview();TryQueueDragFrame();PositionInlineText();inlineInput.BringToFront();inlineHint.BringToFront();inlineInput.Focus();inlineInput.SelectAll();UpdateListButtons();
    }
    void UpdateInlineFont(){
        if(inlineInput==null||inlineTarget==null)return;bool old=inlineSync;inlineSync=true;using(((CanvasTextInput)inlineInput).Batch())try{int start=inlineInput.SelectionStart,length=inlineInput.SelectionLength;inlineInput.ForeColor=Color.FromArgb(inlineTarget.Color);var previous=inlineDisplayFont;var next=TextFonts.Create(inlineTarget.FontFamily,inlineTarget.Size*View.Zoom,RichLayout.BaseStyle(inlineTarget));if(previous!=null&&previous.Equals(next)){next.Dispose();}else{inlineInput.Font=next;inlineDisplayFont=next;if(previous!=null)previous.Dispose();}inlineInput.SelectAll();inlineInput.SelectionColor=Color.FromArgb(inlineTarget.Color);if(inlineTarget.Runs!=null&&String.Concat(inlineTarget.Runs.Select(run=>run.Text))==inlineInput.Text){int pos=0;foreach(var run in inlineTarget.Runs){inlineInput.Select(pos,run.Text.Length);using(var font=TextFonts.Create(inlineTarget.FontFamily,inlineTarget.Size*View.Zoom,(FontStyle)(run.Style&7)))inlineInput.SelectionFont=font;pos+=run.Text.Length;}}inlineInput.SelectAll();inlineInput.SelectionAlignment=(HorizontalAlignment)RichLayout.Alignment(inlineTarget);inlineInput.Select(Math.Min(start,inlineInput.TextLength),Math.Min(length,Math.Max(0,inlineInput.TextLength-start)));}finally{inlineSync=old;}
    }
    internal void RefreshInlinePlacement(){if(inlineTarget!=null){UpdateInlineFont();PositionInlineText();}}
    void PositionInlineText(){
        if(inlineInput==null||inlineTarget==null)return;var origin=View.Origin;var body=inlineTarget.BodyBounds;float inset=inlineTarget.Kind=="Balloon"?10:0;int x=(int)Math.Round(origin.X+(body.X+inset)*View.Zoom),y=(int)Math.Round(origin.Y+(body.Y+inset)*View.Zoom);int width=Math.Max(90,(int)Math.Ceiling((body.Width-inset*2)*View.Zoom)+16),height=Math.Max(34,(int)Math.Ceiling((body.Height-inset*2)*View.Zoom)+12);if(inlineTarget.Kind=="Balloon")width=Math.Max(20,(int)Math.Floor((body.Width-20)*View.Zoom));int lines=inlineInput.Text.Count(c=>c=='\n')+1;int nativeHeight=Math.Max(inlineContentHeight,lines*inlineInput.Font.Height+6);height=Math.Max(height,nativeHeight+inlineInput.Font.Height);var bounds=new Rectangle(x,y,width,height);if(inlineInput.Bounds!=bounds)inlineInput.Bounds=bounds;inlineInput.ScrollToCaret();inlineHint.Location=new Point(Math.Max(0,x),Math.Max(0,Math.Min(View.Height-inlineHint.Height,y+inlineInput.Height+4)));
    }
    void UpdateInlineText(){
        if(inlineInput==null||inlineTarget==null||inlineSync)return;inlineUpdatePending=false;var oldInput=inlineInput.Bounds;inlineSync=true;using(((CanvasTextInput)inlineInput).Batch())try{string previous=inlineTarget.Text,current=inlineInput.Text;bool append=current.Length==previous.Length+1&&current.StartsWith(previous,StringComparison.Ordinal)&&inlineInput.SelectionLength==0&&inlineInput.SelectionStart==current.Length;bool truncate=current.Length==previous.Length-1&&previous.StartsWith(current,StringComparison.Ordinal)&&inlineInput.SelectionLength==0&&inlineInput.SelectionStart==current.Length;if(append||truncate){if(inlineTarget.Runs==null||String.Concat(inlineTarget.Runs.Select(run=>run.Text))!=previous)inlineTarget.Runs=String.IsNullOrEmpty(previous)?new List<TextRun>():new List<TextRun>{new TextRun {Text=previous,Style=(int)RichLayout.BaseStyle(inlineTarget)}};if(append){int style;using(var font=inlineInput.SelectionFont)style=(int)(font==null?RichLayout.BaseStyle(inlineTarget):font.Style);var last=inlineTarget.Runs.LastOrDefault();if(last!=null&&last.Style==style)last.Text+=current.Substring(previous.Length);else inlineTarget.Runs.Add(new TextRun {Text=current.Substring(previous.Length),Style=style});}else if(inlineTarget.Runs.Count>0){var last=inlineTarget.Runs.Last();last.Text=last.Text.Substring(0,last.Text.Length-1);if(last.Text.Length==0)inlineTarget.Runs.RemoveAt(inlineTarget.Runs.Count-1);}}else inlineTarget.Runs=RichLayout.Capture(inlineInput);inlineTarget.Text=current;}finally{inlineSync=false;}inlineChanged=true;bool previousSizing=quickTextSizing;quickTextSizing=true;try{GrowTextBox(inlineTarget,inlineTarget.Size);}finally{quickTextSizing=previousSizing;}PositionInlineText();if(!TryQueueDragFrame()){RefreshImage();StartDragPreview();}View.InvalidateMarkupCache();View.Invalidate(Rectangle.Union(oldInput,inlineInput.Bounds),true);UpdateListButtons();
    }
    void ApplyInlineProperties(){
        if(inlineTarget==null||inlineSync||syncing)return;inlineTarget.FontFamily=SelectedFontName;inlineTarget.Size=FontSizePixels;inlineTarget.Color=ink.ToArgb();inlineTarget.FillColor=fill.ToArgb();inlineTarget.Fill=fillBox.Checked;inlineTarget.FillOpacity=(float)fillOpacity.Value;inlineTarget.Width=(float)thickness.Value;inlineTarget.Radius=(float)cornerRadius.Value;inlineTarget.Tail=tailBox.Checked;inlineTarget.TextAlignment=alignmentChoiceIndex;inlineTarget.Shadow=shadowBox.Checked;inlineTarget.ShadowSize=(float)shadowSize.Value;inlineTarget.ShadowIntensity=(float)shadowIntensity.Value;UpdateInlineFont();UpdateInlineText();
    }
    void ToggleInlineFormat(FontStyle style){bool caret=inlineInput.SelectionLength==0;using(((CanvasTextInput)inlineInput).Batch())RichLayout.Toggle(inlineInput,style);if(caret)using(var font=inlineInput.SelectionFont){if(font!=null){inlineTarget.Bold=font.Bold;inlineTarget.Italic=font.Italic;inlineTarget.Underline=font.Underline;}}UpdateInlineText();}
    bool HandleInlineFormat(CheckBox control){if(inlineTarget==null||syncing||inlineSync)return false;ToggleInlineFormat(control==boldBox?FontStyle.Bold:control==italicBox?FontStyle.Italic:FontStyle.Underline);return true;}
    bool HandleInlineKey(Keys key){
        if(inlineTarget==null)return false;if(key==Keys.Escape)editorEscapeCount=Math.Max(1,editorEscapeCount);if(key==Keys.Enter||key==(Keys.Control|Keys.Enter)||key==Keys.Escape){FinishInlineText(true);Selected.Clear();Active=null;SyncLayers();View.Focus();View.Invalidate();return true;}
        if(key==(Keys.Shift|Keys.Enter)){if(!ContinueInlineList()){InsertInlineText("\n");FlushInlineUpdate();}return true;}
        if(key==(Keys.Control|Keys.B)||key==(Keys.Control|Keys.I)||key==(Keys.Control|Keys.U)){ToggleInlineFormat(key==(Keys.Control|Keys.B)?FontStyle.Bold:key==(Keys.Control|Keys.I)?FontStyle.Italic:FontStyle.Underline);return true;}
        if(key==(Keys.Control|Keys.W)){FinishInlineText(true);CloseDocument(currentDocument);return true;}if(key==(Keys.Control|Keys.Tab)||key==(Keys.Control|Keys.Shift|Keys.Tab)){FinishInlineText(true);CycleDocument((key&Keys.Shift)!=0?-1:1);return true;}
        if(key==(Keys.Control|Keys.Shift|Keys.S)){FinishInlineText(true);Guard(ShowSaveAsMenu);return true;}if(key==(Keys.Control|Keys.S)){FinishInlineText(true);Guard(delegate{SaveImage();});return true;}return false;
    }
    void FinishInlineText(bool apply){
        if(inlineTarget==null)return;if(apply)FlushInlineUpdate();inlineUpdatePending=false;var mark=inlineTarget;bool restore=!apply||(!inlineChanged&&!(inlineNew&&mark.Kind=="Balloon"))||(inlineNew&&mark.Kind!="Balloon"&&String.IsNullOrWhiteSpace(mark.Text));inlineTarget=null;inlineSync=true;EndDragPreview();if(inlineInput!=null){inlineInput.Dispose();inlineInput=null;}if(inlineHint!=null){inlineHint.Dispose();inlineHint=null;}if(inlineDisplayFont!=null){inlineDisplayFont.Dispose();inlineDisplayFont=null;}inlineSync=false;
        if(restore){Restore(inlineBefore);undo.Clear();undo.AddRange(inlineUndo);redo.Clear();redo.AddRange(inlineRedo);dirty=inlineWasDirty;Selected.Clear();Active=null;}else{GrowTextBox(mark,mark.Size);EnsureCanvasFitsMarks();Active=mark;Selected.Clear();Selected.Add(mark);}inlineBefore=null;inlineUndo=inlineRedo=null;RefreshImage();UpdateStatus();
    }
}
}
