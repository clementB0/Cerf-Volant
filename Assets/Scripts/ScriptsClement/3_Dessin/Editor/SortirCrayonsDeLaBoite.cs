using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// OUTIL (dans l'éditeur seulement) : menu "Clement > Crayons : poser debout sur la table".
// Dans la scène ouverte, il met les crayons debout, la mine en haut, côte à côte sur la table, aux couleurs de la palette.
// - S'ils sont encore dans "Boite_Crayons_Dessin" : il les sort de la boîte et les range dans un nouvel objet
//   "Crayons_Dessin", qui prend la place de la boîte dans les étapes (il est donc caché jusqu'au dessin, comme la boîte avant).
//   Il les ajoute aussi au RespawnZone (le sol) : s'ils tombent par terre, ils reviennent sur la table. La boîte est supprimée.
// - S'ils sont déjà dans "Crayons_Dessin" : il les remet juste en place (on peut le relancer autant qu'on veut).
// Tout s'annule avec Ctrl+Z. Après : Ctrl+S pour enregistrer la scène.
// (Les scripts des crayons, de Lukas, et celui du respawn ne sont pas modifiés.)
public static class SortirCrayonsDeLaBoite
{
    private const float ecart = 0.035f;    // distance entre le milieu de deux crayons (3,5 cm)
    private const float audessus = 0.002f; // les crayons sont posés 2 mm au-dessus de la table (pour ne pas rentrer dedans)

    // La palette des crayons, de gauche à droite : Jasmine, Tea Green, Steel Blue, Soft Blossom, Vintage Lavender, Dusty Grape.
    private static readonly string[] couleurs = { "#FED37E", "#BED69B", "#84A9C0", "#E6ADBA", "#7E67A3", "#4D4A8A" };

    [MenuItem("Clement/Crayons : poser debout sur la table")]
    private static void Lancer()
    {
        Scene scene = SceneManager.GetActiveScene();
        GameObject boite = TrouverALaRacine(scene, "Boite_Crayons_Dessin");
        GameObject rangee = TrouverALaRacine(scene, "Crayons_Dessin");
        if (boite == null && rangee == null)
        {
            EditorUtility.DisplayDialog("Crayons", "Ni \"Boite_Crayons_Dessin\" ni \"Crayons_Dessin\" dans la scène ouverte.", "OK");
            return;
        }

        Undo.SetCurrentGroupName("Poser les crayons debout sur la table");
        int groupe = Undo.GetCurrentGroup();

        Pen[] crayons;
        float hauteurTable = float.MaxValue;
        if (rangee != null)
        {
            // Déjà sortis : la table est juste sous le crayon le plus bas.
            crayons = rangee.GetComponentsInChildren<Pen>(true);
            foreach (Pen crayon in crayons)
                foreach (MeshFilter morceau in crayon.GetComponentsInChildren<MeshFilter>(true))
                    hauteurTable = Mathf.Min(hauteurTable, PointLePlusBas(morceau));
            hauteurTable -= audessus;
        }
        else
        {
            crayons = boite.GetComponentsInChildren<Pen>(true);

            // La hauteur de la table = le dessous de la boîte (elle est posée dessus).
            foreach (MeshFilter forme in boite.GetComponentsInChildren<MeshFilter>(true))
                if (forme.GetComponentInParent<Pen>(true) == null)
                    hauteurTable = Mathf.Min(hauteurTable, PointLePlusBas(forme));
            rangee = SortirDeLaBoite(scene, boite, crayons);
        }
        if (hauteurTable == float.MaxValue)
            hauteurTable = rangee.transform.position.y;

        // La rangée va "à droite" de l'objet Crayons_Dessin (posé là où était la boîte).
        Vector3 droite = Vector3.ProjectOnPlane(rangee.transform.right, Vector3.up).normalized;

        for (int i = 0; i < crayons.Length; i++)
        {
            PoserDebout(crayons[i], rangee.transform.position + droite * ((i - (crayons.Length - 1) / 2f) * ecart), hauteurTable);

            // Sa couleur (celle du trait et du repère), prise dans la palette, dans l'ordre de la rangée.
            ColorUtility.TryParseHtmlString(couleurs[i % couleurs.Length], out Color couleur);
            SerializedObject champs = new SerializedObject(crayons[i]);
            champs.FindProperty("couleurMine").colorValue = couleur;
            champs.ApplyModifiedProperties();
        }

        Undo.CollapseUndoOperations(groupe);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = rangee;
        Debug.Log(crayons.Length + " crayons posés debout sur la table, mine en haut. Fais Ctrl+S pour enregistrer la scène.");
    }

