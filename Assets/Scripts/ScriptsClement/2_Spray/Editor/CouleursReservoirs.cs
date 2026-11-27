using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// OUTIL (dans l'éditeur seulement) : menu "Clement > Peinture : couleurs des réservoirs".
// Dans la scène ouverte, les réservoirs de peinture du pistolet ("MHS_Reservoir ...") prennent les 6 couleurs de la palette.
// S'il en manque, il duplique un réservoir existant (comme Ctrl+D : même prefab, mêmes réglages), et ajoute la copie
// partout où les autres réservoirs sont listés (le pistolet, les transitions, FinPeinture, le respawn du sol...).
// Ils sont rangés en ligne sur la table, à la place des anciens. On peut le relancer sans risque.
// Tout s'annule avec Ctrl+Z. Après : Ctrl+S pour enregistrer la scène.
// (Les scripts de l'équipe (PaintReservoir, ReservoirSocket...) ne sont pas modifiés.)
public static class CouleursReservoirs
{
    // La palette, dans l'ordre où les réservoirs sont posés sur la table (de gauche à droite).
    private static readonly string[] noms = { "brun", "rouge", "orange", "vert", "bleu", "violet" };
    private static readonly string[] couleurs = { "#956257", "#DD6969", "#F1B06A", "#96AD75", "#7899CE", "#8F5395" };

    private const float ecart = 0.13f; // distance entre deux réservoirs (13 cm, comme avant)

    [MenuItem("Clement/Peinture : couleurs des réservoirs")]
    private static void Lancer()
    {
        // 1) Les réservoirs déjà dans la scène, de gauche à droite.
        Scene scene = SceneManager.GetActiveScene();
        List<PaintReservoir> reservoirs = new List<PaintReservoir>();
        foreach (GameObject racine in scene.GetRootGameObjects())
            if (racine.name.StartsWith("MHS_Reservoir") && racine.TryGetComponent(out PaintReservoir reservoir))
                reservoirs.Add(reservoir);
        if (reservoirs.Count == 0)
        {
            EditorUtility.DisplayDialog("Réservoirs", "Pas de \"MHS_Reservoir ...\" dans la scène ouverte.", "OK");
            return;
        }
        reservoirs.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));

        Undo.SetCurrentGroupName("Couleurs des réservoirs");
        int groupe = Undo.GetCurrentGroup();

        // Le milieu de la rangée actuelle : la nouvelle rangée sera centrée au même endroit.
        float milieu = 0f;
        foreach (PaintReservoir reservoir in reservoirs)
            milieu += reservoir.transform.position.x;
        milieu /= reservoirs.Count;

        // 2) Il en manque : on duplique le premier (comme Ctrl+D) et on ajoute la copie dans les mêmes listes que lui.
        PaintReservoir modele = reservoirs[0];
        while (reservoirs.Count < couleurs.Length)
        {
            Selection.activeGameObject = modele.gameObject;
            Unsupported.DuplicateGameObjectsUsingPasteboard();
            PaintReservoir copie = Selection.activeGameObject.GetComponent<PaintReservoir>();
            AjouterAuxListes(scene, modele, copie);
            reservoirs.Add(copie);
        }

        // 3) Chaque réservoir : son nom, sa couleur, sa place dans la rangée.
        for (int i = 0; i < couleurs.Length; i++)
        {
            PaintReservoir reservoir = reservoirs[i];
            ColorUtility.TryParseHtmlString(couleurs[i], out Color couleur);

            Undo.RecordObject(reservoir.gameObject, "Nom du réservoir");
            reservoir.gameObject.name = "MHS_Reservoir " + noms[i];

            SerializedObject champs = new SerializedObject(reservoir);
            champs.FindProperty("paintColor").colorValue = couleur;
            champs.ApplyModifiedProperties();

            Undo.RecordObject(reservoir.transform, "Place du réservoir");
            Vector3 place = reservoir.transform.position;
            place.x = milieu + (i - (couleurs.Length - 1) / 2f) * ecart;
            reservoir.transform.position = place;
        }

        Undo.CollapseUndoOperations(groupe);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = reservoirs[0].gameObject;
        Debug.Log(couleurs.Length + " réservoirs aux couleurs de la palette. Fais Ctrl+S pour enregistrer la scène.");
    }

    // Partout où un script de la scène a une liste qui contient le modèle (l'objet ou son PaintReservoir),
    // on ajoute la copie à la fin de la liste.
    private static void AjouterAuxListes(Scene scene, PaintReservoir modele, PaintReservoir copie)
    {
        foreach (MonoBehaviour script in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (script == null || script.gameObject.scene != scene || script.gameObject == copie.gameObject)
                continue;

            // On note d'abord les listes (changer leur taille pendant qu'on les parcourt mélangerait le parcours).
            SerializedObject champs = new SerializedObject(script);
            List<string> listes = new List<string>();
            SerializedProperty champ = champs.GetIterator();
            while (champ.Next(true))
                // (Unity montre chaque liste deux fois : "bombes" et "bombes.Array" ; on ne garde que la première.)
                if (champ.isArray && champ.propertyType == SerializedPropertyType.Generic && champ.arrayElementType.StartsWith("PPtr")
                    && !champ.propertyPath.EndsWith(".Array"))
                    listes.Add(champ.propertyPath);

            foreach (string chemin in listes)
            {
                SerializedProperty liste = champs.FindProperty(chemin);
                Object aAjouter = null;
                bool dejaLa = false;
                for (int j = 0; j < liste.arraySize; j++)
                {
                    Object element = liste.GetArrayElementAtIndex(j).objectReferenceValue;
                    if (element == modele.gameObject)
                        aAjouter = copie.gameObject;
                    else if (element == modele)
                        aAjouter = copie;
                    else if (element == copie.gameObject || element == copie)
                        dejaLa = true;
                }
                if (aAjouter == null || dejaLa)
                    continue;
                liste.arraySize++;
                liste.GetArrayElementAtIndex(liste.arraySize - 1).objectReferenceValue = aAjouter;
            }
            champs.ApplyModifiedProperties();
        }
    }
}
