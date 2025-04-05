/// <summary>
/// Interface for components that can handle progression updates
/// </summary>
public interface IProgressionHandler
{
    /// <summary>
    /// Updates the progression value
    /// </summary>
    /// <param name="value">The progression value (0-1)</param>
    void UpdateProgression(float value);
}
