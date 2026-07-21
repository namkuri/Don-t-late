#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""bgm_rules.py — `rules/GAME-BGM-RULES.md` 규격을 코드로 집행한다.

출처: scripts/audio/rules/ (원본 = C:/Works/Game/Don-t-late-bgm/)
  · GAME-BGM-RULES.md   — 게임 BGM 공용 규격 (충돌 시 이 문서가 이긴다)
  · afternoon-bgm-02.md — 낮 스타일 팩 (낮/밤 무드 번역표 포함)
  · night-bgm.md        — 밤 스타일 팩

규격의 핵심 통찰(§0): 프롬프트는 완성품을 뽑는 도구가 아니라 **편집 비용을 낮추는 도구**다.
심리스 루프·러드니스 통일은 프롬프트로 불가능하고 편집으로만 된다 — 그래서 프롬프트를 낼 때
「편집 인계」 블록(§5)을 함께 내서 "어디를 자를지"를 곡과 동시에 손에 쥐게 한다.
"""

# §1 필수 포함 태그 — 스타일 태그 다음, BPM 앞에 배치한다.
MANDATORY_TAGS = [
    "no intro fade-in", "no outro fade-out", "no ending",
    "continuous groove from start to finish",
    "consistent dynamics", "steady level", "no dramatic build-ups", "no drops",
    "background music for a game", "understated melody", "texture-driven", "unobtrusive",
    "minimal arrangement", "sparse midrange", "leave space in the mids",
]

# §3 금지 태그 — 상시 BGM 한정. 타이틀·트레일러·엔딩(단발 연출)은 예외.
BANNED_TAGS = [
    "catchy hook", "catchy synth hook", "memorable melody", "anthemic",
    "energetic build", "epic ending", "climax", "final chord", "big finish",
]

# §4 루프 난이도 🔴 — 금지가 아니라 비용 고지. 포함되면 편집 경고를 남긴다.
LOOP_RISK_TAGS = [
    "hazy", "dreamy", "spacious", "cavernous reverb", "long tail",
    "vinyl warmth", "tape hiss", "dusty", "lo-fi",
]
EDIT_WARNING = "⚠ 편집 경고: 긴 잔향/지속 노이즈 포함. 루프 이음매에 테일 랩 필요."

# 스타일 팩 — 각 md의 "기본 프롬프트"와 무드 번역표에서 옮긴 것.
STYLES = {
    "day": {   # afternoon-bgm-02.md · 낮(오후·마을) 기준
        "anchors": [
            "major key city pop", "retro 80s", "bright FM electric piano",
            "sparkling bell synth", "punchy analog synths", "driving synth bass",
            "bright arpeggiated synth", "clean bright synth lead",
            "crisp dry drum machine", "glossy pads", "warm analog",
        ],
        "key": "major key",
        "bpm": 105,
        "negatives": ["no vocals", "no jazz", "no saxophone", "no acoustic guitar",
                      "no neon", "no nighttime mood"],
    },
    "night": {   # night-bgm.md · 앵커 트랙 "Every Day Is Night"
        "anchors": [
            "downtempo synthwave city pop", "lo-fi", "warm analog synth pads",
            "round mellow synth bass", "dreamy nostalgic lead synth",
            "dusty laid-back drum machine beat", "soft bell tones", "vinyl warmth",
            "jazzy 7th chords",
        ],
        "key": "minor key",
        "bpm": 88,
        "negatives": ["no vocals", "no EDM drops"],
    },
    "title": {   # §3 예외 — 단발 연출이라 훅이 있어야 한다
        "anchors": [
            "electric synthwave city pop", "retro 80s", "punchy analog synths",
            "driving synth bass", "bright arpeggiated synth",
            "gated reverb drum machine", "neon retrowave lead", "glossy pads",
        ],
        "key": "minor key",
        "bpm": 104,
        "negatives": ["no vocals", "no jazz", "no acoustic guitar"],
        "oneshot": True,   # 금지 태그·필수 태그 규칙 면제
    },
}

# 네거티브 태그 ↔ 그것이 금지하는 대상. 감독이 명시 요청한 악기는 네거티브에서 뺀다.
NEGATIVE_SUBJECT = {
    "no saxophone": ["saxophone", "sax"],
    "no jazz": ["jazz", "jazzy"],
    "no acoustic guitar": ["acoustic guitar"],
    "no neon": ["neon"],
    "no nighttime mood": ["nighttime", "night drive"],
    "no vocals": ["vocal", "choir", "vocoder"],
    "no EDM drops": ["edm", "drop"],
}


def bar_seconds(bpm):
    """1마디(4/4) 길이 = 240 ÷ BPM."""
    return 240.0 / bpm


def resolve_negatives(style_negatives, creative_tags):
    """감독이 요청한 악기를 금지하는 네거티브는 제거한다 — 노트가 규격 기본값을 이긴다."""
    joined = " ".join(creative_tags).lower()
    kept, dropped = [], []
    for neg in style_negatives:
        subjects = NEGATIVE_SUBJECT.get(neg, [])
        if any(s in joined for s in subjects):
            dropped.append(neg)
        else:
            kept.append(neg)
    return kept, dropped


def compose_bgm(style_name, creative_tags, bpm, length_s, extra_negatives=None):
    """§1·§2·§3 규격을 적용해 단일 프롬프트 블록을 만든다.

    배치 순서(각 md의 출력 규칙 2·3): 앵커 → 창작 태그 → 조성 → 필수 태그 → instrumental → BPM → 네거티브
    """
    style = STYLES[style_name]
    oneshot = style.get("oneshot", False)

    parts = list(style["anchors"]) + list(creative_tags) + [style["key"]]
    if not oneshot:
        parts += MANDATORY_TAGS
    parts.append("instrumental")
    parts.append(f"{bpm} BPM")

    negatives, dropped = resolve_negatives(style["negatives"], creative_tags)
    negatives += list(extra_negatives or [])
    parts += negatives

    # 중복 태그 제거 — 같은 태그가 두 번 들어가면 모델 가중치가 왜곡된다. 순서는 보존.
    seen, uniq = set(), []
    for t in parts:
        k = t.strip().lower()
        if k and k not in seen:
            seen.add(k)
            uniq.append(t.strip())
    parts = uniq

    prompt = ", ".join(parts)
    warnings = [t for t in LOOP_RISK_TAGS if t in prompt.lower()]
    return prompt, {"dropped_negatives": dropped, "loop_risk": warnings, "oneshot": oneshot}


def verify_bgm(prompt, bpm, length_s, loop_bars, oneshot=False):
    """규격 위반을 돌려준다(빈 리스트 = 통과)."""
    low = prompt.lower()
    problems = []

    if not isinstance(bpm, int):
        problems.append("§2 BPM은 정수 1개여야 한다(범위 금지)")
    if "instrumental" not in low:
        problems.append("§2 instrumental 누락")
    if "major key" not in low and "minor key" not in low:
        problems.append("§2 조성(major/minor key) 미명시")

    if not oneshot:
        for t in MANDATORY_TAGS:
            if t.lower() not in low:
                problems.append(f"§1 필수 태그 누락: {t}")
        for t in BANNED_TAGS:
            if t.lower() in low:
                problems.append(f"§3 금지 태그 포함: {t}")
        loop_s = bar_seconds(bpm) * loop_bars
        if length_s < loop_s * 2:
            need = loop_s * 2
            half = bar_seconds(bpm) * (loop_bars // 2)
            problems.append(
                f"§2 길이 부족: 요청 {length_s:.0f}s < 목표 루프 {loop_s:.1f}s × 2 = {need:.0f}s\n"
                f"    (편집은 덜어내는 작업 — 재료가 딱 맞으면 실패한다)\n"
                f"    해법 ①  --length {int(need) + 1}          (길이를 늘린다 · 권장)\n"
                f"    해법 ②  --loop-bars {loop_bars // 2}      (루프 {half:.1f}s로 줄인다 — 반복이 잦아진다)"
            )
        if loop_s < 60:
            problems.append(
                f"§2 루프 {loop_s:.1f}s — 60s는 30분 플레이에 30회 반복(❌ 판정). 마디를 늘려라"
            )
    return problems


def loop_verdict(bpm, loop_bars):
    """§2 목표 루프 길이 기준 — 판정어와 경고를 돌려준다(차단 아님)."""
    loop_s = bar_seconds(bpm) * loop_bars
    reps = 1800.0 / loop_s   # 30분 플레이 기준 반복 횟수
    if loop_s >= 120:
        mark = "⭕ 권장"
    elif loop_s >= 90:
        mark = "🟡 최소선"
    else:
        mark = f"🟡 최소선 미달 — {loop_bars * 2}마디({loop_s * 2:.0f}s) 검토 권장"
    return loop_s, reps, mark


def handoff_block(bpm, length_s, loop_bars, loop_risk):
    """§5 편집 인계 블록 — 곡을 받는 순간 어디를 자를지가 손에 있어야 한다."""
    bar = bar_seconds(bpm)
    alt = loop_bars // 2
    loop_s, reps, mark = loop_verdict(bpm, loop_bars)
    lines = [
        "── 편집 인계 ──",
        f"BPM        : {bpm}",
        f"1마디      : {bar:.2f}초   (240 ÷ {bpm})",
        f"권장 루프  : {loop_bars}마디 = {loop_s:.2f}초   (대안: {alt}마디 = {bar * alt:.2f}초)",
        f"반복 내성  : 30분 플레이 시 {reps:.0f}회 반복 — {mark}",
        f"요청 길이  : {length_s:.0f}초",
        f"편집 경고  : {EDIT_WARNING + ' [' + ', '.join(loop_risk) + ']' if loop_risk else '없음'}",
    ]
    return "\n".join(lines)
