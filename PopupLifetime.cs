using System;
using System.Windows.Forms;
namespace ShotOut {
static class PopupLifetime {
    // ToolStrip closes during its mouse/keyboard event. Keep the menu alive until
    // its owner closes, and run actions after that event stack has returned.
    public static void AfterClick(Control owner,Action action){
        if(owner.IsDisposed||owner.Disposing)return;
        if(!owner.IsHandleCreated){action();return;}
        owner.BeginInvoke((Action)delegate{if(!owner.IsDisposed&&!owner.Disposing)action();});
    }
    public static void Reset(ContextMenuStrip menu){menu.Close();while(menu.Items.Count>0){var item=menu.Items[0];menu.Items.RemoveAt(0);item.Dispose();}}
    public static ContextMenuStrip Own(Control owner,ContextMenuStrip menu){owner.Disposed+=delegate{menu.Dispose();};return menu;}
}
}
