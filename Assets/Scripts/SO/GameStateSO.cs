using System.Collections.Generic;
using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 전역 상태 보관소. 계산·판정 로직은 넣지 않는다 — 매니저의 몫.
    /// 저장 없음(세션제)이므로 CoreBootstrap이 기동 시 초기화한다.
    /// </summary>
    [CreateAssetMenu(menuName = "DontLate/GameState")]
    public class GameStateSO : ScriptableObject
    {
        [Header("시계 — WorldDayNightManager가 쓰고 나머지는 읽는다")]
        public int day;
        // 하루 기준 분. 0~1440.
        public float minuteOfDay;

        [Header("경제")]
        public int money;
        public int debt;

        [Header("배송")]
        // 적재 목록의 단일 소유처. Player 쪽에 Inventory를 두지 않는다.
        public List<DeliveryOrderSO> cargo = new List<DeliveryOrderSO>();
        /// <summary>바코드 스캔된 주문 id (S-011 — 스캔한 짐만 픽업 가능. 스캔 순서 유지).</summary>
        public List<int> scannedOrderIds = new List<int>();
        /// <summary>
        /// S-264 — **트럭 짐칸에 실제로 실은** 주문 id. `cargo`는 손에 드는 순간 등록되므로
        /// (S-066 ① — 도보 배달도 정산이 인정되게 한 조치) "실었다"를 구분하지 못한다.
        /// 트럭으로 이동할 때는 이 목록에 있는 짐만 구역에 내린다 — 안 실은 건 캠프에 남는다.
        /// </summary>
        public List<int> truckCargoIds = new List<int>();
        /// <summary>S-268 — 첫인상 독백을 이미 한 씬 이름. 세션 단위(첫인상은 한 번뿐이다).</summary>
        public List<string> greetedScenes = new List<string>();
        /// <summary>S-268 — 트럭 앞 "빌릴까?" 독백을 이미 했는가.</summary>
        public bool truckRemarkDone;
        /// <summary>이동맵에서 고른 목적 구역 (S-015) — District 도착 시 이 구역의 짐·비콘만 스폰.</summary>
        public string currentDistrict;

        // ── S-019: 폰 앱·하우징·투자 ──
        /// <summary>완료 배송 기록 (택배앱 히스토리).</summary>
        public List<DeliveryRecord> deliveryHistory = new List<DeliveryRecord>();
        /// <summary>누적 수익 (택배앱 수익 탭 — money와 달리 지출로 줄지 않는다).</summary>
        public int totalEarned;
        /// <summary>보유 가구 id (구매 후 미배치 인벤토리).</summary>
        public List<string> ownedFurnitureIds = new List<string>();
        /// <summary>배치된 가구 (Home 씬 재생성용 — 세션제).</summary>
        public List<PlacedFurniture> placedFurniture = new List<PlacedFurniture>();
        public string apartmentGatePassword; // S-038 — 아파트 공동현관 비번 (세션 추첨, 폰 배송앱에 표시)
        public int wallpaperIndex;  // S-031 ④ — 벽지 팔레트 (HomeDecorator)
        public int floorIndex;      // S-031 ④ — 바닥 팔레트
        /// <summary>보유 코인 수량 (금융앱).</summary>
        public List<PlacedDelivery> placedDeliveries = new List<PlacedDelivery>(); // S-034 ④ 비콘 배치 기록
        public float coinUnits;
        public float coinCostBasis; // S-032 ⑤ — 현재 보유분의 총 매수금액 (차익 = 평가액 − 이것)
        /// <summary>런타임 생성 주문의 다음 일련번호 (S-021 ③ — 캠프 주문 갱신).</summary>
        public int nextOrderSerial = 200;

        [Header("통계")]
        public int completedCount;
        public int lateCount;

        [Header("진행")]
        public GameScene currentScene;
        public bool bossIntroPlayed; // S-052 ① — 캠프 사장님 튜토리얼 1회 재생 여부 (세션제)
        public bool tutorialDone;    // S-146 — 튜토리얼 완주 여부 (세션제)
        /// <summary>
        /// S-184 — 첫 인상 구간. 세션 시작 ~ **첫 배송 성공 후 집 복귀**까지 참.
        /// 이 동안 하늘은 낮·맑음으로 고정된다(시계는 정상 진행 — 마감 압박은 코어루프라 안 멈춘다).
        /// 처음 켠 사람이 안개·비·밤을 먼저 만나면 "잘 안 보이는 게임"으로 각인되기 때문(민지님 요청).
        /// </summary>
        public bool introGraceActive = true;

        // S-158 — 튜토리얼 중 귀가 엣지워크 잠금(캠프 왼쪽 끝 → 집).
        // 도중에 나가면 `bossIntroPlayed`가 이미 true라 **다시 와도 재개되지 않아** 조작을
        // 다 배우지 못한 채 게임이 시작된다(남규님 실관찰). 상자 픽업 단계를 넘기면 풀린다.
        // 게이트가 진행부를 직접 참조하면 도메인 경계를 넘으므로 상태를 여기 둔다.
        public bool tutorialExitLocked;
        /// <summary>해금된 배송 구역 (S-054 진행 시스템 — 비어 있으면 전체 해금으로 취급: 테스트·그레이박스 호환).</summary>
        public List<string> unlockedDistricts = new List<string>();
        /// <summary>회사 트럭 수령 여부 (S-054) — 전 구역 개척 보상. 지도앱 즉시 이동 해금.</summary>
        public bool hasTruck;

        /// <summary>체력 칸 (S-134 ④ — 정수님 QA "사고 패널티 완화"). 차에 치이면 2칸.
        /// 0이 되면 강제 귀가 + 정산(남규님 결정). 사망은 없다.</summary>
        public int health = HEALTH_MAX;
        public const int HEALTH_MAX = 5;

        // ── S-063 캐릭터 진행 ──
        public string nickname = "늦지마맨";
        public int playerLevel = 1;
        /// <summary>숙련도 — 배송 성공 +·주행 50m +1·실패 −. 만충 시 레벨업 (판정은 MasteryProgress).</summary>
        public float mastery;

        /// <summary>구루마(대차) 보유 (S-056 상점 구매). 도보 시대=캠프에서 밀기, 트럭 시대=배송지 자동 스폰.</summary>
        public bool ownsCart;

        /// <summary>정산 완료 신호 (S-068 ③) — 캠프 주문판이 이걸 보고 하루 주문을 리롤한 뒤 끈다.</summary>
        public bool daySettled;
        /// <summary>당일 확정 배송 물량 (S-072 ⑩) — 캠프 첫 진입 때 확정, 정산 후에만 리셋.
        /// 씬 오브젝트가 아니라 여기 살아서 캠프 재진입(씬 리로드)에도 물량이 불변이다.</summary>
        public List<DeliveryOrderSO> dayOrders = new List<DeliveryOrderSO>();
        /// <summary>파손된 주문 (S-074 ③) — 정산에서 실패로 청산하고 비운다. 상자 재스폰 금지 판정에도 쓴다.</summary>
        public List<int> destroyedOrderIds = new List<int>();
        /// <summary>씬 간 스태미나 영속 (S-081 ①) — 음수=미초기화(풀 충전으로 시작). 정산 시 리셋.</summary>
        public float stamina = -1f;

        /// <summary>만난 NPC와 호감도 (S-079 ④) — 등재=만남. 증감은 NpcAffinityLedger 경유.</summary>
        public List<NpcAffinity> npcAffinities = new List<NpcAffinity>();

        /// <summary>엔딩 1단 독백 재생됨 (S-104) — 빚 0 + Home 도착 1회. Camp 엔딩 시퀀스의 선행 조건.</summary>
        public bool endingMonologuePlayed;
        /// <summary>엔딩 시퀀스 완주됨 (S-104) — 재발동 방지.</summary>
        public bool endingPlayed;
        /// <summary>배송지에 버려둔 짐 위치 (S-068 ③) — 재입장 시 그 자리에 복원.</summary>
        public List<DroppedCargo> droppedCargo = new List<DroppedCargo>();

        /// <summary>씬 전환 중 손에 든 주문 보존 (S-066 ② — 엣지 워크 운반). 도착 씬에서 복원 후 비운다.</summary>
        public List<DeliveryOrderSO> carriedOrders = new List<DeliveryOrderSO>();

        // ── S-059 고양이 ──
        public bool catFriend;   // 언덕에서 데려옴
        public bool catRanAway;  // 하루 넘게 굶겨 떠남
        public int catFedDay;    // 마지막 급여 day

        // ── S-064 가방 (기본 4칸) ──
        public List<BagItem> bagItems = new List<BagItem>();

        [Header("세션 초기값")]
        public int startDay = 1;
        public float startMinuteOfDay = 8f * 60f;
        public int startMoney;
        public int startDebt = 10000;

        /// <summary>
        /// S-242 — 하루 정산에서 빚 상환에 **끌려가지 않는** 종잣돈. 세션 시작 지급액이자
        /// 이후에도 유지되는 하한선이다(정산은 이 금액을 넘는 잔액만 가져간다).
        /// 이게 없으면 첫 정산에 지갑이 0이 되어 다음 날 아무것도 살 수 없다.
        /// </summary>
        public const int PROTECTED_CASH = 3000;
    }

    [System.Serializable]
    public struct DeliveryRecord
    {
        public int orderId;
        public string address;
        public int reward;
        public int day;
        public int minuteOfDay;
    }

    /// <summary>NPC 호감도 1건 (S-079 ④) — npcId는 NpcSO.npcId와 일치.</summary>
    [System.Serializable]
    public struct NpcAffinity
    {
        public string npcId;
        public int affinity;
    }

    /// <summary>배송지에 버려둔 짐 (S-068 ③) — 구역·좌표 기록. 스포너가 재입장 시 이 자리에 깐다.</summary>
    [System.Serializable]
    public struct DroppedCargo
    {
        public int orderId;
        public string district;
        public Vector3 position;
    }

    /// <summary>가방 아이템 1칸 (S-064) — stackable이면 count 겹침, holdable이면 좌클릭으로 손에 든다.</summary>
    [System.Serializable]
    public struct BagItem
    {
        public string id;
        public string label;
        public int count;
        public bool stackable;
        public bool holdable;
    }

    /// <summary>비콘에 내려놓은 배송 기록 (S-034 ④) — 정산 때 주소 일치를 일괄 판정한다.</summary>
    [System.Serializable]
    public struct PlacedDelivery
    {
        public int orderId;
        public string beaconAddress;
    }

    [System.Serializable]
    public struct PlacedFurniture
    {
        public string furnitureId;
        public Vector3 position;
        public float rotationY; // S-030 ③ — R 회전 각도 보존
    }
}
