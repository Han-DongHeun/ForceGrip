using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum RGB
{
    R,
    G,
    B
}

public class PickAndPlaceTaskManager : MonoBehaviour, IEvaluationTask
{
    [Header("Object Settings")]
    public List<InnerList<GameObject>> targetObjectList;
    [ShowOnly] public List<InnerList<int>> objectIdxSequences;
    public float objectMass;
    public Transform spawnPosition;

    [Header("RGB Texture Settings")]
    [ShowOnly] public List<InnerList<RGB>> rgbSequences;

    public Texture redTexture;
    public Texture greenTexture;
    public Texture blueTexture;
    private UVCoords _uvCoords;

    [Header("Box Trigger Settings")]
    public Collider redBoxTriggerCollider;
    public Collider greenBoxTriggerCollider;
    public Collider blueBoxTriggerCollider;

    [Header("Pick And Place Task UIs")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI taskNumberText;
    public TextMeshProUGUI successCountText;
    public TextMeshProUGUI execTimeText;
    public TextMeshProUGUI explainText;
    public TextMeshProUGUI tipText;

    public GameObject expStartButton;
    public GameObject changeTaskButton;

    [Header("Evaluation Infos")]
    [ShowOnly] public int curObjectSequenceIdx;

    [ShowOnly] public RGB objectRGB;
    [ShowOnly] public GameObject currentObject;

    [ShowOnly] public int taskNumber;
    [ShowOnly] public int successCnt;
    [ShowOnly] public float taskExecTime;
    private float _taskStartTime;

    public static PickAndPlaceTaskManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;

        StandbyExperiment();
    }

    void Update()
    {
        if (currentObject != null)
        {
            taskExecTime = Time.realtimeSinceStartup - _taskStartTime;
            execTimeText.text = $"Execution Time: {taskExecTime:F2} sec";
        }

        if (currentObject != null &&
            (currentObject.transform.position.y < 0f || currentObject.transform.position.y > 2f))
        {
            ResetCurrentObject();
        }
    }

    public void StandbyExperiment()
    {
        _uvCoords = new UVCoords();

        if (objectIdxSequences.Count != rgbSequences.Count)
            throw new System.Exception("Object sequence and RGB sequence length should be the same.");
        for (int i = 0; i < objectIdxSequences.Count; i++)
        {
            if (objectIdxSequences[i].Count != rgbSequences[i].Count)
                throw new System.Exception($"Object sequence and RGB sequence length should be the same. " +
                                           $"Object sequence {i} length: {objectIdxSequences[i].Count}, " +
                                           $"RGB sequence {i} length: {rgbSequences[i].Count}");
        }

        objectIdxSequences = new List<InnerList<int>>();
        rgbSequences = new List<InnerList<RGB>>();
        for (int sdx = 0; sdx < targetObjectList.Count; sdx++)
        {
            objectIdxSequences.Add(new InnerList<int>());
            rgbSequences.Add(new InnerList<RGB>());
        }

        if (currentObject != null)
            Destroy(currentObject);

        titleText.text = "Pick and Place Task";

        taskNumberText.text = "";
        successCountText.text = "Welcome to the ForceGrip Demo!\n" +
                                "This task is Pick and Place Task.\n" +
                                "Please read the instructions below. Enjoy!";
        successCountText.fontSize -= 1f;
        execTimeText.text = "";

        curObjectSequenceIdx = 0;

        /* Korean
         explainText.text = "이 데모에서는 생성된 물체를 들어 올려 밑바닥의 색 표시를 확인한 뒤,\n" +
                            "그 색상에 맞는 상자에 물체를 넣어야 합니다.\n\n" +
                            "오른쪽 위에서 예시 영상을 볼 수 있습니다.\n" +
                            "이해했다면, 왼쪽의 Exp Start 버튼을 건드려 시작하세요.";
        */
        /* English
         explainText.text = "This task involves lifting the generated object\n" +
                            "to check the color mark on its bottom,\n" +
                            "and then placing the object into the corresponding box.\n\n" +
                            "You can see an example video in the top-right.\n" +
                            "Once you understand, touch the Exp Start button on the left to begin.";
        */
        explainText.text = "This task involves lifting the generated object\n" +
                           "to check the color mark on its bottom,\n" +
                           "and then placing the object into the corresponding box.\n\n" +
                           "You can see an example video in the top-right.\n" +
                           "Once you understand, touch the Exp Start button on the left to begin.";
        /* Korean
         tipText.text = "Tip\n" +
                        "- 다섯 손가락을 모두 사용해 집으려고 하는게 보다 안정적입니다.\n" +
                        "- 너무 강하게 쥐거나 빠르게 움직이면 물체가 튕겨나갈 수 있습니다.";
        */
        /* English
         tipText.text = "Tip\n" +
                        "- It is more stable if you try to grab using all five fingers.\n" +
                        "- If you squeeze too hard or move too quickly,\n" +
                        "the object may bounce away.";
        */
        tipText.text = "Tip\n" +
                       "- It is more stable if you try to grab using all five fingers.\n" +
                       "- If you squeeze too hard or move too quickly,\n" +
                       "the object may bounce away.";

        expStartButton.SetActive(true);
        changeTaskButton.SetActive(false);
    }

