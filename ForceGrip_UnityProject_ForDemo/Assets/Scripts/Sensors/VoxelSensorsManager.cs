using System.Collections.Generic;
using UnityEngine;

namespace Sensors
{
    public class VoxelSensorsManager : MonoBehaviour
    {
        [Header("Debug")]
        [Tooltip("Works on Editor Only. You have to call VoxelSensor.Sense() by your own timing at Build version.")]
        public bool senseAllSensorsOnFixedUpdate;
        
        public bool drawGVS;
        public bool drawLVS;
        
        public Color colorOccupied = Color.green;
        public Color colorEmpty = Color.clear;
        
        [Header("Voxel Sensors")]
        public VoxelSensor GlobalVoxelSensor;
        public List<VoxelSensor> LocalVoxelSensors;
        public LayerMask layersToBeIgnored;
        
        void Start()
        {
            layersToBeIgnored = ~layersToBeIgnored;
            
            // for Visualization
            GlobalVoxelSensor.GetComponent<MeshRenderer>().enabled = false;
            GlobalVoxelSensor.Sense(layersToBeIgnored);
            foreach (var voxelSensor in LocalVoxelSensors)
            {
                voxelSensor.GetComponent<MeshRenderer>().enabled = false;
                voxelSensor.Sense(layersToBeIgnored);
            }
            //
        }
        
        #if UNITY_EDITOR
        void FixedUpdate()
        {
            if (Physics.simulationMode != SimulationMode.FixedUpdate)
                return;
            
            SenseAllSensorsOnFixedUpdate();
        }
        #endif
        
        void SenseAllSensorsOnFixedUpdate()
        {
            if (senseAllSensorsOnFixedUpdate)
            {
                GlobalVoxelSensor.Sense(layersToBeIgnored);
                foreach (var voxelSensor in LocalVoxelSensors)
                {
                    voxelSensor.Sense(layersToBeIgnored);
                }
            }
        }
        
        public VoxelSensor SenseGVS()
        {
            GlobalVoxelSensor.Sense(layersToBeIgnored);
            return GlobalVoxelSensor;
        }
        
        public List<VoxelSensor> SenseLVS()
        { 
            foreach (var voxelSensor in LocalVoxelSensors)
            {
                voxelSensor.Sense(layersToBeIgnored);
            }
            return LocalVoxelSensors;
        }
        
        private void OnRenderObject()
        {
            if (drawGVS)
            {
                GlobalVoxelSensor.Draw(colorOccupied: colorOccupied, colorEmpty: colorEmpty);
            }
            if (drawLVS)
            {
                foreach (var voxelSensor in LocalVoxelSensors)
                {
                    voxelSensor.Draw(colorOccupied: colorOccupied, colorEmpty: colorEmpty);
                }
            }
        }
    }
}
