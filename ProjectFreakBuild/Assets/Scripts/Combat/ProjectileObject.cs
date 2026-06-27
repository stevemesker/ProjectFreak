using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileObject : MonoBehaviour
{
    public int damageAmount = 1;
    [SerializeField] private DamageType.Type dType;
    public List<ElementType.Element> element;
    public GameObject instigator;
    public float speed;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {

        print("Doot");

        if (other.gameObject == instigator)
        {
            print("Hit the unit " + other.gameObject.name + " that spawned " + gameObject.name);
            return;
        }

        IDamagable damagable = other.GetComponent<IDamagable>();
        if (damagable == null) { print("Detecting hit " + other.gameObject.name); Destroy(gameObject); return; }

        //spawn hit effects here
        print("Detecting hit " + other.gameObject.name);
        Destroy(gameObject);
        
    }
}
