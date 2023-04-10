using FileToVox.Utils;
using FileToVoxCore.Schematics;
using ImageMagick;
using System;
using System.IO;

namespace FileToVox.Converter.Image
{
	public class ImageToSchematic : AbstractToSchematic
    {
        protected readonly bool Excavate;
        protected readonly int MaxHeight;
        protected readonly bool Color;
        protected readonly string ColorPath;
        protected readonly int ColorLimit;

        public ImageToSchematic(string filePath, string colorPath, int height, bool excavate, bool color, int colorLimit) : base(filePath)
        {
            ColorPath = colorPath;
            MaxHeight = height;
            Excavate = excavate;
            Color = color;
            ColorLimit = colorLimit;
        }

        public override Schematic WriteSchematic()
        {
	        if (!File.Exists(filePath))
	        {
		        Console.WriteLine("[ERROR] The file path is invalid for path : " + filePath);
		        return null;
	        }

	        if (!string.IsNullOrEmpty(ColorPath) && !File.Exists(ColorPath))
	        {
		        Console.WriteLine("[ERROR] The color path is invalid");
		        return null;
	        }

	        MagickImage image = new MagickImage(filePath);
	        MagickImage colorImage = null;

			if (!string.IsNullOrEmpty(ColorPath))
			{
				colorImage = new MagickImage(ColorPath);
			}

			LoadImageParam loadImageParam = new LoadImageParam()
			{
				TexturePath = filePath,
				ColorLimit = ColorLimit,
				ColorTexturePath = ColorPath,
				EnableColor = Color,
				Excavate = Excavate,
				Height = MaxHeight,
			};
			return ImageUtils.WriteSchematicFromImage(image, colorImage, loadImageParam);

		}
	}
}
