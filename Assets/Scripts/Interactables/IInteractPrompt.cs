namespace DontLate
{
    /// <summary>
    /// S-249 — 상호작용 대상이 [E] 안내 문구를 **직접** 정한다.
    ///
    /// 종전 HUD의 [E] 줄은 "[E] 상호작용" 고정이고, 배송지만 주소를 병기하는 특례가 하드코딩돼 있었다.
    /// 값이 붙은 선택지(트럭 구매 — 빚 1,000원)는 화면에 그 값이 보여야 선택이 된다.
    ///
    /// <see cref="IInteractable"/>은 동결이라(CODE_RULES §6) 건드리지 않고 곁가지로 붙인다
    /// — <see cref="IFocusGate"/>와 같은 방식이다. 구현하지 않은 대상은 종전 문구 그대로다.
    /// </summary>
    public interface IInteractPrompt
    {
        /// <summary>[E] 줄에 그대로 쓸 문장. 비면 기본 문구로 떨어진다.</summary>
        string PromptText { get; }
    }
}
