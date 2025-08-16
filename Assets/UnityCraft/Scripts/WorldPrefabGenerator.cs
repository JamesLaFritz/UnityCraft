#region Header
// WorldPrefabGenerator.cs
// Author: James LaFritz
// Description: Baseline prototype world using instantiated block prefabs placed via a heightmap.
#endregion

using CoreFramework.Random;
using Unity.Mathematics;
using UnityEngine;

namespace UnityCraft
{
    /// <summary>
    /// Generates a small voxel terrain by instantiating a block prefab at each grid column
    /// up to a height computed from Perlin noise. This is the slow, baseline approach.
    /// </summary>
    public class WorldPrefabGenerator : MonoBehaviour
    {
        #region Fields

        /// <summary>
        /// Seed for deterministic height noise. Changing this changes the terrain layout.
        /// </summary>
        [Tooltip("Seed for deterministic height noise. Changing this changes the terrain layout.")]
        [SerializeField] private uint _seed = 12345;

        /// <summary>
        /// Size of the world to build. 
        /// X = half-width in blocks (world spans -X..X),
        /// Y = vertical span above MinHeight (so MaxHeight = MinHeight + Y),
        /// Z = half-length in blocks (world spans -Z..Z).
        /// ⚠ Too large will drop FPS. (~60FPS at 10x10x10, ~8FPS at 30x40x30).
        /// </summary>
        [Tooltip("World size in blocks. X = half-width (world spans -X..X), " +
                 "Y = vertical span above MinHeight (MaxHeight = MinHeight + Y), " +
                 "Z = half-length (world spans -Z..Z).\n" +
                 "⚠ Too large will drop FPS. (~60FPS at 10x10x10, ~8FPS at 30x40x30).")]
        [SerializeField] private Vector3Int _buildSize = new Vector3Int(32, 380, 32);

        /// <summary>
        /// Minimum Y (inclusive) to start building from. Surface height will be >= this value.
        /// Default: -10
        /// </summary>
        [Tooltip("Minimum Y (inclusive) to start building from. Default: -64")]
        [SerializeField] private int _minHeight = -64;

        // <summary>
        /// Y (inclusive) to start using the SubsurfaceBlock block.
        /// Default: -62
        /// </summary>
        [Tooltip("Y (inclusive) to start using the SubsurfaceBlock block. Default: -62")]
        [SerializeField] private int _bottomLayerHeight = -62;

        /// <summary>
        /// Noise frequency used for height sampling. Larger values = more frequent variation.
        /// </summary>
        [Tooltip("Perlin noise frequency for heightmap. Larger values = more frequent variation.")]
        [Min(0.0001f)]
        [SerializeField] private float _noiseFrequency = 0.05f;

        /// <summary>
        /// Surface block placed at the noise-determined surface height (e.g., Grass).
        /// </summary>
        [Header("Blocks")]
        [Tooltip("Surface block placed at the top surface height.")]
        [SerializeField] private BlockData _surfaceBlock;

        /// <summary>
        /// Subsurface block used to fill from BottomLayerHeight up to (surfaceHeight - 1), e.g., Dirt/Stone.
        /// </summary>
        [Tooltip("Block used to fill beneath the surface down to BottomLayerHeight (e.g., Dirt/Stone).")]
        [SerializeField] private BlockData _subsurfaceBlock;

        /// <summary>
        /// Block used to fill from MinHeight to BottomLayerHeight (e.g., Stone).
        /// </summary>
        [Tooltip("Block used to fill from MinHeight to BottomLayerHeight (e.g., Stone).")]
        [SerializeField] private BlockData _bottomSubsurfaceBlock;

        /// <summary>
        /// Parent container to keep the hierarchy tidy (created at runtime if missing).
        /// </summary>
        [Header("Hierarchy")]
        [Tooltip("Optional parent transform for spawned blocks. Created at runtime if null.")]
        [SerializeField] private Transform _blocksParent;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the maximum height (inclusive) we are allowed to build up to.
        /// Calculated as MinHeight + BuildSize.y.
        /// </summary>
        private int MaxHeight => _minHeight + _buildSize.y;

        #endregion

        #region Unity Messages

        /// <summary>
        /// Ensures configuration is sane and optionally auto-generates on play.
        /// </summary>
        private void Start()
        {
            ValidateConfig();
            if (!_blocksParent)
            {
                var parent = new GameObject("Blocks").transform;
                parent.SetParent(transform, false);
                _blocksParent = parent;
            }

            // For Milestone 1 we generate immediately on play.
            GenerateWorld();
        }

        /// <summary>
        /// Draws a simple gizmo box indicating the world extents in the Scene view.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
#if UNITY_EDITOR
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.25f);

