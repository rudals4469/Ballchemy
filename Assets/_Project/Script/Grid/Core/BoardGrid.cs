using UnityEngine;

public sealed class BoardGrid : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private BoardGridSettings settings;

    [Header("Scene View")]
    [SerializeField]
    private bool drawGridAlways;

    [SerializeField, Min(0.01f)]
    private float centerMarkerSize = 0.2f;

    public BoardGridSettings Settings =>
        settings;

    public int ColumnCount =>
        settings != null
            ? settings.ColumnCount
            : 0;

    public int RowCount =>
        settings != null
            ? settings.RowCount
            : 0;

    public float CellSize =>
        settings != null
            ? settings.CellSize
            : 1f;

    public int CenterColumn =>
        settings != null
            ? settings.CenterColumn
            : 0;

    public int CenterRow =>
        settings != null
            ? settings.CenterRow
            : 0;

    public Vector2Int CenterCell =>
        new Vector2Int(
            CenterColumn,
            CenterRow
        );

    public Vector3 CenterCellWorldPosition =>
        GetCellWorldPosition(
            CenterColumn,
            CenterRow
        );

    private void Awake()
    {
        ValidateSettings();
    }

    private void OnValidate()
    {
        centerMarkerSize =
            Mathf.Max(
                centerMarkerSize,
                0.01f
            );
    }

    private void ValidateSettings()
    {
        if (settings == null)
        {
            Debug.LogError(
                "BoardGrid: " +
                "BoardGridSettings가 연결되지 않았습니다.",
                this
            );

            return;
        }

        if (!settings.HasExactCenterCell)
        {
            Debug.LogWarning(
                "BoardGrid: 현재 격자는 정확한 중앙 셀이 없습니다. " +
                $"크기: {settings.ColumnCount} × " +
                $"{settings.RowCount}",
                this
            );
        }
    }

    public bool IsInside(
        int column,
        int row)
    {
        return settings != null &&
               settings.IsInside(
                   column,
                   row
               );
    }

    public bool IsInside(
        Vector2Int cell)
    {
        return IsInside(
            cell.x,
            cell.y
        );
    }

    public Vector3 GetCellWorldPosition(
        int column,
        int row)
    {
        if (settings == null)
        {
            return transform.position;
        }

        float horizontalCenter =
            (settings.ColumnCount - 1) *
            0.5f;

        float verticalCenter =
            (settings.RowCount - 1) *
            0.5f;

        float localX =
            (
                column -
                horizontalCenter
            ) *
            settings.CellSize;

        float localY =
            (
                verticalCenter -
                row
            ) *
            settings.CellSize;

        return transform.position +
               transform.right * localX +
               transform.up * localY;
    }

    public Vector3 GetCellWorldPosition(
        Vector2Int cell)
    {
        return GetCellWorldPosition(
            cell.x,
            cell.y
        );
    }

    public bool TryGetCellWorldPosition(
        int column,
        int row,
        out Vector3 worldPosition)
    {
        if (!IsInside(
                column,
                row))
        {
            worldPosition =
                transform.position;

            return false;
        }

        worldPosition =
            GetCellWorldPosition(
                column,
                row
            );

        return true;
    }

    public Vector2Int GetNearestCell(
        Vector3 worldPosition)
    {
        if (settings == null)
        {
            return Vector2Int.zero;
        }

        Vector3 difference =
            worldPosition -
            transform.position;

        float localX =
            Vector3.Dot(
                difference,
                transform.right
            );

        float localY =
            Vector3.Dot(
                difference,
                transform.up
            );

        float horizontalCenter =
            (settings.ColumnCount - 1) *
            0.5f;

        float verticalCenter =
            (settings.RowCount - 1) *
            0.5f;

        int column =
            Mathf.RoundToInt(
                localX /
                settings.CellSize +
                horizontalCenter
            );

        int row =
            Mathf.RoundToInt(
                verticalCenter -
                localY /
                settings.CellSize
            );

        return new Vector2Int(
            column,
            row
        );
    }

    public Vector2Int GetClampedCell(
        Vector3 worldPosition)
    {
        Vector2Int cell =
            GetNearestCell(
                worldPosition
            );

        if (settings == null)
        {
            return cell;
        }

        cell.x =
            Mathf.Clamp(
                cell.x,
                0,
                settings.ColumnCount - 1
            );

        cell.y =
            Mathf.Clamp(
                cell.y,
                0,
                settings.RowCount - 1
            );

        return cell;
    }

    public Vector3 GetBoardTopLeftCorner()
    {
        if (settings == null)
        {
            return transform.position;
        }

        return transform.position -
               transform.right *
               settings.HalfBoardWidth +
               transform.up *
               settings.HalfBoardHeight;
    }

    public Vector3 GetBoardBottomRightCorner()
    {
        if (settings == null)
        {
            return transform.position;
        }

        return transform.position +
               transform.right *
               settings.HalfBoardWidth -
               transform.up *
               settings.HalfBoardHeight;
    }

    private void OnDrawGizmos()
    {
        if (drawGridAlways)
        {
            DrawGridGizmos();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGridAlways)
        {
            DrawGridGizmos();
        }
    }

    private void DrawGridGizmos()
    {
        if (settings == null)
        {
            return;
        }

        float boardWidth =
            settings.BoardWidth;

        float boardHeight =
            settings.BoardHeight;

        float halfWidth =
            settings.HalfBoardWidth;

        float halfHeight =
            settings.HalfBoardHeight;

        Matrix4x4 previousMatrix =
            Gizmos.matrix;

        Gizmos.matrix =
            Matrix4x4.TRS(
                transform.position,
                transform.rotation,
                Vector3.one
            );

        Gizmos.color =
            new Color(
                1f,
                1f,
                1f,
                0.35f
            );

        for (int column = 0;
             column <= settings.ColumnCount;
             column++)
        {
            float x =
                -halfWidth +
                column *
                settings.CellSize;

            Gizmos.DrawLine(
                new Vector3(
                    x,
                    -halfHeight,
                    0f
                ),
                new Vector3(
                    x,
                    halfHeight,
                    0f
                )
            );
        }

        for (int row = 0;
             row <= settings.RowCount;
             row++)
        {
            float y =
                halfHeight -
                row *
                settings.CellSize;

            Gizmos.DrawLine(
                new Vector3(
                    -halfWidth,
                    y,
                    0f
                ),
                new Vector3(
                    halfWidth,
                    y,
                    0f
                )
            );
        }

        Gizmos.color =
            new Color(
                1f,
                0.85f,
                0.15f,
                1f
            );

        Gizmos.DrawWireCube(
            Vector3.zero,
            new Vector3(
                boardWidth,
                boardHeight,
                0f
            )
        );

        float centerLocalX =
            (
                settings.CenterColumn -
                (
                    settings.ColumnCount - 1
                ) *
                0.5f
            ) *
            settings.CellSize;

        float centerLocalY =
            (
                (
                    settings.RowCount - 1
                ) *
                0.5f -
                settings.CenterRow
            ) *
            settings.CellSize;

        Gizmos.color =
            new Color(
                1f,
                0.2f,
                0.2f,
                1f
            );

        Gizmos.DrawSphere(
            new Vector3(
                centerLocalX,
                centerLocalY,
                0f
            ),
            centerMarkerSize
        );

        Gizmos.matrix =
            previousMatrix;
    }
}