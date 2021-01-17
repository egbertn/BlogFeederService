using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Tga;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ADCCure.BlogFeederService.Helpers
{
	public static class ImageHelper
	{
		/// <summary>
		/// detects image type from binary data
		/// </summary>
		/// <param name="blob">must be supplied</param>
		public async static Task<(int width, int height, string mimeType)> DetectImageType(this byte[] blob)
		{
			if (blob == null)
			{
				throw new ArgumentNullException(nameof(blob));
			}
			var config = new Configuration();

			var formatsDetectionsWanted = new IImageFormatDetector[] { new JpegImageFormatDetector(), new PngImageFormatDetector(), new SixLabors.ImageSharp.Formats.Gif.GifImageFormatDetector(), new SixLabors.ImageSharp.Formats.Tga.TgaImageFormatDetector() };
			var decodersWanted = new (IImageDecoder decoder, IImageFormat format)[] {
				(new JpegDecoder(), JpegFormat.Instance),
				(new PngDecoder(), PngFormat.Instance),
				(new GifDecoder(), GifFormat.Instance),
				(new TgaDecoder(), TgaFormat.Instance) };

			IImageFormat detected = default;
			foreach (var fmt in formatsDetectionsWanted)
			{
				config.ImageFormatsManager.AddImageFormatDetector(fmt);
				if (detected == null)
				{
					detected = fmt.DetectFormat(blob);
				}
			}
			foreach (var (decoder, format) in decodersWanted)
			{
				config.ImageFormatsManager.SetDecoder(format, decoder);
			}
			if (detected != null)
			{
				var image = await Image.LoadAsync(config, new MemoryStream(blob));

				return (image.Width, image.Height, detected.DefaultMimeType);
			}
			return default;
		}

		/// <summary>
		/// packs an image to inline data: format
		/// data:image/png;base64,...[content]
		/// </summary>
		/// <param name="blob"></param>
		public static async Task<string> ImageInlineEncode(this byte[] blob)
		{
			if (blob == null)
			{
				throw new ArgumentNullException(nameof(blob));
			}
			var (_,_, imageType) = await blob.DetectImageType();
			return string.Concat("data:", imageType, ";base64,", Convert.ToBase64String(blob));
		}
		/// <summary>
		/// Decodes a base64 encoded string to a byte array and a mimeType
		/// </summary>
		/// <param name="content">a valid base64 inlined image of type png/gif/jpg etc</param>
		/// <returns></returns>
		public static (byte[] content, string mimeType) ImageInlineDecode(this string content)
		{
			if (string.IsNullOrEmpty(content))
			{
				throw new ArgumentNullException(nameof(content));
			}
			var parts = content.Split(';');

			string mimeType;
			if (parts.Length == 2)
			{
				if (parts[0].StartsWith("data:"))
				{
					mimeType = parts[0].Split("data:").Last();
					if (parts[1].StartsWith("base64", StringComparison.CurrentCultureIgnoreCase))
					{
						return (Convert.FromBase64String(parts[1].Split(',').Last()), mimeType);
					}
				}
			}

			return default;
		}
	}
}
