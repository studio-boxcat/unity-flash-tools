using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using FTRuntime.Internal;
using FTSwfTools;
using FTSwfTools.SwfTags;
using FTSwfTools.SwfTypes;

namespace FTEditor.Importer {
	readonly struct SwfFileData
	{
		public readonly float FrameRate;
		public readonly SwfTagBase[] Tags;

		public SwfFileData(float frameRate, SwfTagBase[] tags)
		{
			FrameRate = frameRate;
			Tags = tags;
		}
	}

	static class SwfParser {
        public const string stage_symbol = "_Stage_";

		public static SwfFileData Parse(string swf_path)
		{
			using var stream = DecompressSwfData(File.ReadAllBytes(swf_path));
			var header = DecodeSwf(new SwfStreamReader(stream), out var tags);
			return new SwfFileData(header.FrameRate, tags.ToArray());

			static MemoryStream DecompressSwfData(byte[] raw_swf_data) {
				var raw_reader = new SwfStreamReader(raw_swf_data);
				var originalHeader = SwfShortHeader.Read(raw_reader);
				switch ( originalHeader.Format ) {
				case "FWS":
					return new MemoryStream(raw_swf_data);
				case "CWS":
					var rest_stream = SwfStreamReader.DecompressZBytes(
						raw_reader.ReadRest());
					var new_short_header = new SwfShortHeader{
						Format     = "FWS",
						Version    = originalHeader.Version,
						FileLength = originalHeader.FileLength};
					var uncompressed_stream = new MemoryStream();
					SwfShortHeader.Write(new_short_header, uncompressed_stream);
					rest_stream.CopyTo(uncompressed_stream);
					uncompressed_stream.Position = 0;
					return uncompressed_stream;
				default:
					throw new System.Exception("Unsupported swf format: " + originalHeader.Format);
				}
			}

			static SwfLongHeader DecodeSwf(SwfStreamReader reader, out List<SwfTagBase> tags) {
				var header = SwfLongHeader.Read(reader);
				tags = new List<SwfTagBase>();
				while ( !reader.IsEOF ) {
					var tag = SwfTagBase.Read(reader);
					if ( tag is EndTag ) break;
					tags.Add(tag);
				}
				return header;
			}
		}

		// ---------------------------------------------------------------------
		//
		// LoadSymbols
		//
		// ---------------------------------------------------------------------

		public static SwfSymbolData[] LoadSymbols(SwfTagBase[] tags, out SwfLibrary library)
		{
			var l = new SwfLibrary();
			var symbol = LoadSymbol(stage_symbol, tags, l);

			var symbols = new List<SwfSymbolData> { symbol };
			symbols.AddRange(l.GetSpriteDefines()
				.Where(p => !string.IsNullOrEmpty(p.ExportName))
				.Select(def => LoadSymbol(def.ExportName, def.ControlTags, l)));

			library = l;
			return symbols.ToArray();
		}

		static SwfSymbolData LoadSymbol(string symbol_name, SwfTagBase[] tags, SwfLibrary library)
		{
			var disp_lst = new SwfDisplayList();
			var executer = new SwfContextExecuter(library, 0);
			var symbol_frames = new List<SwfFrameData>();
			while ( executer.NextFrame(tags, disp_lst) )
				symbol_frames.Add(LoadSymbolFrameData(library, disp_lst));
			return new SwfSymbolData(symbol_name, symbol_frames.ToArray());
		}

		static SwfFrameData LoadSymbolFrameData(
			SwfLibrary library, SwfDisplayList display_list)
		{
			var instances = new List<SwfInstanceData>();
			AddDisplayListToFrame(
				library,
				display_list,
				SwfMatrix.identity,
				SwfBlendMode.Normal,
				SwfColorTransData.identity,
				0,
				0,
				null,
				instances);

			var anchor = display_list.FrameAnchors.Count > 0 ? display_list.FrameAnchors[0] : string.Empty;
			var labels = display_list.FrameLabels.ToArray();
			return new SwfFrameData(anchor, labels, instances.ToArray());
		}

