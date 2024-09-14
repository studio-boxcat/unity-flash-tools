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

	public abstract class SwfLibraryDefine {
		public string ExportName = string.Empty;
	}

	class SwfLibraryShapeDefine : SwfLibraryDefine
	{
		public readonly (ushort, Matrix4x4)[] Bitmaps;
		public SwfLibraryShapeDefine((ushort, Matrix4x4)[] bitmaps) => Bitmaps = bitmaps;
	}

	class SwfLibraryBitmapDefine : SwfLibraryDefine {
		public readonly IBitmapData Data;
		public SwfLibraryBitmapDefine(IBitmapData data) => Data = data;
	}

	class SwfLibrarySpriteDefine : SwfLibraryDefine {
		public readonly SwfTagBase[] ControlTags;
		public SwfLibrarySpriteDefine(SwfTagBase[] controlTags) => ControlTags = controlTags;
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

		public bool TryGet(ushort define_id, out SwfLibraryDefine define) => Defines.TryGetValue(define_id, out define);

		public SwfLibraryShapeDefine GetShapeDefine(ushort define_id) => (SwfLibraryShapeDefine) Defines[define_id];
		public SwfLibrarySpriteDefine GetSpriteDefine(ushort define_id) => (SwfLibrarySpriteDefine) Defines[define_id];

		public Dictionary<ushort, IBitmapData> GetBitmaps()
		{
			return Defines
				.Where(p => p.Value is SwfLibraryBitmapDefine)
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
		public SwfDisplayList DisplayList = new();

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