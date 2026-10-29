using System;
using UnityEngine;

namespace SpiritPrototype.Editor
{
    public static class PlayerSpriteBaker
    {
        public const int SourceSize = 1254, Canvas = 128, BodyPixels = 112, FeetY = 8;
        // Exact gutters of the supplied generated atlas, measured in top-left coordinates.
        private static readonly int[] Left = {64, 368, 648, 944}, Right = {315, 608, 896, 1208};
        private static readonly int[] Top = {64, 368, 656, 932}, Bottom = {355, 653, 928, 1208};
        public struct Frame
        {
            public RectInt bounds;
            public float anchorX;
        }
        public static Frame[] Analyze(Color32[] pixels, int width, int height, out float scale)
        {
            if (width != SourceSize || height != SourceSize || pixels.Length != width * height)
                throw new ArgumentException("Se necesita PlayerWalkSheet_v01.png de 1254 x 1254.");
            var frames = new Frame[16]; int maxHeight = 0, maxWidth = 0;
            for (int row = 0; row < 4; row++)
            for (int col = 0; col < 4; col++)
            {
                var cell = new RectInt(Left[col], height - Bottom[row], Right[col] - Left[col], Bottom[row] - Top[row]);
                var box = Bounds(pixels, width, cell);
                // Use the hood rather than the swinging cloak to anchor the horizontal center.
                var head = new RectInt(box.x, box.y + (int)(box.height * .6f), box.width, box.height - (int)(box.height * .6f));
                var hood = Bounds(pixels, width, head, false);
                frames[row * 4 + col] = new Frame {bounds = box, anchorX = hood.x + hood.width * .5f};
                maxHeight = Math.Max(maxHeight, box.height); maxWidth = Math.Max(maxWidth, box.width);
            }
            scale = Math.Min((float)BodyPixels / maxHeight, (float)BodyPixels / maxWidth);
            return frames;
        }
        private static RectInt Bounds(Color32[] pixels, int width, RectInt cell, bool padding = true)
        {
            int x0 = cell.xMax, y0 = cell.yMax, x1 = -1, y1 = -1;
            for (int y = cell.y; y < cell.yMax; y++)
            for (int x = cell.x; x < cell.xMax; x++)
                if (pixels[y * width + x].a > 12) { x0 = Math.Min(x0, x); y0 = Math.Min(y0, y); x1 = Math.Max(x1, x); y1 = Math.Max(y1, y); }
            if (x1 < x0) throw new InvalidOperationException("Hay una celda sin personaje en la hoja.");
            int extra = padding ? 2 : 0;
            x0 = Math.Max(cell.x, x0 - extra); y0 = Math.Max(cell.y, y0 - extra);
            x1 = Math.Min(cell.xMax - 1, x1 + extra); y1 = Math.Min(cell.yMax - 1, y1 + extra);
            return new RectInt(x0, y0, x1 - x0 + 1, y1 - y0 + 1);
        }
        public static Color32[] Bake(Color32[] source, int sourceWidth, Frame frame, float scale)
        {
            var output = new Color32[Canvas * Canvas]; var bounds = frame.bounds;
            int width = Math.Max(1, (int)Math.Round(bounds.width * scale)), height = Math.Max(1, (int)Math.Round(bounds.height * scale));
            int left = (int)Math.Round(Canvas * .5f - (frame.anchorX - bounds.x) * scale);
            if (left < 0 || left + width > Canvas || FeetY + height > Canvas) throw new InvalidOperationException("El recorte no cabe en el fotograma normalizado.");
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int sx = bounds.x + Math.Min(bounds.width - 1, (int)((x + .5) * bounds.width / width));
                int sy = bounds.y + Math.Min(bounds.height - 1, (int)((y + .5) * bounds.height / height));
                output[(FeetY + y) * Canvas + left + x] = source[sy * sourceWidth + sx];
            }
            return output;
        }
    }
}
