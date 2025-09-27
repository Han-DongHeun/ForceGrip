using TrainingSequence;

public class ConfigManager
{
    private static TrainConfigs configs;

    public static void ApplyAgentsInferenceManagerConfig(TrainConfigs _configs = null)
    {
        if (_configs != null)
            configs = _configs;

        AgentsInferenceManager agentsInferenceManager = AgentsInferenceManager.Instance;

        // Apply configs to AgentsInferenceManager
        agentsInferenceManager.fixedDeltaTime = configs.manualPhysicsDeltaTime;
        agentsInferenceManager.inferenceInterval = configs.manualPhysicsDeltaTime * configs.decisionPeriod;
        agentsInferenceManager.vectorObsSize = configs.vectorObsSize;
        agentsInferenceManager.onDegOffRadForAllInputs = configs.onDegOffRadForAllInputs;
        agentsInferenceManager.onDegOffRadForTorqueOutputs = configs.onDegOffRadForTorqueOutputs;
        agentsInferenceManager.inputObjVelAngVelSubtractWristVelAngVel =
            configs.inputObjVelAngVelSubtractWristVelAngVel;
        agentsInferenceManager.inputObjVelAngVelWristInverseTransform = configs.inputObjVelAngVelWristInverseTransform;
        agentsInferenceManager.actionClampRange_Deg = configs.actionClampRange_Deg;
        agentsInferenceManager.prevTriggerInputCount = configs.prevTriggerInputCount;
        agentsInferenceManager.handMaxTotalForce_Kg = configs.handMaxTotalForce_Kg;

        agentsInferenceManager.stateUseFlags = configs.stateUseFlags;
        agentsInferenceManager.stateScalingFloats = configs.stateScalingFloats;

    }
}

[System.Serializable]
public class TrainConfigs
{
    // ManualPhysicsSimulator.cs
    public float manualPhysicsDeltaTime;
    
    // AgentsTrainManager.cs
    // - Training Configs
    public int decisionPeriod;
    public int vectorObsSize;
    public bool onDegOffRadForAllInputs;
    public bool onDegOffRadForTorqueOutputs;
    public bool inputObjVelAngVelSubtractWristVelAngVel;
    public bool inputObjVelAngVelWristInverseTransform;
    public float actionClampRange_Deg;
    public int prevTriggerInputCount;
    public float handMaxTotalForce_Kg;
    
    // - State Configs
    public StateUseFlags stateUseFlags;
    public StateScalingFloats stateScalingFloats;
}