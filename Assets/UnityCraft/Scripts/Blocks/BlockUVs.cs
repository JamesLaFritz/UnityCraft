#region Header
// BlockUVs.cs
// Author: James LaFritz
// Description: 
#endregion

using Unity.Burst;

namespace UnityCraft
{
    /// <summary>
    /// A full set of per-face atlas picks for one block.
    /// </summary>
    [System.Serializable, BurstCompile]
    public struct BlockUVs
    {
        /// <summary>
        /// Defines the texture or atlas information for the front face of a block.
        /// </summary>
        /// <remarks>
        /// This property specifies the appearance and texture mapping for the front-facing side of a voxel block.
        /// It is a key part of the <see cref="BlockUVs"/> struct, which allows configuration of textures per block face.
        /// </remarks>
        public FaceData front;

        /// <summary>
        /// Defines the texture or atlas information for the left face of a block.
        /// </summary>
        /// <remarks>
        /// This property specifies the appearance and texture mapping for the left-facing side of a voxel block.
        /// It is a key part of the <see cref="BlockUVs"/> struct, used to configure individual textures for each block face.
        /// </remarks>
        public FaceData left;

        /// <summary>
        /// Specifies the texture or atlas information for the back face of a block.
        /// </summary>
        /// <remarks>
        /// This property specifies the appearance and texture mapping for the back-facing side of a voxel block.
        /// It is a key part of the <see cref="BlockUVs"/> struct, used to configure individual textures for each block face.
        /// </remarks>
        public FaceData back;

        /// <summary>
        /// Defines the texture or atlas information for the right face of a block.
        /// </summary>
        /// <remarks>
        /// This property specifies the appearance and texture mapping for the right-facing side of a voxel block.
        /// It is a key part of the <see cref="BlockUVs"/> struct, which allows configuration of textures per block face.
        /// </remarks>
        public FaceData right;

        /// <summary>
        /// Specifies the texture or atlas data for the top face of a block.
        /// </summary>
        /// <remarks>
        /// This property determines the visual appearance and texture mapping for the top-facing side of a voxel block.
        /// It is a key part of the <see cref="BlockUVs"/> struct and facilitates texture configuration for the top surface of the block.
        /// </remarks>
        public FaceData top;

        /// <summary>
        /// Defines the texture or atlas information for the bottom face of a block.
        /// </summary>
        /// <remarks>
        /// This property determines the visual representation and texture mapping for the bottom-facing side of a voxel block.
        /// It is a key part of the <see cref="BlockUVs"/> struct, enabling the configuration of textures for each block face.
        /// </remarks>
        public FaceData bottom;

        /// <summary>Indexer to fetch the face UV by <see cref="Face"/>.</summary>
        public FaceData this[Face face] =>
            face switch
            {
                Face.Front  => front,
                Face.Left   => left,
                Face.Back   => back,
                Face.Right  => right,
                Face.Top    => top,
                Face.Bottom => bottom,
                _                => front
            };
    }
}