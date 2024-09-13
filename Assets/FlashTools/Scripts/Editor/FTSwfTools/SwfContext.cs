using System;
using System.Collections.Generic;
using System.Linq;
using FTSwfTools.SwfTags;
using FTSwfTools.SwfTypes;
using UnityEngine;

namespace FTSwfTools {

	using LibraryDefines   = SortedDictionary<ushort, SwfLibraryDefine>;
	using DisplayInstances = SortedDictionary<ushort, SwfDisplayInstance>;

	//
	// SwfLibrary
	//

	public enum SwfLibraryDefineType {
		Shape,
		Bitmap,
		Sprite
	}

	public abstract class SwfLibraryDefine {
		public string ExportName = string.Empty;
		public abstract SwfLibraryDefineType Type { get; }
	}

	class SwfLibraryShapeDefine : SwfLibraryDefine {
		public ushort[]    Bitmaps  = Array.Empty<ushort>();
		public Matrix4x4[] Matrices = Array.Empty<Matrix4x4>();

		public override SwfLibraryDefineType Type => SwfLibraryDefineType.Shape;
	}

	class SwfLibraryBitmapDefine : SwfLibraryDefine {
		public IBitmapData Data;

		public SwfLibraryBitmapDefine(IBitmapData data) => Data = data;

		public override SwfLibraryDefineType Type => SwfLibraryDefineType.Bitmap;
	}

	class SwfLibrarySpriteDefine : SwfLibraryDefine {
		public SwfControlTags ControlTags = SwfControlTags.identity;

		public override SwfLibraryDefineType Type => SwfLibraryDefineType.Sprite;
	}

	class SwfLibrary {
		public readonly LibraryDefines Defines = new();

		public bool HasDefine<T>(ushort define_id) where T : SwfLibraryDefine {
			return FindDefine<T>(define_id) != null;
		}

		public T FindDefine<T>(ushort define_id) where T : SwfLibraryDefine
		{
			return Defines.TryGetValue(define_id, out var def)
				? def as T : null;
		}

		public Dictionary<ushort, IBitmapData> GetBitmaps()
		{
			return Defines
				.Where(p => p.Value.Type is SwfLibraryDefineType.Bitmap)
				.ToDictionary(
					p => p.Key,
					p => ((SwfLibraryBitmapDefine) p.Value).Data);
		}
	}

	//
	// SwfDisplayList
	//

	public enum SwfDisplayInstanceType {
		Shape,
		Bitmap,
		Sprite
	}

	abstract class SwfDisplayInstance {
		public abstract SwfDisplayInstanceType Type { get; }

		public ushort            Id;
		public ushort            Depth;
		public ushort            ClipDepth;
		public bool              Visible;
		public Matrix4x4         Matrix;
		public SwfBlendMode      BlendMode;
		public SwfSurfaceFilters FilterList;
		public SwfColorTransform ColorTransform;
	}

	class SwfDisplayShapeInstance : SwfDisplayInstance {
		public override SwfDisplayInstanceType Type => SwfDisplayInstanceType.Shape;
	}

	class SwfDisplayBitmapInstance : SwfDisplayInstance {
		public override SwfDisplayInstanceType Type => SwfDisplayInstanceType.Bitmap;
	}

	class SwfDisplaySpriteInstance : SwfDisplayInstance {
		public int            CurrentTag  = 0;
		public SwfDisplayList DisplayList = new SwfDisplayList();

		public override SwfDisplayInstanceType Type => SwfDisplayInstanceType.Sprite;

		public void Reset() {
			CurrentTag  = 0;
			DisplayList = new SwfDisplayList();
		}
	}

	class SwfDisplayList {
		public DisplayInstances Instances    = new DisplayInstances();
		public List<string>     FrameLabels  = new List<string>();
		public List<string>     FrameAnchors = new List<string>();
	}
}