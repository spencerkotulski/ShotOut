using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
namespace ShotOut {
class ListButton : ModernButton {
    public bool Numbered;
    protected override void OnPaint(PaintEventArgs e){
        base.OnPaint(e);var g=e.Graphics;float left=Width/2f-10;using(var pen=new Pen(ForeColor,1.4f))using(var brush=new SolidBrush(ForeColor))using(var font=new Font("Segoe UI",8,FontStyle.Regular,GraphicsUnit.Pixel)){
            for(int i=0;i<3;i++){float y=Height/2f-7+i*7;g.DrawLine(pen,left+8,y,left+20,y);if(Numbered)g.DrawString((i+1).ToString(),font,brush,left-1,y-5);else g.FillEllipse(brush,left,y-1.5f,3,3);}
        }
    }
}
partial class Editor {
    Control listRow,stepNumberRow,stepSizeRow;
    ListButton bulletList,numberList;
    NumberField stepNumber,stepSize;
    static readonly Regex ListPrefix=new Regex(@"^(• |\d+\. )",RegexOptions.CultureInvariant);
    void QueueInlineUpdate(){
        if(inlineUpdatePending||inlineInput==null||inlineSync)return;inlineUpdatePending=true;var input=inlineInput;
        BeginInvoke((Action)delegate{if(inlineInput==input&&!input.IsDisposed)FlushInlineUpdate();});
    }
    void FlushInlineUpdate(){if(inlineUpdatePending&&inlineInput!=null&&!inlineSync)UpdateInlineText();}
    void AddTextWorkflowInspector(){
        var row=Row();row.Controls.Add(Caption("Lists",82));bulletList=new ListButton {Width=62,Height=34,Margin=new Padding(0,0,4,0),BackColor=Color.FromArgb(43,46,54),ForeColor=text,TabStop=false,AccessibleName="Bulleted list"};numberList=new ListButton {Numbered=true,Width=62,Height=34,Margin=Padding.Empty,BackColor=bulletList.BackColor,ForeColor=text,TabStop=false,AccessibleName="Numbered list"};tips.SetToolTip(bulletList,"Bulleted list · Shift+Enter continues · Enter or Esc finishes editing");tips.SetToolTip(numberList,"Numbered list · Shift+Enter continues · Shift+Enter on an empty item ends the list");bulletList.Click+=delegate{ToggleInlineList(false);};numberList.Click+=delegate{ToggleInlineList(true);};row.Controls.Add(bulletList);row.Controls.Add(numberList);listRow=row;inspector.Controls.Add(row);
        row=Row();row.Controls.Add(Caption("Next number",145));stepNumber=Number(1,999999,1);stepNumber.AccessibleName="Starting number for new numbered circles";stepNumber.ValueChanged+=delegate{if(!syncing)step=(int)stepNumber.Value;};tips.SetToolTip(stepNumber,"Next circle starts here. Existing numbers stay unchanged.");row.Controls.Add(stepNumber);stepNumberRow=row;inspector.Controls.Add(row);
        row=Row();row.Controls.Add(Caption("Circle size · px",145));stepSize=Number(4,2000,40);stepSize.AcceleratedWheel=true;stepSize.AccessibleName="Circle size";stepSize.ValueChanged+=delegate{ChangeCircleSize();};tips.SetToolTip(stepSize,"Resizes selected number circles and sets the size for new circles.");row.Controls.Add(stepSize);stepSizeRow=row;inspector.Controls.Add(row);
    }
    void UpdateTextWorkflowInspector(string kind){
        if(listRow==null)return;listRow.Visible=(kind=="Text"||kind=="Balloon")&&Preferences.Current.TextOptionsExpanded;stepNumberRow.Visible=stepSizeRow.Visible=kind=="Step";stepNumber.Value=step;SyncCircleSize();
        bulletList.Enabled=numberList.Enabled=inlineTarget!=null||(Active!=null&&(Active.Kind=="Text"||Active.Kind=="Balloon"));UpdateListButtons();
    }
    string CurrentInlineLine(){int caret=Math.Min(inlineInput.SelectionStart,inlineInput.TextLength);string value=inlineInput.Text;int start=caret==0?0:value.LastIndexOf('\n',caret-1)+1;int end=value.IndexOf('\n',caret);return value.Substring(start,(end<0?value.Length:end)-start);}
    void UpdateListButtons(){
        if(bulletList==null)return;string line=inlineTarget!=null&&inlineInput!=null?CurrentInlineLine():Active!=null?Active.Text.Split('\n').FirstOrDefault()??"":"";bulletList.Selected=line.StartsWith("• ",StringComparison.Ordinal);numberList.Selected=!bulletList.Selected&&ListPrefix.IsMatch(line);bulletList.Invalidate();numberList.Invalidate();
    }
    void ToggleInlineList(bool numbered){
        if(inlineTarget==null){if(Active==null||(Active.Kind!="Text"&&Active.Kind!="Balloon"))return;BeginInlineText(Active,false);}
        FlushInlineUpdate();var input=inlineInput;int start=input.SelectionStart,end=start+input.SelectionLength;string value=input.Text;int first=start==0?0:value.LastIndexOf('\n',start-1)+1;
        int last=end>start&&value[end-1]=='\n'?end-1:end;var lines=new List<int>{first};for(int i=first;i<last&&i<value.Length;i++)if(value[i]=='\n')lines.Add(i+1);
        bool remove=lines.All(index=>{var match=ListPrefix.Match(value.Substring(index));return match.Success&&(numbered?match.Value!="• ":match.Value=="• ");});
        bool old=inlineSync;inlineSync=true;using(((CanvasTextInput)input).Batch())try{
            for(int n=lines.Count-1;n>=0;n--){int index=lines[n],oldLength=ListPrefix.Match(value.Substring(index)).Length;string prefix=remove?"":numbered?(n+1)+". ":"• ";input.Select(index,oldLength);InsertInlineText(prefix);int delta=prefix.Length-oldLength;if(index<=start)start=Math.Max(index+prefix.Length,start+delta);if(index<=end)end=Math.Max(index+prefix.Length,end+delta);}
            input.Select(Math.Min(start,input.TextLength),Math.Max(0,Math.Min(end,input.TextLength)-Math.Min(start,input.TextLength)));
        }finally{inlineSync=old;}UpdateInlineText();input.Focus();
    }
    void InsertInlineText(string value){using(((CanvasTextInput)inlineInput).Batch()){var color=Color.FromArgb(inlineTarget.Color);inlineInput.SelectionColor=color;inlineInput.SelectedText=value;inlineInput.SelectionColor=color;}}
    bool ContinueInlineList(){
        string line=CurrentInlineLine();var match=ListPrefix.Match(line);if(!match.Success)return false;var input=inlineInput;int caret=input.SelectionStart;int start=caret==0?0:input.Text.LastIndexOf('\n',caret-1)+1;
        using(((CanvasTextInput)input).Batch()){
            if(line.Length==match.Length){input.Select(start,match.Length);input.SelectedText="";}
            else{string prefix="• ";int number;if(match.Value!="• "&&Int32.TryParse(match.Value.Substring(0,match.Length-2),out number))prefix=(Math.Min(999999,number+1))+". ";InsertInlineText("\n"+prefix);}
        }FlushInlineUpdate();return true;
    }
    int NextStepNumber(){int highest=0;foreach(var mark in Marks){int number;if(mark.Kind=="Step"&&Int32.TryParse(mark.Text,out number))highest=Math.Max(highest,number);}return Math.Max(1,Math.Min(999999,highest+1));}
    public void VerifyTextWorkflow(List<string> report,string folder){
        Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception(label);report.Add("PASS "+label);};LoadImageDocument(Blank(1000,700),"Text workflow");SetTool("Text");BeginInlineText(NewMark("Text",new PointF(100,100)),true);inlineInput.Text="First";inlineInput.Select(inlineInput.TextLength,0);FlushInlineUpdate();float height=inlineTarget.H;inlineInput.AppendText("\n");FlushInlineUpdate();check(inlineTarget.H>height&&inlineInput.Height>inlineContentHeight,"Return leaves room for the next native text line and caret");
        inlineInput.Select(inlineInput.TextLength,0);HandleInlineKey(Keys.Shift|Keys.Enter);Application.DoEvents();FlushInlineUpdate();var caret=inlineInput.GetPositionFromCharIndex(inlineInput.TextLength);check(caret.Y>0&&caret.Y+inlineInput.Font.Height<=inlineInput.ClientSize.Height,"Shift+Enter keeps the new-line caret fully visible after layout");
        for(int i=0;i<8;i++)inlineInput.AppendText("Line "+i+"\n");check(inlineUpdatePending,"Rapid typing queues one layout update instead of repainting per event");FlushInlineUpdate();check(inlineTarget.Text==inlineInput.Text&&!inlineUpdatePending,"Queued multiline editing flushes all content");
        inlineInput.Text="First\nSecond";inlineInput.SelectAll();ToggleInlineList(false);check(inlineTarget.Text=="• First\n• Second","Bullet button formats selected paragraphs");inlineInput.Select(inlineInput.TextLength,0);check(HandleInlineKey(Keys.Shift|Keys.Enter)&&inlineInput.Text.EndsWith("\n• "),"Shift+Enter continues a bullet list");check(HandleInlineKey(Keys.Shift|Keys.Enter)&&!inlineInput.Text.EndsWith("• "),"Shift+Enter on an empty bullet exits the list");
        inlineInput.Text="First\nSecond";inlineInput.SelectAll();ToggleInlineList(true);check(inlineTarget.Text=="1. First\n2. Second","Numbered button formats selected paragraphs");inlineInput.Select(inlineInput.TextLength,0);HandleInlineKey(Keys.Shift|Keys.Enter);check(inlineInput.Text.EndsWith("\n3. "),"Shift+Enter increments numbered list items");inlineInput.SelectedText="Third";FinishInlineText(true);check(Marks.Last().Text.EndsWith("3. Third"),"Committing flushes pending typing and preserves list prefixes");
        SetTool("Step");stepNumber.Value=12;stepSize.Value=64;View.Zoom=1;var origin=View.Origin;CanvasDownWithModifiers(new MouseEventArgs(MouseButtons.Left,1,(int)origin.X+600,(int)origin.Y+300,0),Keys.Control);var first=Marks.Last();check(first.Kind=="Step"&&first.Text=="12"&&first.W==64&&first.H==64&&stepNumber.Value==13,"Number circles honor starting number and default diameter and advance the counter");stepNumber.Value=30;stepSize.Value=90;check(first.Text=="12"&&first.W==90,"Circle size updates the selected circle without changing its number");CanvasDownWithModifiers(new MouseEventArgs(MouseButtons.Left,1,(int)origin.X+760,(int)origin.Y+300,0),Keys.Control);check(Marks.Last().Text=="30"&&Marks.Last().W==90,"Later circle defaults affect only new circles");
        Show();Application.DoEvents();check(BackColor==Color.FromArgb(38,40,47),"Application border matches the upper toolbar");using(var image=new Bitmap(Width,Height)){DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(Path.Combine(folder,"number-circle-controls.png"));}SetTool("Text");Active=Marks.First(mark=>mark.Kind=="Text");BeginInlineText(Active,false);Application.DoEvents();using(var image=new Bitmap(Width,Height)){DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(Path.Combine(folder,"lists-and-text-controls.png"));}FinishInlineText(false);Hide();stepSize.Value=40;LoadBitmap(Blank(100,100),"Test");
    }
}
}
