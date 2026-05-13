using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace baranggaysystem1.helper;

internal static class EllieAvatarRenderer
{
	public static Bitmap Render(Size targetSize, EllieAvatarMood mood, int frame)
	{
		int num = Math.Max(220, targetSize.Width);
		int num2 = Math.Max(360, targetSize.Height);
		Bitmap bitmap = new Bitmap(num, num2);
		float num3 = (float)frame * 0.11f;
		float num4 = (float)Math.Sin(num3 * 0.9f) * ((mood == EllieAvatarMood.Thinking) ? 1.6f : 3f);
		float num5 = (float)Math.Sin(num3 * 1.6f) * ((mood == EllieAvatarMood.Thinking) ? 1.1f : 2.6f);
		using Graphics graphics = Graphics.FromImage(bitmap);
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		graphics.CompositingQuality = CompositingQuality.HighQuality;
		graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
		graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
		DrawBackground(graphics, num, num2);
		float num6 = (float)num * 0.5f + num4;
		float num7 = (float)num2 * 0.56f + num5;
		float bodyW = (float)num * 0.25f;
		float num8 = (float)num2 * 0.32f;
		float num9 = (float)num * 0.48f;
		float num10 = (float)num2 * 0.23f;
		float num11 = num6 - num9 / 2f;
		float num12 = num7 - num8 * 0.55f - num10 * 0.58f;
		DrawGroundGlow(graphics, num, num2, num6, num7);
		DrawArms(graphics, num6, num7, bodyW, num8, mood, frame);
		DrawBody(graphics, num6, num7, bodyW, num8);
		DrawNeck(graphics, num6, num7, bodyW, num8);
		DrawHead(graphics, num11, num12, num9, num10);
		DrawScreen(graphics, num11, num12, num9, num10, mood, frame);
		DrawFace(graphics, num11, num12, num9, num10, mood, frame);
		DrawNeckLight(graphics, num6, num7, bodyW, num8, mood, frame);
		DrawFeet(graphics, num6, num7, bodyW, num8);
		DrawSignalAura(graphics, num11, num12, num9, num10, mood, frame);
		return bitmap;
	}

	private static void DrawBackground(Graphics g, int width, int height)
	{
		using LinearGradientBrush brush = new LinearGradientBrush(new Rectangle(0, 0, width, height), Color.FromArgb(242, 247, 255), Color.FromArgb(233, 241, 252), 90f);
		g.FillRectangle(brush, 0, 0, width, height);
		using SolidBrush brush2 = new SolidBrush(Color.FromArgb(70, 177, 208, 245));
		g.FillEllipse(brush2, (float)(-width) * 0.1f, (float)height * 0.62f, (float)width * 1.2f, (float)height * 0.5f);
	}

	private static void DrawGroundGlow(Graphics g, int width, int height, float cx, float cy)
	{
		float num = (float)width * 0.72f;
		float height2 = (float)height * 0.22f;
		using GraphicsPath graphicsPath = new GraphicsPath();
		graphicsPath.AddEllipse(cx - num / 2f, cy + (float)height * 0.18f, num, height2);
		PathGradientBrush pathGradientBrush = new PathGradientBrush(graphicsPath);
		pathGradientBrush.CenterColor = Color.FromArgb(95, 123, 154, 214);
		pathGradientBrush.SurroundColors = new Color[1] { Color.FromArgb(0, 123, 154, 214) };
		using PathGradientBrush brush = pathGradientBrush;
		g.FillPath(brush, graphicsPath);
	}

	private static void DrawArms(Graphics g, float cx, float cy, float bodyW, float bodyH, EllieAvatarMood mood, int frame)
	{
		float jointR = bodyW * 0.16f;
		float forearmW = bodyW * 0.36f;
		float forearmH = bodyH * 0.2f;
		float num = cy - bodyH * 0.06f;
		float num2 = ((mood == EllieAvatarMood.Thinking) ? ((float)Math.Sin((float)frame * 0.22f) * 5f) : 0f);
		DrawArm(g, cx - bodyW * 0.78f, num + num2, jointR, forearmW, forearmH, isLeft: true);
		DrawArm(g, cx + bodyW * 0.62f, num - num2, jointR, forearmW, forearmH, isLeft: false);
	}

