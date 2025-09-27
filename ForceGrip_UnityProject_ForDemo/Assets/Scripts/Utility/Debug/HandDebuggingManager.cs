using System;
using System.Collections.Generic;
using System.Linq;
using PhysicsSimulation;
using TMPro;
using TrainingSequence;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class HandDebuggingManager : MonoBehaviour
{
    public bool showInputTriggerValue;
    private bool _prevShowInputTriggerValue;

    public Slider inputTriggerValueSlider;
    public TextMeshProUGUI inputTriggerValueText;
    public Slider totalForceSumSlider;
    public TextMeshProUGUI totalForceSumValueText;
    public RealTimeGraph triggerValueGraph;

    public Slider gameViewInputTriggerValueSlider;
    public TextMeshProUGUI gameViewInputTriggerValueText;
    public Slider gameViewTotalForceSumSlider;
    public TextMeshProUGUI gameViewTotalForceSumValueText;

    [Header("Hand Visualization")]
    public bool showHandSkinnedMesh;
    private bool _prevShowHandSkinnedMesh;

    public bool showHandColliders;
    private bool _prevShowHandColliders;

    private MeshRenderer[] _handColliderMeshRenderers;
    private SkinnedMeshRenderer _handSkinnedMeshRenderer;

    [Header("Editor Debugging")]
    public ArticulationBody printThisArticulationBodyInfo;

    private ArticulationBody _wristRootArticulationBody;
    public bool followHandPrefabRoot;

    void Start()
    {
        _wristRootArticulationBody = GetComponentInChildren<ArticulationBody>();

        _prevShowInputTriggerValue = showInputTriggerValue;
        inputTriggerValueSlider.transform.parent.parent.parent.gameObject.SetActive(showInputTriggerValue);

        _handColliderMeshRenderers = GetComponentsInChildren<MeshRenderer>();
        _handColliderMeshRenderers =
            _handColliderMeshRenderers.Where(meshRenderer => meshRenderer.name.Contains("CapsuleMesh")).ToArray();
        _handSkinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();

        _prevShowHandColliders = showHandColliders;
        foreach (var meshRenderer in _handColliderMeshRenderers)
            meshRenderer.gameObject.SetActive(showHandColliders);

        _prevShowHandSkinnedMesh = showHandSkinnedMesh;
        _handSkinnedMeshRenderer.gameObject.SetActive(showHandSkinnedMesh);
    }

    void Update()
    {
        if (_prevShowInputTriggerValue != showInputTriggerValue)
        {
            _prevShowInputTriggerValue = showInputTriggerValue;
            inputTriggerValueSlider.transform.parent.parent.parent.gameObject.SetActive(showInputTriggerValue);
        }

        if (_prevShowHandColliders != showHandColliders)
        {
            _prevShowHandColliders = showHandColliders;

            foreach (var meshRenderer in _handColliderMeshRenderers)
                meshRenderer.gameObject.SetActive(showHandColliders);
        }

        if (_prevShowHandSkinnedMesh != showHandSkinnedMesh)
        {
            _prevShowHandSkinnedMesh = showHandSkinnedMesh;

            _handSkinnedMeshRenderer.gameObject.SetActive(showHandSkinnedMesh);
        }
    }

#if UNITY_EDITOR
    void FixedUpdate()
    {
        if (Physics.simulationMode != SimulationMode.FixedUpdate)
            return;
        FollowHandPrefabRootOnFixedUpdate();
        if (printThisArticulationBodyInfo != null)
        {
            Debug.Log($"{printThisArticulationBodyInfo.name}-velocity: {printThisArticulationBodyInfo.velocity}");
            Debug.Log(
                $"{printThisArticulationBodyInfo.name}-angularVelocity: {printThisArticulationBodyInfo.angularVelocity}");
            if (printThisArticulationBodyInfo.dofCount == 1)
            {
                Debug.Log(
                    $"{printThisArticulationBodyInfo.name}-jointPosition: {printThisArticulationBodyInfo.jointPosition[0]}");
                Debug.Log(
                    $"{printThisArticulationBodyInfo.name}-jointVelocity: {printThisArticulationBodyInfo.jointVelocity[0]}");
                Debug.Log(
                    $"{printThisArticulationBodyInfo.name}-jointAcceleration: {printThisArticulationBodyInfo.jointAcceleration[0]}");
            }
            else
            {
                Debug.Log(
                    $"{printThisArticulationBodyInfo.name}-jointPosition: " +
                    $"{printThisArticulationBodyInfo.jointPosition[0]}, " +
                    $"{printThisArticulationBodyInfo.jointPosition[1]}, " +
                    $"{printThisArticulationBodyInfo.jointPosition[2]}");
                Debug.Log($"{printThisArticulationBodyInfo.name}-jointVelocity: " +
                          $"{printThisArticulationBodyInfo.jointVelocity[0]}, " +
                          $"{printThisArticulationBodyInfo.jointVelocity[1]}, " +
                          $"{printThisArticulationBodyInfo.jointVelocity[2]}");
                Debug.Log($"{printThisArticulationBodyInfo.name}-jointAcceleration: " +
                          $"{printThisArticulationBodyInfo.jointAcceleration[0]}, " +
                          $"{printThisArticulationBodyInfo.jointAcceleration[1]}, " +
                          $"{printThisArticulationBodyInfo.jointAcceleration[2]}");
            }

            Debug.Log(
                $"{printThisArticulationBodyInfo.name}-xDriveTarget: {printThisArticulationBodyInfo.xDrive.target}");
            Debug.Log(
                $"{printThisArticulationBodyInfo.name}-xDriveVelocity: {printThisArticulationBodyInfo.xDrive.targetVelocity}");
            Debug.Log(
                $"{printThisArticulationBodyInfo.name}-yDriveTarget: {printThisArticulationBodyInfo.yDrive.target}");
            Debug.Log(
                $"{printThisArticulationBodyInfo.name}-yDriveVelocity: {printThisArticulationBodyInfo.yDrive.targetVelocity}");
            Debug.Log(
                $"{printThisArticulationBodyInfo.name}-zDriveTarget: {printThisArticulationBodyInfo.zDrive.target}");
            Debug.Log(
                $"{printThisArticulationBodyInfo.name}-zDriveVelocity: {printThisArticulationBodyInfo.zDrive.targetVelocity}");
        }
    }

    void FollowHandPrefabRootOnFixedUpdate()
    {
        if (followHandPrefabRoot)
        {
            var wristRootTransform = _wristRootArticulationBody.transform;
            _wristRootArticulationBody.velocity =
                HandPhysicsSimulator.Instance.CalcWristVelocity(wristRootTransform.position, transform.position);
            _wristRootArticulationBody.angularVelocity =
                HandPhysicsSimulator.Instance.CalcWristAngularVelocity(wristRootTransform.rotation, transform.rotation);
        }
    }
#endif

    public void UpdateInputTriggerValue(float triggerValue, float triggerValueForce, float totalForceSumTriggerValue,
                                        float totalForceSum)
    {
        inputTriggerValueSlider.value = Mathf.Clamp01(triggerValue);
        string triggerText = triggerValue.ToString("F3") + "\n" + triggerValueForce.ToString("F2") + "kg";
        inputTriggerValueText.text = triggerText;

        totalForceSumSlider.value = totalForceSumTriggerValue;
        string totalForceSumText =
            totalForceSumTriggerValue.ToString("F3") + "\n" + totalForceSum.ToString("F2") + "kg";
        totalForceSumValueText.text = totalForceSumText;

        triggerValueGraph.AdjustGraph();

        gameViewInputTriggerValueSlider.value = Mathf.Clamp01(triggerValue);
        gameViewInputTriggerValueText.text = triggerText;
        gameViewTotalForceSumSlider.value = totalForceSumTriggerValue;
        gameViewTotalForceSumValueText.text = totalForceSumText;
    }
}