            // With MaxHeight = MinHeight + BuildSize.y, the vertical size is simply BuildSize.y + 1 blocks.
            var size = new Vector3(
                _buildSize.x * 2 + 1,
                (MaxHeight - _minHeight) + 1,         // == _buildSize.y + 1
                _buildSize.z * 2 + 1
            );

            var center = new Vector3(
                0f,
                _minHeight + (size.y - 1) * 0.5f,
                0f
            );

            Gizmos.DrawWireCube(transform.TransformPoint(center), size);
#endif
        }

        #endregion

        #region Public API

        /// <summary>
        /// Destroys all previously spawned blocks and rebuilds the world using current settings.
        /// </summary>
        [ContextMenu("Generate World")]
        public void GenerateWorld()
        {
            if (!_surfaceBlock.Prefab || !_subsurfaceBlock.Prefab || !_bottomSubsurfaceBlock.Prefab)
            {
                Debug.LogWarning("[World] Missing block prefabs. Assign Surface/Subsurface/BottomSubsurface in the inspector.");
                return;
            }

            ClearWorld();

            // Clamp ranges
            var yMin = math.min(_minHeight, MaxHeight);
            var yMax = math.max(_minHeight, MaxHeight);
            var heightRange = math.max(1, yMax - yMin); // exclusive mapping

            for (var z = -_buildSize.z; z <= _buildSize.z; z++)
            for (var x = -_buildSize.x; x <= _buildSize.x; x++)
            {
                // --- Surface Height from 2D Noise ---
                // Using Squirrel Perlin noise (deterministic with seed).
                var n = SquirrelNoise32Bit.Perlin(x * _noiseFrequency, z * _noiseFrequency, _seed);

                var surfaceY = (int)(yMin + math.round(n * heightRange));
                // Clamp surface to be at least bottom-layer height (prevents tiny columns dipping below the bottom fill band)
                surfaceY = math.clamp(surfaceY, _bottomLayerHeight, yMax);

                // --- Fill from MinHeight..(surfaceY-1) ---
                for (var y = yMin; y < surfaceY; y++)
                {
                    var blockToSpawn = y < _bottomLayerHeight ? _bottomSubsurfaceBlock : _subsurfaceBlock;
                    SpawnBlock(blockToSpawn, new Vector3Int(x, y, z));
                }

                // --- Place surface block at surfaceY ---
                SpawnBlock(_surfaceBlock, new Vector3Int(x, surfaceY, z));
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Deletes all spawned block children under <see cref="_blocksParent"/>.
        /// </summary>
        [ContextMenu("Clear World")]
        public void ClearWorld()
        {
            if (!_blocksParent) return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                for (var i = _blocksParent.childCount - 1; i >= 0; i--)
                {
                    UnityEditor.Undo.DestroyObjectImmediate(_blocksParent.GetChild(i).gameObject);
                }
                return;
            }
#endif
            for (var i = _blocksParent.childCount - 1; i >= 0; i--)
            {
                Destroy(_blocksParent.GetChild(i).gameObject);
            }
        }

        /// <summary>
        /// Spawns the given block prefab at the provided grid position.
        /// </summary>
        private void SpawnBlock(in BlockData block, in Vector3Int gridPos)
        {
            var prefab = block.Prefab;
            if (!prefab) return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                var go = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, _blocksParent);
                go.name = $"{block.Name}_{gridPos.x}_{gridPos.y}_{gridPos.z}";
                go.transform.localPosition = gridPos;
                return;
            }
#endif
            var instance = Instantiate(prefab, _blocksParent);
            instance.name = $"{block.Name}_{gridPos.x}_{gridPos.y}_{gridPos.z}";
            instance.transform.localPosition = gridPos;
        }

        /// <summary>
        /// Validates core configuration to prevent out-of-range builds or null refs.
        /// </summary>
        private void ValidateConfig()
        {
            // Ensure sensible sizes; Y is a span, must be >= 1
            _buildSize.x = Mathf.Max(1, _buildSize.x);
            _buildSize.y = Mathf.Max(1, _buildSize.y);
            _buildSize.z = Mathf.Max(1, _buildSize.z);

            // Keep bottom layer within [MinHeight .. MaxHeight]
            var maxHeight = MaxHeight;
            if (_bottomLayerHeight < _minHeight) _bottomLayerHeight = _minHeight + 1;
            if (_bottomLayerHeight > maxHeight)  _bottomLayerHeight = maxHeight;

            if (!_blocksParent && transform.childCount > 0)
            {
                _blocksParent = transform;
            }
        }

        #endregion
    }
}