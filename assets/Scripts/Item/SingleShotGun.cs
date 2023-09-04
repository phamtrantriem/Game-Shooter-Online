using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class SingleShotGun : Gun
{
    [SerializeField] Camera cam;
    PhotonView PV;
    [SerializeField] GameObject bulletPrefab;
    [SerializeField] Transform spawnPosition;

    private void Awake()
    {
        PV = GetComponent<PhotonView>();   
    }
    public override void Use()
    {
        Shoot();
    }

    void Shoot()
    {
        //New ray comes straight out of the center of screen, mean UV
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        ray.origin = cam.transform.position;

        if(Physics.Raycast(ray, out RaycastHit hit))
        {
            hit.collider.gameObject.GetComponent<IDamageable>()?.TakeDamage(((GunInfo)itemInfo).damage);
            PV.RPC("RPC_Shoot", RpcTarget.All, hit.point, hit.normal); 
        }
    }

    [PunRPC]
    void RPC_Shoot(Vector3 hitPosition, Vector3 hitNormal)
    {
        Collider[] colliders = Physics.OverlapSphere(hitPosition, 0.3f);
        if(colliders.Length != 0)
        {
            GameObject bullet = GameObject.Instantiate(bulletPrefab, spawnPosition.position, Quaternion.identity);
            BulletScript bulletController = bullet.GetComponent<BulletScript>();

            bulletController.target = hitPosition;


            GameObject bulletImpactObject = Instantiate(bulletImpactPrefab, hitPosition + hitNormal * 0.001f, Quaternion.LookRotation(hitNormal, Vector3.up) * bulletImpactPrefab.transform.rotation);
            Destroy(bulletImpactObject, 3f);
            bulletImpactObject.transform.SetParent(colliders[0].transform);
        }
    }
}
