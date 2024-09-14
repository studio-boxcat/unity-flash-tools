using System.Linq;
using System.Collections.Generic;
using FTSwfTools.SwfTags;
using FTSwfTools.SwfTypes;
using UnityEngine;

namespace FTSwfTools {
	class SwfContextExecuter : SwfTagVisitor<SwfDisplayList, SwfDisplayList> {
		public SwfLibrary            Library    = null;
		public int                   CurrentTag = 0;

		public SwfContextExecuter(SwfLibrary library, int current_tag) {
			Library    = library;
			CurrentTag = current_tag;
		}

		public bool NextFrame(SwfTagBase[] tags, SwfDisplayList dl) {
			dl.FrameLabels.Clear();
			dl.FrameAnchors.Clear();
			while ( CurrentTag < tags.Length ) {
				var tag = tags[CurrentTag++];
				tag.AcceptVisitor(this, dl);
				if ( tag.TagType == SwfTagType.ShowFrame ) {
					ChildrenNextFrameLooped(dl);
					return true;
				}
			}
			ChildrenNextFrameLooped(dl);
			return false;
		}

		public SwfDisplayList Visit(PlaceObjectTag tag, SwfDisplayList dl) {
			if (Library.TryGet(tag.CharacterId, out var define) is false)
				return dl;

			SwfDisplayInstance new_inst = define switch
			{
				SwfLibraryShapeDefine => new SwfDisplayShapeInstance(),
				SwfLibraryBitmapDefine => new SwfDisplayBitmapInstance(),
				SwfLibrarySpriteDefine => new SwfDisplaySpriteInstance(),
				_ => null
			};

			if ( new_inst != null ) {
				new_inst.Id             = tag.CharacterId;
				new_inst.Depth          = tag.Depth;
				new_inst.ClipDepth      = 0;
				new_inst.Visible        = true;
				new_inst.Matrix         = tag.Matrix;
				new_inst.BlendMode      = SwfBlendMode.identity;
				new_inst.ColorTransform = tag.ColorTransform;
				dl.Instances.Add(new_inst.Depth, new_inst);
			}
			return dl;
		}

		public SwfDisplayList Visit(PlaceObject2Tag tag, SwfDisplayList dl) {
			var is_shape  = tag.HasCharacter && Library.HasDefine<SwfLibraryShapeDefine >(tag.CharacterId);
			var is_bitmap = tag.HasCharacter && Library.HasDefine<SwfLibraryBitmapDefine>(tag.CharacterId);
			var is_sprite = tag.HasCharacter && Library.HasDefine<SwfLibrarySpriteDefine>(tag.CharacterId);
			if ( tag.HasCharacter ) {
				SwfDisplayInstance old_inst = null;
				if ( tag.Move ) { // replace character
					dl.Instances.Remove(tag.Depth, out old_inst);
				}
				// new character
				SwfDisplayInstance new_inst = null;
				if ( is_shape ) {
					new_inst = new SwfDisplayShapeInstance();
				} else if ( is_bitmap ) {
					new_inst = new SwfDisplayBitmapInstance();
				} else if ( is_sprite ) {
					new_inst = new SwfDisplaySpriteInstance();
				}
				if ( new_inst != null ) {
					new_inst.Id             = tag.CharacterId;
					new_inst.Depth          = tag.Depth;
					new_inst.ClipDepth      = tag.HasClipDepth      ? tag.ClipDepth      : (old_inst?.ClipDepth ?? 0);
					new_inst.Visible        = true;
					new_inst.Matrix         = tag.HasMatrix         ? tag.Matrix         : (old_inst?.Matrix ?? Matrix4x4.identity);
					new_inst.BlendMode      = SwfBlendMode.identity;
					new_inst.ColorTransform = tag.HasColorTransform ? tag.ColorTransform : (old_inst?.ColorTransform ?? default);
					dl.Instances.Add(new_inst.Depth, new_inst);
				}
			} else if ( tag.Move ) { // move character
				if ( dl.Instances.TryGetValue(tag.Depth, out var inst) ) {
					if ( tag.HasClipDepth ) inst.ClipDepth = tag.ClipDepth;
					if ( tag.HasMatrix ) inst.Matrix = tag.Matrix;
					if ( tag.HasColorTransform ) inst.ColorTransform = tag.ColorTransform;
				}
			}
			return dl;
		}

