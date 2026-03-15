using UnityEngine;
using System.Collections.Generic;

public class PlayerInventory : MonoBehaviour
{
    // Stores key IDs player is carrying
    // Uses List so any number of keys can be held
    private List<string> keys = new List<string>();

    // Called by KeyPickup when player picks up key
    public void AddKey(string keyID)
    {
        if (!keys.Contains(keyID))
        {
            keys.Add(keyID);
            Debug.Log("Inventory: Added key — " + keyID);
        }
    }

    // Called by Door to check if player has correct key
    public bool HasKey(string keyID)
    {
        return keys.Contains(keyID);
    }

    // Called by Door after opening — removes key from inventory
    // Delete call in Door.cs if you want key to be reusable
    public void RemoveKey(string keyID)
    {
        if (keys.Contains(keyID))
        {
            keys.Remove(keyID);
            Debug.Log("Inventory: Used key — " + keyID);
        }
    }

    // Debug helper — prints all keys player holds
    public void PrintInventory()
    {
        Debug.Log("Keys held: " + string.Join(", ", keys));
    }
}