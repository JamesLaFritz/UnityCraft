#region Header
// WorldPrefabGenerator.cs
// Author: James LaFritz
// Description: Baseline prototype world using instantiated block prefabs placed via a heightmap.
#endregion

using System;
using CoreFramework.Random;
using Unity.Mathematics;
using UnityEngine;

namespace UnityCraft
{
    /// <summary>
    /// Generates a small voxel terrain by instantiating a block prefab at each grid column
    /// up to a height computed from Perlin noise. This is the slow, baseline approach.
    /// </summary>
    public class WorldGenerator : MonoBehaviour
    {
        /// <summary>
        /// Specifies the type of terrain generation approach for the world.
        /// </summary>
        private enum GenerateType
        {
            Prefab,
            Mesh,
        }
        
        #region Fields

        /// <summary>
        /// Defines the selected method for terrain generation within the world creation system.
        /// This variable determines whether terrain is generated using a prefab-based approach
        /// or through mesh-based generation, influencing performance and visual fidelity.
        /// </summary>
        /// <remarks>
        /// This field is serialized to allow for configuration in the Unity Inspector and directly
        /// affects how the terrain is constructed. Changes in this setting will impact the
        /// rendering process and may require regenerating the world to observe effects.
        /// </remarks>
        [SerializeField] private GenerateType _generateType = GenerateType.Prefab;

        /// <summary>
        /// Represents the configuration data used by the world generation system in UnityCraft.
        /// The variable holds an instance of the <see cref="WorldData"/> struct, which includes
        /// properties for defining the size, seed, noise parameters, and block data for procedural
        /// generation of a voxel-based world.
        /// </summary>
        /// <remarks>
        /// This data structure is serialized to allow modification directly in the Unity Editor. It
        /// contains essential parameters required to customize and generate a terrain grid using
        /// Perlin noise and other procedural techniques.
        /// </remarks>
        [SerializeField] private WorldData _worldData;

        /// <summary>
        /// Serves as the parent transform for all instantiated block objects in the voxel terrain.
        /// This transform organizes and contains block prefabs, ensuring the hierarchy remains
        /// clean and manageable within the Unity Editor during runtime and development.
        /// </summary>
        /// <remarks>
        /// If not set manually, this transform is automatically created as a child of the
        /// current object when the generator script initializes. It is used for optimizing
        /// cleanup and structural organization when blocks are generated or cleared.
        /// </remarks>
        [SerializeField] private Transform _blocksParent;

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
            
            // Uncomment to use Unity's built-in random number generator.
            //UnityEngine.Random.InitState((int)_seed);

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
            
            var buildSize = _worldData.BuildSize;
            var minHeight = _worldData.MinHeight;
            var maxHeight = _worldData.MaxHeight;

            // With MaxHeight = MinHeight + BuildSize.y, the vertical size is simply BuildSize.y + 1 blocks.
            var size = new Vector3(
                buildSize.x * 2 + 1,
                (maxHeight - minHeight) + 1,         // == _buildSize.y + 1
                buildSize.z * 2 + 1
            );

            var center = new Vector3(
                0f,
                minHeight + (size.y - 1) * 0.5f,
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
            var blockCount = _worldData.Blocks.Length;
            var surfaceBlock = _worldData.Blocks[1];
            var subsurfaceBlock = blockCount < 3 ? _worldData.Blocks[^1] : _worldData.Blocks[2];
            var bottomSubsurfaceBlock = _worldData.Blocks[^1];

            ClearWorld();
            
            var buildSize = _worldData.BuildSize;
            var minHeight = _worldData.MinHeight;
            var bottomLayerHeight = _worldData.BottomLayerHeight;
            var noiseFrequency = _worldData.NoiseFrequency;
            var seed = _worldData.Seed;
            var maxHeight = _worldData.MaxHeight;

            // Clamp ranges
            var yMin = math.min(minHeight, maxHeight);
            var yMax = math.max(minHeight, maxHeight);
            var heightRange = math.max(1, yMax - yMin); // exclusive mapping

            for (var z = -buildSize.z; z <= buildSize.z; z++)
            for (var x = -buildSize.x; x <= buildSize.x; x++)
            {
                // --- Surface Height from 2D Noise ---

                #region Unity's built-in random number generator.

                // Uncomment if you want to use Unity's built-in random number generator.
                //var n = Mathf.PerlinNoise(x * _noiseFrequency, z * _noiseFrequency);
                
                #endregion

                #region SquirrelNoise32Bit

                // Using Squirrel Perlin noise (deterministic with seed). Comment if you want to use Unity's built-in random number generator.
                var n = SquirrelNoise32Bit.Perlin(x * noiseFrequency, z * noiseFrequency, seed);
                //var n = SquirrelNoise32Bit.Get2DNoise((int)(x * _noiseFrequency), (int)(z * _noiseFrequency), _seed);

                #endregion

                var surfaceY = (int)(yMin + math.round(n * heightRange));
                // Clamp surface to be at least bottom-layer height (prevents tiny columns dipping below the bottom fill band)
                surfaceY = math.clamp(surfaceY, bottomLayerHeight, yMax);

                // --- Fill from MinHeight..(surfaceY-1) ---
                for (var y = yMin; y < surfaceY; y++)
                {
                    var blockToSpawn = y < bottomLayerHeight ? bottomSubsurfaceBlock : subsurfaceBlock;
                    CreateBlock(blockToSpawn, new Vector3Int(x, y, z));
                }

                // --- Place surface block at surfaceY ---
                CreateBlock(surfaceBlock, new Vector3Int(x, surfaceY, z));
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
        /// Creates a block at a specific grid position based on the current generation type.
        /// </summary>
        /// <param name="block">The data defining the block's properties, such as name, prefab, and textures.</param>
        /// <param name="gridPos">The grid position where the block will be generated.</param>
        private void CreateBlock(in BlockData block, in Vector3Int gridPos)
        {
            switch (_generateType)
            {
                case GenerateType.Prefab:
                    SpawnBlock(block, gridPos);
                    break;
                case GenerateType.Mesh:
                    CreateMesh(block, gridPos);
                    break;
            }
        }

        /// <summary>
        /// Creates a mesh for the given block data at a specified grid position.
        /// </summary>
        /// <param name="block">The block data containing information to define the block's properties.</param>
        /// <param name="gridPos">The grid position where the mesh will be created.</param>
        private void CreateMesh(in BlockData block, in Vector3Int gridPos)
        {
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
            _worldData.ValidateConfig();

            if (!_blocksParent && transform.childCount > 0)
            {
                _blocksParent = transform;
            }
        }

        #endregion
    }
}