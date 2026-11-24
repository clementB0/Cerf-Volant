using UnityEngine;

// FONCTIONNALITÉ : pendant le vol, le cerf-volant bat des ailes, de haut en bas (perpendiculaire au tissu :
// posé à plat, ça bougerait vers le haut et vers le bas).
// Chaque aile pivote en entier autour du bord du corps, comme une charnière (le corps, lui, ne bouge pas).
// La largeur du corps vient de la forme (Papillon, Raie, Dragon : champ "Corps", mesurée sur les modèles 3D).
// Il commence en douceur au décollage et s'arrête quand il ne vole plus ou quand il tombe (touché par un oiseau).
// À mettre sur l'objet "PieceDecoupee", avec VolCerfVolant et GonflementCerfVolant.
// Il passe juste après GonflementCerfVolant (qui redessine le tissu gonflé à chaque image) et ajoute le battement par-dessus.
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(GonflementCerfVolant))]
public class BattementAiles : MonoBehaviour
{
    [SerializeField] private float angleMax = 25f;              // de combien les ailes se relèvent et s'abaissent, en degrés
    [SerializeField] private float battementsParSeconde = 1.2f; // un battement lent
    [SerializeField] private float tempsPourDemarrer = 1f;      // il met 1 seconde à commencer (ou à s'arrêter)

    private VolCerfVolant vol;
    private MeshFilter filtre;
    private float force;           // de 0 (pas de battement) à 1 (battement complet)
    private float milieu;          // le milieu du corps, sur l'axe des ailes (x)
    private float bordDuCorps;     // la distance du milieu au bord du corps : les ailes commencent là

    private void Start()
    {
        vol = GetComponent<VolCerfVolant>();
        filtre = GetComponent<MeshFilter>();

        // La forme de ce cerf-volant : elle est sur le Tissu, à côté de la pièce (dans le même atelier).
        PatronPointilles patron = transform.parent.GetComponentInChildren<PatronPointilles>(true);
        float min = float.MaxValue, max = float.MinValue;
        foreach (Vector2 coin in patron.Forme.Coins)
        {
            min = Mathf.Min(min, coin.x);
            max = Mathf.Max(max, coin.x);
        }
        milieu = (min + max) / 2f;
        bordDuCorps = patron.Forme.Corps * (max - min) / 2f;
    }

    // LateUpdate, après GonflementCerfVolant (ordre 100) : le tissu vient d'être redessiné, on ajoute le battement.
    private void LateUpdate()
    {
        // Il bat des ailes en vol, mais pas quand il tombe (touché par un oiseau) : les ailes se reposent.
        force = Mathf.MoveTowards(force, vol.EnVol && !vol.Tombe ? 1f : 0f, Time.deltaTime / tempsPourDemarrer);
        Mesh maillage = filtre.sharedMesh;

        // Seulement sur le tissu de GonflementCerfVolant : il est redessiné à chaque image, donc le battement ne s'additionne pas.
        // (Avant le premier vol, c'est le maillage de la pièce, qui sert aussi à la peinture : on n'y touche pas.)
        if (force <= 0f || maillage == null || maillage.name != "CerfVolant_Gonfle")
            return;

        // L'angle des ailes en ce moment : il monte et descend (en douceur au début et à la fin du vol).
        float angle = Mathf.Sin(Time.time * battementsParSeconde * Mathf.PI * 2f) * angleMax * force * Mathf.Deg2Rad;
        float cos = Mathf.Cos(angle), sin = Mathf.Sin(angle);

        Vector3[] sommets = maillage.vertices;
        for (int i = 0; i < sommets.Length; i++)
        {
            float x = sommets[i].x - milieu;
            float surAile = Mathf.Abs(x) - bordDuCorps; // la distance depuis la charnière (négatif = dans le corps)
            if (surAile <= 0f)
                continue; // le corps ne bouge pas

            // L'aile tourne autour de la charnière : elle garde sa longueur, son bout monte (z) et se rapproche un peu (x).
            float cote = Mathf.Sign(x); // aile gauche (-1) ou droite (+1)
            sommets[i].x = milieu + cote * (bordDuCorps + surAile * cos);
            sommets[i].z += surAile * sin;
        }
        maillage.vertices = sommets;
        maillage.RecalculateNormals();
    }
}
