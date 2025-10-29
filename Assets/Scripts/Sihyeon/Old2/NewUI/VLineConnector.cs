using UnityEngine;

public class VLineConnector : MonoBehaviour
{
    // [Inspector 설정]
    public Rigidbody pictureFrameRoot; // PictureFrame_Root의 Rigidbody
    public Vector3 leftConnectionOffset = new Vector3(-1.9f, 1.9f, 0f);
    public Vector3 rightConnectionOffset = new Vector3(1.9f, 1.9f, 0f);

    private Transform ropeApexTransform; // 이 스크립트가 붙은 RopeApex의 Transform
    private LineRenderer lineRenderer;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        ropeApexTransform = transform; // RopeApex Transform 가져오기

        if (lineRenderer == null || pictureFrameRoot == null)
        {
            Debug.LogError("필수 컴포넌트(Line Renderer) 또는 Rigidbody가 누락되었습니다.");
            enabled = false;
        }

        // Position Count가 3인지 확인
        if (lineRenderer.positionCount != 3)
        {
            lineRenderer.positionCount = 3;
        }
    }

    void Update()
    {
        if (pictureFrameRoot == null) return;

        // 1. 액자 왼쪽 지점 (로컬 -> 월드 변환)
        Vector3 leftCornerWorld = pictureFrameRoot.transform.TransformPoint(leftConnectionOffset);

        // 2. 줄 중심 지점 (RopeApex의 월드 위치)
        Vector3 apexWorld = ropeApexTransform.position;

        // 3. 액자 오른쪽 지점 (로컬 -> 월드 변환)
        Vector3 rightCornerWorld = pictureFrameRoot.transform.TransformPoint(rightConnectionOffset);

        // Line Renderer 위치 설정 (왼쪽 모서리 -> 중심(Apex) -> 오른쪽 모서리 순)
        lineRenderer.SetPosition(0, leftCornerWorld);
        lineRenderer.SetPosition(1, apexWorld);
        lineRenderer.SetPosition(2, rightCornerWorld);
    }
}