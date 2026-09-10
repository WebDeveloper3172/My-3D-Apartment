using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public sealed class SteelFrameGenerator : MonoBehaviour
{
    [Header("Overall dimensions, in millimeters")]
    [SerializeField] private float lengthMm = 5100f;
    [SerializeField] private float widthMm = 2790f;
    [SerializeField] private float heightMm = 2120f;

    [Header("Steel profiles, in millimeters")]
    [SerializeField] private Vector2 postAndBeamProfileMm = new Vector2(80f, 80f);
    [SerializeField] private Vector2 joistProfileMm = new Vector2(80f, 40f);
    [SerializeField] private float joistSpacingMm = 350f;

    [Header("Clear stair opening at the +X end, right (+Z) side, in millimeters")]
    [SerializeField] private float stairWidthMm = 700f;
    [SerializeField] private float stairLengthMm = 1500f;

    [Header("Plywood deck, in millimeters")]
    [SerializeField] private bool addPlywoodDeck = true;
    [SerializeField] private float plywoodThicknessMm = 20f;
    [SerializeField] private Color plywoodColor = new Color(0.55f, 0.32f, 0.15f, 1f);

    [Header("Layout")]
    [SerializeField] private int portalFrameCount = 4;
    [SerializeField] private bool addCenterLongitudinalBeam = true;
    [SerializeField] private Color steelColor = new Color(0.08f, 0.075f, 0.065f, 1f);
    [SerializeField] private Color joistColor = new Color(0.18f, 0.17f, 0.14f, 1f);

    private const string GeneratedRootName = "_GeneratedSteelFrame";
    private Material steelMaterial;
    private Material joistMaterial;
    private Material plywoodMaterial;

    private void OnEnable()
    {
        Rebuild();
    }

    private void OnValidate()
    {
        portalFrameCount = Mathf.Max(2, portalFrameCount);
        joistSpacingMm = Mathf.Max(100f, joistSpacingMm);
        lengthMm = Mathf.Max(1000f, lengthMm);
        widthMm = Mathf.Max(500f, widthMm);
        heightMm = Mathf.Max(500f, heightMm);
        plywoodThicknessMm = Mathf.Max(1f, plywoodThicknessMm);
#if UNITY_EDITOR
        // OnValidate may run during deserialization; rebuild on the editor thread.
        UnityEditor.EditorApplication.delayCall -= RebuildInEditor;
        UnityEditor.EditorApplication.delayCall += RebuildInEditor;
#else
        Rebuild();
#endif
    }

#if UNITY_EDITOR
    private void RebuildInEditor()
    {
        if (this != null && isActiveAndEnabled) Rebuild();
    }
#endif

    [ContextMenu("Rebuild Steel Frame")]
    private void Rebuild()
    {
        ClearGeneratedChildren();

        Transform root = new GameObject(GeneratedRootName).transform;
        root.SetParent(transform, false);

        steelMaterial = CreateMaterial("Steel Frame Dark", steelColor);
        joistMaterial = CreateMaterial("Steel Frame Joists", joistColor);
        plywoodMaterial = CreateMaterial("Plywood Deck", plywoodColor);

        float length = MmToMeters(lengthMm);
        float width = MmToMeters(widthMm);
        float height = MmToMeters(heightMm);
        float postSize = MmToMeters(postAndBeamProfileMm.x);
        float beamSize = MmToMeters(postAndBeamProfileMm.y);
        float joistWidth = MmToMeters(joistProfileMm.y);
        float joistHeight = MmToMeters(joistProfileMm.x);

        // Overall dimensions are outside faces of the steel, excluding foot plates.
        float minX = -length * 0.5f + postSize * 0.5f;
        float maxX = length * 0.5f - postSize * 0.5f;
        float minZ = -width * 0.5f + postSize * 0.5f;
        float maxZ = width * 0.5f - postSize * 0.5f;
        float topY = height - beamSize * 0.5f;
        float clearStairWidth = Mathf.Clamp(MmToMeters(stairWidthMm), 0.1f, width - 3f * beamSize);
        float clearStairLength = Mathf.Clamp(MmToMeters(stairLengthMm), 0.1f, length - 3f * beamSize);
        float stairInnerZ = maxZ - beamSize - clearStairWidth;
        float stairHeaderX = maxX - beamSize - clearStairLength;

        List<float> frameXs = BuildEvenPositions(minX, maxX, portalFrameCount);
        // The intermediate portal carries the stair opening's near header.
        if (frameXs.Count > 2) frameXs[frameXs.Count - 2] = stairHeaderX;
        frameXs.Sort();

        foreach (float x in frameXs)
        {
            AddBox(root, $"Post L {x:0.00}", new Vector3(x, (height - beamSize) * 0.5f, minZ), new Vector3(postSize, height - beamSize, postSize), steelMaterial);
            // At the stair end, the right-hand support sits on the inner edge of the opening.
            // This removes the outer posts that would otherwise stand inside the stair void.
            float rightPostZ = x >= stairHeaderX - 0.001f ? stairInnerZ : maxZ;
            AddBox(root, $"Post R {x:0.00}", new Vector3(x, (height - beamSize) * 0.5f, rightPostZ), new Vector3(postSize, height - beamSize, postSize), steelMaterial);
            float endZ = x > stairHeaderX + 0.001f && x < maxX - 0.001f ? stairInnerZ : maxZ;
            AddBox(root, $"Portal Beam {x:0.00}", new Vector3(x, topY, (minZ + endZ) * 0.5f), new Vector3(beamSize, beamSize, endZ - minZ - beamSize), steelMaterial);
        }

        AddBox(root, "Left Longitudinal Beam", new Vector3(0f, topY, minZ), new Vector3(length, beamSize, beamSize), steelMaterial);
        AddBox(root, "Right Longitudinal Beam", new Vector3(0f, topY, maxZ), new Vector3(length, beamSize, beamSize), steelMaterial);
        AddBox(root, "Stair Inner Longitudinal Beam", new Vector3(0f, topY, stairInnerZ), new Vector3(length, beamSize, beamSize), steelMaterial);
        if (!frameXs.Exists(x => Mathf.Abs(x - stairHeaderX) < 0.001f))
        {
            AddBox(root, "Stair Opening Header", new Vector3(stairHeaderX, topY, (stairInnerZ + maxZ) * 0.5f), new Vector3(beamSize, beamSize, maxZ - stairInnerZ - beamSize), steelMaterial);
        }

        if (addCenterLongitudinalBeam)
        {
            AddBox(root, "Main Deck Center Longitudinal Beam", new Vector3(0f, topY, (minZ + stairInnerZ) * 0.5f), new Vector3(length, beamSize, beamSize), steelMaterial);
        }

        if (addPlywoodDeck)
        {
            AddPlywoodDeck(root, length, width, height, beamSize, stairHeaderX, stairInnerZ);
        }

        foreach (float x in BuildJoistPositions(minX, maxX, MmToMeters(joistSpacingMm)))
        {
            if (frameXs.Exists(frameX => Mathf.Abs(x - frameX) < (beamSize + joistWidth) * 0.5f)) continue;
            if (Mathf.Abs(x - stairHeaderX) < (beamSize + joistWidth) * 0.5f) continue;
            float endZ = x + joistWidth * 0.5f > stairHeaderX - beamSize * 0.5f ? stairInnerZ : maxZ;
            // Joists share the top surface and never cross the clear stair opening.
            AddBox(root, $"Deck Joist {x:0.00}", new Vector3(x, height - joistHeight * 0.5f, (minZ + endZ) * 0.5f), new Vector3(joistWidth, joistHeight, endZ - minZ - beamSize), joistMaterial);
        }

        AddFootPlates(root, frameXs, minZ, maxZ, stairHeaderX, stairInnerZ, postSize);
    }

    private void AddPlywoodDeck(Transform root, float length, float width, float steelTopY, float beamSize, float stairHeaderX, float stairInnerZ)
    {
        float thickness = MmToMeters(plywoodThicknessMm);
        float openingMinX = stairHeaderX + beamSize * 0.5f;
        float openingMinZ = stairInnerZ + beamSize * 0.5f;
        float deckMinX = -length * 0.5f;
        float deckMinZ = -width * 0.5f;
        float deckMaxX = length * 0.5f;
        float deckMaxZ = width * 0.5f;
        float deckY = steelTopY + thickness * 0.5f;

        // Two plywood panels cover the complete top while retaining the clear stair opening.
        float mainPanelLength = openingMinX - deckMinX;
        AddBox(root, "Plywood Deck Main", new Vector3(deckMinX + mainPanelLength * 0.5f, deckY, 0f), new Vector3(mainPanelLength, thickness, width), plywoodMaterial);

        float sidePanelLength = deckMaxX - openingMinX;
        float sidePanelWidth = openingMinZ - deckMinZ;
        AddBox(root, "Plywood Deck Stair Side", new Vector3(openingMinX + sidePanelLength * 0.5f, deckY, deckMinZ + sidePanelWidth * 0.5f), new Vector3(sidePanelLength, thickness, sidePanelWidth), plywoodMaterial);
    }

    private void AddFootPlates(Transform root, IReadOnlyList<float> frameXs, float minZ, float maxZ, float stairHeaderX, float stairInnerZ, float postSize)
    {
        float plateSize = postSize * 2.3f;
        float plateHeight = 0.015f;

        foreach (float x in frameXs)
        {
            AddBox(root, $"Foot Plate L {x:0.00}", new Vector3(x, plateHeight * 0.5f, minZ), new Vector3(plateSize, plateHeight, plateSize), steelMaterial);
            float rightPostZ = x >= stairHeaderX - 0.001f ? stairInnerZ : maxZ;
            AddBox(root, $"Foot Plate R {x:0.00}", new Vector3(x, plateHeight * 0.5f, rightPostZ), new Vector3(plateSize, plateHeight, plateSize), steelMaterial);
        }
    }

    private static List<float> BuildEvenPositions(float start, float end, int count)
    {
        var positions = new List<float>(count);
        float step = (end - start) / (count - 1);

        for (int i = 0; i < count; i++)
        {
            positions.Add(start + step * i);
        }

        return positions;
    }

    private static List<float> BuildJoistPositions(float start, float end, float spacing)
    {
        var positions = new List<float>();
        int count = Mathf.FloorToInt((end - start) / spacing);

        for (int i = 0; i <= count; i++)
        {
            positions.Add(start + spacing * i);
        }

        if (positions.Count == 0 || Mathf.Abs(positions[^1] - end) > 0.05f)
        {
            positions.Add(end);
        }

        return positions;
    }

    private static void AddBox(Transform parent, string objectName, Vector3 position, Vector3 scale, Material material)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = objectName;
        box.transform.SetParent(parent, false);
        box.transform.localPosition = position;
        box.transform.localScale = scale;

        if (box.TryGetComponent(out MeshRenderer renderer))
        {
            renderer.sharedMaterial = material;
        }
    }

    private static Material CreateMaterial(string materialName, Color color)
    {
        var material = new Material(Shader.Find("Standard"))
        {
            name = materialName,
            color = color
        };
        material.SetFloat("_Glossiness", 0.22f);
        return material;
    }

    private void ClearGeneratedChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name != GeneratedRootName)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }

    private static float MmToMeters(float value)
    {
        return value * 0.001f;
    }
}
