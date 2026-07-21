using System;
using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 대사 시나리오 1편의 정적 데이터. 로직 없음 — 재생은 WorldDialogueManager 몫.
    /// LLM 배치 생성물을 이 에셋에 구워 넣는다(런타임 외부 호출 금지).
    /// </summary>
    [CreateAssetMenu(menuName = "DontLate/DialogueScenario")]
    public class DialogueScenarioSO : ScriptableObject
    {
        [Serializable]
        public class Line
        {
            public string speaker;
            [TextArea] public string text;
            [Tooltip("초상 스프라이트 — 아트 도착 전까지 자리만.")]
            public Sprite portrait;
        }

        public Line[] lines;
    }
}
