using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System.IO;
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
					if ( tag.TagType == SwfTagType.End )
						break;
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
			var symbols = new List<SwfSymbolData>();
			library = new SwfLibrary();
			symbols.Add(LoadSymbol(stage_symbol, tags, library));
			var sprite_defs = library.Defines.Values
				.OfType<SwfLibrarySpriteDefine>()
				.Where(p => !string.IsNullOrEmpty(p.ExportName))
				.ToList();
			foreach (var def in sprite_defs) {
				var name = def.ExportName;
				var control_tags = def.ControlTags.Tags;
				symbols.Add(LoadSymbol(name, control_tags, library));
			}
			return symbols.ToArray();
		}

		static SwfSymbolData LoadSymbol(string symbol_name, SwfTagBase[] tags, SwfLibrary library)
		{
			var disp_lst = new SwfDisplayList();
			var executer = new SwfContextExecuter(library, 0);
			var symbol_frames = new List<SwfFrameData>();
			while ( executer.NextFrame(tags, disp_lst) ) {
				symbol_frames.Add(LoadSymbolFrameData(library, disp_lst));
			}
			return new SwfSymbolData{
				Name   = symbol_name,
				Frames = symbol_frames};
		}

		static SwfFrameData LoadSymbolFrameData(
			SwfLibrary library, SwfDisplayList display_list)
		{
			var frame = new SwfFrameData{
				Anchor = display_list.FrameAnchors.Count > 0
					? display_list.FrameAnchors[0]
					: string.Empty,
				Labels = new List<string>(display_list.FrameLabels)};
			return AddDisplayListToFrame(
				library,
				display_list,
				Matrix4x4.identity,
				SwfBlendModeData.identity,
				SwfColorTransData.identity,
				0,
				0,
				null,
				frame);
		}

		static SwfFrameData AddDisplayListToFrame(
			SwfLibrary            library,
			SwfDisplayList        display_list,
			Matrix4x4             parent_matrix,
			SwfBlendModeData      parent_blend_mode,
			SwfColorTransData     parent_color_transform,
			ushort                parent_masked,
			ushort                parent_mask,
			List<SwfInstanceData> parent_masks,
			SwfFrameData          frame)
		{
			var self_masks = new List<SwfInstanceData>();
			foreach ( var inst in display_list.Instances.Values.Where(p => p.Visible) ) {
				CheckSelfMasks(self_masks, inst.Depth, frame);
				var child_matrix          = parent_matrix          * inst.Matrix;
				var child_blend_mode      = parent_blend_mode      * (SwfBlendModeData) inst.BlendMode;
				var child_color_transform = parent_color_transform * inst.ColorTransform;
				switch ( inst.Type ) {
				case SwfDisplayInstanceType.Shape:
					AddShapeInstanceToFrame(
						library,
						inst as SwfDisplayShapeInstance,
						child_matrix,
						child_blend_mode,
						child_color_transform,
						parent_masked,
						parent_mask,
						parent_masks,
						self_masks,
						frame);
					break;
				case SwfDisplayInstanceType.Bitmap:
					AddBitmapInstanceToFrame(
						library,
						inst as SwfDisplayBitmapInstance,
						child_matrix,
						child_blend_mode,
						child_color_transform,
						parent_masked,
						parent_mask,
						parent_masks,
						self_masks,
						frame);
					break;
				case SwfDisplayInstanceType.Sprite:
					AddSpriteInstanceToFrame(
						library,
						inst as SwfDisplaySpriteInstance,
						child_matrix,
						child_blend_mode,
						child_color_transform,
						parent_masked,
						parent_mask,
						parent_masks,
						self_masks,
						frame);
					break;
				default:
					throw new UnityException($"unsupported SwfDisplayInstanceType: {inst.Type}");
				}
			}
			CheckSelfMasks(self_masks, ushort.MaxValue, frame);
			return frame;
		}

		static void AddShapeInstanceToFrame(
			SwfLibrary              library,
			SwfDisplayShapeInstance inst,
			Matrix4x4               inst_matrix,
			SwfBlendModeData        inst_blend_mode,
			SwfColorTransData       inst_color_transform,
			ushort                  parent_masked,
			ushort                  parent_mask,
			List<SwfInstanceData>   parent_masks,
			List<SwfInstanceData>   self_masks,
			SwfFrameData            frame)
		{
			var shape_def = library.FindDefine<SwfLibraryShapeDefine>(inst.Id);
			if (shape_def == null) return;

			for ( var i = 0; i < shape_def.Bitmaps.Length; ++i ) {
				var bitmap_id     = shape_def.Bitmaps[i];
				var bitmap_def    = library.FindDefine<SwfLibraryBitmapDefine>(bitmap_id);
				if (bitmap_def is null) continue;

				var bitmap_matrix = i < shape_def.Matrices.Length ? shape_def.Matrices[i] : Matrix4x4.identity;
				var frame_inst_type =
					(parent_mask > 0 || inst.ClipDepth > 0)
						? SwfInstanceData.Types.Mask
						: (parent_masked > 0 || self_masks.Count > 0)
							? SwfInstanceData.Types.Masked
							: SwfInstanceData.Types.Group;
				var frame_inst_clip_depth =
					(parent_mask > 0)
						? parent_mask
						: (inst.ClipDepth > 0)
							? inst.ClipDepth
							: parent_masked + self_masks.Count;
				frame.Instances.Add(new SwfInstanceData{
					Type       = frame_inst_type,
					ClipDepth  = (ushort)frame_inst_clip_depth,
					Bitmap     = bitmap_id,
					Matrix     = inst_matrix * bitmap_matrix,
					BlendMode  = inst_blend_mode,
					ColorTrans = inst_color_transform});
				if ( parent_mask > 0 ) {
					parent_masks.Add(frame.Instances[^1]);
				} else if ( inst.ClipDepth > 0 ) {
					self_masks.Add(frame.Instances[^1]);
				}
			}
		}

		static void AddBitmapInstanceToFrame(
			SwfLibrary               library,
			SwfDisplayBitmapInstance inst,
			Matrix4x4                inst_matrix,
			SwfBlendModeData         inst_blend_mode,
			SwfColorTransData        inst_color_transform,
			ushort                   parent_masked,
			ushort                   parent_mask,
			List<SwfInstanceData>    parent_masks,
			List<SwfInstanceData>    self_masks,
			SwfFrameData             frame)
		{
			var bitmap_def = library.FindDefine<SwfLibraryBitmapDefine>(inst.Id);
			if (bitmap_def is null) return;

			var frame_inst_type =
				(parent_mask > 0 || inst.ClipDepth > 0)
					? SwfInstanceData.Types.Mask
					: (parent_masked > 0 || self_masks.Count > 0)
						? SwfInstanceData.Types.Masked
						: SwfInstanceData.Types.Group;
			var frame_inst_clip_depth =
				(parent_mask > 0)
					? parent_mask
					: (inst.ClipDepth > 0)
						? inst.ClipDepth
						: parent_masked + self_masks.Count;
			frame.Instances.Add(new SwfInstanceData{
				Type       = frame_inst_type,
				ClipDepth  = (ushort)frame_inst_clip_depth,
				Bitmap     = inst.Id,
				Matrix     = inst_matrix,
				BlendMode  = inst_blend_mode,
				ColorTrans = inst_color_transform});
			if ( parent_mask > 0 ) {
				parent_masks.Add(frame.Instances[^1]);
			} else if ( inst.ClipDepth > 0 ) {
				self_masks.Add(frame.Instances[^1]);
			}
		}

		static void AddSpriteInstanceToFrame(
			SwfLibrary               library,
			SwfDisplaySpriteInstance inst,
			Matrix4x4                inst_matrix,
			SwfBlendModeData         inst_blend_mode,
			SwfColorTransData        inst_color_transform,
			ushort                   parent_masked,
			ushort                   parent_mask,
			List<SwfInstanceData>    parent_masks,
			List<SwfInstanceData>    self_masks,
			SwfFrameData             frame)
		{
			var sprite_def = library.FindDefine<SwfLibrarySpriteDefine>(inst.Id);
			if (sprite_def is null) return;

			AddDisplayListToFrame(
				library,
				inst.DisplayList,
				inst_matrix,
				inst_blend_mode,
				inst_color_transform,
				(ushort)(parent_masked + self_masks.Count),
				(ushort)(parent_mask > 0
					? parent_mask
					: (inst.ClipDepth > 0
						? inst.ClipDepth
						: (ushort)0)),
				parent_mask > 0
					? parent_masks
					: (inst.ClipDepth > 0
						? self_masks
						: null),
				frame);
		}

		static void CheckSelfMasks(
			List<SwfInstanceData> masks,
			ushort                depth,
			SwfFrameData          frame)
		{
			foreach ( var mask in masks )
			{
				if (mask.ClipDepth >= depth) continue;
				frame.Instances.Add(new SwfInstanceData{
					Type       = SwfInstanceData.Types.MaskReset,
					ClipDepth  = 0,
					Bitmap     = mask.Bitmap,
					Matrix     = mask.Matrix,
					BlendMode  = mask.BlendMode,
					ColorTrans = mask.ColorTrans});
			}
			masks.RemoveAll(p => p.ClipDepth < depth);
		}
	}
}