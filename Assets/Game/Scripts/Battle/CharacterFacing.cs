using UnityEngine;

namespace GeometryTD
{
    public class CharacterFacing : MonoBehaviour
    {
        [SerializeField] private Transform visualRoot;

        public void FaceToward(Vector3 targetPos)
        {
            if (visualRoot == null) return;
            bool faceLeft = targetPos.x < transform.position.x;
            Vector3 s = visualRoot.localScale;
            s.x = faceLeft ? -Mathf.Abs(s.x) : Mathf.Abs(s.x);
            visualRoot.localScale = s;
        }
    }
}
