using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class VisibleRaycaster : MonoBehaviour
{
    [Header("Paramètres du Rayon")]
    [Tooltip("Le Transform représentant les yeux ou la caméra du joueur.")]
    public Transform eyesTransform;
    public float rayDistance = 50f;
    public float rayWidth = 0.02f;
    public Color rayColor = Color.red;

    private LineRenderer lineRenderer;

    private void Start()
    {
        // Initialisation du LineRenderer
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2; // Le rayon a un début et une fin
        lineRenderer.startWidth = rayWidth;
        lineRenderer.endWidth = rayWidth;

        // Utilisation d'un matériau basique pour que la couleur s'affiche correctement
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = rayColor;
        lineRenderer.endColor = rayColor;
    }

    private void Update()
    {
        if (eyesTransform == null)
            return;

        // Le point de départ (0) est toujours la position des yeux
        lineRenderer.SetPosition(0, eyesTransform.position);

        RaycastHit hit;
        // Lance un rayon dans la direction vers laquelle les yeux regardent
        if (Physics.Raycast(eyesTransform.position, eyesTransform.forward, out hit, rayDistance))
        {
            // Le rayon touche un objet : le point d'arrivée (1) est le point d'impact
            lineRenderer.SetPosition(1, hit.point);

            // Indiquez ici ce qui doit se passer lors d'une interaction
            // par exemple : Debug.Log("Rayon a touché : " + hit.collider.name);
        }
        else
        {
            // Le rayon ne touche rien : il s'étend jusqu'à sa distance maximale en ligne droite
            Vector3 endPosition = eyesTransform.position + (eyesTransform.forward * rayDistance);
            lineRenderer.SetPosition(1, endPosition);
        }
    }
}