	private static void DrawArm(Graphics g, float jointCenterX, float jointCenterY, float jointR, float forearmW, float forearmH, bool isLeft)
	{
		float angle = (isLeft ? (-8f) : 8f);
		using LinearGradientBrush brush = new LinearGradientBrush(new RectangleF(jointCenterX - jointR, jointCenterY - jointR, jointR * 2f, jointR * 2f), Color.FromArgb(240, 246, 255), Color.FromArgb(206, 222, 245), 90f);
		using Pen pen = new Pen(Color.FromArgb(160, 188, 224), 1.6f);
		g.FillEllipse(brush, jointCenterX - jointR, jointCenterY - jointR, jointR * 2f, jointR * 2f);
		g.DrawEllipse(pen, jointCenterX - jointR, jointCenterY - jointR, jointR * 2f, jointR * 2f);
		float x = (isLeft ? (jointCenterX - forearmW * 0.62f) : (jointCenterX + jointR * 0.2f));
		float y = jointCenterY - forearmH * 0.5f;
		using GraphicsPath path = RoundedRect(x, y, forearmW, forearmH, forearmH * 0.45f);
		using LinearGradientBrush brush2 = new LinearGradientBrush(new RectangleF(x, y, forearmW, forearmH), Color.FromArgb(242, 248, 255), Color.FromArgb(214, 226, 243), isLeft ? 20f : 160f);
		using Pen pen2 = new Pen(Color.FromArgb(170, 190, 223), 1.6f);
		g.TranslateTransform(jointCenterX, jointCenterY);
		g.RotateTransform(angle);
		g.TranslateTransform(0f - jointCenterX, 0f - jointCenterY);
		g.FillPath(brush2, path);
		g.DrawPath(pen2, path);
		g.ResetTransform();
	}

	private static void DrawBody(Graphics g, float cx, float cy, float bodyW, float bodyH)
	{
		float x = cx - bodyW / 2f;
		float num = cy - bodyH / 2f;
		DrawCapsule(g, x, num, bodyW, bodyH, 38f, Color.FromArgb(252, 253, 255), Color.FromArgb(214, 226, 243), Color.FromArgb(173, 192, 224));
		using GraphicsPath graphicsPath = new GraphicsPath();
		graphicsPath.AddEllipse(cx - bodyW * 0.2f, num + bodyH * 0.28f, bodyW * 0.4f, bodyH * 0.42f);
		PathGradientBrush pathGradientBrush = new PathGradientBrush(graphicsPath);
		pathGradientBrush.CenterColor = Color.FromArgb(80, 182, 202, 255);
		pathGradientBrush.SurroundColors = new Color[1] { Color.FromArgb(0, 182, 202, 255) };
		using PathGradientBrush brush = pathGradientBrush;
		g.FillPath(brush, graphicsPath);
	}

	private static void DrawNeck(Graphics g, float cx, float cy, float bodyW, float bodyH)
	{
		float num = bodyW * 0.32f;
		float h = bodyH * 0.1f;
		float x = cx - num / 2f;
		float y = cy - bodyH * 0.55f;
		DrawCapsule(g, x, y, num, h, 10f, Color.FromArgb(235, 242, 253), Color.FromArgb(207, 223, 246), Color.FromArgb(165, 184, 216));
	}

	private static void DrawHead(Graphics g, float x, float y, float w, float h)
	{
		DrawCapsule(g, x, y, w, h, 42f, Color.FromArgb(254, 255, 255), Color.FromArgb(219, 230, 247), Color.FromArgb(176, 194, 224));
	}

	private static void DrawScreen(Graphics g, float x, float y, float w, float h, EllieAvatarMood mood, int frame)
	{
		float num = w * 0.1f;
		float num2 = h * 0.2f;
		float num3 = h * 0.24f;
		float num4 = x + num;
		float num5 = y + num2;
		float num6 = w - num * 2f;
		float num7 = h - num2 - num3;
		using GraphicsPath path = RoundedRect(num4, num5, num6, num7, 24f);
		using LinearGradientBrush brush = new LinearGradientBrush(new RectangleF(num4, num5, num6, num7), Color.FromArgb(14, 28, 62), Color.FromArgb(6, 14, 32), 90f);
		g.FillPath(brush, path);
		using Pen pen = new Pen(Color.FromArgb(92, 122, 186), 2.2f);
		g.DrawPath(pen, path);
		float num8 = (float)(0.65 + 0.35 * Math.Sin((float)frame * 0.18f));
		using SolidBrush brush2 = new SolidBrush(Color.FromArgb((mood == EllieAvatarMood.Thinking) ? ((int)(150f + num8 * 90f)) : 120, 118, 196, 255));
		g.FillEllipse(brush2, num4 + num6 * 0.08f, num5 + num7 * 0.09f, num6 * 0.2f, num7 * 0.2f);
	}

