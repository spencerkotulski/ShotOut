using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace ShotOut {
static class PopupTestInput {
    [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr window,int message,IntPtr wParam,IntPtr lParam);
    public static void RightClick(Control control){if(!Preferences.Testing)throw new InvalidOperationException("Popup simulation is only available during tests.");var point=control.PointToScreen(new Point(2,2));SendMessage(control.Handle,0x7B,control.Handle,new IntPtr((point.X&0xffff)|((point.Y&0xffff)<<16)));}
    public static void Click(ContextMenuStrip menu,int index){
        if(!Preferences.Testing)throw new InvalidOperationException("Popup simulation is only available during tests.");
        var bounds=menu.Items[index].Bounds;var point=new IntPtr((bounds.Left+bounds.Width/2)|((bounds.Top+bounds.Height/2)<<16));
        SendMessage(menu.Handle,0x200,IntPtr.Zero,point);SendMessage(menu.Handle,0x201,new IntPtr(1),point);SendMessage(menu.Handle,0x202,IntPtr.Zero,point);
    }
}
partial class Editor {
    public void VerifyMenuStability(List<string> report,string folder){
        Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception(label);report.Add("PASS "+label);};
        AnnotationLibrary.ResetForTests();var library=AnnotationLibrary.Current;var preset=AnnotationPreset.Capture("Blue & gold",new Mark {Kind="Rectangle",Color=Color.Blue.ToArgb(),FillColor=Color.Gold.ToArgb(),Fill=true,Width=6});library.Styles.Add(preset);
        LoadImageDocument(Blank(700,420),"Popup stability");SetTool("Rectangle");Show();Application.DoEvents();
        for(int i=0;i<4;i++){ShowAnnotationStyles();Application.DoEvents();check(annotationStylesPopup.Visible&&!annotationStylesPopup.IsDisposed,"Styles popup opens normally, cycle "+i);annotationStylesPopup.Close();Application.DoEvents();check(!annotationStylesPopup.IsDisposed,"Closing styles popup preserves its event target, cycle "+i);}
        ShowAnnotationStyles();Application.DoEvents();check(annotationStylesPopup.Items[0].Text.Contains("&&"),"Style names preserve literal ampersands in the menu");PopupTestInput.Click(annotationStylesPopup,0);Application.DoEvents();check(!annotationStylesPopup.IsDisposed&&NewMark("Rectangle",PointF.Empty).Color==Color.Blue.ToArgb(),"Native popup click applies a style after closing without disposing the menu");
        using(var timer=new Timer {Interval=20}){int attempts=0;timer.Tick+=delegate{var prompt=Application.OpenForms.Cast<Form>().OfType<ApplyPrompt>().FirstOrDefault(form=>form.Text.StartsWith("Save style"));if(prompt!=null){timer.Stop();check(!annotationStylesPopup.Visible&&!annotationStylesPopup.IsDisposed,"Style save prompt opens after the popup closes");prompt.Controls.OfType<TextBox>().Single().Text="Saved from popup";prompt.ApplyText();}else if(++attempts>100){timer.Stop();throw new Exception("Style popup did not open its save prompt");}};ShowAnnotationStyles();Application.DoEvents();int save=annotationStylesPopup.Items.Cast<ToolStripItem>().ToList().FindIndex(item=>item.Text=="Save current style…");timer.Start();PopupTestInput.Click(annotationStylesPopup,save);Application.DoEvents();check(library.Styles.Any(style=>style.Name=="Saved from popup"),"Save current style succeeds through the displayed native menu");}
        var a=new Mark {Kind="Rectangle",X=20,Y=40,W=60,H=30};var b=new Mark {Kind="Rectangle",X=240,Y=130,W=80,H=50};Marks.Add(a);Marks.Add(b);SetTool("Select");Active=b;Selected.Clear();Selected.Add(a);Selected.Add(b);SyncStyle();SyncLayers();ShowArrangeMenu();Application.DoEvents();PopupTestInput.Click(arrangePopup,0);Application.DoEvents();check(a.X==b.X&&!arrangePopup.IsDisposed,"Native alignment popup click completes without a disposed-menu error");
        using(var picker=new PaletteDialog(Color.CornflowerBlue)){picker.Show();Application.DoEvents();picker.VerifyFavoriteMenuCycles(report,folder);picker.Hide();}
        Hide();AnnotationLibrary.ResetForTests();LoadBitmap(Blank(100,100),"Test");
    }
}
partial class PaletteDialog {
    internal void VerifyFavoriteMenuCycles(List<string> report,string folder){
        Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception(label);report.Add("PASS "+label);};var library=AnnotationLibrary.Current;library.FavoriteColors.Clear();library.AddFavorite(Color.Coral);library.AddFavorite(Color.MidnightBlue);RefreshFavorites();
        var eyedropper=Controls.OfType<ModernButton>().Single(button=>button.AccessibleName=="Eyedropper");check(eyedropper.Text==""&&eyedropper.IconName=="Eyedropper"&&eyedropper.Width==34&&eyedropper.Top<320,"Eyedropper is a compact icon beside the color field");
        for(int i=0;i<4;i++){var button=favorites.Controls.OfType<ModernButton>().First();check(button.ContextMenuStrip==favoritePopup,"Favorite swatch uses its native right-click context menu, cycle "+i);PopupTestInput.RightClick(button);Application.DoEvents();check(favoritePopup.Visible,"Favorite right-click popup opens, cycle "+i);favoritePopup.Close();Application.DoEvents();check(!favoritePopup.IsDisposed&&library.FavoriteColors.Count==2,"Cancelling favorite popup leaves colors intact, cycle "+i);}
        var chosen=Chosen;var coral=favorites.Controls.OfType<ModernButton>().First(button=>button.BackColor.ToArgb()==Color.Coral.ToArgb());PopupTestInput.RightClick(coral);Application.DoEvents();PopupTestInput.Click(favoritePopup,0);Application.DoEvents();check(!library.FavoriteColors.Contains(Color.Coral.ToArgb())&&library.FavoriteColors.Contains(Color.MidnightBlue.ToArgb())&&!favoritePopup.IsDisposed,"Native Remove favorite click removes only its target and keeps the menu usable");check(Chosen==chosen,"Removing a favorite does not alter the selected annotation color");
        library.AddFavorite(Color.Coral);RefreshFavorites();coral=favorites.Controls.OfType<ModernButton>().First(button=>button.BackColor.ToArgb()==Color.Coral.ToArgb());
        using(var bitmap=new Bitmap(coral.Width,coral.Height)){coral.DrawToBitmap(bitmap,new Rectangle(Point.Empty,bitmap.Size));int left=bitmap.Width,right=-1,top=bitmap.Height,bottom=-1;for(int y=0;y<bitmap.Height;y++)for(int x=0;x<bitmap.Width;x++)if(bitmap.GetPixel(x,y).ToArgb()==Color.Coral.ToArgb()){left=Math.Min(left,x);right=Math.Max(right,x);top=Math.Min(top,y);bottom=Math.Max(bottom,y);}check(right>=left&&Math.Abs((left+right)/2f-coral.Width/2f)<=1&&Math.Abs((top+bottom)/2f-coral.Height/2f)<=1,"Favorite color swatch is centered horizontally and vertically");}
        using(var bitmap=new Bitmap(Width,Height)){DrawToBitmap(bitmap,new Rectangle(Point.Empty,bitmap.Size));bitmap.Save(Path.Combine(folder,"compact-color-picker.png"));}
    }
}
}
