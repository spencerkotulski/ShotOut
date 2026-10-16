using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace ShotOut {
public class TextRun { public string Text=""; public int Style; }
static class RichLayout {
    public static FontStyle BaseStyle(Mark m) {return (m.Bold?FontStyle.Bold:FontStyle.Regular)|(m.Italic?FontStyle.Italic:FontStyle.Regular)|(m.Underline?FontStyle.Underline:FontStyle.Regular);}
    public static void ApplyStyle(Mark m,bool bold,bool italic,bool underline) {
        foreach(var run in m.Runs??new List<TextRun>()) {int s=run.Style;if(m.Bold!=bold)s=bold?s|(int)FontStyle.Bold:s&~(int)FontStyle.Bold;if(m.Italic!=italic)s=italic?s|(int)FontStyle.Italic:s&~(int)FontStyle.Italic;if(m.Underline!=underline)s=underline?s|(int)FontStyle.Underline:s&~(int)FontStyle.Underline;run.Style=s;}
        m.Bold=bold;m.Italic=italic;m.Underline=underline;
    }
    public static List<TextRun> Capture(RichTextBox input) {
        int start=input.SelectionStart,length=input.SelectionLength;var result=new List<TextRun>();
        for(int i=0;i<input.TextLength;i++) {input.Select(i,1);int style=(int)(input.SelectionFont==null?input.Font.Style:input.SelectionFont.Style);string letter=input.Text.Substring(i,1);if(result.Count>0&&result[result.Count-1].Style==style)result[result.Count-1].Text+=letter;else result.Add(new TextRun{Text=letter,Style=style});}
        input.Select(start,length);return result;
    }
    public static void Toggle(RichTextBox input,FontStyle flag) {
        int start=input.SelectionStart,length=input.SelectionLength;if(length==0) {var font=input.SelectionFont??input.Font;using(var f=new Font(font,(font.Style&flag)!=0?font.Style&~flag:font.Style|flag))input.SelectionFont=f;return;}
        bool remove=true;for(int i=start;i<start+length;i++){input.Select(i,1);if(((input.SelectionFont??input.Font).Style&flag)==0)remove=false;}
        for(int i=start;i<start+length;i++){input.Select(i,1);var font=input.SelectionFont??input.Font;using(var f=new Font(font,remove?font.Style&~flag:font.Style|flag))input.SelectionFont=f;}input.Select(start,length);input.Focus();
    }
    public static SizeF NaturalSize(Graphics graphics,Mark mark){
        var runs=mark.Runs;bool rich=runs!=null&&runs.Count>0&&String.Concat(runs.Select(run=>run.Text))==mark.Text;
        var styles=new List<int>();if(rich)foreach(var run in runs)for(int i=0;i<run.Text.Length;i++)styles.Add(run.Style&7);
        var fonts=new Dictionary<int,Font>();float longest=0,lineWidth=0,lineHeight=0;int lines=1;
        using(var format=(StringFormat)StringFormat.GenericTypographic.Clone()){
            format.FormatFlags|=StringFormatFlags.MeasureTrailingSpaces|StringFormatFlags.NoWrap;
            Func<int,Font> getFont=delegate(int style){Font font;if(!fonts.TryGetValue(style,out font)){font=new Font("Segoe UI",mark.Size,(FontStyle)style,GraphicsUnit.Pixel);fonts[style]=font;}return font;};
            try {
                if(!rich){var font=getFont((int)BaseStyle(mark));lineHeight=font.GetHeight(graphics);var parts=mark.Text.Replace("\r","").Split('\n');lines=parts.Length;foreach(var part in parts)longest=Math.Max(longest,graphics.MeasureString(part,font,PointF.Empty,format).Width);}
                else {for(int i=0;i<mark.Text.Length;i++){char c=mark.Text[i];if(c=='\r')continue;if(c=='\n'){longest=Math.Max(longest,lineWidth);lineWidth=0;lines++;continue;}var font=getFont(styles[i]);lineWidth+=graphics.MeasureString(c.ToString(),font,PointF.Empty,format).Width;lineHeight=Math.Max(lineHeight,font.GetHeight(graphics));}longest=Math.Max(longest,lineWidth);lineHeight=Math.Max(lineHeight,mark.Size*1.3f);}
                return new SizeF(Math.Max(1,longest),Math.Max(1,lines*lineHeight));
            }finally{foreach(var font in fonts.Values)font.Dispose();}
        }
    }
    public static int Alignment(Mark mark){return mark.TextAlignment<0?(mark.Kind=="Balloon"?1:0):Math.Max(0,Math.Min(2,mark.TextAlignment));}
    public static void Draw(Graphics g,Mark mark,RectangleF bounds) {
        if(mark.Kind=="Text"){bounds.X-=mark.TextInsetLeft;bounds.Y-=mark.TextInsetTop;bounds.Width+=mark.TextInsetLeft+mark.TextInsetRight;bounds.Height+=mark.TextInsetTop+mark.TextInsetBottom;}
        var runs=mark.Runs;int alignment=Alignment(mark);
        if(runs==null||runs.Count==0||String.Concat(runs.Select(x=>x.Text))!=mark.Text) {
            using(var f=new Font(mark.Kind=="Emoji"?"Segoe UI Emoji":"Segoe UI",mark.Size,BaseStyle(mark),GraphicsUnit.Pixel))using(var brush=new SolidBrush(Color.FromArgb(mark.Color)))using(var format=(StringFormat)StringFormat.GenericTypographic.Clone()){format.Alignment=(StringAlignment)alignment;format.LineAlignment=mark.Kind=="Balloon"?StringAlignment.Center:StringAlignment.Near;g.DrawString(mark.Text,f,brush,bounds,format);}return;
        }
        var text=mark.Text;var styles=new List<int>();foreach(var run in runs)for(int i=0;i<run.Text.Length;i++)styles.Add(run.Style&7);
        var fonts=new Dictionary<int,Font>();var widths=new Dictionary<string,float>();var state=g.Save();
        using(var brush=new SolidBrush(Color.FromArgb(mark.Color)))using(var format=(StringFormat)StringFormat.GenericTypographic.Clone()) {
            format.FormatFlags|=StringFormatFlags.MeasureTrailingSpaces|StringFormatFlags.NoWrap;g.SetClip(bounds,System.Drawing.Drawing2D.CombineMode.Intersect);
            try {
                Func<int,Font> font=delegate(int style){if(!fonts.ContainsKey(style))fonts[style]=new Font("Segoe UI",mark.Size,(FontStyle)style,GraphicsUnit.Pixel);return fonts[style];};
                Func<int,float> width=delegate(int index){string key=styles[index]+":"+text[index];float w;if(!widths.TryGetValue(key,out w)){w=g.MeasureString(text[index].ToString(),font(styles[index]),PointF.Empty,format).Width;widths[key]=w;}return w;};
                var lines=new List<List<int>>();var line=new List<int>();float used=0;
                for(int i=0;i<text.Length;i++) {
                    char c=text[i];if(c=='\r')continue;if(c=='\n'){lines.Add(line);line=new List<int>();used=0;continue;}
                    if(!Char.IsWhiteSpace(c)&&(i==0||Char.IsWhiteSpace(text[i-1]))) {float word=0;for(int j=i;j<text.Length&&!Char.IsWhiteSpace(text[j]);j++)word+=width(j);if(used>0&&word<=bounds.Width&&used+word>bounds.Width){lines.Add(line);line=new List<int>();used=0;} }
                    float w=width(i);if(used+w>bounds.Width&&used>0){lines.Add(line);line=new List<int>();used=0;}line.Add(i);used+=w;
                }
                lines.Add(line);float lineHeight=mark.Size*1.3f;int visible=Math.Min(lines.Count,Math.Max(1,(int)Math.Floor((bounds.Height-mark.Size)/lineHeight)+1));float blockHeight=(visible-1)*lineHeight+mark.Size*1.25f;float y=bounds.Top+(mark.Kind=="Balloon"?Math.Max(0,(bounds.Height-blockHeight)/2):0);
                foreach(var row in lines){int count=row.Count;while(count>0&&Char.IsWhiteSpace(text[row[count-1]]))count--;float length=0;for(int i=0;i<count;i++)length+=width(row[i]);float x=bounds.Left+(alignment==1?(bounds.Width-length)/2:alignment==2?bounds.Width-length:0);
                    if(y+mark.Size>bounds.Bottom)break;foreach(int i in row){g.DrawString(text[i].ToString(),font(styles[i]),brush,new PointF(x,y),format);x+=width(i);}y+=mark.Size*1.3f;
                }
            }finally{foreach(var f in fonts.Values)f.Dispose();g.Restore(state);}
        }
    }

}
static class RichTextEntry {
    public static string Ask(IWin32Window owner,Mark mark) {
        using(var form=new ApplyPrompt {Text="Edit annotation",ClientSize=new Size(570,370),StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,BackColor=Color.FromArgb(30,32,38),ForeColor=Color.White,Font=new Font("Segoe UI",13,FontStyle.Regular,GraphicsUnit.Pixel)}) {
            var input=new RichTextBox {Location=new Point(20,64),Size=new Size(530,224),BackColor=Color.FromArgb(43,46,54),ForeColor=Color.White,BorderStyle=BorderStyle.None,DetectUrls=false,AcceptsTab=true,Font=new Font("Segoe UI",mark.Size,RichLayout.BaseStyle(mark),GraphicsUnit.Pixel),Text=mark.Text};
            var bar=new FlowLayoutPanel {Location=new Point(17,14),Size=new Size(535,40)};
            foreach(var entry in new[]{new {Text="Bold",Style=FontStyle.Bold},new {Text="Italic",Style=FontStyle.Italic},new {Text="Underline",Style=FontStyle.Underline}}) {var flag=entry.Style;var b=new ModernButton{Text=entry.Text,Width=100,Height=34,Margin=new Padding(3),BackColor=Color.FromArgb(43,46,54),ForeColor=Color.White,Font=form.Font,TabStop=false};b.Click+=delegate {RichLayout.Toggle(input,flag);};bar.Controls.Add(b);}
            form.Controls.Add(bar);form.Controls.Add(input);
            if(mark.Runs!=null&&String.Concat(mark.Runs.Select(x=>x.Text))==mark.Text) {int pos=0;foreach(var run in mark.Runs){input.Select(pos,run.Text.Length);using(var f=new Font("Segoe UI",mark.Size,(FontStyle)(run.Style&7),GraphicsUnit.Pixel))input.SelectionFont=f;pos+=run.Text.Length;} }
            var apply=new ModernButton {Text="Apply",Location=new Point(348,316),Size=new Size(96,36),BackColor=Color.FromArgb(84,119,239),ForeColor=Color.White,Font=form.Font,DialogResult=DialogResult.OK};var cancel=new ModernButton{Text="Cancel",Location=new Point(454,316),Size=new Size(96,36),BackColor=Color.FromArgb(43,46,54),ForeColor=Color.White,Font=form.Font,DialogResult=DialogResult.Cancel};form.Controls.Add(new Label{Text="Ctrl+Enter to apply\nCtrl+B / I / U formats selection",Location=new Point(20,313),Size=new Size(315,44),ForeColor=Color.FromArgb(160,170,190)});form.Controls.Add(apply);form.Controls.Add(cancel);form.CancelButton=cancel;
            input.KeyDown+=delegate(object s,KeyEventArgs e){if(e.Control&&e.KeyCode==Keys.Enter){e.Handled=e.SuppressKeyPress=true;apply.PerformClick();}else if(e.Control&&(e.KeyCode==Keys.B||e.KeyCode==Keys.I||e.KeyCode==Keys.U)){e.Handled=e.SuppressKeyPress=true;RichLayout.Toggle(input,e.KeyCode==Keys.B?FontStyle.Bold:e.KeyCode==Keys.I?FontStyle.Italic:FontStyle.Underline);} };
            form.Shown+=delegate{input.Focus();input.SelectAll();};if(form.ShowDialog(owner)!=DialogResult.OK)return null;mark.Runs=RichLayout.Capture(input);mark.Text=input.Text;return input.Text;
        }
    }
}
partial class Editor {
    public void VerifySettingsAndText(List<string> report,string folder) {
        Preferences.Testing=true;Action<bool,string> check=delegate(bool ok,string label){if(!ok)throw new Exception(label);report.Add("PASS "+label);};
        var p=new Preferences();p.AllowMultiple=true;p.CaptureDelay=4;p.IncludeCursor=true;p.Tools.First(x=>x.Action=="Select").Chord=(int)Keys.Y;p.Save(System.IO.Path.Combine(folder,"preferences-test.xml"));var saved=Preferences.Read(System.IO.Path.Combine(folder,"preferences-test.xml"));check(saved.AllowMultiple&&saved.CaptureDelay==4&&saved.IncludeCursor&&saved.ToolMap()[Keys.Y]=="Select","Preferences and custom tool hotkeys persist");
        using(var settings=new SettingsDialog(saved,false)) {settings.Show();Application.DoEvents();using(var preview=new Bitmap(settings.Width,settings.Height)){settings.DrawToBitmap(preview,new Rectangle(Point.Empty,preview.Size));preview.Save(System.IO.Path.Combine(folder,"settings-interface.png"));}settings.Hide();}
        var copy=saved.Clone();check(copy.Tools.Count==new Preferences().Tools.Count&&copy.Capture.Count==3,"Settings clone preserves unique bindings");copy.Tools[1].Chord=copy.Tools[0].Chord;bool rejected=false;try{copy.Validate();}catch(ArgumentException){rejected=true;}check(rejected,"Duplicate hotkeys rejected");check(LoginStartup.Command=="\""+Application.ExecutablePath+"\" --startup","Startup command quotes the executable path");
        using(var input=new RichTextBox {Text="Bold plain italic underlined",Font=new Font("Segoe UI",18,FontStyle.Regular,GraphicsUnit.Pixel)}) {
            input.Select(0,4);RichLayout.Toggle(input,FontStyle.Bold);input.Select(11,6);RichLayout.Toggle(input,FontStyle.Italic);input.Select(18,10);RichLayout.Toggle(input,FontStyle.Underline);var runs=RichLayout.Capture(input);
            check(runs.Any(x=>x.Text.Contains("Bold")&&(x.Style&(int)FontStyle.Bold)!=0)&&runs.Any(x=>x.Text.Contains("italic")&&(x.Style&(int)FontStyle.Italic)!=0)&&runs.Any(x=>x.Text.Contains("underlined")&&(x.Style&(int)FontStyle.Underline)!=0),"Per-selection bold italic and underline captured");
            var mark=new Mark {Kind="Text",Text=input.Text,Runs=runs,X=10,Y=10,W=500,H=120,Size=22,Bold=false,Color=Color.Black.ToArgb()};using(var bg=Blank(550,150))using(var rendered=Art.Render(bg,new List<Mark>{mark}))rendered.Save(System.IO.Path.Combine(folder,"rich-text.png"));
            var cloned=mark.Clone();cloned.Runs[0].Style=0;check(mark.Runs[0].Style!=0,"Undo clones rich formatting independently");var project=new Project {Background=new byte[]{1},Marks=new List<Mark>{mark}};using(var stream=new System.IO.MemoryStream()){var xml=new System.Xml.Serialization.XmlSerializer(typeof(Project));xml.Serialize(stream,project);stream.Position=0;var roundtrip=(Project)xml.Deserialize(stream);check(roundtrip.Marks[0].Runs.Count==runs.Count&&roundtrip.Marks[0].Runs.Last().Style==runs.Last().Style,"Rich formatting survives editable project round trip");}
        }
    }
}
}
