using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace baranggaysystem1.helper;

internal static class AvatarHelper
{
	public static Image CreateDefaultAvatar(Size size)
	{
		int num = Math.Max(96, size.Width);
		int num2 = Math.Max(96, size.Height);
		Bitmap bitmap = new Bitmap(num, num2);
		using Graphics graphics = Graphics.FromImage(bitmap);
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		graphics.Clear(Color.White);
		using SolidBrush brush = new SolidBrush(Color.FromArgb(235, 237, 240));
		using Pen pen = new Pen(Color.FromArgb(210, 214, 219), 2f);
		RectangleF rect = new RectangleF(4f, 4f, num - 8, num2 - 8);
		graphics.FillEllipse(brush, rect);
		graphics.DrawEllipse(pen, rect);
		float num3 = (float)Math.Min(num, num2) * 0.34f;
		float x = ((float)num - num3) / 2f;
		float num4 = (float)num2 * 0.22f;
		RectangleF rect2 = new RectangleF(x, num4, num3, num3);
		using SolidBrush brush2 = new SolidBrush(Color.FromArgb(190, 197, 204));
		graphics.FillEllipse(brush2, rect2);
		float num5 = num3 * 1.6f;
		float height = num3 * 1.25f;
		float x2 = ((float)num - num5) / 2f;
		float y = num4 + num3 * 0.85f;
		RectangleF rect3 = new RectangleF(x2, y, num5, height);
		using SolidBrush brush3 = new SolidBrush(Color.FromArgb(190, 197, 204));
		graphics.FillEllipse(brush3, rect3);
		return bitmap;
	}
}
