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
}