		public SwfDisplayList Visit(PlaceObject3Tag tag, SwfDisplayList dl) {
			var is_shape  = tag.HasCharacter && Library.HasDefine<SwfLibraryShapeDefine >(tag.CharacterId);
			var is_bitmap = tag.HasCharacter && Library.HasDefine<SwfLibraryBitmapDefine>(tag.CharacterId);
			var is_sprite = tag.HasCharacter && Library.HasDefine<SwfLibrarySpriteDefine>(tag.CharacterId);
			if ( tag.HasCharacter ) {
				SwfDisplayInstance old_inst = null;
				if ( tag.Move ) { // replace character
					dl.Instances.Remove(tag.Depth, out old_inst);
				}
				// new character
				SwfDisplayInstance new_inst = null;
				if ( is_shape ) {
					new_inst = new SwfDisplayShapeInstance();
				} else if ( is_bitmap ) {
					new_inst = new SwfDisplayBitmapInstance();
				} else if ( is_sprite ) {
					new_inst = new SwfDisplaySpriteInstance();
				}
				if ( new_inst != null ) {
					new_inst.Id             = tag.CharacterId;
					new_inst.Depth          = tag.Depth;
					new_inst.ClipDepth      = tag.HasClipDepth      ? tag.ClipDepth      : (old_inst != null ? old_inst.ClipDepth      : (ushort)0);
					new_inst.Visible        = tag.HasVisible        ? tag.Visible        : (old_inst != null ? old_inst.Visible        : true);
					new_inst.Matrix         = tag.HasMatrix         ? tag.Matrix         : (old_inst != null ? old_inst.Matrix         : Matrix4x4.identity);
					new_inst.BlendMode      = tag.HasBlendMode      ? tag.BlendMode      : (old_inst != null ? old_inst.BlendMode      : SwfBlendMode.identity);
					new_inst.ColorTransform = tag.HasColorTransform ? tag.ColorTransform : (old_inst != null ? old_inst.ColorTransform : default);
					dl.Instances.Add(new_inst.Depth, new_inst);
				}
			} else if ( tag.Move ) { // move character
				if ( dl.Instances.TryGetValue(tag.Depth, out var inst) ) {
					if ( tag.HasClipDepth ) inst.ClipDepth = tag.ClipDepth;
					if ( tag.HasVisible ) inst.Visible = tag.Visible;
					if ( tag.HasMatrix ) inst.Matrix = tag.Matrix;
					if ( tag.HasBlendMode ) inst.BlendMode = tag.BlendMode;
					if ( tag.HasColorTransform ) inst.ColorTransform = tag.ColorTransform;
				}
			}
			return dl;
		}

		public SwfDisplayList Visit(RemoveObjectTag tag, SwfDisplayList dl) {
			dl.Instances.Remove(tag.Depth);
			return dl;
		}

		public SwfDisplayList Visit(RemoveObject2Tag tag, SwfDisplayList dl) {
			dl.Instances.Remove(tag.Depth);
			return dl;
		}

		public SwfDisplayList Visit(ShowFrameTag tag, SwfDisplayList dl) {
			return dl;
		}

		public SwfDisplayList Visit(SetBackgroundColorTag tag, SwfDisplayList dl) {
			return dl;
		}

		public SwfDisplayList Visit(FrameLabelTag tag, SwfDisplayList dl) {
			const string anchor_prefix = "FT_ANCHOR:";
			if ( tag.Name.StartsWith(anchor_prefix) ) {
				dl.FrameAnchors.Add(tag.Name.Remove(0, anchor_prefix.Length).Trim());
			} else if ( tag.AnchorFlag == 0 ) {
				dl.FrameLabels.Add(tag.Name.Trim());
			} else {
				dl.FrameAnchors.Add(tag.Name.Trim());
			}
			return dl;
		}

		public SwfDisplayList Visit(ProtectTag tag, SwfDisplayList dl) => dl;

		public SwfDisplayList Visit(EndTag tag, SwfDisplayList dl) => dl;

		public SwfDisplayList Visit(ExportAssetsTag tag, SwfDisplayList dl) {
			foreach ( var asset_tag in tag.AssetTags ) {
				if ( Library.TryGet(asset_tag.Tag, out var define) )
					define.ExportName = asset_tag.Name.Trim();
			}
			return dl;
		}

		public SwfDisplayList Visit(EnableDebuggerTag tag, SwfDisplayList dl) => dl;

		public SwfDisplayList Visit(EnableDebugger2Tag tag, SwfDisplayList dl) => dl;

		public SwfDisplayList Visit(ScriptLimitsTag tag, SwfDisplayList dl) => dl;