    public void StartExperiment()
    {
        _taskStartTime = Time.realtimeSinceStartup;

        (objectIdxSequences[curObjectSequenceIdx].list, rgbSequences[curObjectSequenceIdx].list) =
            EvaluationTaskSequenceCounterBalancer.PickAndPlaceTaskGenerateRGBSequence(
                targetObjectList[curObjectSequenceIdx]);

        taskNumber = 0;
        successCnt = 0;

        titleText.text = "Pick and Place Task";

        taskNumberText.text = $"Object [{taskNumber} / {objectIdxSequences[curObjectSequenceIdx].Count}]";
        successCountText.text = "Success: 0 | Fail: 0";
        successCountText.fontSize += 1f;
        execTimeText.text = "Execution Time: 0 sec";

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
        if (objectIdxSequences[curObjectSequenceIdx].Count == 0)
            throw new System.Exception("Object sequence is empty.");
        if (rgbSequences[curObjectSequenceIdx].Count == 0)
            throw new System.Exception("RGB sequence is empty.");

        if (currentObject != null)
            Destroy(currentObject);

        // Object Spawn
        int objectIdx =
            objectIdxSequences[curObjectSequenceIdx][taskNumber % objectIdxSequences[curObjectSequenceIdx].Count];

        Vector3 objectPosition = spawnPosition.position;
        Quaternion objectRotation = spawnPosition.rotation;

        currentObject = Instantiate(targetObjectList[curObjectSequenceIdx][objectIdx], objectPosition, objectRotation);
        currentObject.AddComponent<PickAndPlaceObjectTrigger>();
        currentObject.GetComponent<Rigidbody>().mass = objectMass;
        //

        // Object Color and UV Setting
        objectRGB = rgbSequences[curObjectSequenceIdx][taskNumber % objectIdxSequences[curObjectSequenceIdx].Count];

        MeshRenderer mr = currentObject.GetComponentInChildren<MeshRenderer>();
        if (objectRGB == RGB.R)
            mr.material.mainTexture = redTexture;
        else if (objectRGB == RGB.G)
            mr.material.mainTexture = greenTexture;
        else if (objectRGB == RGB.B)
            mr.material.mainTexture = blueTexture;

        string objectName = currentObject.name.Replace("(Clone)", "").Trim();
        string objectUVName = objectName.Substring(objectName.LastIndexOf("-") + 1);

        if (currentObject.name.Contains("0.5"))
        {
            mr.material.mainTextureOffset = _uvCoords.PickAndPlaceTaskObjectUVInfos[objectUVName][0].offset * 0.5f;
            mr.material.mainTextureScale = _uvCoords.PickAndPlaceTaskObjectUVInfos[objectUVName][0].tiling * 0.5f;
        }
        else if (currentObject.name.Contains("1.5"))
        {
            mr.material.mainTextureOffset = _uvCoords.PickAndPlaceTaskObjectUVInfos[objectUVName][0].offset * 1.5f;
            mr.material.mainTextureScale = _uvCoords.PickAndPlaceTaskObjectUVInfos[objectUVName][0].tiling * 1.5f;
        }
        else
        {
            mr.material.mainTextureOffset = _uvCoords.PickAndPlaceTaskObjectUVInfos[objectUVName][0].offset;
            mr.material.mainTextureScale = _uvCoords.PickAndPlaceTaskObjectUVInfos[objectUVName][0].tiling;
        }

        currentObject.transform.rotation =
            Quaternion.Euler(_uvCoords.PickAndPlaceTaskObjectUVInfos[objectUVName][0].rotation);
        //

        taskNumber++;
        taskNumberText.text = $"Object [{taskNumber} / {objectIdxSequences[curObjectSequenceIdx].Count}]";
    }

    public void EndExperiment()
    {
        print($"Experiment {curObjectSequenceIdx + 1}/{objectIdxSequences.Count} End");

        explainText.text = "Task Completed.\nTouch the Change Task button to proceed to Can Squeeze task.";

        EvaluationSoundController.Instance.PlaySuccess();

        curObjectSequenceIdx++;
        if (curObjectSequenceIdx < objectIdxSequences.Count)
            StartExperiment();
        else
            print("All Experiments End");
    }

    public void CheckBoxInsertion(Collider insertionBoxTrigger)
    {
        // Compare with object's RGB label
        bool isSuccess = false;
        if (insertionBoxTrigger == redBoxTriggerCollider)
        {
            isSuccess = objectRGB == RGB.R;
        }
        else if (insertionBoxTrigger == greenBoxTriggerCollider)
        {
            isSuccess = objectRGB == RGB.G;
        }
        else if (insertionBoxTrigger == blueBoxTriggerCollider)
        {
            isSuccess = objectRGB == RGB.B;
        }
        else
        {
            print("Wrong Box Trigger");
            return;
        }
        //

        // Show UI Feedback
        Vector3 uiOffset = new Vector3(0f, 0.2f, 0.15f);
        if (isSuccess)
        {
            successCnt++;
            print("Right");
            EvaluationUIManager.Instance.PlotCorrect(insertionBoxTrigger.transform.position + uiOffset);
        }
        else
        {
            print("Wrong");
            EvaluationUIManager.Instance.PlotWrong(insertionBoxTrigger.transform.position + uiOffset);
        }

        successCountText.text = $"Success: {successCnt} | Fail: {taskNumber - successCnt}";
        //

        // Object Destroy and Next Object Spawn
        Destroy(currentObject);

        if (taskNumber < objectIdxSequences[curObjectSequenceIdx].Count)
            NextObject();
        else
            EndExperiment();
        //
    }
}