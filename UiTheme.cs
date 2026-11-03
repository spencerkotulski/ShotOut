using System.Drawing;
namespace ShotOut {
// Shared interface colors, independent of document pixels and drawing defaults.
static class UiTheme {
    internal static readonly Color Accent=Color.FromArgb(26,130,248);
    internal static readonly Color AccentInk=Color.FromArgb(7,24,43);
    internal static readonly Color AccentLight=Color.FromArgb(99,176,255);
    internal static readonly Color SelectionSurface=Color.FromArgb(31,64,101);
    internal static readonly Color HoverSurface=Color.FromArgb(38,67,100);
}
}
