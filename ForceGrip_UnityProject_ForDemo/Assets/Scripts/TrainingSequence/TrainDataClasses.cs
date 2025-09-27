using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TrainingSequence
{
    [Serializable]
    public class TrainCandidate
    {
        public int trainDataIndex;
        public int startFrameIndex;
        public float selectedProbAccumulated;
        public List<float> rewardList;
        public bool selectable;
    }

    [Serializable]
    public class TrainCandidateList // Only used for json serialization saving.
    {
        public List<TrainCandidate> trainCandidates;
    }

    public class TrainData
    {
        public List<float> trainingTriggerList;

        public Wrist wrist = new Wrist();
        public Hand hand = new Hand();
        public Object obj = new Object();

        public int nFrames;
        public int nJoints;
        public int nVertices;
        public Vector3 scale;
        public List<bool> isGVSSensored;
        public List<bool> isObjMoving;
        public bool isSensedData;

        public class Wrist
        {
            public List<Vector3> wristPositions;
            public List<Quaternion> wristRotations;
            public List<Vector3> wristLinearVelocities;
            public List<Vector3> wristAngularVelocities; // deg/s
        }

        public class Hand
        {
            public List<List<Vector3>> jointAndEndEffectorPositions;  // Wrist Coordinate [joints+EndEffector][x, y, z]
            public List<List<float>> jointDoFs;                       // [DoFs] deg
            public List<List<Vector3>> jointAndEndEffectorVelocities; // Wrist Coordinate [joints][x, y, z]
            public List<List<float>> jointDoFVelocities;              // [DoFs] deg/s

            public List<List<Quaternion>> jointRotations;
        }

        public class Object
        {
            public string objectName;
            public List<Vector3> objectPositions;
            public List<Quaternion> objectRotations;
            public List<Vector3> tablePositions;
            public List<Quaternion> tableRotations;
            public Vector3 gravity = Vector3.zero;
            public float mass;
            public List<bool> isKinematic;
        }

        public List<List<bool>> contactLabels;

        public TrainData()
        {
        }
    }

    public class State
    {
        public Hand hand = new Hand();
        public Object obj = new Object();
        
        public class Hand
        {
            public Vector3 upVector; // World Coordinate x, y, z
            public Vector3 forwardVector; // World Coordinate x, y, z
            public Vector3[] jointAndEndEffectorPositions; // Wrist Coordinate [joints+EndEffector][x, y, z]
            public float[] jointDoFs; // [DoFs] deg or rad
            public Vector3[] jointVelocities; // Wrist Coordinate [joints][x, y, z]
            public float[] jointDoFVelocities; // [DoFs] deg or rad/s
            public float[] jointDoFAccelerations; // [DoFs] deg or rad/s^2
        }

        public class Object
        {
            public Vector3 objectVelocity; // World Coordinate x, y, z m/s
            public Vector3 objectAngularVelocity; // World Coordinate Axis-Angle x, y, z deg or rad/s
            public Vector3 objectGravity; // Wrist Coordinate x, y, z m/s^2
            public float objectMass; // kg
        }
        
        public float[][][] gvs;
        public float[][][][] lvs;
        public Vector3[] closestPointVectors;
        public Vector3[] normalVectors;
        
        public Vector3[] forceVectors; // kgf
        
        public float[] triggerValues;
        public float[] previousAction; // deg
    }
    
    [Serializable]
    public class StateUseFlags
    {
        public bool upVector;
        public bool forwardVector;
        public bool jointAndEndEffectorPositions;
        public bool jointDoFs;
        public bool jointVelocities;
        public bool jointDoFVelocities;
        public bool jointDoFAccelerations;
        
        public bool objectVelocity;
        public bool objectAngularVelocity;
        public bool objectGravity;
        public bool objectMass;
        
        public bool gvs;
        public bool lvs;
        public bool closestPointVectors;
        public bool normalVectors;
        
        public bool forceVectors;
        
        public bool triggerValues;
        public bool previousAction;
    }
    
    [Serializable]
    public class StateScalingFloats
    {
        public float upVector;
        public float forwardVector;
        public float jointAndEndEffectorPositions;
        public float jointDoFs;
        public float jointVelocities;
        public float jointDoFVelocities;
        public float jointDoFAccelerations;
        
        public float objectVelocity;
        public float objectAngularVelocity;
        public float objectGravity;
        public float objectMass;
        
        public float gvs;
        public float lvs;
        public float closestPointVectors;
        public float normalVectors;
        
        public float forceVectors;
        
        public float triggerValues;
        public float previousAction;
    }
}