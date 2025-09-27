using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

public enum CanSqueezeStep
{
    Zero,
    One,
    Two,
    Three,
    Four,
    Five,
    Six,
    Seven,
    Eight,
    Nine,
    Ten
}

public class CanSqueezeTaskManager : MonoBehaviour, IEvaluationTask
{
    [Header("Task Settings")]
    public int repeatCount;
    public float maxForce_kgf;
    [ShowOnly] public CanSqueezeStep[] targetSqueezeStepSequence;
    [SerializeField, ShowOnly] private float[] _targetMaxForces;

    [Header("Object Settings")]
    public GameObject canReferenceObject;
    public Transform spawnPosition;
    public Collider trashCanInsideCollider;

    [Header("UI Elements")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI explainText;
    public TextMeshProUGUI taskNumberText;
    public TextMeshProUGUI tipText;

    public CanSqueezeStepUI stepUI;

    public GameObject expStartButton;
    public GameObject changeTaskButton;

    [Header("Evaluation Infos")]
    [ShowOnly] public int taskNumber = 0;

    [ShowOnly] public GameObject currentObject;
    private CanObjectManager _currentCanObjectManager;
    [ShowOnly] public CanSqueezeStep currentStep;
    [ShowOnly] public CanSqueezeStep targetStep;
    [SerializeField, ShowOnly] private float _totalForceMagnitude;

    public static CanSqueezeTaskManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;

        StandbyExperiment();
    }

    public void StandbyExperiment()
    {
        _targetMaxForces = new float[Enum.GetValues(typeof(CanSqueezeStep)).Length - 1];
        for (int i = 0; i < _targetMaxForces.Length; i++)
        {
            _targetMaxForces[i] = maxForce_kgf * ((float)i + 1) / (_targetMaxForces.Length);
        }

        targetSqueezeStepSequence =
            EvaluationTaskSequenceCounterBalancer.CanSqueezeTaskGenerateSqueezeLevelSequence(
                repeatCount, Enum.GetValues(typeof(CanSqueezeStep)).Length);

        if (currentObject != null)
            Destroy(currentObject);

        titleText.text = "Can Squeeze Task";
        taskNumber = 0;
        taskNumberText.text = $"Task [{taskNumber} / {targetSqueezeStepSequence.Length}]";

        /* Korean
         explainText.text = "이 데모에서는 생성된 캔을 찌그러트려야 하는 정도가 앞에 표시 됩니다.\n" +
                            "트리거 버튼을 조심스럽게 조작하여 최대한 세밀하게 힘을 조정해보세요.\n" +
                            "오른쪽 위에서 예시 영상을 볼 수 있습니다.\n" +
                            "이해했다면, 왼쪽의 Exp Start 버튼을 건드려 시작하세요.";
        */
        /* English
         explainText.text = "This task involves squeezing the generated can\n" +
                            "to the level indicated on the front UI\n" +
                            "by carefully adjusting the trigger button to control the applied force.\n\n" +
                            "You can see an example video in the top-right.\n" +
                            "Once you understand, touch the Exp Start button on the left to begin.";
        */
        explainText.text = "This task involves squeezing the generated can\n" +
                           "to the level indicated on the front UI\n" +
                           "by carefully adjusting the trigger button to control the applied force.\n\n" +
                           "You can see an example video in the top-right.\n" +
                           "Once you understand, touch the Exp Start button on the left to begin.";

        /* Korean
         tipText.text = "Tip\n" +
                        "- 캔을 바닥에 누르는 힘도 캔을 찌그러트립니다.\n" +
                        "최대한 손으로 캔을 누르지 않도록 하세요.\n" +
                        "- 힘을 주기 전에 안정적인 자세로\n" +
                        "캔을 공중에 들어올리는 것이 우선입니다.";
        */
        /* English
         tipText.text = "Tip\n" +
                        "- Pressing the can against the floor can also crush it.\n" +
                        "Try not to press the can with your hand.\n" +
                        "- It is better to first lift the can into the air\n" +
                        "and hold it stably before applying force.";
        */
        tipText.text = "Tip\n" +
                       "- Pressing the can against the floor can also crush it.\n" +
                       "Try not to press the can with your hand.\n" +
                       "- It is better to first lift the can into the air\n" +
                       "and hold it stably before applying force.";

        stepUI.OffAllSteps();
        expStartButton.SetActive(true);
        changeTaskButton.SetActive(false);
    }

