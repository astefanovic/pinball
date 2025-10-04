using Godot;

public interface IMultiCell
{
    /// <summary>
    /// Gets the size in grid cells that this component occupies
    /// </summary>
    Vector2I CellSize { get; }
    
    /// <summary>
    /// Gets the offset of cells that this component occupies relative to its grid position
    /// For example, a 2x1 component might occupy cells at (0,0) and (1,0) relative to its origin
    /// </summary>
    Vector2I[] OccupiedCells { get; }
}