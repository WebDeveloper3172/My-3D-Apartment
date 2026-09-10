using UnityEngine;

// All dimensions are meters. The room dimensions are measured between inner faces.
[ExecuteAlways]
public sealed class ApartmentWalls : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float wallThickness = 0.10f;
    [SerializeField, Min(0.01f)] private float floorThickness = 0.10f;
    [SerializeField] private Transform floor;
    [SerializeField] private float entranceX = -2.55f;
    [SerializeField] private float roomLength = 8.30f;
    [SerializeField] private float roomWidth = 2.77f;
    [SerializeField] private float roomHeight = 3.80f;
    [SerializeField] private float doorLeftOffset = 0.40f;
    [SerializeField] private float doorWidth = 0.90f;
    [SerializeField] private float doorHeight = 2.05f;
    [SerializeField] private float windowWidth = 1.52f;
    [SerializeField] private float windowHeight = 2.27f;
    [SerializeField] private float windowSill = 0.90f;

    [Header("Bathroom under the upper deck (meters)")]
    [SerializeField] private bool addBathroom = true;
    [SerializeField, Min(0.01f)] private float bathroomWidth = 1.54f;
    [SerializeField, Min(0.01f)] private float bathroomLength = 2.00f;
    [SerializeField, Min(0.01f)] private float bathroomDoorWidth = 0.60f;
    [SerializeField, Min(0.01f)] private float bathroomDoorOffset = 0.40f;
    [SerializeField, Min(0.01f)] private float bathroomWallHeight = 2.00f;

    [Header("Existing wall sections (no generated replacement objects)")]
    [SerializeField] private Transform entranceLeft;
    [SerializeField] private Transform entranceRight;
    [SerializeField] private Transform entranceTop;
    [SerializeField] private Transform windowLeft;
    [SerializeField] private Transform windowRight;
    [SerializeField] private Transform windowBottom;
    [SerializeField] private Transform windowTop;
    [SerializeField] private Transform leftSide;
    [SerializeField] private Transform rightSide;

    private void OnEnable() => ApplyLayout();

#if UNITY_EDITOR
    private void OnValidate()
    {
        UnityEditor.EditorApplication.delayCall -= ApplyDeferred;
        UnityEditor.EditorApplication.delayCall += ApplyDeferred;
    }

    private void ApplyDeferred()
    {
        if (this != null && isActiveAndEnabled) ApplyLayout();
    }

    private void OnDisable() => UnityEditor.EditorApplication.delayCall -= ApplyDeferred;
