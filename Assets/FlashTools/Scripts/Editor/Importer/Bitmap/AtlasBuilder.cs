using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FTRuntime;
using FTSwfTools;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace FTEditor.Importer
{
    static class AtlasBuilder
    {
        public static Texture2D PackAtlas(
            string outputPath, string spriteFolder, int maxSize, int shapePadding)
        {
            var dataPath = outputPath.Replace(".png", ".tpsheet");
            var result = ExecutePack(outputPath, dataPath, spriteFolder, maxSize, shapePadding);
            if (result is false) return null;
            AssetDatabase.ImportAsset(outputPath);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(outputPath);
        }

        [MustUseReturnValue]
        public static bool ExecutePack(string sheet, string data, string spriteFolder, int maxSize, int shapePadding)
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
                "--tracer-tolerance 200 " +
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
                L.E($"TexturePacker failed with exit code {proc.ExitCode}\n{err}");
                return false;
            }

            return true;
        }

        public static Mesh[] MigrateSpriteToMesh(string atlasPath, string outputDir, out MeshId[] bitmapToMesh)
        {
            // create output directory
            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            // assign mesh id
            var sprites = AssetDatabase.LoadAllAssetsAtPath(atlasPath)
                .OfType<Sprite>()
                .Select(x => (Bitmap: (BitmapId) int.Parse(x.name), Sprite: x))
                .OrderBy(x => (int) x.Bitmap)
                .ToArray();
            DeleteFrom(outputDir, (MeshId) sprites.Length); // remove old meshes

            // create meshes
            var meshCount = sprites.Length;
            var meshes = new Mesh[meshCount];
            for (var i = 0; i < meshCount; i++) // mesh index
            {
                var mesh = CreateOrGetMesh(outputDir, (MeshId) i);
                SpriteMeshFactory.Set(sprites[i].Sprite, mesh);
                meshes[i] = mesh;
            }

            // delete .tpsheet & sub-asset sprites
            var dataPath = atlasPath.Replace(".png", ".tpsheet");
            AssetDatabase.DeleteAsset(dataPath);
            foreach (var (_, sprite) in sprites)
                AssetDatabase.RemoveObjectFromAsset(sprite);

            // configure texture
            var ti = (TextureImporter) AssetImporter.GetAtPath(atlasPath);
            ti.textureType = TextureImporterType.Default;
            ti.filterMode = FilterMode.Bilinear;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;

            // build bitmap to mesh map
            var maxBitmapId = (int) sprites[^1].Bitmap;
            bitmapToMesh = new MeshId[maxBitmapId + 1];
            for (var i = 0; i < meshCount; i++)
            {
                var bitmap = (int) sprites[i].Bitmap;
                bitmapToMesh[bitmap] = (MeshId) i;
            }

            return meshes;


            static Mesh CreateOrGetMesh(string root, MeshId id)
            {
                var name = id.ToName();
                var path = $"{root}/{name}.asset";
                var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (mesh is not null) return mesh;

                mesh = MeshBuilder.CreateEmpty(false);
                AssetDatabase.CreateAsset(mesh, path);
                return mesh;
            }

            static void DeleteFrom(string root, MeshId start)
            {
                var toDelete = Directory.GetFiles(root, "*.asset")
                    .Where(x =>
                    {
                        var name = Path.GetFileNameWithoutExtension(x);
                        if (name == "00") return false; // skip SwfClip asset
                        var id = MeshIdUtils.ToIndex(name);
                        return id >= start;
                    })
                    .ToArray();

                var errors = new List<string>();
                AssetDatabase.DeleteAssets(toDelete, errors);
                foreach (var error in errors)
                    L.E("Failed to delete mesh: " + error);
            }
        }
    }
}