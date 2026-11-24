using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

// FONCTIONNALITÉ : enlève le liseré blanc au bord du cerf-volant à peindre.
// Pourquoi il y en a un : dans l'image de la peinture, tout ce qui est hors de la forme reste blanc.
// Au bord de la forme, l'affichage mélange un peu les pixels voisins, donc ce blanc "déborde" sur le bord.
// Ce script recopie la couleur du bord de la forme sur quelques pixels autour, à chaque fois que la peinture change.
// Il ne touche pas au script de peinture : il passe juste après lui.
// À mettre sur l'objet "PieceDecoupee", avec le script de peinture.
[DefaultExecutionOrder(100)] // après le script de peinture, qui remet l'image à jour dans son LateUpdate
public class BordsPeinture : MonoBehaviour
{
    [SerializeField] private int marge = 4; // de combien de pixels on agrandit la couleur autour de la forme

    private Texture2D peinture;   // l'image de la peinture (créée par le script de peinture)
    private int[] cibles;         // les pixels autour de la forme...
    private int[] sources;        // ...et le pixel de la forme dont ils prennent la couleur
    private int derniereVersion = -1;

    private void LateUpdate()
    {
        if (peinture == null && !Preparer())
            return;

        // L'image n'a pas changé depuis la dernière fois : rien à refaire.
        if (peinture.updateCount == derniereVersion)
            return;

        NativeArray<Color32> pixels = peinture.GetPixelData<Color32>(0);
        for (int k = 0; k < cibles.Length; k++)
            pixels[cibles[k]] = pixels[sources[k]];
        peinture.Apply(false, false);
        derniereVersion = (int)peinture.updateCount;
    }

    // Trouve l'image de la peinture et calcule une fois pour toutes quels pixels recopier.
    private bool Preparer()
    {
        Material matiere = GetComponent<Renderer>().sharedMaterial;
        Texture2D image = (matiere.HasProperty("_BaseMap") ? matiere.GetTexture("_BaseMap") : matiere.mainTexture) as Texture2D;
        // Le script de peinture nomme son image "..._Paint" : tant qu'elle n'est pas là, on attend.
        if (image == null || !image.name.EndsWith("_Paint") || !image.isReadable || image.format != TextureFormat.RGBA32)
            return false;

        int largeur = image.width, hauteur = image.height;
        Mesh forme = GetComponent<MeshCollider>().sharedMesh; // le même maillage que le script de peinture
        int[] source = new int[largeur * hauteur];             // pour chaque pixel : de quel pixel de la forme il prend la couleur (-1 = aucun)
        for (int i = 0; i < source.Length; i++)
            source[i] = -1;

        // 1) Les pixels de la forme (même règle que le script de peinture : le centre du pixel est dans un triangle).
        Vector2[] uv = forme.uv;
        int[] triangles = forme.triangles;
        for (int t = 0; t < triangles.Length; t += 3)
        {
            Vector2 a = uv[triangles[t]], b = uv[triangles[t + 1]], c = uv[triangles[t + 2]];
            float aire = Croise(b - a, c - a);
            if (Mathf.Abs(aire) < 1e-8f)
                continue;
            int minX = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.x, b.x, c.x) * largeur), 0, largeur - 1);
            int maxX = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.x, b.x, c.x) * largeur), 0, largeur - 1);
            int minY = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.y, b.y, c.y) * hauteur), 0, hauteur - 1);
            int maxY = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.y, b.y, c.y) * hauteur), 0, hauteur - 1);
            for (int y = minY; y <= maxY; y++)
                for (int x = minX; x <= maxX; x++)
                {
                    Vector2 p = new Vector2((x + 0.5f) / largeur, (y + 0.5f) / hauteur);
                    float pa = Croise(b - p, c - p) / aire, pb = Croise(c - p, a - p) / aire;
                    if (pa >= -1e-6f && pb >= -1e-6f && 1f - pa - pb >= -1e-6f)
                        source[y * largeur + x] = y * largeur + x; // un pixel de la forme garde sa propre couleur
                }
        }

        // 2) On agrandit la forme d'un pixel à la fois : chaque pixel voisin prend la couleur d'un pixel de la forme à côté.
        List<int> listeCibles = new List<int>(), listeSources = new List<int>();
        for (int tour = 0; tour < marge; tour++)
        {
            int[] avant = (int[])source.Clone();
            for (int y = 0; y < hauteur; y++)
                for (int x = 0; x < largeur; x++)
                {
                    int i = y * largeur + x;
                    if (avant[i] >= 0)
                        continue;
                    for (int dy = -1; dy <= 1 && source[i] < 0; dy++)
                        for (int dx = -1; dx <= 1 && source[i] < 0; dx++)
                        {
                            int vx = x + dx, vy = y + dy;
                            if (vx >= 0 && vx < largeur && vy >= 0 && vy < hauteur && avant[vy * largeur + vx] >= 0)
                                source[i] = avant[vy * largeur + vx];
                        }
                    if (source[i] >= 0)
                    {
                        listeCibles.Add(i);
                        listeSources.Add(source[i]);
                    }
                }
        }

        cibles = listeCibles.ToArray();
        sources = listeSources.ToArray();
        peinture = image;
        return true;
    }

    private static float Croise(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
}
