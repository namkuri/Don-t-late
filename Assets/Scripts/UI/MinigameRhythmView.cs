using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DontLate
{
    /// <summary>
    /// 방향키 리듬 오버레이(View) — S-007. MinigameRequested를 받아 패널을 열고
    /// 방향키 시퀀스를 표시·판정한 뒤 결과를 WorldMinigameManager.SubmitResult로 넘긴다.
    /// 난이도는 성공/실패 2단뿐(SCOPE sacrifice ①). 게임 반영은 하지 않는다.
    /// </summary>
    public class MinigameRhythmView : MonoBehaviour
    {
        private static readonly string[] ARROW_GLYPHS = { "←", "↑", "→", "↓" };

        [SerializeField] private TuningConfigSO _tuning;
        [Tooltip("오버레이 패널 루트. 평소엔 꺼져 있다.")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_Text _titleLabel;
        [SerializeField] private TMP_Text _sequenceLabel;

        private Coroutine _running;

        private void OnEnable() => WorldEvents.MinigameRequested += OnRequested;

        private void OnDisable()
        {
            WorldEvents.MinigameRequested -= OnRequested;
            if (_running != null) { StopCoroutine(_running); _running = null; }
            if (_panel != null) _panel.SetActive(false);
        }

        private void OnRequested()
        {
            if (_running != null) return;
            _running = StartCoroutine(RunSequence());
        }

        private IEnumerator RunSequence()
        {
            int count = Mathf.Max(1, _tuning.minigameKeyCount);
            var sequence = new int[count];
            for (int i = 0; i < count; i++) sequence[i] = Random.Range(0, 4);

            _panel.SetActive(true);
            if (_titleLabel != null) _titleLabel.text = "진상 전화! 방향키를 따라 눌러라";

            int hit = 0;
            for (int i = 0; i < count; i++)
            {
                ShowSequence(sequence, i);

                float remain = _tuning.minigameKeyStepSeconds;
                bool judged = false;
                while (remain > 0f && !judged)
                {
                    int pressed = ReadArrowPressed();
                    if (pressed >= 0)
                    {
                        if (pressed == sequence[i])
                        {
                            hit++;
                            WorldAudioManager.Instance?.PlayRhythmHitSfx(); // AU-009 — 노트당 1회
                        }
                        else
                        {
                            WorldAudioManager.Instance?.PlayRhythmMissSfx(); // AU-009
                        }
                        judged = true;
                    }
                    remain -= Time.deltaTime;
                    yield return null;
                }
                if (!judged) WorldAudioManager.Instance?.PlayRhythmMissSfx(); // AU-009 — 타임아웃도 미스
            }

            _panel.SetActive(false);
            _running = null;

            var result = new MinigameResult
            {
                HitCount = hit,
                TotalCount = count,
                Accuracy = (float)hit / count,
                Success = hit == count
            };
            WorldMinigameManager.Instance.SubmitResult(result);
        }

        /// <summary>진행 위치를 강조해 시퀀스를 그린다. 지나간 키는 흐리게.</summary>
        private void ShowSequence(int[] sequence, int cursor)
        {
            if (_sequenceLabel == null) return;

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < sequence.Length; i++)
            {
                if (i == cursor) sb.Append("<color=#35e0c8><b>").Append(ARROW_GLYPHS[sequence[i]]).Append("</b></color>");
                else if (i < cursor) sb.Append("<color=#3a4152>").Append(ARROW_GLYPHS[sequence[i]]).Append("</color>");
                else sb.Append(ARROW_GLYPHS[sequence[i]]);
                sb.Append(' ');
            }
            _sequenceLabel.text = sb.ToString();
        }

        /// <summary>이번 프레임에 눌린 방향키 인덱스(←0 ↑1 →2 ↓3), 없으면 -1.</summary>
        private static int ReadArrowPressed()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return -1;
            if (kb.leftArrowKey.wasPressedThisFrame) return 0;
            if (kb.upArrowKey.wasPressedThisFrame) return 1;
            if (kb.rightArrowKey.wasPressedThisFrame) return 2;
            if (kb.downArrowKey.wasPressedThisFrame) return 3;
            return -1;
        }
    }
}
