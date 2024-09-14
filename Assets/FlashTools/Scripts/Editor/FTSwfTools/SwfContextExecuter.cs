using System.Linq;
using System.Collections.Generic;
using FTSwfTools.SwfTags;
using FTSwfTools.SwfTypes;
using UnityEngine;

namespace FTSwfTools {
	class SwfContextExecuter {
		public readonly SwfLibrary   Library;
		public int                   CurrentTag;

		public SwfContextExecuter(SwfLibrary library, int current_tag) {
			Library    = library;
			CurrentTag = current_tag;
		}

		public bool NextFrame(SwfTagBase[] tags, SwfDisplayList dl) {
			dl.FrameLabels.Clear();
			dl.FrameAnchors.Clear();
			while ( CurrentTag < tags.Length ) {
				var tag = tags[CurrentTag++];
				Visit(this, tag, dl);
				if ( tag is ShowFrameTag ) {
					ChildrenNextFrameLooped(dl);
					return true;
				}
			}
			ChildrenNextFrameLooped(dl);
			return false;
		}

		static void Visit(SwfContextExecuter executer, SwfTagBase tag, SwfDisplayList displayList)
		{
			switch (tag)
			{
			case PlaceObjectTag t: executer.Visit(t, displayList); break;
			case PlaceObject2Tag t: executer.Visit(t, displayList); break;
			case PlaceObject3Tag t: executer.Visit(t, displayList); break;
			case RemoveObjectTag t: executer.Visit(t, displayList); break;
			case RemoveObject2Tag t: executer.Visit(t, displayList); break;

			case FrameLabelTag t: executer.Visit(t, displayList); break;
			case ExportAssetsTag t: executer.Visit(t, displayList); break;
			case SymbolClassTag t: executer.Visit(t, displayList); break;

			case DefineShapeTag t: executer.Visit(t, displayList); break;
			case DefineShape2Tag t: executer.Visit(t, displayList); break;
			case DefineShape3Tag t: executer.Visit(t, displayList); break;
			case DefineShape4Tag t: executer.Visit(t, displayList); break;

			case DefineBitsLosslessTag t: executer.Visit(t, displayList); break;
			case DefineBitsLossless2Tag t: executer.Visit(t, displayList); break;

			case DefineSpriteTag t: executer.Visit(t, displayList); break;

			case ShowFrameTag:
			case SetBackgroundColorTag:
			case ProtectTag:
			case EndTag:
			case EnableDebuggerTag:
			case EnableDebugger2Tag:
			case ScriptLimitsTag:
			case MetadataTag:
			case DefineSceneAndFrameLabelDataTag:
			case DoABCTag:
			case FileAttributesTag:
			case EnableTelemetryTag:
			case DefineBinaryDataTag:
				break;

			case UnknownTag:
			case UnsupportedTag:
				L.W(tag.ToString());
				break;

			}
		}

		public void Visit(PlaceObjectTag tag, SwfDisplayList dl) {
			var new_inst = CreateDisplayInstanceFromDefine(Library[tag.CharacterId]);
			new_inst.Id             = tag.CharacterId;
			new_inst.Depth          = tag.Depth;
			new_inst.ClipDepth      = 0;
			new_inst.Visible        = true;
			new_inst.Matrix         = tag.Matrix;
			new_inst.BlendMode      = SwfBlendMode.identity;
			new_inst.ColorTransform = tag.ColorTransform;
			dl.Instances.Add(new_inst.Depth, new_inst);
		}

		public void Visit(PlaceObject2Tag tag, SwfDisplayList dl) {
			if ( tag.HasCharacter ) {
				SwfDisplayInstance old_inst = null;
				if ( tag.Move ) { // replace character
					dl.Instances.Remove(tag.Depth, out old_inst);
				}

				// new character
				var new_inst = CreateDisplayInstanceFromDefine(Library[tag.CharacterId]);
				new_inst.Id             = tag.CharacterId;
				new_inst.Depth          = tag.Depth;
				new_inst.ClipDepth      = tag.HasClipDepth      ? tag.ClipDepth      : (old_inst?.ClipDepth ?? 0);
				new_inst.Visible        = true;
				new_inst.Matrix         = tag.HasMatrix         ? tag.Matrix         : (old_inst?.Matrix ?? Matrix4x4.identity);
				new_inst.BlendMode      = SwfBlendMode.identity;
				new_inst.ColorTransform = tag.HasColorTransform ? tag.ColorTransform : (old_inst?.ColorTransform ?? default);
				dl.Instances.Add(new_inst.Depth, new_inst);
			} else if ( tag.Move ) { // move character
				var inst = dl.Instances[tag.Depth];
				if ( tag.HasClipDepth ) inst.ClipDepth = tag.ClipDepth;
				if ( tag.HasMatrix ) inst.Matrix = tag.Matrix;
				if ( tag.HasColorTransform ) inst.ColorTransform = tag.ColorTransform;
			}
		}

