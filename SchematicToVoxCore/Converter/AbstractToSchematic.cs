using FileToVoxCore.Schematics;

namespace FileToVox.Converter
{
	public abstract class AbstractToSchematic
    {
        public string filePath { get; private set; }

        protected AbstractToSchematic(string filePath)
        {
            this.filePath = filePath;
        }

        protected AbstractToSchematic()
        {

        }

        public abstract Schematic WriteSchematic();

    }
}
