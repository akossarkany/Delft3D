using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GeoJSON.Net;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Netherlands3D.Coordinates;
using Netherlands3D.SelectionTools;
using Netherlands3D.Twin.Projects;
using Netherlands3D.Twin.UI.LayerInspector;
using UnityEngine;

namespace Netherlands3D.Twin
{
    [Serializable]
    public class GeoJSONPolygonLayer : LayerNL3DBase
    {
        public class FeatureSpawnedVisualisation
        {
            public Feature feature;
            public List<PolygonVisualisation> visualisations = new();
            public Bounds bounds;

            private float boundsRoundingCeiling = 1000;
            public float BoundsRoundingCeiling { get => boundsRoundingCeiling; set => boundsRoundingCeiling = value; }

            /// <summary>
            /// Calculate bounds by combining all visualisation bounds
            /// </summary>
            public void CalculateBounds()
            {
                if (visualisations.Count > 0)
                {
                    bounds = visualisations[0].GetBounds();
                    for(int i = 1; i < visualisations.Count; i++)
                        bounds.Encapsulate(visualisations[i].GetBounds());
                }

                // Expand bounds to ceiling to steps of 1000
                bounds.size = new Vector3(
                    Mathf.Ceil(bounds.size.x / BoundsRoundingCeiling) * BoundsRoundingCeiling,
                    Mathf.Ceil(bounds.size.y / BoundsRoundingCeiling) * BoundsRoundingCeiling,
                    Mathf.Ceil(bounds.size.z / BoundsRoundingCeiling) * BoundsRoundingCeiling
                );
                bounds.center = new Vector3(
                    Mathf.Round(bounds.center.x / BoundsRoundingCeiling) * BoundsRoundingCeiling,
                    Mathf.Round(bounds.center.y / BoundsRoundingCeiling) * BoundsRoundingCeiling,
                    Mathf.Round(bounds.center.z / BoundsRoundingCeiling) * BoundsRoundingCeiling
                );
            }
        }

        public List<FeatureSpawnedVisualisation> SpawnedVisualisations = new();
        public List<PolygonVisualisation> PolygonVisualisations { get; private set; } = new();

        private Material polygonVisualizationMaterial;

        public Material PolygonVisualizationMaterial
        {
            get { return polygonVisualizationMaterial; }
            set
            {
                polygonVisualizationMaterial = value;
                foreach (var visualization in PolygonVisualisations)
                {
                    visualization.GetComponent<MeshRenderer>().material = polygonVisualizationMaterial;
                }
            }
        }

        public GeoJSONPolygonLayer(string name) : base(name)
        {
            ProjectData.Current.AddStandardLayer(this);
        }
        
        protected override void OnLayerActiveInHierarchyChanged(bool activeInHierarchy)
        {
            foreach (var visualization in PolygonVisualisations)
            {
                visualization.gameObject.SetActive(activeInHierarchy);
            }
        }

        public void AddAndVisualizeFeature<T>(Feature feature, CoordinateSystem originalCoordinateSystem)
            where T : GeoJSONObject
        {
            var newFeatureVisualisation = new FeatureSpawnedVisualisation { feature = feature };

            if (feature.Geometry is MultiPolygon multiPolygon)
            {
                var polygonVisualisation = GeoJSONGeometryVisualizerUtility.VisualizeMultiPolygon(multiPolygon, originalCoordinateSystem, PolygonVisualizationMaterial);
                newFeatureVisualisation.visualisations = polygonVisualisation;
            }
            else if(feature.Geometry is Polygon polygon)
            {
                var singlePolygonVisualisation = GeoJSONGeometryVisualizerUtility.VisualizePolygon(polygon, originalCoordinateSystem, PolygonVisualizationMaterial);
                newFeatureVisualisation.visualisations.Append(singlePolygonVisualisation);
            }
            
            newFeatureVisualisation.CalculateBounds();
            SpawnedVisualisations.Add(newFeatureVisualisation);
        }

        public override void DestroyLayer()
        {
            base.DestroyLayer();
            if (Application.isPlaying)
            {
                foreach (var visualization in PolygonVisualisations)
                {
                    if(visualization.gameObject)
                        GameObject.Destroy(visualization.gameObject);
                }
            }
        }

        /// <summary>
        /// Checks the Bounds of the visualisations and checks them against the camera frustum
        /// to remove visualisations that are out of view
        /// </summary>
        public void RemoveFeaturesOutOfView()
        {
            // Remove visualisations that are out of view
            var frustumPlanes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
            for (int i = SpawnedVisualisations.Count - 1; i >= 0 ; i--)
            {
                var inCameraFrustum = GeometryUtility.TestPlanesAABB(frustumPlanes, SpawnedVisualisations[i].bounds);
                if (inCameraFrustum)
                    continue;

                var featureVisualisation = SpawnedVisualisations[i];
                RemoveFeature(featureVisualisation);
            }
        }
        
        private void RemoveFeature(FeatureSpawnedVisualisation featureVisualisation)
        {
            foreach (var polygonVisualisation in featureVisualisation.visualisations)
            {
                PolygonVisualisations.Remove(polygonVisualisation);
                if(polygonVisualisation.gameObject)
                    GameObject.Destroy(polygonVisualisation.gameObject);
            }
            SpawnedVisualisations.Remove(featureVisualisation);
        }
    }
}