using UnityEngine;

/// <summary>
/// Represents a single cell on the battle grid.
/// In a full FFT-style game each tile would also store height/elevation
/// for jump calculations, but we keep it flat to start.
/// </summary>
public class Tile : MonoBehaviour
{
    public int x;
    public int z;
    public bool walkable = true;
    public int height = 0; // elevation level, used later for jump checks

    [HideInInspector] public Unit occupant; // unit currently standing here

    private Renderer _renderer;
    private Color _baseColor;

    public static readonly Color ColorDefault = new Color(0.85f, 0.85f, 0.85f);
    public static readonly Color ColorMoveRange = new Color(0.3f, 0.6f, 1f);
    public static readonly Color ColorAttackRange = new Color(1f, 0.35f, 0.3f);
    public static readonly Color ColorSelected = new Color(1f, 0.9f, 0.2f);

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        if (_renderer != null) _baseColor = _renderer.material.color;
    }

    public bool IsOccupied => occupant != null;

    public void SetHighlight(Color color)
    {
        if (_renderer != null) _renderer.material.color = color;
    }

    public void ClearHighlight()
    {
        if (_renderer != null) _renderer.material.color = ColorDefault;
    }

    public Vector2Int Coord => new Vector2Int(x, z);
}
