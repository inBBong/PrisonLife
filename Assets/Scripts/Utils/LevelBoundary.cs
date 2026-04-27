// LevelBoundary.cs
// 레벨 경계 펜스 시각화 및 충돌체 설정
// 채석장 외벽의 철조망 펜스 표현

using UnityEngine;

public class LevelBoundary : MonoBehaviour
{
    [Header("Boundary Settings")]
    public float width = 20f;
    public float height = 30f;
    public float wallThickness = 0.3f;
    public float wallHeight = 2.5f;

    [Header("Visual")]
    public Material fenceMaterial;
    public bool createVisuals = true;

    void Start()
    {
        if (createVisuals) CreateBoundaryWalls();
    }

    void CreateBoundaryWalls()
    {
        // 4개의 벽 생성 (북, 남, 동, 서)
        CreateWall("Wall_North", new Vector3(0, wallHeight * 0.5f, height * 0.5f),
                   new Vector3(width, wallHeight, wallThickness));
        CreateWall("Wall_South", new Vector3(0, wallHeight * 0.5f, -height * 0.5f),
                   new Vector3(width, wallHeight, wallThickness));
        CreateWall("Wall_East", new Vector3(width * 0.5f, wallHeight * 0.5f, 0),
                   new Vector3(wallThickness, wallHeight, height));
        CreateWall("Wall_West", new Vector3(-width * 0.5f, wallHeight * 0.5f, 0),
                   new Vector3(wallThickness, wallHeight, height));
    }

    void CreateWall(string wallName, Vector3 localPos, Vector3 size)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = wallName;
        wall.transform.parent = transform;
        wall.transform.localPosition = localPos;
        wall.transform.localScale = size;

        if (fenceMaterial != null)
            wall.GetComponent<Renderer>().material = fenceMaterial;

        // 콜라이더는 이미 포함되어 있음
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(width, wallHeight, height));
    }
}
