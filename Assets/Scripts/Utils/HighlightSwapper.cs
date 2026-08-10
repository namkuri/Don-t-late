using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// S-248 — 오브젝트 하나가 아니라 **트리 전체**를 하이라이트 머티리얼로 갈아끼운다.
    ///
    /// 종전 트럭 인터랙트는 몸통 렌더러 하나만 스왑해서, 아트 모델이 붙자 하이라이트가 몸통에만
    /// 걸렸다(남규님 지시: "트럭이랑 같이 되게끔"). 파트마다 원래 머티리얼이 다르므로
    /// **각자의 원본을 기억했다가 되돌린다** — 공용 `_normalMaterial` 하나로는 복원이 틀린다.
    /// </summary>
    public sealed class HighlightSwapper
    {
        private readonly Renderer[] _renderers;
        private readonly Material[] _originals;

        public HighlightSwapper(Transform root)
        {
            _renderers = root != null ? root.GetComponentsInChildren<Renderer>(true) : new Renderer[0];
            _originals = new Material[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
                _originals[i] = _renderers[i] != null ? _renderers[i].sharedMaterial : null;
        }

        public void Set(bool on, Material highlight)
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null) continue;
                _renderers[i].sharedMaterial = on && highlight != null ? highlight : _originals[i];
            }
        }
    }
}
