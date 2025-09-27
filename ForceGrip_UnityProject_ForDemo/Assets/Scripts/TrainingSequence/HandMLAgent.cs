using System;
using System.Collections.Generic;
using Sensors;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace TrainingSequence
{
    public class HandMLAgent : Agent
    {
        private float _dt;
        private float _actionClampRange_Deg;

        private bool _onDegOffRadForAllInputs;
        private bool _onDegOffRadForTorqueOutputs;
        private bool _inputObjVelAngVelSubtractWristVelAngVel;
        private bool _inputObjVelAngVelWristInverseTransform;
        private StateUseFlags _stateUseFlags;
        private StateScalingFloats _stateScalingFloats;

        [ShowOnly, SerializeField] private GameObject _curTrainObject;
        private Rigidbody _curTrainObjectRigidbody;
        private Collider[] _curTrainObjectColliders;

        private float[] _previousAction;
        private State _currentState;

        [Header("Components (GetComponent called in InitAgent)")]
        [ShowOnly, SerializeField] public HandDebuggingManager handDebuggingManager;
        [ShowOnly, SerializeField] public VoxelSensorsManager voxelSensorsManager;
        [ShowOnly, SerializeField] public SurfaceSensorManager surfaceSensorManager;
        [ShowOnly, SerializeField] public TactileSensorManager tactileSensorManager;

        [Header("Articulation Bodies")]
        public ArticulationBody wristArticulationBody;
        private Transform _wristTransform;

        [Tooltip("Joint Articulation Bodies should be in order.")]
        public ArticulationBody[] jointArticulationBodies;

        public (bool, bool, bool)[] jointArticulationBodyFreeDofs;
        private int _totalJointDofCount;

        [Tooltip("End Effector Transforms should be in order.")]
        public Transform[] endEffectorTransforms;

        private Transform[] _jointAndEndEffectorTransforms;

        protected override void Awake()
        {
            if (AgentsInferenceManager.Instance == null)
            {
                Debug.LogWarning(
                    "Both AgentsTrainManager and AgentsInferenceManager are null." +
                    $"Destroying agent {gameObject.name}.");
                DestroyImmediate(this);
            }
            else
            {
                base.Awake();
            }
        }

        public void InitAgent()
        {
            handDebuggingManager = GetComponent<HandDebuggingManager>();
            if (handDebuggingManager == null)
                throw new Exception($"HandDebuggingManager component is not found in the agent. {gameObject.name}");
            voxelSensorsManager = GetComponent<VoxelSensorsManager>();
            if (voxelSensorsManager == null)
                throw new Exception($"VoxelSensorsManager component is not found in the agent. {gameObject.name}");
            surfaceSensorManager = GetComponent<SurfaceSensorManager>();
            if (surfaceSensorManager == null)
                throw new Exception($"SurfaceSensorManager component is not found in the agent. {gameObject.name}");
            tactileSensorManager = GetComponent<TactileSensorManager>();
            if (tactileSensorManager == null)
                throw new Exception($"TactileSensorManager component is not found in the agent. {gameObject.name}");

            _dt = AgentsInferenceManager.Instance.inferenceInterval;
            _actionClampRange_Deg = AgentsInferenceManager.Instance.actionClampRange_Deg;

            _onDegOffRadForAllInputs = AgentsInferenceManager.Instance.onDegOffRadForAllInputs;
            _onDegOffRadForTorqueOutputs = AgentsInferenceManager.Instance.onDegOffRadForTorqueOutputs;
            _inputObjVelAngVelSubtractWristVelAngVel =
                AgentsInferenceManager.Instance.inputObjVelAngVelSubtractWristVelAngVel;
            _inputObjVelAngVelWristInverseTransform =
                AgentsInferenceManager.Instance.inputObjVelAngVelWristInverseTransform;
            _stateUseFlags = AgentsInferenceManager.Instance.stateUseFlags;
            _stateScalingFloats = AgentsInferenceManager.Instance.stateScalingFloats;

            wristArticulationBody.immovable = false;

            _wristTransform = wristArticulationBody.transform;

            _jointAndEndEffectorTransforms =
                new Transform[jointArticulationBodies.Length + endEffectorTransforms.Length];
            for (int jdx = 0; jdx < jointArticulationBodies.Length; jdx++)
                _jointAndEndEffectorTransforms[jdx] = jointArticulationBodies[jdx].transform;
            for (int edx = 0; edx < endEffectorTransforms.Length; edx++)
                _jointAndEndEffectorTransforms[jointArticulationBodies.Length + edx] = endEffectorTransforms[edx];

            surfaceSensorManager.AddTargetSenseTransform(_wristTransform);
            foreach (var targetSenseTransform in _jointAndEndEffectorTransforms)
                surfaceSensorManager.AddTargetSenseTransform(targetSenseTransform);

            int totalDofCount = 0;
            /*
             * Currently, an articulation body can have up to three degrees of freedom:
             * a fixed joint has no degrees of freedom;
             * a revolute joint has one rotational degree of freedom -- rotation around the X axis, called twist;
             * a prismatic joint has one translational degree of freedom -- translation along X, Y, or Z axis;
             * a spherical joint has up to three, depending on the amount of unlocked motions.
             * Currently, if only one axis is unlocked, then the amount of degrees of freedom will be reported as 1, and 3 otherwise.
             * The order of axes is as follows: first is twist, then the two swing values.
             */
            jointArticulationBodyFreeDofs = new (bool, bool, bool)[jointArticulationBodies.Length];
            for (int jdx = 0; jdx < jointArticulationBodies.Length; jdx++)
            {
                jointArticulationBodyFreeDofs[jdx] = (false, false, false);
                if (jointArticulationBodies[jdx].dofCount == 1)
                {
                    totalDofCount += 1;
                    jointArticulationBodyFreeDofs[jdx].Item3 = true; // if dofCount is 1, then it's zDrive.
                }
                else
                {
                    if (jointArticulationBodies[jdx].twistLock != ArticulationDofLock.LockedMotion)
                    {
                        totalDofCount += 1;
                        jointArticulationBodyFreeDofs[jdx].Item1 = true;
                    }

                    if (jointArticulationBodies[jdx].swingYLock != ArticulationDofLock.LockedMotion)
                    {
                        totalDofCount += 1;
                        jointArticulationBodyFreeDofs[jdx].Item2 = true;
                    }

                    if (jointArticulationBodies[jdx].swingZLock != ArticulationDofLock.LockedMotion)
                    {
                        totalDofCount += 1;
                        jointArticulationBodyFreeDofs[jdx].Item3 = true;
                    }
                }
            }

            _totalJointDofCount = totalDofCount;
        }

        public override void OnEpisodeBegin()
        {
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            _currentState = MakeCurrentState();

            // Hand
            if (_stateUseFlags.upVector)
                sensor.AddObservation(_currentState.hand.upVector * _stateScalingFloats.upVector);
            if (_stateUseFlags.forwardVector)
                sensor.AddObservation(_currentState.hand.forwardVector * _stateScalingFloats.forwardVector);

            if (_stateUseFlags.jointAndEndEffectorPositions)
                for (int jdx = 0; jdx < _currentState.hand.jointAndEndEffectorPositions.Length; jdx++)
                    sensor.AddObservation(_currentState.hand.jointAndEndEffectorPositions[jdx] *
                                          _stateScalingFloats.jointAndEndEffectorPositions);
            if (_stateUseFlags.jointDoFs)
                for (int ddx = 0; ddx < _currentState.hand.jointDoFs.Length; ddx++)
                    sensor.AddObservation(_currentState.hand.jointDoFs[ddx] * _stateScalingFloats.jointDoFs);

            if (_stateUseFlags.jointVelocities)
                for (int jdx = 0; jdx < _currentState.hand.jointVelocities.Length; jdx++)
                    sensor.AddObservation(_currentState.hand.jointVelocities[jdx] *
                                          _stateScalingFloats.jointVelocities);
            if (_stateUseFlags.jointDoFVelocities)
                for (int ddx = 0; ddx < _currentState.hand.jointDoFVelocities.Length; ddx++)
                    sensor.AddObservation(_currentState.hand.jointDoFVelocities[ddx] *
                                          _stateScalingFloats.jointDoFVelocities);
            if (_stateUseFlags.jointDoFAccelerations)
                for (int ddx = 0; ddx < _currentState.hand.jointDoFAccelerations.Length; ddx++)
                    sensor.AddObservation(_currentState.hand.jointDoFAccelerations[ddx] *
                                          _stateScalingFloats.jointDoFAccelerations);
            //

            // Object
            if (_stateUseFlags.objectVelocity)
                sensor.AddObservation(_currentState.obj.objectVelocity * _stateScalingFloats.objectVelocity);
            if (_stateUseFlags.objectAngularVelocity)
                sensor.AddObservation(_currentState.obj.objectAngularVelocity *
                                      _stateScalingFloats.objectAngularVelocity);
            if (_stateUseFlags.objectGravity)
                sensor.AddObservation(_currentState.obj.objectGravity * _stateScalingFloats.objectGravity);
            if (_stateUseFlags.objectMass)
                sensor.AddObservation(_currentState.obj.objectMass * _stateScalingFloats.objectMass);
            //

            // Global Voxel Sensor
            if (_stateUseFlags.gvs)
                for (int gdx1 = 0; gdx1 < _currentState.gvs.Length; gdx1++)
                    for (int gdx2 = 0; gdx2 < _currentState.gvs[gdx1].Length; gdx2++)
                        for (int gdx3 = 0; gdx3 < _currentState.gvs[gdx1][gdx2].Length; gdx3++)
                            sensor.AddObservation(_currentState.gvs[gdx1][gdx2][gdx3] * _stateScalingFloats.gvs);
            //

            // Local Voxel Sensor
            if (_stateUseFlags.lvs)
                for (int ldx1 = 0; ldx1 < _currentState.lvs.Length; ldx1++)
                    for (int ldx2 = 0; ldx2 < _currentState.lvs[ldx1].Length; ldx2++)
                        for (int ldx3 = 0; ldx3 < _currentState.lvs[ldx1][ldx2].Length; ldx3++)
                            for (int ldx4 = 0; ldx4 < _currentState.lvs[ldx1][ldx2][ldx3].Length; ldx4++)
                                sensor.AddObservation(_currentState.lvs[ldx1][ldx2][ldx3][ldx4] *
                                                      _stateScalingFloats.lvs);
            //

            // Surface Sensor
            if (_stateUseFlags.closestPointVectors)
                for (int idx = 0; idx < _currentState.closestPointVectors.Length; idx++)
                    sensor.AddObservation(_currentState.closestPointVectors[idx] *
                                          _stateScalingFloats.closestPointVectors);
            if (_stateUseFlags.normalVectors)
                for (int idx = 0; idx < _currentState.normalVectors.Length; idx++)
                    sensor.AddObservation(_currentState.normalVectors[idx] * _stateScalingFloats.normalVectors);
            //

            // Force Sensor
            if (_stateUseFlags.forceVectors)
                for (int cdx = 0; cdx < _currentState.forceVectors.Length; cdx++)
                    sensor.AddObservation(_currentState.forceVectors[cdx] * _stateScalingFloats.forceVectors);
            //

            // Trigger Values & Previous Action
            if (_stateUseFlags.triggerValues)
                for (int tdx = 0; tdx < _currentState.triggerValues.Length; tdx++)
                    sensor.AddObservation(_currentState.triggerValues[tdx] * _stateScalingFloats.triggerValues);
            if (_stateUseFlags.previousAction)
                for (int pdx = 0; pdx < _currentState.previousAction.Length; pdx++)
                    sensor.AddObservation(_currentState.previousAction[pdx] * _stateScalingFloats.previousAction);
            //

            handDebuggingManager.triggerValueGraph.StackGraph(_currentState.triggerValues[0]);
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            //Debug.Log($"Agent {agentNumber} OnActionReceived. Frame Index: {curFrameIndex}");
            // If you change actionspec, you should adjust it to AgentsTrainManager.cs's ActionSpec Setting part.
            if (actions.ContinuousActions.Length != _totalJointDofCount)
                throw new Exception(
                    $"Action length({actions.ContinuousActions.Length}) " +
                    $"is not matched with the total joint dof count({_totalJointDofCount})");

            // Action Output Adjust
            _previousAction = new float[_totalJointDofCount];
            List<float> targets = new List<float>();
            List<float> targetVelocities = new List<float>();
            for (int ddx = 0; ddx < _totalJointDofCount; ddx++)
            {
                float actionClamped;
                if (_onDegOffRadForTorqueOutputs)
                {
                    actionClamped =
                        Mathf.Clamp(actions.ContinuousActions[ddx], -_actionClampRange_Deg, _actionClampRange_Deg);
                }
                else
                {
                    float actionClampRange_Rad = _actionClampRange_Deg * Mathf.Deg2Rad;
                    actionClamped =
                        Mathf.Clamp(actions.ContinuousActions[ddx], -actionClampRange_Rad, actionClampRange_Rad);
                    actionClamped *= Mathf.Rad2Deg;
                }

                _previousAction[ddx] = _onDegOffRadForAllInputs ? actionClamped : actionClamped * Mathf.Deg2Rad;

                targets.Add(actionClamped);
                targetVelocities.Add(actionClamped / _dt);
            }

            AgentsInferenceManager.Instance.SetPreviousAction(_previousAction);

            SetArticulationDrives(targets, targetVelocities, _dt, additive: true);
            //
        }

        private State MakeCurrentState()
        {
            State currentState = new State();

            // Hand
            if (_stateUseFlags.upVector)
                currentState.hand.upVector = _wristTransform.up;
            if (_stateUseFlags.forwardVector)
                currentState.hand.forwardVector = _wristTransform.forward;

            if (_stateUseFlags.jointAndEndEffectorPositions)
            {
                currentState.hand.jointAndEndEffectorPositions = new Vector3[_jointAndEndEffectorTransforms.Length];
                for (int jdx = 0; jdx < _jointAndEndEffectorTransforms.Length; jdx++)
                    currentState.hand.jointAndEndEffectorPositions[jdx] = _jointAndEndEffectorTransforms[jdx].position;
                _wristTransform.InverseTransformPoints(currentState.hand.jointAndEndEffectorPositions);
            }

            if (_stateUseFlags.jointVelocities)
            {
                currentState.hand.jointVelocities = new Vector3[jointArticulationBodies.Length];
                for (int jdx = 0; jdx < jointArticulationBodies.Length; jdx++)
                    currentState.hand.jointVelocities[jdx] = jointArticulationBodies[jdx].velocity;
                _wristTransform.InverseTransformDirections(currentState.hand.jointVelocities);
            }

            int ddx = 0;
            if (_stateUseFlags.jointDoFs)
                currentState.hand.jointDoFs = new float[_totalJointDofCount];
            if (_stateUseFlags.jointDoFVelocities)
                currentState.hand.jointDoFVelocities = new float[_totalJointDofCount];
            if (_stateUseFlags.jointDoFAccelerations)
                currentState.hand.jointDoFAccelerations = new float[_totalJointDofCount];
            for (int jdx = 0; jdx < jointArticulationBodies.Length; jdx++)
            {
                if (jointArticulationBodies[jdx].dofCount == 1)
                {
                    if (_stateUseFlags.jointDoFs)
                    {
                        currentState.hand.jointDoFs[ddx] = jointArticulationBodies[jdx].jointPosition[0];
                        if (_onDegOffRadForAllInputs)
                            currentState.hand.jointDoFs[ddx] *= Mathf.Rad2Deg;
                    }

                    if (_stateUseFlags.jointDoFVelocities)
                    {
                        currentState.hand.jointDoFVelocities[ddx] = jointArticulationBodies[jdx].jointVelocity[0];
                        if (_onDegOffRadForAllInputs)
                            currentState.hand.jointDoFVelocities[ddx] *= Mathf.Rad2Deg;
                    }

                    if (_stateUseFlags.jointDoFAccelerations)
                    {
                        currentState.hand.jointDoFAccelerations[ddx] =
                            jointArticulationBodies[jdx].jointAcceleration[0];
                        if (_onDegOffRadForAllInputs)
                            currentState.hand.jointDoFAccelerations[ddx] *= Mathf.Rad2Deg;
                    }

                    ddx += 1;
                }
                else
                {
                    if (jointArticulationBodyFreeDofs[jdx].Item1)
                    {
                        if (_stateUseFlags.jointDoFs)
                        {
                            currentState.hand.jointDoFs[ddx] = jointArticulationBodies[jdx].jointPosition[0];
                            if (_onDegOffRadForAllInputs)
                                currentState.hand.jointDoFs[ddx] *= Mathf.Rad2Deg;
                        }

                        if (_stateUseFlags.jointDoFVelocities)
                        {
                            currentState.hand.jointDoFVelocities[ddx] = jointArticulationBodies[jdx].jointVelocity[0];
                            if (_onDegOffRadForAllInputs)
                                currentState.hand.jointDoFVelocities[ddx] *= Mathf.Rad2Deg;
                        }

                        if (_stateUseFlags.jointDoFAccelerations)
                        {
                            currentState.hand.jointDoFAccelerations[ddx] =
                                jointArticulationBodies[jdx].jointAcceleration[0];
                            if (_onDegOffRadForAllInputs)
                                currentState.hand.jointDoFAccelerations[ddx] *= Mathf.Rad2Deg;
                        }

                        ddx += 1;
                    }

                    if (jointArticulationBodyFreeDofs[jdx].Item2)
                    {
                        if (_stateUseFlags.jointDoFs)
                        {
                            currentState.hand.jointDoFs[ddx] = jointArticulationBodies[jdx].jointPosition[1];
                            if (_onDegOffRadForAllInputs)
                                currentState.hand.jointDoFs[ddx] *= Mathf.Rad2Deg;
                        }

                        if (_stateUseFlags.jointDoFVelocities)
                        {
                            currentState.hand.jointDoFVelocities[ddx] = jointArticulationBodies[jdx].jointVelocity[1];
                            if (_onDegOffRadForAllInputs)
                                currentState.hand.jointDoFVelocities[ddx] *= Mathf.Rad2Deg;
                        }

                        if (_stateUseFlags.jointDoFAccelerations)
                        {
                            currentState.hand.jointDoFAccelerations[ddx] =
                                jointArticulationBodies[jdx].jointAcceleration[1];
                            if (_onDegOffRadForAllInputs)
                                currentState.hand.jointDoFAccelerations[ddx] *= Mathf.Rad2Deg;
                        }

                        ddx += 1;
                    }

                    if (jointArticulationBodyFreeDofs[jdx].Item3)
                    {
                        if (_stateUseFlags.jointDoFs)
                        {
                            currentState.hand.jointDoFs[ddx] = jointArticulationBodies[jdx].jointPosition[2];
                            if (_onDegOffRadForAllInputs)
                                currentState.hand.jointDoFs[ddx] *= Mathf.Rad2Deg;
                        }

                        if (_stateUseFlags.jointDoFVelocities)
                        {
                            currentState.hand.jointDoFVelocities[ddx] = jointArticulationBodies[jdx].jointVelocity[2];
                            if (_onDegOffRadForAllInputs)
                                currentState.hand.jointDoFVelocities[ddx] *= Mathf.Rad2Deg;
                        }

                        if (_stateUseFlags.jointDoFAccelerations)
                        {
                            currentState.hand.jointDoFAccelerations[ddx] =
                                jointArticulationBodies[jdx].jointAcceleration[2];
                            if (_onDegOffRadForAllInputs)
                                currentState.hand.jointDoFAccelerations[ddx] *= Mathf.Rad2Deg;
                        }

                        ddx += 1;
                    }
                }
            }
            //

            // Object - World Coordinate
            if (_stateUseFlags.objectVelocity)
            {
                // Obj Velocity
                Vector3 objVelocity = _curTrainObjectRigidbody.velocity;
                if (_inputObjVelAngVelSubtractWristVelAngVel)
                    objVelocity = objVelocity - wristArticulationBody.velocity;
                if (_inputObjVelAngVelWristInverseTransform)
                    objVelocity = _wristTransform.InverseTransformDirection(objVelocity);
                currentState.obj.objectVelocity = objVelocity;
                //
            }

            if (_stateUseFlags.objectAngularVelocity)
            {
                // Obj Angular Velocity
                Vector3 objAngularVelocity = _curTrainObjectRigidbody.angularVelocity; // (deg)
                if (_inputObjVelAngVelSubtractWristVelAngVel)
                {
                    float objAngle = objAngularVelocity.magnitude;
                    Vector3 objAxis = objAngularVelocity.normalized;
                    Quaternion objAngleAxis = Quaternion.AngleAxis(objAngle, objAxis);

                    float wristAngle = wristArticulationBody.angularVelocity.magnitude;
                    Vector3 wristAxis = wristArticulationBody.angularVelocity.normalized;
                    Quaternion wristAngleAxis = Quaternion.AngleAxis(wristAngle, wristAxis);

                    (wristAngleAxis.GetInverse() * objAngleAxis).ToAngleAxis(
                        out float targetAngle, out Vector3 targetAxis);
                    objAngularVelocity = targetAxis * targetAngle; // Wrist relative angular velocity (deg)
                }

                if (!_onDegOffRadForAllInputs)
                    objAngularVelocity *= Mathf.Deg2Rad;

                if (_inputObjVelAngVelWristInverseTransform)
                    objAngularVelocity = _wristTransform.InverseTransformDirection(objAngularVelocity);
                currentState.obj.objectAngularVelocity = objAngularVelocity;
                //
            }

            // Obj Gravity - Wrist Coordinate
            if (_stateUseFlags.objectGravity)
            {
                currentState.obj.objectGravity = Physics.gravity;
                currentState.obj.objectGravity =
                    _wristTransform.InverseTransformDirection(currentState.obj.objectGravity);
            }
            //

            // Obj Mass
            if (_stateUseFlags.objectMass)
                currentState.obj.objectMass = _curTrainObjectRigidbody.mass;
            //

            // GVS Sensor
            if (_stateUseFlags.gvs)
                currentState.gvs = voxelSensorsManager.SenseGVS().occupancies;
            //

            // LVS Sensor
            if (_stateUseFlags.lvs)
            {
                currentState.lvs = new float[voxelSensorsManager.LocalVoxelSensors.Count][][][];
                var lvs = voxelSensorsManager.SenseLVS();
                for (int ldx = 0; ldx < lvs.Count; ldx++)
                    currentState.lvs[ldx] = lvs[ldx].occupancies;
            }
            //

            // Surface Sensor
            if (_stateUseFlags.closestPointVectors || _stateUseFlags.normalVectors)
            {
                (currentState.closestPointVectors, currentState.normalVectors) =
                    surfaceSensorManager.SenseAllCurrentTargetSurfaces();
                if (_stateUseFlags.closestPointVectors)
                    _wristTransform.InverseTransformDirections(currentState.closestPointVectors);
                if (_stateUseFlags.normalVectors)
                    _wristTransform.InverseTransformDirections(currentState.normalVectors);
            }
            //

            // Force Sensor
            if (_stateUseFlags.forceVectors)
            {
                var capsuleColliderForces = tactileSensorManager.GetDecisionStepAvgForces();
                currentState.forceVectors = new Vector3[capsuleColliderForces.Length];
                for (int cdx = 0; cdx < capsuleColliderForces.Length; cdx++)
                {
                    currentState.forceVectors[cdx] = capsuleColliderForces[cdx]; // Newton
                    float forceClampMagnitude =
                        Mathf.Clamp(currentState.forceVectors[cdx].magnitude,
                                    0f,
                                    AgentsInferenceManager.Instance.handMaxTotalForce_Kg * 9.81f *
                                    AgentsInferenceManager.Instance.forceMagnitudeLimit_Ratio);
                    currentState.forceVectors[cdx] =
                        currentState.forceVectors[cdx].normalized * forceClampMagnitude;
                }

                _wristTransform.InverseTransformDirections(currentState.forceVectors);
            }
            //

            // Trigger Values & Previous Action
            if (_stateUseFlags.triggerValues)
                currentState.triggerValues = AgentsInferenceManager.Instance.triggerInputs;
            if (_stateUseFlags.previousAction)
                currentState.previousAction = AgentsInferenceManager.Instance.previousAction;
            //

            return currentState;
        }

        public void ClearTactileSensorHeatmap()
        {
            // This should be called right before the physics update.
            // Since the tactile sensor heatmap is updated during the physics update,
            // it should be cleared previous impulse heatmap before the physics update.
            tactileSensorManager.ClearAllContactResults();
        }

        public void AddTactileSensorCycleForceListIdx()
        {
            tactileSensorManager.AddCycleForceListIdx();
        }

        public void ResetCapsuleColliderForceList()
        {
            tactileSensorManager.ResetCapsuleColliderForceList();
        }

        void Update()
        {
            if (handDebuggingManager.showInputTriggerValue)
            {
                var capsuleColliderForces = tactileSensorManager.GetPrevPhysicsStepForces();
                float forceSum = 0f;
                foreach (var capsuleColliderForce in capsuleColliderForces)
                {
                    for (int cdx = 0; cdx < capsuleColliderForce.Count; cdx++)
                    {
                        forceSum += capsuleColliderForce[cdx].magnitude;
                    }
                }
            }
        }

        public void SetArticulationDrives(List<float> targets, List<float> targetVelocities, float dt,
                                          bool additive = false)
        {
            TrainUtilities.SetArticulationDrives(targets, targetVelocities,
                                                 _totalJointDofCount, jointArticulationBodies,
                                                 jointArticulationBodyFreeDofs, dt, additive);
        }

        public void SetTargetObjectForInference(GameObject targetObject)
        {
            _curTrainObject = targetObject;
            if (_curTrainObject == null)
                return;

            _curTrainObjectRigidbody = _curTrainObject.GetComponent<Rigidbody>();
            _curTrainObjectColliders = _curTrainObject.GetComponentsInChildren<Collider>();
            surfaceSensorManager.SetTargetObject(_curTrainObject, _curTrainObjectColliders);
        }

        public float[] GetCurrentHandJointDrives_Deg()
        {
            float[] handJointDrives = new float[_totalJointDofCount];
            int ddx = 0;
            for (int jdx = 0; jdx < jointArticulationBodies.Length; jdx++)
            {
                if (jointArticulationBodies[jdx].dofCount == 1)
                {
                    handJointDrives[ddx] = jointArticulationBodies[jdx].zDrive.target;
                    ddx += 1;
                }
                else
                {
                    if (jointArticulationBodyFreeDofs[jdx].Item1)
                    {
                        handJointDrives[ddx] = jointArticulationBodies[jdx].xDrive.target;
                        ddx += 1;
                    }

                    if (jointArticulationBodyFreeDofs[jdx].Item2)
                    {
                        handJointDrives[ddx] = jointArticulationBodies[jdx].yDrive.target;
                        ddx += 1;
                    }

                    if (jointArticulationBodyFreeDofs[jdx].Item3)
                    {
                        handJointDrives[ddx] = jointArticulationBodies[jdx].zDrive.target;
                        ddx += 1;
                    }
                }
            }

            return handJointDrives;
        }
    }
}