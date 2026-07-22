using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// Home 벽지·바닥 색 적용기 (S-031 ④). 폰 가구앱이 GameState의 팔레트 인덱스를 바꾸면
    /// 여기서 MaterialPropertyBlock으로 반영한다(공유 머티리얼 에셋 무오염).
    /// 프레임 데이터가 아니라 인덱스 비교 캐시로 변화 시에만 적용 — 이벤트 불요(도메인 내 시각 적용).
    /// </summary>
    public class HomeDecorator : MonoBehaviour
    {
        // 팔레트 — 폰 UI의 순환 버튼과 인덱스 공유 (0 = 기본).
        public static readonly (string name, Color color)[] WallPalette =
        {
            ("기본 베이지", new Color(0.55f, 0.52f, 0.46f)),
            ("크림", new Color(0.78f, 0.73f, 0.62f)),
            ("민트", new Color(0.55f, 0.72f, 0.65f)),
            ("로즈", new Color(0.72f, 0.55f, 0.58f)),
        };

        public static readonly (string name, Color color)[] FloorPalette =
        {
            ("원목", new Color(0.42f, 0.35f, 0.27f)),
            ("그레이", new Color(0.45f, 0.46f, 0.48f)),
            ("네이비", new Color(0.22f, 0.26f, 0.36f)),
            ("체리", new Color(0.38f, 0.24f, 0.22f)),
        };

        [SerializeField] private GameStateSO _gameState;
        [SerializeField] private Renderer[] _wallRenderers;
        [SerializeField] private Renderer[] _floorRenderers;

        private int _appliedWall = -1;
        private int _appliedFloor = -1;
        private MaterialPropertyBlock _block;

        private void Update()
        {
            if (_gameState == null) return;
            if (_gameState.wallpaperIndex == _appliedWall && _gameState.floorIndex == _appliedFloor) return;

            _appliedWall = _gameState.wallpaperIndex;
            _appliedFloor = _gameState.floorIndex;
            _block ??= new MaterialPropertyBlock();

            Apply(_wallRenderers, WallPalette[Mathf.Abs(_appliedWall) % WallPalette.Length].color);
            Apply(_floorRenderers, FloorPalette[Mathf.Abs(_appliedFloor) % FloorPalette.Length].color);
        }

        private void Apply(Renderer[] renderers, Color color)
        {
            if (renderers == null) return;
            _block.SetColor("_BaseColor", color);
            foreach (Renderer renderer in renderers)
                if (renderer != null) renderer.SetPropertyBlock(_block);
        }
    }
}
