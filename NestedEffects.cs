using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace ShotOut {
// Display rows reference image settings; they never enter the document's layer stack.
sealed class ImageEffectRow : Mark {
    internal Mark ParentImage;
    internal ImageAdjustmentZone Zone;
    internal string Effect;
}
partial class Editor {
    readonly Dictionary<Mark,Dictionary<string,ImageEffectRow>> imageEffectRows=new Dictionary<Mark,Dictionary<string,ImageEffectRow>>();
    ImageEffectRow selectedEffectRow;
    static bool SavedGeneralSettings(Mark image){return image.Exposure!=0||image.Contrast!=0||image.Brightness!=0||image.Saturation!=0||image.Temperature!=0;}
    List<Mark> DesiredLayerRows(){
        foreach(var stale in imageEffectRows.Keys.Where(mark=>!Marks.Contains(mark)).ToList())imageEffectRows.Remove(stale);var result=new List<Mark>();
        foreach(var image in Marks.AsEnumerable().Reverse()){result.Add(image);if(image.Kind!="Image")continue;Dictionary<string,ImageEffectRow> rows;if(!imageEffectRows.TryGetValue(image,out rows)){rows=new Dictionary<string,ImageEffectRow>();imageEffectRows[image]=rows;}var effects=new List<Tuple<string,string,ImageAdjustmentZone>>();if(SavedGeneralSettings(image))effects.Add(Tuple.Create("General","General adjustments",(ImageAdjustmentZone)null));if(image.ImageFilter>0)effects.Add(Tuple.Create("Filter","Filter · "+FilterNames[Math.Max(0,Math.Min(3,image.ImageFilter))],(ImageAdjustmentZone)null));for(int i=0;i<image.ImageZones.Count;i++)effects.Add(Tuple.Create("Zone:"+image.ImageZones[i].Id,"Zone "+(i+1),image.ImageZones[i]));
            var valid=new HashSet<string>();foreach(var entry in Art.ImageEffectDisplayOrder(image).Select(key=>effects.First(effect=>effect.Item1==key))){valid.Add(entry.Item1);ImageEffectRow row;if(!rows.TryGetValue(entry.Item1,out row)){row=new ImageEffectRow {Kind="Image effect",ParentImage=image,Effect=entry.Item1};rows.Add(entry.Item1,row);}row.Name=entry.Item2;row.Zone=entry.Item3;row.Visible=row.Zone!=null?row.Zone.Settings.ImageAdjustmentsEnabled:row.Effect=="Filter"?image.ImageFilterEnabled:image.ImageAdjustmentsEnabled;result.Add(row);}foreach(var stale in rows.Keys.Where(key=>!valid.Contains(key)).ToList())rows.Remove(stale);
        }if(selectedEffectRow!=null&&!result.Contains(selectedEffectRow))selectedEffectRow=null;return result;
    }
    bool DrawImageEffectRow(DrawItemEventArgs e){var row=layers.Items[e.Index] as ImageEffectRow;if(row==null)return false;bool hover=e.Index==layerHover,selected=Object.ReferenceEquals(row,selectedEffectRow);using(var background=new SolidBrush(selected?UiTheme.SelectionSurface:hover?Color.FromArgb(39,44,55):layers.BackColor))e.Graphics.FillRectangle(background,e.Bounds);DrawLayerInsertion(e);using(var branch=new Pen(Color.FromArgb(72,80,96))){e.Graphics.DrawLine(branch,23,e.Bounds.Top,23,e.Bounds.Top+22);e.Graphics.DrawLine(branch,23,e.Bounds.Top+22,31,e.Bounds.Top+22);}Glyph.Draw(e.Graphics,row.Zone!=null?"Ellipse":row.Effect=="Filter"?"Effects":"Adjust",new Rectangle(34,e.Bounds.Y+13,16,16),row.Visible&&row.ParentImage.Visible?Color.FromArgb(166,185,216):muted);TextRenderer.DrawText(e.Graphics,row.Name,Font,new Rectangle(57,e.Bounds.Y,Math.Max(10,e.Bounds.Width-87),e.Bounds.Height),row.Visible&&row.ParentImage.Visible?text:muted,TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);DrawEye(e.Graphics,new Rectangle(e.Bounds.Right-23,e.Bounds.Y+15,15,14),row.Visible);return true;}
    internal bool HandleImageEffectMouse(MouseEventArgs e){int index=layers.IndexFromPoint(e.Location);if(index<0)return false;var row=layers.Items[index] as ImageEffectRow;effectDragCandidate=null;if(row==null){selectedEffectRow=null;return false;}selectedEffectRow=row;layerDragging=null;Active=null;Selected.Clear();bool previous=syncing;syncing=true;layers.ClearSelected();syncing=previous;layers.Invalidate();View.Invalidate();
        if(e.Button==MouseButtons.Left&&e.X>=layers.ClientSize.Width-UiPixels(30)){ToggleImageEffect(row);return true;}if(e.Button==MouseButtons.Right){var menu=DarkMenu();menu.Items.Add("Edit effect",null,delegate{OpenImageEffectRow(row);});menu.Items.Add(row.Visible?"Hide effect":"Show effect",null,delegate{ToggleImageEffect(row);});menu.Items.Add("Remove effect",null,delegate{RemoveImageEffect(row);});menu.Show(layers,e.Location);return true;}if(e.Button==MouseButtons.Left){effectDragCandidate=row;effectMouseDown=e.Location;layers.Focus();}return true;
    }
    void ToggleImageEffect(ImageEffectRow row){if(!Marks.Contains(row.ParentImage))return;FinishImageAdjustment();Push();if(row.Zone!=null)row.Zone.Settings.ImageAdjustmentsEnabled=!row.Zone.Settings.ImageAdjustmentsEnabled;else if(row.Effect=="Filter")row.ParentImage.ImageFilterEnabled=!row.ParentImage.ImageFilterEnabled;else row.ParentImage.ImageAdjustmentsEnabled=!row.ParentImage.ImageAdjustmentsEnabled;View.InvalidateMarkupCache();RefreshImage();}
    void RemoveImageEffect(ImageEffectRow row){if(!Marks.Contains(row.ParentImage))return;CloseImageEffects();Push();if(row.Zone!=null)row.ParentImage.ImageZones.Remove(row.Zone);else if(row.Effect=="Filter")row.ParentImage.ImageFilter=0;else {row.ParentImage.Exposure=row.ParentImage.Contrast=row.ParentImage.Brightness=row.ParentImage.Saturation=row.ParentImage.Temperature=0;row.ParentImage.ImageAdjustmentsEnabled=true;}selectedEffectRow=null;View.InvalidateMarkupCache();RefreshImage();}
    void OpenImageEffectRow(ImageEffectRow row){if(!Marks.Contains(row.ParentImage))return;Active=row.ParentImage;Selected.Clear();Selected.Add(Active);if(row.Effect=="Filter"){OpenFilterControls(row.ParentImage,row.ParentImage.ImageFilter,false);return;}OpenImageAdjustmentCard(row.Zone!=null,row.ParentImage,row.Zone);SyncImageAdjustmentFields();View.Invalidate();}
    bool HandleImageEffectKey(Keys key){if(!layers.ContainsFocus||selectedEffectRow==null)return false;if(key==Keys.Space){ToggleImageEffect(selectedEffectRow);return true;}if(key==Keys.Delete){RemoveImageEffect(selectedEffectRow);return true;}if(key==Keys.Enter){OpenImageEffectRow(selectedEffectRow);return true;}return false;}
}
}
