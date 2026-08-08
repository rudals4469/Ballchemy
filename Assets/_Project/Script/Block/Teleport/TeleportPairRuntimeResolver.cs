using System.Collections.Generic;
using UnityEngine;

public static class TeleportPairRuntimeResolver
{
    public static void Connect(
        IReadOnlyList<BlockSpawnRequest> requests,
        IReadOnlyList<Block> blocks)
    {
        if (requests == null || blocks == null) return;

        Dictionary<Vector2Int, Block> byPosition = new Dictionary<Vector2Int, Block>();
        Dictionary<Vector2Int, BlockSpawnRequest> requestsByPosition =
            new Dictionary<Vector2Int, BlockSpawnRequest>();

        for (int i = 0; i < requests.Count; i++)
        {
            BlockSpawnRequest request = requests[i];
            if (request != null)
            {
                requestsByPosition[new Vector2Int(request.StartColumn, request.StartRow)] = request;
            }
        }

        for (int i = 0; i < blocks.Count; i++)
        {
            Block block = blocks[i];
            if (block != null) byPosition[new Vector2Int(block.StartColumn, block.StartRow)] = block;
        }

        for (int i = 0; i < requests.Count; i++)
        {
            BlockSpawnRequest request = requests[i];
            if (request == null || !request.HasTeleportPair)
            {
                continue;
            }

            Vector2Int sourcePosition =
                new Vector2Int(request.StartColumn, request.StartRow);

            byPosition.TryGetValue(sourcePosition, out Block source);
            byPosition.TryGetValue(request.TeleportPartnerPosition, out Block destination);

            bool hasValidPair =
                requestsByPosition.TryGetValue(
                    request.TeleportPartnerPosition,
                    out BlockSpawnRequest partnerRequest) &&
                partnerRequest.TeleportPairId == request.TeleportPairId &&
                partnerRequest.TeleportPartnerPosition == sourcePosition &&
                source != null &&
                destination != null;

            if (!hasValidPair)
            {
                source?.ExpireWithoutReward();
                continue;
            }

            TeleportPortalController sourcePortal = source.GetComponent<TeleportPortalController>();
            TeleportPortalController destinationPortal = destination.GetComponent<TeleportPortalController>();

            if (sourcePortal != null && destinationPortal != null)
            {
                sourcePortal.Link(destinationPortal);
                continue;
            }

            source.ExpireWithoutReward();
        }
    }
}
