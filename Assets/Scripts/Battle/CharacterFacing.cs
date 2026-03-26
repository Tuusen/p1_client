using UnityEngine;

namespace GeometryTD
{
    public class CharacterFacing : MonoBehaviour
    {
        private const int CharacterSortOrder = 5;

        [SerializeField] private Transform visualRoot;

        private void Awake()
        {
            if (visualRoot == null) return;
            foreach (var sr in visualRoot.GetComponentsInChildren<SpriteRenderer>())
                sr.sortingOrder = CharacterSortOrder;
        }

        public void FaceToward(Vector3 targetPos)
        {
            if (visualRoot == null) return;
            bool faceLeft = targetPos.x < transform.position.x;
            Vector3 s = visualRoot.localScale;
            s.x = faceLeft ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
            visualRoot.localScale = s;
        }
    }
}
