using UnityEngine;

public class JointBreakDetector : MonoBehaviour
{
    private void OnJointBreak(float breakForce)
    {
        if (PhysicsDrag.Instance != null)
        {
            PhysicsDrag.Instance.NotifyJointBroken();
        }
    }
}