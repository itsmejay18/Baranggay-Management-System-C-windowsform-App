using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace baranggaysystem1.helper;

internal static class LogoImageSanitizer
{
	private const byte AlphaThreshold = 12;

	private const byte NearWhiteThreshold = 242;

	private const int MaxPaddingPixels = 8;

	internal static byte[] NormalizeLogoImage(byte[] sourceBytes)
	{
		if (sourceBytes == null || sourceBytes.Length == 0)
		{
			throw new InvalidDataException("Logo image is empty.");
		}
		BitmapSource bitmapSource = PrepareBitmap(sourceBytes);
		int pixelWidth = bitmapSource.PixelWidth;
		int pixelHeight = bitmapSource.PixelHeight;
		int num = pixelWidth * 4;
		byte[] pixels = new byte[num * pixelHeight];
		bitmapSource.CopyPixels(pixels, num, 0);
		RemoveEdgeBackground(pixels, pixelWidth, pixelHeight, num);
		if (!TryFindContentBounds(pixels, pixelWidth, pixelHeight, num, out var left, out var top, out var right, out var bottom))
		{
			return EncodePng(bitmapSource);
		}
		int num2 = ResolvePadding(pixelWidth, pixelHeight);
		left = Math.Max(0, left - num2);
		top = Math.Max(0, top - num2);
		right = Math.Min(pixelWidth - 1, right + num2);
		bottom = Math.Min(pixelHeight - 1, bottom + num2);
		return EncodePng(CropBitmap(bitmapSource, pixels, num, left, top, right, bottom));
	}

	private static BitmapSource PrepareBitmap(byte[] sourceBytes)
	{
		using MemoryStream bitmapStream = new MemoryStream(sourceBytes);
		BitmapSource bitmapSource = BitmapDecoder.Create(bitmapStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad).Frames[0];
		if (bitmapSource.Format == PixelFormats.Bgra32)
		{
			((Freezable)bitmapSource).Freeze();
			return bitmapSource;
		}
		FormatConvertedBitmap formatConvertedBitmap = new FormatConvertedBitmap();
		formatConvertedBitmap.BeginInit();
		formatConvertedBitmap.Source = bitmapSource;
		formatConvertedBitmap.DestinationFormat = PixelFormats.Bgra32;
		formatConvertedBitmap.EndInit();
		((Freezable)formatConvertedBitmap).Freeze();
		return formatConvertedBitmap;
	}

	private static void RemoveEdgeBackground(byte[] pixels, int width, int height, int stride)
	{
		Queue<(int X, int Y)> queue = new Queue<(int, int)>();
		bool[] visited = new bool[width * height];
		for (int i = 0; i < width; i++)
		{
			EnqueueIfBackground(i, 0);
			EnqueueIfBackground(i, height - 1);
		}
		for (int j = 1; j < height - 1; j++)
		{
			EnqueueIfBackground(0, j);
			EnqueueIfBackground(width - 1, j);
		}
		while (queue.Count > 0)
		{
			var (num, num2) = queue.Dequeue();
			ClearPixel(num, num2);
			EnqueueIfBackground(num - 1, num2);
			EnqueueIfBackground(num + 1, num2);
			EnqueueIfBackground(num, num2 - 1);
			EnqueueIfBackground(num, num2 + 1);
		}
		void ClearPixel(int x, int y)
		{
			int num3 = y * stride + x * 4;
			pixels[num3] = 0;
			pixels[num3 + 1] = 0;
			pixels[num3 + 2] = 0;
			pixels[num3 + 3] = 0;
		}
		void EnqueueIfBackground(int x, int y)
		{
			if (x >= 0 && x < width && y >= 0 && y < height)
			{
				int num3 = y * width + x;
				if (!visited[num3] && IsBackgroundPixel(x, y))
				{
					visited[num3] = true;
					queue.Enqueue((x, y));
				}
			}
		}
		bool IsBackgroundPixel(int x, int y)
		{
			int num3 = y * stride + x * 4;
			byte val = pixels[num3];
			byte val2 = pixels[num3 + 1];
			byte val3 = pixels[num3 + 2];
			if (pixels[num3 + 3] <= 12)
			{
				return true;
			}
			byte b = Math.Max(val3, Math.Max(val2, val));
			byte b2 = Math.Min(val3, Math.Min(val2, val));
			if (b2 >= 242)
			{
				return b - b2 <= 14;
			}
			return false;
		}
	}

	private static bool TryFindContentBounds(byte[] pixels, int width, int height, int stride, out int left, out int top, out int right, out int bottom)
	{
		left = width;
		top = height;
		right = -1;
		bottom = -1;
		for (int i = 0; i < height; i++)
		{
			for (int j = 0; j < width; j++)
			{
				int num = i * stride + j * 4;
				if (pixels[num + 3] > 12)
				{
					left = Math.Min(left, j);
					top = Math.Min(top, i);
					right = Math.Max(right, j);
					bottom = Math.Max(bottom, i);
				}
			}
		}
		if (right >= left)
		{
			return bottom >= top;
		}
		return false;
	}

	private static int ResolvePadding(int width, int height)
	{
		return Math.Clamp((int)Math.Round((double)Math.Max(width, height) * 0.035), 2, 8);
	}

	private static BitmapSource CropBitmap(BitmapSource preparedBitmap, byte[] pixels, int stride, int left, int top, int right, int bottom)
	{
		int num = right - left + 1;
		int num2 = bottom - top + 1;
		int num3 = num * 4;
		byte[] array = new byte[num3 * num2];
		for (int i = 0; i < num2; i++)
		{
			int srcOffset = (top + i) * stride + left * 4;
			int dstOffset = i * num3;
			Buffer.BlockCopy(pixels, srcOffset, array, dstOffset, num3);
		}
		BitmapSource bitmapSource = BitmapSource.Create(num, num2, preparedBitmap.DpiX, preparedBitmap.DpiY, PixelFormats.Bgra32, null, array, num3);
		((Freezable)bitmapSource).Freeze();
		return bitmapSource;
	}

	private static byte[] EncodePng(BitmapSource bitmap)
	{
		PngBitmapEncoder pngBitmapEncoder = new PngBitmapEncoder();
		pngBitmapEncoder.Frames.Add(BitmapFrame.Create(bitmap));
		using MemoryStream memoryStream = new MemoryStream();
		pngBitmapEncoder.Save(memoryStream);
		return memoryStream.ToArray();
	}
}
