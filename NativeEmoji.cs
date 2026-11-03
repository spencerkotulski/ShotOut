using System;using System.Drawing;using System.Runtime.InteropServices;using System.Windows.Forms;
namespace ShotOut {
static class NativeEmojiPicker {
    [StructLayout(LayoutKind.Sequential)]struct Keyboard {public ushort Key,Scan;public uint Flags,Time;public UIntPtr Extra;}
    [StructLayout(LayoutKind.Sequential)]struct Mouse {public int X,Y;public uint Data,Flags,Time;public UIntPtr Extra;}
    [StructLayout(LayoutKind.Explicit)]struct InputData {[FieldOffset(0)]public Keyboard Keyboard;[FieldOffset(0)]public Mouse Mouse;}
    [StructLayout(LayoutKind.Sequential)]struct Input {public uint Type;public InputData Data;}
    [DllImport("user32.dll",SetLastError=true)]static extern uint SendInput(uint count,Input[] input,int size);
    static Input Key(ushort key,bool up){return new Input{Type=1,Data=new InputData{Keyboard=new Keyboard{Key=key,Flags=up?2u:0u}}};}
    internal static bool Open(){if(Preferences.Testing)return false;var keys=new[]{Key(0x5b,false),Key(0xbe,false),Key(0xbe,true),Key(0x5b,true)};return SendInput(4,keys,Marshal.SizeOf(typeof(Input)))==4;}
}
sealed class EmojiInsertDialog : Form {
    internal readonly TextBox Entry;internal string Value {get{return Entry.Text.Trim();}}
    internal EmojiInsertDialog(string initial){Text="Choose emoji";ClientSize=new Size(420,200);StartPosition=FormStartPosition.CenterParent;FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=MinimizeBox=false;BackColor=Color.FromArgb(30,32,38);ForeColor=Color.White;Font=new Font("Segoe UI",13,FontStyle.Regular,GraphicsUnit.Pixel);
        Controls.Add(new Label{Text="Choose in the Windows panel, then click Place emoji.\nYou can also paste an emoji here. Win + . reopens the picker.",Location=new Point(16,12),Size=new Size(390,42)});
        Entry=new ThemedTextBox{Text=initial??"",Font=new Font("Segoe UI Emoji",30,FontStyle.Regular,GraphicsUnit.Pixel),Location=new Point(16,60),Size=new Size(386,52),BackColor=Color.FromArgb(43,46,54),ForeColor=Color.White,BorderStyle=BorderStyle.FixedSingle};Controls.Add(Entry);var colorPreview=new ColorEmojiEntryPreview(Entry){Bounds=Entry.Bounds,BackColor=Entry.BackColor};Controls.Add(colorPreview);colorPreview.BringToFront();Entry.BorderStyle=BorderStyle.None;Entry.Width=1;Entry.AccessibleName="Emoji selection";
        var picker=new ModernButton{Text="Windows emoji…",Location=new Point(16,132),Size=new Size(150,38),BackColor=Color.FromArgb(43,46,54),ForeColor=ForeColor,Font=Font};picker.Click+=delegate{Entry.Focus();Entry.SelectionStart=Entry.TextLength;NativeEmojiPicker.Open();};Controls.Add(picker);
        var cancel=new ModernButton{Text="Cancel",Location=new Point(178,132),Size=new Size(84,38),BackColor=Color.FromArgb(43,46,54),ForeColor=ForeColor,Font=Font,DialogResult=DialogResult.Cancel};Controls.Add(cancel);
        var insert=new ModernButton{Text="Place emoji",Location=new Point(272,132),Size=new Size(130,38),BackColor=UiTheme.Accent,ForeColor=ForeColor,Font=Font,Enabled=!String.IsNullOrWhiteSpace(initial)};insert.Click+=delegate{if(Value.Length>0){DialogResult=DialogResult.OK;Close();}};Controls.Add(insert);Entry.TextChanged+=delegate{insert.Enabled=Value.Length>0;};AcceptButton=insert;CancelButton=cancel;
        Shown+=delegate{Entry.Focus();Entry.SelectAll();if(!Preferences.Testing)BeginInvoke((Action)delegate{if(!IsDisposed&&ContainsFocus)NativeEmojiPicker.Open();});};
    }
}
partial class Editor {
    void EditNativeEmoji(Mark mark){using(var dialog=new EmojiInsertDialog(mark.Text))if(dialog.ShowDialog(this)==DialogResult.OK){Push();mark.Text=dialog.Value;mark.FontFamily="Segoe UI Emoji";GrowTextBox(mark,mark.Size);EnsureCanvasFitsMarks();RefreshImage();}}
    void PlaceNativeEmoji(PointF point){using(var dialog=new EmojiInsertDialog(""))if(dialog.ShowDialog(this)==DialogResult.OK)InsertEmojiAt(point,dialog.Value);}
    void InsertEmojiAt(PointF point,string value){if(String.IsNullOrWhiteSpace(value))return;var mark=NewMark("Emoji",point);mark.Text=value;mark.FontFamily="Segoe UI Emoji";mark.Size=46;mark.W=mark.H=72;GrowTextBox(mark,mark.Size);Push();Marks.Add(mark);Active=mark;EnsureCanvasFitsMarks();RefreshImage();TouchTabActivity();}
}
}

