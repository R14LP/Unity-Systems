using UnityEngine;
using TMPro;

public class ItemInteraction : MonoBehaviour
{
    public Transform holdPoint;
    public float pickupRange = 3f;
    public float rotateSpeed = 5f;
    public float throwForce = 10f;
    private int originalLayer;
    public Material highlightMaterial;
    public TMP_Text itemNameText;
    public PlayerCam playerCam;
    public Transform playerCamera;
    private GameObject targetItem;
    private GameObject heldItem;
    private Material originalMaterial;
    private Renderer targetRenderer;
    private bool rotatingItem;

    void Update()
    {
        HandleRaycast();

        if (heldItem == null && targetItem != null && Input.GetKeyDown(KeyCode.E))
        {
            PickUp(targetItem);
        }
        
        if (heldItem != null)
        {
            HandleRotation();

            if(Input.GetMouseButtonDown(0))
            {
                Throw();
            }
        }
    }

    void HandleRaycast()
    {
        if (heldItem != null)
        {
            ClearTarget();
            return;
        }

        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, pickupRange) && hit.collider.CompareTag("Item"))
        {
            if (targetItem != hit.collider.gameObject)
            {
                ClearTarget();
                targetItem = hit.collider.gameObject;
                targetRenderer = targetItem.GetComponent<Renderer>();
                originalMaterial = targetRenderer.material;
                targetRenderer.material = highlightMaterial;
                itemNameText.text = targetItem.name;
                itemNameText.gameObject.SetActive(true);
            }
        }
        else
        {
            ClearTarget();
        }
    }

    void ClearTarget()
    {
        if (targetItem != null)
        {
            targetRenderer.material = originalMaterial;
            itemNameText.gameObject.SetActive(false);
            targetItem = null;
        }
    }

    void PickUp(GameObject item)
    {
        targetRenderer.material = originalMaterial;
        itemNameText.gameObject.SetActive(false);
        targetItem = null;

        heldItem = item;
        originalLayer = heldItem.layer;
        heldItem.GetComponent<Collider>().enabled = false;
        heldItem.GetComponent<Rigidbody>().isKinematic = true;

        heldItem.layer = LayerMask.NameToLayer("HeldItem");
        heldItem.transform.SetParent(holdPoint);
        heldItem.transform.localPosition = Vector3.zero;
    }

    void HandleRotation()
    {
        if (Input.GetMouseButtonDown(1))
        {
            rotatingItem = true;
            playerCam.enabled = false;
        }

        if (Input.GetMouseButtonUp(1))
        {
            rotatingItem = false;
            playerCam.enabled = true;
        }

        if (rotatingItem)
        {
            float mouseX = Input.GetAxis("Mouse X") * rotateSpeed;
            float mouseY = Input.GetAxis("Mouse Y") * rotateSpeed;

            heldItem.transform.Rotate(Vector3.up, -mouseX, Space.World);
            heldItem.transform.Rotate(Vector3.right, mouseY, Space.World);
        }
    }

    void Throw()
    {
        Rigidbody rb = heldItem.GetComponent<Rigidbody>();
        Collider col = heldItem.GetComponent<Collider>();

        heldItem.transform.SetParent(null);

        float itemRadius = col.bounds.extents.magnitude;
        float maxDistance = 1.5f;

        Vector3 safePosition;
        bool blocked;

        if (Physics.SphereCast(playerCamera.position, itemRadius, playerCamera.forward, out RaycastHit hit, maxDistance))
        {
            float safeDistance = Mathf.Max(hit.distance - 0.05f, 0.1f);
            safePosition = playerCamera.position + playerCamera.forward * safeDistance;
            blocked = true;
        }
        else
        {
            safePosition = playerCamera.position + playerCamera.forward * maxDistance;
            blocked = false;
        }

        heldItem.transform.position = safePosition;

        col.enabled = true;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.linearVelocity = Vector3.zero;

        if (blocked)
        {
            rb.AddForce(Vector3.down * 2f, ForceMode.VelocityChange);
        }
        else
        {
            rb.AddForce(playerCamera.forward * throwForce, ForceMode.VelocityChange);
        }

        heldItem.layer = originalLayer;
        heldItem = null;
        rotatingItem = false;
        playerCam.enabled = true;
        ClearTarget();
    }
}
