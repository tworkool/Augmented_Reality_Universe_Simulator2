using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SolarSystem : MonoBehaviour
{
    [SerializeField]
    public GameObject solarSystemCentre;
    [SerializeField]
    public int DISTANCE_MULTIPLIER = 50;
    [SerializeField]
    public bool alignPlanets = true;
    private GameObject[] celestialBodies;

    // pass in astronomical 
    private Vector3 ConvertAstronomicalUnitsToPositionAlongZ(Vector3 posInput, float AU)
    {
        float newZ = posInput.z + AU * DISTANCE_MULTIPLIER;
        return new Vector3(posInput.x, posInput.y, newZ);
    }

    void AlignCelestialBodiesAlongCentre()
    {
        Vector3 solarSystemOrigin = solarSystemCentre.transform.position;

        foreach(var go in this.GetComponentsInChildren<CelestialBody>())
        {
            if (go.gameObject.GetInstanceID() == solarSystemCentre.GetInstanceID())
                continue;
            if (go.astronomicalUnitsFromSolarSystemCentre == 0)
                continue;
            go.gameObject.transform.position = ConvertAstronomicalUnitsToPositionAlongZ(solarSystemOrigin, go.astronomicalUnitsFromSolarSystemCentre);
        }
    }

    private void Awake()
    {
        if (alignPlanets)
            AlignCelestialBodiesAlongCentre();
    }

    // Start is called before the first frame update
    void Start()
    {
        celestialBodies = GameObject.FindGameObjectsWithTag("CelestialBody");
        //InitialVelocity();
    }

    // Update is called once per frame
    void Update()
    {

    }

    void InitialVelocity()
    {
        foreach (GameObject celestialBody1 in celestialBodies)
        {
            var cb1RigidBody = celestialBody1.GetComponent<Rigidbody>();
            var cb1 = celestialBody1.GetComponent<CelestialBody>();
            var orbit = cb1?.orbit == null ? solarSystemCentre : cb1.orbit;
            Debug.Log(cb1?.orbit);

            // skip sun
            if (celestialBody1.GetInstanceID() == solarSystemCentre.GetInstanceID())
                continue;

            var solarSystemCentreRigidBody = orbit.GetComponent<Rigidbody>();
            var d = Vector3.Distance(celestialBody1.transform.position, orbit.transform.position);
            var m2 = solarSystemCentreRigidBody.mass;
            var a = d;

            cb1RigidBody.velocity += celestialBody1.transform.right * Mathf.Sqrt(20 * m2 * (2/d - 1/a));
            // https://www.vanderbilt.edu/AnS/physics/astrocourses/ast201/orbitalvelocity.html
            // replace a in function with aphelion or periphelion. This will cause an elliptic orbit
            // (don't forget to translate the AU units into Unity distance units)
        }
    }
}
