using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class BulletScript : MonoBehaviour
{

    [SerializeField]
    private float speed = 50f;

    [SerializeField]
    private float timeToDestroy = 3f;
    public Vector3 target { get; set; }
    public bool hit { get; set; }

    private void OnEnable()
    {
        Destroy(gameObject, timeToDestroy);
    }
    Rigidbody m_Rigidbody;
    private void Start()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        Physics.IgnoreLayerCollision(6, 7);
    }
    void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position, target , speed * Time.deltaTime);
        if (!hit && Vector3.Distance(transform.position, target) < 0.01f)
        {
            Destroy(gameObject);
        }
    }


    private void OnCollisionEnter(Collision other)
    {
/*        ContactPoint contact = other.GetContact(0);
        GameObject.Instantiate(fireImpact, contact.point + contact.normal * 0.001f, Quaternion.LookRotation(contact.normal));
        GameObject.Instantiate(bulletDecal, contact.point + contact.normal * 0.001f, Quaternion.LookRotation(contact.normal));*/

        Destroy(gameObject);
    }
}
