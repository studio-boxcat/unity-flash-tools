using System.Collections.Generic;
using System.Threading.Tasks;
using FTRuntime;
using UnityEngine;
using UnityEngine.Assertions;

namespace FTEditor.Importer
{
    static class SwfClipBaker
    {
        public static void Bake(SwfFrameData[] data, Mesh[] objMeshes, MeshId[] bitmapToMesh, out SwfFrame[] frames, out SwfSequence[] sequences)
        {
            frames = BuildFrames(data, objMeshes, bitmapToMesh, out var frameMap, out var materialGroups);
            sequences = BuildSequences(data, frameMap, materialGroups);
        }

        static SwfFrame[] BuildFrames( // returns: asset frames
            SwfFrameData[] data,
            Mesh[] objMeshes, // asset frame index -> mesh
            MeshId[] bitmapToMesh, // BitmapId -> MeshId
            out Dictionary<int, SwfFrameId> frameMap, // swf frame index -> asset frame index
            out MaterialGroupIndex[] materialGroups) // asset frame index -> material group index
        {
            // get index counts.
            var indexCounts = new ushort[objMeshes.Length];
            for (var i = 0; i < objMeshes.Length; i++)
            {
                if (objMeshes[i] == null) continue;
                indexCounts[i] = (ushort) objMeshes[i].GetIndexCount(0);
            }

            // build frames.
            var orgFrames = new SwfFrame[data.Length];
            var orgMaterials = new object[data.Length]; // to avoid compilation error, use object[] instead of MaterialKey[][].
            Parallel.For(0, data.Length, i =>
            {
                var b = new SwfBatcher();
                foreach (var inst in data[i].Instances) b.Feed(inst, bitmapToMesh);
                orgFrames[i] = b.Flush(indexCounts, out var material);
                orgMaterials[i] = material;
            });

            // remove duplicate frames.
            frameMap = new Dictionary<int, SwfFrameId>(); // swf frame index -> asset frame index
            var frames = new List<SwfFrame>();
            var materials = new List<MaterialKey[]>();
            for (var i = 0; i < orgFrames.Length; i++)
            {
                if (ContainsFrame(orgFrames[i], frames, out var j)
                    && MaterialKey.Equals((MaterialKey[]) orgMaterials[i], materials[(int) j])) // material should also be same.
                {
                    L.I($"[SwfClipBaker] Frame {i} is duplicated with frame {j}");
                    frameMap.Add(i, j);
                }
                else
                {
                    frameMap.Add(i, (SwfFrameId) frames.Count);
                    frames.Add(orgFrames[i]);
                    materials.Add((MaterialKey[]) orgMaterials[i]);
                }
            }

            // build material groups.
            materialGroups = new MaterialGroupIndex[frames.Count];
            var materialStore = MaterialStore.Instance;
            for (var i = 0; i < frames.Count; i++)
                materialGroups[i] = materialStore.Put(LoadMaterials(materials[i]));

            return frames.ToArray();


            static bool ContainsFrame(SwfFrame frame, List<SwfFrame> frames, out SwfFrameId index)
            {
                for (var i = 0; i < frames.Count; i++)
                {
                    if (frames[i].Equals(frame))
                    {
                        index = (SwfFrameId) i;
                        return true;
                    }
                }

                index = default;
                return false;
            }

            static Material[] LoadMaterials(MaterialKey[] materialKeys)
            {
                var materials = new Material[materialKeys.Length];
                for (var i = 0; i < materialKeys.Length; i++)
                    materials[i] = SwfMaterialCache.Load(materialKeys[i]);
                return materials;
            }
        }

        static SwfSequence[] BuildSequences(
            SwfFrameData[] frameData,
            Dictionary<int, SwfFrameId> frameMap, // swf frame index -> asset frame index
            MaterialGroupIndex[] materialGroups) // asset frame index -> material group index
        {
            List<SwfSequence> yield = new();

            string curSequenceName = default;
            var curFrames = new List<SwfFrameId>();
            var curMaterialGroup = MaterialGroupIndex.Invalid;

            for (var index = 0; index < frameData.Length; index++)
            {
                var anchor = frameData[index].Anchor;
                var assetFrameId = (int) frameMap[index];
                var materialGroup = materialGroups[assetFrameId];

                if (!string.IsNullOrEmpty(anchor) && curSequenceName != anchor)
                {
                    // 시퀀스가 변경되었다면 시퀀스를 추가해줌.
                    if (curSequenceName is not null) // curSequenceName could be null at the first iteration.
                        yield.Add(CreateSequence(curSequenceName, curFrames, materialGroup));

                    curSequenceName = anchor;
                    curFrames.Clear();
                    curMaterialGroup = materialGroup;
                }

                Assert.AreEqual(curMaterialGroup, materialGroup,
                    "Material group must be consistent within a sequence");
                curFrames.Add((SwfFrameId) assetFrameId);
            }

            if (curFrames is { Count: > 0 })
                yield.Add(CreateSequence(curSequenceName!, curFrames, curMaterialGroup));

            return yield.ToArray();

            static SwfSequence CreateSequence(string name, List<SwfFrameId> frames, MaterialGroupIndex materialGroup)
                => new(SwfHash.SequenceId(name), frames.ToArray(), materialGroup);
        }
    }
}