﻿using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace AllowTool {
	/// <summary>
	/// Draws a selection overlay over filtered map cells. 
	/// Caches highlighted cell positions for better performance.
	/// </summary>
	public class MapCellHighlighter {
		private readonly List<Vector3> cachedHighlightQuadPositions = new List<Vector3>();
		private readonly Func<IEnumerable<IntVec3>> cellSelector;
		private readonly float recacheInterval;
		
		private Material dragHighlightMat;
		public Texture2D HighlightTexture {
			set { dragHighlightMat = MaterialPool.MatFrom(value, ShaderDatabase.MetaOverlay, Color.white); }
		}

		private float nextHighlightRecacheTime;

		public MapCellHighlighter(Func<IEnumerable<IntVec3>> cellSelector, float recacheInterval = .5f) {
			this.cellSelector = cellSelector ?? throw new ArgumentNullException(nameof(cellSelector));
			this.recacheInterval = recacheInterval;
		}

		public void ClearCachedCells() {
			nextHighlightRecacheTime = 0f;
			cachedHighlightQuadPositions.Clear();
		}

		public void DrawCellHighlights() {
			if (Time.time >= nextHighlightRecacheTime) {
				RecacheCellPositions();
			}
			DrawCachedCellHighlights();
		}

		private void RecacheCellPositions() {
			nextHighlightRecacheTime = Time.time + recacheInterval;
			cachedHighlightQuadPositions.Clear();
			var drawOnTopOffset = 10f * Vector3.up;
			foreach (var cell in cellSelector()) {
				cachedHighlightQuadPositions.Add(cell.ToVector3Shifted() + drawOnTopOffset);
			}
		}

		private void DrawCachedCellHighlights() {
			if (dragHighlightMat == null) return;
			for (var i = 0; i < cachedHighlightQuadPositions.Count; i++) {
				Graphics.DrawMesh(MeshPool.plane10, cachedHighlightQuadPositions[i], Quaternion.identity, dragHighlightMat, 0);	
			}
		}
	}
}