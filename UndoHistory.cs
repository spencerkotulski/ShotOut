using System;using System.Drawing;using System.Linq;using System.Text.RegularExpressions;using System.Windows.Forms;
namespace ShotOut {partial class Editor {
    string DescribeHistoryAction(string operation){
        switch(operation){case "CanvasDownWithModifiers":return resizing?"Resize selection":moving?"Move selection":"Draw "+(draft==null?tool:draft.Kind).ToLowerInvariant();case "ChangeStyle":return "Change annotation style";case "ChangeTextSize":return "Change text size";case "ChangeImageAdjustment":return "Adjust image";case "ChangeBackgroundControls":return "Change background";case "HandleCloneDown":return "Clone stamp stroke";case "CommitCrop":return tool.StartsWith("Cut ")?"Cut out strip":"Crop image";case "Delete":return "Delete selection";case "Duplicate":return "Duplicate selection";case "PasteCopiedAnnotations":return "Paste annotations";case "PasteFromData":return "Paste text";case "AddImageAt":case "AddImage":return "Insert image";case "CloseInlineRename":return "Rename layer";case "FinishInlineText":case "BeginInlineText":return "Edit text";case "ArrangeSelection":return "Align or distribute selection";case "ApplyEditableCrop":return "Crop image";case "Transform":return tool.StartsWith("Cut ")?"Cut out strip":"Transform image";}
        if(String.IsNullOrEmpty(operation))return "Edit image";return Regex.Replace(operation,"([a-z])([A-Z])","$1 $2");
    }
    void SettleHistoryInteraction(){FinishBackgroundAdjustment();FinishImageAdjustment();AcceptCut();FinishInlineText(true);FinishCloneStroke();FinishStrokeAdjustment();CloseInlineRename(true);}
    void NavigateHistory(bool forward,int count){
        SettleHistoryInteraction();var source=forward?redo:undo;var destination=forward?undo:redo;if(count<1||count>source.Count)return;
        // Transfer the intermediate states, then render only the chosen destination.
        var state=Snapshot();for(int i=0;i<count;i++){var target=source[source.Count-1];state.HistoryAction=target.HistoryAction;destination.Add(state);source.RemoveAt(source.Count-1);state=target;}
        Restore(state);TouchTabActivity();documentCopied=false;UpdateStatus();
    }
    ModernButton CreateHistoryButton(bool forward){var button=ActionButton("",forward?"Redo":"Undo",forward?"Redo (Ctrl+Y) · Right-click for history":"Undo (Ctrl+Z) · Right-click for history",forward?(Action)Redo:Undo,36);button.MouseUp+=delegate(object sender,MouseEventArgs e){if(e.Button==MouseButtons.Right)Guard(delegate{ShowHistoryMenu(button,forward);});};return button;}
    ContextMenuStrip CreateHistoryMenu(bool forward){
        SettleHistoryInteraction();var menu=DarkMenu();menu.MaximumSize=new Size(480,Math.Max(180,Math.Min(480,Screen.FromControl(this).WorkingArea.Height-80)));var source=forward?redo:undo;
        if(source.Count==0){menu.Items.Add(forward?"Nothing to redo":"Nothing to undo").Enabled=false;return menu;}
        object document=currentDocument>=0&&currentDocument<documents.Count?documents[currentDocument]:null;
        for(int steps=1;steps<=source.Count;steps++){int count=steps;var target=source[source.Count-steps];string action=String.IsNullOrEmpty(target.HistoryAction)?"Edit image":target.HistoryAction;var item=menu.Items.Add(count+" · "+action);item.ToolTipText=(forward?"Redo ":"Undo ")+count+(count==1?" action":" actions");item.Click+=delegate{PopupLifetime.AfterClick(this,delegate{if(currentDocument<0||currentDocument>=documents.Count||!Object.ReferenceEquals(document,documents[currentDocument]))return;var stack=forward?redo:undo;if(stack.Count<count||!Object.ReferenceEquals(stack[stack.Count-count],target)){UpdateStatus("History changed. Open the history menu again.");return;}Guard(delegate{NavigateHistory(forward,count);});});};}
        menu.ShowItemToolTips=true;return menu;
    }
    void ShowHistoryMenu(Control button,bool forward){var menu=CreateHistoryMenu(forward);menu.Closed+=delegate{PopupLifetime.AfterClick(this,delegate{menu.Dispose();});};menu.Show(button,new Point(0,button.Height+4));}
}}