		static void AddDisplayListToFrame(
			SwfLibrary            library,
			SwfDisplayList        display_list,
			SwfMatrix             parent_matrix,
			SwfBlendMode          parent_blend_mode,
			SwfColorTransData     parent_color_transform,
			ushort                parent_masked,
			Depth                 parent_mask,
			List<SwfInstanceData> parent_masks,
			List<SwfInstanceData> result)
		{
			var self_masks = new List<SwfInstanceData>();
			foreach ( var inst in display_list.Instances.Values.Where(p => p.Visible) ) {
				for (var i = self_masks.Count - 1; i >= 0; i--)
				{
					var self_mask = self_masks[i];
					if (self_mask.ClipDepth >= inst.Depth) continue;
					result.Add(SwfInstanceData.MaskOut(self_mask));
					self_masks.RemoveAt(i);
				}

				var masked = parent_masked + self_masks.Count;
				var child_type = ResolveInstType(parent_mask, inst.ClipDepth, masked);
				var child_depth = ResolveClipDepth(parent_mask, inst.ClipDepth, masked);
				var child_matrix          = parent_matrix          * inst.Matrix;
				var child_blend_mode      = parent_blend_mode.Composite(inst.BlendMode);
				var child_color_transform = parent_color_transform * inst.ColorTransform;

				switch ( inst ) {
				case SwfDisplayShapeInstance:
				{
					var shape_def = library.GetShapeDefine(inst.Id);
					result.AddRange(shape_def.Bitmaps.Select(x =>
					{
						var (bitmap_id, bitmap_matrix) = x;
						return new SwfInstanceData
						{
							Type = child_type,
							ClipDepth = child_depth,
							Bitmap = bitmap_id,
							Matrix = child_matrix * bitmap_matrix,
							BlendMode = child_blend_mode,
							ColorTrans = child_color_transform
						};
					}));

					var inst_data = result[^1];
					if ( parent_mask > 0 ) {
						parent_masks.Add(inst_data);
					} else if ( inst.ClipDepth > 0 ) {
						self_masks.Add(inst_data);
					}
					break;
				}
				case SwfDisplayBitmapInstance b:
				{
					var inst_data = new SwfInstanceData{
						Type       = child_type,
						ClipDepth  = child_depth,
						Bitmap     = b.Bitmap,
						Matrix     = child_matrix,
						BlendMode  = child_blend_mode,
						ColorTrans = child_color_transform};
					result.Add(inst_data);

					if ( parent_mask > 0 ) {
						parent_masks.Add(inst_data);
					} else if ( inst.ClipDepth > 0 ) {
						self_masks.Add(inst_data);
					}
					break;
				}
				case SwfDisplaySpriteInstance s:
				{
					AddDisplayListToFrame(
						library,
						s.DisplayList,
						child_matrix,
						child_blend_mode,
						child_color_transform,
						parent_masked: (ushort)masked,
						parent_mask: parent_mask > 0
							? parent_mask
							: (inst.ClipDepth > 0
								? inst.ClipDepth
								: 0),
						parent_masks: parent_mask > 0
							? parent_masks
							: (inst.ClipDepth > 0
								? self_masks
								: null),
						result);
					break;
				}
				default:
					throw new UnityException($"unsupported SwfDisplayInstanceType: {inst.GetType()}");
				}
			}

			result.AddRange(self_masks.Select(SwfInstanceData.MaskOut));
			self_masks.Clear();
		}

		static Depth ResolveClipDepth(Depth parent_mask, Depth inst_mask, int masked)
		{
			if (parent_mask > 0) return parent_mask;
			if (inst_mask > 0) return inst_mask;
			return (Depth)masked;
		}

		static SwfInstanceData.Types ResolveInstType(Depth parent_mask, Depth inst_mask, int masked)
		{
			if (parent_mask > 0 || inst_mask > 0) return SwfInstanceData.Types.MaskIn;
			return masked is not 0 ? SwfInstanceData.Types.Masked : SwfInstanceData.Types.Simple;
		}
	}
}