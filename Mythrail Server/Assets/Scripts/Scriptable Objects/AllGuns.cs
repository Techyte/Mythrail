using System.Collections.Generic;
using UnityEngine;

public class AllGuns : MonoBehaviour
{
    public static List<Weapon> weapons;
    [SerializeField] private List<Weapon> Weapons = new List<Weapon>();

    private void Start()
    {
        weapons = Weapons;
    }
}
