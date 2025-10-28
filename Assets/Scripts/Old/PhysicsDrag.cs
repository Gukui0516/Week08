using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class PhysicsDrag : SingletonObject<PhysicsDrag>
{
    // ... (기존 변수들은 그대로) ...
    private LineRenderer line1;

    [Header("라인 렌더러 설정")]
    [SerializeField] private Material lineMaterial; // 라인에 사용할 머티리얼
    [SerializeField] private Gradient stressGradient;
    [SerializeField] private float maxLineWidth = 0.05f;
    [SerializeField] private float minLineWidth = 0.01f;
    [SerializeField] private float dangerThreshold = 0.8f; // 위험 색상으로 바뀌는 임계값

    [Header("드래그 가능 태그")]
    [SerializeField] List<string> draggableTags = new List<string> { "Draggable", "Bomb" };

    private Camera cam;
    public SpringJoint grabJoint;
    private Rigidbody grabbedRb;
    // private float initialGrabDistance;
    private float currentGrabDistance;
    private float originalAngularDrag;
    private float originalLinearDamping;
    private RigidbodyInterpolation originalInterpolation;
    private CollisionDetectionMode originalCollisionMode;

    public Vector3 currentGrabPoint { get; private set; }


    [Header("잡기 설정")]
    [SerializeField] private float grabMaxDistance = 10f;
    [Tooltip("물체를 잡았을 때 적용할 각마찰(회전 저항) 값입니다.")]
    [SerializeField] private float grabAngularDrag = 5.0f; // <<< 추가: 잡았을 때 적용할 각마찰 값
    [SerializeField] private float grabLinearDrag = 5.0f;

    [Header("조인트 설정")]
    [SerializeField] private float springStiffness = 2000f;
    [SerializeField] private float springDamper = 20f;
    [SerializeField] private float jointBreakForce = 500f;


    [Header("마우스 휠 줌 설정")]
    [SerializeField] private float minGrabDistance = 1f;
    [SerializeField] private float maxGrabDistance = 20f;

    [Tooltip("마우스 휠 입력이 줌 속도에 얼마나 영향을 주는지 (가속도)")]
    [SerializeField] private float scrollAcceleration = 20f;
    [Tooltip("휠을 멈췄을 때 줌 속도가 얼마나 빨리 0으로 줄어드는지 (감속)")]
    [SerializeField] private float scrollDamping = 5f;
    private float distanceChangeVelocity = 0f;


    protected override void Awake()
    {
        base.Awake();
        cam = Camera.main;
    }


    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TryGrab();


        }

        if (Input.GetMouseButtonUp(0))
        {
            ReleaseAll(); // <<< 변경: Release() 대신 ReleaseAll()을 호출하여 모든 상태를 확실히 초기화합니다.

        }

    }

    public void UpdateDistance()
    {
        if (grabJoint != null)
        {
            // 우클릭(카메라 회전) 중이 아닐 때만 휠 입력을 받습니다.
            float scrollInput = 0f;

            scrollInput = Input.mouseScrollDelta.y;

            // 1. 휠 입력으로 속도(가속도)를 더합니다.
            if (scrollInput != 0)
            {
                distanceChangeVelocity += scrollInput * scrollAcceleration;
            }

            // // 2. 현재 속도를 감속(Damping)시킵니다. (서서히 멈춤)
            // // Time.deltaTime을 곱해 프레임에 독립적으로 만듭니다.
            distanceChangeVelocity = Mathf.Lerp(distanceChangeVelocity, 0, scrollDamping * Time.deltaTime);

            // // 3. 현재 거리에 속도를 적용합니다.
            currentGrabDistance += distanceChangeVelocity * Time.deltaTime;

            // // 4. 거리가 경계에 닿았는지 확인합니다.
            bool hitBoundary = (currentGrabDistance <= minGrabDistance && distanceChangeVelocity < 0) ||
                               (currentGrabDistance >= maxGrabDistance && distanceChangeVelocity > 0);

            // // 5. 거리를 경계 내로 제한합니다.
            currentGrabDistance = Mathf.Clamp(currentGrabDistance, minGrabDistance, maxGrabDistance);

            // // 6. 경계에 닿았다면, 속도를 0으로 만들어 "튕김"이나 "달라붙음" 현상을 방지합니다.
            if (hitBoundary)
            {
                distanceChangeVelocity = 0f;
            }

            if (distanceChangeVelocity > 0.05f)
            {
                CursorManager.Instance.SetCursorToForwardWheel();
            }
            else if (distanceChangeVelocity < -0.05f)
            {
                CursorManager.Instance.SetCursorToBackWheel();
            }
            else
            {
                CursorManager.Instance.SetCursorToGrab();
            }
            CursorManager.Instance.SetCursorUIImagePosition(distanceChangeVelocity);
        }
    }

    public void UpdateGrabDistance()
    {
        Ray ray = cam.ScreenPointToRay(CursorManager.Instance.CursorPosition);
        Vector3 vectorToPoint = currentGrabPoint - ray.origin;
        currentGrabDistance = vectorToPoint.magnitude;
    }

    void LateUpdate()
    {
        if (line1 != null)
        {
            SpringJoint activeJoint = grabJoint;
            UpdateLine(line1, activeJoint);
        }
    }
    void ApplyGrabSettings(Rigidbody rb)
    {
        if (rb != null)
        {
            // <<< 추가: 잡는 순간, 원래 물리 설정을 저장
            originalAngularDrag = rb.angularDamping;
            originalLinearDamping = rb.linearDamping;
            originalInterpolation = rb.interpolation;
            originalCollisionMode = rb.collisionDetectionMode;

            // <<< 추가: 잡는 동안 사용할 고품질 물리 설정으로 변경
            rb.angularDamping = grabAngularDrag;
            rb.linearDamping = grabLinearDrag;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
    }

    void RestoreOriginalSettings(Rigidbody rb)
    {
        if (rb != null)
        {
            rb.linearDamping = originalLinearDamping;
            rb.angularDamping = originalAngularDrag;
            rb.interpolation = originalInterpolation;
            rb.collisionDetectionMode = originalCollisionMode;
        }
    }


    void TryGrab()
    {
        Ray ray = cam.ScreenPointToRay(CursorManager.Instance.CursorPosition);
        if (Physics.Raycast(ray, out RaycastHit hit, grabMaxDistance))
        {
            if (hit.collider.attachedRigidbody != null && draggableTags.Contains(hit.rigidbody.gameObject.tag))
            {
                grabbedRb = hit.collider.attachedRigidbody;
                currentGrabDistance = hit.distance;

                ApplyGrabSettings(grabbedRb);

                grabJoint = grabbedRb.gameObject.AddComponent<SpringJoint>();
                grabJoint.autoConfigureConnectedAnchor = false;
                grabJoint.anchor = grabbedRb.transform.InverseTransformPoint(hit.point);
                grabJoint.spring = springStiffness;
                grabJoint.damper = springDamper;

                grabJoint.breakForce = jointBreakForce;
                var JointBreakDetector =   grabJoint.gameObject.GetComponent<JointBreakDetector>();
                if(JointBreakDetector == null)
                    grabJoint.gameObject.AddComponent<JointBreakDetector>();

                CreateLineRenderer(ref line1);

                Vector3 targetPoint = ray.GetPoint(currentGrabDistance);
                grabJoint.connectedAnchor = targetPoint;
                currentGrabPoint = targetPoint;


                if (line1 != null)
                {
                    line1.SetPosition(0, grabJoint.transform.TransformPoint(grabJoint.anchor));
                    line1.SetPosition(1, targetPoint);
                }
                CursorManager.Instance.SetCursorToGrab();
                LogSystem.PushLog(LogLevel.INFO, "InputGrab", grabbedRb.gameObject.name ,true);
            }
        }
    }



    public void Drag()
    {
        Ray ray = cam.ScreenPointToRay(CursorManager.Instance.CursorPosition);
        Vector3 targetPoint = ray.GetPoint(currentGrabDistance);
        grabJoint.connectedAnchor = targetPoint;
        currentGrabPoint = targetPoint;
    }



    void Release()
    {
        if (grabJoint != null)
        {
            if (grabbedRb != null)
            {
                RestoreOriginalSettings(grabbedRb);
            }
            LogSystem.PushLog(LogLevel.INFO, "InputGrabBreak", grabbedRb != null ? grabbedRb.gameObject.name : "null", true);
            Destroy(grabJoint);
            grabJoint = null;
            grabbedRb = null;
            if (line1 != null)
            {
                Destroy(line1.gameObject);
                line1 = null;
            }
        }
    }

    public void NotifyJointBroken()
    {
        ReleaseAll();
        CursorManager.Instance.SetCursorColor(1.0f);
    }

    void ReleaseAll()
    {
        Release();
        CursorManager.Instance.StopTracking();
        CursorManager.Instance.SetCursorToDefault();
        distanceChangeVelocity = 0f;
    }

    void StopTracking()
    {
        if (CursorManager.Instance.CheckAndClampCursorPosition())
            ReleaseAll();
        else
            CursorManager.Instance.StopTracking();
    }

    void CreateLineRenderer(ref LineRenderer line)
    {
        if (line != null) Destroy(line.gameObject);

        GameObject lineObj = new GameObject("GrabLine");
        line = lineObj.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.material = lineMaterial;
        line.startWidth = maxLineWidth;
        line.endWidth = maxLineWidth;
        line.numCapVertices = 10;
        line.useWorldSpace = true; // 월드 좌표 사용을 명시해 위치 초기화가 즉시 반영되도록
    }



    void UpdateLine(LineRenderer line, SpringJoint joint)
    {
        // 조인트가 파괴되면 line은 있지만 joint는 null일 수 있음
        if (joint == null || joint.connectedBody != null)
        {
            if (line != null) Destroy(line.gameObject);
            return;
        }

        // 라인의 시작점 (오브젝트의 앵커)과 끝점 (조인트의 목표) 설정
        line.SetPosition(0, joint.transform.TransformPoint(joint.anchor));
        line.SetPosition(1, joint.connectedAnchor);

        // 1. breakForce가 0이거나 무한대(기본값)인지 체크
        float currentBreakForce = joint.breakForce;
        float forceRatio = 0f;

        // breakForce가 유효한 값일 때만(0보다 크고 무한대가 아닐 때) 비율 계산
        if (currentBreakForce > 0 && !float.IsInfinity(currentBreakForce))
        {
            forceRatio = joint.currentForce.magnitude / currentBreakForce;
        }

        // 2. 색상/두께에 사용할 '스트레스' 값 계산 (0.0 ~ 1.0)
        // dangerThreshold 기준으로 1.0에 도달하도록 함
        float stress = Mathf.Clamp01(forceRatio / dangerThreshold);

        // 3. 색상 변경 (기존 로직)
        Color stressColor = stressGradient.Evaluate(stress);
        line.startColor = stressColor;
        line.endColor = stressColor;

        // 4. 두께 변경 (새로 추가된 로직)
        // stress가 0일 때 maxLineWidth, 1일 때 minLineWidth가 되도록 보간(Lerp)
        float currentWidth = Mathf.Lerp(maxLineWidth, minLineWidth, stress);
        line.startWidth = currentWidth;
        line.endWidth = currentWidth;
        CursorManager.Instance.SetCursorColor(stress);
    }
}