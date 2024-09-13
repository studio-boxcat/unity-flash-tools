namespace FTEditor.Importer
{
    static class TexturePackerUtils
    {
        public static void Pack(string sheet, string data, string spriteFolder)
        {
            const string texturePacker = "/Applications/TexturePacker.app/Contents/MacOS/TexturePacker";

            var proc = System.Diagnostics.Process.Start(texturePacker,
                $"--format unity-texture2d --sheet {sheet} --data {data} " +
                "--alpha-handling ReduceBorderArtifacts " +
                "--max-size 800 " +
                "--size-constraints AnySize " +
                "--extrude 4 " +
                "--algorithm Polygon " +
                "--trim-mode Polygon " +
                "--trim-margin 0 " +
                "--tracer-tolerance 50 " +
                "--pack-mode Best " +
                "--enable-rotation " +
                spriteFolder);

            proc!.WaitForExit();
        }
    }
}