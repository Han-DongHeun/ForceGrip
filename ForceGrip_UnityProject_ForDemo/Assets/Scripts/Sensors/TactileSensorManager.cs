using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Sensors
{
    public class TactileSensorManager : MonoBehaviour
    {
        public LayerMask layerToSense;
        public Material lineMaterial;
        
        private int _decisionPeriod;
        
        [Header("Debug")]
        public bool forceVectorVisualizeOn;
        public bool forceTextVisualizeOn;
        public float forceVectorVisualizeThreshold_KG = 0.5f;
        public float lineCmPerKG = 10f;
        public float arrowheadSize = 0.02f;
        public float lineRendererWidth = 0.015f;
        public Color forceLineColorMin = Color.yellow;
        public Color forceLineColorMax = Color.red;
        public float minForceMagnitudeForLineColor_KG = 0.5f;
        public float maxForceMagnitudeForLineColor_KG = 2f;
        public float forceTextScale = 0.002f;
        public Color forceTextColor = Color.gray;
        private List<GameObject> forceTextObjects;
        private List<LineRenderer> forceLineRenderers;
        
        [ShowOnly, SerializeField]
        private Collider[] capsuleCollidersForVis; // Only for visualization, not used in the code.
        [SerializeField] private Collider[] _capsuleColliders;
        private List<Vector3>[] _capsuleColliderForces;
        private List<Vector3>[] _capsuleColliderContactPoints;
        
        private int curForceListStackIdx;
        private List<List<Vector3>[]> _capsuleColliderForcesList;
        
        [ShowOnly, SerializeField]
        private bool[] isCapsuleColliderCollidingForVis; // Only for visualization, not used in the code.
        private bool[] _isCapsuleColliderColliding;
        
        // Only for debugging purpose on the Editor.
        private void OnValidate()
        {
            isCapsuleColliderCollidingForVis = _isCapsuleColliderColliding;
            capsuleCollidersForVis = _capsuleColliders;
        }
        
        void Awake()
        {
            // Tactile Sensor 초기화
            if (_capsuleColliders == null || _capsuleColliders.Length == 0)
            {
                _capsuleColliders = gameObject.GetComponentsInChildren<CapsuleCollider>();
            }
            _isCapsuleColliderColliding = new bool[_capsuleColliders.Length];
            _capsuleColliderForces = new List<Vector3>[_capsuleColliders.Length];
            _capsuleColliderContactPoints = new List<Vector3>[_capsuleColliders.Length];
            for (var i = 0; i < _capsuleColliders.Length; i++)
            {
                _capsuleColliderForces[i] = new List<Vector3>();
                _capsuleColliderContactPoints[i] = new List<Vector3>();
            }
            _capsuleColliderForcesList = new List<List<Vector3>[]>();
            curForceListStackIdx = -1;
            
            var articulationBodies = new List<ArticulationBody>();
            foreach (var capsuleCollider in _capsuleColliders)
            {
                var articulationBody = capsuleCollider.attachedArticulationBody;
                Assert.IsNotNull(articulationBody);
                if (!articulationBodies.Contains(articulationBody))
                {
                    articulationBodies.Add(articulationBody);
                    articulationBody.gameObject.AddComponent<CollisionCallbackController>();
                }
            }
            //
            
            if (AgentsInferenceManager.Instance != null)
            {
                _decisionPeriod = Mathf.RoundToInt(AgentsInferenceManager.Instance.inferenceInterval / Time.fixedDeltaTime);
            }
            else
            {
                _decisionPeriod = 99999;
            }
            
            // For Debugging
            forceTextObjects = new List<GameObject>();
            for (int cdx = 0; cdx < _capsuleColliders.Length; cdx++)
            {
                var forceTextObject = new GameObject($"ForceText_{cdx}");
                forceTextObject.transform.SetParent(transform.parent);
                forceTextObject.transform.localScale = new Vector3(forceTextScale, forceTextScale, forceTextScale);
                TextMesh textMesh = forceTextObject.AddComponent<TextMesh>();
                textMesh.fontSize = 100;
                textMesh.anchor = TextAnchor.LowerCenter;
                forceTextObject.SetActive(false);
                forceTextObjects.Add(forceTextObject);
            }
            
            forceLineRenderers = new List<LineRenderer>();
            for (int cdx = 0; cdx < _capsuleColliders.Length; cdx++)
            {
                var forceLineRendererObject = new GameObject($"ForceLineRenderer_{cdx}");
                forceLineRendererObject.transform.SetParent(transform.parent);
                LineRenderer lineRenderer = forceLineRendererObject.AddComponent<LineRenderer>();
                lineRenderer.startWidth = lineRendererWidth;
                lineRenderer.endWidth = lineRendererWidth;
                lineRenderer.material = lineMaterial;
                lineRenderer.receiveShadows = false;
                lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                forceLineRendererObject.SetActive(false);
                forceLineRenderers.Add(lineRenderer);
            }
            //
        }
        
        public void AddCycleForceListIdx()
        {
            curForceListStackIdx += 1;
            
            if (curForceListStackIdx >= _decisionPeriod)
            {
                curForceListStackIdx = 0;
                _capsuleColliderForcesList.Clear();
            }
            
            _capsuleColliderForcesList.Add(new List<Vector3>[_capsuleColliders.Length]);
            for (var i = 0; i < _capsuleColliders.Length; i++)
            {
                _capsuleColliderForcesList[curForceListStackIdx][i] = new List<Vector3>();
            }
        }
        
        public void CollisionStayCall(Collision collision)
        {
            var contactPoints = collision.contacts;
            foreach (var contactPoint in contactPoints)
            {
                int otherColliderLayer = contactPoint.otherCollider.gameObject.layer;
                if ((layerToSense.value & (1 << otherColliderLayer)) == 0) continue;
                
                for (int i = 0; i < _capsuleColliders.Length; i++)
                {
                    if (contactPoint.thisCollider.GetInstanceID() != _capsuleColliders[i].GetInstanceID()) continue;
                    
                    _isCapsuleColliderColliding[i] = true;
                    
                    Vector3 force = contactPoint.impulse / Time.fixedDeltaTime; 
                    _capsuleColliderContactPoints[i].Add(contactPoint.point);// since impulse is force*time
                    _capsuleColliderForces[i].Add(force);
                    _capsuleColliderForcesList[curForceListStackIdx][i].Add(force);
                    //Debug.Log($"{force.magnitude} - {force}");
                }
                break;
            }
        }
        
        public void CollisionExitCall(Collision collision)
        {
            for (int i = 0; i < _capsuleColliders.Length; i++)
            {
                if (collision.collider.name != _capsuleColliders[i].name) continue;
                _isCapsuleColliderColliding[i] = false;
                break;
            }
        }
        
        #if UNITY_EDITOR
        void Update()
        {
            // Force vector visualization
            foreach (var forceTextObject in forceTextObjects)
                forceTextObject.SetActive(false);
            foreach (var forceLineRenderer in forceLineRenderers)
                forceLineRenderer.gameObject.SetActive(false);

            if (forceVectorVisualizeOn)
            {
                for (int cdx = 0; cdx < _capsuleColliderForces.Length; cdx++)
                {
                    if (_capsuleColliderForces[cdx].Count == 0)
                    {
                        continue;
                    }
                    
                    Vector3 forceSum = Vector3.zero;
                    Vector3 contactPointMiddle = Vector3.zero;
                    for (int fdx = 0; fdx < _capsuleColliderForces[cdx].Count; fdx++)
                    {
                        forceSum += _capsuleColliderForces[cdx][fdx];
                        contactPointMiddle += _capsuleColliderContactPoints[cdx][fdx]; // Contact Point가 여러 개인 경우도 함께 처리하고 있기 때문에.
                    }
                    contactPointMiddle /= _capsuleColliderContactPoints[cdx].Count;
                    if (forceSum.magnitude < forceVectorVisualizeThreshold_KG * 9.81f)
                    {
                        continue;
                    }
                    
                    //forceLineRenderers[cdx].SetPosition(0, contactPointMiddle);
                    //forceLineRenderers[cdx]
                    //    .SetPosition(1, contactPointMiddle + forceSum / 9.81f / 10); // 10 나누면 1kg 당 10cm ray로 표현됨.
                    
                    var arrowLine = forceLineRenderers[cdx];
                    Vector3 drawStartPos = contactPointMiddle;
                    Vector3 pointer = contactPointMiddle + forceSum / 9.81f / 100 * lineCmPerKG;
                    
                    // 힘 세기에 따라 min 에서 max로 color 변화.
                    arrowLine.startColor = Color.Lerp(forceLineColorMin, forceLineColorMax,
                                                      Mathf.Clamp01((forceSum.magnitude / 9.81f - minForceMagnitudeForLineColor_KG) / maxForceMagnitudeForLineColor_KG));
                    arrowLine.endColor = Color.Lerp(forceLineColorMin, forceLineColorMax,
                                                    Mathf.Clamp01((forceSum.magnitude / 9.81f - minForceMagnitudeForLineColor_KG) / maxForceMagnitudeForLineColor_KG));
                    
                    float percentSize = (arrowheadSize / Vector3.Distance(drawStartPos, pointer));
                    arrowLine.positionCount = 4;
                    arrowLine.SetPosition(0, drawStartPos);
                    arrowLine.SetPosition(1, Vector3.Lerp(drawStartPos, pointer, 0.999f - percentSize));
                    arrowLine.SetPosition(2, Vector3.Lerp(drawStartPos, pointer, 1 - percentSize));
                    arrowLine.SetPosition(3, pointer);
                    arrowLine.widthCurve = new AnimationCurve(
                        new Keyframe(0, lineRendererWidth),
                        new Keyframe(0.999f - percentSize, lineRendererWidth),
                        new Keyframe(1 - percentSize, lineRendererWidth * 2),
                        new Keyframe(1 - percentSize, lineRendererWidth * 2),
                        new Keyframe(1, 0f));
                    
                    forceLineRenderers[cdx].gameObject.SetActive(true);
                    
                    if (forceTextVisualizeOn)
                    {
                        //mid point를 line Renderer width에 안겹치게 살짝 수직 위 위치로.
                        Vector3 textPointer =
                            pointer + Vector3.Cross(forceSum, Vector3.up).normalized * lineRendererWidth;
                        var forceTextObject = forceTextObjects[cdx];
                        
                        forceTextObject.transform.position = textPointer;
                        forceTextObject.transform.localScale =
                            new Vector3(forceTextScale, forceTextScale, forceTextScale);
                        TextMesh textMesh = forceTextObject.GetComponent<TextMesh>();
                        textMesh.text = (forceSum.magnitude / 9.81f).ToString("F2") + "KG";
                        textMesh.color = forceTextColor;
                        forceTextObject.SetActive(true);
                        
                        forceTextObject.transform.LookAt(Camera.main.transform);
                        forceTextObject.transform.Rotate(0, 180f, 0); // 텍스트가 반대로 보이지 않도록 180도 회전
                    }
                }
            }
            //
            
            #if UNITY_EDITOR
            isCapsuleColliderCollidingForVis = _isCapsuleColliderColliding;
            #endif
        }
        #endif
        
        /// <summary>
        /// 바로 이전의 physics step에서 구해진 실제 Capsule Collider에 가해진 힘을 반환합니다.
        /// 이것은 GetDecisionStepAvgForces()와 달리, 이전 physics step의 영향이 없고 바로 직전의 physics step에서 구해진 힘만을 반환합니다.
        /// </summary>
        /// <returns></returns>
        public List<Vector3>[] GetPrevPhysicsStepForces()
        {
            return _capsuleColliderForces;
        }
        
        /// <summary>
        /// Decision step 동안에 감지된 힘들의 평균 힘을 반환합니다.
        /// 이것은 GetPrevPhysicsStepForces()와 달리, Decision step 동안에 감지된 힘들의 평균 힘을 반환합니다.
        /// 매 Physics step에 저장된 힘 vector는 addition이 되어 하나의 vector로 합쳐집니다.
        /// 그리고, 그 vector들의 평균이 Capsule Collider의 개수만큼 반환됩니다.
        /// </summary>
        /// <returns></returns>
        public Vector3[] GetDecisionStepAvgForces()
        {
            Vector3[] avgForces = new Vector3[_capsuleColliders.Length];
            for (int cdx = 0; cdx < _capsuleColliders.Length; cdx++)
            {
                Vector3 avgForce = Vector3.zero;
                for (int fdx = 0; fdx < _capsuleColliderForcesList.Count; fdx++)
                {
                    Vector3 addForce = Vector3.zero;
                    if (_capsuleColliderForcesList[fdx][cdx].Count == 0) continue;
                    for (int idx = 0; idx < _capsuleColliderForcesList[fdx][cdx].Count; idx++)
                    {
                        addForce += _capsuleColliderForcesList[fdx][cdx][idx];
                    }
                    
                    avgForce += addForce / _capsuleColliderForcesList.Count;
                }
                avgForces[cdx] = avgForce;
            }
            return avgForces;
        }
        
        public void ResetCapsuleColliderForceList()
        {
            curForceListStackIdx = -1;
            _capsuleColliderForcesList.Clear();
        }

        public void ClearAllContactResults()
        {
            for (var cdx = 0; cdx < _capsuleColliders.Length; cdx++)
            {
                _capsuleColliderForces[cdx].Clear();
                _capsuleColliderContactPoints[cdx].Clear();
            }
        }
    }
}