    // Un crayon : debout, la mine en haut, son milieu à "place", son pied sur la table.
    private static void PoserDebout(Pen pen, Vector3 place, float hauteurTable)
    {
        Transform crayon = pen.transform;
        Undo.RecordObject(crayon, "Poser le crayon");

        // Le milieu du crayon (sa forme), et la direction du milieu vers la mine ("Pointe").
        MeshFilter forme = crayon.GetComponent<MeshFilter>();
        Vector3 milieu = forme != null && forme.sharedMesh != null
            ? crayon.TransformPoint(forme.sharedMesh.bounds.center)
            : crayon.position;
        Transform pointe = crayon.Find("Pointe");
        Vector3 versLaMine = pointe != null ? pointe.position - milieu : crayon.up;

        // On le tourne autour de son milieu pour que la mine pointe tout droit vers le haut.
        Quaternion tour = Quaternion.FromToRotation(versLaMine, Vector3.up);
        crayon.SetPositionAndRotation(milieu + tour * (crayon.position - milieu), tour * crayon.rotation);

        // Son milieu va à sa place dans la rangée (la hauteur, on la règle juste après).
        Vector3 decalage = place - milieu;
        decalage.y = 0f;
        crayon.position += decalage;

        // Posé sur la table : son point le plus bas (repère de couleur compris) touche la table.
        float bas = float.MaxValue;
        foreach (MeshFilter morceau in crayon.GetComponentsInChildren<MeshFilter>(true))
            bas = Mathf.Min(bas, PointLePlusBas(morceau));
        if (bas != float.MaxValue)
            crayon.position += Vector3.up * (hauteurTable - bas + audessus);
    }

    // Sort les crayons de la boîte, les range dans "Crayons_Dessin", branche cet objet à la place de la boîte,
    // ajoute les crayons au respawn du sol, et supprime la boîte. Renvoie "Crayons_Dessin".
    private static GameObject SortirDeLaBoite(Scene scene, GameObject boite, Pen[] crayons)
    {
        // Le nouvel objet, à la place de la boîte, tourné comme elle (caché si la boîte l'était).
        Vector3 devant = Vector3.ProjectOnPlane(boite.transform.forward, Vector3.up).normalized;
        GameObject rangee = new GameObject("Crayons_Dessin");
        Undo.RegisterCreatedObjectUndo(rangee, "Crayons_Dessin");
        rangee.transform.SetPositionAndRotation(boite.transform.position, Quaternion.LookRotation(devant));
        rangee.SetActive(boite.activeSelf);

        foreach (Pen pen in crayons)
        {
            Transform crayon = pen.transform;

            // On ne peut pas sortir un objet d'un prefab : on "défait" le prefab de la boîte, niveau par niveau,
            // jusqu'à ce que le crayon soit libre (le crayon garde son propre prefab s'il en a un).
            while (crayon.parent != null && PrefabUtility.IsPartOfPrefabInstance(crayon.parent.gameObject))
                PrefabUtility.UnpackPrefabInstance(PrefabUtility.GetOutermostPrefabInstanceRoot(crayon.parent.gameObject),
                    PrefabUnpackMode.OutermostRoot, InteractionMode.UserAction);

            Undo.SetTransformParent(crayon, rangee.transform, "Sortir le crayon");
        }

        // Partout où les scripts de la scène parlaient de la boîte (ex. FinPeinture, ChoixMotifAvantDessin,
        // les transitions), ils parlent maintenant de "Crayons_Dessin" : il apparaît au même moment.
        foreach (MonoBehaviour script in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (script == null || script.gameObject.scene != scene)
                continue;
            SerializedObject champs = new SerializedObject(script);
            SerializedProperty champ = champs.GetIterator();
            while (champ.Next(true))
            {
                if (champ.propertyType != SerializedPropertyType.ObjectReference)
                    continue;
                if (champ.objectReferenceValue == boite)
                    champ.objectReferenceValue = rangee;
                else if (champ.objectReferenceValue == boite.transform)
                    champ.objectReferenceValue = rangee.transform;
            }
            champs.ApplyModifiedProperties();
        }

        // Les crayons sont ajoutés au respawn du sol (s'ils n'y sont pas déjà).
        foreach (RespawnZone sol in Object.FindObjectsByType<RespawnZone>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (sol.gameObject.scene != scene)
                continue;
            SerializedObject champs = new SerializedObject(sol);
            SerializedProperty liste = champs.FindProperty("objetsAutorises");
            foreach (Pen crayon in crayons)
            {
                bool dejaLa = false;
                for (int j = 0; j < liste.arraySize; j++)
                    if (liste.GetArrayElementAtIndex(j).objectReferenceValue == crayon.gameObject)
                        dejaLa = true;
                if (dejaLa)
                    continue;
                liste.arraySize++;
                liste.GetArrayElementAtIndex(liste.arraySize - 1).objectReferenceValue = crayon.gameObject;
            }
            champs.ApplyModifiedProperties();
        }

        // La boîte (vide) est supprimée.
        Undo.DestroyObjectImmediate(boite);
        return rangee;
    }

    private static GameObject TrouverALaRacine(Scene scene, string nom)
    {
        foreach (GameObject racine in scene.GetRootGameObjects())
            if (racine.name == nom)
                return racine;
        return null;
    }

    // Le point le plus bas d'un objet dans le monde (on regarde les 8 coins de sa forme).
    private static float PointLePlusBas(MeshFilter forme)
    {
        if (forme.sharedMesh == null)
            return float.MaxValue;
        Bounds b = forme.sharedMesh.bounds;
        float bas = float.MaxValue;
        for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 coin = b.center + Vector3.Scale(b.extents, new Vector3(x, y, z));
                    bas = Mathf.Min(bas, forme.transform.TransformPoint(coin).y);
                }
        return bas;
    }
}
