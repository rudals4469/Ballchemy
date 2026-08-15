using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SecretRoomCoordinateSelector :
    MonoBehaviour
{
    [Header("Candidate Rules")]

    [Tooltip(
        "비밀방 후보 좌표에 인접해야 하는 최소 입구 방 수입니다."
    )]
    [SerializeField, Range(1, 4)]
    private int minimumAdjacentEntranceRooms = 2;

    [Tooltip(
        "NormalCombat 방을 비밀방 입구 후보로 허용합니다."
    )]
    [SerializeField]
    private bool allowNormalCombatEntrance = true;

    [Tooltip(
        "NamedCombat 방을 비밀방 입구 후보로 허용합니다."
    )]
    [SerializeField]
    private bool allowNamedCombatEntrance = true;

    [Header("Score")]

    [Tooltip(
        "인접한 입구 후보 방 하나당 추가되는 점수입니다."
    )]
    [SerializeField, Min(1)]
    private int scorePerEntranceRoom = 100;

    [Tooltip(
        "맵 중심에서 멀어질수록 차감되는 점수입니다."
    )]
    [SerializeField, Min(0)]
    private int distanceFromCenterPenalty = 5;

    [Tooltip(
        "시작방에 인접한 후보에 적용되는 감점입니다."
    )]
    [SerializeField, Min(0)]
    private int startRoomAdjacentPenalty = 1000;

    [Tooltip(
        "보스방에 인접한 후보에 적용되는 감점입니다."
    )]
    [SerializeField, Min(0)]
    private int bossRoomAdjacentPenalty = 1000;

    [Tooltip(
        "상점·보상·이벤트방에 인접한 후보에 적용되는 감점입니다."
    )]
    [SerializeField, Min(0)]
    private int specialRoomAdjacentPenalty = 500;

    [Header("Debug")]

    [SerializeField]
    private bool logCandidateScores;

    [SerializeField]
    private bool logSelectedCandidate = true;

    private static readonly RoomDirection[]
        Directions =
        {
            RoomDirection.Up,
            RoomDirection.Right,
            RoomDirection.Down,
            RoomDirection.Left
        };

    private void Awake()
    {
        NormalizeSettings();
    }

    private void OnValidate()
    {
        NormalizeSettings();
    }

    public bool TryFindSecretRoomPosition(
        StageMap map,
        out Vector2Int selectedPosition)
    {
        int seed =
            Environment.TickCount;

        System.Random random =
            new System.Random(
                seed
            );

        return TryFindSecretRoomPosition(
            map,
            random,
            out selectedPosition
        );
    }

    public bool TryFindSecretRoomPosition(
        StageMap map,
        System.Random random,
        out Vector2Int selectedPosition)
    {
        selectedPosition =
            default;

        NormalizeSettings();

        if (map == null)
        {
            Debug.LogWarning(
                "SecretRoomCoordinateSelector: " +
                "StageMap이 없어 비밀방 후보를 찾을 수 없습니다.",
                this
            );

            return false;
        }

        if (random == null)
        {
            Debug.LogWarning(
                "SecretRoomCoordinateSelector: " +
                "Random 인스턴스가 없어 비밀방 후보를 " +
                "선택할 수 없습니다.",
                this
            );

            return false;
        }

        if (map.RoomCount <= 0 ||
            map.Rooms == null)
        {
            Debug.LogWarning(
                "SecretRoomCoordinateSelector: " +
                "생성된 방이 없어 비밀방 후보를 찾을 수 없습니다.",
                this
            );

            return false;
        }

        HashSet<Vector2Int> emptyPositions =
            CollectEmptyAdjacentPositions(
                map
            );

        if (emptyPositions.Count <= 0)
        {
            Debug.LogWarning(
                "SecretRoomCoordinateSelector: " +
                "검사할 빈 좌표가 없습니다.",
                this
            );

            return false;
        }

        List<CandidateEvaluation>
            validCandidates =
                new List<CandidateEvaluation>();

        foreach (Vector2Int position
                 in emptyPositions)
        {
            CandidateEvaluation evaluation =
                EvaluateCandidate(
                    map,
                    position
                );

            if (logCandidateScores)
            {
                LogCandidate(
                    evaluation
                );
            }

            if (!evaluation.IsValid)
            {
                continue;
            }

            validCandidates.Add(
                evaluation
            );
        }

        if (validCandidates.Count <= 0)
        {
            Debug.LogWarning(
                "SecretRoomCoordinateSelector: " +
                "조건을 만족하는 비밀방 후보 좌표가 없습니다. " +
                $"최소 인접 입구 수=" +
                $"{minimumAdjacentEntranceRooms}",
                this
            );

            return false;
        }

        int highestScore =
            FindHighestScore(
                validCandidates
            );

        List<CandidateEvaluation>
            highestScoreCandidates =
                CollectHighestScoreCandidates(
                    validCandidates,
                    highestScore
                );

        if (highestScoreCandidates.Count <= 0)
        {
            return false;
        }

        CandidateEvaluation selectedCandidate =
            highestScoreCandidates[
                random.Next(
                    highestScoreCandidates.Count
                )
            ];

        selectedPosition =
            selectedCandidate.Position;

        if (logSelectedCandidate)
        {
            Debug.Log(
                "SecretRoomCoordinateSelector: " +
                "비밀방 후보 선정 완료\n" +
                $"Position={selectedCandidate.Position}\n" +
                $"Score={selectedCandidate.Score}\n" +
                $"EntranceRooms=" +
                $"{selectedCandidate.EntranceRoomCount}\n" +
                $"AdjacentRooms=" +
                $"{selectedCandidate.TotalAdjacentRoomCount}\n" +
                $"HighestScoreCandidateCount=" +
                $"{highestScoreCandidates.Count}",
                this
            );
        }

        return true;
    }

    public List<RoomNode> GetEntranceRooms(
        StageMap map,
        Vector2Int secretRoomPosition)
    {
        List<RoomNode> entranceRooms =
            new List<RoomNode>();

        if (map == null)
        {
            return entranceRooms;
        }

        for (int i = 0;
             i < Directions.Length;
             i++)
        {
            Vector2Int adjacentPosition =
                secretRoomPosition +
                RoomDirectionUtility.ToOffset(
                    Directions[i]
                );

            RoomNode adjacentRoom =
                map.GetRoomAt(
                    adjacentPosition
                );

            if (!CanUseAsEntranceRoom(
                    adjacentRoom
                ))
            {
                continue;
            }

            entranceRooms.Add(
                adjacentRoom
            );
        }

        return entranceRooms;
    }

    private HashSet<Vector2Int>
        CollectEmptyAdjacentPositions(
            StageMap map)
    {
        HashSet<Vector2Int> emptyPositions =
            new HashSet<Vector2Int>();

        if (map == null ||
            map.Rooms == null)
        {
            return emptyPositions;
        }

        IReadOnlyList<RoomNode> rooms =
            map.Rooms;

        for (int roomIndex = 0;
             roomIndex < rooms.Count;
             roomIndex++)
        {
            RoomNode room =
                rooms[roomIndex];

            if (room == null)
            {
                continue;
            }

            for (int directionIndex = 0;
                 directionIndex < Directions.Length;
                 directionIndex++)
            {
                Vector2Int candidatePosition =
                    room.GridPosition +
                    RoomDirectionUtility.ToOffset(
                        Directions[directionIndex]
                    );

                if (map.ContainsPosition(
                        candidatePosition
                    ))
                {
                    continue;
                }

                emptyPositions.Add(
                    candidatePosition
                );
            }
        }

        return emptyPositions;
    }

    private CandidateEvaluation EvaluateCandidate(
        StageMap map,
        Vector2Int position)
    {
        CandidateEvaluation evaluation =
            new CandidateEvaluation(
                position
            );

        for (int i = 0;
             i < Directions.Length;
             i++)
        {
            Vector2Int adjacentPosition =
                position +
                RoomDirectionUtility.ToOffset(
                    Directions[i]
                );

            RoomNode adjacentRoom =
                map.GetRoomAt(
                    adjacentPosition
                );

            if (adjacentRoom == null)
            {
                continue;
            }

            evaluation.TotalAdjacentRoomCount++;

            if (CanUseAsEntranceRoom(
                    adjacentRoom
                ))
            {
                evaluation.EntranceRoomCount++;

                continue;
            }

            switch (adjacentRoom.RoomType)
            {
                case RoomType.Start:
                    evaluation.IsAdjacentToStart =
                        true;
                    break;

                case RoomType.Boss:
                    evaluation.IsAdjacentToBoss =
                        true;
                    break;

                case RoomType.Shop:
                case RoomType.Alchemy:
                case RoomType.Event:
                case RoomType.Augment:
                    evaluation.AdjacentSpecialRoomCount++;
                    break;

                case RoomType.Secret:
                    evaluation.IsAdjacentToSecret =
                        true;
                    break;
            }
        }

        evaluation.IsValid =
            evaluation.EntranceRoomCount >=
                minimumAdjacentEntranceRooms &&
            !evaluation.IsAdjacentToSecret;

        if (!evaluation.IsValid)
        {
            return evaluation;
        }

        int score =
            evaluation.EntranceRoomCount *
            scorePerEntranceRoom;

        int centerDistance =
            Mathf.Abs(
                position.x
            ) +
            Mathf.Abs(
                position.y
            );

        score -=
            centerDistance *
            distanceFromCenterPenalty;

        if (evaluation.IsAdjacentToStart)
        {
            score -=
                startRoomAdjacentPenalty;
        }

        if (evaluation.IsAdjacentToBoss)
        {
            score -=
                bossRoomAdjacentPenalty;
        }

        score -=
            evaluation.AdjacentSpecialRoomCount *
            specialRoomAdjacentPenalty;

        evaluation.Score =
            score;

        return evaluation;
    }

    private bool CanUseAsEntranceRoom(
        RoomNode room)
    {
        if (room == null)
        {
            return false;
        }

        if (room.RoomType ==
                RoomType.NormalCombat &&
            allowNormalCombatEntrance)
        {
            return true;
        }

        if (room.RoomType ==
                RoomType.NamedCombat &&
            allowNamedCombatEntrance)
        {
            return true;
        }

        return false;
    }

    private static int FindHighestScore(
        List<CandidateEvaluation> candidates)
    {
        int highestScore =
            int.MinValue;

        for (int i = 0;
             i < candidates.Count;
             i++)
        {
            CandidateEvaluation candidate =
                candidates[i];

            if (candidate.Score <=
                highestScore)
            {
                continue;
            }

            highestScore =
                candidate.Score;
        }

        return highestScore;
    }

    private static List<CandidateEvaluation>
        CollectHighestScoreCandidates(
            List<CandidateEvaluation> candidates,
            int highestScore)
    {
        List<CandidateEvaluation>
            highestScoreCandidates =
                new List<CandidateEvaluation>();

        for (int i = 0;
             i < candidates.Count;
             i++)
        {
            CandidateEvaluation candidate =
                candidates[i];

            if (candidate.Score !=
                highestScore)
            {
                continue;
            }

            highestScoreCandidates.Add(
                candidate
            );
        }

        return highestScoreCandidates;
    }

    private void LogCandidate(
        CandidateEvaluation candidate)
    {
        Debug.Log(
            "SecretRoomCoordinateSelector: " +
            "후보 평가\n" +
            $"Position={candidate.Position}\n" +
            $"Valid={candidate.IsValid}\n" +
            $"Score={candidate.Score}\n" +
            $"EntranceRooms=" +
            $"{candidate.EntranceRoomCount}\n" +
            $"AdjacentRooms=" +
            $"{candidate.TotalAdjacentRoomCount}\n" +
            $"AdjacentToStart=" +
            $"{candidate.IsAdjacentToStart}\n" +
            $"AdjacentToBoss=" +
            $"{candidate.IsAdjacentToBoss}\n" +
            $"AdjacentSpecialRooms=" +
            $"{candidate.AdjacentSpecialRoomCount}",
            this
        );
    }

    private void NormalizeSettings()
    {
        minimumAdjacentEntranceRooms =
            Mathf.Clamp(
                minimumAdjacentEntranceRooms,
                1,
                4
            );

        scorePerEntranceRoom =
            Mathf.Max(
                scorePerEntranceRoom,
                1
            );

        distanceFromCenterPenalty =
            Mathf.Max(
                distanceFromCenterPenalty,
                0
            );

        startRoomAdjacentPenalty =
            Mathf.Max(
                startRoomAdjacentPenalty,
                0
            );

        bossRoomAdjacentPenalty =
            Mathf.Max(
                bossRoomAdjacentPenalty,
                0
            );

        specialRoomAdjacentPenalty =
            Mathf.Max(
                specialRoomAdjacentPenalty,
                0
            );
    }

    private struct CandidateEvaluation
    {
        public Vector2Int Position;

        public int Score;

        public int EntranceRoomCount;

        public int TotalAdjacentRoomCount;

        public int AdjacentSpecialRoomCount;

        public bool IsAdjacentToStart;

        public bool IsAdjacentToBoss;

        public bool IsAdjacentToSecret;

        public bool IsValid;

        public CandidateEvaluation(
            Vector2Int position)
        {
            Position =
                position;

            Score =
                int.MinValue;

            EntranceRoomCount =
                0;

            TotalAdjacentRoomCount =
                0;

            AdjacentSpecialRoomCount =
                0;

            IsAdjacentToStart =
                false;

            IsAdjacentToBoss =
                false;

            IsAdjacentToSecret =
                false;

            IsValid =
                false;
        }
    }
}
