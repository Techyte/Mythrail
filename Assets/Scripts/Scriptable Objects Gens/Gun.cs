using UnityEngine;

[CreateAssetMenu(fileName = "New Gun", menuName = "Custom/Gun", order = 1)]
public class Gun : ScriptableObject
{
    public new string name;
    public float distance;
    public GameObject prefab;
    public int damage;
    public float reloadTime;
    public float fireRate;
    public float bloom;
    public float recoil;
    public float kickBack;
    public float aimSpeed;
}
