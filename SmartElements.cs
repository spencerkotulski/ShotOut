using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Windows.Forms;
namespace ShotOut {
// One outstanding provider call across the process: a stalled app cannot accumulate workers.
static class ElementRegions {
    static int busy;
    internal static Func<IntPtr,Point,Rectangle,Rectangle[]> Probe=Read;
    internal static Task<Rectangle[]> Start(IntPtr window,Point point,Rectangle bounds) {
        if(Interlocked.CompareExchange(ref busy,1,0)!=0)return null;
        return Task.Factory.StartNew(delegate {try{return Probe(window,point,bounds);}catch{return new Rectangle[0];}finally{Interlocked.Exchange(ref busy,0);}},CancellationToken.None,TaskCreationOptions.None,TaskScheduler.Default);
    }
    static Rectangle[] Read(IntPtr window,Point point,Rectangle bounds) {
        var clock=Stopwatch.StartNew();int remaining=160;var found=new List<Rectangle>();
        var root=AutomationElement.FromHandle(window);var walker=TreeWalker.ControlViewWalker;clock.Restart();
        Action<AutomationElement,int> visit=null;
        visit=delegate(AutomationElement element,int depth) {
            if(element==null||depth>18||remaining--<=0||clock.ElapsedMilliseconds>100)return;
            var current=element.Current;var raw=current.BoundingRectangle;
            if(raw.IsEmpty||Double.IsInfinity(raw.Width)||Double.IsInfinity(raw.Height))return;
            var rect=Rectangle.Intersect(bounds,Rectangle.FromLTRB((int)Math.Floor(raw.Left),(int)Math.Floor(raw.Top),(int)Math.Ceiling(raw.Right),(int)Math.Ceiling(raw.Bottom)));
            if(!rect.Contains(point)||current.IsOffscreen)return;
            if(current.ControlType!=ControlType.Text&&current.ControlType!=ControlType.ToolTip&&rect.Width>=24&&rect.Height>=16&&rect!=bounds)found.Add(rect);
            var child=walker.GetFirstChild(element);
            while(child!=null&&remaining>0&&clock.ElapsedMilliseconds<=100){visit(child,depth+1);child=walker.GetNextSibling(child);}
        };
        visit(root,0);return Normalize(found,point,bounds);
    }
    internal static Rectangle[] Normalize(IEnumerable<Rectangle> regions,Point point,Rectangle bounds) {
        return regions.Select(r=>Rectangle.Intersect(r,bounds)).Where(r=>r.Width>=24&&r.Height>=16&&r.Contains(point)&&r!=bounds).Distinct().OrderBy(r=>(long)r.Width*r.Height).Take(20).Concat(new[]{bounds}).ToArray();
    }
}
partial class CaptureOverlay {
    bool elements,elementFallback,elementPending;
    readonly System.Windows.Forms.Timer elementTimer=new System.Windows.Forms.Timer {Interval=40};
    Task<Rectangle[]> elementTask;Rectangle[] elementCandidates;
    IntPtr queryHandle;Point elementPoint,queryPoint;Rectangle elementWindow;IntPtr elementHandle;
    Rectangle heldRegion;bool wheelScope;
    int candidateIndex;long movedAt,queryAt;readonly Stopwatch elementClock=Stopwatch.StartNew();
    void InstallElementSelection() {
        elements=!window&&Preferences.Current.SmartElements;smart|=elements;
        Disposed+=delegate{elementTimer.Dispose();};if(!elements||selectionPicker!=null)return;
        elementTimer.Tick+=delegate {PollElements();};elementTimer.Start();
        KeyDown+=delegate(object sender,KeyEventArgs e){if(e.KeyCode==Keys.ShiftKey||e.KeyCode==Keys.Menu){MoveSelection(PointToClient(Cursor.Position),(ModifierKeys&Keys.Alt)==0);e.Handled=true;}};
        KeyUp+=delegate(object sender,KeyEventArgs e){if(e.KeyCode==Keys.ShiftKey||e.KeyCode==Keys.Menu)MoveSelection(PointToClient(Cursor.Position),(ModifierKeys&Keys.Alt)==0);};
        FormClosed+=delegate{elementTimer.Stop();};
    }
    void TrackElements(Point local,IntPtr handle,Rectangle screenWindow) {
        if(!elements||elementFallback||dragging)return;
        if(elementPoint!=local||elementHandle!=handle||elementWindow!=screenWindow){
            bool retain=elementHandle==handle&&elementWindow==screenWindow&&heldRegion.Contains(new Point(local.X+origin.X,local.Y+origin.Y));
            elementPoint=local;elementHandle=handle;elementWindow=screenWindow;
            if(!retain){elementCandidates=null;heldRegion=Rectangle.Empty;wheelScope=false;candidateIndex=0;}
            elementPending=handle!=IntPtr.Zero&&!wheelScope;movedAt=elementClock.ElapsedMilliseconds;
        }
        ApplyElementCandidate();
    }
    void ApplyElementCandidate() {
        if(elementCandidates==null||(ModifierKeys&(Keys.Alt|Keys.Shift))!=0)return;
        var r=elementCandidates[Math.Min(candidateIndex,elementCandidates.Length-1)];r.Offset(-origin.X,-origin.Y);Selection=Rectangle.Intersect(ClientRectangle,r);
    }
    void PollElements() {
        if(IsDisposed||!Visible||dragging||(ModifierKeys&(Keys.Alt|Keys.Shift))!=0)return;
        long now=elementClock.ElapsedMilliseconds;
        if(elementTask!=null) {
            if(!elementTask.IsCompleted){if(now-queryAt>250){elementFallback=true;elementTask=null;elementPending=false;}return;}
            var result=elementTask.Result;elementTask=null;
            if(queryHandle==elementHandle&&!elementFallback&&queryPoint==elementPoint){AcceptElementRegions(result);ApplyElementCandidate();Invalidate();}
        }
        if(!elementFallback&&elementPending&&elementHandle!=IntPtr.Zero&&now-movedAt>=140) {
            elementPending=false;queryPoint=elementPoint;queryHandle=elementHandle;queryAt=now;
            elementTask=ElementRegions.Start(elementHandle,new Point(elementPoint.X+origin.X,elementPoint.Y+origin.Y),elementWindow);
            if(elementTask==null)elementFallback=true;
        }
    }
    internal void AcceptElementRegions(Rectangle[] result) {
        var point=new Point(elementPoint.X+origin.X,elementPoint.Y+origin.Y);
        bool retain=heldRegion.Contains(point);
        var regions=retain?result.Concat(new[]{heldRegion}):result;
        elementCandidates=ElementRegions.Normalize(regions,point,elementWindow);candidateIndex=0;
        if(retain){
            int smaller=wheelScope?-1:Array.FindIndex(elementCandidates,r=>r!=heldRegion&&heldRegion.Contains(r));
            candidateIndex=smaller>=0?smaller:Array.IndexOf(elementCandidates,heldRegion);
            if(candidateIndex<0)candidateIndex=elementCandidates.Length-1;
        }
        heldRegion=elementCandidates[candidateIndex];
    }
    protected override void OnMouseWheel(MouseEventArgs e) {
        base.OnMouseWheel(e);if(!elements||dragging||elementCandidates==null)return;
        candidateIndex=Math.Max(0,Math.Min(elementCandidates.Length-1,candidateIndex+(e.Delta>0?1:-1)));
        heldRegion=elementCandidates[candidateIndex];wheelScope=true;elementPending=false;ApplyElementCandidate();Invalidate();
    }
}
}
