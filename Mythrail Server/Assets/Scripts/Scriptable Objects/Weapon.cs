using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Mythrail/New Weapon")]
public class Weapon : ScriptableObject
{
    public new string name;
    public int damage;
    public float distance;
    public float fireRate;
    public float reloadRate;
    public float swapInRate;
    public float bloom;
    public float recoil;
    public float kickBack;
}
