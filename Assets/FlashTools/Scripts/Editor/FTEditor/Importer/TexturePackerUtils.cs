using System.Diagnostics;

namespace FTEditor.Importer
{
    static class TexturePackerUtils
    {
        public static void Pack(string sheet, string data, string spriteFolder, int maxSize, int shapePadding)
        {
            // https://www.codeandweb.com/texturepacker/documentation/texture-settings
            const string texturePacker = "/Applications/TexturePacker.app/Contents/MacOS/TexturePacker";

            var arguments =
                $"--format unity-texture2d --sheet {sheet} --data {data} " +
                "--alpha-handling ReduceBorderArtifacts " +
                $"--max-size {maxSize} " +
                "--size-constraints AnySize " +
                "--algorithm Polygon " +
                "--trim-mode Polygon " +
                "--trim-margin 0 " +
                "--tracer-tolerance 80 " +
                "--extrude 0 " +
                $"--shape-padding {shapePadding} " +
                "--pack-mode Best " +
                "--enable-rotation " +
                spriteFolder;

            L.I($"Running TexturePacker: {texturePacker} {arguments}");

            var procInfo = new ProcessStartInfo(texturePacker, arguments)
            {
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };
            var proc = Process.Start(procInfo);
            proc!.WaitForExit();

            if (proc.ExitCode != 0)
            {
                var err = proc.StandardError.ReadToEnd();
                throw new System.Exception($"TexturePacker failed with exit code {proc.ExitCode}\n{err}");
            }
        }
    }
}