		public SwfDisplayList Visit(SymbolClassTag tag, SwfDisplayList dl) {
			foreach ( var symbol_tag in tag.SymbolTags ) {
				var define = Library.FindDefine<SwfLibraryDefine>(symbol_tag.Tag);
				if ( define != null ) define.ExportName = symbol_tag.Name.Trim();
			}
			return dl;
		}

		public SwfDisplayList Visit(MetadataTag tag, SwfDisplayList dl) => dl;

		public SwfDisplayList Visit(DefineSceneAndFrameLabelDataTag tag, SwfDisplayList dl) => dl;

		public SwfDisplayList Visit(DoABCTag tag, SwfDisplayList dl) => dl;

		public SwfDisplayList Visit(DefineShapeTag tag, SwfDisplayList dl) {
			AddShapesToLibrary(tag.ShapeId, tag.Shapes);
			return dl;
		}

		public SwfDisplayList Visit(DefineShape2Tag tag, SwfDisplayList dl) {
			AddShapesToLibrary(tag.ShapeId, tag.Shapes);
			return dl;
		}

		public SwfDisplayList Visit(DefineShape3Tag tag, SwfDisplayList dl) {
			AddShapesToLibrary(tag.ShapeId, tag.Shapes);
			return dl;
		}

		public SwfDisplayList Visit(DefineShape4Tag tag, SwfDisplayList dl) {
			AddShapesToLibrary(tag.ShapeId, tag.Shapes);
			return dl;
		}

		public SwfDisplayList Visit(DefineBitsLosslessTag tag, SwfDisplayList dl) {
			AddBitmapToLibrary(tag.CharacterId, tag);
			return dl;
		}

		public SwfDisplayList Visit(DefineBitsLossless2Tag tag, SwfDisplayList dl) {
			AddBitmapToLibrary(tag.CharacterId, tag);
			return dl;
		}

		public SwfDisplayList Visit(DefineSpriteTag tag, SwfDisplayList dl) {
			var define = new SwfLibrarySpriteDefine(tag.ControlTags);
			Library.Defines.Add(tag.SpriteId, define);
			return dl;
		}

		public SwfDisplayList Visit(FileAttributesTag tag, SwfDisplayList dl) => dl;

		public SwfDisplayList Visit(EnableTelemetryTag tag, SwfDisplayList dl) => dl;

		public SwfDisplayList Visit(DefineBinaryDataTag tag, SwfDisplayList dl) => dl;

		public SwfDisplayList Visit(UnknownTag tag, SwfDisplayList dl) {
			L.W(tag.ToString());
			return dl;
		}

		public SwfDisplayList Visit(UnsupportedTag tag, SwfDisplayList dl) {
			L.W(tag.ToString());
			return dl;
		}

		//
		//
		//

		void AddShapesToLibrary(ushort define_id, SwfShapesWithStyle shapes) {
			var bitmap_styles = shapes.FillStyles.Where(p => p.Type.IsBitmapType);
			var define = new SwfLibraryShapeDefine(bitmap_styles.Select(p => (p.BitmapId, p.BitmapMatrix)).ToArray());
			Library.Defines.Add(define_id, define);
		}

		void AddBitmapToLibrary(ushort define_id, IBitmapData bitmapData) {
			var define = new SwfLibraryBitmapDefine(bitmapData);
			Library.Defines.Add(define_id, define);
		}

		bool IsSpriteTimelineEnd(SwfDisplaySpriteInstance sprite) {
			var sprite_def = Library.GetSpriteDefine(sprite.Id);
			if ( sprite.CurrentTag < sprite_def.ControlTags.Length ) {
				return false;
			}
			var children = sprite.DisplayList.Instances.Values
				.Where (p => p.Type == SwfDisplayInstanceType.Sprite)
				.Select(p => p as SwfDisplaySpriteInstance);
			foreach ( var child in children ) {
				if ( !IsSpriteTimelineEnd(child) ) {
					return false;
				}
			}
			return true;
		}

		void ChildrenNextFrameLooped(SwfDisplayList dl) {
			var sprites = dl.Instances.Values
				.Where (p => p.Type == SwfDisplayInstanceType.Sprite)
				.Select(p => p as SwfDisplaySpriteInstance);
			foreach ( var sprite in sprites ) {
				var sprite_def = Library.GetSpriteDefine(sprite.Id);
				if ( IsSpriteTimelineEnd(sprite) ) sprite.Reset();
				var sprite_executer = new SwfContextExecuter(Library, sprite.CurrentTag);
				sprite_executer.NextFrame(sprite_def.ControlTags, sprite.DisplayList);
				sprite.CurrentTag = sprite_executer.CurrentTag;
			}
		}
	}
}