	private static void DrawFace(Graphics g, float x, float y, float w, float h, EllieAvatarMood mood, int frame)
	{
		float num = w * 0.1f;
		float num2 = h * 0.18f;
		float num3 = h * 0.22f;
		float num4 = x + num;
		float num5 = y + num2;
		float num6 = w - num * 2f;
		float num7 = h - num2 - num3;
		using SolidBrush brush = new SolidBrush(Color.FromArgb(160, 229, 255));
		using Pen pen = new Pen(Color.FromArgb(100, 160, 229, 255), 3f);
		using Pen pen2 = new Pen(Color.FromArgb(130, 180, 235), 2f);
		float num8 = num5 + num7 * 0.5f;
		float num9 = num4 + num6 * 0.34f;
		float num10 = num4 + num6 * 0.66f;
		if (mood == EllieAvatarMood.Idle && frame % 120 > 102 && frame % 120 < 108)
		{
			g.DrawLine(pen, num9 - 9f, num8, num9 + 9f, num8);
			g.DrawLine(pen, num10 - 9f, num8, num10 + 9f, num8);
		}
		else
		{
			g.FillEllipse(brush, num9 - 9f, num8 - 7f, 18f, 13f);
			g.FillEllipse(brush, num10 - 9f, num8 - 7f, 18f, 13f);
		}
		if (mood == EllieAvatarMood.Thinking)
		{
			float y2 = num5 + num7 * 0.72f;
			float num11 = 12f;
			float num12 = (float)(frame % 24) / 24f * 3f;
			for (int i = 0; i < 3; i++)
			{
				float num13 = 0.35f + (float)Math.Abs(Math.Sin((float)(frame + i * 6) * 0.18f)) * 0.65f;
				using SolidBrush brush2 = new SolidBrush(Color.FromArgb((int)(90f + num13 * 165f), 150, 220, 255));
				g.FillEllipse(brush2, num4 + num6 * 0.43f + ((float)(i - 1) + num12 * 0.1f) * num11, y2, 6f, 6f);
			}
		}
		else
		{
			g.DrawArc(pen2, num4 + num6 * 0.42f, num5 + num7 * 0.65f, num6 * 0.16f, num7 * 0.18f, 200f, 140f);
		}
	}

	private static void DrawNeckLight(Graphics g, float cx, float cy, float bodyW, float bodyH, EllieAvatarMood mood, int frame)
	{
		float num = ((mood == EllieAvatarMood.Thinking) ? ((float)(0.55 + 0.45 * Math.Abs(Math.Sin((float)frame * 0.24f)))) : 0.45f);
		using Pen pen = new Pen(Color.FromArgb((int)(90f + num * 150f), 127, 195, 255), 3f);
		g.DrawArc(pen, cx - bodyW * 0.18f, cy - bodyH * 0.56f, bodyW * 0.36f, bodyH * 0.2f, 200f, 140f);
	}

	private static void DrawFeet(Graphics g, float cx, float cy, float bodyW, float bodyH)
	{
		using SolidBrush brush = new SolidBrush(Color.FromArgb(94, 108, 140));
		float width = bodyW * 0.32f;
		float height = bodyH * 0.12f;
		g.FillEllipse(brush, cx - bodyW * 0.2f, cy + bodyH * 0.4f, width, height);
		g.FillEllipse(brush, cx - bodyW * 0.06f, cy + bodyH * 0.4f, width, height);
	}

	private static void DrawSignalAura(Graphics g, float headX, float headY, float headW, float headH, EllieAvatarMood mood, int frame)
	{
		if (mood != EllieAvatarMood.Thinking)
		{
			return;
		}
		float num = (float)(0.35 + 0.65 * Math.Abs(Math.Sin((float)frame * 0.14f)));
		using Pen pen = new Pen(Color.FromArgb((int)(80f + num * 140f), 123, 197, 255), 2f);
		g.DrawArc(pen, headX - headW * 0.18f, headY - headH * 0.18f, headW * 1.36f, headH * 1.08f, 205f, 130f);
		g.DrawArc(pen, headX - headW * 0.26f, headY - headH * 0.26f, headW * 1.52f, headH * 1.22f, 210f, 120f);
	}

	private static void DrawCapsule(Graphics g, float x, float y, float w, float h, float radius, Color topColor, Color bottomColor, Color borderColor)
	{
		using GraphicsPath path = RoundedRect(x, y, w, h, radius);
		using LinearGradientBrush brush = new LinearGradientBrush(new RectangleF(x, y, w, h), topColor, bottomColor, 90f);
		g.FillPath(brush, path);
		using GraphicsPath graphicsPath = new GraphicsPath();
		graphicsPath.AddEllipse(x + w * 0.1f, y + h * 0.05f, w * 0.8f, h * 0.28f);
		PathGradientBrush pathGradientBrush = new PathGradientBrush(graphicsPath);
		pathGradientBrush.CenterColor = Color.FromArgb(85, 255, 255, 255);
		pathGradientBrush.SurroundColors = new Color[1] { Color.FromArgb(0, 255, 255, 255) };
		using PathGradientBrush brush2 = pathGradientBrush;
		g.FillPath(brush2, graphicsPath);
		using Pen pen = new Pen(borderColor, 1.8f);
		g.DrawPath(pen, path);
	}

	private static GraphicsPath RoundedRect(float x, float y, float w, float h, float radius)
	{
		float num = radius * 2f;
		GraphicsPath graphicsPath = new GraphicsPath();
		graphicsPath.AddArc(x, y, num, num, 180f, 90f);
		graphicsPath.AddArc(x + w - num, y, num, num, 270f, 90f);
		graphicsPath.AddArc(x + w - num, y + h - num, num, num, 0f, 90f);
		graphicsPath.AddArc(x, y + h - num, num, num, 90f, 90f);
		graphicsPath.CloseFigure();
		return graphicsPath;
	}
}
