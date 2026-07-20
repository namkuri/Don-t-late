namespace DontLate
{
    /// <summary>
    /// 월드 상호작용 계약. 시그니처는 동결 — 변경이 필요하면 구현 전에 사람에게 묻는다.
    /// </summary>
    public interface IInteractable
    {
        void Interact(PlayerContext ctx);
        void SetHighlight(bool on);
    }

    /// <summary>포커스 조건을 추가 제한하려는 상호작용물이 선택 구현. 센서가 후보 선별 시 검문한다.</summary>
    public interface IFocusGate { bool AllowsFocus(UnityEngine.Vector3 playerPosition); }
}
