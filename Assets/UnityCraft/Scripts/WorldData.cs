using Unity.Burst;
using UnityEngine;

namespace UnityCraft
{
    /// <summary>
    /// Represents the data structure for configuring and generating a procedural world in UnityCraft.
    /// This struct includes properties for defining the size and boundaries of the world along
    /// with seed-based deterministic generation, noise parameters, and block data definitions.
    /// </summary>
    /// <remarks>
    /// The <see cref="WorldData"/> struct supports customization of the world by allowing specific
    /// configurations related to height, dimensions, and noise frequency. The size of the world
    /// directly impacts performance and should be configured within appropriate limits.
    /// </remarks>
    [System.Serializable]
    [BurstCompile]
    public struct WorldData
    {
        /// <summary>
        /// Defines the default size of the world in blocks.
        /// X represents half-width, Y represents the maximum height above Minimum Height,
        /// and Z represents half-length. Used when no specific build size is provided.
        /// </summary>
        private static readonly Vector3Int _defaultBuildSize = new(32, 380, 32);

        #region Fields

        /// <summary>
        /// Seed for deterministic height noise. Changing this changes the terrain layout.
        /// </summary>
        [Tooltip("Seed for deterministic height noise. Changing this changes the terrain layout.")]
        [SerializeField] private uint _seed;

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
        [SerializeField] private Vector3Int _buildSize;

        /// <summary>
        /// Minimum Y (inclusive) to start building from. Surface height will be >= this value.
        /// Default: -10
        /// </summary>
        [Tooltip("Minimum Y (inclusive) to start building from. Default: -64")]
        [SerializeField] private int _minHeight;

        /// <summary>
        /// Y (inclusive) to start using the SubsurfaceBlock block.
        /// Default: -62
        /// </summary>
        [Tooltip("Y (inclusive) to start using the SubsurfaceBlock block. Default: -62")]
        [SerializeField] private int _bottomLayerHeight;

        /// <summary>
        /// Noise frequency used for height sampling. Larger values = more frequent variation.
        /// </summary>
        [Tooltip("Perlin noise frequency for heightmap. Larger values = more frequent variation.")]
        [Min(0.0001f)]
        [SerializeField] private float _noiseFrequency;
        
        /// <summary>
        /// Represents the collection of block data used to define the types of blocks available in the world.
        /// The index position of each element corresponds to its associated block type in the world generation.
        /// </summary>
        /// <remarks>
        /// Blocks[0] represents air (empty space),
        /// Blocks[1] represents the surface block,
        /// Blocks[n] represents subsurface blocks,
        /// and Blocks[Count-1] represents the bottom layer block.
        /// This collection determines the layers and composition of the world.
        /// </remarks>
        [Tooltip("Blocks[0] → air (empty), Blocks[1] → fallback/default block, Blocks[2] → surface block, Blocks[n] → subsurface blocks, Blocks[Count-1] → bottom layer")]
        [SerializeField] private BlockData[] _blocks;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the maximum height (inclusive) we are allowed to build up to.
        /// Calculated as MinHeight + BuildSize.y.
        /// </summary>
        public int MaxHeight => _minHeight + _buildSize.y;

        /// <summary>
        /// Seed for deterministic height noise.
        /// </summary>
        public uint Seed => _seed;

        /// <summary>
        /// Specifies the dimensions of the world to be built in blocks.
        /// X represents half the width of the world (world spans -X to X),
        /// Y denotes the vertical span above the minimum height (MaxHeight equals MinHeight plus Y),
        /// and Z signifies half the length of the world (world spans -Z to Z).
        /// Larger values negatively impact performance.
        /// The default value is 32x380x32.
        /// </summary>
        public Vector3Int BuildSize => _buildSize;

        /// <summary>
        /// Gets the minimum Y (inclusive) coordinate at which blocks start being generated.
        /// The surface height will always be greater than or equal to this value.
        /// The default value is -64.
        /// </summary>
        public int MinHeight => _minHeight;

        /// <summary>
        /// The inclusive Y-coordinate at which the SubsurfaceBlock layer begins.
        /// Determines the starting height of the bottom layer in the world generation.
        /// The default value is -62.
        /// </summary>
        public int BottomLayerHeight => _bottomLayerHeight;

        /// <summary>
        /// Gets the frequency of the noise used in procedural terrain heightmap generation.
        /// Higher values result in more frequent and sharper terrain variations.
        /// This property influences the granularity of the noise pattern.
        /// Default value is 1.0f.
        /// </summary>
        public float NoiseFrequency => _noiseFrequency;

        /// <summary>
        /// Represents the collection of block data used to define the types of blocks available in the world.
        /// The index position of each element corresponds to its associated block type in the world generation.
        /// </summary>
        /// <remarks>
        /// Blocks[0] represents air (empty space), Blocks[1] represents the fallback/default block,
        /// Blocks[2] represents the surface block, Blocks[n] represents subsurface blocks,
        /// and Blocks[Count-1] represents the bottom layer block.
        /// This collection determines the layers and composition of the world.
        /// </remarks>
        public BlockData[] Blocks => _blocks;

        #endregion

        /// <summary>
        /// Represents configurable world data within the UnityCraft simulation.
        /// Contains properties essential for procedural world generation, including
        /// seed values for deterministic terrain, world dimensions, height variants,
        /// noise frequency, and block definitions.
        /// </summary>
        public WorldData(BlockData[] blocks, uint seed = 0, Vector3Int buildSize = default,
            int minHeight = -64, int bottomLayerHeight = -62, float noiseFrequency = 1f)
        {
            _blocks = blocks;
            _seed = seed;
            _buildSize = buildSize == default ? _defaultBuildSize : buildSize;
            _minHeight = minHeight;
            _bottomLayerHeight = bottomLayerHeight;
            _noiseFrequency = noiseFrequency;
        }
        
        /// <summary>
        /// Validates core configuration to prevent out-of-range builds or null refs.
        /// </summary>
        public void ValidateConfig()
        {
            if (_blocks == null || _blocks.Length < 2)
                throw new System.ArgumentException("WorldData.Blocks must contain at least 3 elements.");

            if (_blocks.Length < 3)
                Debug.LogWarning("[World] WorldData.Blocks has no subsurface blocks defined");
            
            // Ensure sensible sizes; Y is a span, must be >= 1
            _buildSize.x = Mathf.Max(1, _buildSize.x);
            _buildSize.y = Mathf.Max(1, _buildSize.y);
            _buildSize.z = Mathf.Max(1, _buildSize.z);

            // Keep the bottom layer within [MinHeight, MaxHeight]
            var maxHeight = MaxHeight;
            if (_bottomLayerHeight < _minHeight) _bottomLayerHeight = _minHeight + 1;
            if (_bottomLayerHeight > maxHeight)  _bottomLayerHeight = maxHeight;
        }
    }
}