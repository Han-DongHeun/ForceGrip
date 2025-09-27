using UnityEngine;

namespace PhysicsSimulation
{
    public class HandPhysicsSimulator
    {
        // Singleton Pattern
        private static HandPhysicsSimulator _instance;
        public static HandPhysicsSimulator Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new HandPhysicsSimulator();
                return _instance;
            }
        }
        
        public Vector3 CalcWristVelocity(Vector3 currentPosition, Vector3 targetPosition)
        {
            var deltaPosition_Meter = targetPosition - currentPosition;
            var velocity_MeterPerSec = deltaPosition_Meter / Time.fixedDeltaTime;
            
            return velocity_MeterPerSec;
        }
        
        public Vector3 CalcWristAngularVelocity(Quaternion currentRotation, Quaternion targetRotation)
        {
            var deltaQuaternion = targetRotation * Quaternion.Inverse(currentRotation);
            
            // ΔQuaternion을 각속도로 변환
            Vector3 angularVelocity = new Vector3(deltaQuaternion.x, deltaQuaternion.y, deltaQuaternion.z);
            
            // ΔQuaternion.w는 축 반전/각도 정보에 따라 처리
            float angle = 2f * Mathf.Acos(Mathf.Clamp(deltaQuaternion.w, -1f, 1f));
            if (angle > Mathf.PI)
            {
                angle = 2 * Mathf.PI - angle;
                angularVelocity = -angularVelocity;
            }
            
            angularVelocity.Normalize();
            angularVelocity *= angle;
            
            return angularVelocity / Time.fixedDeltaTime;
        }
    }
}
