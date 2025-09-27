using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sensors
{
    public class SurfaceSensorManager : MonoBehaviour
    {
        [Header("Debug")]
        public bool drawClosestVector;
        public bool drawNormalVector;
        public float normalVectorLength = 0.005f;
        public Material lineMaterial;
        private List<GameObject> _distanceTextObjects = new List<GameObject>();
        private List<LineRenderer> _closestLineRenderers = new List<LineRenderer>();
        private List<LineRenderer> _normalLineRenderers = new List<LineRenderer>();
        private List<(Vector3, Vector3, Vector3)> _senseResults = new List<(Vector3, Vector3, Vector3)>();
        
        [Header("Target Object")]
        [ShowOnly, SerializeField] private GameObject _targetObject;
        private Collider[] _colliders = Array.Empty<Collider>();
        
        private List<Transform> _toBeSensedTransforms = new List<Transform>();
        
        public void AddTargetSenseTransform(Transform targetSenseTransform)
        {
            _toBeSensedTransforms.Add(targetSenseTransform);
            
            // 텍스트 오브젝트 생성
            var distanceTextObject = new GameObject($"DistanceText_{_toBeSensedTransforms.Count}");
            distanceTextObject.transform.SetParent(transform.parent);
            distanceTextObject.transform.localScale = new Vector3(0.0004f, 0.0004f, 0.0004f);
            TextMesh textMesh = distanceTextObject.AddComponent<TextMesh>();
            textMesh.fontSize = 100;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.color = Color.red;
            distanceTextObject.SetActive(false);
            
            _distanceTextObjects.Add(distanceTextObject);
            //
            
            // _closestLineRenderers Line Renderer 생성
            var lineRendererObject = new GameObject($"ClosestLineRenderer_{_toBeSensedTransforms.Count}");
            lineRendererObject.transform.SetParent(transform.parent);
            LineRenderer lineRenderer = lineRendererObject.AddComponent<LineRenderer>();
            lineRenderer.startWidth = 0.0002f;
            lineRenderer.endWidth = 0.0002f;
            lineRenderer.material = lineMaterial;
            lineRenderer.startColor = Color.red;
            lineRenderer.endColor = Color.red;
            lineRenderer.positionCount = 2;
            lineRendererObject.SetActive(false);
            
            _closestLineRenderers.Add(lineRenderer);
            //
            
            // _normalLineRenderers Line Renderer 생성
            lineRendererObject = new GameObject($"NormalLineRenderer_{_toBeSensedTransforms.Count}");
            lineRendererObject.transform.SetParent(transform.parent);
            lineRenderer = lineRendererObject.AddComponent<LineRenderer>();
            lineRenderer.startWidth = 0.0002f;
            lineRenderer.endWidth = 0.0002f;
            lineRenderer.material = lineMaterial;
            lineRenderer.startColor = Color.green;
            lineRenderer.endColor = Color.green;
            lineRenderer.positionCount = 2;
            lineRendererObject.SetActive(false);
            
            _normalLineRenderers.Add(lineRenderer);
            //
        }
        
        public void SetTargetObject(GameObject targetObject, Collider[] colliders)
        {
            _targetObject = targetObject;
            _colliders = colliders;
        }
        
        public (Vector3[], Vector3[]) SenseAllCurrentTargetSurfaces()
        {
            _senseResults = new List<(Vector3, Vector3, Vector3)>();
            if (_toBeSensedTransforms == null || _toBeSensedTransforms.Count == 0)
            {
                Debug.LogWarning("Target sense transforms are not assigned or empty.");
                return (null, null);
            }
            
            Vector3[] closestVectors = new Vector3[_toBeSensedTransforms.Count];
            Vector3[] normalVectors = new Vector3[_toBeSensedTransforms.Count];
            
            for (int i = 0; i < _toBeSensedTransforms.Count; i++)
            {
                Vector3 sensePosition = _toBeSensedTransforms[i].position;
                (Vector3 closestPoint, Vector3 normalVector) = SenseTargetObjectSurface(sensePosition);
                Vector3 closestVector = closestPoint - sensePosition;
                closestVectors[i] = closestVector;
                normalVectors[i] = normalVector;
                _senseResults.Add((sensePosition, closestPoint, normalVector));
            }
            
            return (closestVectors, normalVectors);
        }
        
        (Vector3, Vector3) SenseTargetObjectSurface(Vector3 sensePosition)
        {
            if (_colliders.Length == 0)
            {
                Debug.LogWarning("Collider list is not assigned or empty.");
                return (Vector3.zero, Vector3.zero);
            }
            
            // 가장 가까운 Collider를 찾기 위한 변수들
            bool isClosestPointFound = false;
            Collider closestCollider = null;
            float closestDistance = float.MaxValue;
            Vector3 closestPoint = Vector3.zero;
            
            // Collider 리스트를 순회하면서 가장 가까운 Collider 찾기
            for (int cdx = 0; cdx < _colliders.Length; cdx++)
            {
                Collider _collider = _colliders[cdx];
                if (_collider.bounds.size.magnitude == 0)
                {
                    Debug.LogWarning($"Collider size is zero. Object: {_collider.name}");
                    continue;
                }
                // Collider의 표면에서 가장 가까운 점 구하기
                Vector3 point = _collider.ClosestPoint(sensePosition);
                float distance = Vector3.Distance(sensePosition, point);
                
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestCollider = _collider;
                    closestPoint = point;
                    isClosestPointFound = true;
                }
            }
            if (!isClosestPointFound)
            {
                Debug.LogWarning("Failed to find the closest collider.");
                return (Vector3.zero, Vector3.zero);
            }
            
            // 표면의 점에서의 노말 벡터를 찾기 위한 RaycastHit
            RaycastHit hit;
            Vector3 direction = (closestCollider.transform.position - closestPoint).normalized;
            if (direction == Vector3.zero)
            {
                Debug.LogWarning("Failed to get the direction vector.");
                return (closestPoint, Vector3.zero);
            }
            if (!closestCollider.Raycast(new Ray(closestPoint - direction * 0.05f, direction), out hit, 0.06f))
                // This is for the steep corner case.
                // If the corner is too steep such as pyramid top, there is chance that the ray misses the collider.
            {
                direction = (closestPoint - sensePosition).normalized;
                if (direction == Vector3.zero)
                {
                    Debug.LogWarning("Failed to get the direction vector.");
                    return (closestPoint, Vector3.zero);
                }
                if (!closestCollider.Raycast(new Ray(sensePosition, direction), out hit, 0.1f))
                    // However, if previous ray's direction is almost parallel to the collider's surface,
                    // the ray may not hit the collider.
                    // In this case, we try to shoot the ray from the sense position to the closest point.
                {
                    Debug.LogWarning($"Failed to find the normal vector From All Ray. Object: {closestCollider.name}");
                    return (Vector3.zero, Vector3.zero);
                }
            }
            return (closestPoint, hit.normal);
        }
        
        public bool AreColliderIntersecting(Collider otherCollider)
        {
            if (_colliders == null || _colliders.Length == 0)
            {
                Debug.LogWarning("Collider list is not assigned or empty.");
                return false;
            }
            foreach (Collider _collider in _colliders)
            {
                //Physics.ComputePenetration 로 구현
                if (Physics.ComputePenetration(_collider, _collider.transform.position, _collider.transform.rotation,
                    otherCollider, otherCollider.transform.position, otherCollider.transform.rotation,
                    out _, out _))
                {
                    return true;
                }
            }
            return false;
        }
        
        public List<(Vector3, Vector3, Vector3)> GetSenseResults()
        {
            return _senseResults;
        }
        
        #if UNITY_EDITOR
        void Update()
        {
            // Component의 transform 위치에서 표면의 점까지의 벡터를 시각화
            foreach (var distanceTextObject in _distanceTextObjects)
                distanceTextObject.SetActive(false);
            foreach (var closestLineRenderer in _closestLineRenderers)
                closestLineRenderer.gameObject.SetActive(false);
            foreach (var normalLineRenderer in _normalLineRenderers)
                normalLineRenderer.gameObject.SetActive(false);
            
            if (drawClosestVector)
            {
                for (int i = 0; i < _senseResults.Count; i++)
                {
                    var senseResult = _senseResults[i];
                    Vector3 sensePosition = senseResult.Item1;
                    Vector3 closestPoint = senseResult.Item2;
                    if (closestPoint == Vector3.zero)
                        continue;
                    
                    _closestLineRenderers[i].SetPosition(0, sensePosition);
                    _closestLineRenderers[i].SetPosition(1, closestPoint);
                    _closestLineRenderers[i].gameObject.SetActive(true);
                    
                    Vector3 midPoint = (sensePosition + closestPoint) / 2;
                    // 텍스트 위치 및 내용 업데이트
                    _distanceTextObjects[i].transform.position = midPoint;
                    _distanceTextObjects[i].GetComponent<TextMesh>().text =
                        ((closestPoint - sensePosition).magnitude * 100).ToString("F1");
                    _distanceTextObjects[i].SetActive(true);
                    
                    // 텍스트가 카메라를 바라보도록 설정
                    _distanceTextObjects[i].transform.LookAt(Camera.main.transform);
                    _distanceTextObjects[i].transform.Rotate(0, 180f, 0); // 텍스트가 반대로 보이지 않도록 180도 회전
                }
            }
            //
            
            // 표면의 점에서의 노말 벡터를 시각화
            if (drawNormalVector)
            {
                foreach (var senseResult in _senseResults)
                {
                    Vector3 closestPoint = senseResult.Item2;
                    Vector3 normalVector = senseResult.Item3;
                    if (closestPoint == Vector3.zero || normalVector == Vector3.zero)
                        continue;
                    
                    Vector3 normalEndPoint = closestPoint + normalVector * normalVectorLength;
                    _normalLineRenderers[_senseResults.IndexOf(senseResult)].SetPosition(0, closestPoint);
                    _normalLineRenderers[_senseResults.IndexOf(senseResult)].SetPosition(1, normalEndPoint);
                    _normalLineRenderers[_senseResults.IndexOf(senseResult)].gameObject.SetActive(true);
                }
            }
            //
        }
        #endif
    }
}