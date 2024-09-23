using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FTRuntime;
using UnityEngine;
using UnityEngine.Assertions;

namespace FTEditor.Importer {
	readonly struct FrameDef
	{
		public readonly RenderUnit[] Batches;
		public FrameDef(RenderUnit[] batches) => Batches = batches;
	}

	readonly struct SequenceDef
	{
		public readonly string Name;
		public readonly FrameDef[] Frames;
		public readonly SwfClipAsset.Label[] Labels;

		public SequenceDef(string name, FrameDef[] frames, SwfClipAsset.Label[] labels)
		{
			Name = name;
			Frames = frames;
			Labels = labels;
		}
	}

	static class ClipBaker {
		public static SwfClipAsset.Sequence[] Bake(SwfFrameData[] frames, AtlasDef atlasDef,
			out Mesh[] meshes,
			out SwfClipAsset.MaterialGroup[] materialGroups)
		{
			var bakedSequences = BakeSequenceDef(frames, atlasDef);
			return BuildSequences(bakedSequences, out meshes, out materialGroups);
		}

		static SequenceDef[] BakeSequenceDef(SwfFrameData[] frameData, AtlasDef atlasDef)
		{
			var frames = new FrameDef[frameData.Length];
			Parallel.For(0, frameData.Length,
				i => frames[i] = BakeFrame(frameData[i], atlasDef));

			var sb = new SequenceBuilder();
			for (var index = 0; index < frameData.Length; index++)
				sb.Feed(frameData[index], frames[index]);
			return sb.Flush();

			static FrameDef BakeFrame(SwfFrameData frame, AtlasDef atlasDef)
			{
				var batcher = new InstanceBatcher();
				foreach (var inst in frame.Instances)
					batcher.Feed(inst, atlasDef[inst.Bitmap]);
				return new FrameDef(batcher.Flush());
			}
		}

		static SwfClipAsset.Sequence[] BuildSequences(SequenceDef[] sequences,
			out Mesh[] meshes,
			out SwfClipAsset.MaterialGroup[] materialGroups)
		{
			var meshStore = new Dictionary<int, Mesh>(); // Key: Hash, Value: Mesh
			var materialStore = new Dictionary<int, (byte, Material[])>(); // Key: Hash, Value: (GroupIndex, Materials)

			var assetSequences = new SwfClipAsset.Sequence[sequences.Length];

			for (var si = 0; si < sequences.Length; si++)
			{
				var sequenceDef = sequences[si];
				var assetFrames = new SwfClipAsset.Frame[sequenceDef.Frames.Length];

				for (var fi = 0; fi < sequenceDef.Frames.Length; fi++)
				{
					var frame = sequenceDef.Frames[fi];
					var mesh = GetOrCreateMesh(frame.Batches.Select(x => x.Mesh).ToArray(), meshStore);
					var materialGroupIndex = GetOrAddMaterialGroup(
						frame.Batches.Select(x => x.Material).ToArray(), materialStore);
					assetFrames[fi] = new SwfClipAsset.Frame(mesh, materialGroupIndex);
				}

				assetSequences[si] = new SwfClipAsset.Sequence(
					sequenceDef.Name, assetFrames, sequenceDef.Labels);
			}

			meshes = meshStore.Values.ToArray();
			materialGroups = new SwfClipAsset.MaterialGroup[materialStore.Count];
			foreach (var (groupIndex, materials) in materialStore.Values)
			{
				Assert.IsNull(materialGroups[groupIndex].Materials, "MaterialGroup index collision.");
				materialGroups[groupIndex] = new SwfClipAsset.MaterialGroup(materials);
			}
			return assetSequences;

			static Mesh GetOrCreateMesh(MeshData[] meshes, Dictionary<int, Mesh> meshStore)
			{
				var hash = OrderSensitiveHash(meshes);
				if (meshStore.TryGetValue(hash, out var mesh))
					return mesh;

				mesh = MeshBuilder.Build(meshes.ToArray());
				meshStore.Add(hash, mesh);
				return mesh;
			}

			static byte GetOrAddMaterialGroup(MaterialKey[] materialKeys, Dictionary<int, (byte, Material[])> materialStore)
			{
				var hash = OrderSensitiveHash(materialKeys);
				if (materialStore.TryGetValue(hash, out var data))
					return data.Item1;

				var materials = new Material[materialKeys.Length];
				for (var index = 0; index < materialKeys.Length; index++)
					materials[index] = SwfMaterialCache.Query(materialKeys[index]);

				var groupIndex = (byte) materialStore.Count;
				materialStore.Add(hash, (groupIndex, materials));
				return groupIndex;
			}

			static int OrderSensitiveHash<T>(T[] items)
			{
				var hash = 0;
				for (var i = 0; i < items.Length; i++)
				{
					hash ^= i; // Order matters.
					hash ^= items[i].GetHashCode();
				}

				return hash;
			}
		}

			// Mesh
			// var batchCount = batches.Length;
			/*
			// Material
			var materials = new Material[batchCount];
			for (var index = 0; index < batchCount; index++)
			{
				var p = batches[index].Material;
				var material = SwfMaterialCache.Query(p.Type, p.BlendMode, p.ClipDepth);
				Assert.IsNotNull(material);
				materials[index] = material;
			}

			return new FrameDef(
				MeshData.GetHashCode(meshes),
				context.MaterialMemory.ResolveMaterialGroupIndex(materials));
			*/

		struct SequenceBuilder
		{
			string _curSequenceName;
			List<SequenceDef> _sequences;
			List<FrameDef> _curFrames;
			List<SwfClipAsset.Label> _curLabels;

			public void Feed(SwfFrameData swfFrameData, FrameDef bakedFrame)
			{
				_sequences ??= new List<SequenceDef>();
				_curFrames ??= new List<FrameDef>();
				_curLabels ??= new List<SwfClipAsset.Label>();

				var anchor = swfFrameData.Anchor;
				if (!string.IsNullOrEmpty(anchor) && _curSequenceName != anchor)
				{
					// 시퀀스가 변경되었다면 시퀀스를 추가해줌.
					if (_curSequenceName is not null)
						_sequences.Add(new SequenceDef(
							_curSequenceName, _curFrames.ToArray(), _curLabels.ToArray()));

					_curSequenceName = anchor;
					_curFrames = new List<FrameDef>();
					_curLabels = new List<SwfClipAsset.Label>();
				}

				// 프레임 삽입.
				var frameIndex = (ushort) _curFrames.Count;
				_curFrames.Add(bakedFrame);

				// 레이블 삽입.
				foreach (var label in swfFrameData.Labels)
					_curLabels.Add(new SwfClipAsset.Label(SwfHash.Hash(label), frameIndex));
			}

			public SequenceDef[] Flush()
			{
				if (_curFrames is {Count: > 0})
				{
					_sequences.Add(new SequenceDef(
						_curSequenceName, _curFrames.ToArray(), _curLabels.ToArray()));
				}

				var sequences = _sequences.ToArray();
				_sequences.Clear();
				return sequences;
			}
		}
	}
}