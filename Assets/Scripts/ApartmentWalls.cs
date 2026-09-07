using UnityEngine;

// All dimensions are meters. The room dimensions are measured between inner faces.
[ExecuteAlways]
public sealed class ApartmentWalls : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float wallThickness = 0.10f;
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
        if (wallThickness <= 0f || roomLength <= 0f || roomWidth <= 0f || roomHeight <= 0f ||
            doorLeftOffset <= 0f || doorWidth <= 0f || doorHeight <= 0f ||
            doorLeftOffset + doorWidth >= roomWidth || doorHeight >= roomHeight ||
            windowWidth <= 0f || windowWidth >= roomWidth || windowHeight <= 0f ||
            windowSill <= 0f || windowSill + windowHeight >= roomHeight)
        {
            Debug.LogWarning("Wall dimensions must leave positive sections around the openings.", this);
            return;
        }

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
    }

    private static void Set(Transform section, float x, float y, float z, float sx, float sy, float sz)
    {
        if (section == null) return;
        section.localPosition = new Vector3(x, y, z);
        section.localRotation = Quaternion.identity;
        section.localScale = new Vector3(sx, sy, sz);
    }
}
