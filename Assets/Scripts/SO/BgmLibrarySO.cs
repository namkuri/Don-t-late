using System;
using System.Collections.Generic;
using UnityEngine;

namespace DontLate
{
    /// <summary>BGM 슬롯. Unsorted 는 추첨에서 제외 — 사람 청취로 분류가 확정되기 전 상태다.</summary>
    public enum BgmSlot
    {
        Unsorted,
        Day,
        Night,
        Title
    }

    /// <summary>
    /// BGM 곡 목록. 곡 컷 = 항목 제거, 분류 변경 = 슬롯 드롭다운 — 둘 다 코드 수정 없이 인스펙터에서 끝난다.
    /// 데이터만 보관한다(로직은 WorldAudioManager 몫).
    /// </summary>
    [CreateAssetMenu(menuName = "DontLate/BgmLibrary")]
    public class BgmLibrarySO : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public AudioClip clip;
            public BgmSlot slot;
        }

        public List<Entry> entries = new List<Entry>();
    }
}
