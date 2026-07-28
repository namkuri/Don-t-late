using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// NPC 프로필 (S-079 ④ — 남규님 발주, 매니페스트 직교 추가). 소셜앱·호감도의 데이터 원장.
    /// 데이터만 — 호감도 수치는 GameStateSO.npcAffinities, 증감 로직은 NpcAffinityLedger 몫.
    /// </summary>
    [CreateAssetMenu(menuName = "DontLate/Npc")]
    public class NpcSO : ScriptableObject
    {
        public string npcId;
        public string displayName;
        [Tooltip("프로필 초상 — 실아트 소켓 (비면 소셜앱이 색 블록+이니셜 플레이스홀더).")]
        public Sprite portrait;
        [TextArea] public string intro;
        [Tooltip("플레이스홀더 프로필 색 (실아트 전).")]
        public Color placeholderColor = new Color(0.45f, 0.52f, 0.62f);
    }
}
