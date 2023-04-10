using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using FileToVoxCore.Schematics.Tools;

namespace FileToVox.Converter.PointCloud
{
	public class CSVToSchematic : PointCloudToSchematic
    {
	    protected sealed override BodyDataDTO ReadContentFile()
	    {
			BodyDataDTO dataFile = new();

			List<Vector3> bodyVertices = new();
			List<Color> bodyColors = new();
			using (StreamReader reader = new(filePath))
			{
				while (!reader.EndOfStream)
				{
					string line = reader.ReadLine() ?? "";
					line = line.Replace(" ", "");
					string[] data = line.Split(',');
					if (data.Length > 14)
					{
						try
						{
							float[] values = new float[data.Length];
							for (int i = 0; i < data.Length; i++)
							{
								string s = data[i];
								values[i] = float.Parse(s, CultureInfo.InvariantCulture);
							}

							Vector3 vertex = new(values[11], values[12], values[13]);
							bodyVertices.Add(vertex);
							bodyColors.Add(Color.FromArgb((byte)Math.Round(values[7] * 255),
								(byte)Math.Round(values[8] * 255),
								(byte)Math.Round(values[9] * 255)));
						}
						catch (Exception e)
						{
							// ignored
						}
					}
				}
			}

			dataFile.BodyColors = bodyColors;
			dataFile.BodyVertices = bodyVertices;

			return dataFile;
	    }
        public CSVToSchematic(string filePath, float scale, int colorLimit) : base(filePath, scale, colorLimit)
        {
	        BodyDataDTO data = ReadContentFile();
			VoxelizeData(data);
        }
    }
}
