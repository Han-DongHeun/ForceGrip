using System;
using UnityEngine;

namespace Sensors
{
    public class VoxelSensor: MonoBehaviour
    {
        [ShowOnly, SerializeField] private Vector3 size;
        [ShowOnly, SerializeField] private Vector3Int resolution;
        [SerializeField] private float cellSize;
    
        private Matrix4x4 _pivot = Matrix4x4.identity;
        private Vector3[][][] _points = Array.Empty<Vector3[][]>();
        private Vector3[][][] _references = Array.Empty<Vector3[][]>();
        public float[][][] occupancies = Array.Empty<float[][]>();
        
        private Collider[] _colliders = new Collider[128];
        
        public Vector3 Size
        {
            get => size;
            set
            {
                size = value;
                Generate();
            }
        }
        public Vector3Int Resolution
        {
            get => resolution;
        }
        
        public float CellSize
        {
            get => cellSize;
            set
            {
                cellSize = value;
                if (cellSize <= 0)
                    cellSize = Mathf.Min(Size.x, Size.y, Size.z);
                Generate();
            }
        }
        public Matrix4x4 Pivot => _pivot;
        public Vector3[][][] Points => _points;
        public Vector3[][][] References => _references;
        
        private float previousCellSize;
        void OnValidate()
        {
            if (!Mathf.Approximately(previousCellSize, cellSize))
            {
                CellSize = cellSize;
                previousCellSize = cellSize;
            }
        }
        
        void Start()
        {
            size = transform.lossyScale;
            if (cellSize <= 0)
                cellSize = Mathf.Min(Size.x, Size.y, Size.z);
            previousCellSize = cellSize;
            Generate();
        }
        
        #if UNITY_EDITOR 
        void Update()
        {
            if (Size != transform.lossyScale)
                Size = transform.lossyScale;
        }
        #endif
        
        public int GetOccupiedNumber()
        {
            int num = 0;
            for (int x = 0; x < occupancies.Length; x++)
            {
                for (int y = 0; y < occupancies[x].Length; y++)
                {
                    for (int z = 0; z < occupancies[x][y].Length; z++)
                    {
                        if (occupancies[x][y][z] > 0)
                        {
                            num++;
                        }
                    }
                }
            }
            return num;
        }
        
        public Vector3 GetStep()
        {
            return new Vector3(Size.x / Resolution.x, Size.y / Resolution.y, Size.z / Resolution.z);
        }
        
        private int GetDimensionality()
        {
            return Resolution.x * Resolution.y * Resolution.z;
        }
        
        void Generate()
        {
            resolution = Vector3Int.RoundToInt(Size / CellSize);
            _points = new Vector3[Resolution.x][][];
            _references = new Vector3[Resolution.x][][];
            occupancies = new float[Resolution.x][][];
            for (int x = 0; x < Resolution.x; x++)
            {
                _points[x] = new Vector3[Resolution.y][];
                _references[x] = new Vector3[Resolution.y][];
                occupancies[x] = new float[Resolution.y][];
                for (int y = 0; y < Resolution.y; y++)
                {
                    _points[x][y] = new Vector3[Resolution.z];
                    _references[x][y] = new Vector3[Resolution.z];
                    occupancies[x][y] = new float[Resolution.z];
                }
            }
            for (int x = 0; x < Resolution.x; x++)
            {
                for (int y = 0; y < Resolution.y; y++)
                {
                    for (int z = 0; z < Resolution.z; z++)
                    {
                        Points[x][y][z] = new Vector3(
                            (-0.5f + (x + 0.5f) / Resolution.x) * Size.x,
                            (-0.5f + (y + 0.5f) / Resolution.y) * Size.y,
                            (-0.5f + (z + 0.5f) / Resolution.z) * Size.z
                        );
                    }
                }
            }
        }
        
