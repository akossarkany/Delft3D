using System;
using System.Collections.Generic;
using Netherlands3D.SelectionTools;
using Netherlands3D.Twin.UI.LayerInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers
{
    public enum ShapeType
    {
        Undefined = 0,
        Polygon = 1,
        Line = 2
    }

    public class PolygonSelectionLayer : LayerNL3DBase
    {
        public CompoundPolygon Polygon { get; set; }
        public PolygonVisualisation PolygonVisualisation { get; private set; }

        private float polygonExtrusionHeight;
        private Material polygonMeshMaterial;
        public Material PolygonMeshMaterial => polygonMeshMaterial;

        public UnityEvent<PolygonSelectionLayer> polygonSelected = new();
        public UnityEvent polygonChanged = new();

        private ShapeType shapeType;
        public ShapeType ShapeType { get => shapeType; set => shapeType = value;}
        
        public List<Vector3> OriginalPolygon;
        private float lineWidth = 10.0f;

        public void Initialize(List<Vector3> polygon, float polygonExtrusionHeight, Material polygonMeshMaterial, ShapeType shapeType)
        {
            this.ShapeType = shapeType;
            this.polygonExtrusionHeight = polygonExtrusionHeight;
            this.polygonMeshMaterial = polygonMeshMaterial;
            OriginalPolygon = polygon;

            if(shapeType == Layers.ShapeType.Line)
                polygon = PolygonFromLine(polygon, lineWidth);

            SetPolygon(polygon);
            PolygonVisualisation.reselectVisualisedPolygon.AddListener(OnPolygonVisualisationSelected);
        }

        private void OnEnable()
        {
            ClickNothingPlane.ClickedOnNothing.AddListener(DeselectPolygon);
        }

        private void OnDisable()
        {
            ClickNothingPlane.ClickedOnNothing.RemoveListener(DeselectPolygon);
        }

        private void OnPolygonVisualisationSelected(PolygonVisualisation visualisation)
        {
            if (UI)
                UI.Select(!LayerUI.SequentialSelectionModifierKeyIsPressed() && !LayerUI.AddToSelectionModifierKeyIsPressed()); //if there is no UI, this will do nothing. this is intended as when the layer panel is closed the polygon should not be (accidentally) selectable
        }

        public void DeselectPolygon()
        {
            if (UI && UI.IsSelected)
                UI.Deselect(); // processes OnDeselect as well
            else
                OnDeselect(); // only call this if the UI does not exist. This should not happen with the intended behaviour being that polygon selection is only active when the layer panel is open
        }

        public void SetPolygon(List<Vector3> solidPolygon)
        {
            var flatPolygon = PolygonCalculator.FlattenPolygon(solidPolygon.ToArray(), new Plane(Vector3.up, 0));
            var polygon = new CompoundPolygon(flatPolygon);
            Polygon = polygon;

            if (PolygonVisualisation)
                PolygonVisualisation.UpdateVisualisation(solidPolygon);
            else
                PolygonVisualisation = CreatePolygonMesh(solidPolygon, polygonExtrusionHeight, polygonMeshMaterial);
            
            polygonChanged.Invoke();
        }

        public void SetLine(List<Vector3> line)
        {
            if(shapeType != ShapeType.Line)
                Debug.LogError("The polygon layer is not a line layer, this will result in unexpected behaviour");
            
            var polygon = PolygonFromLine(line, lineWidth);
            SetPolygon(polygon);
        }

        private List<Vector3> PolygonFromLine(List<Vector3> originalLine, float width)
        {
            if (originalLine.Count != 2)
            {
                Debug.LogError("cannot create rectangle because position list contains more than 2 entries");
                return null;
            }

            var worldPlane = new Plane(Vector3.up, 0); //todo: work with terrain height
            var flatPolygon = PolygonCalculator.FlattenPolygon(originalLine, worldPlane);
            var dir = flatPolygon[1] - flatPolygon[0];
            var normal = new Vector2(-dir.y, dir.x).normalized;

            var dist = normal * width/2;

            var point1 = originalLine[0] + new Vector3(dist.x, 0, dist.y);
            var point2 = originalLine[0] - new Vector3(dist.x, 0, dist.y);
            var point3 = originalLine[1] - new Vector3(dist.x, 0, dist.y);
            var point4 = originalLine[1] + new Vector3(dist.x, 0, dist.y);
            
            var polygon = new List<Vector3>() {
                point1,
                point2,
                point3,
                point4
            };
            return polygon;
        }

        public static PolygonVisualisation CreatePolygonMesh(List<Vector3> polygon, float polygonExtrusionHeight, Material polygonMeshMaterial)
        {
            var contours = new List<List<Vector3>> { polygon };
            var polygonVisualisation = PolygonVisualisationUtility.CreateAndReturnPolygonObject(contours, polygonExtrusionHeight, true, false, false, polygonMeshMaterial);
            polygonVisualisation.DrawLine = false; //lines will be drawn per layer, but a single mesh will receive clicks to select
            
            return polygonVisualisation;
        }

        protected override void OnLayerActiveInHierarchyChanged(bool activeInHierarchy)
        {
            print("setting active: " + activeInHierarchy);
            PolygonVisualisation.gameObject.SetActive(activeInHierarchy);
        }

        public override void OnSelect()
        {
            base.OnSelect();
            polygonSelected.Invoke(this);
        }

        public override void OnDeselect()
        {
            base.OnDeselect();
            polygonSelected.Invoke(null);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            PolygonVisualisation.reselectVisualisedPolygon.RemoveListener(OnPolygonVisualisationSelected);
            Destroy(PolygonVisualisation.gameObject);
        }
    }
}