#endif

    [ContextMenu("Apply Wall Dimensions")]
    public void ApplyLayout()
    {
        if (floorThickness <= 0f || wallThickness <= 0f || roomLength <= 0f || roomWidth <= 0f || roomHeight <= 0f ||
            doorLeftOffset <= 0f || doorWidth <= 0f || doorHeight <= 0f ||
            doorLeftOffset + doorWidth >= roomWidth || doorHeight >= roomHeight ||
            windowWidth <= 0f || windowWidth >= roomWidth || windowHeight <= 0f ||
            windowSill <= 0f || windowSill + windowHeight >= roomHeight)
        {
            Debug.LogWarning("Wall dimensions must leave positive sections around the openings.", this);
            return;
        }

        // The top of the floor is exactly at Y = 0.
        Set(floor, entranceX + roomLength / 2f, -floorThickness / 2f, 0f,
            roomLength, floorThickness, roomWidth);

        // Looking toward the entrance from inside (-X), left is -Z.
        float zMin = -roomWidth / 2f;
        float doorStart = zMin + doorLeftOffset;
        float doorEnd = doorStart + doorWidth;
        float front = entranceX - wallThickness / 2f;
        float back = entranceX + roomLength + wallThickness / 2f;
        Set(entranceLeft, front, roomHeight / 2f, zMin + doorLeftOffset / 2f,
            wallThickness, roomHeight, doorLeftOffset);
        Set(entranceRight, front, roomHeight / 2f, (doorEnd + roomWidth / 2f) / 2f,
            wallThickness, roomHeight, roomWidth - doorLeftOffset - doorWidth);
        Set(entranceTop, front, (doorHeight + roomHeight) / 2f, (doorStart + doorEnd) / 2f,
            wallThickness, roomHeight - doorHeight, doorWidth);

        float jamb = (roomWidth - windowWidth) / 2f;
        Set(windowLeft, back, roomHeight / 2f, zMin + jamb / 2f, wallThickness, roomHeight, jamb);
        Set(windowRight, back, roomHeight / 2f, -zMin - jamb / 2f, wallThickness, roomHeight, jamb);
        Set(windowBottom, back, windowSill / 2f, 0f, wallThickness, windowSill, windowWidth);
        Set(windowTop, back, (windowSill + windowHeight + roomHeight) / 2f, 0f,
            wallThickness, roomHeight - windowSill - windowHeight, windowWidth);
        Set(leftSide, entranceX + roomLength / 2f, roomHeight / 2f, -(roomWidth + wallThickness) / 2f,
            roomLength, roomHeight, wallThickness);
        Set(rightSide, entranceX + roomLength / 2f, roomHeight / 2f, (roomWidth + wallThickness) / 2f,
            roomLength, roomHeight, wallThickness);

        RebuildBathroom();
    }

    private void RebuildBathroom()
    {
        const string bathroomRootName = "Bathroom";
        Transform previous = transform.Find(bathroomRootName);
        if (previous != null)
        {
            if (Application.isPlaying) Destroy(previous.gameObject);
            else DestroyImmediate(previous.gameObject);
        }

        if (!addBathroom) return;

        // Entering through the -X wall, the apartment's left side is +Z.
        // The existing +Z exterior wall is reused as the bathroom's outer wall.
        float exteriorInnerZ = roomWidth * 0.5f;
        float bathroomInnerMinZ = exteriorInnerZ - bathroomWidth;
        float partitionZ = bathroomInnerMinZ - wallThickness * 0.5f;
        float entranceInnerX = entranceX;
        float rearWallX = entranceInnerX + bathroomLength + wallThickness * 0.5f;
        float wallY = bathroomWallHeight * 0.5f;
        float doorOffset = Mathf.Clamp(bathroomDoorOffset, 0f, bathroomLength - bathroomDoorWidth);
        float afterDoor = bathroomLength - doorOffset - bathroomDoorWidth;

        Transform bathroom = new GameObject(bathroomRootName).transform;
        bathroom.SetParent(transform, false);

        Material wallMaterial = entranceLeft != null && entranceLeft.TryGetComponent(out MeshRenderer renderer)
            ? renderer.sharedMaterial
            : null;

        // Partition along the 2 m bathroom length, split into segments around the 0.60 m door opening.
        AddBathroomWall(bathroom, "BathroomWall_DoorLeft", new Vector3(entranceInnerX + doorOffset * 0.5f, wallY, partitionZ), new Vector3(doorOffset, bathroomWallHeight, wallThickness), wallMaterial);
        AddBathroomWall(bathroom, "BathroomWall_DoorRight", new Vector3(entranceInnerX + doorOffset + bathroomDoorWidth + afterDoor * 0.5f, wallY, partitionZ), new Vector3(afterDoor, bathroomWallHeight, wallThickness), wallMaterial);

        // Closing wall at the end of the 2 m length; it meets the existing exterior wall at +Z.
        AddBathroomWall(bathroom, "BathroomWall_Back", new Vector3(rearWallX, wallY, (bathroomInnerMinZ + exteriorInnerZ) * 0.5f), new Vector3(wallThickness, bathroomWallHeight, bathroomWidth + wallThickness), wallMaterial);
    }

    private static void AddBathroomWall(Transform parent, string wallName, Vector3 position, Vector3 scale, Material material)
    {
        if (scale.x <= 0f || scale.y <= 0f || scale.z <= 0f) return;

        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = wallName;
        wall.transform.SetParent(parent, false);
        wall.transform.localPosition = position;
        wall.transform.localScale = scale;

        if (material != null && wall.TryGetComponent(out MeshRenderer renderer))
        {
            renderer.sharedMaterial = material;
        }
    }

    private static void Set(Transform section, float x, float y, float z, float sx, float sy, float sz)
    {
        if (section == null) return;
        section.localPosition = new Vector3(x, y, z);
        section.localRotation = Quaternion.identity;
        section.localScale = new Vector3(sx, sy, sz);
    }
}
