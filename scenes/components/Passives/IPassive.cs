using Godot;

public interface IPassive
{
    /// <summary>
    /// The display name of this passive ability
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// A brief description of what this passive does
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// The icon/visual representation scene for this passive
    /// </summary>
    PackedScene IconScene { get; }
    
    /// <summary>
    /// Called when the passive is acquired by the player
    /// </summary>
    void OnAcquired();
    
    /// <summary>
    /// Called when the passive should be removed (e.g., game reset)
    /// </summary>
    void OnRemoved();
}