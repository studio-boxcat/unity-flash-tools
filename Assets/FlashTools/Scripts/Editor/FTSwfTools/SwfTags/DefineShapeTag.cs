﻿using FTSwfTools.SwfTypes;
using UnityEngine;

namespace FTSwfTools.SwfTags {
	class DefineShapeTag : SwfTagBase {
		public ushort             ShapeId;
		public Rect               ShapeBounds;
		public SwfShapesWithStyle Shapes;

		public static DefineShapeTag Create(SwfStreamReader reader) {
			return new DefineShapeTag
			{
				ShapeId = reader.ReadUInt16(),
				ShapeBounds = SwfRect.Read(reader),
				Shapes = SwfShapesWithStyle.Read(reader, SwfShapesWithStyle.ShapeStyleType.Shape)
			};
		}
	}
}