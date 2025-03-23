using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HighlighterController : MonoBehaviour
{
    [field: SerializeField] public GameObject Prefab_Highlighter { get; private set; }

    [SerializeField] private float dampingFactor = 0.24f;

    public GameObject Highlighter { get; private set; }

    private int cursorX;
    private int cursorY;

    private Vector3 targetPosition;
    private bool bIsTargeted = false;

    private float cellSize = 1.0f;

    private void Awake()
    {
        MapSubsystem.Instance.OnGridGenerated += (cellSize) =>
        {
            if (Prefab_Highlighter != null)
            {
                Highlighter = Instantiate(Prefab_Highlighter);
                Highlighter.transform.localScale = new Vector3(cellSize, cellSize, cellSize);
                Highlighter.SetActive(true);
            }

            cursorX = 0;
            cursorY = 0;

            this.cellSize = cellSize;
        };
    }

    private void Update()
    {
        HandleMouseInput();
    }

    void HandleMouseInput()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, LayerUtils.GroundLayer))
        {
            Vector3 hitPosition = hit.point;
            int x = Mathf.RoundToInt(hitPosition.x / cellSize);
            int y = Mathf.RoundToInt(hitPosition.z / cellSize);

            if (x != cursorX || y != cursorY)
            {
                cursorX = x;
                cursorY = y;

                SetTarget(new Vector3(cursorX * cellSize, 0.1f, cursorY * cellSize));
            }

            if (Input.GetMouseButtonDown(0))
            {
                hit.collider.gameObject.GetComponent<TileComponent>().SetType(TileType.Grass);
            }
        }

    }

    private void FixedUpdate()
    {
        if (bIsTargeted)
        {
            MoveHighlighter();
        }
    }

    void MoveHighlighter()
    {
        if (Highlighter == null)
            return;

        Highlighter.transform.position = Vector3.Lerp(Highlighter.transform.position, targetPosition, dampingFactor);

        if (Vector3.Distance(Highlighter.transform.position, targetPosition) < 0.01f)
        {
            bIsTargeted = false;
            Highlighter.transform.position = targetPosition;
        }
    }

    public void SetTarget(Vector3 newTarget)
    {
        targetPosition = newTarget;
        bIsTargeted = true;
    }

    private void HighlightTile(int x, int y)
    {
        Highlighter.SetActive(true);

        Highlighter.transform.position = new Vector3(x, 0.1f, y);
    }

   
}
