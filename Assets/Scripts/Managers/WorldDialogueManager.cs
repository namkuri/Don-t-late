using System;
using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 대사 시나리오 재생. Core 상주 World 싱글톤 — 라인 인덱스만 관리하고 표시는 DialogueView 몫.
    /// 시작/종료는 WorldEvents(저빈도 경계)로, 라인 전환은 LineChanged(도메인 내 UI 통지)로 흘린다.
    /// </summary>
    public class WorldDialogueManager : MonoBehaviour
    {
        public static WorldDialogueManager Instance { get; private set; }

        /// <summary>현재 라인 통지. WorldEvents가 아니라 여기 두는 이유는 라인 단위가 준-고빈도라서다.
        /// DialogueView만 구독한다. 정적이라 View의 구독이 매니저 Awake 순서에 영향받지 않는다.</summary>
        public static event Action<DialogueScenarioSO.Line> LineChanged;

        private DialogueScenarioSO _current;
        private int _index;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>시나리오 재생 시작. Started 발행 후 첫 라인을 통지한다.</summary>
        public void PlayScenario(DialogueScenarioSO scenario)
        {
            if (scenario == null || scenario.lines == null || scenario.lines.Length == 0) return;

            _current = scenario;
            _index = 0;
            WorldEvents.RaiseDialogueStarted(scenario.name);
            LineChanged?.Invoke(_current.lines[0]);
        }

        /// <summary>
        /// View가 진행 입력을 받았을 때 호출(Instance 명령). 타이핑 스킵은 View가 자체 처리하므로
        /// 여기 도달하면 항상 "현재 라인 완료 → 다음 라인 또는 종료"를 뜻한다.
        /// </summary>
        public void AdvanceRequested()
        {
            if (_current == null) return;

            _index++;
            if (_index >= _current.lines.Length)
            {
                string finished = _current.name;
                _current = null;
                WorldEvents.RaiseDialogueEnded(finished);
                return;
            }

            LineChanged?.Invoke(_current.lines[_index]);
        }
    }
}
