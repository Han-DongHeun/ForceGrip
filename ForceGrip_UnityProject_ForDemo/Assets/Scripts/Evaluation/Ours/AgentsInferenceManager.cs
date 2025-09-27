using System.Collections.Generic;
using System.Linq;
using PhysicsSimulation;
using TrainingSequence;
using Unity.Barracuda;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;

[DefaultExecutionOrder(-100)] // Earlier than HandMLAgent Scripts
public class AgentsInferenceManager : MonoBehaviour
{
    [Tooltip("Model Settings")]
    public HandMLAgent handMLAgent;
    public BoxCollider graspableArea;

    [Header("Debug Inputs")]
    public bool bendFingersWithoutObject;
    public Transform vrControllerTransform;
    public Vector3 vrControllerRotationOffset;
    public GameObject OVRCameraRigObject;
    public Transform centerEyeAnchor;
    public Transform VrCameraStablePositionReference;

    public bool handFollowThisObject;
    [Range(0, 1)] public float currentTriggerValue;

    [Header("Parameters Settings")] public NNModel onnxModel;
    [SerializeField] public TextAsset unityEnvTrainConfig;

    [Header("Parameters Settings: (From UnityEnvTrainConfig)")]
    [ShowOnly] public float fixedDeltaTime;

    [ShowOnly] public float inferenceInterval;
    private float _inferenceTimer;

    [ShowOnly] public int prevTriggerInputCount;
    [ShowOnly] public int vectorObsSize;

    [ShowOnly] public bool onDegOffRadForAllInputs;
    [ShowOnly] public bool onDegOffRadForTorqueOutputs;
    [ShowOnly] public bool inputObjVelAngVelSubtractWristVelAngVel;
    [ShowOnly] public bool inputObjVelAngVelWristInverseTransform;
    [ShowOnly] public StateUseFlags stateUseFlags;
    [ShowOnly] public StateScalingFloats stateScalingFloats;

    [ShowOnly] public float actionClampRange_Deg;
    [ShowOnly] public float handMaxTotalForce_Kg;

    [Header("Parameters Settings: (Manual)")]
    public float totalForceAdjust_Kg;

    public float forceMagnitudeLimit_Ratio;
    public float bendSpeedPerSec_Deg;
    public ArticulationBody[] bendExcludedArticulationBodies;

    public LayerMask toBeSensedLayersMask;
    public int interactiveObjectLayerNumber;
    public int oursInferenceTargetObjectOnlyLayerNumber;

    [Header("Current Infos")]
    [ShowOnly] public float[] triggerInputs;
    [ShowOnly] public float[] previousAction;

    [ShowOnly, SerializeField] private GameObject _targetObject;

    public GameObject targetObject
    {
        get => _targetObject;
        set
        {
            if (_targetObject != null)
            {
                _targetObject.layer = interactiveObjectLayerNumber;
                foreach (Transform child in _targetObject.transform)
                {
                    child.gameObject.layer = interactiveObjectLayerNumber;
                }
            }

            _targetObject = value;
            if (_targetObject != null)
            {
                _targetObject.layer = oursInferenceTargetObjectOnlyLayerNumber;
                // target의 자식들까지.
                foreach (Transform child in _targetObject.transform)
                {
                    child.gameObject.layer = oursInferenceTargetObjectOnlyLayerNumber;
                }
            }

            handMLAgent.SetTargetObjectForInference(value);
        }
    }

    [ShowOnly, SerializeField] public int objectSenseCnt;
    private Dictionary<GameObject, int> sensedObjects = new Dictionary<GameObject, int>();

    private BehaviorParameters behaviorParameters;
    private List<float> allJointAngularDampings;

    public static AgentsInferenceManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;

        if (unityEnvTrainConfig != null)
        {
            TrainConfigs trainConfigs = JsonUtility.FromJson<TrainConfigs>(unityEnvTrainConfig.text);
            ConfigManager.ApplyAgentsInferenceManagerConfig(trainConfigs);
        }

