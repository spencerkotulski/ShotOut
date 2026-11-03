using System;
using System.Drawing;
using System.Threading.Tasks;
namespace ShotOut {
partial class Editor {
    Task<byte[]> snapshotPreparation;
    Bitmap snapshotPreparationSource;
    void WarmSnapshotImageBytes(){
        if(Background==null||IsDisposed||Disposing||Object.ReferenceEquals(snapshotBitmap,Background)||(snapshotPreparation!=null&&!snapshotPreparation.IsCompleted))return;
        Bitmap copy=null;try{copy=(Bitmap)Background.Clone();var isolated=copy;snapshotPreparationSource=Background;snapshotPreparation=Task.Factory.StartNew(delegate{using(isolated){try{return Art.Bytes(isolated);}catch(Exception){return null;}}});}catch(Exception){if(copy!=null)copy.Dispose();}
    }
    byte[] PreparedSnapshotBytes(){if(snapshotPreparation!=null&&Object.ReferenceEquals(snapshotPreparationSource,Background)&&snapshotPreparation.Status==TaskStatus.RanToCompletion)return snapshotPreparation.Result;return null;}
}
}
