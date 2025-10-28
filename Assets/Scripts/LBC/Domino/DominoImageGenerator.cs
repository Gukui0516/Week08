using UnityEngine;
using System.Collections.Generic;

public class DominoImageGenerator : MonoBehaviour
{
    [Header("이미지 설정")]
    public Texture2D sourceImage; // 변환할 이미지
    public int resolutionX = 50; // 가로 도미노 개수
    public int resolutionY = 50; // 세로 도미노 개수

    [Header("도미노 설정")]
    public GameObject dominoPrefab; // 도미노 프리팹
    public Vector3 dominoScale = new Vector3(1f, 2f, 0.2f); // 도미노 크기
    public float spacingX = 1.05f; // 도미노 가로 간격 (같은 줄 내) - 연쇄 효과를 위해 더 좁게
    public float spacingZ = 1.05f; // 도미노 세로 간격 (줄 간격) - 연쇄 효과를 위해 더 좁게

    [Header("색상 설정")]
    public bool useColoredDominoes = true; // 컬러 도미노 사용
    public Material dominoMaterial; // 기본 머티리얼
    public Color brightColor = Color.white; // 밝은 색
    public Color darkColor = Color.black; // 어두운 색
    public float colorThreshold = 0.5f; // 흑백 판단 임계값 (0~1)

    [Header("고급 설정")]
    public bool useGrayscaleSpectrum = false; // 흑백 스펙트럼 사용
    public int colorLevels = 8; // 색상 단계 수 (useGrayscaleSpectrum 사용 시)
    public bool flipVertical = true; // 이미지 상하 반전
    public bool createInRows = false; // 행 단위로 생성 (도미노 효과)

    [Header("연쇄 도미노 설정")]
    public bool enableChainReaction = true; // 연쇄 반응 활성화
    public bool addCornerDominoes = true; // 모서리 회전 도미노 추가
    public float cornerDominoOffset = 0.5f; // 모서리 도미노 간격
    public bool zigzagPattern = true; // 지그재그 패턴 (좌→우, 우→좌 교차)
    public int cornerDominoCount = 7; // 모서리 도미노 개수 (더 많이)
    public float turnRadius = 1.5f; // U턴 반지름 (spacing의 배수)

    [Header("푸셔 도미노 설정")]
    public bool addPusherDominoes = true; // 각 줄 시작에 푸셔 도미노 추가
    public float pusherDistance = 1.5f; // 푸셔와 첫 도미노 사이 거리
    public Color pusherColor = new Color(1f, 0.3f, 0.3f); // 푸셔 도미노 색상 (빨간색)

    [Header("물리 설정")]
    public float dominoMass = 0.2f; // 도미노 질량
    public float dominoDrag = 0.1f; // 도미노 저항
    public float dominoAngularDrag = 0.5f; // 도미노 회전 저항
    public PhysicsMaterial physicMaterial; // 물리 머티리얼 (마찰력 등)

    [Header("트리거 설정")]
    public bool addStartTrigger = true; // 시작 트리거 추가
    public KeyCode triggerKey = KeyCode.Space; // 트리거 키
    public float triggerForce = 300f; // 트리거 힘

    [Header("디버그")]
    public bool showPreview = true;
    public float previewScale = 0.5f;

    private List<GameObject> spawnedDominoes = new List<GameObject>();
    private List<GameObject> pusherDominoes = new List<GameObject>();
    private Texture2D processedImage;
    private GameObject firstPusher; // 첫 번째 푸셔 (트리거용)

    void Start()
    {
        // 자동 생성을 원하면 주석 해제
        // GenerateDominoImage();
    }

    void Update()
    {
        // 스페이스바로 첫 번째 도미노 쓰러뜨리기
        if (addStartTrigger && Input.GetKeyDown(triggerKey) && firstPusher != null)
        {
            TriggerDominoEffect();
        }
    }

    [ContextMenu("도미노 이미지 생성")]
    public void GenerateDominoImage()
    {
        if (sourceImage == null)
        {
            Debug.LogError("소스 이미지를 설정해주세요!");
            return;
        }

        ClearDominoes();

        // 이미지 전처리
        processedImage = ProcessImage(sourceImage);

        // 부모 오브젝트 생성
        GameObject parent = new GameObject("DominoImage_" + sourceImage.name);
        parent.transform.position = transform.position;

        // 도미노 생성
        if (createInRows)
        {
            GenerateDominoesInRows(parent.transform);
        }
        else
        {
            GenerateDominoesAll(parent.transform);
        }

        Debug.Log($"도미노 이미지 생성 완료! 총 {spawnedDominoes.Count}개의 도미노, {pusherDominoes.Count}개의 푸셔");
    }

