using System.Linq;
using System.Collections.Generic;
using FT.SwfTags;

namespace FT {
	internal class SwfContextExecuter {
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

		private static void Visit(SwfContextExecuter executer, SwfTagBase tag, SwfDisplayList displayList)
		{
			switch (tag)
			{
			case PlaceObjectTag t: executer.ApplyPlaceObjectTag(t, displayList); break;
			case PlaceObject2Tag t: executer.ApplyPlaceObjectTag(t, displayList); break;
			case PlaceObject3Tag t: executer.ApplyPlaceObjectTag(t, displayList); break;

			case RemoveObjectTagBase t: displayList.Instances.Remove(t.Depth); break;

			case FrameLabelTag t: executer.Visit(t, displayList); break;

			case NameTag t:
			{
				foreach ( var n in t.Names )
					executer.Library[n.Tag].ExportName = n.Name;
				break;
			}

			case DefineShapeTagBase t:
			{
				var bitmap_styles = t.Shapes.FillStyles.Where(p => p.Type.IsBitmapType());
				var define = new SwfLibraryShapeDefine(bitmap_styles.Select(p => (p.BitmapId, p.BitmapMatrix)).ToArray());
				executer.Library.Add(t.ShapeId, define);
				break;
			}

			case IDefineBitsLosslessTag t:
			{
				var define = new SwfLibraryBitmapDefine(t);
				executer.Library.Add(t.CharacterId, define);
				break;
			}

			case DefineSpriteTag t:
            {
				executer.Library.Add(t.SpriteId, new SwfLibrarySpriteDefine(t.ControlTags));
				break;
			}

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
				// Do nothing
				break;

			case UnknownTag:
			case UnsupportedTag:
				L.W(tag.ToString());
				break;

			}
		}

		private void ApplyPlaceObjectTag(PlaceObjectTag tag, SwfDisplayList dl) {
			var new_inst = CreateDisplayInstanceFromDefine(Library[tag.CharacterId]);
			new_inst.Id             = tag.CharacterId;
			new_inst.Depth          = tag.Depth;
			new_inst.ClipDepth      = 0;
			new_inst.Visible        = true;
			new_inst.Matrix         = tag.Matrix;
			new_inst.BlendMode      = SwfBlendMode.Normal;
			new_inst.ColorTransform = tag.ColorTransform;
			dl.Instances.Add(new_inst.Depth, new_inst);
		}

		private void ApplyPlaceObjectTag(PlaceObject2Tag tag, SwfDisplayList dl) {
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
				new_inst.Matrix         = tag.HasMatrix         ? tag.Matrix         : (old_inst?.Matrix ?? SwfMatrix.Identity);
				new_inst.BlendMode      = SwfBlendMode.Normal;
				new_inst.ColorTransform = tag.HasColorTransform ? tag.ColorTransform : (old_inst?.ColorTransform ?? default);
				dl.Instances.Add(new_inst.Depth, new_inst);
			} else if ( tag.Move ) { // move character
				var inst = dl.Instances[tag.Depth];
				if ( tag.HasClipDepth ) inst.ClipDepth = tag.ClipDepth;
				if ( tag.HasMatrix ) inst.Matrix = tag.Matrix;
				if ( tag.HasColorTransform ) inst.ColorTransform = tag.ColorTransform;
			}
		}

		private void ApplyPlaceObjectTag(PlaceObject3Tag tag, SwfDisplayList dl) {
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
				new_inst.Matrix         = tag.HasMatrix         ? tag.Matrix         : old_inst?.Matrix ?? SwfMatrix.Identity;
				new_inst.BlendMode      = tag.HasBlendMode      ? tag.BlendMode      : old_inst?.BlendMode ?? SwfBlendMode.Normal;
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

		private static SwfDisplayInstance CreateDisplayInstanceFromDefine(SwfLibraryDefine def)
		{
			return def switch
			{
				SwfLibraryShapeDefine => new SwfDisplayShapeInstance(),
				SwfLibraryBitmapDefine => new SwfDisplayBitmapInstance(),
				SwfLibrarySpriteDefine => new SwfDisplaySpriteInstance(),
				_ => throw new System.Exception($"Unknown define type: {def}")
			};
		}

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

		//
		//
		//

		private void ChildrenNextFrameLooped(SwfDisplayList dl) {
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