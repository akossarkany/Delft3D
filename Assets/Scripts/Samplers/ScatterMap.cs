using System;
using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Netherlands3D.Twin
{
    [RequireComponent(typeof(Camera))]
    public class ScatterMap : MonoBehaviour
    {
        public static ScatterMap Instance { get; private set; }

        private Camera depthCamera;
        private Texture2D samplerTexture;
        public float GridSampleSize = 1f; //how many pixels per square meter should be used in the texture for sampling?
        
        public ScatterSettingsPropertySection propertyPanelPrefab; //todo: find a better way to reference this.

#if UNITY_EDITOR //for debug purposes
        private Bounds polyBounds;
        private Bounds gridBounds;
        private float gridCellSize;
#endif

        private void Awake()
        {
            depthCamera = GetComponent<Camera>();
            if (Instance)
                Debug.LogError("There should only be one ScatterMap Instance. Having multiple may result in unexpected behaviour.", gameObject);
            else
                Instance = this;
        }

        private void Start()
        {
            //We will only render on demand using camera.Render()
            depthCamera.enabled = false;
        }

        private void OnDestroy()
        {
            Destroy(samplerTexture);
        }

        /// <summary>
        /// Renders the depth camera and creates the texture to sample from.
        /// </summary>
        private void RenderDepthCamera()
        {
            //Read pixels from the depth texture
            depthCamera.Render();
            RenderTexture.active = depthCamera.targetTexture;
            //Create a texture with 32 bits per channel that we can sample  from. this is needed because the RenderTexture only exists on the GPU
            samplerTexture = new Texture2D(depthCamera.targetTexture.width, depthCamera.targetTexture.height, TextureFormat.RGBAFloat, false);
            samplerTexture.ReadPixels(new Rect(0, 0, depthCamera.targetTexture.width, depthCamera.targetTexture.height), 0, 0);
            samplerTexture.Apply();
            RenderTexture.active = null;
        }

        /// <summary>
        /// Starts a coroutine to generate scatterpoints and sample randomness at the scattered points.
        /// </summary>
        /// <param name="polygonBounds">Bounds of the polygon to generate points in (in world space)</param>
        /// <param name="density">Density of the points in the grid</param>
        /// <param name="scatter">Normalized amount of scatter to apply to the points (0=no scatter, 1=max scatter)</param>
        /// <param name="angle">Angle to rotate the grid of points by</param>
        /// <param name="onPointsGeneratedCallback">The generation takes a few frames because we need to wait for the camera to render. This callback will be invoked once the generation is done.</param>
        public void GenerateScatterPoints(Bounds polygonBounds, float density, float scatter, float angle, System.Action<List<Vector3>, List<Vector2>> onPointsGeneratedCallback)
        {
            if (onPointsGeneratedCallback == null)
                return;

            StartCoroutine(GenerateScatterPointsCoroutine(polygonBounds, density, scatter, angle, onPointsGeneratedCallback));
        }
        private IEnumerator GenerateScatterPointsCoroutine(Bounds polygonBounds, float density, float scatter, float angle, System.Action<List<Vector3>, List<Vector2>> onPointsGeneratedCallback)
        {
            float cellSize = 1f / Mathf.Sqrt(density);
            var gridPoints = CompoundPolygon.GenerateGridPoints(polygonBounds, cellSize, angle, out var gridBounds);
            
#if UNITY_EDITOR 
            this.gridBounds = gridBounds;
            this.gridCellSize = cellSize;
#endif
            yield return new WaitForEndOfFrame(); //wait for new polygon mesh to be created in case this function was coupled to the same event as the polygon mesh generation and this would be called before the mesh creation.
            CreateRenderTexture(gridBounds, GridSampleSize); //todo: polygon should be rendered with an outline to include the random offset of points outside the polygon that due to the random offset will be shifted inside the polygon.
            AlignCameraToPolygon(depthCamera, gridBounds);

            yield return null; //wait for next frame to begin rendering with the new settings
            RenderDepthCamera(); //don't know exactly why it is needed to wait twice, but not doing so causes unexpected behaviour
            yield return new WaitForEndOfFrame(); //wait for rendering to complete

            //sample texture at points to get random offset and add random offset to world space points. Sample texture at new point to see if it is inside the poygon and if so to get the height.
            var offsetPoints = AddRandomOffsetAndSampleHeightAndSampleScale(gridPoints, gridBounds, GridSampleSize, scatter, cellSize);
            onPointsGeneratedCallback?.Invoke(offsetPoints.Item1, offsetPoints.Item2);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(polyBounds.center, polyBounds.size);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(gridBounds.center, gridBounds.size);
            var width = Mathf.CeilToInt(1f * (gridBounds.size.x + 2 * gridCellSize)); //add 2*maxRandomOffset to include the max scatter range on both sides
            var height = Mathf.CeilToInt(1f * (gridBounds.size.z + 2 * gridCellSize));
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(gridBounds.center, new Vector3(width, 0, height));
        }
#endif

        /// <summary>
        /// This function will add a random offset to the grid points, and sample the new point's height and whether or not it falls inside or outside of the polygon.
        /// </summary>
        /// <param name="worldPoints">Array of world points to process</param>
        /// <param name="gridBounds">Bounds of the world points</param>
        /// <param name="gridSampleSize">How many samples the texture has per square world unit</param>
        /// <param name="randomness">How much scatter to apply in a Range from 0 (no scatter) to 1 (max scatter)</param>
        /// <param name="gridCellSize">size of a single grid cell</param>
        /// <returns>List of offset points with the sampled height and the raw random sample at the offset point</returns>
        private (List<Vector3>, List<Vector2>) AddRandomOffsetAndSampleHeightAndSampleScale(Vector2[] worldPoints, Bounds gridBounds, float gridSampleSize, float randomness, float gridCellSize)
        {
            var points = new List<Vector3>(worldPoints.Length);
            var sampledScales = new List<Vector2>(worldPoints.Length);

            var boundsCenter2D = new Vector2(gridBounds.center.x, gridBounds.center.z);
            var boundsExtents2D = new Vector2(gridBounds.extents.x, gridBounds.extents.z);
            var pointSamplePositionOffset = -boundsCenter2D + boundsExtents2D;

            var pixels = samplerTexture.GetPixels();
            var textureWidth = samplerTexture.width;
            var textureHeight = samplerTexture.height;
            var offsetPoint = new Vector3(); //define vector3 here to avoid calling constructor in the (potentially large) loop
            var sampledRandomness = new Vector2();

            for (int i = 0; i < worldPoints.Length; i++)
            {
                var originalWorldPoint = worldPoints[i];
                int originalXInPixelSpace = (int)((originalWorldPoint.x + pointSamplePositionOffset.x) * gridSampleSize); //casting is more efficient than Mathf.FloorToInt
                int originalYInPixelSpace = (int)((originalWorldPoint.y + pointSamplePositionOffset.y) * gridSampleSize);

                if (originalXInPixelSpace < 0 || originalXInPixelSpace >= textureWidth || originalYInPixelSpace < 0 || originalYInPixelSpace >= textureHeight) // since we are converting a 2D coordinate to a 1D index, we need to account for points (mostly when rotating the grid) that are out of bounds of the texture so we do not accidentally read pixels from the previous or next pixel rows. By also checking the y, we also skip points that that are completely out of bounds of the texture
                    continue;

                int index = originalYInPixelSpace * textureWidth + originalXInPixelSpace;

                var colorSample = pixels[index];
                float randomOffsetX = (colorSample.r - 0.5f) * randomness * gridCellSize; //range [-0.5*gridCellSize, 0.5*gridCellSize]
                float randomOffsetY = (colorSample.g - 0.5f) * randomness * gridCellSize;

                float scatteredPointX = originalWorldPoint.x + randomOffsetX;
                float scatteredPointY = originalWorldPoint.y + randomOffsetY;


                int scatteredXInPixelSpace = (int)((scatteredPointX + pointSamplePositionOffset.x) * gridSampleSize);
                int scatteredYInPixelSpace = (int)((scatteredPointY + pointSamplePositionOffset.y) * gridSampleSize);

                index = scatteredYInPixelSpace * textureWidth + scatteredXInPixelSpace;
                if (index < 0 || index >= pixels.Length) //in case the grid is rotated, points can end up outside of the texture bounds, this point is then by definition outside of the polygon and can be ignored.
                {
                    continue;
                }

                var newColorSample = pixels[index];

                if (newColorSample.a < 0.5f) //new sampled color does not have an alpha value, so it falls outside of the polygon. Therefore this point can be skipped. This wil clip out any points outside of the polygon
                    continue;

                offsetPoint.x = scatteredPointX;
                offsetPoint.y = newColorSample.b;
                offsetPoint.z = scatteredPointY;

                sampledRandomness.x = newColorSample.r; //for our purposes, it doesn't really matter if these use the same sampled values as for the random offset
                sampledRandomness.y = newColorSample.g;

                points.Add(offsetPoint);
                sampledScales.Add(sampledRandomness);
            }

            return (points, sampledScales);
        }

        /// <summary>
        /// Create a render texture for the camera to render to, using the to polygon size + max random offset on all sides.
        /// </summary>
        /// <param name="Bounds">the bounds of the grid to fit to</param>
        /// <param name="gridSampleSize">how many samples per square world unit should be taken in the texture</param>
        private void CreateRenderTexture(Bounds gridBounds, float gridSampleSize)
        {
            var width = Mathf.CeilToInt(gridSampleSize * gridBounds.size.x);
            var height = Mathf.CeilToInt(gridSampleSize * gridBounds.size.z);

            var renderTexture = new RenderTexture(width, height, GraphicsFormat.R32G32B32A32_SFloat, GraphicsFormat.None);
            depthCamera.targetTexture = renderTexture;
            if (depthCamera.targetTexture.width > 4096 || depthCamera.targetTexture.height > 4096)
                throw new ArgumentOutOfRangeException("Texture size should not be higher than 4096");
            //todo: cap resolution to max 4096 x 4096 and render the texture in batches if it is higher
        }

        /// <summary>
        /// Align the camera to the polygon bounds
        /// </summary>
        /// <param name="camera">Camera to align. The camera must be orthographic to set the size properly</param>
        /// <param name="bounds">Polygon to align to. The camera orthographic size will be set to the polygon height (z) value.</param>
        public void AlignCameraToPolygon(Camera camera, Bounds bounds)
        {
            camera.transform.position = new Vector3(bounds.center.x, camera.transform.position.y, bounds.center.z);
            camera.orthographicSize = bounds.extents.z;
        }
    }
}