        public void SenseSingleCollider(Matrix4x4 pivot, LayerMask mask, Collider specificCol)
        {
            _pivot = pivot;

            Vector3 pivotPosition = Pivot.GetPosition();
            Quaternion pivotRotation = Pivot.rotation;

            if (!Physics.CheckBox(pivotPosition, Size / 2, pivotRotation, mask))
            {
                for (int x = 0; x < occupancies.Length; x++)
                {
                    for (int y = 0; y < occupancies[x].Length; y++)
                    {
                        for (int z = 0; z < occupancies[x][y].Length; z++)
                        {
                            occupancies[x][y][z] = 0f;
                        }
                    }
                }

                return;
            }

            Vector3 sensorPosition = pivot.GetPosition();
            Quaternion sensorRotation = pivot.rotation;
            Vector3 step = GetStep();
            float range = Mathf.Max(step.x, step.y, step.z);
            for (int x = 0; x < Points.Length; x++)
            {
                for (int y = 0; y < Points[x].Length; y++)
                {
                    for (int z = 0; z < Points[x][y].Length; z++)
                    {
                        Vector3 sensor = sensorPosition + sensorRotation * Points[x][y][z];
                        Collider c;
                        Vector3 closest = Utility.GetClosestPointOverlapBoxSpecificCollider(
                            sensor, step / 2f, sensorRotation,
                            mask, out c, specificCol);
                        occupancies[x][y][z] =
                            (c == null
                                ? 0f
                                : 1f - Vector3.Distance(sensor, closest) /
                                (range * Mathf.Sqrt(3) / 2)); // 2분의 루트 3 * range
                    }
                }
            }
        }
        
        public void Sense(LayerMask mask)
        {
            var _transform = transform;
            Matrix4x4 pivot = Matrix4x4.TRS(_transform.position, _transform.rotation, _transform.lossyScale);
            Sense(pivot, mask);
        }
        
        public void Sense(Matrix4x4 pivot, LayerMask mask)
        {
            _pivot = pivot;
            
            Vector3 pivotPosition = Pivot.GetPosition();
            Quaternion pivotRotation = Pivot.rotation;
            Vector3 sensorPosition = Pivot.GetPosition();
            Quaternion sensorRotation = Pivot.rotation;
            Vector3 step = GetStep();
            float range = step.magnitude / 2f;
            
            int colliderCount = Physics.OverlapBoxNonAlloc(pivotPosition, Size / 2, _colliders, pivotRotation, mask,
                QueryTriggerInteraction.Ignore);
            
            if (colliderCount == 0)
            {
                for (int x = 0; x < occupancies.Length; x++)
                {
                    for (int y = 0; y < occupancies[x].Length; y++)
                    {
                        for (int z = 0; z < occupancies[x][y].Length; z++)
                        {
                            occupancies[x][y][z] = 0f;
                        }
                    }
                }
                return;
            }
            
            for (int x = 0; x < Points.Length; x++)
            {
                for (int y = 0; y < Points[x].Length; y++)
                {
                    for (int z = 0; z < Points[x][y].Length; z++)
                    {
                        Vector3 sensor = sensorPosition + sensorRotation * Points[x][y][z];
                        float closestDistance = Utility.GetClosestDistanceAmongColliders(sensor, range, _colliders, colliderCount, out bool found);
                        occupancies[x][y][z] = Mathf.Clamp01(!found ? 0f : 1f - closestDistance / range);
                    }
                }
            }
        }
        
        public void Draw(Color? colorOccupied = null, Color? colorEmpty = null, Color? colorNegativeOccupied = null)
        {
            colorOccupied ??= Color.black;
            colorEmpty ??= new Color(1f, 1f, 1f, 0.025f);
            colorNegativeOccupied ??= Color.red;
            
            Vector3 position = Pivot.GetPosition();
            Quaternion rotation = Pivot.rotation;
            UltiDraw.SetDepthRendering(false);
            UltiDraw.Begin();
            Vector3 step = GetStep();
            if (Size != Vector3.zero)
            {
                UltiDraw.DrawWireCuboid(position, rotation, Size, (Color) colorOccupied);
                for (int x = 0; x < Points.Length; x++)
                {
                    for (int y = 0; y < Points[x].Length; y++)
                    {
                        for (int z = 0; z < Points[x][y].Length; z++)
                        {
                            _references[x][y][z] = position + rotation * Points[x][y][z];
                            if (occupancies[x][y][z] > 0f)
                            {
                                UltiDraw.DrawCuboid(References[x][y][z], rotation, step,
                                    Color.Lerp(UltiDraw.None, (Color) colorOccupied, occupancies[x][y][z]));
                            }
                            else if (occupancies[x][y][z] < 0f)
                            {
                                UltiDraw.DrawCuboid(References[x][y][z], rotation, step,
                                    Color.Lerp(UltiDraw.None, (Color) colorNegativeOccupied, -occupancies[x][y][z]));
                            }
                            else
                            {
                                UltiDraw.DrawCuboid(References[x][y][z], rotation, step, (Color) colorEmpty);
                            }
                        }
                    }
                }
            }
            
            UltiDraw.End();
        }
    }
}