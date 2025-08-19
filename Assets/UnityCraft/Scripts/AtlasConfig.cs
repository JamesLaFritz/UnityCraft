#region Header
// AtlasInfo.cs
// Author: James LaFritz
// Description: 
#endregion

using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

namespace UnityCraft
{
    
    /// <summary>
    /// Describes the atlas grid and converts tile (row,col) to UVs in Unity's bottom-left UV space.
    /// </summary>
    [System.Serializable, BurstCompile]
    public struct AtlasConfig
    {
        #region Fields
        [Tooltip("Total rows in the atlas texture (top-left origin).")]
        [SerializeField, Min(1)] private int _rows;
        [Tooltip("Total columns in the atlas texture (top-left origin).")]
        [SerializeField, Min(1)] private int _columns;
        #endregion

        #region Properties

        /// <summary>
        /// Gets the total number of rows in the atlas grid.
        /// </summary>
        /// <remarks>
        /// This property represents the count of rows defined in the atlas, where the origin is at the top-left.
        /// The value is initialized through the serialized field and reflects the defined grid layout.
        /// It must have a minimum value of 1.
        /// </remarks>
        public int Rows => _rows;

        /// <summary>
        /// Gets the total number of columns in the atlas grid.
        /// </summary>
        /// <remarks>
        /// This property represents the count of columns defined in the atlas, where the origin is at the top-left.
        /// The value is initialized through the serialized field and corresponds to the defined grid layout.
        /// It must have a minimum value of 1.
        /// </remarks>
        public int Columns => _columns;

        #endregion

        /// <summary>
        /// Represents the configuration for a texture atlas in UnityCraft.
        /// </summary>
        public AtlasConfig(int rows, int cols)
        {
            _rows = math.max(rows, 1);
            _columns = math.max(cols, 1);
        }

        /// <summary>
        /// Calculates the UV rectangle for a given face on the atlas, converting from the atlas grid to Unity's bottom-left UV space.
        /// </summary>
        /// <param name="face">The data representing the row and column of the face on the atlas grid.</param>
        /// <param name="uMin">The minimum U texture coordinate (horizontal start position).</param>
        /// <param name="vMin">The minimum V texture coordinate (vertical start position).</param>
        /// <param name="uMax">The maximum U texture coordinate (horizontal end position).</param>
        /// <param name="vMax">The maximum V texture coordinate (vertical end position).</param>
        public void ToUVRect(FaceData face, out float uMin, out float vMin, out float uMax, out float vMax)
        {
            // Flip artist row to Unity's bottom-left space
            var unityRow = (_rows - 1) - Mathf.Clamp(face.row, 0, _rows - 1);
            var col = Mathf.Clamp(face.col, 0, _columns - 1);
            
            uMin = (float)col / _columns;
            uMax = (float)(col + 1) / _columns;
            vMin = (float)unityRow  / _rows;
            vMax = (float)(unityRow  + 1) / _rows;
        }

        /// <summary>
        /// Computes the UV mapping for a given row and column in the texture atlas.
        /// </summary>
        /// <param name="row">The row index in the texture atlas.</param>
        /// <param name="col">The column index in the texture atlas.</param>
        /// <returns>A <see cref="FaceData"/> instance representing the UV mapping for the specified row and column.</returns>
        public static FaceData UV(int row, int col) => new(row, col);
    }
}