using System;
using System.Collections.Generic;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Netherlands3D.Coordinates;
using Netherlands3D.SelectionTools;
using Netherlands3D.Twin.Projects;
using UnityEngine;

namespace Netherlands3D.Twin.Layers
{
    [Serializable]
    public class GeoJSONPolygonLayer : LayerData
    {
        public List<Feature> PolygonFeatures = new();
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

        public void AddAndVisualizeFeature(Feature feature, MultiPolygon geometry, CoordinateSystem originalCoordinateSystem)
        {
            PolygonFeatures.Add(feature);
            PolygonVisualisations.AddRange(GeoJSONGeometryVisualizerUtility.VisualizeMultiPolygon(geometry, originalCoordinateSystem, PolygonVisualizationMaterial));
        }

        public void AddAndVisualizeFeature(Feature feature, Polygon geometry, CoordinateSystem originalCoordinateSystem)
        {
            PolygonFeatures.Add(feature);
            PolygonVisualisations.Add(GeoJSONGeometryVisualizerUtility.VisualizePolygon(geometry, originalCoordinateSystem, PolygonVisualizationMaterial));
        }

        public override void DestroyLayer()
        {
            base.DestroyLayer();
            if (Application.isPlaying)
            {
                foreach (var visualization in PolygonVisualisations)
                {
                    GameObject.Destroy(visualization.gameObject);
                }
            }
        }
    }
}