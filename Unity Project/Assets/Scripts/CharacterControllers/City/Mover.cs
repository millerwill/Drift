using UnityEngine;

public class Mover : MonoBehaviour
{
    [SerializeField]
    private ParticleSystem[] particles;

    public bool engToggle;

    private void Start()
    {
        engToggle = false;
        SetParticleEmission(false);
    }

    private void Update()
    {
        SetParticleEmission(engToggle);
    }

    private void SetParticleEmission(bool enabled)
    {
        foreach (ParticleSystem particle in particles)
        {
            if (particle == null)
                continue;

            ParticleSystem.EmissionModule emission = particle.emission;
            emission.enabled = enabled;
        }
    }
}