    [ContextMenu("도미노 삭제")]
    public void ClearDominoes()
    {
        foreach (GameObject domino in spawnedDominoes)
        {
            if (domino != null)
                DestroyImmediate(domino);
        }
        spawnedDominoes.Clear();

        foreach (GameObject pusher in pusherDominoes)
        {
            if (pusher != null)
                DestroyImmediate(pusher);
        }
        pusherDominoes.Clear();

        firstPusher = null;

        // 부모 오브젝트 삭제
        GameObject[] parents = GameObject.FindGameObjectsWithTag("Untagged");
        foreach (GameObject obj in parents)
        {
            if (obj.name.StartsWith("DominoImage_"))
                DestroyImmediate(obj);
        }
    }

    [ContextMenu("도미노 효과 시작")]
    public void TriggerDominoEffect()
    {
        if (firstPusher != null)
        {
            Rigidbody rb = firstPusher.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // 첫 번째 푸셔에 회전력 가하기
                rb.AddForceAtPosition(
                    Vector3.forward * triggerForce,
                    firstPusher.transform.position + Vector3.up * dominoScale.y * 0.8f,
                    ForceMode.Impulse
                );
                Debug.Log("도미노 효과 시작!");
            }
        }
        else
        {
            Debug.LogWarning("시작 푸셔를 찾을 수 없습니다!");
        }
    }

    private Texture2D ProcessImage(Texture2D original)
    {
        // 읽기 가능한 텍스처로 변환
        Texture2D readable = MakeTextureReadable(original);

        // 리사이즈
        Texture2D resized = ResizeTexture(readable, resolutionX, resolutionY);

        return resized;
    }

    private Texture2D MakeTextureReadable(Texture2D texture)
    {
        // 텍스처가 읽기 가능한지 확인
        RenderTexture tmp = RenderTexture.GetTemporary(
            texture.width,
            texture.height,
            0,
            RenderTextureFormat.Default,
            RenderTextureReadWrite.Linear);

        Graphics.Blit(texture, tmp);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = tmp;

        Texture2D readable = new Texture2D(texture.width, texture.height);
        readable.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
        readable.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(tmp);

        return readable;
    }

    private Texture2D ResizeTexture(Texture2D source, int newWidth, int newHeight)
    {
        RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);
        rt.filterMode = FilterMode.Bilinear;

        RenderTexture.active = rt;
        Graphics.Blit(source, rt);

        Texture2D result = new Texture2D(newWidth, newHeight);
        result.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
        result.Apply();

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);

        return result;
    }

    private void GenerateDominoesAll(Transform parent)
    {
        for (int y = 0; y < resolutionY; y++)
        {
            // 지그재그 패턴 적용
            bool reverseOrder = zigzagPattern && (y % 2 == 1);

            // 푸셔 도미노 추가 (각 줄의 시작점)
            if (addPusherDominoes && enableChainReaction)
            {
                GameObject pusher = CreatePusherDomino(y, reverseOrder, parent);
                pusherDominoes.Add(pusher);

                // 첫 번째 줄의 푸셔를 저장 (트리거용)
                if (y == 0)
                {
                    firstPusher = pusher;
                }
            }

            if (reverseOrder)
            {
                for (int x = resolutionX - 1; x >= 0; x--)
                {
                    CreateDominoAtPosition(x, y, parent);
                }
            }
            else
            {
                for (int x = 0; x < resolutionX; x++)
                {
                    CreateDominoAtPosition(x, y, parent);
                }
            }

            // 모서리 도미노 추가
            if (addCornerDominoes && enableChainReaction && y < resolutionY - 1)
            {
                AddCornerDomino(y, reverseOrder, parent, parent);
            }
        }
    }

    private void GenerateDominoesInRows(Transform parent)
    {
        // 행별로 부모 생성 (도미노 효과를 위해)
        for (int y = 0; y < resolutionY; y++)
        {
            GameObject rowParent = new GameObject($"Row_{y}");
            rowParent.transform.parent = parent;

            // 지그재그 패턴: 짝수 행은 왼쪽→오른쪽, 홀수 행은 오른쪽→왼쪽
            bool reverseOrder = zigzagPattern && (y % 2 == 1);

            // 푸셔 도미노 추가
            if (addPusherDominoes && enableChainReaction)
            {
                GameObject pusher = CreatePusherDomino(y, reverseOrder, rowParent.transform);
                pusherDominoes.Add(pusher);

                if (y == 0)
                {
                    firstPusher = pusher;
                }
            }

            if (reverseOrder)
            {
                // 오른쪽에서 왼쪽으로
                for (int x = resolutionX - 1; x >= 0; x--)
                {
                    CreateDominoAtPosition(x, y, rowParent.transform);
                }
            }
            else
            {
                // 왼쪽에서 오른쪽으로
                for (int x = 0; x < resolutionX; x++)
                {
                    CreateDominoAtPosition(x, y, rowParent.transform);
                }
            }

            // 모서리 회전 도미노 추가 (다음 행으로 연결)
            if (addCornerDominoes && enableChainReaction && y < resolutionY - 1)
            {
                AddCornerDomino(y, reverseOrder, rowParent.transform, parent);
            }
        }
    }

    private GameObject CreatePusherDomino(int rowIndex, bool isReversed, Transform parent)
    {
        // 푸셔 위치 계산 (각 줄의 시작점 앞에 배치)
        int startX = isReversed ? resolutionX - 1 : 0;
        float baseX = startX * dominoScale.x * spacingX;
        float baseZ = rowIndex * dominoScale.x * spacingZ;

        // 푸셔는 시작점보다 앞에 배치
        float offsetX = isReversed ? pusherDistance : -pusherDistance;

        Vector3 position = transform.position + new Vector3(
            baseX + offsetX * dominoScale.x,
            dominoScale.y / 2f,
            baseZ
        );

        // 도미노 생성
        GameObject pusher;
        if (dominoPrefab != null)
        {
            pusher = Instantiate(dominoPrefab, position, Quaternion.identity, parent);
        }
        else
        {
            pusher = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pusher.transform.position = position;
            pusher.transform.parent = parent;
        }

        pusher.transform.localScale = dominoScale;
        pusher.name = $"Pusher_{rowIndex}";

        // 푸셔 색상 적용 (빨간색으로 구분)
        Renderer renderer = pusher.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (dominoMaterial != null)
            {
                renderer.material = new Material(dominoMaterial);
            }
            renderer.material.color = pusherColor;
        }

        // Rigidbody 및 물리 설정
        SetupPhysics(pusher);

        return pusher;
    }

    private void CreateDominoAtPosition(int x, int y, Transform parent)
    {
        // 이미지에서 픽셀 색상 가져오기
        int pixelY = flipVertical ? (resolutionY - 1 - y) : y;
        Color pixelColor = processedImage.GetPixel(x, pixelY);

        // 밝기 계산
        float brightness = pixelColor.grayscale;

        // 임계값보다 어두우면 도미노 생성
        if (brightness < colorThreshold || useColoredDominoes || useGrayscaleSpectrum)
        {
            // 위치 계산 (spacingX와 spacingZ 분리)
            float posX = x * dominoScale.x * spacingX;
            float posZ = y * dominoScale.x * spacingZ;
            Vector3 position = transform.position + new Vector3(posX, dominoScale.y / 2f, posZ);

            // 도미노 생성
            GameObject domino;
            if (dominoPrefab != null)
            {
                domino = Instantiate(dominoPrefab, position, Quaternion.identity, parent);
            }
            else
            {
                domino = GameObject.CreatePrimitive(PrimitiveType.Cube);
                domino.transform.position = position;
                domino.transform.parent = parent;
            }

            domino.transform.localScale = dominoScale;
            domino.name = $"Domino_{x}_{y}";

            // 색상 적용
            Renderer renderer = domino.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (dominoMaterial != null)
                {
                    renderer.material = new Material(dominoMaterial);
                }

                Color dominoColor = GetDominoColor(brightness, pixelColor);
                renderer.material.color = dominoColor;
            }

            // Rigidbody 및 물리 설정
            if (enableChainReaction)
            {
                SetupPhysics(domino);
            }

            spawnedDominoes.Add(domino);
        }
    }

    private void SetupPhysics(GameObject domino)
    {
        Rigidbody rb = domino.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = domino.AddComponent<Rigidbody>();
        }

        // 물리 설정
        rb.mass = dominoMass;
        rb.linearDamping = dominoDrag;
        rb.angularDamping = dominoAngularDrag;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate; // 부드러운 움직임

        // Collider 설정
        Collider collider = domino.GetComponent<Collider>();
        if (collider != null && physicMaterial != null)
        {
            collider.material = physicMaterial;
        }
    }

    private void AddCornerDomino(int rowIndex, bool isReversed, Transform rowParent, Transform mainParent)
    {
        // 현재 행의 끝 지점 계산
        int endX = isReversed ? 0 : resolutionX - 1;

        float baseX = endX * dominoScale.x * spacingX;
        float baseZ = rowIndex * dominoScale.x * spacingZ;

        // U턴 반지름 계산
        float actualTurnRadius = dominoScale.x * spacingX * turnRadius;

        for (int i = 0; i < cornerDominoCount; i++)
        {
            float t = (float)i / (cornerDominoCount - 1);

            // 180도 호를 따라 배치 (U턴)
            float angle = isReversed ?
                Mathf.Lerp(0f, 180f, t) : // 왼쪽 끝: 0° → 180° (시계방향)
                Mathf.Lerp(180f, 360f, t); // 오른쪽 끝: 180° → 360° (시계방향)

            float angleRad = angle * Mathf.Deg2Rad;

            // 원호를 따라 위치 계산
            float circleX = Mathf.Sin(angleRad) * actualTurnRadius;
            float circleZ = -Mathf.Cos(angleRad) * actualTurnRadius + actualTurnRadius;

            // 최종 위치
            Vector3 position = transform.position + new Vector3(
                baseX + (isReversed ? circleX : -circleX),
                dominoScale.y / 2f,
                baseZ + circleZ + dominoScale.x * spacingZ * 0.5f
            );

            // 도미노 방향 (접선 방향)
            float tangentAngle = angle + 90f;
            if (isReversed) tangentAngle = angle - 90f;

            Quaternion rotation = Quaternion.Euler(0, tangentAngle, 0);

            // 모서리 도미노 생성
            GameObject cornerDomino = CreateCornerDomino(position, rotation, rowParent);
            cornerDomino.name = $"Corner_{rowIndex}_{i}";

            spawnedDominoes.Add(cornerDomino);
        }
    }

    private GameObject CreateCornerDomino(Vector3 position, Quaternion rotation, Transform parent)
    {
        GameObject domino;

        if (dominoPrefab != null)
        {
            domino = Instantiate(dominoPrefab, position, rotation, parent);
        }
        else
        {
            domino = GameObject.CreatePrimitive(PrimitiveType.Cube);
            domino.transform.position = position;
            domino.transform.rotation = rotation;
            domino.transform.parent = parent;
        }

        domino.transform.localScale = dominoScale;

        // 모서리 도미노 색상
        Renderer renderer = domino.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (dominoMaterial != null)
            {
                renderer.material = new Material(dominoMaterial);
            }
            // 모서리 도미노는 약간 밝은 색으로 표시 (선택적)
            renderer.material.color = new Color(0.9f, 0.9f, 1f);
        }

        // Rigidbody 및 물리 설정
        SetupPhysics(domino);

        return domino;
    }

    private Color GetDominoColor(float brightness, Color originalColor)
    {
        if (useColoredDominoes)
        {
            // 원본 이미지의 색상 사용
            return originalColor;
        }
        else if (useGrayscaleSpectrum)
        {
            // 여러 단계의 회색조 사용
            float level = Mathf.Floor(brightness * colorLevels) / colorLevels;
            return Color.Lerp(darkColor, brightColor, level);
        }
        else
        {
            // 단순 흑백 (임계값 기준)
            return brightness < colorThreshold ? darkColor : brightColor;
        }
    }

    private void OnDrawGizmos()
    {
        if (showPreview && processedImage != null && !Application.isPlaying)
        {
            // 처리된 이미지 미리보기
            Gizmos.color = Color.white;
            Vector3 previewPos = transform.position;
            float previewWidth = resolutionX * dominoScale.x * spacingX * previewScale;
            float previewHeight = resolutionY * dominoScale.x * spacingZ * previewScale;

            // 경계선 그리기
            Vector3[] corners = new Vector3[]
            {
                previewPos,
                previewPos + new Vector3(previewWidth, 0, 0),
                previewPos + new Vector3(previewWidth, 0, previewHeight),
                previewPos + new Vector3(0, 0, previewHeight)
            };

            for (int i = 0; i < 4; i++)
            {
                Gizmos.DrawLine(corners[i], corners[(i + 1) % 4]);
            }
        }

        // 첫 번째 푸셔 위치 표시
        if (firstPusher != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(firstPusher.transform.position, 0.5f);
        }
    }
}