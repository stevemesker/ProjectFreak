using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileObject : MonoBehaviour
{
    public float _Speed;
    public DamagePackage _Damage;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position += transform.forward * _Speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {

        if (other.gameObject == _Damage._Source || other.isTrigger)
        {
            return;
        }

        IDamagable damagable = other.GetComponent<IDamagable>();
        if (damagable == null) 
        { 
            Destroy(gameObject); 
            return; 
        }

        //spawn hit effects here
        damagable.TakeDamage(_Damage);
        Destroy(gameObject);
        
    }

}
