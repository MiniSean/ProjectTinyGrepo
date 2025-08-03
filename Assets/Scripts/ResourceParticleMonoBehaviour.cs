using UnityEngine;
using System.Collections;

/// <summary>
/// Controls the movement of a single resource particle from a start point
/// to an end point along a parabolic arc.
/// </summary>
public class ResourceParticleMonoBehaviour : MonoBehaviour
{
    /// <summary>
    /// Initializes the particle's movement.
    /// </summary>
    /// <param name="startPosition">The world space position to start from.</param>
    /// <param name="endTarget">The Transform of the target to move towards.</param>
    /// <param name="duration">The time in seconds the journey should take.</param>
    /// <param name="arcHeight">The height of the arc in world units.</param>
    public void Initialize(Vector3 startPosition, Transform endTarget, float duration, float arcHeight)
    {
        StartCoroutine(MoveInArc(startPosition, endTarget, duration, arcHeight));
    }

    private IEnumerator MoveInArc(Vector3 start, Transform endTarget, float duration, float arcHeight)
    {
        float timer = 0f;
        Vector3 arcVector = Vector3.up * arcHeight;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;

            // This formula calculates the position along the parabolic arc.
            // The (t - t*t) term creates the arc shape, peaking at t=0.5.
            transform.position = Vector3.Lerp(start, endTarget.position, t) + arcVector * (t - t * t) * 4f;

            yield return null;
        }

        // Ensure the final position is exact and then destroy the particle.
        transform.position = endTarget.position;
        Destroy(gameObject);
    }
}
