﻿using FileToVox.Converter;
using FileToVox.Converter.Image;
using FileToVox.Converter.PaletteSchematic;
using FileToVox.Converter.PointCloud;
using FileToVoxCore.Schematics;
using FileToVoxCore.Vox;
using NDesk.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace FileToVox
{
	class Program
	{
		private const string INPUT_SEPARATOR = ";";
		
		private const string ASC_EXTENSION = ".asc";
		private const string BINVOX_EXTENSION = ".binvox";
		private const string CSV_EXTENSION = ".csv";
		private const string PLY_EXTENSION = ".ply";
		private const string PNG_EXTENSION = ".png";
		private const string TIF_EXTENSION = ".tif";
		private const string QB_EXTENSION = ".qb";
		private const string SCHEMATIC_EXTENSION = ".schematic";
		private const string XYZ_EXTENSION = ".xyz";
		private const string VOX_EXTENSION = ".vox";
		private const string OBJ_EXTENSION = ".obj";
		private const string FBX_EXTENSION = ".fbx";
		
		private static string INPUT_PATH;
		private static string OUTPUT_PATH;
		private static string INPUT_COLOR_FILE;
		private static string INPUT_PALETTE_FILE;

		private static bool SHOW_HELP;
		private static bool EXCAVATE;
		private static bool COLOR;
		private static bool DISABLE_QUANTIZATION;

		private static float GRID_SIZE = 10;
		private static int HEIGHT_MAP = 1;
		private static int COLOR_LIMIT = 256;

		public static void Main(string[] args)
		{
			OptionSet options = new()
			{
				{"i|input=", "input path", v => INPUT_PATH = v},
				{"o|output=", "output path", v => OUTPUT_PATH = v},
				{"c|color", "enable color when generating heightmap", v => COLOR = v != null},
				{"cm|color-from-file=", "load colors from file", v => INPUT_COLOR_FILE = v },
				{"cl|color-limit=", "set the maximal number of colors for the palette", (int v) => COLOR_LIMIT =v },
				{"cs|chunk-size=", "set the chunk size", (int v) => Schematic.CHUNK_SIZE = v},
				{"e|excavate", "delete all voxels which doesn't have at least one face connected with air",  v => EXCAVATE = v != null },
				{"h|help", "help information", v => SHOW_HELP = v != null},
				{"hm|heightmap=", "create voxels terrain from heightmap (only for PNG file)", (int v) => HEIGHT_MAP = v},
				{"p|palette=", "set the palette", v => INPUT_PALETTE_FILE = v },
				{"gs|grid-size=", "set the grid-size", (float v) => GRID_SIZE = v},
				{"d|debug", "enable the debug mode", v => Schematic.DEBUG = v != null},
				{"dq|disable-quantization", "Disable the quantization step ", v => DISABLE_QUANTIZATION= v != null},
			};

			try
			{
				List<string> extra = options.Parse(args);
				DisplayInformation();
				CheckHelp(options);
				CheckArguments();
				DisplayArguments();
				CreateOutputPathIfNeeded();
				
				bool success = Process();

				Console.WriteLine(success ? "[INFO] Done." : "[ERROR] Failed.");
				
				if (Schematic.DEBUG)
				{
					Console.ReadKey();
				}
			}
			catch (Exception e)
			{
				Console.Write("FileToVox: ");
				Console.WriteLine(e.Message);
				Console.WriteLine("Try `FileToVox --help` for more information.");
				Console.ReadLine();
			}
		}

		private static void CreateOutputPathIfNeeded()
		{
			string directoryPath = Path.GetFullPath(OUTPUT_PATH);
			
			if (Path.HasExtension(directoryPath))
				directoryPath = directoryPath[..^Path.GetExtension(directoryPath).Length];

			if (string.IsNullOrEmpty(directoryPath))
				return;
			
			if (!Directory.Exists(directoryPath))
				Directory.CreateDirectory(directoryPath);
		}

		private static void DisplayInformation()
		{
			Console.WriteLine("[INFO] FileToVox v" + Assembly.GetExecutingAssembly().GetName().Version);
			Console.WriteLine("[INFO] Author: @Zarbuz. Contact : https://twitter.com/Zarbuz");
		}

		private static void CheckHelp(OptionSet options)
		{
			if (!SHOW_HELP) 
				return;
			ShowHelp(options);
			Environment.Exit(0);
		}

		private static void CheckArguments()
		{
			if (INPUT_PATH == null)
				throw new ArgumentNullException("[ERROR] Missing required option: --i");
			if (OUTPUT_PATH == null)
				throw new ArgumentNullException("[ERROR] Missing required option: --o");
			if (GRID_SIZE is < 10 or > Schematic.MAX_WORLD_LENGTH)
				throw new ArgumentException("[ERROR] --grid-size argument must be greater than 10 and smaller than " + Schematic.MAX_WORLD_LENGTH);
			if (HEIGHT_MAP < 1)
				throw new ArgumentException("[ERROR] --heightmap argument must be positive");
			if (COLOR_LIMIT is < 0 or > 256)
				throw new ArgumentException("[ERROR] --color-limit argument must be between 1 and 256");
			if (Schematic.CHUNK_SIZE <= 10 || Schematic.CHUNK_SIZE > 256)
				throw new ArgumentException("[ERROR] --chunk-size argument must be between 10 and 256");
		}

		private static void DisplayArguments()
		{
			if (INPUT_PATH != null)
				Console.WriteLine("[INFO] Specified input path: " + INPUT_PATH);
			if (OUTPUT_PATH != null)
				Console.WriteLine("[INFO] Specified output path: " + OUTPUT_PATH);
			if (INPUT_COLOR_FILE != null)
				Console.WriteLine("[INFO] Specified input color file: " + INPUT_COLOR_FILE);
			if (INPUT_PALETTE_FILE != null)
				Console.WriteLine("[INFO] Specified palette file: " + INPUT_PALETTE_FILE);
			if (COLOR_LIMIT != 256)
				Console.WriteLine("[INFO] Specified color limit: " + COLOR_LIMIT);
			if (Math.Abs(GRID_SIZE - 10) > 0.0001f)
				Console.WriteLine("[INFO] Specified grid size: " + GRID_SIZE);
			if (Schematic.CHUNK_SIZE != 128)
				Console.WriteLine("[INFO] Specified chunk size: " + Schematic.CHUNK_SIZE);
			if (EXCAVATE)
				Console.WriteLine("[INFO] Enabled option: excavate");
			if (COLOR)
				Console.WriteLine("[INFO] Enabled option: color");
			if (HEIGHT_MAP != 1)
				Console.WriteLine("[INFO] Enabled option: heightmap (value=" + HEIGHT_MAP + ")");
			if (Schematic.DEBUG)
				Console.WriteLine("[INFO] Enabled option: debug");
			if (DISABLE_QUANTIZATION)
				Console.WriteLine("[INFO] Enabled option: disable-quantization");
		}

		private static bool Process()
		{
			Console.WriteLine("Start processing...");
			string[] paths = INPUT_PATH.Split(INPUT_SEPARATOR);
			return Process(paths);
		}

		private static bool Process(IEnumerable<string> paths)
		{
			bool success = true;
			
			foreach (string path in paths)
			{
				Console.WriteLine($"Processing path ({path})...");

				if (string.IsNullOrEmpty(path))
				{
					Console.WriteLine("[ERROR] Cannot process empty path");
					continue;
				}

				if (Directory.Exists(path))
				{
					Console.WriteLine("[INFO] Processing Folder: " + path);
					success = success && Process(Directory.GetFiles(path));
					continue;
				}

				if (!File.Exists(path))
				{
					Console.WriteLine("[ERROR] File not found at: " + path);
					continue;
				}


				if (!IsSupportedExtension(path))
				{
					Console.WriteLine("[ERROR] File extension not supported: " + path);
					continue;
				}
				
				success = success && ProcessFile(path);
			}
			
			return success;
		}

		private static bool ProcessFile(string path)
		{
			Console.WriteLine("[INFO] File processed: " + path);
			
			try {
				AbstractToSchematic converter = GetConverter(path);
				if (converter != null)
				{
					return SchematicToVox(converter);
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				return false;
			}

			return true;
		}

		private static AbstractToSchematic GetConverter(string path)
		{
			switch (Path.GetExtension(path))
			{
				case ASC_EXTENSION:
					return new ASCToSchematic(path);
				case BINVOX_EXTENSION:
					return new BinvoxToSchematic(path);
				case CSV_EXTENSION:
					return new CSVToSchematic(path, GRID_SIZE, COLOR_LIMIT);
				case PLY_EXTENSION:
					return new PLYToSchematic(path, GRID_SIZE, COLOR_LIMIT);
				case PNG_EXTENSION:
				case TIF_EXTENSION:
					return new ImageToSchematic(path, INPUT_COLOR_FILE, HEIGHT_MAP, EXCAVATE, COLOR, COLOR_LIMIT);
				case QB_EXTENSION:
					return new QBToSchematic(path);
				case SCHEMATIC_EXTENSION:
					return new SchematicToSchematic(path, EXCAVATE);
				case XYZ_EXTENSION:
					return new XYZToSchematic(path, GRID_SIZE, COLOR_LIMIT);
				case VOX_EXTENSION:
					return new VoxToSchematic(path);
				case OBJ_EXTENSION:
				case FBX_EXTENSION:
					throw new Exception("[FAILED] Voxelization of 3D models is no longer done in FileToVox but with MeshToVox. Check the url : https://github.com/Zarbuz/FileToVox/releases for download link");
				default:
					return null;
			}
		}
		
		private static bool IsSupportedExtension(string path)
		{
			switch (Path.GetExtension(path))
			{
				case ASC_EXTENSION:
				case BINVOX_EXTENSION:
				case CSV_EXTENSION:
				case PLY_EXTENSION:
				case PNG_EXTENSION:
				case TIF_EXTENSION:
				case QB_EXTENSION:
				case SCHEMATIC_EXTENSION:
				case XYZ_EXTENSION:
				case VOX_EXTENSION:
				case OBJ_EXTENSION:
				case FBX_EXTENSION:
					return true;
				default:
					return false;
			}
		}

		private static bool SchematicToVox(AbstractToSchematic converter)
		{
			Schematic schematic = converter.WriteSchematic();
			Console.WriteLine($"[INFO] Vox Width: {schematic.Width}");
			Console.WriteLine($"[INFO] Vox Length: {schematic.Length}");
			Console.WriteLine($"[INFO] Vox Height: {schematic.Height}");

			if (schematic.Width > Schematic.MAX_WORLD_WIDTH || schematic.Length > Schematic.MAX_WORLD_LENGTH || schematic.Height > Schematic.MAX_WORLD_HEIGHT)
			{
				Console.WriteLine("[ERROR] Model is too big ! MagicaVoxel can't support model bigger than 2000x2000x1000");
				return false;
			}

			VoxWriter writer = new();

			if (INPUT_PALETTE_FILE == null)
				return writer.WriteModel(FormatOutputDestination(converter.filePath), null, schematic);
			
			PaletteSchematicConverter converterPalette = new(INPUT_PALETTE_FILE);
			schematic = converterPalette.ConvertSchematic(schematic);
			return writer.WriteModel(FormatOutputDestination(converter.filePath), converterPalette.GetPalette(), schematic);
		}

		private static string FormatOutputDestination(string path)
		{
			string fileName = Path.GetFileNameWithoutExtension(path);
			return Path.Join(OUTPUT_PATH, fileName + VOX_EXTENSION);
		}

		private static void ShowHelp(OptionSet p)
		{
			Console.WriteLine("Usage: FileToVox --i INPUT --o OUTPUT");
			Console.WriteLine("Options: ");
			p.WriteOptionDescriptions(Console.Out);
		}

		public static bool DisableQuantization()
		{
			return DISABLE_QUANTIZATION;
		}
	}
}
