using UnityEngine;

// Spawns floating combat-text popups (posture breaks, parries, guard breaks,
// etc) above this GameObject. Drop a DamagePopup prefab into popupPrefab.
// Attach this to the player and/or any enemy that should show popups.
public class DamagePopupSpawner : MonoBehaviour
{
    [SerializeField] private DamagePopup popupPrefab;
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 1.2f, 0f);

    public void Spawn(string text, Color color)
    {
        if (popupPrefab == null)
        {
            return;
        }

        DamagePopup popup = Instantiate(popupPrefab, transform.position + spawnOffset, Quaternion.identity);
        popup.Show(text, color);
    }
}
