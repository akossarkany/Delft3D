using System;
using System.Collections;
using Netherlands3D.Coordinates;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Profiling;

namespace Netherlands3D.Twin.FloatingOrigin
{
    public class Origin : MonoBehaviour, IHasCoordinate
    {
        public Coordinate Coordinate { get; set; }
        public Camera mainCamera;
        public CoordinateSystem referenceCoordinateSystem;

        /// <summary>
        /// The maximum of 30.000 is to prevent integer overflows when squaring the distance, with
        /// a safety margin for when the camera moves really fast.
        /// </summary>
        [Tooltip("The distance from the mainCamera before shifting the Origin")]
        [Range(1000, 30000)]
        [SerializeField] private int distanceBeforeShifting = 10000;
        
        /// <summary>
        /// A cached square root of the distanceBeforeShifting value for performance optimization.
        /// </summary>
        private int sqrDistanceBeforeShifting = 100000000;

        public UnityEvent<Coordinate, Coordinate> onPreShift = new(); 
        public UnityEvent<Coordinate, Coordinate> onPostShift = new();

        public bool LogShifts = false;
        
        private void Start()
        {
            mainCamera = mainCamera == null ? Camera.main : mainCamera;

            // Cache the square of the distance
            sqrDistanceBeforeShifting = distanceBeforeShifting * distanceBeforeShifting;

            Coordinate = CoordinateConverter.ConvertTo(
                new Coordinate(
                    CoordinateSystem.Unity,
                    transform.position.x,
                    transform.position.y,
                    transform.position.z
                ),
                referenceCoordinateSystem
            );

            StartCoroutine(AttemptShift());
        }

        private IEnumerator AttemptShift() 
        {
            while (true)
            {
                yield return new WaitForEndOfFrame();

                var mainCameraPosition = mainCamera.transform.position;
                var distanceToCamera = mainCameraPosition - transform.position;

                // sqrMagnitude is used because it outperforms magnitude quite a bit: https://docs.unity3d.com/ScriptReference/Vector3-sqrMagnitude.html
                if (distanceToCamera.sqrMagnitude < sqrDistanceBeforeShifting)
                {
                    continue;
                }

                Profiler.BeginSample("PerformOriginShift");
                var newPosition = new Vector3(mainCameraPosition.x, transform.position.y, mainCameraPosition.z);

                // Move this Origin to the camera's position, but keep the same height as it is now. 
                MoveOriginTo(
                    CoordinateConverter.ConvertTo(
                        new Coordinate(CoordinateSystem.Unity, newPosition.x, newPosition.y, newPosition.z),
                        Coordinate.CoordinateSystem
                    )
                );

                Profiler.EndSample();
            }
        }

        public void MoveOriginTo(Coordinate destination)
        {
            var from = Coordinate;
            var to = CoordinateConverter.ConvertTo(destination, Coordinate.CoordinateSystem);
#if UNITY_EDITOR
            if (LogShifts) Debug.Log($"Moving origin from {from.ToVector3()} (EPSG:{from.CoordinateSystem}) to {to.ToVector3()} (EPSG:{to.CoordinateSystem})");
#endif

            onPreShift.Invoke(from, to);
            
            Coordinate = to;
            // TODO: Shouldn't this be a listener to the event below?
            var rdCoordinate = CoordinateConverter.ConvertTo(Coordinate, CoordinateSystem.RD);
            EPSG7415.relativeCenter = new Vector2RD(rdCoordinate.Points[0], rdCoordinate.Points[1]);

            // Shout to the world that the origin has changed to this coordinate
            onPostShift.Invoke(from, to);
        }
    }
}