		public void Visit(PlaceObject3Tag tag, SwfDisplayList dl) {
			if ( tag.HasCharacter ) {
				SwfDisplayInstance old_inst = null;
				if ( tag.Move ) { // replace character
					dl.Instances.Remove(tag.Depth, out old_inst);
				}
				// new character
				var new_inst = CreateDisplayInstanceFromDefine(Library[tag.CharacterId]);
				new_inst.Id             = tag.CharacterId;
				new_inst.Depth          = tag.Depth;
				new_inst.ClipDepth      = tag.HasClipDepth      ? tag.ClipDepth      : old_inst?.ClipDepth ?? 0;
				new_inst.Visible        = tag.HasVisible        ? tag.Visible        : (old_inst == null || old_inst.Visible);
				new_inst.Matrix         = tag.HasMatrix         ? tag.Matrix         : old_inst?.Matrix ?? Matrix4x4.identity;
				new_inst.BlendMode      = tag.HasBlendMode      ? tag.BlendMode      : old_inst?.BlendMode ?? SwfBlendMode.identity;
				new_inst.ColorTransform = tag.HasColorTransform ? tag.ColorTransform : old_inst?.ColorTransform ?? default;
				dl.Instances.Add(new_inst.Depth, new_inst);
			} else if ( tag.Move ) { // move character
				var inst = dl.Instances[tag.Depth];
				if ( tag.HasClipDepth ) inst.ClipDepth = tag.ClipDepth;
				if ( tag.HasVisible ) inst.Visible = tag.Visible;
				if ( tag.HasMatrix ) inst.Matrix = tag.Matrix;
				if ( tag.HasBlendMode ) inst.BlendMode = tag.BlendMode;
				if ( tag.HasColorTransform ) inst.ColorTransform = tag.ColorTransform;
			}
		}

		static SwfDisplayInstance CreateDisplayInstanceFromDefine(SwfLibraryDefine def)
		{
			return def switch
			{
				SwfLibraryShapeDefine => new SwfDisplayShapeInstance(),
				SwfLibraryBitmapDefine => new SwfDisplayBitmapInstance(),
				SwfLibrarySpriteDefine => new SwfDisplaySpriteInstance(),
				_ => throw new System.Exception($"Unknown define type: {def}")
			};
		}

		public void Visit(RemoveObjectTag tag, SwfDisplayList dl) => dl.Instances.Remove(tag.Depth);
		public void Visit(RemoveObject2Tag tag, SwfDisplayList dl) => dl.Instances.Remove(tag.Depth);

		public void Visit(FrameLabelTag tag, SwfDisplayList dl) {
			const string anchor_prefix = "FT_ANCHOR:";
			if ( tag.Name.StartsWith(anchor_prefix) ) {
				dl.FrameAnchors.Add(tag.Name.Remove(0, anchor_prefix.Length).Trim());
			} else if ( tag.AnchorFlag == 0 ) {
				dl.FrameLabels.Add(tag.Name.Trim());
			} else {
				dl.FrameAnchors.Add(tag.Name.Trim());
			}
		}

		public void Visit(ExportAssetsTag tag, SwfDisplayList dl) {
			foreach ( var asset_tag in tag.AssetTags )
				Library[asset_tag.Tag].ExportName = asset_tag.Name.Trim();
		}

		public void Visit(SymbolClassTag tag, SwfDisplayList dl) {
			foreach ( var symbol_tag in tag.SymbolTags )
				Library[symbol_tag.Tag].ExportName = symbol_tag.Name.Trim();
		}

		public void Visit(DefineShapeTag tag, SwfDisplayList dl) => AddShapesToLibrary(tag.ShapeId, tag.Shapes);
		public void Visit(DefineShape2Tag tag, SwfDisplayList dl) => AddShapesToLibrary(tag.ShapeId, tag.Shapes);
		public void Visit(DefineShape3Tag tag, SwfDisplayList dl) => AddShapesToLibrary(tag.ShapeId, tag.Shapes);
		public void Visit(DefineShape4Tag tag, SwfDisplayList dl) => AddShapesToLibrary(tag.ShapeId, tag.Shapes);
		public void Visit(DefineBitsLosslessTag tag, SwfDisplayList dl) => AddBitmapToLibrary(tag.CharacterId, tag);
		public void Visit(DefineBitsLossless2Tag tag, SwfDisplayList dl) => AddBitmapToLibrary(tag.CharacterId, tag);

		public void Visit(DefineSpriteTag tag, SwfDisplayList dl) => Library.Add(tag.SpriteId, new SwfLibrarySpriteDefine(tag.ControlTags));

		//
		//
		//

		void AddShapesToLibrary(ushort define_id, SwfShapesWithStyle shapes) {
			var bitmap_styles = shapes.FillStyles.Where(p => p.Type.IsBitmapType);
			var define = new SwfLibraryShapeDefine(bitmap_styles.Select(p => (p.BitmapId, p.BitmapMatrix)).ToArray());
			Library.Add(define_id, define);
		}

		void AddBitmapToLibrary(ushort define_id, IBitmapData bitmapData) {
			var define = new SwfLibraryBitmapDefine(bitmapData);
			Library.Add(define_id, define);
		}

		void ChildrenNextFrameLooped(SwfDisplayList dl) {
			var sprites = dl.Instances.Values
				.OfType<SwfDisplaySpriteInstance>();
			foreach ( var sprite in sprites ) {
				var sprite_def = Library.GetSpriteDefine(sprite.Id);
				if ( IsSpriteTimelineEnd(sprite, Library) ) sprite.Reset();
				var sprite_executer = new SwfContextExecuter(Library, sprite.CurrentTag);
				sprite_executer.NextFrame(sprite_def.ControlTags, sprite.DisplayList);
				sprite.CurrentTag = sprite_executer.CurrentTag;
			}
			return;

			static bool IsSpriteTimelineEnd(SwfDisplaySpriteInstance sprite, SwfLibrary library) {
				var sprite_def = library.GetSpriteDefine(sprite.Id);
				if ( sprite.CurrentTag < sprite_def.ControlTags.Length )
					return false;

				return sprite.DisplayList.Instances.Values
					.OfType<SwfDisplaySpriteInstance>()
					.All(x => IsSpriteTimelineEnd(x, library));
			}
		}
	}
}