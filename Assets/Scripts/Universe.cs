using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Universe : MonoBehaviour
{
    //public List<GameObject> celestialBodies;
    public float G = 50f;
    public bool disableTrails = false;
    private bool previousDisableTrails;
    public float massMultiplier = 1;
    public GameObject[] celestialBodies;

    // Start is called before the first frame update
    void Start()
    {
        //celestialBodies.AddRange(GameObject.FindGameObjectsWithTag("CelestialBody"));
        InitialVelocity();
    }

    private void FixedUpdate()
    {
        celestialBodies = GameObject.FindGameObjectsWithTag("CelestialBody");
        ApplyGravity();
        if (previousDisableTrails != disableTrails)
            ToggleTrails();
        previousDisableTrails = disableTrails;
    }

    public Vector3 CalculateNextBodyVelocity(GameObject body)
    {
        Vector3 velocity = Vector3.zero;

        foreach (GameObject celestialBody2 in celestialBodies)
        {
            if (body.GetInstanceID() == celestialBody2.GetInstanceID())
                continue;
            if (body.name == "Sol")
                continue;

            var cb1RigidBody = body.GetComponent<Rigidbody>();
            var cb2RigidBody = celestialBody2.GetComponent<Rigidbody>();

            var d = Vector3.Distance(body.transform.position, celestialBody2.transform.position);
            var m1 = cb1RigidBody.mass;
            var m2 = cb2RigidBody.mass;

            Vector3 partlyVelocity = (celestialBody2.transform.position - body.transform.position).normalized * (G * (m1 * m2) / (d * d));
            velocity += partlyVelocity;
        }

        return velocity;
    }

    public List<Vector3> SimulateNextGravitySteps(int steps, float stepSize, Vector3 initialPosition, Vector3 initialVelocity, float mass, float size, out Vector3? hitPosition)
    {
        var trajectoryPoints = new List<Vector3>();
        var numOfBodies = celestialBodies.Length + 1;
        var massList = new float[numOfBodies];
        var posList = new Vector3[numOfBodies];
        var velList = new Vector3[numOfBodies];
        var sizeList = new float[numOfBodies];
        hitPosition = null;

        // set init pos, velocity and general mass
        for (int i = 0; i < numOfBodies - 1; i++)
        {
            GameObject go = celestialBodies[i];
            if (go == null) return trajectoryPoints;
            Rigidbody rb = go.GetComponent<Rigidbody>();
            posList[i] = go.transform.position;
            massList[i] = rb.mass;
            velList[i] = rb.velocity;
            //sizeList[i] = go.GetComponent<SphereCollider>().radius;
            sizeList[i] = go.GetComponent<SphereCollider>().radius * Mathf.Max(go.transform.lossyScale.x, go.transform.lossyScale.y, go.transform.lossyScale.z);
        }
        // add those values also for virtual object
        posList[numOfBodies - 1] = initialPosition;
        massList[numOfBodies - 1] = mass;
        velList[numOfBodies - 1] = initialVelocity;
        sizeList[numOfBodies - 1] = size;

        // simulate gravity for steps by calculating stepwise position and velocity
        for (int i = 0; i < steps; i++)
        {
            //float stepTimePassed = stepSize * i;
            Vector3 currentVirtualBodyPosition = posList[numOfBodies - 1];
            if (Vector3.positiveInfinity == currentVirtualBodyPosition || Vector3.negativeInfinity == currentVirtualBodyPosition) continue;
            trajectoryPoints.Add(currentVirtualBodyPosition);

            for (int j = 0; j < numOfBodies; j++)
            {
                var cTempPosJ = posList[j];
                var cTempMassJ = massList[j];
                Vector3 diffVelocity = Vector3.zero;

                for (int k = 0; k < numOfBodies; k++)
                {
                    // same body skip
                    if (k == j) continue;

                    var cTempPosK = posList[k];
                    var cTempMassK = massList[k];

                    // check if virtual body hits any other body
                    if (j == numOfBodies - 1)
                    {
                        var dist = Vector3.Distance(cTempPosJ, cTempPosK) - sizeList[j] - sizeList[k];
                        if (dist <= 0)
                        {
                            hitPosition = currentVirtualBodyPosition;
                            return trajectoryPoints;
                        }
                    }

                    var d = Vector3.Distance(cTempPosJ, cTempPosK);
                    var m1 = cTempMassJ;
                    var m2 = cTempMassK;

                    Vector3 partlyVelocity = (cTempPosK - cTempPosJ).normalized * (G * (m1 * m2) / (d * d));
                    diffVelocity += partlyVelocity;
                }

                velList[j] += diffVelocity * stepSize / cTempMassJ;
                posList[j] += velList[j] * stepSize;
            }
        }

        return trajectoryPoints;
    }

    void ApplyGravity()
    {
        foreach (GameObject celestialBody1 in celestialBodies)
        {
            var cb1RigidBody = celestialBody1.GetComponent<Rigidbody>();
            var velocity = CalculateNextBodyVelocity(celestialBody1);
            cb1RigidBody.AddForce(velocity);
        }
    }

    void ToggleTrails()
    {
        foreach (TrailRenderer tr in GetComponentsInChildren<TrailRenderer>())
        {
            tr.enabled = !disableTrails;
        }
    }

    void InitialVelocity()
    {
        var _celestialBodies = GameObject.FindGameObjectsWithTag("CelestialBody");
        foreach (GameObject celestialBody1 in _celestialBodies)
        {
            var cb1RigidBody = celestialBody1.GetComponent<Rigidbody>();
            cb1RigidBody.mass *= massMultiplier;

            if (celestialBody1.name == "Sol")
                continue;

            foreach (GameObject celestialBody2 in _celestialBodies)
            {
                if (celestialBody1.GetInstanceID() == celestialBody2.GetInstanceID())
                    continue;

                var cb2RigidBody = celestialBody2.GetComponent<Rigidbody>();

                var d = Vector3.Distance(celestialBody1.transform.position, celestialBody2.transform.position);
                var m2 = cb2RigidBody.mass;
                var a = d;

                //cb1RigidBody.velocity += celestialBody1.transform.right * Mathf.Sqrt((G * m2) / d);

                cb1RigidBody.velocity += celestialBody1.transform.right * Mathf.Sqrt(G * m2 * (2 / d - 1 / a));
            }
        }
    }
}
