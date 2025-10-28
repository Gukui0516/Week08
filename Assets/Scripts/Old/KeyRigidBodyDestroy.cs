using UnityEngine;
using UnityEngine.UI;

public class KeyRigidBodyDestroy : MonoBehaviour
{
    [SerializeField] private Button _exitBtn;
    [SerializeField] private Rigidbody rb;

    private void OnEnable()
    {
        _exitBtn.onClick.AddListener(ExitBtn);
    }
    
    private void OnDisable()
    {
        _exitBtn.onClick.RemoveAllListeners();
    }   

    void ExitBtn()
    {
        rb.isKinematic = true;
    }

}
