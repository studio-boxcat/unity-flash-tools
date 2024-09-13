using UnityEngine;
using System.Collections.Generic;
using FTRuntime;
using UnityEngine.Assertions;

namespace FTEditor.Importer {
	static class ClipBaker {
		public static void Bake(SwfSymbolData symbol, AtlasDef atlasDef,
			out SwfClipAsset.Sequence[] sequences,
			out Mesh[] meshes,
			out SwfClipAsset.MaterialGroup[] materialGroups)
		{
			var context = new ConvertContext();
			sequences = BakeSequences(symbol, atlasDef, context);
			materialGroups = context.MaterialMemory.Bake();
			meshes = context.MeshMemory.GetMeshes();
		}

		static SwfClipAsset.Sequence[] BakeSequences(
			SwfSymbolData symbol, AtlasDef atlasDef, ConvertContext context)
		{
			var sb = new SequenceBuilder();
			foreach ( var frame in symbol.Frames )
				sb.Feed(frame, BakeFrame(frame, atlasDef, context));
			return sb.Flush();
		}

		static SwfClipAsset.Frame BakeFrame(
			SwfFrameData frame, AtlasDef atlasDef, ConvertContext context)
		{
			// Batch
			var batcher = new InstanceBatcher();
			foreach ( var inst in frame.Instances ) {
				var spriteData = atlasDef[inst.Bitmap];
				batcher.Feed(inst, spriteData);
			}
			var batches = batcher.Flush();
			var batchCount = batches.Length;

			// Mesh
			var meshes = new MeshData[batchCount];
			for (var index = 0; index < batchCount; index++)
			{
				meshes[index] = new MeshData(
					batches[index].Poses,
					batches[index].UVAs,
					batches[index].Indices);
			}

			// Material
			var materials = new Material[batchCount];
			for (var index = 0; index < batchCount; index++)
			{
				var p = batches[index].Property;
				var material = SwfMaterialCache.Query(p.Type, p.BlendMode, p.ClipDepth);
				Assert.IsNotNull(material);
				materials[index] = material;
			}

			return new SwfClipAsset.Frame(
				context.MeshMemory.GetOrAdd(MeshBuilder.GetHashCode(meshes), () => MeshBuilder.Build(meshes)),
				context.MaterialMemory.ResolveMaterialGroupIndex(materials));
		}

		struct SequenceBuilder
		{
			string _curSequenceName;
			List<SwfClipAsset.Frame> _curFrames;
			List<SwfClipAsset.Label> _curLabels;
			List<SwfClipAsset.Sequence> _sequences;

			public void Feed(SwfFrameData frame, SwfClipAsset.Frame bakedFrame)
			{
				_sequences ??= new List<SwfClipAsset.Sequence>();
				_curFrames ??= new List<SwfClipAsset.Frame>();
				_curLabels ??= new List<SwfClipAsset.Label>();

				if (!string.IsNullOrEmpty(frame.Anchor) && _curSequenceName != frame.Anchor)
				{
					// 시퀀스가 변경되었다면 시퀀스를 추가해줌.
					if (_curSequenceName is not null)
						_sequences.Add(new SwfClipAsset.Sequence(
							_curSequenceName, _curFrames.ToArray(), _curLabels.ToArray()));

					_curSequenceName = frame.Anchor;
					_curFrames = new List<SwfClipAsset.Frame>();
					_curLabels = new List<SwfClipAsset.Label>();
				}

				// 프레임 삽입.
				var frameIndex = (ushort) _curFrames.Count;
				_curFrames.Add(bakedFrame);

				// 레이블 삽입.
				foreach (var label in frame.Labels)
					_curLabels.Add(new SwfClipAsset.Label(SwfHash.Hash(label), frameIndex));
			}

			public SwfClipAsset.Sequence[] Flush()
			{
				if (_curFrames is {Count: > 0})
				{
					_sequences.Add(new SwfClipAsset.Sequence(
						_curSequenceName, _curFrames.ToArray(), _curLabels.ToArray()));
				}

				var sequences = _sequences.ToArray();
				_sequences.Clear();
				return sequences;
			}
		}
	}
}