using UnityEngine;
using UnityEngine.UI;

namespace KiKs.UI
{
    /// <summary>
    /// 水平切变：上下边不动，左右边倾斜，形成平行四边形。
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    public class CardSkew : BaseMeshEffect
    {
        public float skew = 0f;

        public float Skew
        {
            get => skew;
            set { skew = value; graphic?.SetVerticesDirty(); }
        }

        public override void ModifyMesh(VertexHelper verts)
        {
            if (!IsActive() || verts.currentVertCount == 0 || Mathf.Abs(skew) < 0.001f) return;

            float minY = float.MaxValue, maxY = float.MinValue;
            for (int i = 0; i < verts.currentVertCount; i++)
            {
                UIVertex v = new UIVertex();
                verts.PopulateUIVertex(ref v, i);
                if (v.position.y < minY) minY = v.position.y;
                if (v.position.y > maxY) maxY = v.position.y;
            }

            float height = maxY - minY;
            if (height < 0.001f) return;

            for (int i = 0; i < verts.currentVertCount; i++)
            {
                UIVertex v = new UIVertex();
                verts.PopulateUIVertex(ref v, i);
                // 归一化Y：-1(底) ~ 1(顶)
                float ny = ((v.position.y - minY) / height - 0.5f) * 2f;
                v.position.x += skew * ny;
                verts.SetUIVertex(v, i);
            }
        }
    }
}
