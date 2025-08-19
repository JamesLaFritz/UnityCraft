#region Header
// FaceData.cs
// Author: James LaFritz
// Description: Face enumeration and per-face atlas coordinates for blocks.
#endregion

using Unity.Burst;

namespace UnityCraft
{
    /// <summary>
    /// The six faces of a cube, used for UV selection and face-culling.
    /// </summary>
    public enum Face : byte { Front, Back, Left, Right, Top, Bottom }

    /// <summary>
    /// Represents the data for a specific face of a cube, including texture mapping information.
    /// </summary>
    /// <remarks>
    /// This struct is primarily used for defining texture coordinates for each face of a cube in a voxel-based rendering system.
    /// </remarks>
    [System.Serializable, BurstCompile]
    public struct FaceData
    {
        /// <summary>Atlas row (artist grid). Top row = 0 in the artist sheet.</summary>
        public int row;
        /// <summary>Atlas column (artist grid). Leftmost column = 0.</summary>
        public int col;

        /// <summary>
        /// Represents the data associated with a specific face of a cube, including texture mapping information.
        /// </summary>
        /// <remarks>
        /// This structure is commonly used in voxel-based rendering systems to define texture coordinates for individual cube faces.
        /// The <see cref="row"/> and <see cref="col"/> properties correspond to the specific position in an atlas.
        /// </remarks>
        public FaceData(int row, int col)
        {
            this.row = row;
            this.col = col;
        }
    }
}