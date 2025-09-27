using System;
using UnityEngine;

namespace Sensors
{
    [AddComponentMenu("")]
    public class CollisionCallbackController: MonoBehaviour
    {
        private TactileSensorManager _tactileSensorManager;
        
        private void Start()
        {
            int parentSearchDepthCnt = 0;
            Transform parent = transform.parent;
            while (_tactileSensorManager == null)
            {
                parentSearchDepthCnt += 1;
                if (parentSearchDepthCnt > 100)
                {
                    throw new Exception("Parent search depth is too deep.");
                }
                if (parent == null)
                {
                    throw new Exception("TactileSensorManager is not found in all parents.");
                }
                
                TactileSensorManager manager = parent.GetComponent<TactileSensorManager>();
                if (manager != null)
                {
                    _tactileSensorManager = manager;
                }
                else
                {
                    parent = parent.parent;
                }
            }
        }
        
        private void OnCollisionStay(Collision collision)
        {
            _tactileSensorManager.CollisionStayCall(collision);
        }
        
        private void OnCollisionExit(Collision collision)
        {
            _tactileSensorManager.CollisionExitCall(collision);
        }
    }
}