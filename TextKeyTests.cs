using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
namespace ShotOut {
partial class Editor {
    public void VerifyInlineTextColor(List<string> report){
        Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception(label);report.Add("PASS "+label);};
        foreach(string kind in new[]{"Text","Balloon"})foreach(int list in new[]{0,1,2}){LoadImageDocument(Blank(700,500),"Preview color");SetTool(kind);var mark=NewMark(kind,new PointF(70,70));mark.Color=Color.RoyalBlue.ToArgb();BeginInlineText(mark,true);inlineInput.SelectedText="First";FlushInlineUpdate();if(list>0)ToggleInlineList(list==2);inlineInput.Select(inlineInput.TextLength,0);HandleInlineKey(Keys.Shift|Keys.Enter);check(inlineInput.SelectionColor.ToArgb()==mark.Color&&inlineInput.ForeColor.ToArgb()==mark.Color,"New "+kind+" line keeps the chosen insertion and default color; list="+list);foreach(char character in "Next"){SendMessage(inlineInput.Handle,0x102,new IntPtr(character),new IntPtr(1));FlushInlineUpdate();}int end=inlineInput.TextLength;inlineInput.Select(end-4,4);check(inlineInput.SelectedText=="Next"&&inlineInput.SelectionColor.ToArgb()==mark.Color,"Typed lower-line preview retains annotation color; "+kind+" list="+list);inlineInput.Select(end,0);HandleInlineKey(Keys.Shift|Keys.Enter);check(inlineInput.SelectionColor.ToArgb()==mark.Color,"Repeated line breaks retain preview color; "+kind+" list="+list);HandleInlineKey(Keys.Escape);check(Marks[1].Color==Color.RoyalBlue.ToArgb(),"Finishing preserves the same chosen export color");}
        LoadBitmap(Blank(100,100),"Test");
    }
    public void VerifyTextKeys(List<string> report){
        Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception(label);report.Add("PASS "+label);};
        foreach(string kind in new[]{"Text","Balloon"})foreach(Keys key in new[]{Keys.Escape,Keys.Enter,Keys.Control|Keys.Enter}){
            LoadImageDocument(Blank(700,400),"Text keys");SetTool(kind);BeginInlineText(NewMark(kind,new PointF(90,90)),true);inlineInput.Text="Keep these words";check(HandleInlineKey(key)&&inlineTarget==null&&inlineInput==null&&Active==null&&SelectedObjects().Count==0&&Marks.Count==2&&Marks[1].Text=="Keep these words",key+" saves and deselects new "+kind+" without adding a newline");Undo();check(Marks.Count==1,"Finished "+kind+" remains undoable after "+key);
        }
        LoadImageDocument(Blank(700,400),"Text keys");SetTool("Text");BeginInlineText(NewMark("Text",new PointF(90,90)),true);inlineInput.Text="First";inlineInput.Select(inlineInput.TextLength,0);FlushInlineUpdate();float height=inlineTarget.H;check(HandleInlineKey(Keys.Shift|Keys.Enter)&&inlineTarget!=null&&inlineInput.Text=="First\n"&&inlineTarget.H>height,"Shift+Enter expands the text box and keeps editing active");inlineInput.SelectedText="Second";HandleInlineKey(Keys.Enter);var saved=Marks[1];check(saved.Text=="First\nSecond","Enter commits all multiline text including queued typing");
        Active=saved;Selected.Clear();Selected.Add(saved);BeginInlineText(saved,false);inlineInput.SelectAll();inlineInput.SelectedText="Revised";HandleInlineKey(Keys.Escape);check(Marks[1].Text=="Revised"&&Active==null,"Escape preserves edits to an existing text object");
        saved=Marks[1];Active=saved;Selected.Add(saved);BeginInlineText(saved,false);Show();inlineInput.Focus();inlineInput.Select(0,1);var message=Message.Create(inlineInput.Handle,0x100,new IntPtr((int)Keys.Delete),IntPtr.Zero);ProcessCmdKey(ref message,Keys.Delete);check(inlineTarget==saved&&Marks.Count==2,"Delete command while the text caret is active cannot delete the annotation");SendMessage(inlineInput.Handle,0x100,new IntPtr((int)Keys.Delete),new IntPtr(1));FlushInlineUpdate();check(inlineTarget==saved&&inlineInput.Text=="evised","Delete edits selected characters while typing");HandleInlineKey(Keys.Enter);
        saved=Marks[1];SetTool("Select");Active=saved;Selected.Clear();Selected.Add(saved);SyncStyle();View.Focus();message=Message.Create(View.Handle,0x100,new IntPtr((int)Keys.Delete),IntPtr.Zero);check(ProcessCmdKey(ref message,Keys.Delete)&&Marks.Count==1,"Delete removes a selected text object outside typing mode");Hide();LoadBitmap(Blank(100,100),"Test");
    }
}
}
