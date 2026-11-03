using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
namespace ShotOut {
sealed class ShortcutGuideForm : Form {
    readonly RichTextBox body;readonly TextBox search;readonly Label status;readonly ListBox sections;
    readonly List<int> sectionOffsets=new List<int>();readonly Font normalFont,headingFont,titleFont,keyFont;
    public ShortcutGuideForm(){
        Text="ShotOut 1.4.3 shortcuts and mouse controls";StartPosition=FormStartPosition.CenterParent;Size=new Size(1000,760);MinimumSize=new Size(760,500);BackColor=Color.FromArgb(30,32,38);ForeColor=Color.FromArgb(235,238,245);Font=new Font("Segoe UI",10);
        normalFont=new Font("Segoe UI",10.5f);headingFont=new Font("Segoe UI",15,FontStyle.Bold);titleFont=new Font("Segoe UI",20,FontStyle.Bold);keyFont=new Font("Segoe UI",10.5f,FontStyle.Bold);
        var top=new FlowLayoutPanel {Dock=DockStyle.Top,Height=56,Padding=new Padding(12,10,12,6),WrapContents=false};
        top.Controls.Add(new Label {Text="Find in guide",AutoSize=true,Margin=new Padding(0,7,8,0)});search=new ThemedTextBox {Width=250,BackColor=Color.FromArgb(43,46,54),ForeColor=ForeColor,BorderStyle=BorderStyle.FixedSingle,AccessibleName="Find in shortcut guide"};top.Controls.Add(search);
        var next=new ModernButton {Text="Find next",Width=105,Height=30,BackColor=UiTheme.Accent,ForeColor=ForeColor};next.Click+=delegate{Find(false);};top.Controls.Add(next);status=new Label {Width=250,Height=30,Text="Ctrl+F to find · Esc to close",TextAlign=ContentAlignment.MiddleLeft};top.Controls.Add(status);
        sections=new ListBox {Dock=DockStyle.Left,Width=230,BackColor=Color.FromArgb(25,27,33),ForeColor=ForeColor,BorderStyle=BorderStyle.None,Font=Font,IntegralHeight=false,ItemHeight=25,AccessibleName="Guide sections"};
        body=new RichTextBox {Dock=DockStyle.Fill,ReadOnly=true,HideSelection=false,DetectUrls=false,BackColor=BackColor,ForeColor=ForeColor,BorderStyle=BorderStyle.None,Font=normalFont,ScrollBars=RichTextBoxScrollBars.Vertical,WordWrap=true,AccessibleName="Shortcut guide content"};
        Controls.Add(body);Controls.Add(sections);Controls.Add(top);search.KeyDown+=delegate(object sender,KeyEventArgs e){if(e.KeyCode==Keys.Enter){Find(e.Shift);e.Handled=e.SuppressKeyPress=true;}};
        sections.SelectedIndexChanged+=delegate{if(sections.SelectedIndex<0)return;body.Select(sectionOffsets[sections.SelectedIndex],0);body.ScrollToCaret();};
        using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("ShotOut.Shortcuts"))using(var reader=new StreamReader(stream))Render(reader.ReadToEnd());
        AddHeading("Your current custom bindings",false);Add("These are your saved bindings. The tool table above shows factory defaults.\n\n",normalFont);
        foreach(var binding in Preferences.Current.Capture)Add("Capture "+binding.Action+" — "+Preferences.KeyLabel(binding.Chord)+"\n",normalFont);
        Add("Additional Print Screen region capture — "+(Preferences.Current.UsePrintScreen?"Enabled (Windows screen-snipping shortcut must be off)":"Disabled")+"\n",normalFont);
        foreach(var binding in Preferences.Current.Tools)Add(Editor.ToolDisplayName(binding.Action)+" — "+Preferences.KeyLabel(binding.Chord)+"\n",normalFont);
        body.Select(0,0);Shown+=delegate{body.Select(0,0);body.ScrollToCaret();};
    }
    void Add(string text,Font font){body.Select(body.TextLength,0);body.SelectionFont=font;body.SelectionColor=ForeColor;body.AppendText(text);}
    void AddHeading(string text,bool title){sectionOffsets.Add(body.TextLength);string label=text;switch(text){case "ShotOut shortcuts and mouse controls":label="Overview";break;case "Capture from any app":label="Capture shortcuts";break;case "Region and smart region capture":label="Region selection";break;case "New tab and image or project library":label="Image and project library";break;case "Tabs and files in the editor":label="Tabs and files";break;case "Canvas navigation and selection":label="Canvas and selection";break;case "Drawing and quick size adjustments":label="Size and drawing";break;case "Copy undo and layer organization":label="Copy undo and layers";break;case "Clone stamp and color picker":label="Clone stamp and colors";break;case "Settings and other menus":label="Settings and menus";break;case "Practice a fast workflow":label="Practice workflow";break;case "Your current custom bindings":label="Your current bindings";break;}sections.Items.Add(label);Add(text+"\n\n",title?titleFont:headingFont);}
    void Render(string markdown){foreach(string raw in markdown.Replace("\r","").Split('\n')){string line=raw.Trim();if(line.Length==0)continue;if(line.StartsWith("# ")){AddHeading(line.Substring(2),true);continue;}if(line.StartsWith("## ")){AddHeading(line.Substring(3),false);continue;}if(line.StartsWith("|")){var cells=line.Trim('|').Split('|').Select(s=>s.Trim()).ToArray();if(cells.Length<2||cells[0].StartsWith("---")||cells[0]=="Shortcut or gesture"||cells[0]=="Key")continue;Add(cells[0]+"\n",keyFont);Add(cells[1]+"\n\n",normalFont);}else Add(line+"\n\n",normalFont);}}
    void Find(bool backwards){string query=search.Text;if(String.IsNullOrWhiteSpace(query)){search.Focus();return;}int found;if(backwards){int end=Math.Max(0,body.SelectionStart);found=body.Find(query,0,end,RichTextBoxFinds.Reverse);if(found<0)found=body.Find(query,RichTextBoxFinds.Reverse);}else{int start=Math.Min(body.TextLength,body.SelectionStart+body.SelectionLength);found=body.Find(query,start,RichTextBoxFinds.None);if(found<0)found=body.Find(query,RichTextBoxFinds.None);}status.Text=found<0?"No match found":"Match found · F3 for next";if(found>=0){body.Select(found,query.Length);body.ScrollToCaret();}}
    protected override bool ProcessCmdKey(ref Message message,Keys key){if(key==Keys.Escape){Close();return true;}if(key==(Keys.Control|Keys.F)){search.Focus();search.SelectAll();return true;}if(key==Keys.F3||key==(Keys.Shift|Keys.F3)){Find((key&Keys.Shift)!=0);return true;}return base.ProcessCmdKey(ref message,key);}
    protected override void Dispose(bool disposing){base.Dispose(disposing);if(disposing){normalFont.Dispose();headingFont.Dispose();titleFont.Dispose();keyFont.Dispose();}}
}
partial class Editor {
    ShortcutGuideForm shortcutGuide;
    void ShowShortcutGuide(){if(shortcutGuide!=null&&!shortcutGuide.IsDisposed){shortcutGuide.Activate();return;}shortcutGuide=new ShortcutGuideForm();shortcutGuide.FormClosed+=delegate{shortcutGuide=null;};shortcutGuide.Show(this);}
}
}

