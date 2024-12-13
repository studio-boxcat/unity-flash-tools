using System.Collections.Generic;
using System.Linq;
using FTEditor;
using FTRuntime;

namespace FTSwfTools {

	//
	// SwfLibrary
	//

	abstract class SwfLibraryDefine {
		public string ExportName;
	}

	class SwfLibraryShapeDefine : SwfLibraryDefine
	{
		public readonly (BitmapId, SwfMatrix)[] Bitmaps;
		public SwfLibraryShapeDefine((BitmapId, SwfMatrix)[] bitmaps) => Bitmaps = bitmaps;
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
		readonly SortedDictionary<DefineId, SwfLibraryDefine> _defines = new();

		public SwfLibraryDefine this[DefineId define_id] => _defines[define_id];

		public void Add(DefineId define_id, SwfLibraryDefine define) => _defines.Add(define_id, define);

		public SwfLibraryShapeDefine GetShapeDefine(DefineId define_id) => (SwfLibraryShapeDefine) _defines[define_id];
		public SwfLibrarySpriteDefine GetSpriteDefine(DefineId define_id) => (SwfLibrarySpriteDefine) _defines[define_id];

		public IEnumerable<SwfLibrarySpriteDefine> GetSpriteDefines() => _defines.Values.OfType<SwfLibrarySpriteDefine>();

		public Dictionary<BitmapId, IBitmapData> GetBitmaps()
		{
			return _defines
				.Where(p => p.Value is SwfLibraryBitmapDefine)
				.ToDictionary(
					p => (BitmapId) p.Key,
					p => ((SwfLibraryBitmapDefine) p.Value).Data);
		}
	}

	//
	// SwfDisplayList
	//

	abstract class SwfDisplayInstance {
		public DefineId          Id;
		public Depth             Depth;
		public Depth             ClipDepth;
		public bool              Visible;
		public SwfMatrix         Matrix;
		public SwfBlendMode      BlendMode;
		public SwfColorTransform ColorTransform;
	}

	class SwfDisplayShapeInstance : SwfDisplayInstance {
	}

	class SwfDisplayBitmapInstance : SwfDisplayInstance {
		public BitmapId Bitmap => (BitmapId) Id;
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