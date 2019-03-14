using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

/// <summary>
/// uGUI 描边
/// </summary>
public class UIOutline : BaseMeshEffect
{
    private static readonly List<UIVertex> Vertices = new List<UIVertex>();
    private const AdditionalCanvasShaderChannels TangentShaderChannels = AdditionalCanvasShaderChannels.Tangent;

    private static readonly int Color = Shader.PropertyToID("_OutlineColor");
    private static readonly int Width = Shader.PropertyToID("_OutlineWidth");

    // 描边颜色
    [SerializeField] private Color outlineColor = UnityEngine.Color.white;

    // 描边宽度
    [SerializeField] [Range(0, 6)] private int outlineWidth = 0;

    // 初始材质
    [SerializeField] private Material originMaterial;

    // 是否创建材质实例
    [SerializeField] private bool createMaterialInstance = true;

    private Material _material;

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        Refresh();
    }
#endif

    protected override void Awake()
    {
        base.Awake();

        if (originMaterial == null)
        {
            var shader = Shader.Find("UI/Outline");
            originMaterial = new Material(shader);
        }

        _material = createMaterialInstance ? Instantiate(originMaterial) : originMaterial;

        var thisGraphic = graphic;
        thisGraphic.material = _material;

        var canvasAdditionalShaderChannels = thisGraphic.canvas.additionalShaderChannels;
        if ((canvasAdditionalShaderChannels & TangentShaderChannels) != TangentShaderChannels)
        {
            graphic.canvas.additionalShaderChannels |= TangentShaderChannels;
        }

        Refresh();
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        vh.GetUIVertexStream(Vertices);

        ProcessVertices();

        vh.Clear();
        vh.AddUIVertexTriangleStream(Vertices);
    }

    private void Refresh()
    {
        if (_material == null)
        {
            if (originMaterial == null)
            {
                var shader = Shader.Find("UI/Outline");
                originMaterial = new Material(shader);
            }

            _material = createMaterialInstance ? Instantiate(originMaterial) : originMaterial;
        }

        if (_material != null)
        {
            _material.SetColor(Color, outlineColor);
            _material.SetInt(Width, outlineWidth);
            graphic.SetVerticesDirty();
        }
    }

    private void ProcessVertices()
    {
        for (int i = 0, count = Vertices.Count - 3; i <= count; i += 3)
        {
            var v1 = Vertices[i];
            var v2 = Vertices[i + 1];
            var v3 = Vertices[i + 2];

            var minX = Min(v1.position.x, v2.position.x, v3.position.x);
            var minY = Min(v1.position.y, v2.position.y, v3.position.y);
            var maxX = Max(v1.position.x, v2.position.x, v3.position.x);
            var maxY = Max(v1.position.y, v2.position.y, v3.position.y);
            var posCenter = new Vector2(minX + maxX, minY + maxY) * 0.5f;

            Vector2 triX, triY, uvX, uvY;
            Vector2 pos1 = v1.position;
            Vector2 pos2 = v2.position;
            Vector2 pos3 = v3.position;
            if (Mathf.Abs(Vector2.Dot((pos2 - pos1).normalized, Vector2.right))
                > Mathf.Abs(Vector2.Dot((pos3 - pos2).normalized, Vector2.right)))
            {
                triX = pos2 - pos1;
                triY = pos3 - pos2;
                uvX = v2.uv0 - v1.uv0;
                uvY = v3.uv0 - v2.uv0;
            }
            else
            {
                triX = pos3 - pos2;
                triY = pos2 - pos1;
                uvX = v3.uv0 - v2.uv0;
                uvY = v2.uv0 - v1.uv0;
            }

            var uvMin = Min(v1.uv0, v2.uv0, v3.uv0);
            var uvMax = Max(v1.uv0, v2.uv0, v3.uv0);
            var uvOrigin = new Vector4(uvMin.x, uvMin.y, uvMax.x, uvMax.y);

            v1 = UpdatePosAndUV(v1, outlineWidth, posCenter, triX, triY, uvX, uvY, uvOrigin);
            v2 = UpdatePosAndUV(v2, outlineWidth, posCenter, triX, triY, uvX, uvY, uvOrigin);
            v3 = UpdatePosAndUV(v3, outlineWidth, posCenter, triX, triY, uvX, uvY, uvOrigin);

            Vertices[i] = v1;
            Vertices[i + 1] = v2;
            Vertices[i + 2] = v3;
        }
    }

    private static UIVertex UpdatePosAndUV(
        UIVertex vertex,
        int outlineWidth,
        Vector2 posCenter,
        Vector2 triangleX,
        Vector2 triangleY,
        Vector2 uvx,
        Vector2 uvy,
        Vector4 uvOrigin)
    {
        var pos = vertex.position;
        var posXOffset = pos.x > posCenter.x ? outlineWidth : -outlineWidth;
        var posYOffset = pos.y > posCenter.y ? outlineWidth : -outlineWidth;
        pos.x += posXOffset;
        pos.y += posYOffset;
        vertex.position = pos;

        var uv = vertex.uv0;
        uv += uvx / triangleX.magnitude * posXOffset * (Vector2.Dot(triangleX, Vector2.right) > 0 ? 1 : -1);
        uv += uvy / triangleY.magnitude * posYOffset * (Vector2.Dot(triangleY, Vector2.up) > 0 ? 1 : -1);
        vertex.uv0 = uv;

        vertex.tangent = uvOrigin;

        return vertex;
    }

    private static float Min(float a, float b, float c)
    {
        return Mathf.Min(Mathf.Min(a, b), c);
    }

    private static float Max(float a, float b, float c)
    {
        return Mathf.Max(Mathf.Max(a, b), c);
    }

    private static Vector2 Min(Vector2 a, Vector2 b, Vector2 c)
    {
        return new Vector2(Min(a.x, b.x, c.x), Min(a.y, b.y, c.y));
    }

    private static Vector2 Max(Vector2 a, Vector2 b, Vector2 c)
    {
        return new Vector2(Max(a.x, b.x, c.x), Max(a.y, b.y, c.y));
    }
}