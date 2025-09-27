using System.Collections.Generic;
using System.Linq;
using Accord.Math;
using UnityEngine;

public class EvaluationTaskSequenceCounterBalancer
{
    public static (List<int>, List<RGB>) PickAndPlaceTaskGenerateRGBSequence(List<GameObject> targetObjectList)
    {
        Dictionary<GameObject, Dictionary<RGB, bool>> targetObjectRGBSelected =
            new Dictionary<GameObject, Dictionary<RGB, bool>>();

        foreach (GameObject targetObject in targetObjectList)
        {
            Dictionary<RGB, bool> rgbSelected = new Dictionary<RGB, bool>
            {
                { RGB.R, false },
                { RGB.G, false },
                { RGB.B, false }
            };
            targetObjectRGBSelected.Add(targetObject, rgbSelected);
        }

        List<int> targetObjectSequence = new List<int>();
        List<RGB> rgbSequence = new List<RGB>();

        List<int> targetObjectRandomizedSequence = Enumerable.Range(0, targetObjectList.Count).ToList();
        targetObjectRandomizedSequence.Shuffle();
        targetObjectSequence.AddRange(targetObjectRandomizedSequence);

        foreach (int targetObjectIndex in targetObjectRandomizedSequence)
        {
            GameObject targetObject = targetObjectList[targetObjectIndex];
            Dictionary<RGB, bool> rgbSelected = targetObjectRGBSelected[targetObject];

            bool isRGBSelected = false;
            while (!isRGBSelected)
            {
                RGB randomRGB = (RGB)Random.Range(0, 3);
                if (!rgbSelected[randomRGB])
                {
                    rgbSelected[randomRGB] = true;
                    rgbSequence.Add(randomRGB);
                    isRGBSelected = true;
                }
            }
        }

        return (targetObjectSequence, rgbSequence);
    }

    public static CanSqueezeStep[] CanSqueezeTaskGenerateSqueezeLevelSequence(int repeatCount, int maxLevel)
    {
        CanSqueezeStep[] targetSqueezeStepSequence = new CanSqueezeStep[repeatCount * maxLevel];
        for (int rdx = 0; rdx < repeatCount; rdx++)
        {
            var squeezeLevelSequence = Enumerable.Range(0, maxLevel).Select(x => (CanSqueezeStep)x).ToArray();
            squeezeLevelSequence.Shuffle();
            squeezeLevelSequence.CopyTo(targetSqueezeStepSequence, rdx * maxLevel);
        }

        return targetSqueezeStepSequence;
    }
}