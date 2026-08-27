using System;
using Unity.VisualScripting;
using UnityEngine;

public class LanderVisuals : MonoBehaviour
{
    [SerializeField] private ParticleSystem leftThrusterParticleSystem;
    [SerializeField] private ParticleSystem middleThrusterParticleSystem;
    [SerializeField] private ParticleSystem rightThrusterParticleSystem;
    [SerializeField] private GameObject landerExplosionVfx;

    private Lander lander;
    
    private void Awake()
    {
        lander = GetComponent<Lander>();

        lander.OnUpForce += Lander_OnUpForce;
        lander.OnLeftForce += Lander_OnLeftForce;
        lander.OnRightForce += Lander_OnRightForce;
        lander.OnBeforForce += Lander_OnBeforForce;

        SetEnableThrusterParticleSystem(leftThrusterParticleSystem, false);
        SetEnableThrusterParticleSystem(middleThrusterParticleSystem, false);
        SetEnableThrusterParticleSystem(rightThrusterParticleSystem, false);
    }

    private void Start()
    {
        lander.OnLanded += Lander_OnLanded;
    }
    private void Lander_OnLanded(object sender, Lander.OnLanedEventArgs e)
    {
        switch (e.landingType)
        {
            case Lander.LandingType.TooFastLanding:
            case Lander.LandingType.TooSteepAngle:
            case Lander.LandingType.WrongLandingArea:
                //crash!
                Instantiate(landerExplosionVfx, transform.position, Quaternion.identity);
                gameObject.SetActive(false);
                break;
        }
    }

    private void Lander_OnBeforForce(object sender, EventArgs e)
    {
        SetEnableThrusterParticleSystem(leftThrusterParticleSystem, false);
        SetEnableThrusterParticleSystem(middleThrusterParticleSystem, false);
        SetEnableThrusterParticleSystem(rightThrusterParticleSystem, false);
    }

    private void Lander_OnRightForce(object sender, EventArgs e)
    {
        SetEnableThrusterParticleSystem(leftThrusterParticleSystem, true);
        //SetEnableThrusterParticleSystem(middleThrusterParticleSystem, true);
        SetEnableThrusterParticleSystem(rightThrusterParticleSystem, false);
    }

    private void Lander_OnLeftForce(object sender, EventArgs e)
    {      
        SetEnableThrusterParticleSystem(leftThrusterParticleSystem, false);
        //SetEnableThrusterParticleSystem(middleThrusterParticleSystem, true);
        SetEnableThrusterParticleSystem(rightThrusterParticleSystem, true);
    }

    private void Lander_OnUpForce( object sender, EventArgs e)
    {
        SetEnableThrusterParticleSystem(leftThrusterParticleSystem, true);
        SetEnableThrusterParticleSystem(middleThrusterParticleSystem, true);
        SetEnableThrusterParticleSystem(rightThrusterParticleSystem, true);
    }

    private void SetEnableThrusterParticleSystem(ParticleSystem particleSystem, bool enabled)
    {
        var emission = particleSystem.emission;
        emission.enabled = enabled;
    }
}
