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
            S("quantity_group_acceleration", "집단 가속", "발사한 공 10개마다 이후 공 피해가 증가합니다. (Lv.1 5% / Lv.2 10% / Lv.3 15%, 최대 45%)", AugmentValueTier.Value1, AugmentBuildTag.Quantity, RuleAugmentEffectKind.DamagePerLaunchedCount, ints:new[]{5,10,15}),
            S("quantity_remaining_firepower", "잔여 화력", "대기 공 10개마다 현재 공 피해가 증가합니다. (Lv.1 5% / Lv.2 10% / Lv.3 15%, 최대 45%)", AugmentValueTier.Value1, AugmentBuildTag.Quantity, RuleAugmentEffectKind.DamageByRemainingBalls, ints:new[]{5,10,15}),
            S("quantity_chain_deployment", "연쇄 투입", "같은 특성 공 3연속 발사 시 세 번째 공 피해가 증가합니다. (Lv.1 25% / Lv.2 50%)", AugmentValueTier.Value2, AugmentBuildTag.Quantity, RuleAugmentEffectKind.ConsecutiveTraitBonus, ints:new[]{25,50}),
            S("quantity_variety", "다품종 생산", "이전 공과 다른 특성 공을 발사하면 현재 공 피해가 증가합니다. (Lv.1 20% / Lv.2 40%)", AugmentValueTier.Value2, AugmentBuildTag.Quantity, RuleAugmentEffectKind.AlternatingElementBonus, ints:new[]{20,40}),
            S("quantity_chain_clone", "연쇄 복제", "매 5번째 공을 70% 성능의 독립 공으로 복제합니다. 복제 공은 다시 복제되지 않습니다.", AugmentValueTier.Value3, AugmentBuildTag.Quantity, RuleAugmentEffectKind.ChainClone, ints:new[]{70}),
            S("quantity_total_mobilization", "총동원", "턴 후반 절반에 발사되는 공의 직접 피해와 특성 추가 피해가 50% 증가합니다.", AugmentValueTier.Value3, AugmentBuildTag.Quantity, RuleAugmentEffectKind.LateTurnOverdrive, ints:new[]{50}),

            S("refinement_pure_catalyst", "고순도 촉매", "3성 공의 직접 피해가 증가합니다. (Lv.1 15% / Lv.2 30% / Lv.3 45%)", AugmentValueTier.Value1, AugmentBuildTag.Refinement, RuleAugmentEffectKind.HighGradeDamage, ints:new[]{15,30,45}),
            S("refinement_precision", "정밀 가공", "2성 이상 공의 직접 피해와 특성 추가 피해가 증가합니다. (Lv.1 10% / Lv.2 20% / Lv.3 30%)", AugmentValueTier.Value1, AugmentBuildTag.Refinement, RuleAugmentEffectKind.RefinedTraitDamage, ints:new[]{10,20,30}),
            S("refinement_grade_resonance", "등급 공명", "같은 등급 공을 연속 발사하면 현재 공 피해가 증가합니다. (Lv.1 25% / Lv.2 50%)", AugmentValueTier.Value2, AugmentBuildTag.Refinement, RuleAugmentEffectKind.ConsecutiveGradeBonus, ints:new[]{25,50}),
            S("refinement_oversaturation", "과포화", "3성 공이 최대 스택 또는 동결 블록을 타격하면 피해가 증가합니다. (Lv.1 30% / Lv.2 60%)", AugmentValueTier.Value2, AugmentBuildTag.Refinement, RuleAugmentEffectKind.ThreeStarOverflow, ints:new[]{30,60}),
            S("refinement_complete_crystallization", "완전 결정화", "모든 1·2성 공을 전투 중 한 단계 높은 등급으로 취급합니다.", AugmentValueTier.Value3, AugmentBuildTag.Refinement, RuleAugmentEffectKind.FirstLowGradeUpgrade, ints:new[]{1}),
            S("refinement_grade_transcendence", "등급 초월", "3성 공의 직접 피해와 특성 추가 피해가 50% 증가합니다.", AugmentValueTier.Value3, AugmentBuildTag.Refinement, RuleAugmentEffectKind.GradeTranscendence, ints:new[]{50}),

            S("trajectory_kinetic", "운동 에너지", "반사 횟수에 따라 현재 공 피해가 증가합니다. (Lv.1 5회/최대 30%, Lv.2 3회/최대 50%, Lv.3 2회/최대 70%)", AugmentValueTier.Value1, AugmentBuildTag.Trajectory, RuleAugmentEffectKind.KineticBounce, ints:new[]{5,3,2}),
            S("trajectory_tempering", "궤도 연성", "한 번 이상 반사한 공의 직접 피해와 특성 추가 피해가 증가합니다. (10% / 20% / 30%)", AugmentValueTier.Value1, AugmentBuildTag.Trajectory, RuleAugmentEffectKind.BouncedTraitDamage, ints:new[]{10,20,30}),
            S("trajectory_rebound_hit", "되튐 타격", "같은 블록을 다시 타격하면 피해가 증가합니다. (Lv.1 25% / Lv.2 50%)", AugmentValueTier.Value2, AugmentBuildTag.Trajectory, RuleAugmentEffectKind.SameBlockRehit, ints:new[]{25,50}),
            S("trajectory_chain_collision", "연속 충돌", "서로 다른 블록 연속 타격마다 피해가 누적됩니다. (Lv.1 10%, 최대 50% / Lv.2 20%, 최대 100%)", AugmentValueTier.Value2, AugmentBuildTag.Trajectory, RuleAugmentEffectKind.DifferentBlockChain, ints:new[]{10,20}),
            S("trajectory_ballistic_split", "탄도 분열", "5회 반사 후 다음 충돌에서 50% 성능의 독립 공 2개를 생성합니다. 분열 공은 재분열하지 않습니다.", AugmentValueTier.Value3, AugmentBuildTag.Trajectory, RuleAugmentEffectKind.BallisticSplit, ints:new[]{5}),
            S("trajectory_shockwave", "충격파 궤도", "6회 반사 후 첫 블록 타격에 현재 공 직접 피해의 150%만큼 3×3 충격파 피해를 줍니다.", AugmentValueTier.Value3, AugmentBuildTag.Trajectory, RuleAugmentEffectKind.ShockTrajectory, ints:new[]{150}),

            S("basic_sturdy_shot", "견고한 탄환", "기본 공의 직접 피해가 증가합니다. (Lv.1 10% / Lv.2 20% / Lv.3 30%)", AugmentValueTier.Value1, AugmentBuildTag.Basic, RuleAugmentEffectKind.BasicDamage, ints:new[]{10,20,30}),
            S("basic_catalyst_coating", "촉매 코팅", "기본 공이 스택 블록을 타격하면 가장 높은 스택을 1 증가시킵니다. Lv.2부터 추가 피해도 증가합니다.", AugmentValueTier.Value1, AugmentBuildTag.Basic, RuleAugmentEffectKind.BasicCatalyst, ints:new[]{0,10,20}),
            S("basic_colorless_catalyst", "무색 촉매", "스택이 없는 블록에 기본 공으로 주는 피해가 증가합니다. (Lv.1 25% / Lv.2 50%)", AugmentValueTier.Value2, AugmentBuildTag.Basic, RuleAugmentEffectKind.EmptyTargetDamage, ints:new[]{25,50}),
            S("basic_shatter_hit", "파쇄 타격", "기본 공으로 블록을 파괴하면 주변 3×3에 기본 공 직접 피해를 기준으로 피해를 줍니다. (Lv.1 200% / Lv.2 300%)", AugmentValueTier.Value2, AugmentBuildTag.Basic, RuleAugmentEffectKind.BasicDestroySplash, ints:new[]{200,300}),
            S("basic_alchemy_dismantle", "연금술 해체", "최대 스택 또는 동결 상태 하나를 소비해 중심에 기본 공 피해의 500%, 주변 3×3에 300% 피해를 줍니다. 블록당 턴 1회입니다.", AugmentValueTier.Value3, AugmentBuildTag.Basic, RuleAugmentEffectKind.AlchemyDismantle, ints:new[]{500}),
            S("basic_pure_crystal", "순수 결정", "기본 공 연속 발사마다 이후 기본 공 피해가 10% 증가합니다. 최대 50%이며 다른 특성 공 발사 시 초기화됩니다.", AugmentValueTier.Value3, AugmentBuildTag.Basic, RuleAugmentEffectKind.ConsecutiveBasic, ints:new[]{10}),

            S("poison_concentrate", "맹독 농축", "독 부여량이 증가합니다. (Lv.1 +1 / Lv.2 +1 및 붕괴 피해 +5 / Lv.3 +2 및 붕괴 피해 +5)", AugmentValueTier.Value1, AugmentBuildTag.Poison, RuleAugmentEffectKind.HighGradeElementStack, ints:new[]{1,1,2}),
            S("poison_neurotoxin", "신경독", "독 3스택 이상 블록에 추가 고정 피해를 줍니다. (Lv.1 +5 / Lv.2 +10 / Lv.3 +15)", AugmentValueTier.Value1, AugmentBuildTag.Poison, RuleAugmentEffectKind.PoisonedTargetDamage, ints:new[]{5,10,15}),
            S("poison_contagion", "독성 전염", "독성 붕괴 시 주변 블록에 독을 전파합니다. (Lv.1 최대 2개에 독 1 / Lv.2 최대 4개에 독 2)", AugmentValueTier.Value2, AugmentBuildTag.Poison, RuleAugmentEffectKind.PoisonReactionSpread, ints:new[]{1,2}),
            S("poison_corrosion", "부식", "반사한 독 공이 중독 블록에 주는 피해가 증가합니다. (Lv.1 20% / Lv.2 40%)", AugmentValueTier.Value2, AugmentBuildTag.Poison, RuleAugmentEffectKind.PoisonBounceSynergy, ints:new[]{20,40}),
            S("poison_plague", "역병", "독성 붕괴 범위가 5×5로 증가하고 붕괴 피해가 35가 되며 피격 블록에 독 1을 남깁니다.", AugmentValueTier.Value3, AugmentBuildTag.Poison, RuleAugmentEffectKind.PlagueCollapse, ints:new[]{35}),
            S("poison_cycle", "독성 순환", "독성 붕괴마다 해당 턴 독 공의 독 부여량이 1 증가합니다. 최대 2회 누적됩니다.", AugmentValueTier.Value3, AugmentBuildTag.Poison, RuleAugmentEffectKind.PoisonCycle, ints:new[]{1}),

            S("water_infiltration", "침윤", "젖음 최대 스택이 감소합니다. (Lv.1 -1 / Lv.2 -2 / Lv.3 -3, 최소 2)", AugmentValueTier.Value1, AugmentBuildTag.Water, RuleAugmentEffectKind.WetMaximumReduction, ElementType.Water, new[]{1,2,3}),
            S("water_cohesion", "응집수", "젖은 블록에 물 공으로 주는 피해가 증가합니다. (10% / 20% / 30%)", AugmentValueTier.Value1, AugmentBuildTag.Water, RuleAugmentEffectKind.WaterCohesion, ElementType.Water, new[]{10,20,30}),
            S("water_pressure", "수압", "최대 젖음 블록 재타격 시 중심에 원본 피해의 일부를 추가합니다. (Lv.1 50% / Lv.2 100%)", AugmentValueTier.Value2, AugmentBuildTag.Water, RuleAugmentEffectKind.WaterPressure, ElementType.Water, new[]{50,100}),
            S("water_channel", "수로 형성", "인접한 젖은 블록마다 물 공 피해가 증가합니다. (Lv.1 10%, 최대 40% / Lv.2 20%, 최대 80%)", AugmentValueTier.Value2, AugmentBuildTag.Water, RuleAugmentEffectKind.WetNeighborBonus, ElementType.Water, new[]{10,20}),
            S("water_flood", "홍수", "한 턴에 물 공으로 5회 적중하면 모든 적 블록에 젖음 1을 부여합니다. 턴당 1회입니다.", AugmentValueTier.Value3, AugmentBuildTag.Water, RuleAugmentEffectKind.GlobalWetOnThreshold, ElementType.Water, new[]{5}),
            S("water_tsunami", "해일", "최대 젖음 블록의 젖음 전파가 바깥쪽으로 한 단계 추가 진행합니다.", AugmentValueTier.Value3, AugmentBuildTag.Water, RuleAugmentEffectKind.TsunamiSpread, ElementType.Water, new[]{1}),

            S("lightning_amplify", "전류 증폭", "번개 공의 기본 추가 피해와 전도 피해가 증가합니다. (10% / 20% / 30%)", AugmentValueTier.Value1, AugmentBuildTag.Lightning, RuleAugmentEffectKind.ElectricDamageBonus, ElementType.Electric, new[]{10,20,30}),
            S("lightning_residual", "잔류 전하", "감전 표식을 다음 번개 직접 타격으로 소비해 원본 번개 피해의 일부로 추가 타격합니다. (50% / 75% / 100%)", AugmentValueTier.Value1, AugmentBuildTag.Lightning, RuleAugmentEffectKind.ResidualCharge, ElementType.Electric, new[]{50,75,100}),
            S("lightning_voltage", "전압 상승", "전도의 다음 대상마다 전도 피해가 증가합니다. (Lv.1 10% / Lv.2 20%)", AugmentValueTier.Value2, AugmentBuildTag.Lightning, RuleAugmentEffectKind.ConductionDamageRamp, ElementType.Electric, new[]{10,20}),
            S("lightning_closed_circuit", "폐쇄 회로", "전도 대상 3개 이상이면 중심을 전도 피해의 일부로 추가 타격합니다. (Lv.1 50% / Lv.2 100%)", AugmentValueTier.Value2, AugmentBuildTag.Lightning, RuleAugmentEffectKind.ClosedCircuitStrike, ElementType.Electric, new[]{50,100}),
            S("lightning_chain_strike", "연쇄 낙뢰", "전도망 마지막 대상 주변 3×3에 전도 피해의 150%로 추가 낙뢰를 일으킵니다.", AugmentValueTier.Value3, AugmentBuildTag.Lightning, RuleAugmentEffectKind.ChainLightning, ElementType.Electric, new[]{150}),
            S("lightning_storm", "폭풍", "한 턴 감전 5회 후 남은 턴 최대 전도 대상이 2 증가하고 전도 피해가 50% 증가합니다.", AugmentValueTier.Value3, AugmentBuildTag.Lightning, RuleAugmentEffectKind.LightningStorm, ElementType.Electric, new[]{5}),

            S("ice_severe_cold", "혹한", "냉기 부여량과 동결 대상 피해를 강화합니다. (Lv.1 냉기 +1 / Lv.2 냉기 +1·피해 +20% / Lv.3 냉기 +2·피해 +20%)", AugmentValueTier.Value1, AugmentBuildTag.Ice, RuleAugmentEffectKind.FrostStackBonus, ElementType.Ice, new[]{1,1,2}),
            S("ice_brittleness", "취성", "동결 블록에 주는 피해가 증가합니다. (15% / 30% / 45%)", AugmentValueTier.Value1, AugmentBuildTag.Ice, RuleAugmentEffectKind.FrozenShatterDamage, ElementType.Ice, new[]{15,30,45}),
            S("ice_frost_infection", "서리 전염", "블록 동결 시 주변에 냉기를 부여합니다. (Lv.1 냉기 1 / Lv.2 냉기 2)", AugmentValueTier.Value2, AugmentBuildTag.Ice, RuleAugmentEffectKind.FrostSpreadOnShatter, ElementType.Ice, new[]{1,2}),
            S("ice_wall", "빙벽", "동결된 공격형 블록의 다음 공격을 지연합니다. (Lv.1 1회 / Lv.2 2회)", AugmentValueTier.Value2, AugmentBuildTag.Ice, RuleAugmentEffectKind.IceAttackDelay, ElementType.Ice, new[]{1,2}),
            S("ice_absolute_zero", "절대영도", "동결 발생 시 주변에서 냉기가 최대치보다 1 부족한 블록도 함께 동결시킵니다.", AugmentValueTier.Value3, AugmentBuildTag.Ice, RuleAugmentEffectKind.MassFreeze, ElementType.Ice, new[]{1}),
            S("ice_age", "빙하 시대", "이번 턴 첫 동결 후 남은 턴 얼음 공 피해가 50% 증가하고 냉기 부여량이 1 증가합니다.", AugmentValueTier.Value3, AugmentBuildTag.Ice, RuleAugmentEffectKind.IceAge, ElementType.Ice, new[]{50}),

            S("fire_high_heat", "고열", "화상 부여량과 화상 피해를 강화합니다. (Lv.1 화상 +1 / Lv.2 화상 +1·피해 +10% / Lv.3 화상 +2·피해 +10%)", AugmentValueTier.Value1, AugmentBuildTag.Fire, RuleAugmentEffectKind.FireStackBonus, ElementType.Fire, new[]{1,1,2}),
            S("fire_embers", "잔불", "화상 블록에 주는 피해가 증가합니다. (10% / 20% / 30%)", AugmentValueTier.Value1, AugmentBuildTag.Fire, RuleAugmentEffectKind.BurningTargetDamage, ElementType.Fire, new[]{10,20,30}),
            S("fire_heat_spread", "열 확산", "화상 블록 재타격 시 주변에 화상을 부여합니다. (Lv.1 화상 1 / Lv.2 화상 2)", AugmentValueTier.Value2, AugmentBuildTag.Fire, RuleAugmentEffectKind.FireSpread, ElementType.Fire, new[]{1,2}),
            S("fire_furnace", "용광로", "같은 블록 반복 타격마다 피해가 증가합니다. (Lv.1 10%, 최대 50% / Lv.2 20%, 최대 100%)", AugmentValueTier.Value2, AugmentBuildTag.Fire, RuleAugmentEffectKind.RepeatedFireHit, ElementType.Fire, new[]{10,20}),
            S("fire_explosive_shatter", "폭렬 파쇄", "열충격 범위가 5×5로 증가하고 중심 피해가 100%, 주변 피해가 50% 증가합니다.", AugmentValueTier.Value3, AugmentBuildTag.Fire, RuleAugmentEffectKind.ThermalShockAmplify, ElementType.Fire, new[]{100}),
            S("fire_rampage", "화염 폭주", "한 턴 불 공 적중 5회마다 피해가 15% 증가합니다. 최대 45%이며 화상 부여량은 최대 2 증가합니다.", AugmentValueTier.Value3, AugmentBuildTag.Fire, RuleAugmentEffectKind.FireTurnRamp, ElementType.Fire, new[]{15})
        };
    }
}
#endif
