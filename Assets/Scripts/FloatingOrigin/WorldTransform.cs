using Netherlands3D.Coordinates;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.FloatingOrigin
{
    public class WorldTransform : MonoBehaviour, IHasCoordinate
    {
        [SerializeField] private Origin origin;
        [SerializeField] private WorldTransformShifter worldTransformShifter;
        [SerializeField] private CoordinateSystem referenceCoordinateSystem = CoordinateSystem.WGS84;
        public Coordinate Coordinate {
            get;
            set;
        }

        public CoordinateSystem ReferenceCoordinateSystem => referenceCoordinateSystem;
        public Origin Origin => origin;

        public UnityEvent<WorldTransform, Coordinate> onPreShift = new();
        public UnityEvent<WorldTransform, Coordinate> onPostShift = new();

        private void Awake()
        {
            if (origin == null)
            {
                origin = FindObjectOfType<Origin>();
            }

            if (worldTransformShifter == null)
            {
                worldTransformShifter = gameObject.AddComponent<GameObjectWorldTransformShifter>();
            }

            // Pre-initialize the coordinates before using them
            Coordinate = new Coordinate(ReferenceCoordinateSystem, 0, 0, 0);
        }

        private void OnValidate()
        {
            if (referenceCoordinateSystem == CoordinateSystem.Unity)
            {
                Debug.LogError(
                    "Reference coordinate system for a World Transform cannot be in Unity coordinates; "+
                    "otherwise the Origin's location won't be taken into account."
                );
                referenceCoordinateSystem = CoordinateSystem.WGS84;
            }
        }

        private void OnEnable()
        {
            UpdateCoordinateBasedOnUnityTransform();
            origin.onPreShift.AddListener(PrepareToShift);
            origin.onPostShift.AddListener(ShiftTo);
        }

        private void OnDisable()
        {
            origin.onPreShift.RemoveListener(PrepareToShift);
            origin.onPostShift.RemoveListener(ShiftTo);
        }

        private void Update()
        {
            if (transform.hasChanged)
            {
                UpdateCoordinateBasedOnUnityTransform();
                transform.hasChanged = false;
            }
        }

        private void UpdateCoordinateBasedOnUnityTransform()
        {
            var position = transform.position;
            Coordinate = CoordinateConverter.ConvertTo(
                new Coordinate(CoordinateSystem.Unity, position.x, position.y, position.z), 
                Coordinate.CoordinateSystem
            );
        }

        private void PrepareToShift(Coordinate from, Coordinate to)
        {
            // Invoke Pre-shifting event first so that a listener might do some things before the shifter is invoked
            onPreShift.Invoke(this, Coordinate);
            
            worldTransformShifter.PrepareToShift(this, from, to);
        }

        private void ShiftTo(Coordinate from, Coordinate to)
        {
            worldTransformShifter.ShiftTo(this, from, to);
            
            onPostShift.Invoke(this, Coordinate);
        }
    }
}