namespace Seanzo.LevelDesign.Editor
{
    public static class PaintSession
    {
        public static KitPalette Palette;
        public static int EntryIndex = -1;
        public static int RotationStep;
        public static int ActiveLayerId;
        public static PaintToolMode ToolMode = PaintToolMode.Brush;

        public static KitPaletteEntry ActiveEntry =>
            Palette != null && EntryIndex >= 0 && EntryIndex < Palette.entries.Count
                ? Palette.entries[EntryIndex]
                : null;

        public static void SelectEntry(int index)
        {
            EntryIndex = index;
            var entry = ActiveEntry;
            if (entry != null)
            {
                RotationStep = ((entry.defaultRotationStep % 4) + 4) % 4;
            }
        }

        public static void Rotate(int steps)
        {
            RotationStep = (((RotationStep + steps) % 4) + 4) % 4;
        }
    }
}
