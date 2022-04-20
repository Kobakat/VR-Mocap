/// <summary>
/// Attach this to objects that when enabled will cause all avatars within prosimity to die with explosive force.
/// Useful for grenades or gameobejcts that are timed by animations and enable the explosion.
/// </summary>
public class ExplosiveDeath : SandboxBase
{

	public float disableAfterSeconds = 10;

	public float explosionRadius = 25;

	public float explosionForce = 10;
}
