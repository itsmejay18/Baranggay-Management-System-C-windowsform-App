using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace baranggaysystem1.helper;

internal enum EllieAvatarMood
{
    Idle,
    Thinking
}

internal static class EllieAvatarRenderer
{
    public static Bitmap Render(Size targetSize, EllieAvatarMood mood, int frame)
    {
        int width = Math.Max(220, targetSize.Width);
        int height = Math.Max(360, targetSize.Height);
        var bmp = new Bitmap(width, height);

        float t = frame * 0.11f;
        float sway = (float)Math.Sin(t * 0.9f) * (mood == EllieAvatarMood.Thinking ? 1.6f : 3f);
        float bob = (float)Math.Sin(t * 1.6f) * (mood == EllieAvatarMood.Thinking ? 1.1f : 2.6f);

        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.CompositingQuality = CompositingQuality.HighQuality;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        DrawBackground(g, width, height);

        float cx = width * 0.5f + sway;
        float cy = height * 0.56f + bob;
        float bodyW = width * 0.25f;
        float bodyH = height * 0.32f;
        float headW = width * 0.48f;
        float headH = height * 0.23f;
        float headX = cx - headW / 2f;
        float headY = cy - bodyH * 0.55f - headH * 0.58f;

        DrawGroundGlow(g, width, height, cx, cy);
        DrawArms(g, cx, cy, bodyW, bodyH, mood, frame);
        DrawBody(g, cx, cy, bodyW, bodyH);
        DrawNeck(g, cx, cy, bodyW, bodyH);
        DrawHead(g, headX, headY, headW, headH);
        DrawScreen(g, headX, headY, headW, headH, mood, frame);
        DrawFace(g, headX, headY, headW, headH, mood, frame);
        DrawNeckLight(g, cx, cy, bodyW, bodyH, mood, frame);
        DrawFeet(g, cx, cy, bodyW, bodyH);

        DrawSignalAura(g, headX, headY, headW, headH, mood, frame);

        return bmp;
    }

    private static void DrawBackground(Graphics g, int width, int height)
    {
        using var bg = new LinearGradientBrush(
            new Rectangle(0, 0, width, height),
            Color.FromArgb(242, 247, 255),
            Color.FromArgb(233, 241, 252),
            90f);
        g.FillRectangle(bg, 0, 0, width, height);

        using var haze = new SolidBrush(Color.FromArgb(70, 177, 208, 245));
        g.FillEllipse(haze, -width * 0.1f, height * 0.62f, width * 1.2f, height * 0.5f);
    }

    private static void DrawGroundGlow(Graphics g, int width, int height, float cx, float cy)
    {
        float glowW = width * 0.72f;
        float glowH = height * 0.22f;
        using var glow = new GraphicsPath();
        glow.AddEllipse(cx - glowW / 2f, cy + height * 0.18f, glowW, glowH);
        using var brush = new PathGradientBrush(glow)
        {
            CenterColor = Color.FromArgb(95, 123, 154, 214),
            SurroundColors = new[] { Color.FromArgb(0, 123, 154, 214) }
        };
        g.FillPath(brush, glow);
    }

    private static void DrawArms(Graphics g, float cx, float cy, float bodyW, float bodyH, EllieAvatarMood mood, int frame)
    {
        float jointR = bodyW * 0.16f;
        float forearmW = bodyW * 0.36f;
        float forearmH = bodyH * 0.20f;
        float armY = cy - bodyH * 0.06f;
        float wave = mood == EllieAvatarMood.Thinking ? (float)Math.Sin(frame * 0.22f) * 5f : 0f;

        DrawArm(g, cx - bodyW * 0.78f, armY + wave, jointR, forearmW, forearmH, true);
        DrawArm(g, cx + bodyW * 0.62f, armY - wave, jointR, forearmW, forearmH, false);
    }

    private static void DrawArm(Graphics g, float jointCenterX, float jointCenterY, float jointR, float forearmW, float forearmH, bool isLeft)
    {
        float tilt = isLeft ? -8f : 8f;
        using var jointBrush = new LinearGradientBrush(
            new RectangleF(jointCenterX - jointR, jointCenterY - jointR, jointR * 2, jointR * 2),
            Color.FromArgb(240, 246, 255),
            Color.FromArgb(206, 222, 245),
            90f);
        using var jointPen = new Pen(Color.FromArgb(160, 188, 224), 1.6f);
        g.FillEllipse(jointBrush, jointCenterX - jointR, jointCenterY - jointR, jointR * 2, jointR * 2);
        g.DrawEllipse(jointPen, jointCenterX - jointR, jointCenterY - jointR, jointR * 2, jointR * 2);

        float forearmX = isLeft ? jointCenterX - forearmW * 0.62f : jointCenterX + jointR * 0.2f;
        float forearmY = jointCenterY - forearmH * 0.5f;
        using var path = RoundedRect(forearmX, forearmY, forearmW, forearmH, forearmH * 0.45f);
        using var fill = new LinearGradientBrush(new RectangleF(forearmX, forearmY, forearmW, forearmH),
            Color.FromArgb(242, 248, 255),
            Color.FromArgb(214, 226, 243),
            isLeft ? 20f : 160f);
        using var border = new Pen(Color.FromArgb(170, 190, 223), 1.6f);
        g.TranslateTransform(jointCenterX, jointCenterY);
        g.RotateTransform(tilt);
        g.TranslateTransform(-jointCenterX, -jointCenterY);
        g.FillPath(fill, path);
        g.DrawPath(border, path);
        g.ResetTransform();
    }

