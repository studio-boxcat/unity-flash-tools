namespace FTSwfTools.SwfTags {
	public enum SwfTagType {
		// -----------------------------
		// Display list
		// -----------------------------

		PlaceObject   = 4,
		PlaceObject2  = 26,
		PlaceObject3  = 70,
		RemoveObject  = 5,
		RemoveObject2 = 28,
		ShowFrame     = 1,

		// -----------------------------
		// Control
		// -----------------------------

		SetBackgroundColor           = 9,
		FrameLabel                   = 43,
		Protect                      = 24,
		End                          = 0,
		ExportAssets                 = 56,
		ImportAssets                 = 57, // Unsupported
		EnableDebugger               = 58,
		EnableDebugger2              = 64,
		ScriptLimits                 = 65,
		SetTabIndex                  = 66, // Unsupported
		ImportAssets2                = 71, // Unsupported
		SymbolClass                  = 76,
		Metadata                     = 77,
		DefineScalingGrid            = 78, // Unsupported
		DefineSceneAndFrameLabelData = 86,

		// -----------------------------
		// Actions
		// -----------------------------

		DoAction     = 12, // Unsupported
		DoInitAction = 59, // Unsupported
		DoABC        = 82,

		// -----------------------------
		// Shape
		// -----------------------------

		DefineShape  = 2,
		DefineShape2 = 22,
		DefineShape3 = 32,
		DefineShape4 = 83,

		// -----------------------------
		// Bitmaps
		// -----------------------------

		DefineBits          = 6,  // Unsupported
		JPEGTables          = 8,  // Unsupported
		DefineBitsJPEG2     = 21, // Unsupported
		DefineBitsJPEG3     = 35, // Unsupported
		DefineBitsLossless  = 20,
		DefineBitsLossless2 = 36,
		DefineBitsJPEG4     = 90, // Unsupported

		// -----------------------------
		// Shape Morphing
		// -----------------------------

		DefineMorphShape  = 46, // Unsupported
		DefineMorphShape2 = 84, // Unsupported

		// -----------------------------
		// Fonts and Text
		// -----------------------------

		DefineFont = 10,           // Unsupported
		DefineFontInfo = 13,       // Unsupported
		DefineFontInfo2 = 62,      // Unsupported
		DefineFont2 = 48,          // Unsupported
		DefineFont3 = 75,          // Unsupported
		DefineFontAlignZones = 73, // Unsupported
		DefineFontName = 88,       // Unsupported
		DefineText = 11,           // Unsupported
		DefineText2 = 33,          // Unsupported
		DefineEditText = 37,       // Unsupported
		CSMTextSettings = 74,      // Unsupported
		DefineFont4 = 91,          // Unsupported

		// -----------------------------
		// Sounds
		// -----------------------------

		DefineSound = 14,      // Unsupported
		StartSound = 15,       // Unsupported
		StartSound2 = 89,      // Unsupported
		SoundStreamHead = 18,  // Unsupported
		SoundStreamHead2 = 45, // Unsupported
		SoundStreamBlock = 19, // Unsupported

		// -----------------------------
		// Buttons
		// -----------------------------

		DefineButton = 7,        // Unsupported
		DefineButton2 = 34,      // Unsupported
		DefineButtonCxform = 23, // Unsupported
		DefineButtonSound = 17,  // Unsupported

		// -----------------------------
		// Sprites and Movie Clips
		// -----------------------------

		DefineSprite = 39,

		// -----------------------------
		// Video
		// -----------------------------

		DefineVideoStream = 60, // Unsupported
		VideoFrame        = 61, // Unsupported

		// -----------------------------
		// Metadata
		// -----------------------------

		FileAttributes   = 69,
		EnableTelemetry  = 93,
		DefineBinaryData = 87,

		// -----------------------------
		// Unknown
		// -----------------------------

		Unknown
	}

	abstract class SwfTagBase {
		struct SwfTagData {
			public int    TagId;
			public byte[] TagData;
		}

		public abstract SwfTagType TagType { get; }
		public abstract TResult AcceptVisitor<TArg, TResult>(
			SwfTagVisitor<TArg, TResult> visitor, TArg arg);

		public static SwfTagBase Read(SwfStreamReader reader) {
			var type_and_size = reader.ReadUInt16();
			var tag_id        = type_and_size >> 6;
			var short_size    = type_and_size & 0x3f;
			var size          = short_size < 0x3f ? (uint)short_size : reader.ReadUInt32();
			var tag_data      = reader.ReadBytes(size);
			return Create(new SwfTagData{
				TagId   = tag_id,
				TagData = tag_data});
		}

		static SwfTagBase Create(SwfTagData tag_data)
		{
			var reader = new SwfStreamReader(tag_data.TagData);
			return (SwfTagType) tag_data.TagId switch
			{
				// Display list
				SwfTagType.PlaceObject => PlaceObjectTag.Create(reader),
				SwfTagType.PlaceObject2 => PlaceObject2Tag.Create(reader),
				SwfTagType.PlaceObject3 => PlaceObject3Tag.Create(reader),
				SwfTagType.RemoveObject => RemoveObjectTag.Create(reader),
				SwfTagType.RemoveObject2 => RemoveObject2Tag.Create(reader),
				SwfTagType.ShowFrame => ShowFrameTag.Create(reader),
				// Control
				SwfTagType.SetBackgroundColor => SetBackgroundColorTag.Create(reader),
				SwfTagType.FrameLabel => FrameLabelTag.Create(reader),
				SwfTagType.Protect => ProtectTag.Create(reader),
				SwfTagType.End => EndTag.Create(reader),
				SwfTagType.ExportAssets => ExportAssetsTag.Create(reader),
				SwfTagType.ImportAssets => UnsupportedTag.Create(SwfTagType.ImportAssets),
				SwfTagType.EnableDebugger => EnableDebuggerTag.Create(reader),
				SwfTagType.EnableDebugger2 => EnableDebugger2Tag.Create(reader),
				SwfTagType.ScriptLimits => ScriptLimitsTag.Create(reader),
				SwfTagType.SetTabIndex => UnsupportedTag.Create(SwfTagType.SetTabIndex),
				SwfTagType.ImportAssets2 => UnsupportedTag.Create(SwfTagType.ImportAssets2),
				SwfTagType.SymbolClass => SymbolClassTag.Create(reader),
				SwfTagType.Metadata => MetadataTag.Create(reader),
				SwfTagType.DefineScalingGrid => UnsupportedTag.Create(SwfTagType.DefineScalingGrid),
				SwfTagType.DefineSceneAndFrameLabelData => DefineSceneAndFrameLabelDataTag.Create(reader),
				// Actions
				SwfTagType.DoAction => UnsupportedTag.Create(SwfTagType.DoAction),
				SwfTagType.DoInitAction => UnsupportedTag.Create(SwfTagType.DoInitAction),
				SwfTagType.DoABC => DoABCTag.Create(reader),
				// Shape
				SwfTagType.DefineShape => DefineShapeTag.Create(reader),
				SwfTagType.DefineShape2 => DefineShape2Tag.Create(reader),
				SwfTagType.DefineShape3 => DefineShape3Tag.Create(reader),
				SwfTagType.DefineShape4 => DefineShape4Tag.Create(reader),
				// Bitmaps
				SwfTagType.DefineBits => UnsupportedTag.Create(SwfTagType.DefineBits),
				SwfTagType.JPEGTables => UnsupportedTag.Create(SwfTagType.JPEGTables),
				SwfTagType.DefineBitsJPEG2 => UnsupportedTag.Create(SwfTagType.DefineBitsJPEG2),
				SwfTagType.DefineBitsJPEG3 => UnsupportedTag.Create(SwfTagType.DefineBitsJPEG3),
				SwfTagType.DefineBitsLossless => DefineBitsLosslessTag.Create(reader),
				SwfTagType.DefineBitsLossless2 => DefineBitsLossless2Tag.Create(reader),
				SwfTagType.DefineBitsJPEG4 => UnsupportedTag.Create(SwfTagType.DefineBitsJPEG4),
				// Shape Morphing
				SwfTagType.DefineMorphShape => UnsupportedTag.Create(SwfTagType.DefineMorphShape),
				SwfTagType.DefineMorphShape2 => UnsupportedTag.Create(SwfTagType.DefineMorphShape2),
				// Fonts and Text
				SwfTagType.DefineFont => UnsupportedTag.Create(SwfTagType.DefineFont),
				SwfTagType.DefineFontInfo => UnsupportedTag.Create(SwfTagType.DefineFontInfo),
				SwfTagType.DefineFontInfo2 => UnsupportedTag.Create(SwfTagType.DefineFontInfo2),
				SwfTagType.DefineFont2 => UnsupportedTag.Create(SwfTagType.DefineFont2),
				SwfTagType.DefineFont3 => UnsupportedTag.Create(SwfTagType.DefineFont3),
				SwfTagType.DefineFontAlignZones => UnsupportedTag.Create(SwfTagType.DefineFontAlignZones),
				SwfTagType.DefineFontName => UnsupportedTag.Create(SwfTagType.DefineFontName),
				SwfTagType.DefineText => UnsupportedTag.Create(SwfTagType.DefineText),
				SwfTagType.DefineText2 => UnsupportedTag.Create(SwfTagType.DefineText2),
				SwfTagType.DefineEditText => UnsupportedTag.Create(SwfTagType.DefineEditText),
				SwfTagType.CSMTextSettings => UnsupportedTag.Create(SwfTagType.CSMTextSettings),
				SwfTagType.DefineFont4 => UnsupportedTag.Create(SwfTagType.DefineFont4),
				// Sounds
				SwfTagType.DefineSound => UnsupportedTag.Create(SwfTagType.DefineSound),
				SwfTagType.StartSound => UnsupportedTag.Create(SwfTagType.StartSound),
				SwfTagType.StartSound2 => UnsupportedTag.Create(SwfTagType.StartSound2),
				SwfTagType.SoundStreamHead => UnsupportedTag.Create(SwfTagType.SoundStreamHead),
				SwfTagType.SoundStreamHead2 => UnsupportedTag.Create(SwfTagType.SoundStreamHead2),
				SwfTagType.SoundStreamBlock => UnsupportedTag.Create(SwfTagType.SoundStreamBlock),
				// Buttons
				SwfTagType.DefineButton => UnsupportedTag.Create(SwfTagType.DefineButton),
				SwfTagType.DefineButton2 => UnsupportedTag.Create(SwfTagType.DefineButton2),
				SwfTagType.DefineButtonCxform => UnsupportedTag.Create(SwfTagType.DefineButtonCxform),
				SwfTagType.DefineButtonSound => UnsupportedTag.Create(SwfTagType.DefineButtonSound),
				// Sprites and Movie Clips
				SwfTagType.DefineSprite => DefineSpriteTag.Create(reader),
				// Video
				SwfTagType.DefineVideoStream => UnsupportedTag.Create(SwfTagType.DefineVideoStream),
				SwfTagType.VideoFrame => UnsupportedTag.Create(SwfTagType.VideoFrame),
				// Metadata
				SwfTagType.FileAttributes => FileAttributesTag.Create(reader),
				SwfTagType.EnableTelemetry => EnableTelemetryTag.Create(reader),
				SwfTagType.DefineBinaryData => DefineBinaryDataTag.Create(reader),
				_ => UnknownTag.Create(tag_data.TagId)
			};
		}
	}
}