    public void StartExperiment()
    {
        titleText.text = "Can Squeeze Task";
        taskNumber = 0;
        taskNumberText.text = $"Task [{taskNumber} / {targetSqueezeStepSequence.Length}]";

        /* Korean
         explainText.text = "컨트롤러의 (A) 버튼을 눌러 시야를 재정렬하세요.\n" +
                            "검지 트리거 버튼을 누르는 깊이를 조절하여\n" +
                            "힘을 조절하면서 물체를 집으세요.\n" +
                            "물체가 손이 닿지 않는 곳으로 떨어지면 오른쪽의 Object Reset 버튼을 건드리세요.";
        */
        /* English
         explainText.text = "Press the (A) button on the controller to realign your view.\n" +
                            "Adjust the depth of the index trigger button\n" +
                            "to grab the object with force control.\n" +
                            "Touch the Object Reset button on the right if the object falls out of reach.";
        */
        explainText.text = "Press the (A) button on the controller to realign your view.\n" +
                           "Adjust the depth of the index trigger button\n" +
                           "to grab the object with force control.\n" +
                           "Touch the Object Reset button on the right if the object falls out of reach.";

        NextObject();
    }

    public void ResetCurrentObject()
    {
        if (currentObject != null)
            Destroy(currentObject);
        else
            return;

        taskNumber--;
        if (taskNumber < 0)
            taskNumber = 0;

        NextObject();
    }

    public void NextObject()
    {
        if (targetSqueezeStepSequence.Length == 0)
            throw new System.Exception("targetSqueezeSequence is empty.");

        if (currentObject != null)
            Destroy(currentObject);

        // Object Spawn
        currentObject = Instantiate(canReferenceObject, spawnPosition.position, spawnPosition.rotation);
        _currentCanObjectManager = currentObject.GetComponent<CanObjectManager>();
        _currentCanObjectManager.SetCanSqueezedStep(CanSqueezeStep.Zero);

        currentStep = CanSqueezeStep.Zero;
        targetStep = targetSqueezeStepSequence[taskNumber % targetSqueezeStepSequence.Length];
        stepUI.SetStep(currentStep, targetStep);
        //

        AgentsInferenceManager.Instance.ClearSensedObjects();

        taskNumber++;
        taskNumberText.text = $"Task [{taskNumber} / {targetSqueezeStepSequence.Length}]";
    }

    void FixedUpdate()
    {
        if (currentObject == null)
            return;

        for (int tdx = 0; tdx <= _targetMaxForces.Length; tdx++)
        {
            if (tdx == _targetMaxForces.Length || _totalForceMagnitude < _targetMaxForces[tdx])
            {
                if (currentStep < (CanSqueezeStep)tdx)
                {
                    currentStep = (CanSqueezeStep)tdx;
                    stepUI.SetStep(currentStep, targetStep);
                    _currentCanObjectManager.SetCanSqueezedStep(currentStep);
                }

                break;
            }
        }

        _totalForceMagnitude = 0;

        if (currentObject != null &&
            (currentObject.transform.position.y < 0f || currentObject.transform.position.y > 2f))
        {
            ResetCurrentObject();
        }
    }

    public void CheckTrashCanInsertion(Collider isThisTrashCan)
    {
        if (isThisTrashCan != trashCanInsideCollider)
            return;

        EvaluationSoundController.Instance.PlayCanTrash(currentObject.transform.position);

        Destroy(currentObject);

        // Next Task
        if (taskNumber >= targetSqueezeStepSequence.Length)
        {
            EndExperiment();
        }
        else
        {
            NextObject();
        }
        //
    }

    public void EndExperiment()
    {
        print("Experiment End");

        EvaluationSoundController.Instance.PlaySuccess();

        explainText.text = "Task Completed.\nTouch the Change Task button to proceed to Pick And Place task.";

        stepUI.OffAllSteps();
    }

    public void AddForceVector(Vector3 forceVector_WorldCoordinate_Newton)
    {
        if (AgentsInferenceManager.Instance != null)
        {
            if (currentObject != null && AgentsInferenceManager.Instance.targetObject == currentObject)
            {
                _totalForceMagnitude += forceVector_WorldCoordinate_Newton.magnitude / 9.81f; // N -> kgf
            }
        }
    }
}