#region Header
// BlockData.cs
// Author: James LaFritz
// Description: Serializable data container for a block type used by the world generator.
//              optional prefab or optional per-face UV picks.
#endregion

using Unity.Burst;
using UnityEngine;

namespace UnityCraft
{
    /// <summary>
    /// Serializable data for a block type used by the world generator.
    /// For Milestone 1, a block is defined by a friendly <see cref="Name"/> and its <see cref="Prefab"/>.
    /// For Mile Stone 2 we need to add per-face UV picks for the block.
    /// Future milestones may extend this with materials, health, sounds, mining level, etc.
    /// </summary>
    [System.Serializable]
    [BurstCompile]
    public struct BlockData
    {
        #region Fields

        /// <summary>
        /// Friendly display name for this block type (e.g., "Grass", "Dirt", "Stone").
        /// </summary>
        [SerializeField] private string _name;

        /// <summary>
        /// Prefab GameObject representing this block (typically a 1×1×1 cube with a material applied).
        /// </summary>
        [Tooltip("Optional prefab GameObject representing this block (typically a 1×1×1 cube with a material applied). Need if instancing prefab in game (Milestone 1).")]
        [SerializeField] private GameObject _prefab;
        
        [Tooltip("Optional per-face atlas selections for this block (front/left/back/right/top/bottom). Need if generating mesh in game (Milestone 2).")]
        [SerializeField] private BlockUVs _uvs;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the friendly display name for this block type.
        /// </summary>
        public string Name => _name;

        /// <summary>
        /// Gets the prefab used to instantiate this block in the scene.
        /// </summary>
        public GameObject Prefab => _prefab;

        /// <summary>
        /// Retrieves the texture atlas coordinates for the block,
        /// allowing optional per-face UV mapping (front, left, back, right, top, bottom).
        /// This data is crucial for generating detailed block meshes in-game.
        /// </summary>
        public BlockUVs UVs => _uvs;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new <see cref="BlockData"/> instance.
        /// </summary>
        /// <param name="name">Display name of the block.</param>
        /// <param name="prefab">Prefab GameObject for this block.</param>
        /// <param name="uvs">Per-face atlas selections for this block</param>
        public BlockData(string name, GameObject prefab, BlockUVs uvs)
        {
            _name = name;
            _prefab = prefab;
            _uvs = uvs;
        }

        #endregion
    }
}