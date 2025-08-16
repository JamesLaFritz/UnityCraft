#region Header
// BlockData.cs
// Author: James LaFritz
// Description: Serializable data container for a block type used by the world generator.
//              Kept intentionally small for Milestone 1. We'll expand as needed in later milestones.
#endregion

using UnityEngine;

namespace UnityCraft
{
    /// <summary>
    /// Serializable data for a block type used by the world generator.
    /// For Milestone 1, a block is defined by a friendly <see cref="Name"/> and its <see cref="Prefab"/>.
    /// Future milestones may extend this with materials, health, sounds, mining level, etc.
    /// </summary>
    [System.Serializable]
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
        [SerializeField] private GameObject _prefab;

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

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new <see cref="BlockData"/> instance.
        /// </summary>
        /// <param name="name">Display name of the block.</param>
        /// <param name="prefab">Prefab GameObject for this block.</param>
        public BlockData(string name, GameObject prefab)
        {
            _name = name;
            _prefab = prefab;
        }

        #endregion
    }
}