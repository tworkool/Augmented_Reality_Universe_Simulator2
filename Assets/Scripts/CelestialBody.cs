using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.ResourceManagement.AsyncOperations;

public class CelestialBody : MonoBehaviour
{
    [SerializeField]
    public float astronomicalUnitsFromSolarSystemCentre;
    [SerializeField]
    public GameObject orbit;
    [SerializeField]
    public GameObject collisionParticleSystem;

    void Start()
    {
        // TODO:
        // * Random rotation along own axis (around poles)
        // add random rotation to rigid body
        //rigidBodyComponent.rotation = Quaternion.Euler(
        //    Random.Range(-5f, 5f),
        //    Random.Range(-5f, 5f),
        //    Random.Range(-5f, 5f)
        //);

        //Debug.DrawLine(new Vector3(0, 0, 0), new Vector3(100, 100, 100), Color.red, 4f);
    }

    //private void OnDrawGizmos()
    //{
    //    Gizmos.DrawLine(new Vector3(0, 0, 0), new Vector3(100, 100, 100));
    //}

    // TODO:
    // * Remove GameObject
    // * Particle System Size based on mass of collider
    // * Shader for crater
    private void OnCollisionEnter(Collision collision)
    {
        ContactPoint collisionContactPoint = collision.GetContact(0);

        GameObject smallerBody;
        GameObject largerBody;
        if (collisionContactPoint.thisCollider.attachedRigidbody.mass > collisionContactPoint.otherCollider.attachedRigidbody.mass)
        {
            smallerBody = collisionContactPoint.otherCollider.gameObject;
            largerBody = collisionContactPoint.thisCollider.gameObject;
        }
        else
        {
            smallerBody = collisionContactPoint.thisCollider.gameObject;
            largerBody = collisionContactPoint.otherCollider.gameObject;
        }

        GameObject largerBodyParticleSystem = largerBody.GetComponent<CelestialBody>().collisionParticleSystem;
        if (largerBodyParticleSystem != null)
        {

            // instantiate particle system as child of celestial body and set rotation and position
            GameObject particleSystem = Instantiate(largerBodyParticleSystem, largerBody.transform);
            Vector3 pos = collisionContactPoint.point;
            Vector3 facingVector = collisionContactPoint.thisCollider.transform.position - collisionContactPoint.otherCollider.transform.position;
            particleSystem.transform.LookAt(facingVector);
            particleSystem.transform.position = new Vector3(pos.x, pos.y, pos.z);

            Debug.DrawLine(pos, pos + (facingVector.normalized * 20), Color.red, 10f);

            // scale particle system based on size
            //float smallerBodySize = (smallerBody.transform.localScale.x * smallerBody.transform.localScale.y * smallerBody.transform.localScale.z) / 3;
            //if (smallerBodySize < 0.1) smallerBodySize = 0.1f;
            //particleSystem.transform.localScale = new Vector3(smallerBodySize, smallerBodySize, smallerBodySize);

        }

        Destroy(smallerBody);
        // remove body from universe celestial bodies list
        //GetComponent<Universe>().celestialBodies.Remove(sourceBody);
    }
}
