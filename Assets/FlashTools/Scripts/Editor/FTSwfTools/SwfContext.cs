using System.Collections.Generic;
using System.Linq;
using FTSwfTools.SwfTags;
using FTSwfTools.SwfTypes;
using UnityEngine;

namespace FTSwfTools {

	//
	// SwfLibrary
	//

	abstract class SwfLibraryDefine {
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
		readonly SortedDictionary<ushort, SwfLibraryDefine> _defines = new();

		public SwfLibraryDefine this[ushort define_id] => _defines[define_id];

		public void Add(ushort define_id, SwfLibraryDefine define) => _defines.Add(define_id, define);

		public SwfLibraryShapeDefine GetShapeDefine(ushort define_id) => (SwfLibraryShapeDefine) _defines[define_id];
		public SwfLibrarySpriteDefine GetSpriteDefine(ushort define_id) => (SwfLibrarySpriteDefine) _defines[define_id];

		public IEnumerable<SwfLibrarySpriteDefine> GetSpriteDefines() => _defines.Values.OfType<SwfLibrarySpriteDefine>();

		public Dictionary<ushort, IBitmapData> GetBitmaps()
		{
			return _defines
				.Where(p => p.Value is SwfLibraryBitmapDefine)
				.ToDictionary(
					p => p.Key,
					p => ((SwfLibraryBitmapDefine) p.Value).Data);
		}
	}

	enum Depth : ushort { }

	//
	// SwfDisplayList
	//

	abstract class SwfDisplayInstance {
		public ushort            Id;
		public Depth             Depth;
		public Depth             ClipDepth;
		public bool              Visible;
		public Matrix4x4         Matrix;
		public SwfBlendMode      BlendMode;
		public SwfColorTransform ColorTransform;
	}

	class SwfDisplayShapeInstance : SwfDisplayInstance {
	}

	class SwfDisplayBitmapInstance : SwfDisplayInstance {
	}

	class SwfDisplaySpriteInstance : SwfDisplayInstance {
		public int            CurrentTag  = 0;
		public SwfDisplayList DisplayList = new();

		public void Reset() {
			CurrentTag  = 0;
			DisplayList = new SwfDisplayList();
		}
	}

	class SwfDisplayList {
		public readonly SortedDictionary<Depth, SwfDisplayInstance> Instances = new();
		public readonly List<string>     FrameLabels  = new();
		public readonly List<string>     FrameAnchors = new();
	}
}