    private static void DrawBody(Graphics g, float cx, float cy, float bodyW, float bodyH)
    {
        float x = cx - bodyW / 2f;
        float y = cy - bodyH / 2f;
        DrawCapsule(g, x, y, bodyW, bodyH, 38f, Color.FromArgb(252, 253, 255), Color.FromArgb(214, 226, 243), Color.FromArgb(173, 192, 224));

        using var coreGlow = new GraphicsPath();
        coreGlow.AddEllipse(cx - bodyW * 0.2f, y + bodyH * 0.28f, bodyW * 0.4f, bodyH * 0.42f);
        using var coreBrush = new PathGradientBrush(coreGlow)
        {
            CenterColor = Color.FromArgb(80, 182, 202, 255),
            SurroundColors = new[] { Color.FromArgb(0, 182, 202, 255) }
        };
        g.FillPath(coreBrush, coreGlow);
    }

    private static void DrawNeck(Graphics g, float cx, float cy, float bodyW, float bodyH)
    {
        float neckW = bodyW * 0.32f;
        float neckH = bodyH * 0.10f;
        float x = cx - neckW / 2f;
        float y = cy - bodyH * 0.55f;
        DrawCapsule(g, x, y, neckW, neckH, 10f, Color.FromArgb(235, 242, 253), Color.FromArgb(207, 223, 246), Color.FromArgb(165, 184, 216));
    }

    private static void DrawHead(Graphics g, float x, float y, float w, float h)
    {
        DrawCapsule(g, x, y, w, h, 42f, Color.FromArgb(254, 255, 255), Color.FromArgb(219, 230, 247), Color.FromArgb(176, 194, 224));
    }

    private static void DrawScreen(Graphics g, float x, float y, float w, float h, EllieAvatarMood mood, int frame)
    {
        float padX = w * 0.1f;
        float padTop = h * 0.2f;
        float padBottom = h * 0.24f;
        float sx = x + padX;
        float sy = y + padTop;
        float sw = w - padX * 2f;
        float sh = h - padTop - padBottom;

        using var path = RoundedRect(sx, sy, sw, sh, 24f);
        using var screenBrush = new LinearGradientBrush(
            new RectangleF(sx, sy, sw, sh),
            Color.FromArgb(14, 28, 62),
            Color.FromArgb(6, 14, 32),
            90f);
        g.FillPath(screenBrush, path);
        using var border = new Pen(Color.FromArgb(92, 122, 186), 2.2f);
        g.DrawPath(border, path);

        float pulse = (float)(0.65 + 0.35 * Math.Sin(frame * 0.18f));
        int glowAlpha = mood == EllieAvatarMood.Thinking ? (int)(150 + pulse * 90) : 120;
        using var glow = new SolidBrush(Color.FromArgb(glowAlpha, 118, 196, 255));
        g.FillEllipse(glow, sx + sw * 0.08f, sy + sh * 0.09f, sw * 0.2f, sh * 0.2f);
    }

