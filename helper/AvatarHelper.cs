using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace baranggaysystem1.helper
{
    internal static class AvatarHelper
    {
        public static Image CreateDefaultAvatar(Size size)
        {
            int width = Math.Max(96, size.Width);
            int height = Math.Max(96, size.Height);
            var bmp = new Bitmap(width, height);

            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.White);

                using var bgBrush = new SolidBrush(Color.FromArgb(235, 237, 240));
                using var outlinePen = new Pen(Color.FromArgb(210, 214, 219), 2f);

                var rect = new RectangleF(4, 4, width - 8, height - 8);
                g.FillEllipse(bgBrush, rect);
                g.DrawEllipse(outlinePen, rect);

                float headSize = Math.Min(width, height) * 0.34f;
                float headX = (width - headSize) / 2f;
                float headY = height * 0.22f;
                var headRect = new RectangleF(headX, headY, headSize, headSize);
                using var headBrush = new SolidBrush(Color.FromArgb(190, 197, 204));
                g.FillEllipse(headBrush, headRect);

                float bodyWidth = headSize * 1.6f;
                float bodyHeight = headSize * 1.25f;
                float bodyX = (width - bodyWidth) / 2f;
                float bodyY = headY + headSize * 0.85f;
                var bodyRect = new RectangleF(bodyX, bodyY, bodyWidth, bodyHeight);
                using var bodyBrush = new SolidBrush(Color.FromArgb(190, 197, 204));
                g.FillEllipse(bodyBrush, bodyRect);
            }

            return bmp;
        }
    }
}
