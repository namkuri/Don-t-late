using UnityEditor;
using UnityEngine;

namespace DontLate.EditorTools
{
    /// <summary>
    /// 노점 배선 키트 (S-125 ② — 남규님 요청 "자판기·편의점·포장마차 별도 구매 UI").
    /// 배경 데코에 상호작용 전용 트리거 + <see cref="KioskShop"/>을 얹는다. 실물 콜라이더는
    /// 건드리지 않는다(PlaceCatalog가 이미 끈 상태 — S-119 ① 규약).
    /// </summary>
    internal static class KioskBuildKit
    {
        // 품목은 폰 쇼핑앱과 같은 id를 쓴다 — 효과 판정이 id 기준이라 중복 구현이 생기지 않는다.
        internal static readonly (string id, string label, int price)[] VendingItems =
        {
            ("drink", "에너지드링크", 1500),
            ("water", "생수 (더위↓)", 800),
            ("hot_drink", "코코아 (추위↓)", 1200),
        };

        internal static readonly (string id, string label, int price)[] StreetFoodItems =
        {
            ("hot_drink", "어묵 국물", 1000),
            ("water", "생수", 800),
            ("flower", "꽃 한 다발", 5000),
        };

        internal static readonly (string id, string label, int price)[] ConvenienceItems =
        {
            ("drink", "에너지드링크", 1500),
            ("water", "생수 (더위↓)", 800),
            ("hot_drink", "코코아 (추위↓)", 1200),
            ("cat_food", "고양이 사료", 2000),
        };

        internal static void MakeKiosk(GameObject host, string title,
            (string id, string label, int price)[] items)
        {
            if (host == null || items == null || items.Length == 0) return;

            Renderer[] renderers = host.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer r in renderers) bounds.Encapsulate(r.bounds);

            // 상호작용 전용 트리거 — 조금 넉넉히(±0.4u) 잡아 앞에 서면 잡히게.
            BoxCollider trigger = host.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = bounds.size + new Vector3(0.8f, 0f, 0.8f);
            trigger.center = host.transform.InverseTransformPoint(bounds.center);

            KioskShop shop = host.AddComponent<KioskShop>();
            var serialized = new SerializedObject(shop);
            serialized.FindProperty("_title").stringValue = title;
            SetStringArray(serialized, "_itemIds", System.Array.ConvertAll(items, i => i.id));
            SetStringArray(serialized, "_itemLabels", System.Array.ConvertAll(items, i => i.label));
            SerializedProperty prices = serialized.FindProperty("_itemPrices");
            prices.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++) prices.GetArrayElementAtIndex(i).intValue = items[i].price;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // 에셋 참조는 리플렉션 직접 주입 (SerializedObject 경유는 SaveScene 시 유실 — 2026-07-20 실측).
            GreyboxStageBuilder.SetReference(shop, "_renderer", renderers[0]);
            GreyboxStageBuilder.SetReference(shop, "_highlightMaterial",
                GreyboxStageBuilder.GetOrCreateHighlightMaterial());
        }

        private static void SetStringArray(SerializedObject serialized, string field, string[] values)
        {
            SerializedProperty property = serialized.FindProperty(field);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).stringValue = values[i];
        }
    }
}