        behaviorParameters = handMLAgent.GetComponent<BehaviorParameters>();
        behaviorParameters.BrainParameters.VectorObservationSize = vectorObsSize;
        behaviorParameters.BrainParameters.ActionSpec = ActionSpec.MakeContinuous(23);
        behaviorParameters.Model = onnxModel;
    }

    public void Start()
    {
        _inferenceTimer = 0;

        var graspableAreaCollisionCallback = graspableArea.gameObject.AddComponent<CollisionCallback>();
        graspableAreaCollisionCallback.AddOnTriggerEnterListener((Collider sensedCollider) =>
        {
            if (toBeSensedLayersMask == (toBeSensedLayersMask | (1 << sensedCollider.gameObject.layer)))
            {
                GameObject sensedObject = sensedCollider.gameObject;
                Rigidbody sensedRigidbody = sensedCollider.GetComponent<Rigidbody>();
                if (sensedRigidbody == null)
                {
                    if (sensedCollider.transform.parent != null)
                    {
                        sensedRigidbody = sensedCollider.transform.parent.GetComponent<Rigidbody>();
                        sensedObject = sensedCollider.transform.parent.gameObject;
                        if (sensedRigidbody == null)
                        {
                            Debug.LogWarning(
                                $"No Rigidbody component found in the sensed object and its parent. ({sensedCollider.name})");
                            return;
                        }
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"No Rigidbody component found in the sensed object. ({sensedCollider.name})");
                        return;
                    }
                }

                if (!sensedObjects.ContainsKey(sensedObject))
                    sensedObjects.Add(sensedObject, 1);
                else
                    sensedObjects[sensedObject]++;

                targetObject = DetermineTargetObject();
            }
        });
        graspableAreaCollisionCallback.AddOnTriggerExitListener((Collider sensedCollider) =>
        {
            if (toBeSensedLayersMask == (toBeSensedLayersMask | (1 << sensedCollider.gameObject.layer)))
            {
                GameObject sensedObject = sensedCollider.gameObject;
                Rigidbody sensedRigidbody = sensedCollider.GetComponent<Rigidbody>();
                if (sensedRigidbody == null)
                {
                    if (sensedCollider.transform.parent != null)
                    {
                        sensedRigidbody = sensedCollider.transform.parent.GetComponent<Rigidbody>();
                        sensedObject = sensedCollider.transform.parent.gameObject;
                        if (sensedRigidbody == null)
                        {
                            return;
                        }
                    }
                    else
                    {
                        return;
                    }
                }

                if (sensedObjects.ContainsKey(sensedObject))
                {
                    sensedObjects[sensedObject]--;
                    if (sensedObjects[sensedObject] == 0)
                    {
                        sensedObjects.Remove(sensedObject);
                    }
                }

                targetObject = DetermineTargetObject();
            }
        });

        triggerInputs = new float[prevTriggerInputCount + 1];

        Academy.Instance.AutomaticSteppingEnabled = false;

        handMLAgent.InitAgent();

        handMLAgent.voxelSensorsManager.layersToBeIgnored = ~(1 << oursInferenceTargetObjectOnlyLayerNumber);
        handMLAgent.tactileSensorManager.layerToSense = 1 << oursInferenceTargetObjectOnlyLayerNumber;

        GameObject dummyTargetObject = Instantiate(new GameObject(), Vector3.zero, Quaternion.identity);
        dummyTargetObject.name = "DummyTargetObject";
        Rigidbody dummyRigidbody = dummyTargetObject.AddComponent<Rigidbody>();
        dummyRigidbody.isKinematic = true;
        dummyRigidbody.useGravity = true;
        Collider dummyCollider = dummyTargetObject.AddComponent<BoxCollider>();
        dummyCollider.isTrigger = true;
        // To load the model right at the start, one inference needs to be run.
        // However, since there is no target object at that point, an error occurs in HandMLAgent.
        // So we just insert a dummy instead.
        targetObject = dummyTargetObject;

        allJointAngularDampings = new List<float>();
        for (int jdx = 0; jdx < handMLAgent.jointArticulationBodies.Length; jdx++)
            allJointAngularDampings.Add(handMLAgent.jointArticulationBodies[jdx].angularDamping);

        handMLAgent.AddTactileSensorCycleForceListIdx();
        Inference(currentTriggerValue);
        handMLAgent.ResetCapsuleColliderForceList();

        dummyTargetObject.SetActive(false);

        OVRCameraRigObject.transform.position +=
            VrCameraStablePositionReference.position - centerEyeAnchor.position;
        Vector3 forward = OVRCameraRigObject.transform.forward;
        float worldYRotationOfRig = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        OVRCameraRigObject.transform.rotation = Quaternion.Euler(
            0,
            worldYRotationOfRig +
            VrCameraStablePositionReference.rotation.eulerAngles.y - centerEyeAnchor.rotation.eulerAngles.y,
            0);
    }

    private void Update()
    {
        if (OVRInput.Get(OVRInput.Button.One, OVRInput.Controller.RTouch))
        {
            OVRCameraRigObject.transform.position +=
                VrCameraStablePositionReference.position - centerEyeAnchor.position;
            Vector3 forward = OVRCameraRigObject.transform.forward;
            float worldYRotationOfRig = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            OVRCameraRigObject.transform.rotation = Quaternion.Euler(
                0,
                worldYRotationOfRig +
                VrCameraStablePositionReference.rotation.eulerAngles.y - centerEyeAnchor.rotation.eulerAngles.y,
                0);
        }
    }

    void FixedUpdate()
    {
        objectSenseCnt = sensedObjects.Count;

        if (!handFollowThisObject)
        {
            if (OVRInput.IsControllerConnected(OVRInput.Controller.RTouch))
            {
                currentTriggerValue = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
            }
            else
            {
                currentTriggerValue = 0;
            }
        }

        WristMovement();

        _inferenceTimer += Time.fixedDeltaTime;
        if (_inferenceTimer >= inferenceInterval)
        {
            if (objectSenseCnt > 0 && currentTriggerValue > 0)
            {
                Inference(currentTriggerValue * totalForceAdjust_Kg / handMaxTotalForce_Kg);
            }
            else
            {
                handMLAgent.handDebuggingManager.triggerValueGraph.StackGraph(
                    currentTriggerValue * totalForceAdjust_Kg / handMaxTotalForce_Kg);
            }

            float triggerValueMean = currentTriggerValue * totalForceAdjust_Kg / handMaxTotalForce_Kg;
            var capsuleColliderForces = handMLAgent.tactileSensorManager.GetPrevPhysicsStepForces();
            float forceSum = 0f;
            foreach (var capsuleColliderForce in capsuleColliderForces)
            {
                for (int cdx = 0; cdx < capsuleColliderForce.Count; cdx++)
                {
                    forceSum += capsuleColliderForce[cdx].magnitude;
                }
            }

            handMLAgent.handDebuggingManager.UpdateInputTriggerValue(
                triggerValueMean,
                triggerValueMean *
                handMaxTotalForce_Kg,
                forceSum / handMaxTotalForce_Kg / 9.81f,
                forceSum / 9.81f);

            handMLAgent.ResetCapsuleColliderForceList();
            _inferenceTimer = 0;
        }

        if (objectSenseCnt == 0 && !bendFingersWithoutObject)
        {
            BendFingersWhenWithoutObject(0f);
        }
        else if (currentTriggerValue == 0 || objectSenseCnt == 0)
        {
            BendFingersWhenWithoutObject(currentTriggerValue);
        }
        else if (CanSqueezeTaskManager.Instance != null && CanSqueezeTaskManager.Instance.isActiveAndEnabled)
        {
            var colliderForceVectors = handMLAgent.tactileSensorManager.GetPrevPhysicsStepForces();
            foreach (var colliderForceVector in colliderForceVectors)
            {
                for (int cdx = 0; cdx < colliderForceVector.Count; cdx++)
                {
                    CanSqueezeTaskManager.Instance.AddForceVector(colliderForceVector[cdx]);
                }
            }
        }

        handMLAgent.ClearTactileSensorHeatmap();
        handMLAgent.AddTactileSensorCycleForceListIdx();
    }

    GameObject DetermineTargetObject()
    {
        if (sensedObjects.Count == 0)
            return null;

        float minClosestDistance = float.MaxValue;
        GameObject targetObject = null;
        foreach (var sensedObject in sensedObjects)
        {
            Collider[] colliders = sensedObject.Key.GetComponentsInChildren<Collider>();
            if (colliders == null || colliders.Length == 0)
            {
                Debug.LogWarning("Collider list is empty or not assigned.");
                return null;
            }

            for (int cdx = 0; cdx < colliders.Length; cdx++)
            {
                Vector3 closestPoint = colliders[cdx].ClosestPoint(graspableArea.transform.position);
                float closestDistance = Vector3.Distance(graspableArea.transform.position, closestPoint);
                if (closestDistance < minClosestDistance)
                {
                    minClosestDistance = closestDistance;
                    targetObject = sensedObject.Key;
                }
            }
        }

        return targetObject;
    }

    void Inference(float triggerValue)
    {
        if (targetObject == null)
        {
            objectSenseCnt = 0;
            sensedObjects.Clear();
            return;
        }

        for (int jdx = 0; jdx < handMLAgent.jointArticulationBodies.Length; jdx++)
            handMLAgent.jointArticulationBodies[jdx].angularDamping = allJointAngularDampings[jdx];

        for (int tdx = prevTriggerInputCount; tdx > 0; tdx--)
            triggerInputs[tdx] = triggerInputs[tdx - 1];
        triggerInputs[0] = triggerValue;

        handMLAgent.RequestDecision();
        Academy.Instance.EnvironmentStep();
    }

    public void BendFingersWhenWithoutObject(float bendRatio)
    {
        bendRatio = Mathf.Clamp01(bendRatio);

        var jointArticulationBodies = handMLAgent.jointArticulationBodies;
        var jointArticulationBodyFreeDofs = handMLAgent.jointArticulationBodyFreeDofs;
        var currentDofs = handMLAgent.GetCurrentHandJointDrives_Deg();

        var targetDofs = new float[currentDofs.Length];
        var targetDofVelocities = new float[currentDofs.Length];

        float bendSpeed_Deg = bendSpeedPerSec_Deg * Time.fixedDeltaTime;

        int ddx = 0;
        for (int jdx = 0; jdx < jointArticulationBodies.Length; jdx++)
        {
            bool isExcluded = bendExcludedArticulationBodies.Contains(jointArticulationBodies[jdx]);
            float lowerLimit = jointArticulationBodies[jdx].zDrive.lowerLimit;
            float upperLimit = jointArticulationBodies[jdx].zDrive.upperLimit;

            void ProcessDof(float targetBaseValue)
            {
                currentDofs[ddx] = Mathf.Clamp(currentDofs[ddx], lowerLimit, upperLimit);
                float targetDof = Mathf.Clamp(targetBaseValue, currentDofs[ddx] - bendSpeed_Deg,
                                              currentDofs[ddx] + bendSpeed_Deg);
                targetDof = Mathf.Clamp(targetDof, lowerLimit, upperLimit);
                targetDofs[ddx] = targetDof;
                targetDofVelocities[ddx] = (targetDof - currentDofs[ddx]) / Time.fixedDeltaTime;
                ddx += 1;
            }

            if (jointArticulationBodyFreeDofs[jdx].Item1)
                ProcessDof(0);
            if (jointArticulationBodyFreeDofs[jdx].Item2)
                ProcessDof(0);
            if (jointArticulationBodyFreeDofs[jdx].Item3)
                ProcessDof(isExcluded ? 0 : lowerLimit * bendRatio);
        }

        for (int jdx = 0; jdx < handMLAgent.jointArticulationBodies.Length; jdx++)
            handMLAgent.jointArticulationBodies[jdx].angularDamping = 10f;

        float[] previousActions = new float[targetDofs.Length];
        for (int adx = 0; adx < targetDofs.Length; adx++)
        {
            previousActions[adx] = targetDofs[adx] - currentDofs[adx];
            previousActions[adx] =
                onDegOffRadForAllInputs ? previousActions[adx] : previousActions[adx] * Mathf.Deg2Rad;
        }

        SetPreviousAction(previousActions);
        handMLAgent.SetArticulationDrives(targetDofs.ToList(), targetDofVelocities.ToList(), Time.fixedDeltaTime);
    }

    public void SetPreviousAction(float[] action)
    {
        previousAction = action;
    }

    void WristMovement()
    {
        if (handFollowThisObject)
        {
            var wristArticulationBody = handMLAgent.wristArticulationBody;
            var wristRootTransform = wristArticulationBody.transform;

            wristArticulationBody.velocity =
                HandPhysicsSimulator.Instance.CalcWristVelocity(wristRootTransform.position, transform.position);

            wristArticulationBody.angularVelocity =
                HandPhysicsSimulator.Instance.CalcWristAngularVelocity(wristRootTransform.rotation, transform.rotation);
        }
        else if (vrControllerTransform != null)
        {
            var wristArticulationBody = handMLAgent.wristArticulationBody;
            var wristRootTransform = wristArticulationBody.transform;

            if (OVRInput.IsControllerConnected(OVRInput.Controller.RTouch))
            {
                wristArticulationBody.velocity =
                    HandPhysicsSimulator.Instance.CalcWristVelocity(
                        wristRootTransform.position, vrControllerTransform.position);

                wristArticulationBody.angularVelocity =
                    HandPhysicsSimulator.Instance.CalcWristAngularVelocity(
                        wristRootTransform.rotation,
                        vrControllerTransform.rotation * Quaternion.Euler(vrControllerRotationOffset));
            }
            else
            {
                wristArticulationBody.velocity = Vector3.zero;
                wristArticulationBody.angularVelocity = Vector3.zero;
            }
        }
    }

    public void ClearSensedObjects()
    {
        sensedObjects.Clear();
        targetObject = null;
    }
}