#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class AugmentRuleCatalogGenerator
{
    private const string CatalogPath =
        "Assets/_Project/ScriptableObjects/Reward/AugmentRuleCatalog.asset";
    private const string RewardCatalogPath =
        "Assets/_Project/ScriptableObjects/Reward/Catalog/MainRewardCatalog.asset";

    private sealed class Spec
    {
        public string Id, Name, Description;
        public AugmentValueTier Tier;
        public AugmentBuildTag Tag;
        public RuleAugmentEffectKind Kind;
        public ElementType Element;
        public int[] Ints;
        public float[] Floats;
    }

    [InitializeOnLoadMethod]
    private static void GenerateAfterScriptReload()
    {
        EditorApplication.delayCall += () =>
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                Generate();
        };
    }

    [MenuItem("Ballchemy/Generate Full Augment Rule Catalog")]
    public static void Generate()
    {
        AugmentRuleCatalog catalog = AssetDatabase.LoadAssetAtPath<AugmentRuleCatalog>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<AugmentRuleCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }

        SerializedObject serialized = new SerializedObject(catalog);
        SerializedProperty entries = serialized.FindProperty("entries");
        List<Spec> specs = CreateSpecs();
        entries.arraySize = specs.Count;

        for (int i = 0; i < specs.Count; i++)
            WriteEntry(entries.GetArrayElementAtIndex(i), specs[i]);

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(catalog);

        RewardCatalog rewardCatalog = AssetDatabase.LoadAssetAtPath<RewardCatalog>(RewardCatalogPath);
        if (rewardCatalog != null)
        {
            SerializedObject rewardSerialized = new SerializedObject(rewardCatalog);
            rewardSerialized.FindProperty("augmentRuleCatalog").objectReferenceValue = catalog;
            rewardSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(rewardCatalog);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"AugmentRuleCatalogGenerator: {specs.Count}개 규칙 증강 생성 완료");
    }

    private static void WriteEntry(SerializedProperty property, Spec spec)
    {
        property.FindPropertyRelative("augmentId").stringValue = spec.Id;
        property.FindPropertyRelative("displayName").stringValue = spec.Name;
        property.FindPropertyRelative("description").stringValue = spec.Description;
        property.FindPropertyRelative("valueTier").enumValueIndex = (int)spec.Tier - 1;
        property.FindPropertyRelative("maxLevel").intValue = spec.Tier == AugmentValueTier.Value1 ? 3 : spec.Tier == AugmentValueTier.Value2 ? 2 : 1;
        property.FindPropertyRelative("buildTag").enumValueIndex = (int)spec.Tag;
        property.FindPropertyRelative("effectKind").enumValueIndex = (int)spec.Kind;
        property.FindPropertyRelative("elementType").enumValueIndex = (int)spec.Element;
        property.FindPropertyRelative("selectionWeight").intValue = 1;
        WriteInts(property.FindPropertyRelative("integerValues"), spec.Ints);
        WriteFloats(property.FindPropertyRelative("floatValues"), spec.Floats);
    }

    private static void WriteInts(SerializedProperty property, int[] values)
    {
        values = values ?? new int[0];
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).intValue = values[i];
    }

    private static void WriteFloats(SerializedProperty property, float[] values)
    {
        values = values ?? new float[0];
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).floatValue = values[i];
    }

    private static Spec S(string id, string name, string desc, AugmentValueTier tier,
        AugmentBuildTag tag, RuleAugmentEffectKind kind, ElementType element = ElementType.Water,
        int[] ints = null, float[] floats = null)
    {
        return new Spec { Id = id, Name = name, Description = desc, Tier = tier, Tag = tag,
            Kind = kind, Element = element, Ints = ints, Floats = floats };
    }

    private static List<Spec> CreateSpecs()
    {
        return new List<Spec>
        {
            S("quantity_group_acceleration", "집단 가속", "이번 턴에 발사한 공 10개마다 이후 공의 직접 피해가 증가합니다.", AugmentValueTier.Value1, AugmentBuildTag.Quantity, RuleAugmentEffectKind.DamagePerLaunchedCount, ints:new[]{1,2,3}),
            S("quantity_remaining_firepower", "잔여 화력", "발사 대기 공 10개마다 현재 공의 직접 피해가 증가합니다.", AugmentValueTier.Value1, AugmentBuildTag.Quantity, RuleAugmentEffectKind.DamageByRemainingBalls, ints:new[]{1,2,3}),
            S("quantity_element_relay", "원소 릴레이", "서로 다른 원소 공이 연속 발사되면 현재 공의 원소 스택이 증가합니다.", AugmentValueTier.Value2, AugmentBuildTag.Quantity, RuleAugmentEffectKind.AlternatingElementBonus, ints:new[]{1,2}),
            S("quantity_chain_deployment", "연쇄 투입", "같은 특성 공이 연속 발사되면 세 번째 공의 직접 피해가 증가합니다.", AugmentValueTier.Value2, AugmentBuildTag.Quantity, RuleAugmentEffectKind.ConsecutiveTraitBonus, ints:new[]{2,4}),
            S("quantity_replication_alchemy", "복제 연성", "매 턴 첫 번째 공을 한 갈래 추가 발사합니다.", AugmentValueTier.Value3, AugmentBuildTag.Quantity, RuleAugmentEffectKind.TemporaryDuplicateLaunch, ints:new[]{1}),
            S("quantity_total_mobilization", "총동원", "턴의 후반 절반에 발사되는 공의 피해와 속성이 강화됩니다.", AugmentValueTier.Value3, AugmentBuildTag.Quantity, RuleAugmentEffectKind.LateTurnOverdrive, ints:new[]{2}),

            S("refinement_pure_catalyst", "고순도 촉매", "3성 공의 직접 피해가 증가합니다.", AugmentValueTier.Value1, AugmentBuildTag.Refinement, RuleAugmentEffectKind.HighGradeDamage, ints:new[]{2,4,6}),
            S("refinement_precision", "정밀 가공", "2성 이상 공이 부여하는 원소 및 독 스택이 증가합니다.", AugmentValueTier.Value1, AugmentBuildTag.Refinement, RuleAugmentEffectKind.HighGradeElementStack, ints:new[]{1,2,3}),
            S("refinement_grade_resonance", "등급 공명", "같은 등급 공이 연속 발사되면 현재 공의 피해가 증가합니다.", AugmentValueTier.Value2, AugmentBuildTag.Refinement, RuleAugmentEffectKind.ConsecutiveGradeBonus, ints:new[]{2,4}),
            S("refinement_oversaturation", "과포화", "3성 공이 최대 원소 스택의 블록을 타격하면 추가 피해를 줍니다.", AugmentValueTier.Value2, AugmentBuildTag.Refinement, RuleAugmentEffectKind.ThreeStarOverflow, ints:new[]{3,6}),
            S("refinement_complete_crystallization", "완전 결정화", "매 턴 처음 발사되는 저등급 공을 해당 비행 동안 3성으로 만듭니다.", AugmentValueTier.Value3, AugmentBuildTag.Refinement, RuleAugmentEffectKind.FirstLowGradeUpgrade, ints:new[]{1}),
            S("refinement_elite_few", "소수 정예", "보유 공이 15개 이하라면 모든 공의 직접 피해가 크게 증가합니다.", AugmentValueTier.Value3, AugmentBuildTag.Refinement, RuleAugmentEffectKind.FewBallElite, ints:new[]{4}),

            S("fire_embers", "잔불", "Fire 상태가 있는 블록에 주는 직접 피해가 증가합니다.", AugmentValueTier.Value1, AugmentBuildTag.Fire, RuleAugmentEffectKind.BurningTargetDamage, ElementType.Fire, new[]{1,2,3}),
            S("fire_high_heat", "고열", "Fire 공이 부여하는 스택이 증가합니다.", AugmentValueTier.Value1, AugmentBuildTag.Fire, RuleAugmentEffectKind.FireStackBonus, ElementType.Fire, new[]{1,2,3}),
            S("fire_heat_spread", "열 확산", "Fire 상태 블록 재타격 시 주변 블록에 Fire 스택을 전파합니다.", AugmentValueTier.Value2, AugmentBuildTag.Fire, RuleAugmentEffectKind.FireSpread, ElementType.Fire, new[]{1,2}),
            S("fire_furnace", "용광로", "같은 블록을 Fire 공으로 반복 타격할수록 추가 피해가 증가합니다.", AugmentValueTier.Value2, AugmentBuildTag.Fire, RuleAugmentEffectKind.RepeatedFireHit, ElementType.Fire, new[]{1,2}),
            S("fire_overheat", "과열", "최대 Fire 스택 블록을 다시 타격하면 스택을 소비해 폭발합니다.", AugmentValueTier.Value3, AugmentBuildTag.Fire, RuleAugmentEffectKind.FireOverheatExplosion, ElementType.Fire, new[]{2}),
            S("fire_rampage", "화염 폭주", "이번 턴의 Fire 적중 수에 따라 이후 Fire 공의 피해가 증가합니다.", AugmentValueTier.Value3, AugmentBuildTag.Fire, RuleAugmentEffectKind.FireTurnRamp, ElementType.Fire, new[]{1}),

            S("water_saturation", "수분 포화", "젖음 최대 스택이 증가합니다.", AugmentValueTier.Value1, AugmentBuildTag.Water, RuleAugmentEffectKind.WetMaximumBonus, ElementType.Water, new[]{2,4,6}),
            S("water_infiltration", "침윤", "Water 공이 부여하는 젖음 스택이 증가합니다.", AugmentValueTier.Value1, AugmentBuildTag.Water, RuleAugmentEffectKind.WaterStackBonus, ElementType.Water, new[]{1,2,3}),
            S("water_flooding", "범람", "최대 젖음 블록 재타격 시 주변에 젖음을 전파합니다.", AugmentValueTier.Value2, AugmentBuildTag.Water, RuleAugmentEffectKind.WetSpread, ElementType.Water, new[]{1,2}),
            S("water_channel", "수로 형성", "인접한 젖은 블록 수에 따라 Water 공의 피해가 증가합니다.", AugmentValueTier.Value2, AugmentBuildTag.Water, RuleAugmentEffectKind.WetNeighborBonus, ElementType.Water, new[]{1,2}),
            S("water_deluge", "홍수", "한 턴에 Water로 5회 적중하면 모든 적 블록에 젖음 1스택을 부여합니다.", AugmentValueTier.Value3, AugmentBuildTag.Water, RuleAugmentEffectKind.GlobalWetOnThreshold, ElementType.Water, new[]{5}),
            S("water_cycle", "순환수", "감전으로 소비한 젖음 일부를 주변 블록에 되돌립니다.", AugmentValueTier.Value3, AugmentBuildTag.Water, RuleAugmentEffectKind.RefundConsumedWet, ElementType.Water, new[]{1}),

            S("lightning_charge", "축전", "전하 최대 스택이 증가합니다.", AugmentValueTier.Value1, AugmentBuildTag.Lightning, RuleAugmentEffectKind.ChargeMaximumBonus, ElementType.Electric, new[]{2,4,6}),
            S("lightning_voltage", "전압 상승", "한 번의 전도에서 다음 대상일수록 전도 피해가 증가합니다.", AugmentValueTier.Value2, AugmentBuildTag.Lightning, RuleAugmentEffectKind.ConductionDamageRamp, ElementType.Electric, new[]{1,2}),
            S("lightning_closed_circuit", "폐쇄 회로", "전도 대상이 3개 이상이면 중심 블록을 추가 타격합니다.", AugmentValueTier.Value2, AugmentBuildTag.Lightning, RuleAugmentEffectKind.ClosedCircuitStrike, ElementType.Electric, new[]{2,4}),
            S("lightning_chain_strike", "연쇄 낙뢰", "전도망 마지막 대상 주변에 추가 낙뢰 피해를 줍니다.", AugmentValueTier.Value3, AugmentBuildTag.Lightning, RuleAugmentEffectKind.ChainLightning, ElementType.Electric, new[]{3}),
            S("lightning_storm", "폭풍", "한 턴에 감전 5회 달성 후 Lightning 공의 전이 대상과 피해가 증가합니다.", AugmentValueTier.Value3, AugmentBuildTag.Lightning, RuleAugmentEffectKind.LightningStorm, ElementType.Electric, new[]{5}),

            S("ice_severe_cold", "혹한", "Ice 공이 부여하는 Frost 스택이 증가합니다.", AugmentValueTier.Value1, AugmentBuildTag.Ice, RuleAugmentEffectKind.FrostStackBonus, ElementType.Ice, new[]{1,2,3}),
            S("ice_brittleness", "취성", "동결 파쇄 피해가 증가합니다.", AugmentValueTier.Value1, AugmentBuildTag.Ice, RuleAugmentEffectKind.FrozenShatterDamage, ElementType.Ice, new[]{2,4,6}),
            S("ice_frost_infection", "서리 전염", "동결 블록 파쇄 시 주변에 Frost 스택을 전파합니다.", AugmentValueTier.Value2, AugmentBuildTag.Ice, RuleAugmentEffectKind.FrostSpreadOnShatter, ElementType.Ice, new[]{1,2}),
            S("ice_glacial_crack", "빙하 균열", "Basic 공으로 동결 블록을 파쇄하면 주변에도 피해를 줍니다.", AugmentValueTier.Value2, AugmentBuildTag.Ice, RuleAugmentEffectKind.BasicShatterSplash, ElementType.Ice, new[]{2,4}),
            S("ice_absolute_zero", "절대영도", "동결 발생 시 주변의 Frost가 높은 블록도 함께 동결시킵니다.", AugmentValueTier.Value3, AugmentBuildTag.Ice, RuleAugmentEffectKind.MassFreeze, ElementType.Ice, new[]{1}),
            S("ice_age", "빙하 시대", "이번 턴 첫 동결 후 남은 턴 동안 모든 Ice 효과가 강화됩니다.", AugmentValueTier.Value3, AugmentBuildTag.Ice, RuleAugmentEffectKind.IceAge, ElementType.Ice, new[]{1}),

            S("poison_residue", "독성 잔류", "턴 종료 시 독 스택 일부가 다음 턴까지 유지됩니다.", AugmentValueTier.Value1, AugmentBuildTag.Poison, RuleAugmentEffectKind.PoisonTurnRetention, ints:new[]{1,2,3}),
            S("poison_neurotoxin", "신경독", "독 스택이 5 이상인 블록에 주는 직접 피해가 증가합니다.", AugmentValueTier.Value1, AugmentBuildTag.Poison, RuleAugmentEffectKind.PoisonedTargetDamage, ints:new[]{1,2,3}),
            S("poison_cross_infection", "교차 감염", "원소 반응 발생 시 주변 블록에 독 스택을 전파합니다.", AugmentValueTier.Value2, AugmentBuildTag.Poison, RuleAugmentEffectKind.PoisonReactionSpread, ints:new[]{1,2}),
            S("poison_corrosion", "부식", "반사한 공이 독 스택이 높은 블록에 주는 피해가 증가합니다.", AugmentValueTier.Value2, AugmentBuildTag.Poison, RuleAugmentEffectKind.PoisonBounceSynergy, ints:new[]{2,4}),
            S("poison_plague", "역병", "독성 붕괴로 적을 파괴하면 주변 블록에 독을 남깁니다.", AugmentValueTier.Value3, AugmentBuildTag.Poison, RuleAugmentEffectKind.PlagueCollapse, ints:new[]{3}),
            S("poison_recirculation", "독성 순환", "독성 붕괴 후 다음 Poison 공의 스택과 피해가 강화됩니다.", AugmentValueTier.Value3, AugmentBuildTag.Poison, RuleAugmentEffectKind.PoisonCycle, ints:new[]{2}),

            S("trajectory_elastic_coating", "탄성 코팅", "첫 반사 이후 공의 속도 손실을 줄입니다.", AugmentValueTier.Value1, AugmentBuildTag.Trajectory, RuleAugmentEffectKind.FirstBounceSpeedRetention, ints:new[]{20,35,50}),
            S("trajectory_charged_bounce", "축전 반사", "반사 횟수에 따라 Lightning 전도 피해가 증가합니다.", AugmentValueTier.Value2, AugmentBuildTag.Trajectory, RuleAugmentEffectKind.BounceLightningSynergy, ElementType.Electric, new[]{1,2}),
            S("trajectory_friction_heat", "마찰열", "반사 횟수에 따라 Fire 스택 부여량이 증가합니다.", AugmentValueTier.Value2, AugmentBuildTag.Trajectory, RuleAugmentEffectKind.BounceFireSynergy, ElementType.Fire, new[]{1,2}),
            S("trajectory_ice_slide", "빙면 활주", "반사한 Ice 공의 다음 충돌 Frost 부여량이 증가합니다.", AugmentValueTier.Value2, AugmentBuildTag.Trajectory, RuleAugmentEffectKind.BounceIceSynergy, ElementType.Ice, new[]{1,2}),
            S("trajectory_ballistic_split", "탄도 분열", "5회 반사한 공이 한 갈래 추가 궤도를 생성합니다.", AugmentValueTier.Value3, AugmentBuildTag.Trajectory, RuleAugmentEffectKind.BallisticSplit, ints:new[]{5}),
            S("trajectory_return", "귀환 궤도", "8회 반사 후 공의 진행 방향을 발사 중심 방향으로 한 번 교정합니다.", AugmentValueTier.Value3, AugmentBuildTag.Trajectory, RuleAugmentEffectKind.ReturnTrajectory, ints:new[]{8}),

            S("hybrid_impurity", "불순물 허용", "보유한 서로 다른 원소 종류마다 모든 공의 직접 피해가 증가합니다.", AugmentValueTier.Value1, AugmentBuildTag.Hybrid, RuleAugmentEffectKind.ElementDiversityDamage, ints:new[]{1,1,2}),
            S("hybrid_inheritance", "원소 계승", "이전 공과 다른 원소 공을 발사하면 현재 공의 스택이 증가합니다.", AugmentValueTier.Value2, AugmentBuildTag.Hybrid, RuleAugmentEffectKind.PreviousElementInheritance, ints:new[]{1,2}),
            S("hybrid_reaction_echo", "반응 잔향", "직전과 다른 종류의 원소 반응을 일으키면 반응 피해가 증가합니다.", AugmentValueTier.Value2, AugmentBuildTag.Hybrid, RuleAugmentEffectKind.ReactionEcho, ints:new[]{2,4}),
            S("hybrid_element_cycle", "원소 순환", "턴마다 하나의 원소가 순환하며 해당 원소의 스택과 피해가 강화됩니다.", AugmentValueTier.Value3, AugmentBuildTag.Hybrid, RuleAugmentEffectKind.ElementCycle, ints:new[]{2}),
            S("hybrid_critical_reaction", "임계 반응", "한 턴에 감전과 열충격을 모두 일으키면 남은 턴의 원소 효과가 강화됩니다.", AugmentValueTier.Value3, AugmentBuildTag.Hybrid, RuleAugmentEffectKind.CriticalReaction, ints:new[]{2}),
            S("hybrid_mutation", "불안정한 변환", "발사할 때 공의 원소를 임시로 다른 원소로 순환 변환합니다.", AugmentValueTier.Value3, AugmentBuildTag.Hybrid, RuleAugmentEffectKind.TemporaryElementMutation, ints:new[]{1})
        };
    }
}
#endif
