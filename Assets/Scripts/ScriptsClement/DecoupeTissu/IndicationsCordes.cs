using TMPro;
using UnityEngine;

// FONCTIONNALITÉ : montrer comment accrocher les cordes au cerf-volant (dernière étape).
//  - "Attache" flotte devant le cerf-volant, avec une flèche dessous vers chacun des deux ronds où accrocher.
//  - "Gauche" et "Droite" flottent au-dessus du bout de chaque corde, avec une flèche dessous qui monte et descend en le montrant.
//  - Chaque indication disparaît dès que sa corde est accrochée (son bout libre disparaît).
// À mettre sur "PieceDecoupee", à côté de PointAccroche.
[RequireComponent(typeof(PointAccroche))]
public class IndicationsCordes : MonoBehaviour
{
    [SerializeField] private Color couleur = new Color(1f, 0.85f, 0.2f); // jaune vif, comme les ronds

    private PointAccroche accroche;                             // sait où sont les deux ronds
    private Transform[] bouts;                                  // les bouts libres des cordes : gauche, puis droite
    private Transform texteAttache;
    private readonly Transform[] flechesAttache = new Transform[2]; // vers le rond gauche, puis le droit
    private readonly Transform[] textesCordes = new Transform[2];   // "Gauche", "Droite"
    private readonly Transform[] flechesCordes = new Transform[2];  // sous "Gauche", sous "Droite"
    private Material matiere;

    private void Start()
    {
        accroche = GetComponent<PointAccroche>();
        bouts = accroche.BoutsDesCordes;
        matiere = new Material(Shader.Find("Sprites/Default")) { color = couleur }; // couleur unie, visible des deux côtés
        Mesh fleche = ReservoirBombe.MaillageFleche();

        texteAttache = Texte("Attache");
        for (int i = 0; i < 2; i++)
        {
            flechesAttache[i] = Fleche(fleche);
            textesCordes[i] = Texte(i == 0 ? "Gauche" : "Droite");
            flechesCordes[i] = Fleche(fleche);
        }
    }

    // LateUpdate : après que tout a bougé (les cordes qu'on tient, le cerf-volant) : les indications suivent sans retard.
    private void LateUpdate()
    {
        if (Camera.main == null)
            return;
        Vector3 oeil = Camera.main.transform.position;

        // Les deux ronds, et la direction du joueur (on avance les indications un peu vers lui : elles ne rentrent pas dans le cerf-volant).
        Vector3 rondGauche = transform.TransformPoint(accroche.PositionLocale(0));
        Vector3 rondDroit = transform.TransformPoint(accroche.PositionLocale(1));
        Vector3 milieu = (rondGauche + rondDroit) / 2f;
        Vector3 versJoueur = (oeil - milieu).normalized;
        Vector3 placeAttache = milieu + Vector3.up * 0.14f + versJoueur * 0.04f;

        float bouge = (Mathf.Sin(Time.time * 5f) + 1f) * 0.015f; // de 0 à 3 cm, un peu moins d'un aller-retour par seconde
        bool resteUneCorde = false;

        for (int i = 0; i < 2; i++)
        {
            bool libre = bouts[i] != null && bouts[i].gameObject.activeInHierarchy; // pas encore accrochée
            resteUneCorde |= libre;
            textesCordes[i].gameObject.SetActive(libre);
            flechesCordes[i].gameObject.SetActive(libre);
            flechesAttache[i].gameObject.SetActive(libre);
            if (!libre)
                continue;

            // "Gauche" / "Droite" au-dessus du bout de la corde, et la flèche qui pointe vers lui en montant et descendant.
            Vector3 bout = bouts[i].position + Vector3.up * bouge;
            Poser(textesCordes[i], bout + Vector3.up * 0.10f, Vector3.up, oeil);
            Poser(flechesCordes[i], bout + Vector3.up * 0.065f, Vector3.down, oeil); // pointe 2,5 cm au-dessus du bout

            // La flèche de "Attache" vers son rond : elle s'arrête juste avant lui.
            Vector3 rond = (i == 0 ? rondGauche : rondDroit) + versJoueur * 0.04f;
            Vector3 direction = (rond - placeAttache).normalized;
            Poser(flechesAttache[i], rond - direction * 0.075f, direction, oeil);
        }

        texteAttache.gameObject.SetActive(resteUneCorde);
        Poser(texteAttache, placeAttache, Vector3.up, oeil);
    }

    // Place un objet plat face au joueur, son "haut" dans la direction donnée (pour une flèche : là où elle pointe).
    private static void Poser(Transform objet, Vector3 place, Vector3 haut, Vector3 oeil)
    {
        Vector3 face = Vector3.ProjectOnPlane(place - oeil, haut);
        if (face.sqrMagnitude < 1e-6f)
            face = Vector3.forward;
        objet.SetPositionAndRotation(place, Quaternion.LookRotation(face, haut));
    }

    private Transform Texte(string mot)
    {
        TextMeshPro texte = new GameObject("Indication " + mot).AddComponent<TextMeshPro>();
        texte.rectTransform.sizeDelta = new Vector2(0.3f, 0.06f);
        texte.text = mot;
        texte.fontSize = 0.35f; // environ 3,5 cm de haut
        texte.fontStyle = FontStyles.Bold;
        texte.alignment = TextAlignmentOptions.Center;
        texte.color = couleur;
        texte.gameObject.SetActive(false);
        return texte.transform;
    }

    private Transform Fleche(Mesh forme)
    {
        GameObject fleche = new GameObject("Indication Fleche");
        fleche.AddComponent<MeshFilter>().sharedMesh = forme;
        fleche.AddComponent<MeshRenderer>().sharedMaterial = matiere;
        fleche.SetActive(false);
        return fleche.transform;
    }

    // Le cerf-volant est caché : les indications (des objets à part) se cachent avec lui.
    private void OnDisable()
    {
        foreach (Transform objet in TousLesObjets())
            if (objet != null) // déjà détruit (ex. en quittant la scène) : rien à faire
                objet.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        foreach (Transform objet in TousLesObjets())
            if (objet != null)
                Destroy(objet.gameObject);
        if (matiere != null)
            Destroy(matiere);
    }

    private Transform[] TousLesObjets()
    {
        return new[] { texteAttache, flechesAttache[0], flechesAttache[1], textesCordes[0], textesCordes[1], flechesCordes[0], flechesCordes[1] };
    }
}