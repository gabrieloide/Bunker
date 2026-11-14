using System.Collections.Generic;
using UnityEngine;

// Builds a limited segment of player-designed ally road within FlagTerritory (outside EnemyTerritory).
// Shows a live monochrome preview of the planned grid tiles while dragging.
public class RoadCard : Card
{
    readonly List<Vector3Int> plannedCells = new List<Vector3Int>();
    Vector3Int plannedAnchor;
    bool hasValidPlan;

    int MaxTilesPerCard => definition is RoadCardDefinition road ? road.maxTilesPerCard : 5;
    float BuildTime => definition is RoadCardDefinition road ? road.buildTime : 0.6f;
    float HalfWidth => definition is RoadCardDefinition road ? road.halfWidth : 0.16f;

    protected override bool SnapsToGrid => true;
    protected override bool OccupiesCell => false;
    protected override bool InTerritory => true;
    protected override bool HasObstacleBelow() => false;

    public override bool CanPlaceNow
    {
        get
        {
            if (!PlayableNow) return false;
            RefreshPlan();
            return hasValidPlan;
        }
    }

    protected override bool AllowPlacement() => CanPlaceNow;

    protected override void ShowPlacementFeedback(bool valid)
    {
        var overlay = AllyRoadOverlay.EnsureExists();
        if (valid && hasValidPlan)
        {
            overlay.ShowPreview(plannedAnchor, plannedCells, HalfWidth);
            PlacementCrossIndicator.Hide();
        }
        else
        {
            overlay.HidePreview();
            Vector3 crossPos = TryGetPreviewPosition(out Vector3 target) ? target : PointerWorld();
            PlacementCrossIndicator.Show(crossPos);
        }
    }

    protected override void HidePlacementFeedback()
    {
        if (AllyRoadOverlay.Instance != null)
            AllyRoadOverlay.Instance.HidePreview();
        base.HidePlacementFeedback();
    }

    protected override void OnDestroy()
    {
        if (AllyRoadOverlay.Instance != null)
            AllyRoadOverlay.Instance.HidePreview();
        base.OnDestroy();
    }

    protected override void CardBehaviour()
    {
        RefreshPlan();
        if (!hasValidPlan || plannedCells.Count == 0)
            return;

        var cellsToCommit = new List<Vector3Int>(plannedCells);
        Vector3 buildSite = AllyRoadNetwork.CellToWorld(cellsToCommit[cellsToCommit.Count - 1]);

        if (BuilderSquad.Instance != null && BuilderSquad.Instance.Available)
        {
            var marker = BuilderSquad.MarkSite(buildSite);
            float duration = BuildTime;
            BuilderSquad.Instance.Send(buildSite, () =>
            {
                if (marker != null) Destroy(marker);
                CommitRoad(cellsToCommit);
                return duration;
            });
        }
        else
        {
            CommitRoad(cellsToCommit);
        }
    }

    void CommitRoad(List<Vector3Int> cellsToCommit)
    {
        if (!AllyRoadNetwork.CommitSegment(cellsToCommit))
            return;

        if (SimulationBridge.Instance != null)
            SimulationBridge.Instance.SyncAllyRoadPath();
    }

    void RefreshPlan()
    {
        if (!AllyRoadNetwork.Initialized)
        {
            Vector3 fallbackBunker = BuilderSquad.Instance != null ? BuilderSquad.Instance.transform.position : Vector3.zero;
            int maxTiles = LevelSetup.Current != null ? LevelSetup.Current.maxAllyRoadTiles : 12;
            AllyRoadNetwork.Initialize(fallbackBunker, maxTiles);
        }

        hasValidPlan = AllyRoadNetwork.TryPlanSegment(PointerWorld(), MaxTilesPerCard, out var cells, out plannedAnchor);
        plannedCells.Clear();
        if (hasValidPlan)
            plannedCells.AddRange(cells);
    }
}