    private static void DrawFace(Graphics g, float x, float y, float w, float h, EllieAvatarMood mood, int frame)
    {
        float padX = w * 0.1f;
        float padTop = h * 0.18f;
        float padBottom = h * 0.22f;
        float sx = x + padX;
        float sy = y + padTop;
        float sw = w - padX * 2f;
        float sh = h - padTop - padBottom;

        using var eyeBrush = new SolidBrush(Color.FromArgb(160, 229, 255));
        using var eyeGlow = new Pen(Color.FromArgb(100, 160, 229, 255), 3f);
        using var mouthPen = new Pen(Color.FromArgb(130, 180, 235), 2f);

        float eyeY = sy + sh * 0.50f;
        float ex1 = sx + sw * 0.34f;
        float ex2 = sx + sw * 0.66f;

        bool blink = mood == EllieAvatarMood.Idle && (frame % 120 > 102 && frame % 120 < 108);
        if (blink)
        {
            g.DrawLine(eyeGlow, ex1 - 9, eyeY, ex1 + 9, eyeY);
            g.DrawLine(eyeGlow, ex2 - 9, eyeY, ex2 + 9, eyeY);
        }
        else
        {
            g.FillEllipse(eyeBrush, ex1 - 9, eyeY - 7, 18, 13);
            g.FillEllipse(eyeBrush, ex2 - 9, eyeY - 7, 18, 13);
        }

        if (mood == EllieAvatarMood.Thinking)
        {
            float dotY = sy + sh * 0.72f;
            float dotSpacing = 12f;
            float offset = (frame % 24) / 24f * 3f;
            for (int i = 0; i < 3; i++)
            {
                float alphaPhase = 0.35f + (float)Math.Abs(Math.Sin((frame + i * 6) * 0.18f)) * 0.65f;
                using var dotBrush = new SolidBrush(Color.FromArgb((int)(90 + alphaPhase * 165), 150, 220, 255));
                g.FillEllipse(dotBrush, sx + sw * 0.43f + (i - 1 + offset * 0.1f) * dotSpacing, dotY, 6, 6);
            }
        }
        else
        {
            g.DrawArc(mouthPen, sx + sw * 0.42f, sy + sh * 0.65f, sw * 0.16f, sh * 0.18f, 200, 140);
        }
    }

    private static void DrawNeckLight(Graphics g, float cx, float cy, float bodyW, float bodyH, EllieAvatarMood mood, int frame)
    {
        float pulse = mood == EllieAvatarMood.Thinking
            ? (float)(0.55 + 0.45 * Math.Abs(Math.Sin(frame * 0.24f)))
            : 0.45f;
        using var pen = new Pen(Color.FromArgb((int)(90 + pulse * 150), 127, 195, 255), 3f);
        g.DrawArc(pen, cx - bodyW * 0.18f, cy - bodyH * 0.56f, bodyW * 0.36f, bodyH * 0.20f, 200, 140);
    }

    private static void DrawFeet(Graphics g, float cx, float cy, float bodyW, float bodyH)
    {
        using var footBrush = new SolidBrush(Color.FromArgb(94, 108, 140));
        float footW = bodyW * 0.32f;
        float footH = bodyH * 0.12f;
        g.FillEllipse(footBrush, cx - bodyW * 0.20f, cy + bodyH * 0.40f, footW, footH);
        g.FillEllipse(footBrush, cx - bodyW * 0.06f, cy + bodyH * 0.40f, footW, footH);
    }

    private static void DrawSignalAura(Graphics g, float headX, float headY, float headW, float headH, EllieAvatarMood mood, int frame)
    {
        if (mood != EllieAvatarMood.Thinking)
        {
            return;
        }

        float signal = (float)(0.35 + 0.65 * Math.Abs(Math.Sin(frame * 0.14f)));
        int alpha = (int)(80 + signal * 140);
        using var pen = new Pen(Color.FromArgb(alpha, 123, 197, 255), 2f);
        g.DrawArc(pen, headX - headW * 0.18f, headY - headH * 0.18f, headW * 1.36f, headH * 1.08f, 205, 130);
        g.DrawArc(pen, headX - headW * 0.26f, headY - headH * 0.26f, headW * 1.52f, headH * 1.22f, 210, 120);
    }

    private static void DrawCapsule(
        Graphics g,
        float x,
        float y,
        float w,
        float h,
        float radius,
        Color topColor,
        Color bottomColor,
        Color borderColor)
    {
        using var path = RoundedRect(x, y, w, h, radius);
        using var fill = new LinearGradientBrush(new RectangleF(x, y, w, h), topColor, bottomColor, 90f);
        g.FillPath(fill, path);

        using var glossy = new GraphicsPath();
        glossy.AddEllipse(x + w * 0.1f, y + h * 0.05f, w * 0.8f, h * 0.28f);
        using var glossyBrush = new PathGradientBrush(glossy)
        {
            CenterColor = Color.FromArgb(85, 255, 255, 255),
            SurroundColors = new[] { Color.FromArgb(0, 255, 255, 255) }
        };
        g.FillPath(glossyBrush, glossy);

        using var border = new Pen(borderColor, 1.8f);
        g.DrawPath(border, path);
    }

    private static GraphicsPath RoundedRect(float x, float y, float w, float h, float radius)
    {
        float d = radius * 2f;
        var path = new GraphicsPath();
        path.AddArc(x, y, d, d, 180, 90);
        path.AddArc(x + w - d, y, d, d, 270, 90);
        path.AddArc(x + w - d, y + h - d, d, d, 0, 90);
        path.AddArc(x, y + h - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
