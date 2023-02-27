using System;
using System.Collections.Generic;
using UnityEngine;

namespace FTRuntime {
	public class SwfClipAsset : ScriptableObject {
		[Serializable]
		public struct Frame {
			public Mesh       Mesh;
			public byte       MaterialGroupIndex;

			public Frame(Mesh mesh, byte materialGroupIndex) {
				Mesh  = mesh;
				MaterialGroupIndex = materialGroupIndex;
			}
		}

		[Serializable]
		public struct Sequence {
			public string  Name;
			public Frame[] Frames;
			public Label[] Labels;

			public Sequence(string name, Frame[] frames, Label[] labels)
			{
				Name = name;
				Frames = frames;
				Labels = labels;
			}

			public bool IsValid => Frames != null;
			public bool IsInvalid => Frames == null;
		}

		[Serializable]
		public struct MaterialGroup
		{
			public Material[] Materials;

			public MaterialGroup(Material[] materials)
			{
				Materials = materials;
			}
		}

		[Serializable]
		public struct Label
		{
			public uint   NameHash;
			public ushort FrameIndex;

			public Label(uint nameHash, ushort frameIndex)
			{
				NameHash = nameHash;
				FrameIndex = frameIndex;
			}
		}

		public string          Name;
		public Texture2D       Atlas;
		public float           FrameRate;
		public string          AssetGUID;
		public Sequence[]      Sequences;
		public MaterialGroup[] MaterialGroups;

		public bool TryGetSequence(string name, out Sequence sequence)
		{
			foreach (var curSequence in Sequences)
			{
				if (curSequence.Name == name)
				{
					sequence = curSequence;
					return true;
				}
			}

			sequence = default;
			return false;
		}

		public Sequence GetSequence(string name)
		{
			return TryGetSequence(name, out var sequence)
				? sequence
				: throw new KeyNotFoundException(name);
		}

		public Material[] GetMaterials(int materialGroupIndex)
		{
			return MaterialGroups[materialGroupIndex].Materials;
		}

#if UNITY_EDITOR
		void Reset() {
			Name      = string.Empty;
			Atlas     = null;
			FrameRate = 1.0f;
			AssetGUID = string.Empty;
			Sequences = Array.Empty<Sequence>();
			MaterialGroups = Array.Empty<MaterialGroup>();
	}
#endif
	}
}