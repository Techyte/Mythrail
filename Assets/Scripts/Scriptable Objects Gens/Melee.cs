using UnityEngine;

[CreateAssetMenu(fileName = "New Melee Weapon", menuName = "Custom/Melee", order = 2)]
public class Melee : ScriptableObject
{
    public new string name;
    public bool isThrowable;
    public float distance;
    public GameObject prefab;
    public int damage;
    [Space]
    public float staggerChance;
    [Space]
    [Header("If weapon is Melee")]
    public float coolDown;
    [Space]
    [Header("If weapon is Throwable")]
    public float throwDistance;
    public float collisionDamage;
    public float throwSpeed;
}
